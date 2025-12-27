using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace DynamicTroopEquipmentReupload;

[HarmonyPatch]
internal static class ReinforcementCaravanConversationPatches {
	private const bool DISABLE_TRADE_BLOCK_FOR_TEST = false; // for testomg

	// no trading for reinforcement 
	[HarmonyPatch(typeof(CaravansCampaignBehavior), "caravan_buy_products_on_condition")]
	[HarmonyPrefix]
	private static bool CaravanBuyProductsOnConditionPrefix(ref bool __result) {
		if (DISABLE_TRADE_BLOCK_FOR_TEST) { return true; }

		if (CutTheirSupplyBehavior.IsReinforcementCaravanParty(MobileParty.ConversationParty)) {
			__result = false;
			return false;
		}

		return true;
	}

	[HarmonyPatch(typeof(CaravansCampaignBehavior), "AddDialogs")]
	[HarmonyPostfix]
	private static void AddDialogsPostfix(CampaignGameStarter starter) {
		starter.AddPlayerLine(
			"caravan_buy_products",
			"caravan_talk",
			"caravan_player_trade",
			"{=t0UGXPV4}I'm interested in trading. What kind of products do you have?",
			ReinforcementTradeLineOnCondition,
			null,
			100,
			ReinforcementTradeLineOnClickableCondition);
	}

	private static bool ReinforcementTradeLineOnCondition() {
		if (DISABLE_TRADE_BLOCK_FOR_TEST) { return false; }

		return CutTheirSupplyBehavior.IsReinforcementCaravanParty(MobileParty.ConversationParty);
	}

	private static bool ReinforcementTradeLineOnClickableCondition(out TextObject explanation) {
		explanation = new TextObject("{=dt_reinforcement_caravan_trade_disabled_expl}These caravans do not trade.");
		return false;
	}

	[HarmonyPatch(typeof(CaravansCampaignBehavior), "IsSurrenderFeasible")]
	[HarmonyPrefix]
	private static bool IsSurrenderFeasiblePrefix(ref bool __result) {
		if (CutTheirSupplyBehavior.IsReinforcementCaravanParty(MobileParty.ConversationParty)) {
			__result = false;
			return false;
		}

		return true;
	}
}