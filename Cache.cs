#region

using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

#endregion

namespace DynamicTroopEquipmentReupload;

public static class Cache {
	private static readonly Dictionary<(ItemObject.ItemTypeEnum, int, BasicCultureObject?), ItemObject[]> CachedItemsByType =
		new();

	private static readonly Dictionary<(int, BasicCultureObject?), ItemObject[]> CachedItems = new();
	private static readonly object CacheLock = new();

	public static ItemObject[]? GetItemsByTierAndCulture(int tier, BasicCultureObject? culture) {
		var key = (tier, culture);

		lock (CacheLock) {
			if (CachedItems.TryGetValue(key, out var cachedItems))
				return cachedItems;
		}

		var items = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
								   .WhereQ(item => item           != null   &&
												   (int)item.Tier <= tier   &&
												   ItemBlackList.Test(item) &&
												   (item.ItemType == ItemObject.ItemTypeEnum.Horse        ||
													item.ItemType == ItemObject.ItemTypeEnum.HorseHarness ||
													(ModSettings.Instance?.RemoveCivilianEquipmentsInRandom ?? false
														 ? !item.IsCivilian
														 : !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale))) &&
												   Global.ItemTypes.ContainsQ(item.ItemType)                       &&
												   !item.IsCraftedByPlayer                                         &&
												   (!item.HasWeaponComponent ||
													!Global.InvalidWeaponClasses.ContainsQ(item.PrimaryWeapon.WeaponClass)) &&
												   (item.Culture == null || item.Culture == culture))
								   .ToArrayQ();

		lock (CacheLock) { CachedItems[key] = items; }

		return items;
	}

	public static ItemObject[]? GetItemsByTypeTierAndCulture(ItemObject.ItemTypeEnum itemType,
															 int                     tier,
															 BasicCultureObject?     culture) {
		if (tier < 0) return null;

		var key = (itemType, tier, culture);

		lock (CacheLock) {
			if (CachedItemsByType.TryGetValue(key, out var cachedItems))
				return cachedItems;
		}

		if (!EveryoneCampaignBehavior.ItemListByType.TryGetValue(itemType, out var pool))
			return null;

		var items = pool
					.WhereQ(item => item           != null   &&
									(int)item.Tier == tier   &&
									ItemBlackList.Test(item) &&
									!item.IsCraftedByPlayer  &&
									(item.ItemType == ItemObject.ItemTypeEnum.Horse        ||
									 item.ItemType == ItemObject.ItemTypeEnum.HorseHarness ||
									 (ModSettings.Instance?.RemoveCivilianEquipmentsInRandom ?? false
										  ? !item.IsCivilian
										  : !item.ItemFlags.HasAnyFlag(ItemFlags.NotUsableByMale))) &&
									(!item.HasWeaponComponent ||
									 !Global.InvalidWeaponClasses.ContainsQ(item.PrimaryWeapon.WeaponClass)) &&
									(item.Culture == null || item.Culture == culture))
					.ToArrayQ();

		if (items.Length == 0)
			items = GetItemsByTypeTierAndCulture(itemType, tier - 1, culture) ?? Array.Empty<ItemObject>();

		lock (CacheLock) { CachedItemsByType[key] = items; }

		return items.Length == 0 ? null : items;
	}
}