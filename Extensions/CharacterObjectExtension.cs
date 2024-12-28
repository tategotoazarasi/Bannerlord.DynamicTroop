using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace DTES2.Extensions;

public static class CharacterObjectExtension {
	public static List<ItemObject> GetRecruitmentEquipment(this CharacterObject? characterObject) {
		HashSet<ItemObject> itemsSet  = [];
		List<ItemObject>    itemsList = [];
		if (characterObject == null) {
			return itemsList;
		}

		for (EquipmentIndex i = EquipmentIndex.ArmorItemBeginSlot; i <= EquipmentIndex.HorseHarness; i++) {
			EquipmentElement equipmentElement = characterObject.RandomBattleEquipment.GetEquipmentFromSlot(i);
			if (!equipmentElement.IsEmpty) {
				_ = itemsSet.Add(equipmentElement.Item);
			}
		}

		for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++) {
			foreach (Equipment? equipment in characterObject.BattleEquipments) {
				EquipmentElement equipmentElement = equipment.GetEquipmentFromSlot(i);
				if (!equipmentElement.IsEmpty) {
					switch (equipmentElement.Item.ItemType) {
						case ItemObject.ItemTypeEnum.Arrows:
						case ItemObject.ItemTypeEnum.Bolts:
						case ItemObject.ItemTypeEnum.Thrown:
							itemsList.Add(equipmentElement.Item);
							break;

						default:
							_ = itemsSet.Add(equipmentElement.Item);
							break;
					}
				}
			}
		}

		itemsList.AddRange(itemsSet);
		return itemsList;
	}
}