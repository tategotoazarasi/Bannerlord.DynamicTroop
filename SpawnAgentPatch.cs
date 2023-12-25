#region

	using System.Linq;
	using HarmonyLib;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(Mission), "SpawnAgent")]
	public class SpawnAgentPatch {
		private static void Prefix(Mission __instance, ref AgentBuildData agentBuildData) {
			// 确保当前任务是战斗类型的，并且AgentBuildData及其属性已初始化
			if (__instance.CombatType         == Mission.MissionCombatType.Combat &&
				__instance.PlayerTeam         != null                             &&
				agentBuildData.AgentCharacter != null                             &&
				agentBuildData.AgentFormation != null                             &&
				agentBuildData.AgentTeam      != null                             &&
				agentBuildData.AgentOrigin    != null                             &&
				agentBuildData.AgentOrigin.IsUnderPlayersCommand                  &&
				agentBuildData.AgentTeam.IsValid                                  &&
				agentBuildData.AgentTeam.IsPlayerTeam                             &&
				!agentBuildData.AgentCharacter.IsHero) {
				//Global.Log("creating agent");
				var characterStringId = agentBuildData.AgentCharacter.StringId;
				var assignment =
					MyMissionBehavior.assignments.FirstOrDefault(a => !a.IsAssigned &&
																	  a.Character.StringId == characterStringId);

				if (assignment != null) {
					// 确保equipment不为空
					agentBuildData = agentBuildData.Equipment(assignment.Equipment);
					ArmyArmory.AssignEquipment(assignment.Equipment);
					assignment.IsAssigned = true;
				}
			}
		}
	}