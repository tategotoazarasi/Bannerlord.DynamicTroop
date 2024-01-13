#region

	using System.Collections.Generic;
	using HarmonyLib;
	using log4net.Core;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
	using TaleWorlds.Core;
	using TaleWorlds.Library;

#endregion

	namespace Bannerlord.DynamicTroop;

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
						if (!equipment.IsEmpty && equipment.Item != null)
							ArmyArmory.AddItemToArmory(equipment.Item);
				}
		}

		public static List<EquipmentElement> GetRecruitEquipments(CharacterObject? character) {
			if (character == null || character.BattleEquipments == null || character.BattleEquipments.IsEmpty())
				return new List<EquipmentElement>();

			var                       armorAndHorse     = character.RandomBattleEquipment;
			List<EquipmentElement>    equipmentElements = new();
			List<EquipmentElement>    weaponList        = new();
			HashSet<EquipmentElement> weaponSet         = new(new EquipmentElementComparer());
			foreach (var slot in Global.ArmourAndHorsesSlots) {
				var item = armorAndHorse.GetEquipmentFromSlot(slot);
				if (!item.IsEmpty && item.Item != null) equipmentElements.Add(item);
			}

			foreach (var equipment in character.BattleEquipments)
				if (equipment.IsValid)
					foreach (var slot in Assignment.WeaponSlots) {
						var item = equipment.GetEquipmentFromSlot(slot);
						if (!item.IsEmpty && item.Item != null && Global.IsWeapon(item.Item)) {
							if (Global.IsConsumableWeapon(item.Item))
								equipmentElements.Add(item); // 直接添加消耗品类型武器
							else
								weaponList.Add(item); // 非消耗品类型武器添加到列表
						}
					}

			weaponList.Shuffle();
			foreach (var weapon in weaponList)
				if (!weaponSet.Contains(weapon))
					_ = weaponSet.Add(weapon);

			equipmentElements.AddRange(weaponSet);
			return equipmentElements;
		}

		public static List<EquipmentElement> GetAllRecruitEquipments(CharacterObject? character) {
			List<EquipmentElement> list = new();
			if (character == null || character.BattleEquipments == null || character.BattleEquipments.IsEmpty())
				return list;

			foreach (var equipment in character.BattleEquipments)
				if (equipment != null && equipment.IsValid)
					foreach (var slot in Global.EquipmentSlots) {
						var item = equipment.GetEquipmentFromSlot(slot);
						if (!item.IsEmpty && item.Item != null) list.Add(item);
					}

			return list;
		}

		public class EquipmentElementComparer : IEqualityComparer<EquipmentElement> {
			public bool Equals(EquipmentElement x, EquipmentElement y) {
				// 检查消耗品类型武器，始终返回不相等
				return !Global.IsConsumableWeapon(x.Item) &&
					   !Global.IsConsumableWeapon(y.Item) &&
					   Global.FullySameWeaponClass(x.Item, y.Item);
			}

			public int GetHashCode(EquipmentElement obj) {
				// 对消耗品类型武器，返回不同的哈希值
				if (Global.IsConsumableWeapon(obj.Item)) return obj.GetHashCode(); // 使用对象本身的哈希值

				var weaponClasses = Global.GetWeaponClass(obj.Item);
				weaponClasses.Sort();

				var hash                                        = 17;
				foreach (var weaponClass in weaponClasses) hash = hash * 31 + weaponClass.GetHashCode();

				return hash;
			}
		}
	}