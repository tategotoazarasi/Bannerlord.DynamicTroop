using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Patches;

[HarmonyPatch(typeof(CharacterObject), "get_UpgradeRequiresItemFromCategory")]
public class GetUpgradeRequiresItemFromCategoryPatch {
	public static bool Prefix(CharacterObject __instance, ref ItemCategory? __result) {
		// 设置自定义返回值
		__result = null;

		// 返回 false 以阻止原始方法执行
		return false;
	}
}