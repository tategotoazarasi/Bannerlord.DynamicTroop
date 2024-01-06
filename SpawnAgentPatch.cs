#region

	using System.Linq;
	using HarmonyLib;
	using log4net.Core;
	using TaleWorlds.CampaignSystem.Party;
	using TaleWorlds.Library;
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

				//Global.IsInPlayerParty(agentBuildData.AgentOrigin)                &&
				Global.GetAgentParty(agentBuildData.AgentOrigin) != null &&
				agentBuildData.AgentTeam.IsValid) {
				var missionLogic = __instance.GetMissionBehavior<DynamicTroopMissionLogic>();
				if (missionLogic == null) {
					Global.Log("missionLogic is null", Colors.Red, Level.Error);
					return;
				}

				var characterStringId = agentBuildData.AgentCharacter.StringId;
				var party             = Global.GetAgentParty(agentBuildData.AgentOrigin);
				if (party == null ||
					!(EveryoneCampaignBehavior.IsMobilePartyValid(party) || party == MobileParty.MainParty))
					return;

				if (!agentBuildData.AgentCharacter.IsHero) {
					Global.Log($"spawning agent {agentBuildData.AgentCharacter.Name}#{agentBuildData.AgentIndex} for {party.Name}",
							   Colors.Green,
							   Level.Debug);
					if (EveryoneCampaignBehavior.IsMobilePartyValid(party) &&
						!missionLogic.Distributors.ContainsKey(party.Id)) {
						missionLogic.Distributors.Add(party.Id,
													  new PartyEquipmentDistributor(__instance,
																						party,
																						EveryoneCampaignBehavior
																							.PartyArmories[party.Id]));
						Global.Log($"party {party.Name} involved", Colors.Green, Level.Debug);
					}

					var assignment = missionLogic.Distributors[party.Id]
												 .assignments
												 .FirstOrDefault(a => !a.IsAssigned &&
																	  a.Character.StringId == characterStringId);

					if (assignment != null) {
						// 确保equipment不为空
						agentBuildData = agentBuildData.Equipment(assignment.Equipment);
						if (Global.IsInPlayerParty(agentBuildData.AgentOrigin))
							ArmyArmory.AssignEquipment(assignment.Equipment);
						else
							missionLogic.Distributors[party.Id].Spawn(assignment.Equipment);

						assignment.IsAssigned = true;
					}
					else {
						Global.Log($"assignment not found for {agentBuildData.AgentCharacter.StringId}",
								   Colors.Red,
								   Level.Error);
					}
				}

				missionLogic.PartyBattleSides[party.Id] = agentBuildData.AgentTeam.Side;
			}
		}
	}