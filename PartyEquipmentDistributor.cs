using System;
using System.Collections.Generic;
using System.Linq;

using log4net.Core;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop;

public class PartyEquipmentDistributor {
	private readonly Dictionary<EquipmentElement, int> _equipmentToAssign;

	private readonly ItemRoster? _itemRoster;

	private readonly Mission _mission;

	private readonly MobileParty _party;

	public List<Assignment> Assignments = new();

	public PartyEquipmentDistributor(Mission mission, MobileParty party, ItemRoster itemRoster) {
		_mission           = mission;
		_party             = party;
		_itemRoster        = itemRoster;
		_equipmentToAssign = new Dictionary<EquipmentElement, int>(new EquipmentElementComparer());
		Init();
	}

	public PartyEquipmentDistributor(Mission                            mission,
									 MobileParty                        party,
									 IDictionary<EquipmentElement, int> equipmentToAssign) {
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
				EquipmentElement element = new(kv.Key);

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
					Assignments.Add(new Assignment(troop.Character));

		Assignments = Assignments.OrderByDescending(assignment => assignment.Character.Tier)
								 .ThenByDescending(assignment => assignment.Character.Level)
								 .ToListQ();
		if (_itemRoster != null)
			foreach (var kv in _itemRoster) {
				if (kv is not { IsEmpty: false, EquipmentElement.IsEmpty: false }) continue;

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
		var unarmedAssignments = Assignments.WhereQ(assignment => assignment.IsUnarmed()).ToListQ();
		foreach (var assignment in unarmedAssignments) {
			Global.Warn($"Found unarmed unit Index {assignment.Index} for {_party.Name}");
			var weapon = GetOneRandomMeleeWeapon(assignment);
			if (weapon.HasValue) {
				assignment.Equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, weapon.Value);
				_equipmentToAssign[weapon.Value]--;
			}
			else { Global.Warn($"Cannot find random melee weapon for {assignment.Index} for {_party.Name}"); }
		}
	}

	private void AssignExtraEquipment(EquipmentFilter equipmentFilter, AssignmentFilter assignmentFilter) {
		var equipmentQuery = _equipmentToAssign
							 .WhereQ(equipment => equipment.Key is { IsEmpty: false, Item: not null } &&
												  equipment.Value > 0                                 &&
												  equipmentFilter(equipment))
							 .OrderByDescending(equipment => equipment.Key.Item.Tier)
							 .ThenByDescending(equipment => equipment.Key.Item.Value);

		LinkedList<KeyValuePair<EquipmentElement, int>> equipmentDeque = new(equipmentQuery);

		if (!equipmentDeque.AnyQ()) return;

		foreach (var assignment in Assignments)
			if (assignmentFilter(assignment)) {
				var slot = assignment.EmptyWeaponSlot;
				if (!slot.HasValue) continue;
				var equipmentNode      = equipmentDeque.First;
				var equipment          = equipmentNode.Value;
				var equipmentItem      = equipment.Key;
				var equipmentItemCount = equipment.Value;

				assignment.Equipment.AddEquipmentToSlotWithoutAgent(slot.Value, equipmentItem);
				Global.Debug($"extra equipment {equipmentItem} assigned to {assignment.Character.StringId}#{assignment.Index} on slot {slot.Value} for {_party.Name}");
				equipmentItemCount--;
				if (_equipmentToAssign[equipmentItem] > 0) _equipmentToAssign[equipmentItem]--;

				if (equipmentItemCount > 0)
					equipmentNode.Value = new KeyValuePair<EquipmentElement, int>(equipmentItem, equipmentItemCount);
				else
					equipmentDeque.RemoveFirst();

				if (!equipmentDeque.AnyQ()) return;
			}
	}

