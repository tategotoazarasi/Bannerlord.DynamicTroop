#region

using System;
using System.Linq;
using HarmonyLib;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

#endregion

namespace DynamicTroopEquipmentReupload.Patches;

[HarmonyPatch(typeof(MapEventSide), "ItemRosterForPlayerLootShare")]
public static class ItemRosterForPlayerLootSharePatch {
	private static readonly Random random = new();

	public static void Postfix(MapEventSide __instance, PartyBase playerParty, ref ItemRoster __result) {
		Global.Debug("ItemRosterForPlayerLootSharePatch");
		if ((ModSettings.Instance?.UseVanillaLootingSystem ?? false) || playerParty != PartyBase.MainParty || !__instance.MapEvent.IsPlayerMapEvent || playerParty.Side != __instance.MapEvent.WinningSide) return;

		Global.Debug("Postfix fired");
		ItemRoster                    replaceRoster      = new();
		var playerContribution =
			__instance.GetPlayerPartyContributionRate() * (ModSettings.Instance?.DropRate ?? 1f);
		playerContribution = MBMath.ClampFloat(playerContribution, 0f, 1f);
		MBReadOnlyList<MapEventParty> defeatedParties    = __instance.MapEvent.PartiesOnSide(__instance.MapEvent.DefeatedSide);
		if (defeatedParties == null) return;

		foreach (var defeatedParty in defeatedParties) {
			var mobilePartyId = defeatedParty?.Party?.MobileParty?.Id;
			if (mobilePartyId is null || !EveryoneCampaignBehavior.PartyArmories.ContainsKey(mobilePartyId.Value)) continue;

			foreach (var entry in EveryoneCampaignBehavior.PartyArmories[mobilePartyId.Value]) {
				if (!ItemBlackList.Test(entry.Key)) continue;
				var expectedCount = entry.Value * playerContribution;

				var lootCount = (int)expectedCount;
				var fractional = expectedCount - lootCount;

				if (fractional > 0f && random.NextDouble() < fractional)
					lootCount++;
				lootCount = Math.Min(lootCount, entry.Value);
				if (lootCount > 0)
					replaceRoster.AddToCounts(entry.Key, lootCount);

			}
		}

		if (replaceRoster.IsEmpty()) return;

		var originalRoster = __result;

		replaceRoster.Add(originalRoster.Where(static element => element is { IsEmpty: false, Amount: > 0, EquipmentElement.Item: not null } &&
																 (element.EquipmentElement.Item.IsTradeGood || element.EquipmentElement.Item.IsBannerItem || element.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Animal)));

		__result = replaceRoster;
	}
}