using System.Collections.Generic;
using System.Linq;
using Bannerlord.DynamicTroop.Patches;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;

namespace Bannerlord.DynamicTroop.Extensions;

public static class MobilePartyExtension
{
    public static int GetClanTier(this MobileParty? party)
    {
        if (!party.IsValid()) return 0;

        var hero = party?.Owner ?? party?.LeaderHero;
        return hero != null ? hero.Clan?.Tier ?? 0 : 0;
    }

    public static bool IsValid(this MobileParty? party)
    {
        return party is {Id: { }} &&
               (EveryoneCampaignBehavior.PartyArmories.ContainsKey(party.Id) ||
                party is
                {
                    Owner : {CharacterObject.IsHero: true, IsPartyLeader: true},
                    LeaderHero : {CharacterObject.IsHero: true, IsPartyLeader: true},
                    MemberRoster: not null,
                    Owner : not {IsHumanPlayerCharacter: true}
                });
    }

    public static List<EquipmentElement> GetItems(this MobileParty? party)
    {
        List<EquipmentElement> listToReturn = new();
        if (party == null) return listToReturn;

        foreach (var element in party.MemberRoster.GetTroopRoster())
            if (element.Character is {IsHero: false})
            {
                var list = RecruitmentPatch.GetRecruitEquipments(element.Character);
                for (var i = 0; i < element.Number; i++) listToReturn.AddRange(list);
            }

        return listToReturn;
    }

    public static List<EquipmentElement> GetRandomEquipmentsFromTroop(this MobileParty? party)
    {
        List<EquipmentElement> list = new();
        if (party == null) return list;

        var batchSize = 50 - (ModSettings.Instance?.Difficulty.SelectedIndex ?? 0) * 10;
        var cnt = party.MemberRoster?.TotalManCount ?? 0;
        var rosterWithoutHeroes = party.MemberRoster?.GetTroopRoster()
            ?.Where(member => !member.Character?.IsHero ?? false)
            ?.ToArrayQ();
        if (rosterWithoutHeroes == null) return list;

        for (var i = 0; i <= cnt / batchSize; i++)
        {
            var equipmentElement = rosterWithoutHeroes.GetRandomElement()
                .Character?.RandomBattleEquipment
                ?.GetEquipmentFromSlot(Global.EquipmentSlots.GetRandomElement());
            if (equipmentElement is not {IsEmpty: false, Item: not null}) continue;
            list.Add(equipmentElement.Value);
        }

        return list;
    }

    public static List<ItemObject> GetRandomEquipmentsFromClan(this MobileParty? party)
    {
        List<ItemObject> list = new();
        if (party == null) return list;

        var clanTier = party.GetClanTier() + (ModSettings.Instance?.Difficulty.SelectedIndex ?? 0);
        for (var i = 0; i <= clanTier; i++)
        {
            var items = Cache.GetItemsByTierAndCulture(clanTier,
                party.Owner?.Clan?.Culture ?? party.LeaderHero?.Clan?.Culture);
            if (items == null || items.IsEmpty()) continue;

            list.Add(items.GetRandomElement());
        }

        return list;
    }

    public static List<ItemObject> GetDailyEquipmentFromFiefs(this MobileParty? party)
    {
        List<ItemObject> list = new();
        if (party == null) return list;

        MBReadOnlyList<Town>? fiefs = (party.LeaderHero?.Clan ?? party.Owner?.Clan)?.Fiefs;
        if (fiefs == null) return list;

        foreach (var town in fiefs)
            if (town.IsTown || town.IsCastle)
                list.AddRange(town.GetRandomEquipments());

        return list;
    }
}