	// 使用示例
	private void AssignExtraShield() {
		Global.Log($"AssignExtraShield for {_party.Name}", Colors.Green, Level.Debug);

		AssignExtraEquipment(ShieldFilter, ShieldAssignmentFilter);
		return;

		static bool ShieldFilter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item.ItemType: ItemObject.ItemTypeEnum.Shield } &&
				   equipment.Value > 0;
		}

		static bool ShieldAssignmentFilter(Assignment assignment) {
			return assignment is { CanBeShielded: true, IsShielded: false };
		}
	}

	private void AssignExtraThrownWeapon() {
		Global.Log($"AssignExtraThrownWeapon for {_party.Name}", Colors.Green, Level.Debug);

		AssignExtraEquipment(ThrownFilter, ThrownAssignmentFilter);
		return;

		static bool ThrownFilter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item: { } item } &&
				   Global.IsThrowing(item)                             &&
				   equipment.Value > 0;
		}

		static bool ThrownAssignmentFilter(Assignment assignment) { return !assignment.HaveThrown; }
	}

	private void AssignExtraArrows() {
		Global.Log($"AssignExtraArrows for {_party.Name}", Colors.Green, Level.Debug);

		AssignExtraEquipment(ArrowFilter, ArrowAssignmentFilter);
		return;

		static bool ArrowFilter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item: { } item } && Global.IsArrow(item) && equipment.Value > 0;
		}

		static bool ArrowAssignmentFilter(Assignment assignment) { return assignment.IsArcher; }
	}

	private void AssignExtraBolts() {
		Global.Log($"AssignExtraBolts for {_party.Name}", Colors.Green, Level.Debug);

		AssignExtraEquipment(BoltFilter, BoltAssignmentFilter);
		return;

		static bool BoltFilter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item: { } item } && Global.IsBolt(item) && equipment.Value > 0;
		}

		static bool BoltAssignmentFilter(Assignment assignment) { return assignment.IsCrossBowMan; }
	}

	private void AssignExtraTwoHandedWeaponOrPolearms() {
		Global.Log($"AssignExtraTwoHandedWeaponOrPolearms for {_party.Name}", Colors.Green, Level.Debug);

		AssignExtraEquipment(Filter, AssignmentFilter);
		return;

		static bool Filter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item: { } item }  &&
				   (Global.IsTwoHanded(item) || Global.IsPolearm(item)) &&
				   equipment.Value > 0;
		}

		static bool AssignmentFilter(Assignment assignment) { return !assignment.HaveTwoHandedWeaponOrPolearms; }
	}

	private EquipmentElement? GetOneRandomMeleeWeapon(Assignment assignment) {
		var weapons = _equipmentToAssign.WhereQ(equipment =>
													equipment.Key is { IsEmpty: false, Item: { } item }       &&
													_equipmentToAssign[equipment.Key] > 0                     &&
													Global.IsWeapon(item)                                     &&
													Global.IsSuitableForCharacter(item, assignment.Character) &&
													!Global.IsThrowing(item)                                  &&
													(Global.IsTwoHanded(item) ||
													 Global.IsOneHanded(item) ||
													 Global.IsPolearm(item)) &&
													(!assignment.IsMounted || Global.IsSuitableForMount(item)))
										.ToArrayQ();
		if (!weapons.AnyQ()) return null;

		Global.Log($"(random) weapon {weapons.First().Key.Item.StringId} assigned for {_party.Name}",
				   Colors.Green,
				   Level.Debug);
		_equipmentToAssign[weapons.First().Key]--;
		return weapons.First().Key;
	}

	private void AssignEquipmentType(ItemObject.ItemTypeEnum itemType) {
		var armours = _equipmentToAssign
					  .WhereQ(kv => kv.Value > 0                 &&
									!kv.Key.IsEmpty              &&
									kv.Key.Item          != null &&
									kv.Key.Item.ItemType == itemType)
					  .OrderByDescending(kv => kv.Key.Item.Tier)
					  .ThenByDescending(kv => kv.Key.Item.Value)
					  .ToArrayQ();

		foreach (var assignment in Assignments) {
			if (itemType is ItemObject.ItemTypeEnum.Horse or ItemObject.ItemTypeEnum.HorseHarness &&
				!assignment.IsMounted)
				continue;

			var currentItemIndex = Array.FindIndex(armours, i => i.Value > 0);
			if (currentItemIndex == -1) break; // 没有更多可用装备时退出循环

			var currentItem = armours[currentItemIndex];
			var index       = ItemEnumTypeToEquipmentIndex(itemType);
			if (!index.HasValue) continue;

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

	private void AssignWeaponByWeaponClass(bool strict) {
		Global.Log($"AssignWeaponByWeaponClass strict={strict} for {_party.Name}", Colors.Green, Level.Debug);
		foreach (var assignment in Assignments) {
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
		if (weapon is { IsEmpty: false, Item: not null } || referenceWeapon.IsEmpty || referenceWeapon.Item == null)
			return;

		var availableWeapon = _equipmentToAssign
							  .WhereQ(equipment =>
										  IsWeaponSuitable(equipment.Key, referenceWeapon.Item, assignment, strict))
							  .OrderByDescending(equipment =>
													 (int)equipment.Key.Item.Tier +
													 CalculateWeaponTierBonus(equipment.Key.Item, mounted))
							  .ThenByDescending(equipment => equipment.Key.Item.Value)
							  .Take(1)
							  .ToListQ();

		AssignWeaponIfAvailable(slot, assignment, availableWeapon);
	}

	private void AssignWeaponByItemEnumType(bool strict) {
		Global.Log($"AssignWeaponByItemEnumType strict={strict} for {_party.Name}", Colors.Green, Level.Debug);

		foreach (var assignment in Assignments) {
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
		if (weapon is { IsEmpty: false, Item: not null } || referenceWeapon.IsEmpty || referenceWeapon.Item == null)
			return;

		var availableWeapon = _equipmentToAssign
							  .WhereQ(equipment =>
										  IsWeaponSuitableByType(equipment,
																 referenceWeapon.Item.ItemType,
																 assignment,
																 strict))
							  .OrderByDescending(equipment =>
													 (int)equipment.Key.Item.Tier +
													 CalculateWeaponTierBonus(equipment.Key.Item, mounted))
							  .ThenByDescending(equipment => equipment.Key.Item.Value)
							  .Take(1)
							  .ToListQ();

		AssignWeaponIfAvailable(slot, assignment, availableWeapon);
	}

	private static EquipmentIndex? ItemEnumTypeToEquipmentIndex(ItemObject.ItemTypeEnum itemType) {
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
		if (!availableWeapon.AnyQ()) return;

		Global.Log($"weapon {availableWeapon.First().Key.Item.StringId} assigned to {assignment.Character.StringId}#{assignment.Index} for {_party.Name}",
				   Colors.Green,
				   Level.Debug);
		assignment.Equipment.AddEquipmentToSlotWithoutAgent(slot, availableWeapon.First().Key);
		_equipmentToAssign[availableWeapon.First().Key]--;
	}

	// 计算基于武器属性的Tier加成
	private static int CalculateWeaponTierBonus(ItemObject weapon, bool mounted) {
		if (mounted) return 0; // 如果骑马，则不应用任何加成

		var bonus = 0;
		MBReadOnlyList<WeaponComponentData> weaponFlags = weapon.WeaponComponent.Weapons;
		var weaponFlag = (WeaponFlags)weaponFlags.Aggregate(0u, (current, flag) => current | (uint)flag.WeaponFlags);

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
		partyArmory[item] = partyArmory.TryGetValue(item, out var existingCount) ? existingCount + count : count;

		Global.Log($"Returned {count} of item {item.StringId} to party {_party.Name}.", Colors.Green, Level.Debug);
	}

	private delegate bool EquipmentFilter(KeyValuePair<EquipmentElement, int> equipment);

	private delegate bool AssignmentFilter(Assignment assignment);
}