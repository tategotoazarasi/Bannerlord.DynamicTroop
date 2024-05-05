using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bannerlord.DynamicTroop.Comparers;
using Bannerlord.DynamicTroop.Extensions;
using log4net.Core;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop;

public class PartyEquipmentDistributor {
	private readonly ConcurrentDictionary<EquipmentElement, int> _equipmentToAssign;
	
	private readonly List<HorseAndHarness> _horseAndHarnesses = new();
	
	private readonly ItemRoster? _itemRoster;
	
	private readonly Mission _mission;
	
	private readonly MobileParty _party;
	
	public readonly List<Assignment> Assignments = new();
	
	public PartyEquipmentDistributor(Mission mission, MobileParty party, ItemRoster itemRoster) {
		_mission           = mission;
		_party             = party;
		_itemRoster        = itemRoster;
		_equipmentToAssign = new ConcurrentDictionary<EquipmentElement, int>(new EquipmentElementComparer());
	}
	
	public PartyEquipmentDistributor(Mission mission, MobileParty party, IDictionary<EquipmentElement, int> equipmentToAssign) {
		_mission           = mission;
		_party             = party;
		_itemRoster        = null;
		_equipmentToAssign = new ConcurrentDictionary<EquipmentElement, int>(equipmentToAssign, new EquipmentElementComparer());
	}
	
	public PartyEquipmentDistributor(Mission mission, MobileParty party, Dictionary<ItemObject, int> objectToAssign) {
		_mission           = mission;
		_party             = party;
		_itemRoster        = null;
		_equipmentToAssign = new ConcurrentDictionary<EquipmentElement, int>(new EquipmentElementComparer());
		
		foreach (var kv in objectToAssign) {
			if (kv.Key != null) {
				EquipmentElement element = new(kv.Key);
				
				if (_equipmentToAssign.TryGetValue(element, out var existingCount)) { _equipmentToAssign[element] = existingCount + kv.Value; }
				else { _                                                                                          = _equipmentToAssign.TryAdd(element, kv.Value); }
			}
		}
	}
	
	public void RunAsync() {
		foreach (var troop in _party.MemberRoster.GetTroopRoster()) {
			for (var i = 0; i < troop.Number - troop.WoundedNumber; i++) {
				if (!troop.Character.IsHero) { Assignments.Add(new Assignment(troop.Character)); }
			}
		}
		
		Assignments.Sort((x, y) => y.CompareTo(x));
		if (_itemRoster != null) {
			foreach (var kv in _itemRoster) {
				if (kv is not { IsEmpty: false, EquipmentElement.IsEmpty: false }) { continue; }
				
				// 尝试获取已存在的数量
				if (!_equipmentToAssign.TryGetValue(kv.EquipmentElement, out var existingAmount)) {
					// 如果键不存在，添加新的键值对
					_ = _equipmentToAssign.TryAdd(kv.EquipmentElement, kv.Amount);
				}
				else {
					// 如果键已存在，更新数量
					_equipmentToAssign[kv.EquipmentElement] = existingAmount + kv.Amount;
				}
			}
		}
		
		DoAssignAsync();
	}
	
	private void GenerateHorseAndHarnessList() {
		Dictionary<int, List<(EquipmentElement Key, int Cnt)>> horsesDict  = new();
		Dictionary<int, List<(EquipmentElement Key, int Cnt)>> harnessDict = new();
		List<(EquipmentElement Key, int Cnt)>                  saddles     = new();
		
		PopulateDictionaries(_equipmentToAssign, horsesDict, harnessDict, saddles);
		SortEquipmentDictionaries(horsesDict, harnessDict, saddles);
		GenerateHorseAndHarnessPairs(horsesDict, harnessDict, saddles);
	}
	
	private static void PopulateDictionaries(IEnumerable<KeyValuePair<EquipmentElement, int>>        equipment,
											 IDictionary<int, List<(EquipmentElement Key, int Cnt)>> horsesDict,
											 IDictionary<int, List<(EquipmentElement Key, int Cnt)>> harnessDict,
											 ICollection<(EquipmentElement Key, int Cnt)>            saddles) {
		foreach (var kvp in equipment) {
			switch (kvp.Key.Item) {
				case { HasHorseComponent: true, ItemType: ItemObject.ItemTypeEnum.Horse }:
					AddToDict(horsesDict, kvp.Key, kvp.Value);
					break;
				
				case { HasArmorComponent: true, ItemType: ItemObject.ItemTypeEnum.HorseHarness }:
					AddToDict(harnessDict, kvp.Key, kvp.Value);
					break;
				
				case {
						 HasArmorComponent : false,
						 HasSaddleComponent: true,
						 ItemType          : ItemObject.ItemTypeEnum.HorseHarness
					 }:
					saddles.Add((kvp.Key, kvp.Value));
					break;
			}
		}
	}
	
