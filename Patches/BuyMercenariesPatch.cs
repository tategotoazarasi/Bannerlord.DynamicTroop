using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;

namespace DynamicTroopEquipmentReupload.Patches;

[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "buy_mercenaries_on_consequence")]
public static class TownMenuMercenaryEquipmentPatch {
	private static void Prefix(RecruitmentCampaignBehavior __instance, out PlayerMercenaryRosterSnapshot __state) {
		var currentSettlement = Campaign.Current.MainParty.CurrentSettlement;
		var character = currentSettlement?.IsTown == true
			? __instance.GetMercenaryData(currentSettlement.Town).TroopType
			: null;
		__state = new PlayerMercenaryRosterSnapshot(character);
	}

	private static void Postfix(PlayerMercenaryRosterSnapshot __state) {
		var recruitedCount = __state.GetRecruitedCount();
		if (recruitedCount > 0)
			RecruitmentPatch.AddStartingEquipmentToPlayerArmory(__state.Character!, recruitedCount);
	}

	internal readonly struct PlayerMercenaryRosterSnapshot {
		public PlayerMercenaryRosterSnapshot(CharacterObject? character) {
			Character = character;
			PreviousCount = character == null ? 0 : Campaign.Current.MainParty.MemberRoster.GetTroopCount(character);
		}

		public CharacterObject? Character { get; }
		private int PreviousCount { get; }

		public int GetRecruitedCount() {
			return Character == null
				? 0
				: Campaign.Current.MainParty.MemberRoster.GetTroopCount(Character) - PreviousCount;
		}
	}
}

[HarmonyPatch(typeof(RecruitmentCampaignBehavior), "BuyMercenaries")]
public static class TavernMercenaryEquipmentPatch {
	private static void Prefix(out TownMenuMercenaryEquipmentPatch.PlayerMercenaryRosterSnapshot __state) {
		__state = new TownMenuMercenaryEquipmentPatch.PlayerMercenaryRosterSnapshot(
			CharacterObject.OneToOneConversationCharacter);
	}

	private static void Postfix(TownMenuMercenaryEquipmentPatch.PlayerMercenaryRosterSnapshot __state) {
		var recruitedCount = __state.GetRecruitedCount();
		if (recruitedCount > 0)
			RecruitmentPatch.AddStartingEquipmentToPlayerArmory(__state.Character!, recruitedCount);
	}
}