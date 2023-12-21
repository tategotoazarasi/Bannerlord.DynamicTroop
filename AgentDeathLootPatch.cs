#region

using System.Collections.Generic;

using HarmonyLib;

using SandBox.Missions.MissionLogics;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

#endregion

namespace Bannerlord.DynamicTroop;

[HarmonyPatch(typeof(CasualtyHandler), "OnAgentRemoved")]
public class AgentDeathLootPatch {
	public static readonly HashSet<Agent> ProcessedAgents = new();

	public static List<ItemObject> LootedItems = new();

	private static void Prefix(MissionBehavior __instance,
							   Agent           affectedAgent,
							   Agent           affectorAgent,
							   AgentState      agentState,
							   KillingBlow     killingBlow) {
		// 确保被击倒的agent是敌方非英雄士兵
		if (__instance.Mission.CombatType == Mission.MissionCombatType.Combat &&
			affectedAgent.Formation!=null && affectorAgent.Formation!=null &&
			affectedAgent.IsHuman &&
			affectorAgent.IsHuman &&
			affectorAgent.Team != null &&
			affectorAgent.Team.IsValid &&
			affectedAgent.Team != null &&
			affectedAgent.Team.IsValid &&
			affectorAgent.Origin != null &&
			affectedAgent.Origin != null &&
			affectorAgent.Character != null &&
			affectedAgent.Character != null &&
			__instance.Mission != null &&
			__instance.Mission.PlayerTeam != null &&
			__instance.Mission.PlayerTeam.IsValid &&
			(agentState == AgentState.Killed || agentState == AgentState.Unconscious) &&
			!ProcessedAgents.Contains(affectedAgent) &&
			!affectedAgent.Character.IsHero &&
			!(affectedAgent.Team.IsPlayerTeam || affectedAgent.Team.IsPlayerAlly) &&
			affectorAgent.Origin.IsUnderPlayersCommand) {
			ProcessedAgents.Add(affectedAgent);
			Equipment enemyEquipment = affectedAgent.SpawnEquipment;
			if (enemyEquipment == null || !enemyEquipment.IsValid) {
				return;
			}

			foreach (EquipmentIndex slot in Global.EquipmentSlots) {
				EquipmentElement element = enemyEquipment.GetEquipmentFromSlot(slot);
				if (!element.IsEmpty && element.Item != null) {

					// 非英雄士兵杀死或击晕敌人，装备进入军械库
					LootedItems.Add(element.Item);
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