using System.Collections.Generic;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.DynamicTroop;

public class DynamicTroopMissionLogic : MissionLogic {
	private readonly Dictionary<MBGUID, PartyBattleRecord> _partyBattleRecords = new();

	private readonly HashSet<Agent> _processedAgents = new();

	public readonly Dictionary<MBGUID, PartyEquipmentDistributor> Distributors = new();

	public readonly Dictionary<MBGUID, BattleSideEnum> PartyBattleSides = new();

	private bool _isMissionEnded;

	public override void AfterStart() {
		base.AfterStart();
		Global.Log("AfterStart", Colors.Green, Level.Debug);
		if (!Mission.DoesMissionRequireCivilianEquipment && Mission.CombatType == Mission.MissionCombatType.Combat)
			Distributors.Add(Campaign.Current.MainParty.Id,
							 new PartyEquipmentDistributor(Mission, Campaign.Current.MainParty, ArmyArmory.Armory));
	}

	public override void OnAgentRemoved(Agent       affectedAgent,
										Agent       affectorAgent,
										AgentState  agentState,
										KillingBlow blow) {
		if (Mission is { CombatType: Mission.MissionCombatType.Combat, PlayerTeam.IsValid: true } &&
			agentState is AgentState.Killed or AgentState.Unconscious                             &&
			Global.IsAgentValid(affectedAgent)                                                    &&
			Global.IsAgentValid(affectorAgent)                                                    &&
			!_processedAgents.Contains(affectedAgent)                                             &&
			affectedAgent.Character is { IsHero: false }) {
			Global.Log($"agent {affectedAgent.Character.Name}#{affectedAgent.Index} removed",
					   Colors.Green,
					   Level.Debug);
			_ = _processedAgents.Add(affectedAgent);
			var affectedPartyId = Global.GetAgentParty(affectedAgent.Origin)?.Id;
			var affectorPartyId = Global.GetAgentParty(affectorAgent.Origin)?.Id;
			if (!affectedPartyId.HasValue || !affectorPartyId.HasValue) return;

			if (!_partyBattleRecords.ContainsKey(affectedPartyId.Value))
				_partyBattleRecords.Add(affectedPartyId.Value, new PartyBattleRecord());

			if (!_partyBattleRecords.ContainsKey(affectorPartyId.Value))
				_partyBattleRecords.Add(affectorPartyId.Value, new PartyBattleRecord());

			var affectedBattleRecord = _partyBattleRecords[affectedPartyId.Value];
			var affectorBattleRecord = _partyBattleRecords[affectorPartyId.Value];

			// 获取受击部位
			var hitBodyPart = blow.VictimBodyPart;

			// 获取受击部位的护甲
			var armors   = Global.GetAgentArmors(affectedAgent);
			var hitArmor = ArmorSelector.GetRandomArmorByBodyPart(armors, hitBodyPart);

			Global.ProcessAgentEquipment(affectedAgent,
										 item => {
											 if (hitArmor == null || item.StringId != hitArmor.StringId) {
												 affectedBattleRecord.AddItemToRecover(item);
												 affectorBattleRecord.AddLootedItem(item);
												 Global.Log($"{item.StringId} looted", Colors.Green, Level.Debug);
											 }
											 else if (item.StringId == hitArmor.StringId) {
												 Global.Log($"{item.StringId} damaged", Colors.Red, Level.Debug);
											 }
										 });
		}

		base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
	}

	public override void OnRetreatMission() {
		Global.Log("OnRetreatMission() called", Colors.Green, Level.Debug);
		OnMissionEnded(null);
		base.OnRetreatMission();
	}

	public override void OnSurrenderMission() {
		Global.Log("OnSurrenderMission() called", Colors.Green, Level.Debug);
		OnMissionEnded(null);
		base.OnSurrenderMission();
	}

	public override void OnMissionResultReady(MissionResult missionResult) {
		Global.Log("OnMissionResultReady() called", Colors.Green, Level.Debug);
		OnMissionEnded(missionResult);
		base.OnMissionResultReady(missionResult);
	}

	public override InquiryData OnEndMissionRequest(out bool canLeave) {
		Global.Log("OnEndMissionRequest() called", Colors.Green, Level.Debug);
		OnMissionEnded(null);
		return base.OnEndMissionRequest(out canLeave);
	}

	public override void ShowBattleResults() {
		Global.Log("ShowBattleResults() called", Colors.Green, Level.Debug);
		OnMissionEnded(null);
		base.ShowBattleResults();
	}

