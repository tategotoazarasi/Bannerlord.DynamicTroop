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
	public static readonly HashSet<Agent> ProcessedAgents = new();

	public static HashSet<ItemObject> LootedItems = new();

	private static void Postfix(MissionBehavior __instance,
								Agent affectedAgent,
								Agent affectorAgent,
								AgentState agentState,
								KillingBlow blow) {
		// 确保被击倒的代理是敌方非英雄士兵
		if (__instance.Mission.CombatType == Mission.MissionCombatType.Combat &&
			__instance.Mission.PlayerTeam != null &&
			__instance.Mission.PlayerTeam.IsValid &&
			(agentState == AgentState.Killed || agentState == AgentState.Unconscious) &&
			!ProcessedAgents.Contains(affectedAgent) &&
			!affectedAgent.Character.IsHero &&
			((affectorAgent.Team.IsPlayerTeam &&
			  !(affectedAgent.Team.IsPlayerTeam || affectedAgent.Team.IsPlayerAlly)) ||
			 affectedAgent.Team.IsPlayerTeam)) {
			ProcessedAgents.Add(affectedAgent);
			Equipment enemyEquipment = affectedAgent.SpawnEquipment;
			foreach (EquipmentIndex slot in Global.EquipmentSlots) {
				EquipmentElement element = enemyEquipment.GetEquipmentFromSlot(slot);
				if (element.Item != null && !element.IsEmpty) {
					//if (element.Item != null && !element.IsEmpty && element.Item.ItemType != ItemObject.ItemTypeEnum.Horse && element.Item.ItemType != ItemObject.ItemTypeEnum.HorseHarness) {
					if (affectorAgent.Character.IsHero && affectorAgent.Team.IsPlayerTeam) {

						// 英雄士兵杀死或击晕敌人，装备进入玩家物品栏
						AddItemToPlayerInventory(element.Item);
					} else {

						// 非英雄士兵杀死或击晕敌人，装备进入军械库
						LootedItems.Add(element.Item);
					}

					//ArmyArmory.AddItemToArmory(element.Item);
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