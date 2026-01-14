using TaleWorlds.Localization;

namespace DynamicTroopEquipmentReupload;

public static class LocalizedTexts {
	public static readonly TextObject SettingEasy = new("{=setting_easy}Easy");

	public static readonly TextObject SettingNormal = new("{=setting_normal}Normal");

	public static readonly TextObject SettingHard = new("{=setting_hard}Hard");

	public static readonly TextObject SettingVeryHard = new("{=setting_very_hard}Very Hard");

	public static readonly TextObject ModName = new("{=mod_name}Dynamic Troop Equipment System");

	public static readonly TextObject ArmorViewOption = new("{=armory_view_option}View Army Armory");

	public static readonly TextObject ArmoryManageOption = new("{=armory_manage_option}Manage Armory");

	public static readonly TextObject SellForThrowing =
		new("{=sell_for_throwing}Trade Excess Equipment for Throwing Weapons and Arrows");

	private static readonly TextObject SoldExcessEquipmentForThrowingWeapons =
		new("{=sell_for_throwing_text}Sold excess equipment worth {VALUE} and obtained {THROWING_COUNT} throwing weapons and {AMMO_COUNT} ammunition");

	private static readonly TextObject LootAddedMessage =
		new("{=loot_added_message}looted {ITEM_COUNT} items from enemy");

	private static readonly TextObject ItemsRecoveredFromFallenMessage =
		new("{=items_recover_from_fallen_message}recover {ITEM_COUNT} from your fallen soldiers");

	public static readonly TextObject ReturnToTown = new("{=return_to_town}Return to Town Menu");

	public static readonly TextObject DebugClearInvalidItems = new("{=debug_clear_invalid_items}DEBUG: Clear invalid items");

	public static readonly TextObject DebugRemovePlayerCraftedItems = new("{=debug_remove_player_crafted_items}DEBUG: Remove player crafted items");

	public static readonly TextObject DebugRebuildArmory = new("{=debug_rebuild_armory}DEBUG: Rebuild player armory");

	public static readonly TextObject DebugExportArmory = new("{=debug_export_armory}DEBUG: Export player armory");

	public static readonly TextObject DebugImportArmory = new("{=debug_import_armory}DEBUG: Import player armory");

	public static readonly TextObject ReinforcementCaravan = new("{=reinforcement_caravan}Reinforcement Caravan");

	public static readonly TextObject ReinforcementCaravanTradeDisabled = new("{=reinforcement_caravan_trade_disabled}These caravans do not trade.");

	public static string GetSoldExcessEquipmentForThrowingWeapons(int value, int throwingCount, int ammoCount) {
		_ = SoldExcessEquipmentForThrowingWeapons.SetTextVariable("VALUE", value);
		_ = SoldExcessEquipmentForThrowingWeapons.SetTextVariable("THROWING_COUNT", throwingCount);
		_ = SoldExcessEquipmentForThrowingWeapons.SetTextVariable("AMMO_COUNT", ammoCount);
		return SoldExcessEquipmentForThrowingWeapons.ToString();
	}

	public static string GetLootAddedMessage(int count) {
		_ = LootAddedMessage.SetTextVariable("ITEM_COUNT", count);
		return LootAddedMessage.ToString();
	}

	public static string GetItemsRecoveredFromFallenMessage(int count) {
		_ = ItemsRecoveredFromFallenMessage.SetTextVariable("ITEM_COUNT", count);
		return ItemsRecoveredFromFallenMessage.ToString();
	}
}