using System.Collections.Generic;
using System.Linq;
using DynamicTroopEquipmentReupload.Extensions;
using HarmonyLib;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace DynamicTroopEquipmentReupload.Patches;

[HarmonyPatch(typeof(RecruitmentVM), "OnDone")]
public class RecruitmentPatch {
	private static void Prefix(RecruitmentVM __instance, out Dictionary<CharacterObject, int> __state) {
		__state = __instance.TroopsInCart
			.Where(troop => !troop.IsTroopEmpty && troop.Character != null)
			.Select(troop => troop.Character!)
			.Distinct()
			.ToDictionary(
				character => character,
				character => Campaign.Current.MainParty.MemberRoster.GetTroopCount(character));
	}

	private static void Postfix(Dictionary<CharacterObject, int> __state) {
		foreach (var rosterEntry in __state) {
			var recruitedCount = Campaign.Current.MainParty.MemberRoster.GetTroopCount(rosterEntry.Key) - rosterEntry.Value;
			if (recruitedCount > 0)
				AddStartingEquipmentToPlayerArmory(rosterEntry.Key, recruitedCount);
		}
	}

	internal static void AddStartingEquipmentToPlayerArmory(CharacterObject character, int troopCount) {
		if (troopCount <= 0)
			return;

		Global.Log($"recruiting {troopCount}x{character.Name}", Colors.Green, Level.Debug);
		var equipments = GetRecruitEquipments(character);
		Global.Debug($"{equipments.Count * troopCount} starting equipments added");
		foreach (var equipment in equipments) {
			if (equipment is { IsEmpty: false, Item: not null })
				ArmyArmory.AddItemToArmory(equipment.Item, troopCount);
		}
	}

	public static List<EquipmentElement> GetRecruitEquipments(CharacterObject? character) {
		if (character?.BattleEquipments?.IsEmpty() ?? true) return new List<EquipmentElement>();

		var armorAndHorse = character.RandomBattleEquipment;
		List<EquipmentElement> equipmentElements = new();
		List<EquipmentElement> weaponList = new();
		HashSet<EquipmentElement> weaponSet = new(new EquipmentElementComparer());
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

		foreach (var equipment in character.BattleEquipments) {
			if (equipment.IsEmpty())
				continue;

			foreach (var slot in Assignment.WeaponSlots) {
				var item = equipment.GetEquipmentFromSlot(slot);
				if (item is not { IsEmpty: false, Item: not null } || !item.Item.HasWeaponComponent) continue;

				if (item.Item.IsConsumable())
					equipmentElements.Add(item);
				else
					weaponList.Add(item);
			}
		}

		weaponList.Shuffle();
		foreach (var weapon in weaponList.Except(weaponSet))
			_ = weaponSet.Add(weapon);

		equipmentElements.AddRange(weaponSet);
		return equipmentElements;
	}

	public static List<EquipmentElement> GetRandomizedRecruitEquipments(CharacterObject? character) {
		List<EquipmentElement> list = new();
		if (character?.BattleEquipments?.IsEmpty() ?? true) return list;

		foreach (var equipment in character.BattleEquipments) {
			if (equipment.IsEmpty())
				continue;

			foreach (var slot in Global.EquipmentSlots) {
				var item = equipment.GetEquipmentFromSlot(slot);
				if (item is { IsEmpty: false, Item: not null }) list.Add(item);
			}
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