using System;
using System.Collections.Generic;
using Bannerlord.DynamicTroop.Comparers;
using Bannerlord.DynamicTroop.TroopEquipmentStrategies;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop;

public class PartyEquipmentDistributor {
	private readonly Dictionary<EquipmentElement, int> _equipmentToAssign;

	private readonly List<HorseAndHarness> _horseAndHarnesses = new();

	private readonly ItemRoster? _itemRoster;

	private readonly Mission _mission;

	private readonly MobileParty _party;

	public List<Assignment> Assignments = new();

	public List<CharacterObject> Characters = new();

	private List<TroopEquipmentStrategy> strategies;

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
					//Assignments.Add(new Assignment(troop.Character));
					Characters.Add(troop.Character);

		//Assignments.Sort((x, y) => y.CompareTo(x));
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

		GenerateHorseAndHarnessList();
		DoAssign();
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
		foreach (var kvp in equipment)
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

	private static void AddToDict(IDictionary<int, List<(EquipmentElement Key, int Cnt)>> dict,
								  EquipmentElement                                        element,
								  int                                                     cnt) {
		var familyType = element.Item.HorseComponent?.Monster?.FamilyType ??
						 element.Item.ArmorComponent?.FamilyType ?? -1;
		if (!dict.TryGetValue(familyType, out var list)) {
			list             = new List<(EquipmentElement Key, int Cnt)>();
			dict[familyType] = list;
		}

		list.Add((element, cnt));
	}

	private static void SortEquipmentDictionaries(Dictionary<int, List<(EquipmentElement Key, int Cnt)>> horsesDict,
												  Dictionary<int, List<(EquipmentElement Key, int Cnt)>> harnessDict,
												  List<(EquipmentElement Key, int Cnt)>                  saddles) {
		SortDict(horsesDict,  CompareEquipment);
		SortDict(harnessDict, CompareEquipment);
		saddles.Sort(CompareEquipment);
		return;

		static int CompareEquipment((EquipmentElement Key, int Cnt) x, (EquipmentElement Key, int Cnt) y) {
			var tierCompare = y.Key.Item.Tier.CompareTo(x.Key.Item.Tier);
			return tierCompare != 0 ? tierCompare : y.Key.Item.Value.CompareTo(x.Key.Item.Value);
		}
	}

	private static void SortDict(Dictionary<int, List<(EquipmentElement Key, int Cnt)>> dict,
								 Comparison<(EquipmentElement Key, int Cnt)>            comparer) {
		foreach (var key in dict.Keys) dict[key].Sort(comparer);
	}

	private void GenerateHorseAndHarnessPairs(Dictionary<int, List<(EquipmentElement Key, int Cnt)>>  horsesDict,
											  IDictionary<int, List<(EquipmentElement Key, int Cnt)>> harnessDict,
											  IList<(EquipmentElement Key, int Cnt)>                  saddles) {
		foreach (var kvp in horsesDict) {
			var familyType = kvp.Key;
			var horses     = kvp.Value.ToArrayQ();
			var harnesses = harnessDict.TryGetValue(familyType, out var harnessesList)
								? harnessesList.ToArrayQ()
								: Array.Empty<(EquipmentElement Key, int Cnt)>();

			int horseIndex = 0, harnessIndex = 0;
			while (horseIndex < horses.Length) {
				(var horseItem, var horseCnt) = horses[horseIndex];
				var harnessItem = harnessIndex < harnesses.Length
									  ? harnesses[harnessIndex].Key
									  : new EquipmentElement(null);
				var harnessCnt = harnessIndex < harnesses.Length ? harnesses[harnessIndex].Cnt : 0;

				if (horseCnt > 0) {
					HorseAndHarness hah = new(horseItem, harnessItem);
					_horseAndHarnesses.Add(hah);
					horses[horseIndex] = (horseItem, --horseCnt);

					if (harnessCnt > 0) harnesses[harnessIndex] = (harnessItem, --harnessCnt);
				}

				if (horseCnt == 0) horseIndex++;

				if (harnessCnt == 0 && harnessIndex < harnesses.Length) harnessIndex++;
			}
		}

		AssignSaddlesToHorseAndHarnesses(_horseAndHarnesses, saddles);
		_horseAndHarnesses.Sort((x, y) => y.CompareTo(x));
	}

	private static void AssignSaddlesToHorseAndHarnesses(IEnumerable<HorseAndHarness>           horseAndHarnesses,
														 IList<(EquipmentElement Key, int Cnt)> saddles) {
		var saddleIndex = 0;
		foreach (var hoh in horseAndHarnesses.WhereQ(h => h.Harness == null)) {
			if (saddleIndex >= saddles.Count) break;

			(var saddleItem, var saddleCnt) = saddles[saddleIndex];
			if (saddleCnt <= 0) continue;

			hoh.Harness          = saddleItem;
			saddles[saddleIndex] = (saddleItem, --saddleCnt);
			if (saddleCnt == 0) saddleIndex++;
		}
	}

	private void DoAssign() {
		strategies = new List<TroopEquipmentStrategy> {
														  new DefaultTroopEquipmentStrategy(_equipmentToAssign,
															  new Queue<HorseAndHarness>(_horseAndHarnesses))
													  };
		foreach (var character in Characters) {
			foreach (var strategy in strategies)
				if (strategy.Matches(character)) {
					Assignments.Add(strategy.AssignEquipment(character));
					break;
				}
		}
	}

	public void Spawn(Equipment equipment) {
		// 确保 PartyArmories 包含特定的 _party.Id
		if (!EveryoneCampaignBehavior.PartyArmories.TryGetValue(_party.Id, out var partyArmory)) {
			partyArmory                                       = new Dictionary<ItemObject, int>();
			EveryoneCampaignBehavior.PartyArmories[_party.Id] = partyArmory;
		}

		foreach (var slot in Global.EquipmentSlots) {
			var element = equipment.GetEquipmentFromSlot(slot);
			if (element is not { IsEmpty: false, Item: not null }) continue;

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
}