	private static void AddToDict(IDictionary<int, List<(EquipmentElement Key, int Cnt)>> dict, EquipmentElement element, int cnt) {
		var familyType = element.Item.HorseComponent?.Monster?.FamilyType ?? element.Item.ArmorComponent?.FamilyType ?? -1;
		if (!dict.TryGetValue(familyType, out var list)) {
			list             = new List<(EquipmentElement Key, int Cnt)>();
			dict[familyType] = list;
		}
		
		list.Add((element, cnt));
	}
	
	private static void SortEquipmentDictionaries(Dictionary<int, List<(EquipmentElement Key, int Cnt)>> horsesDict, Dictionary<int, List<(EquipmentElement Key, int Cnt)>> harnessDict, List<(EquipmentElement Key, int Cnt)> saddles) {
		SortDict(horsesDict,  CompareEquipment);
		SortDict(harnessDict, CompareEquipment);
		saddles.Sort(CompareEquipment);
		return;
		
		static int CompareEquipment((EquipmentElement Key, int Cnt) x, (EquipmentElement Key, int Cnt) y) {
			var tierCompare = y.Key.Item.Tier.CompareTo(x.Key.Item.Tier);
			return tierCompare != 0 ? tierCompare : y.Key.Item.Value.CompareTo(x.Key.Item.Value);
		}
	}
	
	private static void SortDict(Dictionary<int, List<(EquipmentElement Key, int Cnt)>> dict, Comparison<(EquipmentElement Key, int Cnt)> comparer) {
		foreach (var key in dict.Keys) { dict[key].Sort(comparer); }
	}
	
	private void GenerateHorseAndHarnessPairs(Dictionary<int, List<(EquipmentElement Key, int Cnt)>> horsesDict, IDictionary<int, List<(EquipmentElement Key, int Cnt)>> harnessDict, IList<(EquipmentElement Key, int Cnt)> saddles) {
		foreach (var kvp in horsesDict) {
			var familyType = kvp.Key;
			var horses     = kvp.Value.ToArrayQ();
			var harnesses  = harnessDict.TryGetValue(familyType, out var harnessesList) ? harnessesList.ToArrayQ() : Array.Empty<(EquipmentElement Key, int Cnt)>();
			
			int horseIndex = 0, harnessIndex = 0;
			while (horseIndex < horses.Length) {
				(var horseItem, var horseCnt) = horses[horseIndex];
				var harnessItem = harnessIndex < harnesses.Length ? harnesses[harnessIndex].Key : new EquipmentElement(null);
				var harnessCnt  = harnessIndex < harnesses.Length ? harnesses[harnessIndex].Cnt : 0;
				
				if (horseCnt > 0) {
					HorseAndHarness hah = new(horseItem, harnessItem);
					_horseAndHarnesses.Add(hah);
					horses[horseIndex] = (horseItem, --horseCnt);
					
					if (harnessCnt > 0) { harnesses[harnessIndex] = (harnessItem, --harnessCnt); }
				}
				
				if (horseCnt == 0) { horseIndex++; }
				
				if (harnessCnt == 0 && harnessIndex < harnesses.Length) { harnessIndex++; }
			}
		}
		
		AssignSaddlesToHorseAndHarnesses(_horseAndHarnesses, saddles);
		_horseAndHarnesses.Sort((x, y) => y.CompareTo(x));
	}
	
	private static void AssignSaddlesToHorseAndHarnesses(IEnumerable<HorseAndHarness> horseAndHarnesses, IList<(EquipmentElement Key, int Cnt)> saddles) {
		var saddleIndex = 0;
		foreach (var hoh in horseAndHarnesses.WhereQ(h => h.Harness == null)) {
			if (saddleIndex >= saddles.Count) { break; }
			
			(var saddleItem, var saddleCnt) = saddles[saddleIndex];
			if (saddleCnt <= 0) { continue; }
			
			hoh.Harness          = saddleItem;
			saddles[saddleIndex] = (saddleItem, --saddleCnt);
			if (saddleCnt == 0) { saddleIndex++; }
		}
	}
	
