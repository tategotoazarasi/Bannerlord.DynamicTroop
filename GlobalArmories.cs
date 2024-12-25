using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;

namespace DTES2;

[Serializable]
public class GlobalArmories {
	private readonly ConcurrentDictionary<MobileParty, Armory> _data = new();

	private readonly Armory _playerArmory = new();

	public bool AddNewParty(MobileParty party) => this._data.TryAdd(party, new Armory());

	public bool RemoveParty(MobileParty party) => this._data.TryRemove(party, out _);

	public Armory? GetArmory(MobileParty party)
		=> party.IsMainParty                                ? this._playerArmory :
		   this._data.TryGetValue(party, out Armory armory) ? armory : null;

	public Dictionary<MobileParty, List<SaveableArmoryEntry>> ToSavable() {
		Dictionary<MobileParty, List<SaveableArmoryEntry>> data = [];
		foreach (KeyValuePair<MobileParty, Armory> pair in this._data) {
			data.Add(pair.Key, pair.Value.ToSavable());
		}

		data.Add(MobileParty.MainParty, this._playerArmory.ToSavable());
		return data;
	}

	public void FromSavable(Dictionary<MobileParty, List<SaveableArmoryEntry>> data) {
		this._data.Clear();
		foreach (KeyValuePair<MobileParty, List<SaveableArmoryEntry>> pair in data) {
			if (pair.Key.IsMainParty) {
				this._playerArmory.FromSavable(pair.Value);
			} else {
				Armory armory = new(pair.Key);
				armory.FromSavable(pair.Value);
				_ = this._data.TryAdd(pair.Key, armory);
			}
		}
	}

	public void DebugPrint() {
		foreach (KeyValuePair<MobileParty, Armory> pair in this._data) {
			Logger.Instance.Debug($"Party: {pair.Key.StringId}");
			pair.Value.DebugPrint();
		}

		this._playerArmory.DebugPrint();
	}
}