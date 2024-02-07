using System.Collections.Generic;
using System.Linq;
using Bannerlord.DynamicTroop.Extensions;
using HarmonyLib;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.DynamicTroop.Patches;

[HarmonyPatch(typeof(RecruitmentVM), "ExecuteDone")]
public class RecruitmentPatch {
	public static void Prefix(RecruitmentVM __instance) {
		foreach (var troop in __instance.TroopsInCart)
			// 在这里实现将士兵基础装备添加到军火库的逻辑
			if (!troop.IsTroopEmpty && troop.Character != null) {
				Global.Log($"recruiting {troop.Character.StringId}", Colors.Green, Level.Debug);
				var equipments = GetRecruitEquipments(troop.Character);
				Global.Debug($"{equipments.Count} starting equipments added");
				foreach (var equipment in equipments)
					if (equipment is { IsEmpty: false, Item: not null })
						ArmyArmory.AddItemToArmory(equipment.Item);
			}
	}

	public static List<EquipmentElement> GetRecruitEquipments(CharacterObject? character) {
		if (character?.BattleEquipments?.IsEmpty() ?? true) return new List<EquipmentElement>();

		var                       armorAndHorse     = character.RandomBattleEquipment;
		List<EquipmentElement>    equipmentElements = new();
		List<EquipmentElement>    weaponList        = new();
		HashSet<EquipmentElement> weaponSet         = new(new EquipmentElementComparer());
		foreach (var slot in Global.ArmourSlots) {
			var equipmentElement = armorAndHorse.GetEquipmentFromSlot(slot);
			if (equipmentElement is { IsEmpty: false, Item: not null }) {
				if (ModSettings.Instance?.RandomizeStartingEquipment ?? false) {
					List<ItemObject> items  = new();
					ItemObject[]?    items1 = { };
					if (equipmentElement.Item.Culture is CultureObject cultureObject)
						items1 = Cache.GetItemsByTypeTierAndCulture(equipmentElement.Item.ItemType,
																	(int)equipmentElement.Item.Tier,
																	cultureObject);
					var items2 =
						Cache.GetItemsByTypeTierAndCulture(equipmentElement.Item.ItemType,
														   character.Tier,
														   character.Culture);
					if (items1 != null) items.AddRange(items1);
					if (items2 != null) items.AddRange(items2);
					items.Add(equipmentElement.Item);
					items = items.Distinct().ToList();
					equipmentElements.Add(new EquipmentElement(WeightedRandomSelector.SelectItem(items,
																   equipmentElement.Item.Effectiveness)));
				}
				else { equipmentElements.Add(equipmentElement); }
			}
		}

		foreach (var slot in new[] { EquipmentIndex.Horse, EquipmentIndex.HorseHarness }) {
			var equipmentElement = armorAndHorse.GetEquipmentFromSlot(slot);
			if (equipmentElement is { IsEmpty: false, Item: not null }) equipmentElements.Add(equipmentElement);
		}

		foreach (var equipment in character.BattleEquipments)
			if (equipment.IsValid)
				foreach (var slot in Assignment.WeaponSlots) {
					var item = equipment.GetEquipmentFromSlot(slot);
					if (item is not { IsEmpty: false, Item: not null } || !item.Item.HasWeaponComponent) continue;

					if (item.Item.IsConsumable())
						equipmentElements.Add(item); // 直接添加消耗品类型武器
					else
						weaponList.Add(item); // 非消耗品类型武器添加到列表
				}

		weaponList.Shuffle();
		var newWeapons                       = weaponList.Except(weaponSet);
		foreach (var weapon in newWeapons) _ = weaponSet.Add(weapon);

		equipmentElements.AddRange(weaponSet);
		return equipmentElements;
	}

	public static List<EquipmentElement> GetRandomizedRecruitEquipments(CharacterObject? character) {
		List<EquipmentElement> list = new();
		if (character?.BattleEquipments?.IsEmpty() ?? true) return list;

		foreach (var equipment in character.BattleEquipments)
			if (equipment is { IsValid: true })
				foreach (var slot in Global.EquipmentSlots) {
					var item = equipment.GetEquipmentFromSlot(slot);
					if (item is { IsEmpty: false, Item: not null }) list.Add(item);
				}

		return list;
	}

	private class EquipmentElementComparer : IEqualityComparer<EquipmentElement> {
		public bool Equals(EquipmentElement x, EquipmentElement y) {
			// 检查消耗品类型武器，始终返回不相等
			return !x.Item.IsConsumable() && !y.Item.IsConsumable() && Global.FullySameWeaponClass(x.Item, y.Item);
		}

		public int GetHashCode(EquipmentElement obj) {
			// 对消耗品类型武器，返回不同的哈希值
			if (obj.Item.IsConsumable()) return obj.GetHashCode(); // 使用对象本身的哈希值

			var weaponClasses = Global.GetWeaponClass(obj.Item);
			weaponClasses.Sort();

			var hash = 17;
			hash = weaponClasses.Aggregate(hash,
										   (currentHash, weaponClass) => currentHash * 31 + weaponClass.GetHashCode());

			return hash;
		}
	}
}