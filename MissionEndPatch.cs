#region

	using System.Linq;
	using HarmonyLib;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(CampaignEvents), "OnMissionEnded")]
	public class MissionEndPatch {
	private static void Postfix(IMission mission) {
		// 尝试将 IMission 转换为 Mission
		if (mission is Mission missionInstance) {
			var myAgents = missionInstance.Agents
										  .Where(agent => agent.IsHuman && agent.Team != null && agent.Team.IsPlayerTeam)
										  .ToList();

			ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents);
		}
	}
}