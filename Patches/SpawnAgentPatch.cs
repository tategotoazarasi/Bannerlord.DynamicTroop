#region

using System;
using System.Linq;
using DynamicTroopEquipmentReupload.Extensions;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

#endregion

namespace DynamicTroopEquipmentReupload.Patches;

[HarmonyPatch(typeof(Mission), nameof(Mission.SpawnAgent))]
public static class SpawnAgentPatch {
	private static void Prefix(Mission __instance, ref AgentBuildData agentBuildData, ref SpawnAgentState? __state) {
		if (__instance.GetMissionBehavior<MissionAgentSpawnLogic>() == null) { return; }

		if (agentBuildData.AgentOrigin == null) { return; }

		// party not valid
		if (!IsPartyValidForProcessing(agentBuildData.AgentOrigin)) {
			if (ModSettings.Instance?.RandomizeNonHeroLedAiPartiesArmor ?? false) {
				if (agentBuildData.AgentOrigin.Troop is CharacterObject troopCharacterObject && !troopCharacterObject.IsHero) {
					var assignment = new Assignment(troopCharacterObject);
					assignment.FillEmptySlots();
					agentBuildData = agentBuildData.Equipment(assignment.Equipment);
				}
			}

			return;
		}

		var dynamicTroopMissionLogic = __instance.GetMissionBehavior<DynamicTroopMissionLogic>();
		if (dynamicTroopMissionLogic == null) { return; }

		var party = Global.GetAgentParty(agentBuildData.AgentOrigin);
		if (party == null) { return; }

		var isMainParty = party == Campaign.Current.MainParty;
		if (!isMainParty && !party.IsValid()) { return; }

		dynamicTroopMissionLogic.TryInitializeDistributors();

		if (!dynamicTroopMissionLogic.Distributors.TryGetValue(party.Id, out var distributor) || distributor == null) { return; }


		var troopCharacter = agentBuildData.AgentOrigin.Troop as CharacterObject;
		if (troopCharacter == null || troopCharacter.IsHero) { return; }

		var assignmentOrNull = distributor.Assignments.FirstOrDefault(x => x.Character == troopCharacter && !x.IsAssigned);
		if (assignmentOrNull == null) { return; }

		assignmentOrNull.IsAssigned = true;


		var isInPlayerParty = agentBuildData.AgentOrigin.IsUnderPlayersCommand;

		if (!isInPlayerParty) { assignmentOrNull.FillEmptySlots(); }


		agentBuildData = agentBuildData.Equipment(assignmentOrNull.Equipment);
		__state        = new SpawnAgentState(assignmentOrNull, isInPlayerParty, isMainParty, distributor);
	}

	private static void Postfix(Agent __result, SpawnAgentState? __state) {
		if (__result == null || __state == null) { return; }

		var equipmentToConsume = __state.Assignment.CreateEquipmentForArmoryConsumption();

		if (__state.IsMainParty) { ArmyArmory.AssignEquipment(equipmentToConsume); }
		else { __state.Distributor.Spawn(equipmentToConsume); }

		// Underequipped morale penalty (player party only)
		if (__state.IsInPlayerParty && (ModSettings.Instance?.Underequipped ?? true)) {
			var penalty = __state.Assignment.UnderEquippedMoralePenalty;
			if (penalty > 0f) { ApplyMoralePenalty(__result, penalty); }
		}

		var dynamicTroopMissionLogic = Mission.Current.GetMissionBehavior<DynamicTroopMissionLogic>();
		dynamicTroopMissionLogic?.RegisterSpawnedAgentAssignment(__result, __state.Assignment);
	}

	private static void ApplyMoralePenalty(Agent agent, float penalty) {
		var currentMorale = agent.GetMorale();

		if (currentMorale < 0f)
			return;

		agent.SetMorale(Math.Max(0f, currentMorale - penalty));
	}


	private static bool IsPartyValidForProcessing(IAgentOriginBase agentOrigin) {
		var party = agentOrigin switch {
						PartyAgentOrigin partyAgentOrigin           => partyAgentOrigin.Party,
						PartyGroupAgentOrigin partyGroupAgentOrigin => partyGroupAgentOrigin.Party,
						SimpleAgentOrigin simpleAgentOrigin         => simpleAgentOrigin.Party,
						_                                           => null
					};

		if (party is not { IsValid: true, MobileParty: not null }) { return false; }

		var mobileParty = party.MobileParty;
		if (mobileParty.IsCaravan || mobileParty.IsVillager || mobileParty.IsMilitia || mobileParty.IsBandit) { return false; }

		return true;
	}

	private sealed class SpawnAgentState {
		public SpawnAgentState(Assignment assignment, bool isInPlayerParty, bool isMainParty, PartyEquipmentDistributor distributor) {
			Assignment      = assignment;
			IsInPlayerParty = isInPlayerParty;
			IsMainParty     = isMainParty;
			Distributor     = distributor;
		}

		public Assignment Assignment { get; }

		public bool IsInPlayerParty { get; }

		public bool IsMainParty { get; }

		public PartyEquipmentDistributor Distributor { get; }
	}
}