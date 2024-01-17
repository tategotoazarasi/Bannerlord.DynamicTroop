using TaleWorlds.Localization;

namespace Bannerlord.DynamicTroop;

public static class LocalizedTexts {
	public static readonly TextObject SettingEasy = new("{=setting_easy}Easy");

	public static readonly TextObject SettingNormal = new("{=setting_normal}Normal");

	public static readonly TextObject SettingHard = new("{=setting_hard}Hard");

	public static readonly TextObject SettingVeryHard = new("{=setting_very_hard}Very Hard");

	public static readonly TextObject ModName = new("{=mod_name}Dynamic Troop Equipment System");

	public static readonly TextObject ArmorViewOption = new("{=armory_view_option}View Army Armory");

	public static readonly TextObject ArmoryManageOption = new("{=armory_manage_option}Manage Armory");

	public static readonly TextObject SellForThrowing =
		new("{=sell_for_throwing}Sell Excess Equipment for Throwing Weapons");

	private static readonly TextObject SoldExcessEquipmentForThrowingWeapons =
		new("{=sell_for_throwing_text}Sold excess equipment worth {VALUE} and obtained {COUNT} throwing weapons");

	public static readonly TextObject ReturnToTown = new("{=return_to_town}Return to Town Menu");

	public static string GetSoldExcessEquipmentForThrowingWeapons(int value, int count) {
		_ = SoldExcessEquipmentForThrowingWeapons.SetTextVariable("VALUE", value);
		_ = SoldExcessEquipmentForThrowingWeapons.SetTextVariable("COUNT", count);
		return SoldExcessEquipmentForThrowingWeapons.ToString();
	}
}