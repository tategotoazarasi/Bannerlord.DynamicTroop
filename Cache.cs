﻿using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace Bannerlord.DynamicTroop;

public static class Cache {
	private static readonly Dictionary<(ItemObject.ItemTypeEnum, int, CultureObject?), ItemObject[]>
		CachedItems = new();

	public static ItemObject[]? GetItemsByTierAndCulture(ItemObject.ItemTypeEnum itemType,
														 int                     tier,
														 CultureObject?          culture) {
		var key = (itemType, tier, culture);

		if (!CachedItems.TryGetValue(key, out var items)) {
			// If not cached, generate and cache the list
			items = EveryoneCampaignBehavior.ItemListByType[itemType]
											.WhereQ(item => item           != null &&
															(int)item.Tier <= tier &&
															(item.Culture == null || item.Culture == culture))
											.ToArrayQ();

			CachedItems[key] = items;
		}

		return items;
	}
}