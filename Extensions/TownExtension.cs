using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Extensions;

public static class TownExtension {
	public static List<ItemObject> GetRandomEquipments(this Town? town) {
		List<ItemObject> list = new();
		if (town == null) return list;

		foreach (var village in town.Villages) {
			if (village.VillageState != Village.VillageStates.Normal) continue;

			var items = Cache.GetItemsByTierAndCulture(2 * (int)village.GetProsperityLevel() +
													   (ModSettings.Instance?.Difficulty.SelectedIndex ?? 0),
													   town.Culture);
			if (items != null && !items.IsEmpty()) list.Add(items.GetRandomElement());
			if (ModSettings.Instance?.AiCrafting ?? false) {
				var randomItem = Crafting.CreateRandomCraftedItem(town.Culture);
				if (randomItem != null) { 
					CampaignEventDispatcher.Instance.OnNewItemCrafted(randomItem, null, false);
					list.Add(randomItem);
				}
			}
		}

		if (town.IsUnderSiege) return list;

		var items2 =
			Cache.GetItemsByTierAndCulture(2 * (int)town.GetProsperityLevel() +
										   (ModSettings.Instance?.Difficulty.SelectedIndex ?? 0),
										   town.Culture);
		if (items2 != null && !items2.IsEmpty()) list.Add(items2.GetRandomElement());
		if (ModSettings.Instance?.AiCrafting ?? false) {
			var randomItem2 = Crafting.CreateRandomCraftedItem(town.Culture);
			if (randomItem2 != null) {
				CampaignEventDispatcher.Instance.OnNewItemCrafted(randomItem2, null, false);
				list.Add(randomItem2);
			}
		}
		return list;
	}
}