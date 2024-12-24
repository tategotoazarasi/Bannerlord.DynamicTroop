using System.Collections.Concurrent;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace DTES2;

public class DTESCampaignBehavior : CampaignBehaviorBase {
	private readonly ConcurrentDictionary<MobileParty, Armory>? _data;
	private          Armory?                                    _armory;

	public override void RegisterEvents()
		=> CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, this.OnTroopRecruited);

	public void OnTroopRecruited(
		Hero            recruiterHero,
		Settlement      recruitmentSettlement,
		Hero            recruitmentSource,
		CharacterObject troop,
		int             amount
	) { }

	public override void SyncData(IDataStore dataStore) => dataStore.SyncDataAsJson("dtes2armory", ref this._armory);
}