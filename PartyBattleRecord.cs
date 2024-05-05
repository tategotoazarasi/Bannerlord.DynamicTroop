using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

public class PartyBattleRecord {
	public readonly ConcurrentDictionary<ItemObject, int> ItemsToRecover = new();
	
	public readonly ConcurrentDictionary<ItemObject, int> LootedItems = new();
	
	public int ItemsToRecoverCount => ItemsToRecover.Sum(kv => kv.Value);
	
	public int LootedItemsCount => LootedItems.Sum(kv => kv.Value);
	
	public void AddItemToRecover(ItemObject item) {
		AddItemToDictionary(ItemsToRecover, item, 1);
	}
	
	public void AddItemToRecover(ItemObject item, int count) { AddItemToDictionary(ItemsToRecover, item, count); }
	
	public void AddLootedItem(ItemObject item) { AddItemToDictionary(LootedItems, item, 1); }
	
	public void AddLootedItem(ItemObject item, int count) { AddItemToDictionary(LootedItems, item, count); }
	
	private static void AddItemToDictionary(IDictionary<ItemObject, int> dictionary, ItemObject? item, int count) {
		if (item == null || !ItemBlackList.Test(item)) return;
		
		dictionary[item] = dictionary.TryGetValue(item, out var existingCount) ? existingCount + count : count;
	}
}