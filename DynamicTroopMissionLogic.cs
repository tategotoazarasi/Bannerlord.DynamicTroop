using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DynamicTroopEquipmentReupload.Extensions;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace DynamicTroopEquipmentReupload;

public class DynamicTroopMissionLogic : MissionLogic {
	private readonly Dictionary<Agent, Assignment> _assignmentByAgent = new();
	private readonly ConcurrentDictionary<MBGUID, PartyBattleRecord> _partyBattleRecords = new();

	private readonly HashSet<Agent> _processedAgents = new();

	private readonly Random _random = new();

	public readonly Dictionary<MBGUID, PartyEquipmentDistributor> Distributors = new();

	public readonly Dictionary<MBGUID, BattleSideEnum> PartyBattleSides = new();
	private bool _areDistributorsInitialized;

	private bool _isMissionEnded;

	public override void OnCreated() {
		base.OnCreated();
	}

	public override void OnBehaviorInitialize() {
		base.OnBehaviorInitialize();
		Global.Log("OnBehaviorInitialize", Colors.Green, Level.Debug);

		_processedAgents.Clear();
		_partyBattleRecords.Clear();
		Distributors.Clear();
		PartyBattleSides.Clear();
		_assignmentByAgent.Clear();

		_isMissionEnded = false;

		_areDistributorsInitialized = false;
		TryInitializeDistributors();
	}

	public void TryInitializeDistributors() {
		if (_areDistributorsInitialized || _isMissionEnded) { return; }

		if (MapEvent.PlayerMapEvent == null || MapEvent.PlayerMapEvent.IsRaid) { return; }

		var mainParty            = Campaign.Current.MainParty;
		var mainPartyDistributor = new PartyEquipmentDistributor(Mission, mainParty, ArmyArmory.Armory);
		mainPartyDistributor.RunAsync();
		Distributors[mainParty.Id] = mainPartyDistributor;

		PartyBattleSides[mainParty.Id] = PartyBase.MainParty.Side;

		foreach (var partyBase in MapEvent.PlayerMapEvent.InvolvedParties) {
			var party = partyBase.MobileParty;
			if (party == null || !party.IsValid() || party == Campaign.Current.MainParty || party.LeaderHero == null) { continue; }

			if (!EveryoneCampaignBehavior.PartyArmories.TryGetValue(party.Id, out var partyArmory)) { continue; }

			var distributor = new PartyEquipmentDistributor(Mission, party, partyArmory);
			distributor.RunAsync();
			Distributors[party.Id] = distributor;

			PartyBattleSides[party.Id] = partyBase.Side;
		}

		_areDistributorsInitialized = true;
	}


	public override void EarlyStart() {
		base.EarlyStart();
		Global.Log("EarlyStart", Colors.Green, Level.Debug);
	}


	public override void AfterStart() {
		base.AfterStart();
		Global.Log("AfterStart", Colors.Green, Level.Debug);
	}


