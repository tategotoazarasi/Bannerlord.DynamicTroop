using System.Collections.Generic;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

public class PartyBattleRecord {
	private static readonly object                      LockObject     = new();
	public readonly         Dictionary<ItemObject, int> ItemsToRecover = new();

	public readonly Dictionary<ItemObject, int> LootedItems = new();

	public void AddItemToRecover(ItemObject item) { AddItemToDictionary(ItemsToRecover, item, 1); }

	public void AddItemToRecover(ItemObject item, int count) { AddItemToDictionary(ItemsToRecover, item, count); }

	public void AddLootedItem(ItemObject item) { AddItemToDictionary(LootedItems, item, 1); }

	public void AddLootedItem(ItemObject item, int count) { AddItemToDictionary(LootedItems, item, count); }

	private static void AddItemToDictionary(IDictionary<ItemObject, int> dictionary, ItemObject? item, int count) {
		if (item == null) return;

		lock (LockObject) {
			if (dictionary.TryGetValue(item, out var existingCount)) {
				dictionary[item] = existingCount + count;
			} else {
				dictionary[item] = count;
			}
		}
	}
}