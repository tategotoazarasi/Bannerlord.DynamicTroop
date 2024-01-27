using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;

namespace Bannerlord.DynamicTroop;

public static class Cache {
	private static readonly Dictionary<(ItemObject.ItemTypeEnum, int, CultureObject?), ItemObject[]>
		CachedItems = new();

	/// <summary>
	///     根据物品类型、层级和文化获取物品数组。
	/// </summary>
	/// <param name="itemType"> 物品的类型。 </param>
	/// <param name="tier">     物品的层级。 </param>
	/// <param name="culture">  物品的文化属性。如果为null，则不限制文化。 </param>
	/// <returns> 符合条件的物品数组。如果没有符合条件的物品，返回null。 </returns>
	public static ItemObject[]? GetItemsByTierAndCulture(ItemObject.ItemTypeEnum itemType,
														 int                     tier,
														 CultureObject?          culture) {
		var key = (itemType, tier, culture);

		if (!CachedItems.TryGetValue(key, out var items)) {
			// If not cached, generate and cache the list
			items = EveryoneCampaignBehavior.ItemListByType[itemType]
											.WhereQ(item => item           != null &&
															(int)item.Tier <= tier &&
															(item.Culture == null || item.Culture == culture) &&
															!item.IsCivilian)
											.ToArrayQ();

			CachedItems[key] = items;
		}

		return items;
	}
}