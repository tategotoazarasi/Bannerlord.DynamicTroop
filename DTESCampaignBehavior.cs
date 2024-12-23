#region
using Bannerlord.ButterLib.SaveSystem.Extensions;
using TaleWorlds.CampaignSystem;
#endregion
namespace DTES2;

public class DTESCampaignBehavior : CampaignBehaviorBase {
	private Armory _armory;

	public override void RegisterEvents() { }

	public override void SyncData(IDataStore dataStore) {
		dataStore.SyncDataAsJson("dtes2armory", ref _armory);
	}
}