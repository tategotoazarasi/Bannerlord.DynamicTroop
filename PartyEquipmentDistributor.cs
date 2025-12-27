using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DynamicTroopEquipmentReupload.Comparers;
using DynamicTroopEquipmentReupload.Extensions;
using log4net.Core;
using SandBox.Missions.MissionLogics.Hideout;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using static TaleWorlds.Core.ItemObject;

namespace DynamicTroopEquipmentReupload;

public class PartyEquipmentDistributor {
	private const int MAX_TIER_ABOVE_DEFAULT = 2;

	private const float UNDER_EQUIPPED_MORALE_PENALTY_PER_MISSING_TIER = 2f;

	private const float UNDER_EQUIPPED_MORALE_PENALTY_CAP = 20f;
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
				if (troop.Character is CharacterObject characterObject && !characterObject.IsHero) { Assignments.Add(new Assignment(characterObject)); }
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

	private void ConsumeEquipmentToAssign(EquipmentElement equipmentElement) {
		_equipmentToAssign.AddOrUpdate(
			equipmentElement,
			0,
			(_, currentCount) => currentCount > 0 ? currentCount - 1 : 0);
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
				case { HasHorseComponent: true, ItemType: ItemTypeEnum.Horse }:
					AddToDict(horsesDict, kvp.Key, kvp.Value);
					break;

				case { HasArmorComponent: true, ItemType: ItemTypeEnum.HorseHarness }:
					AddToDict(harnessDict, kvp.Key, kvp.Value);
					break;

				case {
						 HasArmorComponent : false,
						 HasSaddleComponent: true,
						 ItemType          : ItemTypeEnum.HorseHarness
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
		var settings                          = ModSettings.Instance;
		var preferDefaultEquipmentThenClosest = settings?.PreferDefaultEquipmentThenClosest ?? true;

		GenerateHorseAndHarnessList();

		AssignEquipmentType(ItemTypeEnum.HeadArmor);
		AssignEquipmentType(ItemTypeEnum.BodyArmor);
		AssignEquipmentType(ItemTypeEnum.LegArmor);
		AssignEquipmentType(ItemTypeEnum.HandArmor);
		AssignEquipmentType(ItemTypeEnum.Cape);

		AssignHorseAndHarness();

		// strict
		AssignWeaponByWeaponClass(true);

		// non strict
		AssignWeaponByWeaponClass(false);

		AssignWeaponByItemEnumType(false);

		// random
		AssignWeaponToUnarmed();

		// Extra equipment
		if (settings?.AssignExtraEquipments ?? true) {
			AssignExtraShield();
			AssignExtraThrownWeapon();
			AssignExtraArrows();
			AssignExtraBolts();
			AssignExtraTwoHandedWeaponOrPolearms();
		}

		// Emergency loadout
		if (settings?.EnableEmergencyLoadout ?? true) { ApplyEmergencyLoadout(); }

		// Underequipped
		MarkUnderEquippedAssignmentsIfEnabled();
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
				ConsumeEquipmentToAssign(weapon.Value);
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
		var candidates = new List<KeyValuePair<EquipmentElement, int>>();

		foreach (var kv in _equipmentToAssign) {
			if (kv.Key is { IsEmpty: false, Item: not null } &&
				kv.Value > 0                                 &&
				equipmentFilter(kv)) { candidates.Add(kv); }
		}

		if (candidates.Count == 0)
			return;

		candidates.Sort(static (a, b) => {
			var tierCompare = b.Key.Item.Tier.CompareTo(a.Key.Item.Tier);
			return tierCompare != 0 ? tierCompare : b.Key.Item.Value.CompareTo(a.Key.Item.Value);
		});

		var currentIndex = 0;

		foreach (var assignment in Assignments) {
			if (!assignmentFilter(assignment))
				continue;

			var slot = assignment.EmptyWeaponSlot;
			if (!slot.HasValue)
				continue;

			while (currentIndex < candidates.Count && candidates[currentIndex].Value <= 0)
				currentIndex++;

			if (currentIndex >= candidates.Count)
				return;

			var current = candidates[currentIndex];

			assignment.SetEquipment(slot.Value, current.Key);
			Global.Debug($"extra equipment {current.Key} assigned to {assignment.Character.StringId}#{assignment.Index} on slot {slot.Value} for {_party.Name}");

			candidates[currentIndex] = new KeyValuePair<EquipmentElement, int>(current.Key, current.Value - 1);
			ConsumeEquipmentToAssign(current.Key);
		}
	}

	private void AssignExtraShield() {
		Global.Log($"AssignExtraShield for {_party.Name}", Colors.Green, Level.Debug);

		AssignExtraEquipment(ShieldFilter, ShieldAssignmentFilter);
		return;

		static bool ShieldFilter(KeyValuePair<EquipmentElement, int> equipment) {
			return equipment.Key is { IsEmpty: false, Item.ItemType: ItemTypeEnum.Shield } && equipment.Value > 0;
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
		var candidates = new List<EquipmentElement>();

		foreach (var kv in _equipmentToAssign) {
			var element = kv.Key;
			if (kv.Value <= 0 || element.IsEmpty || element.Item == null)
				continue;

			var item = element.Item;
			if (!item.HasWeaponComponent)
				continue;

			if (!item.IsSuitableForCharacter(assignment.Character))
				continue;

			if (item.IsThrowing())
				continue;

			if (!item.IsTwoHanded() && !item.IsOneHanded() && !item.IsPolearm())
				continue;

			var hasAssignedHorse =
				!assignment.GetEquipmentFromSlot(EquipmentIndex.Horse).IsEmpty &&
				assignment.GetEquipmentFromSlot(EquipmentIndex.Horse).Item != null;

			if (hasAssignedHorse && !item.IsSuitableForMount())
				continue;

			candidates.Add(element);
		}

		if (candidates.Count == 0)
			return null;

		var randomIndex = MBRandom.RandomInt(candidates.Count);
		var selected    = candidates[randomIndex];

		Global.Log($"(random) weapon {selected.Item.StringId} selected for {_party.Name}", Colors.Green, Level.Debug);
		return selected;
	}

	private void AssignEquipmentType(ItemTypeEnum type) {
		var armours = _equipmentToAssign
					  .Where(kv => kv.Key is { IsEmpty: false, Item: not null } &&
								   kv.Value             > 0                     &&
								   kv.Key.Item.ItemType == type)
					  .Select(kv => new KeyValuePair<EquipmentElement, int>(kv.Key, kv.Value))
					  .ToArray();

		Array.Sort(armours,
				   static (a, b) => {
					   var tierCompare = b.Key.Item.Tier.CompareTo(a.Key.Item.Tier);
					   return tierCompare != 0 ? tierCompare : b.Key.Item.Value.CompareTo(a.Key.Item.Value);
				   });

		var settings                          = ModSettings.Instance;
		var preferDefaultEquipmentThenClosest = settings?.PreferDefaultEquipmentThenClosest ?? true;

		var slotIndex = Global.GetEquipmentIndexByItemType(type);
		if (slotIndex == null) { return; }

		for (var assignmentIndex = 0; assignmentIndex < Assignments.Count; assignmentIndex++) {
			var assignment       = Assignments[assignmentIndex];
			var referenceElement = assignment.ReferenceEquipment.GetEquipmentFromSlot(slotIndex.Value);
			var referenceItem    = referenceElement.Item;

			// Loyal Equipments: first try exact vanilla equipment.
			if (preferDefaultEquipmentThenClosest && referenceItem != null) {
				for (var i = 0; i < armours.Length; i++) {
					var candidate = armours[i];
					if (candidate.Value <= 0) { continue; }

					if (candidate.Key.Item != referenceItem) { continue; }

					assignment.SetEquipment(slotIndex.Value, candidate.Key);
					armours[i] = new KeyValuePair<EquipmentElement, int>(candidate.Key, candidate.Value - 1);
					ConsumeEquipmentToAssign(candidate.Key);
					break;
				}
			}

			if (!assignment.Equipment.GetEquipmentFromSlot(slotIndex.Value).IsEmpty) { continue; }

			var maxAllowedTier = GetMaxAllowedTier(referenceItem);

			// Loyal Equipments: pick the closest-by-tier item (still capped by +2 tier).
			if (preferDefaultEquipmentThenClosest && referenceItem != null) {
				var referenceTier = (int)referenceItem.Tier;

				var bestIndex    = -1;
				var bestTierDiff = int.MaxValue;
				var bestTier     = int.MinValue;
				var bestValue    = int.MinValue;

				for (var i = 0; i < armours.Length; i++) {
					var candidate = armours[i];
					if (candidate.Value <= 0) { continue; }

					var candidateItem = candidate.Key.Item;
					if (candidateItem == null) { continue; }

					var candidateTier = (int)candidateItem.Tier;
					if (candidateTier > maxAllowedTier) { continue; }

					var tierDiff = Math.Abs(candidateTier - referenceTier);

					if (tierDiff < bestTierDiff || tierDiff == bestTierDiff && candidateTier > bestTier || tierDiff == bestTierDiff && candidateTier == bestTier && candidateItem.Value > bestValue) {
						bestIndex    = i;
						bestTierDiff = tierDiff;
						bestTier     = candidateTier;
						bestValue    = candidateItem.Value;
					}
				}

				if (bestIndex >= 0) {
					var chosen = armours[bestIndex];
					assignment.SetEquipment(slotIndex.Value, chosen.Key);
					armours[bestIndex] = new KeyValuePair<EquipmentElement, int>(chosen.Key, chosen.Value - 1);
					ConsumeEquipmentToAssign(chosen.Key);
				}

				continue;
			}

			// Loyal OFF: pick the best available (already sorted best,worst), still capped by +2 tier.
			for (var i = 0; i < armours.Length; i++) {
				var candidate = armours[i];
				if (candidate.Value <= 0) { continue; }

				var candidateItem = candidate.Key.Item;
				if (candidateItem == null) { continue; }

				if ((int)candidateItem.Tier > maxAllowedTier) { continue; }

				assignment.SetEquipment(slotIndex.Value, candidate.Key);
				armours[i] = new KeyValuePair<EquipmentElement, int>(candidate.Key, candidate.Value - 1);
				ConsumeEquipmentToAssign(candidate.Key);
				break;
			}
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

		var settings                 = ModSettings.Instance;
		var preferVanillaThenClosest = settings?.PreferDefaultEquipmentThenClosest ?? true;

		var referenceWeapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
		var currentWeapon   = assignment.GetEquipmentFromSlot(slot);

		if (currentWeapon is { IsEmpty: false, Item: not null } || referenceWeapon.IsEmpty || referenceWeapon.Item == null)
			return;

		var maxAllowedTier = GetMaxAllowedTier(referenceWeapon.Item);

		// Loyal Equipments ON: exact vanilla weapon first (if available)
		if (preferVanillaThenClosest                         &&
			(int)referenceWeapon.Item.Tier <= maxAllowedTier &&
			TryConsumeExactItem(referenceWeapon.Item, out var exactElement)) {
			assignment.SetEquipment(slot, exactElement);
			Global.Log($"weapon (vanilla exact) {exactElement.Item.StringId} assigned to {assignment.Character.StringId}#{assignment.Index} for {_party.Name}", Colors.Green, Level.Debug);
			return;
		}

		EquipmentElement? bestWeapon = null;

		var referenceTier = (int)referenceWeapon.Item.Tier;
		var bestTierDiff  = int.MaxValue;
		var bestTier      = int.MinValue;
		var bestValue     = int.MinValue;

		foreach (var kv in _equipmentToAssign) {
			if (kv.Value <= 0)
				continue;

			var element = kv.Key;
			if (element.IsEmpty || element.Item == null)
				continue;

			var item = element.Item;

			if ((int)item.Tier > maxAllowedTier)
				continue;

			if (!IsWeaponSuitable(element, referenceWeapon.Item, assignment, strict))
				continue;

			var itemTier = (int)item.Tier;
			var tierDiff = Math.Abs(itemTier - referenceTier);

			if (tierDiff < bestTierDiff || tierDiff == bestTierDiff && itemTier > bestTier || tierDiff == bestTierDiff && itemTier == bestTier && item.Value > bestValue) {
				bestTierDiff = tierDiff;
				bestTier     = itemTier;
				bestValue    = item.Value;
				bestWeapon   = element;
			}
		}


		AssignWeaponIfAvailable(slot, assignment, bestWeapon);
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

		var settings                 = ModSettings.Instance;
		var preferVanillaThenClosest = settings?.PreferDefaultEquipmentThenClosest ?? true;

		var referenceWeapon = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
		var currentWeapon   = assignment.GetEquipmentFromSlot(slot);

		if (currentWeapon is { IsEmpty: false, Item: not null } || referenceWeapon.IsEmpty || referenceWeapon.Item == null)
			return;

		var maxAllowedTier = GetMaxAllowedTier(referenceWeapon.Item);

		// Loyal Equipments ON: exact vanilla weapon first (if available)
		if (preferVanillaThenClosest                         &&
			(int)referenceWeapon.Item.Tier <= maxAllowedTier &&
			TryConsumeExactItem(referenceWeapon.Item, out var exactElement)) {
			assignment.SetEquipment(slot, exactElement);
			Global.Log($"weapon (vanilla exact) {exactElement.Item.StringId} assigned to {assignment.Character.StringId}#{assignment.Index} for {_party.Name}", Colors.Green, Level.Debug);
			return;
		}

		EquipmentElement? bestWeapon = null;

		var referenceTier = (int)referenceWeapon.Item.Tier;
		var bestTierDiff  = int.MaxValue;
		var bestTier      = int.MinValue;
		var bestValue     = int.MinValue;

		foreach (var kv in _equipmentToAssign) {
			if (kv.Value <= 0)
				continue;

			var element = kv.Key;
			if (element.IsEmpty || element.Item == null)
				continue;

			var item = element.Item;

			if ((int)item.Tier > maxAllowedTier)
				continue;

			if (!IsWeaponSuitable(element, referenceWeapon.Item, assignment, strict))
				continue;

			var itemTier = (int)item.Tier;
			var tierDiff = Math.Abs(itemTier - referenceTier);

			if (tierDiff < bestTierDiff || tierDiff == bestTierDiff && itemTier > bestTier || tierDiff == bestTierDiff && itemTier == bestTier && item.Value > bestValue) {
				bestTierDiff = tierDiff;
				bestTier     = itemTier;
				bestValue    = item.Value;
				bestWeapon   = element;
			}
		}


		AssignWeaponIfAvailable(slot, assignment, bestWeapon);
	}

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

	private void AssignWeaponIfAvailable(EquipmentIndex slot, Assignment assignment, EquipmentElement? availableWeapon) {
		if (!availableWeapon.HasValue || availableWeapon.Value.IsEmpty || availableWeapon.Value.Item == null)
			return;

		Global.Log($"weapon {availableWeapon.Value.Item.StringId} assigned to {assignment.Character.StringId}#{assignment.Index} for {_party.Name}", Colors.Green, Level.Debug);

		assignment.SetEquipment(slot, availableWeapon.Value);
		ConsumeEquipmentToAssign(availableWeapon.Value);
	}


	public void Spawn(Equipment equipment) {
		// 确保 PartyArmories 包含特定的 _party.Id
		if (!EveryoneCampaignBehavior.PartyArmories.TryGetValue(_party.Id, out var partyArmory)) {
			partyArmory                                       = new Dictionary<ItemObject, int>();
			EveryoneCampaignBehavior.PartyArmories[_party.Id] = partyArmory;
		}

		foreach (var slot in Global.EquipmentSlots) {
			if (_mission.HasMissionBehavior<HideoutMissionController>() && (slot == EquipmentIndex.Horse || slot == EquipmentIndex.HorseHarness)) continue;
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

	private int GetMaxAllowedTier(ItemObject? referenceItem) {
		if (referenceItem == null) {
			return 6; // max
		}

		return Math.Min(6, (int)referenceItem.Tier + MAX_TIER_ABOVE_DEFAULT);
	}


	private bool TryConsumeExactItem(ItemObject item, out EquipmentElement exactElement) {
		var stripped = new EquipmentElement(item);

		if (_equipmentToAssign.TryGetValue(stripped, out var cnt) && cnt > 0) {
			ConsumeEquipmentToAssign(stripped);
			exactElement = stripped;
			return true;
		}

		exactElement = default;
		return false;
	}

	private void ApplyEmergencyLoadout() {
		if (_mission.HasMissionBehavior<HideoutMissionController>())
			return;

		foreach (var assignment in Assignments) {
			// Armour slots
			foreach (var slot in Global.ArmourSlots) {
				if (assignment.GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: not null })
					continue;

				var reference   = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
				var desiredType = reference.Item?.ItemType ?? Helper.EquipmentIndexToItemEnumType(slot);

				if (!desiredType.HasValue)
					continue;

				var emergencyCulture = _party.LeaderHero?.Culture ?? assignment.Character.Culture;
				var basicTroop       = emergencyCulture?.BasicTroop;
				if (basicTroop == null)
					continue;

				var fallbackItem = basicTroop.RandomBattleEquipment.GetEquipmentFromSlot(slot).Item;
				if (fallbackItem == null)
					continue;

				if (!ItemBlackList.Test(fallbackItem) || fallbackItem.IsCraftedByPlayer || !fallbackItem.IsSuitableForCharacter(assignment.Character))
					continue;

				assignment.SetEquipment(slot, new EquipmentElement(fallbackItem));
				assignment.MarkSlotAsTemporary(slot, fallbackItem);
			}

			// Weapon slots (only if empty)
			foreach (var slot in Assignment.WeaponSlots) {
				if (assignment.GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: not null })
					continue;

				var reference   = assignment.ReferenceEquipment.GetEquipmentFromSlot(slot);
				var desiredType = reference.Item?.ItemType;

				if (desiredType == null)
					continue;

				var emergencyCulture = _party.LeaderHero?.Culture ?? assignment.Character.Culture;
				var basicTroop       = emergencyCulture?.BasicTroop;
				if (basicTroop == null)
					continue;

				var fallbackItem = basicTroop.RandomBattleEquipment.GetEquipmentFromSlot(slot).Item;
				if (fallbackItem == null || !fallbackItem.HasWeaponComponent)
					continue;

				if (!ItemBlackList.Test(fallbackItem) || fallbackItem.IsCraftedByPlayer || !fallbackItem.IsSuitableForCharacter(assignment.Character))
					continue;

				assignment.SetEquipment(slot, new EquipmentElement(fallbackItem));
				assignment.MarkSlotAsTemporary(slot, fallbackItem);
			}
		}
	}

	private void MarkUnderEquippedAssignmentsIfEnabled() {
		var settings = ModSettings.Instance;
		if (!(settings?.Underequipped ?? true)) {
			for (var i = 0; i < Assignments.Count; i++) {
				var assignment = Assignments[i];
				assignment.IsUnderEquipped            = false;
				assignment.UnderEquippedMoralePenalty = 0f;
			}

			return;
		}

		for (var i = 0; i < Assignments.Count; i++) {
			var assignment = Assignments[i];

			var referenceScore = CalculateEquipmentTierScoreWithoutMountEquipment(assignment.ReferenceEquipment);
			var assignedScore  = CalculateEquipmentTierScoreWithoutMountEquipment(assignment.Equipment);

			if (assignedScore >= referenceScore) {
				assignment.IsUnderEquipped            = false;
				assignment.UnderEquippedMoralePenalty = 0f;
				continue;
			}

			var missingTierTotal = referenceScore - assignedScore;
			var moralePenalty    = missingTierTotal * UNDER_EQUIPPED_MORALE_PENALTY_PER_MISSING_TIER;

			assignment.IsUnderEquipped            = true;
			assignment.UnderEquippedMoralePenalty = Math.Min(UNDER_EQUIPPED_MORALE_PENALTY_CAP, moralePenalty);
		}
	}

	private static int CalculateEquipmentTierScoreWithoutMountEquipment(Equipment equipment) {
		var score = 0;

		foreach (var slot in Global.EquipmentSlots) {
			if (slot == EquipmentIndex.Horse || slot == EquipmentIndex.HorseHarness) { continue; }

			var element = equipment.GetEquipmentFromSlot(slot);
			if (element is { IsEmpty: false, Item: not null }) { score += (int)element.Item.Tier; }
		}

		return score;
	}

	private delegate bool EquipmentFilter(KeyValuePair<EquipmentElement, int> equipment);


	private delegate bool AssignmentFilter(Assignment assignment);
}