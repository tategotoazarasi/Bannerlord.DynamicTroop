using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bannerlord.DynamicTroop.Extensions;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.DynamicTroop;

public class DynamicTroopMissionLogic : MissionLogic {
	private readonly ConcurrentDictionary<MBGUID, PartyBattleRecord> _partyBattleRecords = new();
	
	private readonly HashSet<Agent> _processedAgents = new();
	
	private readonly Random _random = new();
	
	public readonly Dictionary<MBGUID, PartyEquipmentDistributor> Distributors = new();
	
	public readonly Dictionary<MBGUID, BattleSideEnum> PartyBattleSides = new();
	
	private bool _isMissionEnded;
	
	public override void OnCreated() {
		base.OnCreated();
	}
	
	public override void OnBehaviorInitialize() {
		base.OnBehaviorInitialize();
	}
	
	public override void AfterStart() {
		base.AfterStart();
		Global.Log("AfterStart", Colors.Green, Level.Debug);
		if (!Mission.DoesMissionRequireCivilianEquipment && Mission.CombatType == Mission.MissionCombatType.Combat) {
			var tasks = new List<Task>();
			Distributors.Add(Campaign.Current.MainParty.Id, new PartyEquipmentDistributor(Mission, Campaign.Current.MainParty, ArmyArmory.Armory));
			foreach (var party in MapEvent.PlayerMapEvent.InvolvedParties) {
				var mobileParty = party.MobileParty;
				if (mobileParty != null && mobileParty != Campaign.Current.MainParty && mobileParty.IsValid()) {
					var partyArmory = EveryoneCampaignBehavior.PartyArmories.TryGetValue(mobileParty.Id, out var armory) ? armory : new Dictionary<ItemObject, int>();
					Distributors.Add(mobileParty.Id, new PartyEquipmentDistributor(Mission.Current, mobileParty, partyArmory));
				}
			}
			
			foreach (var distributor in Distributors) { tasks.Add(Task.Run(() => { distributor.Value.RunAsync(); })); }
			
			Task.WhenAll(tasks).GetAwaiter().GetResult();
			Global.Debug("All distribution tasks finished");
		}
	}
	
	/// <summary>
	///     当士兵在战斗中被移除（如被杀或被击晕）时触发的处理。
	/// </summary>
	/// <param name="affectedAgent"> 被移除的士兵。 </param>
	/// <param name="affectorAgent"> 造成移除的士兵。 </param>
	/// <param name="agentState">    被移除的士兵的状态（如被杀、失去意识等）。 </param>
	/// <param name="blow">          造成移除的攻击详情。 </param>
	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow) {
		if (Mission is { CombatType: Mission.MissionCombatType.Combat, PlayerTeam.IsValid: true } &&
			agentState is AgentState.Killed or AgentState.Unconscious                             &&
			affectedAgent.IsValid()                                                               &&
			affectorAgent.IsValid()                                                               &&
			!_processedAgents.Contains(affectedAgent)                                             &&
			affectedAgent.Character is { IsHero: false }) {
			Global.Log($"agent {affectedAgent.Character.Name}#{affectedAgent.Index} removed", Colors.Green, Level.Debug);
			_ = _processedAgents.Add(affectedAgent);
			var affectedPartyId = Global.GetAgentParty(affectedAgent.Origin)?.Id;
			var affectorPartyId = Global.GetAgentParty(affectorAgent.Origin)?.Id;
			if (!affectedPartyId.HasValue || !affectorPartyId.HasValue) return;
			
			if (!_partyBattleRecords.ContainsKey(affectedPartyId.Value))
				_partyBattleRecords.TryAdd(affectedPartyId.Value, new PartyBattleRecord());
			
			if (!_partyBattleRecords.ContainsKey(affectorPartyId.Value))
				_partyBattleRecords.TryAdd(affectorPartyId.Value, new PartyBattleRecord());
			
			var affectedBattleRecord = _partyBattleRecords[affectedPartyId.Value];
			var affectorBattleRecord = _partyBattleRecords[affectorPartyId.Value];
			
			// 获取受击部位
			var hitBodyPart = blow.VictimBodyPart;
			
			// 获取受击部位的护甲
			var armors   = affectedAgent.GetAgentArmors();
			var hitArmor = ArmorSelector.GetRandomArmorByBodyPart(armors, hitBodyPart);
			
			Global.ProcessAgentEquipment(affectedAgent,
										 item => {
											 if (hitArmor == null || item.StringId != hitArmor.StringId) {
												 affectedBattleRecord.AddItemToRecover(item);
												 var dropChance = _random.NextFloat(); // 生成一个0到1之间的随机数
												 if (dropChance > (SubModule.Settings?.DropRate ?? 1f)) return;
												 
												 affectorBattleRecord.AddLootedItem(item);
												 Global.Log($"{item.StringId} looted", Colors.Green, Level.Debug);
											 }
											 else if (item.StringId == hitArmor.StringId) { Global.Log($"{item.StringId} damaged", Colors.Red, Level.Debug); }
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
			var isVictorious  = !unresolvedBattle && (playerVictory && kvPartyBattleSides.Value == Mission.PlayerTeam.Side || playerDefeat  && kvPartyBattleSides.Value != Mission.PlayerTeam.Side);
			var isDefeated    = !unresolvedBattle && (playerDefeat  && kvPartyBattleSides.Value == Mission.PlayerTeam.Side || playerVictory && kvPartyBattleSides.Value != Mission.PlayerTeam.Side);
			
			HandlePartyItems(kvPartyBattleSides.Key, isPlayerParty, isVictorious, isDefeated);
		}
		
		// 回收场上士兵的装备
		foreach (var kvPartyBattleSides in PartyBattleSides) {
			IEnumerable<Agent> partyAgents = Mission.Agents.WhereQ(agent => agent.IsValid() && agent.IsActive() && !agent.IsHero && Global.GetAgentParty(agent.Origin)?.Id == kvPartyBattleSides.Key);
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
			Global.ProcessAgentEquipment(agent,
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
	
	protected override void OnEndMission() {
		Global.Log("OnEndMission() called", Colors.Green, Level.Debug);
		OnMissionEnded(null);
		base.OnEndMission();
	}
}