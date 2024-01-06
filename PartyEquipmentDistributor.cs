#region

	using System.Collections.Generic;
	using System.Linq;
	using log4net.Core;
	using TaleWorlds.CampaignSystem.Party;
	using TaleWorlds.CampaignSystem.Roster;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class PartyEquipmentDistributor {
		private readonly Dictionary<EquipmentElement, int> _equipmentToAssign;
		private readonly ItemRoster?                       _itemRoster;
		private readonly Mission                           _mission;
		private readonly MobileParty                       _party;
		public           List<Assignment>                  assignments = new();

		public PartyEquipmentDistributor(Mission mission, MobileParty party, ItemRoster itemRoster) {
			_mission           = mission;
			_party             = party;
			_itemRoster        = itemRoster;
			_equipmentToAssign = new Dictionary<EquipmentElement, int>(new EquipmentElementComparer());
			Init();
		}

		public PartyEquipmentDistributor(Mission                           mission,
										 MobileParty                       party,
										 Dictionary<EquipmentElement, int> equipmentToAssign) {
			_mission           = mission;
			_party             = party;
			_itemRoster        = null;
			_equipmentToAssign = new Dictionary<EquipmentElement, int>(equipmentToAssign, new EquipmentElementComparer());
			Init();
		}

		public PartyEquipmentDistributor(Mission mission, MobileParty party, Dictionary<ItemObject, int> objectToAssign) {
			_mission           = mission;
			_party             = party;
			_itemRoster        = null;
			_equipmentToAssign = new Dictionary<EquipmentElement, int>(new EquipmentElementComparer());

			foreach (var kv in objectToAssign)
				if (kv.Key != null) {
					var element = new EquipmentElement(kv.Key);

					if (_equipmentToAssign.TryGetValue(element, out var existingCount))
						_equipmentToAssign[element] = existingCount + kv.Value;
					else
						_equipmentToAssign.Add(element, kv.Value);
				}

			Init();
		}

		private void Init() {
			foreach (var troop in _party.MemberRoster.GetTroopRoster())
				for (var i = 0; i < troop.Number - troop.WoundedNumber; i++)
					if (!troop.Character.IsHero)
						assignments.Add(new Assignment(troop.Character));

			assignments = assignments.OrderByDescending(assignment => assignment.Character.Tier)
									 .ThenByDescending(assignment => assignment.Character.Level)
									 .ToList();
			if (_itemRoster != null)
				foreach (var kv in _itemRoster)
					if (!kv.IsEmpty && !kv.EquipmentElement.IsEmpty) {
						// 尝试获取已存在的数量
						if (!_equipmentToAssign.TryGetValue(kv.EquipmentElement, out var existingAmount))
							// 如果键不存在，添加新的键值对
							_equipmentToAssign.Add(kv.EquipmentElement, kv.Amount);
						else
							// 如果键已存在，更新数量
							_equipmentToAssign[kv.EquipmentElement] = existingAmount + kv.Amount;
					}

			DoAssign();
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
		}

		private void AssignArmour() {
			Global.Log($"Assigning HeadArmor for {_party.Name}", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.HeadArmor);
			Global.Log($"Assigning HandArmor for {_party.Name}", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.HandArmor);
			Global.Log($"Assigning BodyArmor for {_party.Name}", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.BodyArmor);
			Global.Log($"Assigning LegArmor for {_party.Name}", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.LegArmor);
			Global.Log($"Assigning Cape for {_party.Name}", Colors.Green, Level.Debug);
			AssignEquipmentType(ItemObject.ItemTypeEnum.Cape);
			if (!_mission.IsSiegeBattle) {
				Global.Log($"Assigning Horse for {_party.Name}", Colors.Green, Level.Debug);
				AssignEquipmentType(ItemObject.ItemTypeEnum.Horse);
				Global.Log($"Assigning HorseHarness for {_party.Name}", Colors.Green, Level.Debug);
				AssignEquipmentType(ItemObject.ItemTypeEnum.HorseHarness);
			}
		}

		private void AssignWeaponToUnarmed() {
			Global.Log($"AssignWeaponToUnarmed for {_party.Name}", Colors.Green, Level.Debug);

			foreach (var assignment in assignments)
				if (assignment.IsUnarmed()) {
					Global.Log($"Found unarmed unit Index {assignment.Index} for {_party.Name}", Colors.Red, Level.Warn);
					var weapon = GetOneRandomMeleeWeapon(assignment);
					if (weapon.HasValue) {
						assignment.Equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, weapon.Value);
						_equipmentToAssign[weapon.Value]--;
					}
					else {
						Global.Log($"Cannot find random melee weapon for {assignment.Index} for {_party.Name}",
								   Colors.Red,
								   Level.Warn);
					}
				}
		}

		private void AssignExtraEquipment(EquipmentFilter equipmentFilter, AssignmentFilter assignmentFilter) {
			var equipmentQuery = _equipmentToAssign
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
						Global.Log($"extra equipment {equipmentItem} assigned to {assignment.Character.StringId}#{assignment.Index} on slot {slot.Value} for {_party.Name}",
								   Colors.Green,
								   Level.Debug);
						equipmentItemCount--;
						if (_equipmentToAssign[equipmentItem] > 0) _equipmentToAssign[equipmentItem]--;

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
			Global.Log($"AssignExtraShield for {_party.Name}", Colors.Green, Level.Debug);

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
			Global.Log($"AssignExtraThrownWeapon for {_party.Name}", Colors.Green, Level.Debug);

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
			Global.Log($"AssignExtraArrows for {_party.Name}", Colors.Green, Level.Debug);

			static bool arrowFilter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty             &&
					   equipment.Key.Item != null         &&
					   Global.IsArrow(equipment.Key.Item) &&
					   equipment.Value > 0;
			}

			static bool arrowAssignmentFilter(Assignment assignment) { return assignment.IsArcher; }

			AssignExtraEquipment(arrowFilter, arrowAssignmentFilter);
		}

		private void AssignExtraBolts() {
			Global.Log($"AssignExtraBolts for {_party.Name}", Colors.Green, Level.Debug);

			static bool boltFilter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty            &&
					   equipment.Key.Item != null        &&
					   Global.IsBolt(equipment.Key.Item) &&
					   equipment.Value > 0;
			}

			static bool boltAssignmentFilter(Assignment assignment) { return assignment.IsCrossBowMan; }

			AssignExtraEquipment(boltFilter, boltAssignmentFilter);
		}

		private void AssignExtraTwoHandedWeaponOrPolearms() {
			Global.Log($"AssignExtraTwoHandedWeaponOrPolearms for {_party.Name}", Colors.Green, Level.Debug);

			static bool filter(KeyValuePair<EquipmentElement, int> equipment) {
				return !equipment.Key.IsEmpty                                                           &&
					   equipment.Key.Item != null                                                       &&
					   (Global.IsTwoHanded(equipment.Key.Item) || Global.IsPolearm(equipment.Key.Item)) &&
					   equipment.Value > 0;
			}

			static bool assignmentFilter(Assignment assignment) { return !assignment.HaveTwoHandedWeaponOrPolearms; }

			AssignExtraEquipment(filter, assignmentFilter);
		}

		private EquipmentElement? GetOneRandomMeleeWeapon(Assignment assignment) {
			var weapons = _equipmentToAssign.Where(equipment =>
													   !equipment.Key.IsEmpty                    &&
													   equipment.Key.Item                != null &&
													   _equipmentToAssign[equipment.Key] > 0     &&
													   Global.IsWeapon(equipment.Key.Item)       &&
													   Global.IsSuitableForCharacter(equipment.Key.Item,
														   assignment.Character)              &&
													   !Global.IsThrowing(equipment.Key.Item) &&
													   (Global.IsTwoHanded(equipment.Key.Item) ||
														Global.IsOneHanded(equipment.Key.Item) ||
														Global.IsPolearm(equipment.Key.Item)) &&
													   (!assignment.IsMounted ||
														Global.IsSuitableForMount(equipment.Key.Item)))
											.ToList();
			if (weapons.Any()) {
				Global.Log($"(random) weapon {weapons.First().Key.Item.StringId} assigned for {_party.Name}",
						   Colors.Green,
						   Level.Debug);
				_equipmentToAssign[weapons.First().Key]--;
				return weapons.First().Key;
			}

			return null;
		}

		private void AssignEquipmentType(ItemObject.ItemTypeEnum itemType) {
			var armours = _equipmentToAssign
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
					Global.Log($"assign equipment {currentItem.Key.Item.StringId} type {itemType} to {assignment.Character.StringId}#{assignment.Index} on slot {index.Value} for {_party.Name}",
							   Colors.Green,
							   Level.Debug);

					// 减少当前物品的数量
					var newValue = currentItem.Value - 1;
					armours[currentItemIndex] = new KeyValuePair<EquipmentElement, int>(currentItem.Key, newValue);
					_equipmentToAssign[currentItem.Key]--;
				}
			}
		}

		private void AssignWeaponByWeaponClass(bool strict) {
			Global.Log($"AssignWeaponByWeaponClass strict={strict} for {_party.Name}", Colors.Green, Level.Debug);
			foreach (var assignment in assignments) {
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon0, assignment, assignment.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon1, assignment, assignment.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon2, assignment, assignment.IsMounted, strict);
				AssignWeaponByWeaponClassBySlot(EquipmentIndex.Weapon3, assignment, assignment.IsMounted, strict);
			}
		}

		private void
			AssignWeaponByWeaponClassBySlot(EquipmentIndex slot, Assignment assignment, bool mounted, bool strict) {
			Global.Log($"AssignWeaponByWeaponClassBySlot slot={slot} character={assignment.Character.StringId}#{assignment.Index} mounted={mounted} strict={strict} for {_party.Name}",
					   Colors.Green,
					   Level.Debug);
			var referenceWeapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
			var weapon          = assignment.Equipment.GetEquipmentFromSlot(slot);
			if ((weapon.IsEmpty || weapon.Item == null) && !referenceWeapon.IsEmpty && referenceWeapon.Item != null) {
				var availableWeapon = _equipmentToAssign
									  .Where(equipment =>
												 IsWeaponSuitable(equipment.Key, referenceWeapon.Item, assignment, strict))
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
			Global.Log($"AssignWeaponByItemEnumType strict={strict} for {_party.Name}", Colors.Green, Level.Debug);

			foreach (var assignment in assignments) {
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon0, assignment, assignment.IsMounted, strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon1, assignment, assignment.IsMounted, strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon2, assignment, assignment.IsMounted, strict);
				AssignWeaponByItemEnumTypeBySlot(EquipmentIndex.Weapon3, assignment, assignment.IsMounted, strict);
			}
		}

		private void
			AssignWeaponByItemEnumTypeBySlot(EquipmentIndex slot, Assignment assignment, bool mounted, bool strict) {
			Global.Log($"AssignWeaponByItemEnumTypeBySlot slot={slot} character={assignment.Character.StringId}#{assignment.Index} mounted={mounted} strict={strict} for {_party.Name}",
					   Colors.Green,
					   Level.Debug);
			var referenceWeapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
			var weapon          = assignment.Equipment.GetEquipmentFromSlot(slot);
			if ((weapon.IsEmpty || weapon.Item == null) && !(referenceWeapon.IsEmpty || referenceWeapon.Item == null)) {
				var availableWeapon = _equipmentToAssign
									  .Where(equipment =>
												 IsWeaponSuitableByType(equipment,
																		referenceWeapon.Item.ItemType,
																		assignment,
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

		// 封装判断逻辑
		private bool IsWeaponSuitable(EquipmentElement equipment,
									  ItemObject       referenceWeapon,
									  Assignment       assignment,
									  bool             strict) {
			if (equipment.IsEmpty                                                    ||
				equipment.Item == null                                               ||
				!Global.IsWeapon(equipment.Item)                                     ||
				_equipmentToAssign[equipment] <= 0                                   ||
				!Global.IsSuitableForCharacter(equipment.Item, assignment.Character) ||
				!Global.HaveSameWeaponClass(Global.GetWeaponClass(equipment.Item), Global.GetWeaponClass(referenceWeapon)))
				return false;

			var isSuitableForMount = Global.IsSuitableForMount(equipment.Item);
			var isCouchable        = Global.IsWeaponCouchable(equipment.Item);

			if (strict) {
				if (!Global.FullySameWeaponClass(equipment.Item, referenceWeapon)) return false;

				if (assignment.IsMounted)
					// 严格模式下骑马：必须适合骑乘；如果是长杆武器，则必须可进行骑枪冲刺
					return isSuitableForMount && (!Global.IsPolearm(equipment.Item) || isCouchable);

				// 严格模式下非骑马：不可选择可进行骑枪冲刺的武器
				return !isCouchable;
			}

			// 非严格模式下：骑马的不能选择不适合骑乘的武器，非骑马的可以选择任意武器
			return !assignment.IsMounted || isSuitableForMount;
		}

		// 封装判断逻辑
		private bool IsWeaponSuitableByType(KeyValuePair<EquipmentElement, int> equipment,
											ItemObject.ItemTypeEnum             itemType,
											Assignment                          assignment,
											bool                                strict) {
			if (equipment.Key.IsEmpty                                                    ||
				equipment.Key.Item == null                                               ||
				!Global.IsWeapon(equipment.Key.Item)                                     ||
				_equipmentToAssign[equipment.Key] <= 0                                   ||
				!Global.IsSuitableForCharacter(equipment.Key.Item, assignment.Character) ||
				equipment.Key.Item.ItemType != itemType)
				return false;

			var isSuitableForMount = Global.IsSuitableForMount(equipment.Key.Item);
			var isCouchable        = Global.IsWeaponCouchable(equipment.Key.Item);

			if (strict) {
				if (assignment.IsMounted)
					// 严格模式下骑马：必须适合骑乘；如果是长杆武器，则必须可进行骑枪冲刺
					return isSuitableForMount && (!Global.IsPolearm(equipment.Key.Item) || isCouchable);

				// 严格模式下非骑马：不可选择可进行骑枪冲刺的武器
				return !isCouchable;
			}

			// 非严格模式下：骑马的不能选择不适合骑乘的武器，非骑马的可以选择任意武器
			return !assignment.IsMounted || isSuitableForMount;
		}

		// 封装武器分配逻辑
		private void AssignWeaponIfAvailable(EquipmentIndex                            slot,
											 Assignment                                assignment,
											 List<KeyValuePair<EquipmentElement, int>> availableWeapon) {
			if (availableWeapon.Any()) {
				Global.Log($"weapon {availableWeapon.First().Key.Item.StringId} assigned to {assignment.Character.StringId}#{assignment.Index} for {_party.Name}",
						   Colors.Green,
						   Level.Debug);
				assignment.Equipment.AddEquipmentToSlotWithoutAgent(slot, availableWeapon.First().Key);
				_equipmentToAssign[availableWeapon.First().Key]--;
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

		public void Spawn(Equipment equipment) {
			// 确保 PartyArmories 包含特定的 _party.Id
			if (!EveryoneCampaignBehavior.PartyArmories.TryGetValue(_party.Id, out var partyArmory)) {
				partyArmory                                       = new Dictionary<ItemObject, int>();
				EveryoneCampaignBehavior.PartyArmories[_party.Id] = partyArmory;
			}

			foreach (var slot in Global.EquipmentSlots) {
				var element = equipment.GetEquipmentFromSlot(slot);
				if (!element.IsEmpty && element.Item != null) {
					if (partyArmory.TryGetValue(element.Item, out var itemCount) && itemCount > 0) {
						// 武器库中有足够的物品，分配一个并减少数量
						partyArmory[element.Item] = itemCount - 1;
						Global.Log($"Spawned item {element.Item.StringId}", Colors.Green, Level.Debug);
					}
					else {
						// 武器库中没有足够的物品或者该物品不存在
						Global.Log($"Insufficient or no items to spawn {element.Item.StringId}", Colors.Red, Level.Warn);
					}
				}
			}
		}

		public void ReturnItem(ItemObject? item, int count) {
			if (item == null || count <= 0) {
				Global.Log("Invalid item or count for return.", Colors.Red, Level.Warn);
				return;
			}

			// 确保 PartyArmories 包含特定的 _party.Id
			if (!EveryoneCampaignBehavior.PartyArmories.TryGetValue(_party.Id, out var partyArmory)) {
				partyArmory                                       = new Dictionary<ItemObject, int>();
				EveryoneCampaignBehavior.PartyArmories[_party.Id] = partyArmory;
			}

			// 如果武器库中已经有这个物品，增加数量；否则，添加新的条目
			if (partyArmory.TryGetValue(item, out var existingCount))
				partyArmory[item] = existingCount + count;
			else
				partyArmory[item] = count;

			Global.Log($"Returned {count} of item {item.StringId} to party {_party.Name}.", Colors.Green, Level.Debug);
		}


		private delegate bool EquipmentFilter(KeyValuePair<EquipmentElement, int> equipment);

		private delegate bool AssignmentFilter(Assignment assignment);
	}