using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Extensions;

[Obsolete]
public static class TroopRosterElementExtension {
	public static bool IsValidForRandom(this TroopRosterElement member, MobileParty party, bool equalTier) {
		return member is { Character: { IsHero: false, BattleEquipments: not null } } &&
			   !member.Character.BattleEquipments.IsEmpty()                           &&
			   (equalTier
					? member.Character.Tier == party.GetClanTier()
					: member.Character.Tier <= party.GetClanTier());
	}
}