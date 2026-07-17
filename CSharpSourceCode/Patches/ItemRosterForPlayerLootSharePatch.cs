using System;
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
	private static readonly Random _random = new();

	private static void Prefix(PartyBase winnerParty, ItemRoster gainedLoots) {
		if ((ModSettings.Instance?.UseVanillaLootingSystem ?? false) || winnerParty != PartyBase.MainParty)
			return;

		var playerEncounter = PlayerEncounter.Current;
		var mapEvent = MapEvent.PlayerMapEvent;
		if (mapEvent == null ||
			mapEvent.WinningSide != mapEvent.PlayerSide ||
			playerEncounter?.IsNavalEncounterFinishedWithDisengage == true ||
			playerEncounter?.ForceHideoutSendTroops == true)
			return;

		var sanitizedVanillaLoot = CreateSanitizedRoster(gainedLoots);
		var dynamicTroopLoot = new ItemRoster();
		var playerContribution = mapEvent.GetPlayerBattleContributionRate();
		if (float.IsNaN(playerContribution) || float.IsInfinity(playerContribution))
			playerContribution = 0f;

		playerContribution = MBMath.ClampFloat(
			playerContribution * (ModSettings.Instance?.DropRate ?? 1f),
			0f,
			1f);

		foreach (var defeatedParty in mapEvent.PartiesOnSide(mapEvent.DefeatedSide)) {
			var defeatedMobileParty = defeatedParty.Party.MobileParty;
			if (defeatedMobileParty == null)
				continue;

			var defeatedArmory = EveryoneCampaignBehavior.SanitizePartyArmory(defeatedMobileParty.Id);
			if (defeatedArmory == null)
				continue;

			foreach (var entry in defeatedArmory) {
				if (entry.Value <= 0 ||
					!ArmyArmory.TryResolveArmoryItem(entry.Key, out var item) ||
					!ItemBlackList.Test(item))
					continue;

				var expectedCount = entry.Value * playerContribution;
				var lootCount = (int)expectedCount;
				if (_random.NextDouble() < expectedCount - lootCount)
					lootCount++;
				lootCount = Math.Min(lootCount, entry.Value);
				if (lootCount > 0)
					dynamicTroopLoot.AddToCounts(item, lootCount);
			}
		}

		if (dynamicTroopLoot.IsEmpty()) {
			ReplaceRosterContents(gainedLoots, sanitizedVanillaLoot);
			return;
		}

		foreach (var rosterElement in sanitizedVanillaLoot) {
			var item = rosterElement.EquipmentElement.Item;
			if (item.IsTradeGood || item.IsBannerItem || item.ItemType == ItemObject.ItemTypeEnum.Animal)
				dynamicTroopLoot.AddToCounts(rosterElement.EquipmentElement, rosterElement.Amount);
		}

		ReplaceRosterContents(gainedLoots, dynamicTroopLoot);
	}

	private static ItemRoster CreateSanitizedRoster(ItemRoster sourceRoster) {
		var sanitizedRoster = new ItemRoster();
		foreach (var rosterElement in sourceRoster) {
			if (rosterElement.Amount <= 0 ||
				!ArmyArmory.TryNormalizeArmoryElement(rosterElement.EquipmentElement, out var equipmentElement))
				continue;

			sanitizedRoster.AddToCounts(equipmentElement, rosterElement.Amount);
		}

		return sanitizedRoster;
	}

	private static void ReplaceRosterContents(ItemRoster targetRoster, ItemRoster sourceRoster) {
		targetRoster.Clear();
		foreach (var rosterElement in sourceRoster)
			targetRoster.AddToCounts(rosterElement.EquipmentElement, rosterElement.Amount);
	}
}