	private void DoAssignAsync() {
		var tasks = new List<Task> {
									   Task.Run(() => {
													Global.Log($"Assigning HeadArmor for {_party.Name}", Colors.Green, Level.Debug);
													AssignEquipmentType(ItemObject.ItemTypeEnum.HeadArmor);
												}),
									   Task.Run(() => {
													Global.Log($"Assigning HandArmor for {_party.Name}", Colors.Green, Level.Debug);
													AssignEquipmentType(ItemObject.ItemTypeEnum.HandArmor);
												}),
									   Task.Run(() => {
													Global.Log($"Assigning BodyArmor for {_party.Name}", Colors.Green, Level.Debug);
													AssignEquipmentType(ItemObject.ItemTypeEnum.BodyArmor);
												}),
									   Task.Run(() => {
													Global.Log($"Assigning LegArmor for {_party.Name}", Colors.Green, Level.Debug);
													AssignEquipmentType(ItemObject.ItemTypeEnum.LegArmor);
												}),
									   Task.Run(() => {
													Global.Log($"Assigning Cape for {_party.Name}", Colors.Green, Level.Debug);
													AssignEquipmentType(ItemObject.ItemTypeEnum.Cape);
												}),
									   Task.Run(() => {
													if (!_mission.IsSiegeBattle && !_mission.HasMissionBehavior<HideoutMissionController>()) {
														GenerateHorseAndHarnessList();
														AssignHorseAndHarness();
													}
												}),
									   Task.Run(() => {
													AssignWeaponByWeaponClass(true);
													AssignWeaponByWeaponClass(false);
													AssignWeaponByItemEnumType(true);
													AssignWeaponByItemEnumType(false);
												})
								   };
		Task.WhenAll(tasks).GetAwaiter().GetResult();
		Global.Debug($"Async distribution for {_party.Name} finished");
		AssignWeaponToUnarmed();
		if (ModSettings.Instance?.AssignExtraEquipments ?? true) {
			AssignExtraArrows();
			AssignExtraBolts();
			AssignExtraShield();
			AssignExtraThrownWeapon();
			AssignExtraTwoHandedWeaponOrPolearms();
		}
	}
	
	private void AssignHorseAndHarness() {
		Global.Debug($"Assigning Horse and Harness for {_party.Name}");
		var currentIndex = 0;
		foreach (var assignment in Assignments) {
			if (!assignment.IsMounted) { continue; }
			
			if (currentIndex >= _horseAndHarnesses.Count) { break; }
			
			var horseAndHarness = _horseAndHarnesses[currentIndex++];
			assignment.SetEquipment(EquipmentIndex.Horse, horseAndHarness.Horse);
			Global.Debug($"assign horse {horseAndHarness.Horse.Item.Name} to {assignment.Character.Name}#{assignment.Index} for {_party.Name}");
			if (!horseAndHarness.Harness.HasValue) { continue; }
			
			assignment.SetEquipment(EquipmentIndex.HorseHarness, horseAndHarness.Harness.Value);
			Global.Debug($"assign horse harness {horseAndHarness.Harness.Value.Item.Name} to {assignment.Character.Name}#{assignment.Index} for {_party.Name}");
		}
	}
	
	private void AssignWeaponToUnarmed() {
		Global.Log($"AssignWeaponToUnarmed for {_party.Name}", Colors.Green, Level.Debug);
		var unarmedAssignments = Assignments.WhereQ(assignment => assignment.IsUnarmed).ToListQ();
		foreach (var assignment in unarmedAssignments) {
			Global.Warn($"Found unarmed unit Index {assignment.Index} for {_party.Name}");
			var weapon = GetOneRandomMeleeWeapon(assignment);
			if (weapon.HasValue) {
				assignment.SetEquipment(EquipmentIndex.Weapon0, weapon.Value);
				_equipmentToAssign[weapon.Value]--;
			}
			else { Global.Warn($"Cannot find random melee weapon for {assignment.Index} for {_party.Name}"); }
		}
	}
	
