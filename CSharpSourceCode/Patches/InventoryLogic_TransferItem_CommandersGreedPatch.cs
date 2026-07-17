using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace DynamicTroopEquipmentReupload.Patches;

// the player can only give stuff to the stash, cant take from. (commander's greed)
[HarmonyPatch(typeof(InventoryLogic), "TransferItem")]
internal static class InventoryLogic_TransferItem_CommandersGreedPatch {
	private static readonly AccessTools.FieldRef<InventoryLogic, ItemRoster[]> RostersRef =
		AccessTools.FieldRefAccess<InventoryLogic, ItemRoster[]>("_rosters");

	private static bool IsAllowedTroopPoolStashItem(ItemObject item) {
		return item.ItemType == ItemObject.ItemTypeEnum.Horse || item.ItemType == ItemObject.ItemTypeEnum.HorseHarness || item.HasWeaponComponent || item.HasArmorComponent;
	}

	private static bool Prefix(InventoryLogic __instance, ref TransferCommand transferCommand, ref List<TransferCommandResult> __result) {
		// exclude vanilla
		var rosters = RostersRef(__instance);
		if (rosters == null || rosters.Length <= (int)InventoryLogic.InventorySide.OtherInventory)
			return true;

		if (!ReferenceEquals(rosters[(int)InventoryLogic.InventorySide.OtherInventory], ArmyArmory.Armory))
			return true;

		// Troop pool stash (OtherInventory) can never take trade goods
		if (transferCommand.ToSide == InventoryLogic.InventorySide.OtherInventory) {
			var item = transferCommand.ElementToTransfer.EquipmentElement.Item;
			if (item != null && !IsAllowedTroopPoolStashItem(item)) {
				__result = new List<TransferCommandResult>();
				return false;
			}
		}


		if (ModSettings.Instance?.CommandersGreed ?? false)
			return true;

		if (transferCommand.FromSide == InventoryLogic.InventorySide.OtherInventory) {
			__result = new List<TransferCommandResult>();
			return false;
		}

		return true;
	}
}