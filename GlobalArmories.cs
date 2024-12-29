using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;

namespace DTES2;

[Serializable]
public static class GlobalArmories {
	private static readonly ConcurrentDictionary<MobileParty, Armory> _data = new();

	private static readonly Armory _playerArmory = new();

	public static bool AddNewParty(MobileParty party) => _data.TryAdd(party, new Armory());

	public static bool RemoveParty(MobileParty party) => _data.TryRemove(party, out _);

	public static Armory? GetArmory(MobileParty party)
		=> party.IsMainParty                           ? _playerArmory :
		   _data.TryGetValue(party, out Armory armory) ? armory : null;

	public static Dictionary<MobileParty, List<SaveableArmoryEntry>> ToSavable() {
		Dictionary<MobileParty, List<SaveableArmoryEntry>> data = [];
		foreach (KeyValuePair<MobileParty, Armory> pair in _data) {
			data.Add(pair.Key, pair.Value.ToSavable());
		}

		data.Add(MobileParty.MainParty, _playerArmory.ToSavable());
		return data;
	}

	public static void FromSavable(Dictionary<MobileParty, List<SaveableArmoryEntry>> data) {
		_data.Clear();
		foreach (KeyValuePair<MobileParty, List<SaveableArmoryEntry>> pair in data) {
			if (pair.Key.IsMainParty) {
				_playerArmory.FromSavable(pair.Value);
			} else {
				Armory armory = new(pair.Key);
				armory.FromSavable(pair.Value);
				_ = _data.TryAdd(pair.Key, armory);
			}
		}
	}

	public static void DebugPrint() {
		foreach (KeyValuePair<MobileParty, Armory> pair in _data) {
			Logger.Instance.Debug($"Party: {pair.Key.StringId}");
			pair.Value.DebugPrint();
		}

		_playerArmory.DebugPrint();
	}
}