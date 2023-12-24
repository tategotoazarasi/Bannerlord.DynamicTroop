#region

	using System.Collections.Generic;
	using System.Linq;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class MyMissionBehavior : MissionLogic {
		private static readonly Dictionary<EquipmentElement, int> equipmentToAssign = new(new EquipmentElementComparer());

		public static List<Assignment> assignments = new();

		public static readonly HashSet<Agent> ProcessedAgents = new();

		public static List<ItemObject> LootedItems = new();

		public override MissionBehaviorType BehaviorType => MissionBehaviorType.Logic;

		public override void AfterStart() {
			base.AfterStart();
			Global.Log("Mission Start");
			if (!Mission.DoesMissionRequireCivilianEquipment && Mission.CombatType == Mission.MissionCombatType.Combat) {
				List<Agent> playerAgents = Mission.AllAgents
												  .Where(agent => agent.Team != null &&
																  agent.IsHuman      &&
																  !agent.IsHero      &&
																  agent.Origin.IsUnderPlayersCommand)
												  .OrderByDescending(agent => agent.Character.GetBattleTier())
												  .ThenByDescending(agent => agent.Character.Level)
												  .ToList();
				foreach (var troop in Campaign.Current.MainParty.MemberRoster.GetTroopRoster())
					for (var i = 0; i < troop.Number - troop.WoundedNumber; i++)
						if (!troop.Character.IsHero)
							assignments.Add(new Assignment(troop.Character));

				assignments = assignments.OrderByDescending(assignment => assignment.Character.Tier)
										 .ThenByDescending(assignment => assignment.Character.Level)
										 .ToList();

				foreach (var kv in ArmyArmory.Armory)
					if (!kv.IsEmpty && !kv.EquipmentElement.IsEmpty) {
						// 尝试获取已存在的数量
						if (!equipmentToAssign.TryGetValue(kv.EquipmentElement, out var existingAmount))
							// 如果键不存在，添加新的键值对
							equipmentToAssign.Add(kv.EquipmentElement, kv.Amount);
						else
							// 如果键已存在，更新数量
							equipmentToAssign[kv.EquipmentElement] = existingAmount + kv.Amount;
					}

				DoAssign();
			}
		}

		/*public override void OnAgentBuild(Agent agent, Banner banner) {
			base.OnAgentBuild(agent, banner);
			if (Global.IsAgentValid(agent) && agent.Origin.IsUnderPlayersCommand && !agent.IsHero) {
				Global.Log("creating agent");
				var characterStringId = agent.Character.StringId;
				var assignment =
					assignments.FirstOrDefault(a => !a.IsAssigned && a.Character.StringId == characterStringId);

				if (assignment != null) {

					// 确保equipment不为空
					//assignment.EquipAgent(agent);
					//assignment.EquipAnother(agent.SpawnEquipment);
					agent.InitializeSpawnEquipment(assignment.Equipment);
					agent.EquipItemsFromSpawnEquipment(true);
					assignment.IsAssigned = true;
				}
			}
		}*/

		public override void OnAgentRemoved(Agent       affectedAgent,
											Agent       affectorAgent,
											AgentState  agentState,
											KillingBlow blow) {
			if (Mission.CombatType == Mission.MissionCombatType.Combat                    &&
				Global.IsAgentValid(affectedAgent)                                        &&
				Global.IsAgentValid(affectorAgent)                                        &&
				Mission            != null                                                &&
				Mission.PlayerTeam != null                                                &&
				Mission.PlayerTeam.IsValid                                                &&
				(agentState == AgentState.Killed || agentState == AgentState.Unconscious) &&
				!ProcessedAgents.Contains(affectedAgent)                                  &&
				!affectedAgent.Character.IsHero                                           &&
				((!(affectedAgent.Team.IsPlayerTeam || affectedAgent.Team.IsPlayerAlly) &&
				  affectorAgent.Origin.IsUnderPlayersCommand) ||
				 affectedAgent.Origin.IsUnderPlayersCommand)) {
				_ = ProcessedAgents.Add(affectedAgent);
				var enemyEquipment = affectedAgent.SpawnEquipment;
				if (enemyEquipment == null || !enemyEquipment.IsValid) return;

				foreach (var slot in Global.EquipmentSlots) {
					var element = enemyEquipment.GetEquipmentFromSlot(slot);
					if (!element.IsEmpty && element.Item != null) {
						// 非英雄士兵杀死或击晕敌人，装备进入军械库
						LootedItems.Add(element.Item);
						Global.Log($"{element.Item.StringId} added to armory");
					}
				}
			}

			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		}

		private void DoAssign() {
			AssignArmour();
			AssignWeaponByWeaponClass(true);
			AssignWeaponByWeaponClass(false);
			AssignWeaponByItemEnumType(true);
			AssignWeaponByItemEnumType(false);
			AssignWeaponToUnarmed();
			AssignExtraArrows();
			AssignExtraBolts();
			AssignExtraShield();
			AssignExtraThrownWeapon();
			AssignExtraTwoHandedWeaponOrPolearms();
			CopyToArmory();
		}

		private void AssignArmour() {
			AssignEquipmentType(ItemObject.ItemTypeEnum.HeadArmor);
			AssignEquipmentType(ItemObject.ItemTypeEnum.HandArmor);
			AssignEquipmentType(ItemObject.ItemTypeEnum.BodyArmor);
			AssignEquipmentType(ItemObject.ItemTypeEnum.LegArmor);
			AssignEquipmentType(ItemObject.ItemTypeEnum.Cape);
			AssignEquipmentType(ItemObject.ItemTypeEnum.Horse);
			AssignEquipmentType(ItemObject.ItemTypeEnum.HorseHarness);
		}

		private void AssignWeaponToUnarmed() {
			foreach (var assignment in assignments)
				if (assignment.IsUnarmed()) {
					Global.Log($"Found unarmed unit Index {assignment.Index}");
					var weapon = GetOneRandomMeleeWeapon(assignment.Character.IsMounted);
					if (weapon.HasValue) {
						assignment.Equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, weapon.Value);
						equipmentToAssign[weapon.Value]--;
					}
				}
		}

		private void AssignExtraEquipment(EquipmentFilter equipmentFilter, AssignmentFilter assignmentFilter) {
			var equipmentQuery = equipmentToAssign
								 .Where(equipment =>
											!equipment.Key.IsEmpty     &&
											equipment.Key.Item != null &&
											equipment.Value    > 0     &&
											equipmentFilter(equipment))
								 .OrderByDescending(equipment => equipment.Key.Item.Tier)
								 .ThenByDescending(equipment => equipment.Key.Item.Value);

			LinkedList<KeyValuePair<EquipmentElement, int>> equipmentDeque = new(equipmentQuery);

			if (!equipmentDeque.Any()) return;

			foreach (var assignment in assignments)
				if (assignmentFilter(assignment)) {
					var slot = assignment.EmptyWeaponSlot;
					if (slot.HasValue) {
						/* 项目“Bannerlord.DynamicTroop (netcoreapp3.1)”的未合并的更改
						在此之前:
											var equipmentNode = equipmentDeque.First;
						在此之后:
											var int>>? equipmentNode = equipmentDeque.First;
						*/
						var equipmentNode      = equipmentDeque.First;
						var equipment          = equipmentNode.Value;
						var equipmentItem      = equipment.Key;
						var equipmentItemCount = equipment.Value;

						assignment.Equipment.AddEquipmentToSlotWithoutAgent(slot.Value, equipmentItem);

						equipmentItemCount--;
						if (equipmentItemCount > 0)
							equipmentNode.Value =
								new KeyValuePair<EquipmentElement, int>(equipmentItem, equipmentItemCount);
						else
							equipmentDeque.RemoveFirst();

						if (!equipmentDeque.Any()) return;
					}
				}
		}

		// 使用示例
		private void AssignExtraShield() {
			static bool shieldFilter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty                                        &&
					   equipment.Key.Item          != null                           &&
					   equipment.Key.Item.ItemType == ItemObject.ItemTypeEnum.Shield &&
					   equipment.Value             > 0;
			}

			static bool shieldAssignmentFilter(Assignment assignment) {
				return assignment.CanBeShielded && !assignment.IsShielded;
			}

			AssignExtraEquipment(shieldFilter, shieldAssignmentFilter);
		}

		private void AssignExtraThrownWeapon() {
			static bool thrownFilter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty                                        &&
					   equipment.Key.Item          != null                           &&
					   equipment.Key.Item.ItemType == ItemObject.ItemTypeEnum.Thrown &&
					   equipment.Value             > 0;
			}

			static bool thrownAssignmentFilter(Assignment assignment) { return !assignment.HaveThrown; }

			AssignExtraEquipment(thrownFilter, thrownAssignmentFilter);
		}

		private void AssignExtraArrows() {
			static bool arrowFilter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty                                        &&
					   equipment.Key.Item          != null                           &&
					   equipment.Key.Item.ItemType == ItemObject.ItemTypeEnum.Arrows &&
					   equipment.Value             > 0;
			}

			static bool arrowAssignmentFilter(Assignment assignment) { return assignment.IsArcher; }

			AssignExtraEquipment(arrowFilter, arrowAssignmentFilter);
		}

		private void AssignExtraBolts() {
			static bool boltFilter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty                                       &&
					   equipment.Key.Item          != null                          &&
					   equipment.Key.Item.ItemType == ItemObject.ItemTypeEnum.Bolts &&
					   equipment.Value             > 0;
			}

			static bool boltAssignmentFilter(Assignment assignment) { return assignment.IsCrossBowMan; }

			AssignExtraEquipment(boltFilter, boltAssignmentFilter);
		}

		private void AssignExtraTwoHandedWeaponOrPolearms() {
			static bool filter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty     &&
					   equipment.Key.Item != null &&
					   (equipment.Key.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
						equipment.Key.Item.ItemType == ItemObject.ItemTypeEnum.Polearm) &&
					   equipment.Value > 0;
			}

			static bool assignmentFilter(Assignment assignment) { return !assignment.HaveTwoHandedWeaponOrPolearms; }

			AssignExtraEquipment(filter, assignmentFilter);
		}

		private EquipmentElement? GetOneRandomMeleeWeapon(bool mounted) {
			var weapons = equipmentToAssign.Where(equipment =>
													  !equipment.Key.IsEmpty                   &&
													  equipment.Key.Item               != null &&
													  equipmentToAssign[equipment.Key] > 0     &&
													  Global.IsWeapon(equipment.Key.Item)      &&
													  (equipment.Key.Item.ItemType ==
													   ItemObject.ItemTypeEnum.OneHandedWeapon ||
													   equipment.Key.Item.ItemType ==
													   ItemObject.ItemTypeEnum.TwoHandedWeapon ||
													   equipment.Key.Item.ItemType == ItemObject.ItemTypeEnum.Polearm) &&
													  (!mounted || Global.IsSuitableForMount(equipment.Key.Item)))
										   .ToList();
			if (weapons.Any()) {
				Global.Log($"(class) weapon {weapons.First().Key.Item.StringId} assigned");
				equipmentToAssign[weapons.First().Key]--;
				return weapons.First().Key;
			}

			return null;
		}

		private void AssignEquipmentType(ItemObject.ItemTypeEnum itemType) {
			var armours = equipmentToAssign
						  .Where(kv => kv.Value > 0                 &&
									   !kv.Key.IsEmpty              &&
									   kv.Key.Item          != null &&
									   kv.Key.Item.ItemType == itemType)
						  .OrderByDescending(kv => kv.Key.Item.Tier)
						  .ThenByDescending(kv => kv.Key.Item.Value)
						  .ToList();

			var currentItemIndex = 0;
			foreach (var assignment in assignments) {
				if ((itemType == ItemObject.ItemTypeEnum.Horse || itemType == ItemObject.ItemTypeEnum.HorseHarness) &&
					!assignment.Character.IsMounted)
					continue;

				while (currentItemIndex < armours.Count && armours[currentItemIndex].Value <= 0) currentItemIndex++;

				if (currentItemIndex >= armours.Count) break; // 没有更多可用装备时退出循环

				var currentItem = armours[currentItemIndex];
				var index       = ItemEnumTypeToEquipmentIndex(itemType);

				if (index.HasValue) {
					assignment.Equipment.AddEquipmentToSlotWithoutAgent(index.Value, currentItem.Key);

					// 减少当前物品的数量
					var newValue = currentItem.Value - 1;
					armours[currentItemIndex] = new KeyValuePair<EquipmentElement, int>(currentItem.Key, newValue);
					equipmentToAssign[currentItem.Key]--;
				}
			}
		}

		private void AssignWeaponByWeaponClass(bool strict) {
			foreach (var assignment in assignments) {
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon0, assignment, assignment.Character.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon1, assignment, assignment.Character.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon2, assignment, assignment.Character.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon3, assignment, assignment.Character.IsMounted, strict);
			}
		}

		private void
			AssignWeaponByWeaponClassBySlot(EquipmentIndex slot, Assignment assignment, bool mounted, bool strict) {
			var weapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
			if (!weapon.IsEmpty && weapon.Item != null) {
				var weaponClass = Global.GetWeaponClass(weapon.Item);
				var availableWeapon = equipmentToAssign
									  .Where(equipment => IsWeaponSuitable(equipment, weaponClass, mounted, strict))
									  .OrderByDescending(equipment =>
															 (int)equipment.Key.Item.Tier +
															 CalculateWeaponTierBonus(equipment.Key.Item, mounted))
									  .ThenByDescending(equipment => equipment.Key.Item.Value)
									  .Take(1)
									  .ToList();

				AssignWeaponIfAvailable(slot, assignment, availableWeapon);
			}
		}

		private void AssignWeaponByItemEnumType(bool strict) {
			foreach (var assignment in assignments) {
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon0,
												 assignment,
												 assignment.Character.IsMounted,
												 strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon1,
												 assignment,
												 assignment.Character.IsMounted,
												 strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon2,
												 assignment,
												 assignment.Character.IsMounted,
												 strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon3,
												 assignment,
												 assignment.Character.IsMounted,
												 strict);
			}
		}

		private void
			AssignWeaponByItemEnumTypeBySlot(EquipmentIndex slot, Assignment assignment, bool mounted, bool strict) {
			var referenceWeapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
			var weapon          = assignment.Equipment.GetEquipmentFromSlot(slot);
			if ((weapon.IsEmpty || weapon.Item == null) && !(referenceWeapon.IsEmpty || referenceWeapon.Item == null)) {
				var availableWeapon = equipmentToAssign
									  .Where(equipment =>
												 IsWeaponSuitableByType(equipment,
																		referenceWeapon.Item.ItemType,
																		mounted,
																		strict))
									  .OrderByDescending(equipment =>
															 (int)equipment.Key.Item.Tier +
															 CalculateWeaponTierBonus(equipment.Key.Item, mounted))
									  .ThenByDescending(equipment => equipment.Key.Item.Value)
									  .Take(1)
									  .ToList();

				AssignWeaponIfAvailable(slot, assignment, availableWeapon);
			}
		}

		private EquipmentIndex? ItemEnumTypeToEquipmentIndex(ItemObject.ItemTypeEnum itemType) {
			return itemType switch {
					   ItemObject.ItemTypeEnum.HeadArmor    => EquipmentIndex.Head,
					   ItemObject.ItemTypeEnum.HandArmor    => EquipmentIndex.Gloves,
					   ItemObject.ItemTypeEnum.BodyArmor    => EquipmentIndex.Body,
					   ItemObject.ItemTypeEnum.LegArmor     => EquipmentIndex.Leg,
					   ItemObject.ItemTypeEnum.Cape         => EquipmentIndex.Cape,
					   ItemObject.ItemTypeEnum.Horse        => EquipmentIndex.Horse,
					   ItemObject.ItemTypeEnum.HorseHarness => EquipmentIndex.HorseHarness,
					   _                                    => null
				   };
		}

		public void CopyToArmory() {
			ArmyArmory.Armory.Clear();
			foreach (var equipment in equipmentToAssign) _ = ArmyArmory.Armory.AddToCounts(equipment.Key, equipment.Value);
		}

		public static void Clear() {
			equipmentToAssign.Clear();
			assignments.Clear();
			LootedItems.Clear();
			ProcessedAgents.Clear();
		}

		private void LogAssignment(EquipmentElement equipment, EquipmentIndex slot, Assignment assignment) {
			Global.Log($"We got {equipmentToAssign[equipment]} (class) weapon {equipment.Item.StringId}");
			Global.Log($"(class) weapon {equipment.Item.StringId} assigned");
			assignment.Equipment.AddEquipmentToSlotWithoutAgent(slot, equipment);
			equipmentToAssign[equipment]--;
			Global.Log($"(class) weapon {equipment.Item.StringId} left {equipmentToAssign[equipment]}");
		}

		// 封装判断逻辑
		private bool IsWeaponSuitable(KeyValuePair<EquipmentElement, int> equipment,
									  WeaponClass?                        weaponClass,
									  bool                                mounted,
									  bool                                strict) {
			return !equipment.Key.IsEmpty                                      &&
				   equipment.Key.Item               != null                    &&
				   equipmentToAssign[equipment.Key] > 0                        &&
				   Global.IsWeapon(equipment.Key.Item)                         &&
				   Global.GetWeaponClass(equipment.Key.Item) == weaponClass    &&
				   (!mounted || Global.IsSuitableForMount(equipment.Key.Item)) &&
				   (!strict  || mounted || !Global.IsWeaponCouchable(equipment.Key.Item));
		}

		// 封装判断逻辑
		private bool IsWeaponSuitableByType(KeyValuePair<EquipmentElement, int> equipment,
											ItemObject.ItemTypeEnum             itemType,
											bool                                mounted,
											bool                                strict) {
			return !equipment.Key.IsEmpty                                      &&
				   equipment.Key.Item               != null                    &&
				   equipmentToAssign[equipment.Key] > 0                        &&
				   Global.IsWeapon(equipment.Key.Item)                         &&
				   equipment.Key.Item.ItemType == itemType                     &&
				   (!mounted || Global.IsSuitableForMount(equipment.Key.Item)) &&
				   (!strict  || mounted || !Global.IsWeaponCouchable(equipment.Key.Item));
		}

		// 封装武器分配逻辑
		private void AssignWeaponIfAvailable(EquipmentIndex                            slot,
											 Assignment                                assignment,
											 List<KeyValuePair<EquipmentElement, int>> availableWeapon) {
			if (availableWeapon.Any()) {
				Global.Log($"(type) weapon {availableWeapon.First().Key.Item.StringId} assigned");
				assignment.Equipment.AddEquipmentToSlotWithoutAgent(slot, availableWeapon.First().Key);
				equipmentToAssign[availableWeapon.First().Key]--;
			}
		}

		// 计算基于武器属性的Tier加成
		private int CalculateWeaponTierBonus(ItemObject weapon, bool mounted) {
			if (mounted) return 0; // 如果骑马，则不应用任何加成

			var                                 bonus       = 0;
			MBReadOnlyList<WeaponComponentData> weaponFlags = weapon.WeaponComponent.Weapons;
			WeaponFlags                         weaponFlag  = 0;
			foreach (var flag in weaponFlags) weaponFlag    |= flag.WeaponFlags;

			// 为每个匹配的WeaponFlag增加加成
			if (weaponFlag.HasFlag(WeaponFlags.BonusAgainstShield)) bonus++;

			if (weaponFlag.HasFlag(WeaponFlags.CanKnockDown)) bonus++;

			if (weaponFlag.HasFlag(WeaponFlags.CanDismount)) bonus++;

			if (weaponFlag.HasFlag(WeaponFlags.MultiplePenetration)) bonus++;

			if (Global.IsWeaponBracable(weapon)) bonus++;

			return bonus;
		}

		private delegate bool EquipmentFilter(KeyValuePair<EquipmentElement, int> equipment);

		private delegate bool AssignmentFilter(Assignment assignment);
	}