#region

	using System.Collections.Generic;
	using System.Linq;
	using log4net.Core;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.Localization;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class MyMissionBehavior : MissionLogic {
		private static readonly Dictionary<EquipmentElement, int> equipmentToAssign = new(new EquipmentElementComparer());

		public static List<Assignment> assignments = new();

		public static readonly HashSet<Agent> ProcessedAgents = new();

		public static List<ItemObject> LootedItems = new();

		private bool IsMissionEnded;

		public override void AfterStart() {
			base.AfterStart();
			Global.Log("AfterStart", Colors.Green, Level.Debug);
			if (!Mission.DoesMissionRequireCivilianEquipment && Mission.CombatType == Mission.MissionCombatType.Combat) {
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

		public override void OnAgentRemoved(Agent       affectedAgent,
											Agent       affectorAgent,
											AgentState  agentState,
											KillingBlow blow) {
			if (Mission.CombatType == Mission.MissionCombatType.Combat                    &&
				agentState         != null                                                &&
				(agentState == AgentState.Killed || agentState == AgentState.Unconscious) &&
				Global.IsAgentValid(affectedAgent)                                        &&
				Global.IsAgentValid(affectorAgent)                                        &&
				Mission            != null                                                &&
				Mission.PlayerTeam != null                                                &&
				Mission.PlayerTeam.IsValid                                                &&
				!ProcessedAgents.Contains(affectedAgent)                                  &&
				!affectedAgent.Character.IsHero                                           &&
				((!(affectedAgent.Team.IsPlayerTeam || affectedAgent.Team.IsPlayerAlly) &&
				  affectorAgent.Origin.IsUnderPlayersCommand) ||
				 affectedAgent.Origin.IsUnderPlayersCommand)) {
				Global.Log($"agent {affectedAgent.Character.StringId} removed", Colors.Green, Level.Debug);
				_ = ProcessedAgents.Add(affectedAgent);

				// 获取受击部位
				var hitBodyPart = blow.VictimBodyPart;

				// 获取受击部位的护甲
				var armors   = Global.GetAgentArmors(affectedAgent);
				var hitArmor = ArmorSelector.GetRandomArmorByBodyPart(armors, hitBodyPart);

				Global.ProcessAgentEquipment(affectedAgent,
											 item => {
												 if (hitArmor == null || item.StringId != hitArmor.StringId) {
													 LootedItems.Add(item);
													 Global.Log($"{item.StringId} added to LootedItems",
																Colors.Green,
																Level.Debug);
												 }
												 else if (item.StringId == hitArmor.StringId) {
													 Global.Log($"{item.StringId} damaged", Colors.Red, Level.Debug);
												 }
											 });
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

			//CopyToArmory();
		}

		private void AssignArmour() {
			Global.Log("Assigning HeadArmor", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.HeadArmor);
			Global.Log("Assigning HandArmor", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.HandArmor);
			Global.Log("Assigning BodyArmor", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.BodyArmor);
			Global.Log("Assigning LegArmor", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.LegArmor);
			Global.Log("Assigning Cape", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.Cape);
			if (!Mission.IsSiegeBattle) {
				Global.Log("Assigning Horse", Colors.Green, Level.Debug);
				AssignEquipmentType(ItemObject.ItemTypeEnum.Horse);
				Global.Log("Assigning HorseHarness", Colors.Green, Level.Debug);
				AssignEquipmentType(ItemObject.ItemTypeEnum.HorseHarness);
			}
		}

		private void AssignWeaponToUnarmed() {
			Global.Log("AssignWeaponToUnarmed", Colors.Green, Level.Debug);

			foreach (var assignment in assignments)
				if (assignment.IsUnarmed()) {
					Global.Log($"Found unarmed unit Index {assignment.Index}", Colors.Red, Level.Warn);
					var weapon = GetOneRandomMeleeWeapon(assignment.IsMounted);
					if (weapon.HasValue) {
						assignment.Equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, weapon.Value);
						equipmentToAssign[weapon.Value]--;
					}
					else { Global.Log($"Cannot find random melee weapon for {assignment.Index}", Colors.Red, Level.Warn); }
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
						var equipmentNode      = equipmentDeque.First;
						var equipment          = equipmentNode.Value;
						var equipmentItem      = equipment.Key;
						var equipmentItemCount = equipment.Value;

						assignment.Equipment.AddEquipmentToSlotWithoutAgent(slot.Value, equipmentItem);
						Global.Log($"extra equipment {equipmentItem} assigned to {assignment.Character.StringId}#{assignment.Index} on slot {slot.Value}",
								   Colors.Green,
								   Level.Debug);
						equipmentItemCount--;
						if (equipmentToAssign[equipmentItem] > 0) equipmentToAssign[equipmentItem]--;

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
			Global.Log("AssignExtraShield", Colors.Green, Level.Debug);

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
			Global.Log("AssignExtraThrownWeapon", Colors.Green, Level.Debug);

			static bool thrownFilter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty                &&
					   equipment.Key.Item != null            &&
					   Global.IsThrowing(equipment.Key.Item) &&
					   equipment.Value > 0;
			}

			static bool thrownAssignmentFilter(Assignment assignment) { return !assignment.HaveThrown; }

			AssignExtraEquipment(thrownFilter, thrownAssignmentFilter);
		}

		private void AssignExtraArrows() {
			Global.Log("AssignExtraArrows", Colors.Green, Level.Debug);

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
			Global.Log("AssignExtraBolts", Colors.Green, Level.Debug);

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
			Global.Log("AssignExtraTwoHandedWeaponOrPolearms", Colors.Green, Level.Debug);

			static bool filter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty                                                           &&
					   equipment.Key.Item != null                                                       &&
					   (Global.IsTwoHanded(equipment.Key.Item) || Global.IsPolearm(equipment.Key.Item)) &&
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
													  !Global.IsThrowing(equipment.Key.Item)   &&
													  (Global.IsTwoHanded(equipment.Key.Item) ||
													   Global.IsOneHanded(equipment.Key.Item) ||
													   Global.IsPolearm(equipment.Key.Item)) &&
													  (!mounted || Global.IsSuitableForMount(equipment.Key.Item)))
										   .ToList();
			if (weapons.Any()) {
				Global.Log($"(random) weapon {weapons.First().Key.Item.StringId} assigned", Colors.Green, Level.Debug);
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
					!assignment.IsMounted)
					continue;

				while (currentItemIndex < armours.Count && armours[currentItemIndex].Value <= 0) currentItemIndex++;

				if (currentItemIndex >= armours.Count) break; // 没有更多可用装备时退出循环

				var currentItem = armours[currentItemIndex];
				var index       = ItemEnumTypeToEquipmentIndex(itemType);

				if (index.HasValue) {
					assignment.Equipment.AddEquipmentToSlotWithoutAgent(index.Value, currentItem.Key);
					Global.Log($"assign equipment {currentItem.Key.Item.StringId} type {itemType} to {assignment.Character.StringId}#{assignment.Index} on slot {index.Value}",
							   Colors.Green,
							   Level.Debug);

					// 减少当前物品的数量
					var newValue = currentItem.Value - 1;
					armours[currentItemIndex] = new KeyValuePair<EquipmentElement, int>(currentItem.Key, newValue);
					equipmentToAssign[currentItem.Key]--;
				}
			}
		}

		private void AssignWeaponByWeaponClass(bool strict) {
			Global.Log($"AssignWeaponByWeaponClass strict={strict}", Colors.Green, Level.Debug);
			foreach (var assignment in assignments) {
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon0, assignment, assignment.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon1, assignment, assignment.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon2, assignment, assignment.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon3, assignment, assignment.IsMounted, strict);
			}
		}

		private void
			AssignWeaponByWeaponClassBySlot(EquipmentIndex slot, Assignment assignment, bool mounted, bool strict) {
			Global.Log($"AssignWeaponByWeaponClassBySlot slot={slot} character={assignment.Character.StringId}#{assignment.Index} mounted={mounted} strict={strict}",
					   Colors.Green,
					   Level.Debug);
			var referenceWeapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
			var weapon          = assignment.Equipment.GetEquipmentFromSlot(slot);
			if ((weapon.IsEmpty || weapon.Item == null) && !referenceWeapon.IsEmpty && referenceWeapon.Item != null) {
				var weaponClass = Global.GetWeaponClass(referenceWeapon.Item);
				var availableWeapon = equipmentToAssign
									  .Where(equipment => IsWeaponSuitable(equipment.Key, weaponClass, mounted, strict))
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
			Global.Log($"AssignWeaponByItemEnumType strict={strict}", Colors.Green, Level.Debug);

			foreach (var assignment in assignments) {
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon0, assignment, assignment.IsMounted, strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon1, assignment, assignment.IsMounted, strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon2, assignment, assignment.IsMounted, strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon3, assignment, assignment.IsMounted, strict);
			}
		}

		private void
			AssignWeaponByItemEnumTypeBySlot(EquipmentIndex slot, Assignment assignment, bool mounted, bool strict) {
			Global.Log($"AssignWeaponByItemEnumTypeBySlot slot={slot} character={assignment.Character.StringId}#{assignment.Index} mounted={mounted} strict={strict}",
					   Colors.Green,
					   Level.Debug);
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

		public static void Clear() {
			equipmentToAssign.Clear();
			assignments.Clear();
			LootedItems.Clear();
			ProcessedAgents.Clear();
		}

		// 封装判断逻辑
		private bool IsWeaponSuitable(EquipmentElement  equipment,
									  List<WeaponClass> weaponClasses,
									  bool              mounted,
									  bool              strict) {
			if (equipment.IsEmpty                 ||
				equipment.Item == null            ||
				!Global.IsWeapon(equipment.Item)  ||
				equipmentToAssign[equipment] <= 0 ||
				!Global.HaveSameWeaponClass(Global.GetWeaponClass(equipment.Item), weaponClasses))
				return false;

			var isSuitableForMount = Global.IsSuitableForMount(equipment.Item);
			var isCouchable        = Global.IsWeaponCouchable(equipment.Item);

			if (strict) {
				if (mounted)
					// 严格模式下骑马：必须适合骑乘；如果是长杆武器，则必须可进行骑枪冲刺
					return isSuitableForMount && (!Global.IsPolearm(equipment.Item) || isCouchable);

				// 严格模式下非骑马：不可选择可进行骑枪冲刺的武器
				return !isCouchable;
			}

			// 非严格模式下：骑马的不能选择不适合骑乘的武器，非骑马的可以选择任意武器
			return !mounted || isSuitableForMount;
		}

		// 封装判断逻辑
		private bool IsWeaponSuitableByType(KeyValuePair<EquipmentElement, int> equipment,
											ItemObject.ItemTypeEnum             itemType,
											bool                                mounted,
											bool                                strict) {
			if (equipment.Key.IsEmpty                 ||
				equipment.Key.Item == null            ||
				!Global.IsWeapon(equipment.Key.Item)  ||
				equipmentToAssign[equipment.Key] <= 0 ||
				equipment.Key.Item.ItemType      != itemType)
				return false;

			var isSuitableForMount = Global.IsSuitableForMount(equipment.Key.Item);
			var isCouchable        = Global.IsWeaponCouchable(equipment.Key.Item);

			if (strict) {
				if (mounted)
					// 严格模式下骑马：必须适合骑乘；如果是长杆武器，则必须可进行骑枪冲刺
					return isSuitableForMount && (!Global.IsPolearm(equipment.Key.Item) || isCouchable);

				// 严格模式下非骑马：不可选择可进行骑枪冲刺的武器
				return !isCouchable;
			}

			// 非严格模式下：骑马的不能选择不适合骑乘的武器，非骑马的可以选择任意武器
			return !mounted || isSuitableForMount;
		}

		// 封装武器分配逻辑
		private void AssignWeaponIfAvailable(EquipmentIndex                            slot,
											 Assignment                                assignment,
											 List<KeyValuePair<EquipmentElement, int>> availableWeapon) {
			if (availableWeapon.Any()) {
				Global.Log($"weapon {availableWeapon.First().Key.Item.StringId} assigned to {assignment.Character.StringId}#{assignment.Index}",
						   Colors.Green,
						   Level.Debug);
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

		public override void OnRetreatMission() {
			Global.Log("OnRetreatMission() called", Colors.Green, Level.Debug);
			OnMissionEnded(null);
			base.OnRetreatMission();
		}

		public override void OnSurrenderMission() {
			Global.Log("OnSurrenderMission() called", Colors.Green, Level.Debug);
			OnMissionEnded(null);
			base.OnSurrenderMission();
		}

		public override void OnMissionResultReady(MissionResult missionResult) {
			Global.Log("OnMissionResultReady() called", Colors.Green, Level.Debug);
			OnMissionEnded(missionResult);
			base.OnMissionResultReady(missionResult);
		}

		public override InquiryData OnEndMissionRequest(out bool canLeave) {
			Global.Log("OnEndMissionRequest() called", Colors.Green, Level.Debug);
			OnMissionEnded(null);
			return base.OnEndMissionRequest(out canLeave);
		}

		public override void ShowBattleResults() {
			Global.Log("ShowBattleResults() called", Colors.Green, Level.Debug);
			OnMissionEnded(null);
			base.ShowBattleResults();
		}

		public override void OnBattleEnded() {
			Global.Log("OnBattleEnded() called", Colors.Green, Level.Debug);
			OnMissionEnded(null);
			base.OnBattleEnded();
		}

		private void OnMissionEnded(MissionResult? missionResult) {
			Global.Log("OnMissionEnded() called", Colors.Green, Level.Debug);
			if (IsMissionEnded) return;

			missionResult ??= Mission.MissionResult;
			if (Mission.CombatType == Mission.MissionCombatType.Combat &&
				Mission.PlayerTeam != null                             &&
				Mission.PlayerTeam.IsValid                             &&
				Mission.Current.PlayerTeam != null) {
				List<Agent> myAgents = Mission.Agents.Where(agent => Global.IsAgentValid(agent)       &&
																	 agent.Team.IsPlayerTeam          &&
																	 agent.State == AgentState.Active &&
																	 !agent.IsHero                    &&
																	 agent.Origin.IsUnderPlayersCommand)
											  .ToList();
				Global.Log($"{myAgents.Count} player active agent remains on the battlefield", Colors.Green, Level.Debug);
				if (missionResult != null && missionResult.BattleResolved && missionResult.PlayerVictory) {
					Global.Log("player victory", Colors.Green, Level.Debug);
					var lootCount = LootedItems.Count;
					Global.Log($"{lootCount} items looted", Colors.Green, Level.Debug);
					TextObject messageText = new("{=loot_added_message}Added {ITEM_COUNT} items to the army armory.");
					_ = messageText.SetTextVariable("ITEM_COUNT", lootCount);
					InformationManager.DisplayMessage(new InformationMessage(messageText.ToString(), Colors.Green));
					foreach (var item in LootedItems) ArmyArmory.AddItemToArmory(item);

					ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents);
				}
				else if (missionResult == null || !missionResult.BattleResolved || !missionResult.PlayerDefeated) {
					Global.Log("mission ended with player not defeated", Colors.Green, Level.Debug);
					ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents);
				}
			}

			IsMissionEnded = true;
			Clear();
		}

		protected override void OnEndMission() {
			Global.Log("OnEndMission() called", Colors.Green, Level.Debug);
			OnMissionEnded(null);
			base.OnEndMission();
		}

		private delegate bool EquipmentFilter(KeyValuePair<EquipmentElement, int> equipment);

		private delegate bool AssignmentFilter(Assignment assignment);
	}