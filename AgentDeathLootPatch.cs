#region

	using System.Collections.Generic;
	using HarmonyLib;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(MissionBehavior), "OnAgentRemoved")]
	public class AgentDeathLootPatch {
		private static readonly HashSet<Agent> ProcessedAgents = new();

		private static void Postfix(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow) {
			if ((agentState == AgentState.Killed || agentState == AgentState.Unconscious) &&
				!ProcessedAgents.Contains(affectedAgent)) {
				ProcessedAgents.Add(affectedAgent);
				var enemyEquipment = affectedAgent.SpawnEquipment;
				EquipmentIndex[] slots = {
											 EquipmentIndex.Weapon0,
											 EquipmentIndex.Weapon1,
											 EquipmentIndex.Weapon2,
											 EquipmentIndex.Weapon3,
											 EquipmentIndex.Head,
											 EquipmentIndex.Body,
											 EquipmentIndex.Leg,
											 EquipmentIndex.Gloves,
											 EquipmentIndex.Cape,
											 EquipmentIndex.Horse,
											 EquipmentIndex.HorseHarness
										 };

				// 遍历装备槽位并添加到玩家的物品栏中
				if (affectedAgent.IsHuman && affectedAgent.Team != null && !affectedAgent.Team.IsPlayerAlly)
					foreach (var slot in slots) {
						var element = enemyEquipment.GetEquipmentFromSlot(slot);
						if (element.Item != null && !element.IsEmpty) ArmyArmory.AddItemToArmory(element.Item); // 添加到军械库
					}
			}
		}

		private static void AddItemToPlayerInventory(ItemObject item) {
			// 获取玩家的物品栏
			var playerInventory = Campaign.Current.MainParty.ItemRoster;

			// 添加物品到玩家的物品栏
			playerInventory.AddToCounts(item, 1);

			// 显示提示信息
			InformationManager.DisplayMessage(new InformationMessage($"已添加物品到物品栏: {item.Name}", Colors.Green));
		}
	}