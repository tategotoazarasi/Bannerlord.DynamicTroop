#region

	using System.Collections.Generic;
	using System.Linq;
	using log4net.Core;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.Localization;
	using TaleWorlds.MountAndBlade;
	using TaleWorlds.ObjectSystem;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class DynamicTroopMissionLogic : MissionLogic {
		public static   Dictionary<MBGUID, PartyEquipmentDistributor> Distributors    = new();
		public readonly HashSet<Agent>                                ProcessedAgents = new();

		private bool IsMissionEnded;

		public List<ItemObject> LootedItems = new();

		public override void AfterStart() {
			base.AfterStart();
			Clear();
			Global.Log("AfterStart", Colors.Green, Level.Debug);
			if (!Mission.DoesMissionRequireCivilianEquipment && Mission.CombatType == Mission.MissionCombatType.Combat)
				Distributors.Add(Campaign.Current.MainParty.Id,
								 new PartyEquipmentDistributor(Mission, Campaign.Current.MainParty, ArmyArmory.Armory));
		}

		public override void OnAgentCreated(Agent agent) {
			/*if (agent != null && agent.Character != null && agent.Origin!=null) {
				Global.Log($"agent {agent.Character.Name}#{agent.Index} created", Colors.Green, Level.Debug);
				var party = Global.GetAgentParty(agent.Origin);
				if (EveryoneCampaignBehavior.IsMobilePartyValid(party) && !Distributors.ContainsKey(party.Id)) {
					Distributors.Add(party.Id,
									 									 new PartyEquipmentDistributor(Mission, party, EveryoneCampaignBehavior.PartyArmories[party.Id]));
				}
			}*/
		}

		public override void OnAgentBuild(Agent agent, Banner banner) {
			if (agent != null && agent.Character != null)
				Global.Log($"agent {agent.Character.Name}#{agent.Index} built", Colors.Green, Level.Debug);
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
				!affectedAgent.Character.IsHero                                           &&
				((!(affectedAgent.Team.IsPlayerTeam || affectedAgent.Team.IsPlayerAlly) &&
				  Global.IsInPlayerParty(affectorAgent.Origin)) ||
				 Global.IsInPlayerParty(affectedAgent.Origin))) {
				Global.Log($"agent {affectedAgent.Character.StringId} removed", Colors.Green, Level.Debug);
				_ = ProcessedAgents.Add(affectedAgent);

				// 获取受击部位
				var hitBodyPart = blow.VictimBodyPart;

				// 获取受击部位的护甲
				var armors   = Global.GetAgentArmors(affectedAgent);
				var hitArmor = ArmorSelector.GetRandomArmorByBodyPart(armors, hitBodyPart);

				Global.ProcessAgentEquipment(affectedAgent,
											 item => {
												 if (hitArmor == null || item.StringId != hitArmor.StringId) {
													 LootedItems.Add(item);
													 Global.Log($"{item.StringId} added to LootedItems",
																Colors.Green,
																Level.Debug);
												 }
												 else if (item.StringId == hitArmor.StringId) {
													 Global.Log($"{item.StringId} damaged", Colors.Red, Level.Debug);
												 }
											 });
			}

			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
		}

		public void Clear() {
			Distributors.Clear();
			LootedItems.Clear();
			ProcessedAgents.Clear();
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

			missionResult ??= Mission.MissionResult;
			if (Mission.CombatType == Mission.MissionCombatType.Combat &&
				Mission.PlayerTeam != null                             &&
				Mission.PlayerTeam.IsValid                             &&
				Mission.Current.PlayerTeam != null) {
				List<Agent> myAgents = Mission.Agents.Where(agent => Global.IsAgentValid(agent)       &&
																	 agent.Team.IsPlayerTeam          &&
																	 agent.State == AgentState.Active &&
																	 !agent.IsHero                    &&
																	 Global.IsInPlayerParty(agent.Origin))
											  .ToList();
				Global.Log($"{myAgents.Count} player active agent remains on the battlefield", Colors.Green, Level.Debug);
				if (missionResult != null && missionResult.BattleResolved && missionResult.PlayerVictory) {
					Global.Log("player victory", Colors.Green, Level.Debug);
					var lootCount = LootedItems.Count;
					Global.Log($"{lootCount} items looted", Colors.Green, Level.Debug);
					TextObject messageText = new("{=loot_added_message}Added {ITEM_COUNT} items to the army armory.");
					_ = messageText.SetTextVariable("ITEM_COUNT", lootCount);
					InformationManager.DisplayMessage(new InformationMessage(messageText.ToString(), Colors.Green));
					foreach (var item in LootedItems) ArmyArmory.AddItemToArmory(item);

					ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents);
				}
				else if (missionResult == null || !missionResult.BattleResolved || !missionResult.PlayerDefeated) {
					Global.Log("mission ended with player not defeated", Colors.Green, Level.Debug);
					ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents);
				}
			}

			IsMissionEnded = true;
			Clear();
		}

		protected override void OnEndMission() {
			Global.Log("OnEndMission() called", Colors.Green, Level.Debug);
			OnMissionEnded(null);
			base.OnEndMission();
		}
	}