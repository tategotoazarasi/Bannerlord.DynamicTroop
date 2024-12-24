#region
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.MountAndBlade;
#endregion
namespace DTES2;

public class DTESMissionLogic : MissionLogic {
	public override void AfterStart() {
		base.AfterStart();
		Logger.Instance.Information("AfterStart");
		if (
			!Mission.DoesMissionRequireCivilianEquipment && Mission.CombatType == Mission.MissionCombatType.Combat && Campaign.Current.MainParty != null && MapEvent.PlayerMapEvent != null
			) {
			// TODO
		}
	}
}