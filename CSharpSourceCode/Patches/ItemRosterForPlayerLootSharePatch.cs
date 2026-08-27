using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace DynamicTroopEquipmentReupload.Patches;

// as of 1.4 finalized party player roster is built and used after MapEvent  
[HarmonyPatch(typeof(CampaignEventDispatcher), nameof(CampaignEventDispatcher.OnCollectLootItems))]
public static class ItemRosterForPlayerLootSharePatch {
	private static void Postfix(PartyBase winnerParty) {
		if (winnerParty != PartyBase.MainParty)
			return;

		var playerEncounter = PlayerEncounter.Current;
		var mapEvent = MapEvent.PlayerMapEvent;
		if (mapEvent == null ||
			mapEvent.IsPlayerSimulation ||
			mapEvent.WinningSide != mapEvent.PlayerSide ||
			playerEncounter?.IsNavalEncounterFinishedWithDisengage == true ||
			playerEncounter?.ForceHideoutSendTroops == true ||
			!ReferenceEquals(EveryoneCampaignBehavior.PendingPlayerCasualtyLootMapEvent, mapEvent))
			return;

		var addedLootCount = EveryoneCampaignBehavior.AddPlayerCasualtyLootToArmory(
			mapEvent,
			EveryoneCampaignBehavior.PendingPlayerCasualtyLoot);

		EveryoneCampaignBehavior.PendingPlayerCasualtyLoot.Clear();
		EveryoneCampaignBehavior.PendingPlayerCasualtyLootMapEvent = null;

		if (addedLootCount > 0)
			MessageDisplayService.EnqueueMessage(new InformationMessage(LocalizedTexts.GetLootAddedMessage(addedLootCount), Colors.Green));
	}
}

[HarmonyPatch(typeof(MapEvent), "LootDefeatedPartyCasualties")]
internal static class VanillaCasualtyLootPatch {
	[HarmonyPrefix]
	[HarmonyPriority(Priority.First)]
	private static void Prefix(
		MapEvent __instance,
		MBReadOnlyList<MapEventParty> winnerParties,
		out Dictionary<ItemObject, int>? __state) {
		__state = null;
		EveryoneCampaignBehavior.VanillaPlayerCasualtyLoot.Clear();
		EveryoneCampaignBehavior.VanillaPlayerCasualtyLootMapEvent = null;

		if ((ModSettings.Instance?.UseVanillaLootingSystem ?? false) ||
			!__instance.IsPlayerMapEvent ||
			__instance.WinningSide != __instance.PlayerSide)
			return;

		var playerParty = FindPlayerParty(winnerParties);
		if (playerParty == null)
			return;

		__state = CountLootItems(playerParty.RosterToReceiveLootItems);
	}

	[HarmonyPostfix]
	[HarmonyPriority(Priority.Last)]
	private static void Postfix(
		MapEvent __instance,
		MBReadOnlyList<MapEventParty> winnerParties,
		Dictionary<ItemObject, int>? __state) {
		if (__state == null)
			return;

		var playerParty = FindPlayerParty(winnerParties);
		if (playerParty == null)
			return;

		var currentLoot = CountLootItems(playerParty.RosterToReceiveLootItems);
		foreach (var entry in currentLoot) {
			var previousCount = __state.TryGetValue(entry.Key, out var count) ? count : 0;
			var gainedCount = entry.Value - previousCount;
			if (gainedCount > 0)
				EveryoneCampaignBehavior.VanillaPlayerCasualtyLoot[entry.Key] = gainedCount;
		}

		EveryoneCampaignBehavior.VanillaPlayerCasualtyLootMapEvent = __instance;
	}

	private static MapEventParty? FindPlayerParty(MBReadOnlyList<MapEventParty> parties) {
		foreach (var party in parties) {
			if (party.Party == PartyBase.MainParty)
				return party;
		}

		return null;
	}

	private static Dictionary<ItemObject, int> CountLootItems(ItemRoster roster) {
		Dictionary<ItemObject, int> itemCounts = new();
		foreach (var rosterElement in roster) {
			if (rosterElement.Amount <= 0 ||
				!ArmyArmory.TryResolveArmoryItem(rosterElement.EquipmentElement.Item, out var item))
				continue;

			itemCounts[item] = itemCounts.TryGetValue(item, out var currentCount)
				? currentCount + rosterElement.Amount
				: rosterElement.Amount;
		}

		return itemCounts;
	}
}