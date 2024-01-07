#region

	using System.Collections.Generic;
	using System.Linq;
	using HarmonyLib;
	using TaleWorlds.CampaignSystem.Party;
	using TaleWorlds.Core;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(Mission), "SpawnAgent")]
	public class SpawnAgentPatch {
		private static void Prefix(Mission __instance, ref AgentBuildData agentBuildData) {
			if (!IsCombatMissionWithValidData(__instance, agentBuildData)) return;

			var party = Global.GetAgentParty(agentBuildData.AgentOrigin);
			if (!IsPartyValidForProcessing(party)) return;

			var missionLogic = __instance.GetMissionBehavior<DynamicTroopMissionLogic>();
			if (missionLogic == null) {
				Global.Error("missionLogic is null");
				return;
			}

			ProcessAgentSpawn(agentBuildData, party, missionLogic);
			UpdateBattleSides(missionLogic, party, agentBuildData.AgentTeam.Side);
		}

		private static bool IsCombatMissionWithValidData(Mission mission, AgentBuildData agentBuildData) {
			return mission.CombatType            == Mission.MissionCombatType.Combat &&
				   mission.PlayerTeam            != null                             &&
				   agentBuildData.AgentCharacter != null                             &&
				   agentBuildData.AgentFormation != null                             &&
				   agentBuildData.AgentTeam      != null                             &&
				   agentBuildData.AgentOrigin    != null                             &&

				   //Global.IsInPlayerParty(agentBuildData.AgentOrigin)                &&
				   Global.GetAgentParty(agentBuildData.AgentOrigin) != null &&
				   agentBuildData.AgentTeam.IsValid;
		}

		private static bool IsPartyValidForProcessing(MobileParty? party) {
			return party != null && (EveryoneCampaignBehavior.IsMobilePartyValid(party) || party == MobileParty.MainParty);
		}

		private static void ProcessAgentSpawn(AgentBuildData           agentBuildData,
											  MobileParty              party,
											  DynamicTroopMissionLogic missionLogic) {
			if (!agentBuildData.AgentCharacter.IsHero) {
				EnsureDistributorExists(missionLogic, party);

				var assignment = GetAssignmentForCharacter(missionLogic, party, agentBuildData.AgentCharacter.StringId);
				if (assignment != null) {
					agentBuildData = agentBuildData.Equipment(assignment.Equipment);
					AssignOrSpawnEquipment(agentBuildData, assignment, missionLogic, party);
					assignment.IsAssigned = true;
				}
				else { Global.Error($"Assignment not found for {agentBuildData.AgentCharacter.StringId}"); }
			}
		}

		private static void EnsureDistributorExists(DynamicTroopMissionLogic missionLogic, MobileParty party) {
			if (!missionLogic.Distributors.ContainsKey(party.Id)) {
				var partyArmory = EveryoneCampaignBehavior.PartyArmories.TryGetValue(party.Id, out var armory)
									  ? armory
									  : new Dictionary<ItemObject, int>();
				missionLogic.Distributors.Add(party.Id, new PartyEquipmentDistributor(Mission.Current, party, partyArmory));
				Global.Debug($"Party {party.Name} involved");
			}
		}

		private static Assignment?
			GetAssignmentForCharacter(DynamicTroopMissionLogic missionLogic, MobileParty party, string characterStringId) {
			return missionLogic.Distributors[party.Id]
							   .assignments.FirstOrDefault(a => !a.IsAssigned && a.Character.StringId == characterStringId);
		}

		private static void AssignOrSpawnEquipment(AgentBuildData           agentBuildData,
												   Assignment               assignment,
												   DynamicTroopMissionLogic missionLogic,
												   MobileParty              party) {
			if (Global.IsInPlayerParty(agentBuildData.AgentOrigin))
				ArmyArmory.AssignEquipment(assignment.Equipment);
			else
				missionLogic.Distributors[party.Id].Spawn(assignment.Equipment);
		}

		private static void UpdateBattleSides(DynamicTroopMissionLogic missionLogic,
											  MobileParty              party,
											  BattleSideEnum           side) {
			if (!missionLogic.PartyBattleSides.ContainsKey(party.Id)) missionLogic.PartyBattleSides.Add(party.Id, side);
		}
	}