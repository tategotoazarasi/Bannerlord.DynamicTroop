using System;
using System.Collections.Generic;
using Bannerlord.DynamicTroop.Extensions;
using HarmonyLib;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.DynamicTroop.Patches;

[HarmonyPatch(typeof(Mission), "SpawnAgent")]
public class SpawnAgentPatch {
	/// <summary>
	///     在生成士兵之前执行的逻辑。
	/// </summary>
	/// <param name="__instance">     当前Mission的实例。 </param>
	/// <param name="agentBuildData"> 士兵生成数据。 </param>
	private static void Prefix(Mission __instance, ref AgentBuildData agentBuildData) {
		Global.Debug($"SpawnAgentPatch for {agentBuildData.AgentCharacter.GetName()}");
		try {
			if (!IsCombatMissionWithValidData(__instance, agentBuildData)) return;
			
			var party = Global.GetAgentParty(agentBuildData.AgentOrigin);
			if (!IsPartyValidForProcessing(party)) {
				if ((ModSettings.Instance?.RandomizeNonHeroLedAiPartiesArmor ?? false) && agentBuildData.AgentCharacter is CharacterObject character) {
					Assignment assignment = new(character);
					assignment.FillEmptySlots();
					agentBuildData = agentBuildData.Equipment(assignment.Equipment);
				}
				
				return;
			}
			
			var missionLogic = __instance.GetMissionBehavior<DynamicTroopMissionLogic>();
			if (missionLogic == null) {
				Global.Error("missionLogic is null");
				return;
			}
			
			Global.Debug($"Spawning {agentBuildData.AgentCharacter.GetName()} for {party.Name}");
			ProcessAgentSpawn(agentBuildData, party, missionLogic);
			UpdateBattleSides(missionLogic, party, agentBuildData.AgentTeam.Side);
		}
		catch (Exception e) { Global.Error(e.ToString()); }
	}
	
	/// <summary>
	///     判断当前任务是否为有效的战斗任务，并且士兵生成数据是否有效。
	/// </summary>
	/// <param name="mission">        当前任务。 </param>
	/// <param name="agentBuildData"> 士兵生成数据。 </param>
	/// <returns> 如果是有效的战斗任务并且数据有效，则返回true。 </returns>
	private static bool IsCombatMissionWithValidData(Mission mission, AgentBuildData agentBuildData) {
		return mission is { CombatType: Mission.MissionCombatType.Combat, PlayerTeam: not null } &&
			   !mission.HasMissionBehavior<TournamentBehavior>()                                 &&
			   !mission.HasMissionBehavior<CustomBattleAgentLogic>()                             &&
			   mission.HasMissionBehavior<DynamicTroopMissionLogic>()                            &&
			   agentBuildData is {
									 AgentCharacter   : not null,
									 AgentFormation   : not null,
									 AgentTeam.IsValid: true,
									 AgentOrigin      : not null
								 } &&
			   Global.GetAgentParty(agentBuildData.AgentOrigin) != null;
	}
	
	private static bool IsPartyValidForProcessing(MobileParty? party) {
		return party != null && (party.IsValid() || party == MobileParty.MainParty);
	}
	
	/// <summary>
	///     处理士兵生成的逻辑。
	/// </summary>
	/// <param name="agentBuildData"> 士兵生成数据。 </param>
	/// <param name="party">          相关联的部队。 </param>
	/// <param name="missionLogic">   任务逻辑。 </param>
	private static void ProcessAgentSpawn(AgentBuildData agentBuildData, MobileParty party, DynamicTroopMissionLogic missionLogic) {
		if (agentBuildData.AgentCharacter.IsHero) return;
		
		EnsureDistributorExists(missionLogic, party);
		
		var assignment = GetAssignmentForCharacter(missionLogic, party, agentBuildData.AgentCharacter.StringId);
		if (assignment != null) {
			if (!Global.IsInPlayerParty(agentBuildData.AgentOrigin)) assignment.FillEmptySlots();
			
			agentBuildData = agentBuildData.Equipment(assignment.Equipment);
			AssignOrSpawnEquipment(agentBuildData, assignment, missionLogic, party);
			assignment.IsAssigned = true;
		}
		else { Global.Error($"Assignment not found for {agentBuildData.AgentCharacter.StringId}"); }
	}
	
	/// <summary>
	///     确保给定部队的装备分配器存在。
	/// </summary>
	/// <param name="missionLogic"> 任务逻辑。 </param>
	/// <param name="party">        部队。 </param>
	private static void EnsureDistributorExists(DynamicTroopMissionLogic missionLogic, MobileParty party) {
		if (missionLogic.Distributors.ContainsKey(party.Id)) return;
		
		var partyArmory = EveryoneCampaignBehavior.PartyArmories.TryGetValue(party.Id, out var armory) ? armory : new Dictionary<ItemObject, int>();
		missionLogic.Distributors.Add(party.Id, new PartyEquipmentDistributor(Mission.Current, party, partyArmory));
		Global.Debug($"Party {party.Name} involved");
	}
	
	/// <summary>
	///     根据给定的部队和角色ID获取分配的装备。
	/// </summary>
	/// <param name="missionLogic">      任务逻辑。 </param>
	/// <param name="party">             部队对象。 </param>
	/// <param name="characterStringId"> 角色ID。 </param>
	/// <returns> 找到的分配任务，如果没有则返回null。 </returns>
	private static Assignment? GetAssignmentForCharacter(DynamicTroopMissionLogic missionLogic, MBObjectBase party, string characterStringId) {
		return missionLogic.Distributors[party.Id].Assignments.FirstOrDefaultQ(a => !a.IsAssigned && a.Character.StringId == characterStringId);
	}
	
	/// <summary>
	///     分配士兵的装备。
	/// </summary>
	/// <param name="agentBuildData"> 士兵生成数据。 </param>
	/// <param name="assignment">     分配任务。 </param>
	/// <param name="missionLogic">   任务逻辑。 </param>
	/// <param name="party">          部队对象。 </param>
	private static void AssignOrSpawnEquipment(AgentBuildData agentBuildData, Assignment assignment, DynamicTroopMissionLogic missionLogic, MBObjectBase party) {
		if (Global.IsInPlayerParty(agentBuildData.AgentOrigin))
			ArmyArmory.AssignEquipment(assignment.Equipment);
		else
			missionLogic.Distributors[party.Id].Spawn(assignment.Equipment);
	}
	
	/// <summary>
	///     更新任务逻辑中的部队战斗方面信息。
	/// </summary>
	/// <param name="missionLogic"> 任务逻辑。 </param>
	/// <param name="party">        部队对象。 </param>
	/// <param name="side">         战斗中的一方。 </param>
	private static void UpdateBattleSides(DynamicTroopMissionLogic missionLogic, MBObjectBase party, BattleSideEnum side) {
		if (!missionLogic.PartyBattleSides.ContainsKey(party.Id)) missionLogic.PartyBattleSides.Add(party.Id, side);
	}
}