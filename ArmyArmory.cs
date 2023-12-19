#region

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using TaleWorlds.CampaignSystem.Roster;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public static class ArmyArmory {
		public static ItemRoster Armory = new();

		public static void AddItemToArmory(ItemObject item) {
			Armory.AddToCounts(item, 1);

			// 显示提示信息
			InformationManager.DisplayMessage(new InformationMessage($"已添加物品到部队军械库: {item.Name}", Colors.Green));
		}


		public static EquipmentElement FindBestItemForSlot(EquipmentIndex slot) {
			var suitableItems = Armory.Where(itemRosterElement => IsItemSuitableForSlot(itemRosterElement, slot))
									  .OrderByDescending(itemRosterElement => itemRosterElement.EquipmentElement.Item.Value)
									  .ToList();

			if (suitableItems.Any()) {
				var bestItem = suitableItems.First().EquipmentElement;

				// 从军械库中移除被选中的装备
				Armory.AddToCounts(bestItem.Item, -1);

				return bestItem;
			}

			return default;
		}

		private static bool IsItemSuitableForSlot(ItemRosterElement itemRosterElement, EquipmentIndex slot) {
			// 根据装备槽位来决定装备是否适合 这里需要根据slot和装备的类型来判断，以下是一个简化的例子
			switch (slot) {
				case EquipmentIndex.Weapon0:
				case EquipmentIndex.Weapon1:
				case EquipmentIndex.Weapon2:
				case EquipmentIndex.Weapon3:
					return itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon ||
						   itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
						   itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Thrown          ||
						   itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Shield          ||
						   itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Polearm;

				case EquipmentIndex.Head:
					return itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HeadArmor;

				case EquipmentIndex.Body:
					return itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.BodyArmor;

				case EquipmentIndex.Leg:
					return itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.LegArmor;

				case EquipmentIndex.Gloves:
					return itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HandArmor;

				case EquipmentIndex.Cape:
					return itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Cape;

				case EquipmentIndex.Horse:
					return itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Horse;

				case EquipmentIndex.HorseHarness:
					return itemRosterElement.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.HorseHarness;

				default: return false;
			}
		}

		public static void ReturnEquipmentToArmoryFromAgents(IEnumerable<Agent> agents) {
			EquipmentIndex[] slots = {
										 EquipmentIndex.Weapon0,
										 EquipmentIndex.Weapon1,
										 EquipmentIndex.Weapon2,
										 EquipmentIndex.Weapon3,
										 EquipmentIndex.Head,
										 EquipmentIndex.Body,
										 EquipmentIndex.Leg,
										 EquipmentIndex.Gloves,
										 EquipmentIndex.Cape,
										 EquipmentIndex.Horse,
										 EquipmentIndex.HorseHarness
									 };

			foreach (var agent in agents) {
				if (agent.IsHuman && agent.Team.IsPlayerAlly) {
					var agentEquipment = agent.SpawnEquipment;
					foreach (var slot in slots) {
						var equipmentElement = agentEquipment.GetEquipmentFromSlot(slot);
						if (equipmentElement.Item != null && !equipmentElement.IsEmpty) {
							Armory.AddToCounts(equipmentElement.Item, 1);
						}
					}
				}
			}
		}
}