using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Patches;

[HarmonyPatch(typeof(MapEventSide), "ItemRosterForPlayerLootShare")]
public static class ItemRosterForPlayerLootSharePatch {
	private static readonly Random random = new();

	public static bool Prefix(MapEventSide __instance, PartyBase playerParty, ref ItemRoster __result) {
		Global.Debug("ItemRosterForPlayerLootSharePatch");
		if (playerParty != PartyBase.MainParty    ||
			!__instance.MapEvent.IsPlayerMapEvent ||
			playerParty.Side != __instance.MapEvent.WinningSide)
			return true;
		Global.Debug("Prefix fired");
		var replaceRoster      = new ItemRoster();
		var playerContribution = __instance.GetPlayerPartyContributionRate();
		var defeatedParties    = __instance.MapEvent.PartiesOnSide(__instance.MapEvent.DefeatedSide);
		if (defeatedParties == null) return true;

		foreach (var defeatedParty in defeatedParties) {
			var mobilePartyId = defeatedParty?.Party?.MobileParty?.Id;
			if (mobilePartyId is null || !EveryoneCampaignBehavior.PartyArmories.ContainsKey(mobilePartyId.Value))
				continue;
			foreach (var entry in EveryoneCampaignBehavior.PartyArmories[mobilePartyId.Value])
				if (entry.Value * playerContribution < 1) {
					for (var i = 0; i < entry.Value; i++)
						if (random.NextDouble() >= playerContribution)
							replaceRoster.AddToCounts(entry.Key, 1);
				}
				else { replaceRoster.AddToCounts(entry.Key, (int)Math.Round(entry.Value * playerContribution)); }
		}

		if (replaceRoster.IsEmpty()) return true;
		__result = replaceRoster;
		return false;
	}
}