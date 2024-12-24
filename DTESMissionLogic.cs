using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.MountAndBlade;

namespace DTES2;

public class DTESMissionLogic : MissionLogic {
	public override void AfterStart() {
		base.AfterStart();
		Logger.Instance.Information("AfterStart");
		if (!this.Mission.DoesMissionRequireCivilianEquipment              &&
			this.Mission.CombatType    == Mission.MissionCombatType.Combat &&
			Campaign.Current.MainParty != null                             &&
			MapEvent.PlayerMapEvent    != null) {
			// TODO
		}
	}
}