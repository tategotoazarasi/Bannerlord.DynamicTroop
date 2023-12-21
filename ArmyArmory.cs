#region

using System.Collections.Generic;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

using static TaleWorlds.Core.ItemObject;

#endregion

namespace Bannerlord.DynamicTroop;

public static class ArmyArmory {
	public static ItemRoster Armory = new();

	public static void AddItemToArmory(ItemObject item) {
		Armory.AddToCounts(item, 1);

		// 显示提示信息
		//InformationManager.DisplayMessage(new InformationMessage($"已添加物品到部队军械库: {item.Name}", Colors.Green));
	}

	public static EquipmentElement FindBestItemForSlot(EquipmentIndex slot, WeaponClass? weaponClass, bool isMounted) {
		List<ItemRosterElement> suitableItems = Armory
								.Where(itemRosterElement =>
										   IsItemSuitableForSlot(itemRosterElement, weaponClass, isMounted, slot))
								.OrderByDescending(itemRosterElement => itemRosterElement.EquipmentElement.Item.Tier)
								.ThenByDescending(itemRosterElement => itemRosterElement.EquipmentElement.Item.Value)
								.ToList();

		if (suitableItems.Any()) {
			EquipmentElement bestItem = suitableItems.First().EquipmentElement;

			// 从军械库中移除被选中的装备
			Armory.AddToCounts(bestItem.Item, -1);

			return bestItem;
		}

		return default;
	}

	private static bool IsItemSuitableForSlot(ItemRosterElement itemRosterElement,
											  WeaponClass? weaponClass,
											  bool isMounted,
											  EquipmentIndex slot) {
		ItemObject item = itemRosterElement.EquipmentElement.Item;

		switch (slot) {
			case EquipmentIndex.Weapon0:
			case EquipmentIndex.Weapon1:
			case EquipmentIndex.Weapon2:
			case EquipmentIndex.Weapon3:
				if (item.HasWeaponComponent) {
					ItemUsageSetFlags weaponUsageSetFlags = MBItem.GetItemUsageSetFlags(item.WeaponComponent.PrimaryWeapon.ItemUsage);
					WeaponClass wc = item.WeaponComponent.PrimaryWeapon.WeaponClass;

					// 检查是否适合骑马使用
					bool suitableForMount = !isMounted ||
											   !weaponUsageSetFlags.HasFlag(ItemUsageSetFlags.RequiresNoMount);

					return wc == weaponClass && suitableForMount;
				}

				return false;

			case EquipmentIndex.Head: return item.ItemType == ItemTypeEnum.HeadArmor;

			case EquipmentIndex.Body: return item.ItemType == ItemTypeEnum.BodyArmor;

			case EquipmentIndex.Leg: return item.ItemType == ItemTypeEnum.LegArmor;

			case EquipmentIndex.Gloves: return item.ItemType == ItemTypeEnum.HandArmor;

			case EquipmentIndex.Cape: return item.ItemType == ItemTypeEnum.Cape;

			case EquipmentIndex.Horse: return isMounted && item.ItemType == ItemTypeEnum.Horse;

			case EquipmentIndex.HorseHarness: return isMounted && item.ItemType == ItemTypeEnum.HorseHarness;

			default: return false;
		}
	}

	public static void ReturnEquipmentToArmoryFromAgents(IEnumerable<Agent> agents) {
		foreach (Agent agent in agents) {
			if (agent.IsHuman && agent.Team.IsPlayerAlly) {
				Equipment agentEquipment = agent.SpawnEquipment; // 获取当前装备，而不是初始装备
				foreach (EquipmentIndex slot in Global.EquipmentSlots) {
					EquipmentElement equipmentElement = agentEquipment.GetEquipmentFromSlot(slot);
					if (equipmentElement.Item != null && !equipmentElement.IsEmpty) {
						Armory.AddToCounts(equipmentElement.Item, 1);
					}
				}

				// 特别处理战马
				/*if (agent.HasMount) {
					var horseItem = agent.MountAgent.Monster.Item; // 获取当前骑乘的战马
					if (horseItem != null) {
						Armory.AddToCounts(horseItem, 1);
					}
				}*/
			}
		}
	}

	public static void AddSoldierEquipmentToArmory(CharacterObject character) {
		if (character.Equipment != null) {
			foreach (Equipment? eq in character.BattleEquipments) {
				foreach (EquipmentIndex slot in Global.EquipmentSlots) {
					EquipmentElement equipmentElement = eq.GetEquipmentFromSlot(slot);
					if (equipmentElement.Item != null && !equipmentElement.IsEmpty) {

						// 添加装备到军火库
						AddItemToArmory(equipmentElement.Item);
					}
				}
			}
		}
	}

