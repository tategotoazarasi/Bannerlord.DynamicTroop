using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.DynamicTroop;

[Obsolete]
public class Armories {
	private readonly ConcurrentDictionary<MobileParty, Armory> _armories = new();

	public bool AddParty(MobileParty party) {
		try { return _armories.TryAdd(party, new Armory(party)); }
		catch (Exception e) {
			Global.Error(e.Message);
			return false;
		}
	}

	public Armory? RemoveParty(MobileParty party) {
		try { return _armories.TryRemove(party, out var armory) ? armory : null; }
		catch (Exception e) {
			Global.Error(e.Message);
			return null;
		}
	}

	public Armory? GetParty(MobileParty party) {
		try { return _armories.TryGetValue(party, out var armory) ? armory : null; }
		catch (Exception e) {
			Global.Error(e.Message);
			return null;
		}
	}

	public void Load(Dictionary<uint, Dictionary<(uint, uint), int>> data) {
		//var partyObj = MBObjectManager.Instance.GetObject()
		_armories.Clear();
		var party = Campaign.Current.MobileParties.First(party => party.Id.InternalValue == 1);
		throw new NotImplementedException();
	}
}