	/// <summary>
	///     根据给定的装备筛选条件和分配筛选条件为角色分配额外装备。
	/// </summary>
	/// <param name="equipmentFilter">  装备筛选函数，用于决定哪些装备可以被分配。 </param>
	/// <param name="assignmentFilter"> 分配筛选函数，用于决定哪些角色可以接收装备。 </param>
	private void AssignExtraEquipment(EquipmentFilter equipmentFilter, AssignmentFilter assignmentFilter) {
		var equipmentQuery = _equipmentToAssign
							 .AsParallel()
							 .WhereQ(equipment => equipment.Key is { IsEmpty: false, Item: not null } && equipment.Value > 0 && equipmentFilter(equipment))
							 .OrderByDescending(equipment => equipment.Key.Item.Tier)
							 .ThenByDescending(equipment => equipment.Key.Item.Value);
		
		LinkedList<KeyValuePair<EquipmentElement, int>> equipmentDeque = new(equipmentQuery);
		
		if (!equipmentDeque.AnyQ()) { return; }
		
		foreach (var assignment in Assignments) {
			if (assignmentFilter(assignment)) {
				var slot = assignment.EmptyWeaponSlot;
				if (!slot.HasValue) { continue; }
				
				var equipmentNode      = equipmentDeque.First;
				var equipment          = equipmentNode.Value;
				var equipmentItem      = equipment.Key;
				var equipmentItemCount = equipment.Value;
				
				assignment.SetEquipment(slot.Value, equipmentItem);
				Global.Debug($"extra equipment {equipmentItem} assigned to {assignment.Character.StringId}#{assignment.Index} on slot {slot.Value} for {_party.Name}");
				equipmentItemCount--;
				if (_equipmentToAssign[equipmentItem] > 0) { _equipmentToAssign[equipmentItem]--; }
				
				if (equipmentItemCount > 0) { equipmentNode.Value = new KeyValuePair<EquipmentElement, int>(equipmentItem, equipmentItemCount); }
				else { equipmentDeque.RemoveFirst(); }
				
				if (!equipmentDeque.AnyQ()) { return; }
			}
		}
	}
	
