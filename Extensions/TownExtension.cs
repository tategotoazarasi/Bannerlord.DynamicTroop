﻿using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Extensions;

public static class TownExtension {
	public static List<ItemObject> GetRandomEquipments(this Town? town) {
		var list = new List<ItemObject>();
		if (town == null) return list;
		foreach (var village in town.Villages) {
			if (village.VillageState != Village.VillageStates.Normal) continue;
			var items = Cache.GetItemsByTierAndCulture(2 * (int)village.GetProsperityLevel() +
													   (ModSettings.Instance?.Difficulty.SelectedIndex ?? 0),
													   town.Culture);
			if (items != null) list.Add(items.GetRandomElement());
		}

		if (town.IsUnderSiege) return list;
		var items2 =
			Cache.GetItemsByTierAndCulture(2 * (int)town.GetProsperityLevel() +
										   (ModSettings.Instance?.Difficulty.SelectedIndex ?? 0),
										   town.Culture);
		if (items2 != null) list.Add(items2.GetRandomElement());
		return list;
	}
}