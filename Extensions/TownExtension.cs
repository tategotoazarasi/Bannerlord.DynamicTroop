#region

using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

#endregion

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
		}

		if (town.IsUnderSiege) return list;

		var items2 =
			Cache.GetItemsByTierAndCulture(2 * (int)town.GetProsperityLevel() +
										   (ModSettings.Instance?.Difficulty.SelectedIndex ?? 0),
										   town.Culture);
		if (items2 != null && !items2.IsEmpty()) list.Add(items2.GetRandomElement());
		return list;
	}
}