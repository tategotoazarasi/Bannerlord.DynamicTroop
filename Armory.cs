using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

	public Dictionary<CharacterObject, List<Equipment>> CreateDistributionTable() {
		Dictionary<CharacterObject, List<Equipment>> res         = [];
		MBList<TroopRosterElement>?                  troopRoster = this._party.MemberRoster.GetTroopRoster();
		if (troopRoster == null) {
			return res;
		}

		foreach (TroopRosterElement element in troopRoster) {
			CharacterObject? character     = element.Character;
			int              healthyNumber = element.Number - element.WoundedNumber;
			if (healthyNumber > 0) {
				res.Add(character, new List<Equipment>(element.Number - element.WoundedNumber));
			}
		}

		EquipmentElement[]? ordered = this.
									  _data.
									  Keys.
									  AsParallel().
									  Where(a => a.Item.ItemType == ItemObject.ItemTypeEnum.BodyArmor).
									  OrderByQ(a => a.Item.Effectiveness).
									  ToArrayQ();

		// TODO
		return res;
	}

	public void DoDistribution(Equipment? equipment) {
		if (equipment == null) {
			Logger.Instance.Warning("Armory.DoDistribution: equipment is null.");
		}

		//TODO
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