	public override void OnBattleEnded() {
		Global.Log("OnBattleEnded() called", Colors.Green, Level.Debug);
		OnMissionEnded(null);
		base.OnBattleEnded();
	}

	private void OnMissionEnded(MissionResult? missionResult) {
		Global.Log("OnMissionEnded() called", Colors.Green, Level.Debug);
		if (_isMissionEnded) return;

		_isMissionEnded = true;

		var playerVictory    = missionResult?.PlayerVictory  ?? false;
		var playerDefeat     = missionResult?.PlayerDefeated ?? false;
		var unresolvedBattle = missionResult == null || !missionResult.BattleResolved;

		// 确定玩家方
		var playerPartyId = Campaign.Current.MainParty.Id;

		foreach (var kvPartyBattleSides in PartyBattleSides) {
			var isPlayerParty = kvPartyBattleSides.Key == playerPartyId;
			var isVictorious = !unresolvedBattle &&
							   ((playerVictory && kvPartyBattleSides.Value == Mission.PlayerTeam.Side) ||
								(playerDefeat  && kvPartyBattleSides.Value != Mission.PlayerTeam.Side));
			var isDefeated = !unresolvedBattle &&
							 ((playerDefeat  && kvPartyBattleSides.Value == Mission.PlayerTeam.Side) ||
							  (playerVictory && kvPartyBattleSides.Value != Mission.PlayerTeam.Side));

			HandlePartyItems(kvPartyBattleSides.Key, isPlayerParty, isVictorious, isDefeated);
		}

		// 回收场上士兵的装备
		foreach (var kvPartyBattleSides in PartyBattleSides) {
			IEnumerable<Agent> partyAgents = Mission.Agents.WhereQ(agent => Global.IsAgentValid(agent) &&
																			agent.IsActive()           &&
																			!agent.IsHero              &&
																			Global.GetAgentParty(agent.Origin)?.Id ==
																			kvPartyBattleSides.Key);
			ReturnEquipmentFromAgents(kvPartyBattleSides.Key, partyAgents);
		}
	}

	private void HandlePartyItems(MBGUID partyId, bool isPlayerParty, bool isVictorious, bool isDefeated) {
		// 检查是否存在对应的battleRecord
		if (!_partyBattleRecords.TryGetValue(partyId, out var battleRecord)) {
			Global.Warn($"No battle record found for party {partyId}");
			return;
		}

		if (isVictorious) {
			ReturnItemsToDestination(partyId, battleRecord.ItemsToRecover, isPlayerParty);
			ReturnItemsToDestination(partyId, battleRecord.LootedItems,    isPlayerParty);
		}
		else if (!isDefeated) { ReturnItemsToDestination(partyId, battleRecord.ItemsToRecover, isPlayerParty); }

		// 被击败的一方不获取任何物品
	}

	private void ReturnItemsToDestination(MBGUID partyId, Dictionary<ItemObject, int> items, bool isPlayerParty) {
		var totalItemCount = 0; // 用于累计物品总数

		foreach (var item in items) {
			if (item.Key == null || item.Value <= 0) continue;

			totalItemCount += item.Value;

			if (isPlayerParty)
				ArmyArmory.AddItemToArmory(item.Key, item.Value);
			else if (Distributors.TryGetValue(partyId, out var distributor))
				distributor.ReturnItem(item.Key, item.Value);
		}

		if (totalItemCount <= 0) return;

		var partyType = isPlayerParty ? "Player party" : "Party";
		Global.Log($"{partyType} {partyId} processed a total of {totalItemCount} items", Colors.Green, Level.Debug);
	}

	private void ReturnEquipmentFromAgents(MBGUID partyId, IEnumerable<Agent> agents) {
		var isPlayerParty  = partyId == Campaign.Current.MainParty.Id;
		var totalItemCount = 0;

		foreach (var agent in agents)
			Global.ProcessAgentEquipment(agent,
										 item => {
											 if (item == null) return;

											 totalItemCount++;
											 if (isPlayerParty)
												 ArmyArmory.AddItemToArmory(item);
											 else if (Distributors.TryGetValue(partyId, out var distributor))
												 distributor.ReturnItem(item, 1);
										 });

		if (totalItemCount <= 0) return;

		var partyType = isPlayerParty ? "Player party" : "Party";
		Global.Log($"{partyType} {partyId} reclaimed {totalItemCount} items from agents", Colors.Green, Level.Debug);
	}

	protected override void OnEndMission() {
		Global.Log("OnEndMission() called", Colors.Green, Level.Debug);
		OnMissionEnded(null);
		base.OnEndMission();
	}
}