#region

	using System.Collections.Generic;
	using log4net.Core;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;
	using TaleWorlds.ObjectSystem;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class DynamicTroopMissionLogic : MissionLogic {
		public readonly HashSet<Agent> ProcessedAgents = new();

		public Dictionary<MBGUID, PartyEquipmentDistributor> Distributors = new();

		private bool IsMissionEnded;

		public Dictionary<MBGUID, PartyBattleRecord> PartyBattleRecords = new();
		public Dictionary<MBGUID, BattleSideEnum>    PartyBattleSides   = new();

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
			if (Mission.CombatType == Mission.MissionCombatType.Combat                    &&
				agentState         != null                                                &&
				(agentState == AgentState.Killed || agentState == AgentState.Unconscious) &&
				Global.IsAgentValid(affectedAgent)                                        &&
				Global.IsAgentValid(affectorAgent)                                        &&
				Mission            != null                                                &&
				Mission.PlayerTeam != null                                                &&
				Mission.PlayerTeam.IsValid                                                &&
				!ProcessedAgents.Contains(affectedAgent)                                  &&
				!affectedAgent.Character.IsHero) {
				Global.Log($"agent {affectedAgent.Character.Name}#{affectedAgent.Index} removed",
						   Colors.Green,
						   Level.Debug);
				_ = ProcessedAgents.Add(affectedAgent);
				var affectedPartyId = Global.GetAgentParty(affectedAgent.Origin)?.Id;
				var affectorPartyId = Global.GetAgentParty(affectorAgent.Origin)?.Id;
				if (!affectedPartyId.HasValue || !affectorPartyId.HasValue) return;
				if (!PartyBattleRecords.ContainsKey(affectedPartyId.Value))
					PartyBattleRecords.Add(affectedPartyId.Value, new PartyBattleRecord());
				if (!PartyBattleRecords.ContainsKey(affectorPartyId.Value))
					PartyBattleRecords.Add(affectorPartyId.Value, new PartyBattleRecord());
				var affectedBattleRecord = PartyBattleRecords[affectedPartyId.Value];
				var affectorBattleRecord = PartyBattleRecords[affectorPartyId.Value];

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
			if (IsMissionEnded) return;
			IsMissionEnded = true;

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
		}

		private void HandlePartyItems(MBGUID partyId, bool isPlayerParty, bool isVictorious, bool isDefeated) {
			var battleRecord = PartyBattleRecords[partyId];

			if (isVictorious) {
				ReturnItemsToDestination(partyId, battleRecord.ItemsToRecover, isPlayerParty);
				ReturnItemsToDestination(partyId, battleRecord.LootedItems,    isPlayerParty);
			}
			else if (!isDefeated) { ReturnItemsToDestination(partyId, battleRecord.ItemsToRecover, isPlayerParty); }
			// 被击败的一方不获取任何物品
		}

		private void ReturnItemsToDestination(MBGUID partyId, Dictionary<ItemObject, int> items, bool isPlayerParty) {
			foreach (var item in items) {
				if (item.Key == null) continue;

				if (isPlayerParty)
					ArmyArmory.AddItemToArmory(item.Key, item.Value);
				else if (Distributors.TryGetValue(partyId, out var distributor))
					distributor.ReturnItem(item.Key, item.Value);
			}
		}

		protected override void OnEndMission() {
			Global.Log("OnEndMission() called", Colors.Green, Level.Debug);
			OnMissionEnded(null);
			base.OnEndMission();
		}
	}