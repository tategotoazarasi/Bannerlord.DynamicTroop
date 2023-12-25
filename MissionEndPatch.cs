#region

	using System.Collections.Generic;
	using System.Linq;
	using HarmonyLib;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.Localization;
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
													  .Where(agent => agent           != null          &&
																	  agent.Formation != null          &&
																	  agent.Team      != null          &&
																	  agent.Origin    != null          &&
																	  agent.IsHuman                    &&
																	  agent.Team.IsValid               &&
																	  agent.Team.IsPlayerTeam          &&
																	  agent.State == AgentState.Active &&
																	  !agent.IsHero                    &&
																	  agent.Origin.IsUnderPlayersCommand)
													  .ToList();

				if (missionInstance.MissionResult != null        &&
					missionInstance.MissionResult.BattleResolved &&
					missionInstance.MissionResult.PlayerVictory) {
					var        lootCount   = MyMissionBehavior.LootedItems.Count;
					TextObject messageText = new("{=loot_added_message}Added {ITEM_COUNT} items to the army armory.");
					_ = messageText.SetTextVariable("ITEM_COUNT", lootCount);
					InformationManager.DisplayMessage(new InformationMessage(messageText.ToString(), Colors.Green));
					foreach (var item in MyMissionBehavior.LootedItems) ArmyArmory.AddItemToArmory(item);

					ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents);
				}
				else if (missionInstance.MissionResult == null || !missionInstance.MissionResult.BattleResolved) {
					ArmyArmory.ReturnEquipmentToArmoryFromAgents(myAgents);
				}
			}

			MyMissionBehavior.Clear();
		}
	}