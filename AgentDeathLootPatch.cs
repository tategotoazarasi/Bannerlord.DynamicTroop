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
		// 确保被击倒的代理是敌方非英雄士兵
		if ((agentState == AgentState.Killed || agentState == AgentState.Unconscious) &&
			!ProcessedAgents.Contains(affectedAgent) &&
			!affectedAgent.Character.IsHero &&
			affectorAgent.Team.IsPlayerTeam) {
			ProcessedAgents.Add(affectedAgent);
			Equipment enemyEquipment = affectedAgent.SpawnEquipment;
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

			foreach (EquipmentIndex slot in slots) {
				EquipmentElement element = enemyEquipment.GetEquipmentFromSlot(slot);
				if (element.Item != null && !element.IsEmpty) {
					if (affectorAgent.Character.IsHero) {
						// 英雄士兵杀死或击晕敌人，装备进入玩家物品栏
						AddItemToPlayerInventory(element.Item);
					} else {
						// 非英雄士兵杀死或击晕敌人，装备进入军械库
						ArmyArmory.AddItemToArmory(element.Item);
					}
				}
			}
		}
	}

	private static void AddItemToPlayerInventory(ItemObject item) {
		// 获取玩家的物品栏
		TaleWorlds.CampaignSystem.Roster.ItemRoster playerInventory = Campaign.Current.MainParty.ItemRoster;

		// 添加物品到玩家的物品栏
		playerInventory.AddToCounts(item, 1);

		// 显示提示信息
		InformationManager.DisplayMessage(new InformationMessage($"已添加物品到物品栏: {item.Name}", Colors.Green));
	}
}