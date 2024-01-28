using System;
using System.Collections.Generic;
using Bannerlord.DynamicTroop.Patches;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace Bannerlord.DynamicTroop.Extensions;

public static class MobilePartyExtension {
	public static int GetClanTier(this MobileParty? party) {
		if (!party.IsValid()) return 0;

		var hero = party?.Owner ?? party?.LeaderHero;
		return hero != null ? hero.Clan?.Tier ?? 0 : 0;
	}

	public static bool IsValid(this MobileParty? party) {
		return party is { Id: { } } &&
			   (EveryoneCampaignBehavior.PartyArmories.ContainsKey(party.Id) ||
				party is {
							 Owner       : { CharacterObject.IsHero: true, IsPartyLeader: true },
							 LeaderHero  : { CharacterObject.IsHero: true, IsPartyLeader: true },
							 MemberRoster: not null,
							 Owner       : not { IsHumanPlayerCharacter: true }
						 });
	}

	public static List<EquipmentElement> GetItems(this MobileParty? party) {
		List<EquipmentElement> listToReturn = new();
		if (party == null) return listToReturn;

		foreach (var element in party.MemberRoster.GetTroopRoster())
			if (element.Character is { IsHero: false }) {
				var list = RecruitmentPatch.GetRecruitEquipments(element.Character);
				for (var i = 0; i < element.Number; i++) listToReturn.AddRange(list);
			}

		return listToReturn;
	}

	public static int CalculateClanProsperityFactor(this MobileParty? mobileParty) {
		if (!mobileParty.IsValid() || mobileParty?.LeaderHero?.Clan == null) return 0;

		var clan = mobileParty.LeaderHero.Clan;

		// 计算领地繁荣度总和
		var prosperitySum = clan.Fiefs?.SumQ(fief => (int)(fief?.GetProsperityLevel() + 1 ?? 0)) ?? 0;

		// 计算因子：氏族等级 + 繁荣度加权
		var factor = (clan.Tier + 1) * Math.Max(1, prosperitySum);

		return factor * (SubModule.Settings?.Difficulty.SelectedIndex ?? 0 + 1);
	}
}