	/// <summary>
	///     当士兵在战斗中被移除（如被杀或被击晕）时触发的处理。
	/// </summary>
	/// <param name="affectedAgent"> 被移除的士兵。 </param>
	/// <param name="affectorAgent"> 造成移除的士兵。 </param>
	/// <param name="agentState">    被移除的士兵的状态（如被杀、失去意识等）。 </param>
	/// <param name="blow">          造成移除的攻击详情。 </param>
	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow) {
		if (Mission is not { PlayerTeam.IsValid: true }) {
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
			return;
		}


		if (!affectedAgent.IsValid() || affectedAgent.Character is not { IsHero: false }) {
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
			return;
		}

		if (_processedAgents.Contains(affectedAgent)) {
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
			return;
		}

		// potential loot for the opposing side.
		if (agentState is AgentState.Killed or AgentState.Unconscious) {
			_ = _processedAgents.Add(affectedAgent);

			var affectedPartyId = Global.GetAgentParty(affectedAgent.Origin)?.Id;
			if (!affectedPartyId.HasValue) {
				base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
				return;
			}

			if (!_partyBattleRecords.ContainsKey(affectedPartyId.Value))
				_partyBattleRecords.TryAdd(affectedPartyId.Value, new PartyBattleRecord());

			var affectedBattleRecord = _partyBattleRecords[affectedPartyId.Value];

			PartyBattleRecord? affectorBattleRecord = null;
			var                affectorPartyId      = affectorAgent.IsValid() ? Global.GetAgentParty(affectorAgent.Origin)?.Id : null;
			if (affectorPartyId.HasValue) {
				if (!_partyBattleRecords.ContainsKey(affectorPartyId.Value))
					_partyBattleRecords.TryAdd(affectorPartyId.Value, new PartyBattleRecord());

				affectorBattleRecord = _partyBattleRecords[affectorPartyId.Value];
			}

			var hitBodyPart = blow.VictimBodyPart;
			var armors      = affectedAgent.GetAgentArmors();
			var hitArmor    = ArmorSelector.GetRandomArmorByBodyPart(armors, hitBodyPart);

			ProcessAgentEquipmentRespectingTemporarySlots(
				affectedAgent,
				item => {
					if (item == null || !ItemBlackList.Test(item))
						return;

					if (hitArmor != null && item.StringId == hitArmor.StringId)
						return;

					var dropRate = SubModule.Settings?.DropRate ?? 1f;

					affectedBattleRecord.AddItemToRecover(item);

					if (affectorBattleRecord != null && _random.NextFloat() <= dropRate) {
						affectorBattleRecord.AddLootedItem(item);
						Global.Log($"{item.StringId} looted", Colors.Green, Level.Debug);
					}
				});
		}

		//Routed (alive, removed mid-battle), make sure their equipment is still returned to their own armory
		else if (!_isMissionEnded && agentState is AgentState.Routed or AgentState.Deleted) {
			_ = _processedAgents.Add(affectedAgent);
			var affectedPartyId = Global.GetAgentParty(affectedAgent.Origin)?.Id;
			if (!affectedPartyId.HasValue) {
				base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
				return;
			}

			if (!_partyBattleRecords.ContainsKey(affectedPartyId.Value))
				_partyBattleRecords.TryAdd(affectedPartyId.Value, new PartyBattleRecord());

			var affectedBattleRecord = _partyBattleRecords[affectedPartyId.Value];
			ProcessAgentEquipmentRespectingTemporarySlots(affectedAgent, item => { affectedBattleRecord.AddItemToRecover(item); });
			Global.Log(
				$"[DTES] Killed/Unconscious processed for {affectedAgent.Character?.Name} | party={affectedPartyId.Value} | recordRecoverNow={affectedBattleRecord.ItemsToRecoverCount}",
				Colors.Green,
				Level.Debug);
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
			var isVictorious  = !unresolvedBattle && (playerVictory && kvPartyBattleSides.Value == Mission.PlayerTeam.Side || playerDefeat  && kvPartyBattleSides.Value != Mission.PlayerTeam.Side);
			var isDefeated    = !unresolvedBattle && (playerDefeat  && kvPartyBattleSides.Value == Mission.PlayerTeam.Side || playerVictory && kvPartyBattleSides.Value != Mission.PlayerTeam.Side);

			HandlePartyItems(kvPartyBattleSides.Key, isPlayerParty, isVictorious, isDefeated);
		}

		// 回收场上士兵的装备
		foreach (var kvPartyBattleSides in PartyBattleSides) {
			IEnumerable<Agent> partyAgents = Mission.Agents.WhereQ(agent =>
																	   agent.IsValid()                   &&
																	   agent.IsActive()                  &&
																	   !agent.IsHero                     &&
																	   !_processedAgents.Contains(agent) &&
																	   Global.GetAgentParty(agent.Origin)?.Id == kvPartyBattleSides.Key);
			ReturnEquipmentFromAgents(kvPartyBattleSides.Key, partyAgents);
		}
	}

	/// <summary>
	///     处理部队战后的物品管理。
	/// </summary>
	/// <param name="partyId">       部队的唯一标识符。 </param>
	/// <param name="isPlayerParty"> 是否为玩家的部队。 </param>
	/// <param name="isVictorious">  部队是否胜利。 </param>
	/// <param name="isDefeated">    部队是否被击败。 </param>
	private void HandlePartyItems(MBGUID partyId, bool isPlayerParty, bool isVictorious, bool isDefeated) {
		// 检查是否存在对应的battleRecord
		if (!_partyBattleRecords.TryGetValue(partyId, out var battleRecord)) {
			Global.Warn($"No battle record found for party {partyId}");
			return;
		}

		if (isVictorious) {
			ReturnItemsToDestination(partyId, battleRecord.ItemsToRecover, isPlayerParty);
			ReturnItemsToDestination(partyId, battleRecord.LootedItems,    isPlayerParty);
			if (isPlayerParty) {
				MessageDisplayService.EnqueueMessage(new InformationMessage(LocalizedTexts.GetLootAddedMessage(battleRecord.LootedItemsCount),                   Colors.Green));
				MessageDisplayService.EnqueueMessage(new InformationMessage(LocalizedTexts.GetItemsRecoveredFromFallenMessage(battleRecord.ItemsToRecoverCount), Colors.Green));
			}
		}
		else if (!isDefeated) { ReturnItemsToDestination(partyId, battleRecord.ItemsToRecover, isPlayerParty); }

		// 被击败的一方不获取任何物品
	}

	/// <summary>
	///     将物品返回到指定部队。
	/// </summary>
	/// <param name="partyId">       部队的唯一标识符。 </param>
	/// <param name="items">         要返回的物品及其数量。 </param>
	/// <param name="isPlayerParty"> 是否为玩家的部队。 </param>
	private void ReturnItemsToDestination(MBGUID partyId, ConcurrentDictionary<ItemObject, int> items, bool isPlayerParty) {
		var totalItemCount = 0; // 用于累计物品总数

		foreach (var item in items) {
			if (item.Key == null || item.Value <= 0 || !ItemBlackList.Test(item.Key)) continue;

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

	/// <summary>
	///     从参与战斗的士兵那里回收装备。
	/// </summary>
	/// <param name="partyId"> 部队的唯一标识符。 </param>
	/// <param name="agents">  参与战斗的士兵集合。 </param>
	private void ReturnEquipmentFromAgents(MBGUID partyId, IEnumerable<Agent> agents) {
		var isPlayerParty  = partyId == Campaign.Current.MainParty.Id;
		var totalItemCount = 0;

		foreach (var agent in agents)
			ProcessAgentEquipmentRespectingTemporarySlots(agent,
														  item => {
															  if (item is null || !ItemBlackList.Test(item)) return;

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

	public void RegisterSpawnedAgentAssignment(Agent agent, Assignment assignment) {
		_assignmentByAgent[agent] = assignment;
	}


	private void ProcessAgentEquipmentRespectingTemporarySlots(Agent agent, Action<ItemObject> processEquipmentItem) {
		if (_assignmentByAgent.TryGetValue(agent, out var assignment)) {
			var processedItemCount = 0;

			Global.ProcessAgentEquipment(
				agent,
				item => {
					processedItemCount++;
					processEquipmentItem(item);
				},
				(slot, missionWeapon, spawnElement) => !assignment.IsTemporarySlot(slot, spawnElement.Item));

			if (processedItemCount == 0) {
				Global.Log($"[DTES] Agent equipment returned 0 items. Fallback to Assignment. Agent={agent.Character?.Name}", Colors.Yellow, Level.Debug);
				ProcessAssignmentEquipmentFallback(assignment, processEquipmentItem);
			}

			return;
		}

		Global.ProcessAgentEquipment(agent, processEquipmentItem);
	}

	private static void ProcessAssignmentEquipmentFallback(Assignment assignment, Action<ItemObject> processEquipmentItem) {
		foreach (var slot in Global.EquipmentSlots) {
			var element = assignment.Equipment[slot];
			if (element.IsEmpty || element.Item is null)
				continue;

			if (element.Item.IsBannerItem || element.Item.ItemType == ItemObject.ItemTypeEnum.Banner)
				continue;

			if (assignment.IsTemporarySlot(slot, element.Item))
				continue;

			if (element.Item.ItemType is ItemObject.ItemTypeEnum.Arrows
										 or ItemObject.ItemTypeEnum.Bolts
										 or ItemObject.ItemTypeEnum.Thrown) { continue; }

			processEquipmentItem(element.Item);
		}
	}


	protected override void OnEndMission() {
		Global.Log("OnEndMission() called", Colors.Green, Level.Debug);
		OnMissionEnded(null);
		base.OnEndMission();
	}
}