	public static EquipmentElement FindBestItemForType(ItemTypeEnum itemType, bool isMounted) {
		// 筛选出符合条件的装备
		List<EquipmentElement> suitableItems = Armory
								.Where(itemRosterElement => IsItemSuitableForType(itemRosterElement, itemType, isMounted))
								.OrderByDescending(itemRosterElement => itemRosterElement.EquipmentElement.Item.Tier)
								.ThenByDescending(itemRosterElement => itemRosterElement.EquipmentElement.Item.Value)
								.Select(itemRosterElement => itemRosterElement.EquipmentElement)
								.ToList();

		if (suitableItems.Any()) {
			return suitableItems.First();
		}

		return default;
	}

	private static bool IsItemSuitableForType(ItemRosterElement itemRosterElement,
											  ItemTypeEnum itemType,
											  bool isMounted) {
		// 检查物品类型是否匹配
		if (itemRosterElement.EquipmentElement.Item.ItemType != itemType) {
			return false;
		}

		// 如果骑马，检查物品是否适合骑马使用
		if (isMounted && !IsSuitableForMount(itemRosterElement.EquipmentElement.Item)) {
			return false;
		}

		return true;
	}

	private static bool IsSuitableForMount(ItemObject item) {
		// 检查物品是否适合骑马使用的逻辑 这可能需要根据游戏的具体实现来调整
		ItemUsageSetFlags weaponUsageSetFlags = MBItem.GetItemUsageSetFlags(item.WeaponComponent.PrimaryWeapon.ItemUsage);
		return !weaponUsageSetFlags.HasFlag(ItemUsageSetFlags.RequiresNoMount);
	}

	public static Equipment CreateEmptyEquipment() {
		Equipment emptyEquipment = new Equipment();
		foreach (EquipmentIndex slot in Global.EquipmentSlots) {
			emptyEquipment.AddEquipmentToSlotWithoutAgent(slot, new EquipmentElement());
		}

		return emptyEquipment;
	}

	public static void AssignEquipmentFromArmory(Equipment equipment,
												 WeaponClass? weaponClass0,
												 WeaponClass? weaponClass1,
												 WeaponClass? weaponClass2,
												 WeaponClass? weaponClass3,
												 bool isMounted) {
		bool hasWeapon = false;
		foreach (EquipmentIndex slot in Global.EquipmentSlots) {
			WeaponClass? weaponClass = DetermineWeaponClassForSlot(slot, weaponClass0, weaponClass1, weaponClass2, weaponClass3);
			EquipmentElement bestItem = FindBestItemForSlot(slot, weaponClass, isMounted);

			if (bestItem.Item != null && Global.IsWeapon(bestItem.Item)) {
				hasWeapon = true;
			}

			equipment.AddEquipmentToSlotWithoutAgent(slot, bestItem);
		}

		// 检查士兵是否有武器，如果没有，则分配一把近战武器
		if (!hasWeapon) {
			EquipmentElement meleeWeapon = FindBestMeleeWeapon(isMounted);
			if (meleeWeapon.Item != null) {
				equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Weapon0, meleeWeapon);
			}
		}
	}

	public static EquipmentElement FindBestMeleeWeapon(bool isMounted) {
		// 此处的逻辑根据您的游戏环境和需求可能有所不同
		ItemTypeEnum[] meleeWeaponTypes = {
												  ItemTypeEnum.OneHandedWeapon,
												  ItemTypeEnum.TwoHandedWeapon,
												  ItemTypeEnum.Polearm
											  };
		foreach (ItemTypeEnum weaponType in meleeWeaponTypes) {
			EquipmentElement bestItem = FindBestItemForType(weaponType, isMounted);
			if (bestItem.Item != null) {
				return bestItem;
			}
		}

		return default;
	}

	public static WeaponClass? DetermineWeaponClassForSlot(EquipmentIndex slot,
														   WeaponClass? weaponClass0,
														   WeaponClass? weaponClass1,
														   WeaponClass? weaponClass2,
														   WeaponClass? weaponClass3) {
		// 根据装备槽位返回相应的武器类别
		switch (slot) {
			case EquipmentIndex.Weapon0: return weaponClass0;
			case EquipmentIndex.Weapon1: return weaponClass1;
			case EquipmentIndex.Weapon2: return weaponClass2;
			case EquipmentIndex.Weapon3: return weaponClass3;
			default: return null; // 对于非武器槽位，返回null
		}
	}

	public static WeaponClass? GetWeaponClassFromEquipment(Equipment equipment, EquipmentIndex weaponSlot) {
		EquipmentElement equipmentElement = equipment.GetEquipmentFromSlot(weaponSlot);
		if (equipmentElement.Item != null && equipmentElement.Item.HasWeaponComponent) {
			return equipmentElement.Item.WeaponComponent.PrimaryWeapon.WeaponClass;
		}

		return null;
	}
}