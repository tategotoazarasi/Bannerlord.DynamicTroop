using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Extensions;

[Obsolete]
public static class TroopRosterElementExtension {
	public static bool IsValidForRandom(this TroopRosterElement member, MobileParty party, bool equalTier) {
		if (member is { Character: { IsHero: false, BattleEquipments: not null } } &&
			!member.Character.BattleEquipments.IsEmpty())
			return equalTier
					   ? member.Character.Tier == party.GetClanTier()
					   : member.Character.Tier <= party.GetClanTier();

		return false;
	}
}