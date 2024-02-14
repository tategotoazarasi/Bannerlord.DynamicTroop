using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.DynamicTroop.Patches;

[HarmonyPatch(typeof(MapEventSide), "ItemRosterForPlayerLootShare")]
public static class ItemRosterForPlayerLootSharePatch {
	private static readonly Random random = new();

	public static void Postfix(MapEventSide __instance, PartyBase playerParty, ref ItemRoster __result) {
		Global.Debug("ItemRosterForPlayerLootSharePatch");
		if ((ModSettings.Instance?.UseVanillaLootingSystem ?? false) ||
			playerParty != PartyBase.MainParty                       ||
			!__instance.MapEvent.IsPlayerMapEvent                    ||
			playerParty.Side != __instance.MapEvent.WinningSide)
			return;

		Global.Debug("Postfix fired");
		ItemRoster replaceRoster = new();
		var playerContribution = __instance.GetPlayerPartyContributionRate() * (ModSettings.Instance?.DropRate ?? 1);
		MBReadOnlyList<MapEventParty> defeatedParties =
			__instance.MapEvent.PartiesOnSide(__instance.MapEvent.DefeatedSide);
		if (defeatedParties == null) return;

		foreach (var defeatedParty in defeatedParties) {
			var mobilePartyId = defeatedParty?.Party?.MobileParty?.Id;
			if (mobilePartyId is null || !EveryoneCampaignBehavior.PartyArmories.ContainsKey(mobilePartyId.Value))
				continue;

			foreach (var entry in EveryoneCampaignBehavior.PartyArmories[mobilePartyId.Value])
				if (entry.Value * playerContribution < 1) {
					for (var i = 0; i < entry.Value; i++)
						if (random.NextDouble() <= playerContribution)
							_ = replaceRoster.AddToCounts(entry.Key, 1);
				}
				else { _ = replaceRoster.AddToCounts(entry.Key, (int)Math.Round(entry.Value * playerContribution)); }
		}

		if (replaceRoster.IsEmpty()) return;

		foreach (var a in __result)
			if (a is { IsEmpty: false, Amount: > 0, EquipmentElement.Item: not null } &&
				(a.EquipmentElement.Item.IsTradeGood  ||
				 a.EquipmentElement.Item.IsBannerItem ||
				 a.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Animal))
				replaceRoster.Add(a);
		__result = replaceRoster;
	}
}