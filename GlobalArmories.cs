#region
using System;
using System.Collections.Concurrent;
using TaleWorlds.CampaignSystem.Party;
#endregion
namespace DTES2;

[Serializable]
public class GlobalArmories {
	private ConcurrentDictionary<MobileParty, Armory> _data = new ConcurrentDictionary<MobileParty, Armory>();

	public bool AddNewParty(MobileParty party) => this._data.TryAdd(party, new Armory());

	public bool RemoveParty(MobileParty party) => this._data.TryRemove(party, out _);

	public Armory? GetArmory(MobileParty party) => this._data.TryGetValue(party, out Armory armory) ? armory : null;
}