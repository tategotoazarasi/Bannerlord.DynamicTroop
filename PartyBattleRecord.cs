#region

	using System.Collections.Generic;
	using TaleWorlds.Core;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class PartyBattleRecord {
		public Dictionary<ItemObject, int> ItemsToRecover = new();

		public Dictionary<ItemObject, int> LootedItems = new();

		public void AddItemToRecover(ItemObject item) { AddItemToDictionary(ItemsToRecover, item, 1); }

		public void AddItemToRecover(ItemObject item, int count) { AddItemToDictionary(ItemsToRecover, item, count); }

		public void AddLootedItem(ItemObject item) { AddItemToDictionary(LootedItems, item, 1); }

		public void AddLootedItem(ItemObject item, int count) { AddItemToDictionary(LootedItems, item, count); }

		private void AddItemToDictionary(Dictionary<ItemObject, int> dictionary, ItemObject item, int count) {
			if (item != null)
				dictionary[item] = dictionary.TryGetValue(item, out var existingCount) ? existingCount + count : count;
		}
	}