#region

	using HarmonyLib;
	using TaleWorlds.CampaignSystem;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(Hero), "ChangeHeroGold")]
	public class GoldChangePatch {
		private static void Postfix(Hero __instance) {
			/*if (__instance == Hero.MainHero) {
				__instance.Gold += 1000;
				int remainingGold = __instance.Gold;
				InformationManager.DisplayMessage(new InformationMessage($"您还剩余 {remainingGold} 金币。", Colors.Yellow));
			}*/
		}
	}