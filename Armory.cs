using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace DTES2;

[Serializable]
public class Armory {
	private readonly ConcurrentDictionary<EquipmentElement, int> _data = new();

	private readonly MobileParty? _party;

	public Armory() { }

	public Armory(MobileParty party) => this._party = party;

	public MobileParty Party => this._party ?? MobileParty.MainParty;

	public void FillFromRoster(IEnumerable<ItemRosterElement> roster) {
		this._data.Clear();
		roster.
			AsParallel().
			ForAll(
				element => {
					this.Store(element.EquipmentElement, element.Amount);
				}
			);
	}

	public void Store(EquipmentElement equipmentElement, int amount = 1) {
		_ = this._data.TryGetValue(equipmentElement, out int currentAmount);
		if (currentAmount + amount < 0) {
			Logger.Instance.Warning("Armory.Store: Attempted to store negative amount of equipment.");
			return;
		}

		_ = this._data.AddOrUpdate(equipmentElement, amount, (equipment, count) => count + amount);
	}

	public void Store(ItemObject item, int amount = 1) {
		if (amount >= 0) {
			EquipmentElement equipment = new(item);
			this.Store(equipment, amount);
		}

		// TODO: remove item from armory
	}

	public int GetAmount(EquipmentElement equipment) {
		_ = this._data.TryGetValue(equipment, out _);
		return 0;
	}

	public int GetAmount(ItemObject item)
		=> this._data.AsParallel().SumQ(equipment => equipment.Key.Item == item ? equipment.Value : 0);

	public ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>> CreateDistributionTable() {
		ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>> res = new();

		MBList<TroopRosterElement>? troopRoster = this.Party.MemberRoster.GetTroopRoster();
		if (troopRoster == null) {
			return res; // 返回线程安全字典的副本
		}

		troopRoster.
			AsParallel().
			ForAll(
				element => {
					CharacterObject? character     = element.Character;
					int              healthyNumber = element.Number - element.WoundedNumber;
					if (healthyNumber > 0) {
						res[character] = [];
						_ = Parallel.For(
							0,
							healthyNumber,
							_ => {
								res[character].Add(new Equipment());
							}
						);
					}
				}
			);

		ConcurrentDictionary<ItemObject.ItemTypeEnum, ConcurrentBag<EquipmentElement>> itemBags = [];
		this.
			_data.
			Keys.
			AsParallel().
			ForAll(
				element => {
					switch (element.Item.ItemType) {
						case ItemObject.ItemTypeEnum.BodyArmor:
						case ItemObject.ItemTypeEnum.HeadArmor:
						case ItemObject.ItemTypeEnum.HandArmor:
						case ItemObject.ItemTypeEnum.ChestArmor:
						case ItemObject.ItemTypeEnum.LegArmor:
						case ItemObject.ItemTypeEnum.Cape:
							if (!itemBags.ContainsKey(element.Item.ItemType)) {
								_ = itemBags.TryAdd(element.Item.ItemType, []);
							}

							itemBags[element.Item.ItemType].Add(element);
							break;
					}
				}
			);
		itemBags.
			AsParallel().
			ForAll(
				kv => {
					List<EquipmentElement> sorted =
						kv.Value.AsParallel().OrderBy(item => item.Item.Effectiveness).ToList();
					int i = 0;
					foreach (KeyValuePair<CharacterObject, ConcurrentBag<Equipment>> pair in res) {
						foreach (Equipment eq in pair.Value) {
							EquipmentIndex slot = EquipmentIndex.None;
							switch (sorted[i].Item.ItemType) {
								case ItemObject.ItemTypeEnum.BodyArmor:
									slot = EquipmentIndex.Body;
									break;

								case ItemObject.ItemTypeEnum.HeadArmor:
									slot = EquipmentIndex.Head;
									break;

								case ItemObject.ItemTypeEnum.HandArmor:
									slot = EquipmentIndex.Gloves;
									break;

								case ItemObject.ItemTypeEnum.ChestArmor:

									// TODO
									break;

								case ItemObject.ItemTypeEnum.LegArmor:
									slot = EquipmentIndex.Leg;
									break;

								case ItemObject.ItemTypeEnum.Cape:
									slot = EquipmentIndex.Cape;
									break;
							}

							if (slot != EquipmentIndex.None) {
								eq[slot] = sorted[i];
							}

							i++;
						}
					}
				}
			);

		return res;
	}

	public void DoDistribution(Equipment? equipment) {
		if (equipment == null) {
			Logger.Instance.Warning("Armory.DoDistribution: equipment is null.");
			return;
		}

		for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumEquipmentSetSlots; i++) {
			this.Store(equipment.GetEquipmentFromSlot(i), -1);
		}
	}

	public ItemRoster ToItemRoster() {
		ItemRoster itemRoster = [];
		foreach (KeyValuePair<EquipmentElement, int> pair in this._data) {
			_ = itemRoster.AddToCounts(pair.Key, pair.Value);
		}

		return itemRoster;
	}

	public List<SaveableArmoryEntry> ToSavable() {
		List<SaveableArmoryEntry> data = [];
		foreach (KeyValuePair<EquipmentElement, int> pair in this._data) {
			data.Add(new SaveableArmoryEntry(pair.Key, pair.Value));
		}

		return data;
	}

	public void FromSavable(List<SaveableArmoryEntry> data) {
		this._data.Clear();
		foreach (SaveableArmoryEntry entry in data) {
			this.Store(entry.Element, entry.Amount);
		}
	}

	public void DebugPrint() {
		foreach (KeyValuePair<EquipmentElement, int> pair in this._data) {
			Logger.Instance.Information($"{pair.Key.Item?.StringId ?? "unknown"}, {pair.Value}");
		}
	}
}