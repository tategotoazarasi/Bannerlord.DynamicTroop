#region

	using System.Collections.Generic;
	using System.Linq;
	using HarmonyLib;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(CampaignEvents), "OnMissionEnded")]
	public class MissionEndPatch {
		private static void Postfix(IMission mission) {
			// 尝试将 IMission 转换为 Mission
			if (mission is Mission missionInstance                             &&
				missionInstance.CombatType == Mission.MissionCombatType.Combat &&
				missionInstance.PlayerTeam != null                             &&
				missionInstance.PlayerTeam.IsValid                             &&
				Mission.Current.PlayerTeam != null) {
				List<Agent> myAgents = missionInstance.Agents
													  .Where(agent => agent != null           &&
																	  agent.IsHuman           &&
																	  agent.Team != null      &&
																	  agent.Team.IsValid      &&
																	  agent.Team.IsPlayerTeam &&
																	  !agent.IsHero           &&
																	  agent.Origin != null    &&
																	  agent.Origin.IsUnderPlayersCommand)
													  .ToList();

				if (missionInstance.MissionResult != null        &&
					missionInstance.MissionResult.BattleResolved &&
					missionInstance.MissionResult.PlayerVictory) {
					InformationManager
						.DisplayMessage(new InformationMessage($"已添加 {AgentDeathLootPatch.LootedItems.Count} 件战利品到部队军火库。",
															   Colors.Green));
					foreach (var item in AgentDeathLootPatch.LootedItems) ArmyArmory.AddItemToArmory(item);

					ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents);

					AgentDeathLootPatch.LootedItems.Clear();
					AgentDeathLootPatch.ProcessedAgents.Clear();
				}
				else if (missionInstance.MissionResult == null) { ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents); }
			}
		}
	}