	// 使用示例
	private void AssignExtraShield() {
		Global.Log($"AssignExtraShield for {_party.Name}", Colors.Green, Level.Debug);
		
		AssignExtraEquipment(ShieldFilter, ShieldAssignmentFilter);
		return;
		
		static bool ShieldFilter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item.ItemType: ItemObject.ItemTypeEnum.Shield } && equipment.Value > 0;
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
			return equipment.Key is { IsEmpty: false, Item: { } item } && item.IsThrowing() && equipment.Value > 0;
		}
		
		static bool ThrownAssignmentFilter(Assignment assignment) {
			return !assignment.HaveThrown;
		}
	}
	
	private void AssignExtraArrows() {
		Global.Log($"AssignExtraArrows for {_party.Name}", Colors.Green, Level.Debug);
		
		AssignExtraEquipment(ArrowFilter, ArrowAssignmentFilter);
		return;
		
		static bool ArrowFilter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item: { } item } && item.IsArrow() && equipment.Value > 0;
		}
		
		static bool ArrowAssignmentFilter(Assignment assignment) {
			return assignment.IsArcher;
		}
	}
	
	private void AssignExtraBolts() {
		Global.Log($"AssignExtraBolts for {_party.Name}", Colors.Green, Level.Debug);
		
		AssignExtraEquipment(BoltFilter, BoltAssignmentFilter);
		return;
		
		static bool BoltFilter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item: { } item } && item.IsBolt() && equipment.Value > 0;
		}
		
		static bool BoltAssignmentFilter(Assignment assignment) {
			return assignment.IsCrossBowMan;
		}
	}
	
	private void AssignExtraTwoHandedWeaponOrPolearms() {
		Global.Log($"AssignExtraTwoHandedWeaponOrPolearms for {_party.Name}", Colors.Green, Level.Debug);
		
		AssignExtraEquipment(Filter, AssignmentFilter);
		return;
		
		static bool Filter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item: { } item } && (item.IsTwoHanded() || item.IsPolearm()) && equipment.Value > 0;
		}
		
		static bool AssignmentFilter(Assignment assignment) {
			return !assignment.HaveTwoHandedWeaponOrPolearms;
		}
	}
	
	private EquipmentElement? GetOneRandomMeleeWeapon(Assignment assignment) {
		var weapons = _equipmentToAssign
					  .WhereQ(equipment => equipment.Key is { IsEmpty: false, Item: { } item }               &&
										   _equipmentToAssign[equipment.Key] > 0                             &&
										   item.HasWeaponComponent                                           &&
										   item.IsSuitableForCharacter(assignment.Character)                 &&
										   !item.IsThrowing()                                                &&
										   (item.IsTwoHanded()    || item.IsOneHanded() || item.IsPolearm()) &&
										   (!assignment.IsMounted || item.IsSuitableForMount()))
					  .ToArrayQ();
		if (!weapons.AnyQ()) { return null; }
		
		Global.Log($"(random) weapon {weapons.First().Key.Item.StringId} assigned for {_party.Name}", Colors.Green, Level.Debug);
		_equipmentToAssign[weapons.First().Key]--;
		return weapons.First().Key;
	}
	
	private void AssignEquipmentType(ItemObject.ItemTypeEnum itemType) {
		var armours = _equipmentToAssign.AsParallel().WhereQ(kv => kv.Value > 0 && !kv.Key.IsEmpty && kv.Key.Item != null && kv.Key.Item.ItemType == itemType).ToArrayQ();
		Array.Sort(armours, (x, y) => y.Key.Item.CompareArmor(x.Key.Item));
		
		foreach (var assignment in Assignments) {
			if (itemType is ItemObject.ItemTypeEnum.Horse or ItemObject.ItemTypeEnum.HorseHarness && !assignment.IsMounted) { continue; }
			
			var currentItemIndex = Array.FindIndex(armours, i => i.Value > 0);
			if (currentItemIndex == -1) {
				break; // 没有更多可用装备时退出循环
			}
			
			var currentItem = armours[currentItemIndex];
			var index       = Helper.ItemEnumTypeToEquipmentIndex(itemType);
			if (!index.HasValue) { continue; }
			
			assignment.SetEquipment(index.Value, currentItem.Key);
			Global.Log($"assign equipment {currentItem.Key.Item.StringId} type {itemType} to {assignment.Character.StringId}#{assignment.Index} on slot {index.Value} for {_party.Name}", Colors.Green, Level.Debug);
			
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
	
	/// <summary>
	///     根据武器大类型和指定的装备槽位为角色分配武器。
	/// </summary>
	/// <param name="slot">       装备槽位，表示需要分配武器的具体位置。 </param>
	/// <param name="assignment"> 包含角色和参考装备信息的分配对象。 </param>
	/// <param name="mounted">    指示角色是否骑乘状态，影响武器选择。 </param>
	/// <param name="strict">     是否启用严格模式。 </param>
	private void AssignWeaponByWeaponClassBySlot(EquipmentIndex slot, Assignment assignment, bool mounted, bool strict) {
		Global.Log($"AssignWeaponByWeaponClassBySlot slot={slot} character={assignment.Character.StringId}#{assignment.Index} mounted={mounted} strict={strict} for {_party.Name}", Colors.Green, Level.Debug);
		var referenceWeapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
		var weapon          = assignment.GetEquipmentFromSlot(slot);
		if (weapon is { IsEmpty: false, Item: not null } || referenceWeapon.IsEmpty || referenceWeapon.Item == null) { return; }
		
		var availableWeapon = _equipmentToAssign
							  .AsParallel()
							  .WhereQ(equipment => IsWeaponSuitable(equipment.Key, referenceWeapon.Item, assignment, strict))
							  .OrderByDescending(equipment => (int)equipment.Key.Item.Tier + equipment.Key.Item.CalculateWeaponTierBonus(mounted))
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
	
	private void AssignWeaponByItemEnumTypeBySlot(EquipmentIndex slot, Assignment assignment, bool mounted, bool strict) {
		Global.Log($"AssignWeaponByItemEnumTypeBySlot slot={slot} character={assignment.Character.StringId}#{assignment.Index} mounted={mounted} strict={strict} for {_party.Name}", Colors.Green, Level.Debug);
		var referenceWeapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
		var weapon          = assignment.GetEquipmentFromSlot(slot);
		if (weapon is { IsEmpty: false, Item: not null } || referenceWeapon.IsEmpty || referenceWeapon.Item == null) { return; }
		
		var availableWeapon = _equipmentToAssign
							  .AsParallel()
							  .WhereQ(equipment => IsWeaponSuitableByType(equipment, referenceWeapon.Item.ItemType, assignment, strict))
							  .OrderByDescending(equipment => (int)equipment.Key.Item.Tier + equipment.Key.Item.CalculateWeaponTierBonus(mounted))
							  .ThenByDescending(equipment => equipment.Key.Item.Value)
							  .Take(1)
							  .ToListQ();
		
		AssignWeaponIfAvailable(slot, assignment, availableWeapon);
	}
	
	// 封装判断逻辑
	private bool IsWeaponSuitable(EquipmentElement equipment, ItemObject referenceWeapon, Assignment assignment, bool strict) {
		if (equipment.IsEmpty                                            ||
			equipment.Item == null                                       ||
			!equipment.Item.HasWeaponComponent                           ||
			_equipmentToAssign[equipment] <= 0                           ||
			!equipment.Item.IsSuitableForCharacter(assignment.Character) ||
			!Global.HaveSameWeaponClass(Global.GetWeaponClass(equipment.Item), Global.GetWeaponClass(referenceWeapon))) { return false; }
		
		var isSuitableForMount = equipment.Item.IsSuitableForMount();
		var isCouchable        = equipment.Item.IsCouchable();
		
		if (strict) {
			if (!Global.FullySameWeaponClass(equipment.Item, referenceWeapon)) { return false; }
			
			if (assignment.IsMounted) {
				// 严格模式下骑马：必须适合骑乘；如果是长杆武器，则必须可进行骑枪冲刺
				return isSuitableForMount && (!equipment.Item.IsPolearm() || isCouchable);
			}
			
			// 严格模式下非骑马：不可选择可进行骑枪冲刺的武器
			return !isCouchable;
		}
		
		// 非严格模式下：骑马的不能选择不适合骑乘的武器，非骑马的可以选择任意武器
		return !assignment.IsMounted || isSuitableForMount;
	}
	
	// 封装判断逻辑
	private bool IsWeaponSuitableByType(KeyValuePair<EquipmentElement, int> equipment, ItemObject.ItemTypeEnum itemType, Assignment assignment, bool strict) {
		if (equipment.Key.IsEmpty || equipment.Key.Item == null || !equipment.Key.Item.HasWeaponComponent || _equipmentToAssign[equipment.Key] <= 0 || !equipment.Key.Item.IsSuitableForCharacter(assignment.Character) || equipment.Key.Item.ItemType != itemType) {
			return false;
		}
		
		var isSuitableForMount = equipment.Key.Item.IsSuitableForMount();
		var isCouchable        = equipment.Key.Item.IsCouchable();
		
		if (strict) {
			if (assignment.IsMounted) {
				// 严格模式下骑马：必须适合骑乘；如果是长杆武器，则必须可进行骑枪冲刺
				return isSuitableForMount && (!equipment.Key.Item.IsPolearm() || isCouchable);
			}
			
			// 严格模式下非骑马：不可选择可进行骑枪冲刺的武器
			return !isCouchable;
		}
		
		// 非严格模式下：骑马的不能选择不适合骑乘的武器，非骑马的可以选择任意武器
		return !assignment.IsMounted || isSuitableForMount;
	}
	
	// 封装武器分配逻辑
	private void AssignWeaponIfAvailable(EquipmentIndex slot, Assignment assignment, List<KeyValuePair<EquipmentElement, int>> availableWeapon) {
		if (!availableWeapon.AnyQ()) { return; }
		
		Global.Log($"weapon {availableWeapon.First().Key.Item.StringId} assigned to {assignment.Character.StringId}#{assignment.Index} for {_party.Name}", Colors.Green, Level.Debug);
		assignment.SetEquipment(slot, availableWeapon.First().Key);
		_equipmentToAssign[availableWeapon.First().Key]--;
	}
	
	public void Spawn(Equipment equipment) {
		// 确保 PartyArmories 包含特定的 _party.Id
		if (!EveryoneCampaignBehavior.PartyArmories.TryGetValue(_party.Id, out var partyArmory)) {
			partyArmory                                       = new Dictionary<ItemObject, int>();
			EveryoneCampaignBehavior.PartyArmories[_party.Id] = partyArmory;
		}
		
		foreach (var slot in Global.EquipmentSlots) {
			var element = equipment.GetEquipmentFromSlot(slot);
			if (element is not { IsEmpty: false, Item: not null }) { continue; }
			
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