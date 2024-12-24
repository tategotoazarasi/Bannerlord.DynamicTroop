#region
using System.Collections.Concurrent;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
#endregion
namespace DTES2;

public class DTESCampaignBehavior : CampaignBehaviorBase {
	private Armory                                    _armory;
	private ConcurrentDictionary<MobileParty, Armory> _data;

	public override void RegisterEvents() { }

	public override void SyncData(IDataStore dataStore) {
		dataStore.SyncDataAsJson("dtes2armory", ref _armory);
	}
}