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
					foreach (var equipment in equipments)
						if (!equipment.IsEmpty && equipment.Item != null)
							ArmyArmory.AddItemToArmory(equipment.Item);
					//ArmyArmory.AddSoldierEquipmentToArmory(troop.Character); //else { InformationManager.DisplayMessage(new InformationMessage("FAILED RECRUITMENT", Colors.Red)); }
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
				if (!item.IsEmpty && item.Item != null) {
					Global.Log($"GetRecruitEquipments {item.Item.StringId} Added", Colors.Green, Level.Debug);
					equipmentElements.Add(item);
				}
			}

			Global.Log($"GetRecruitEquipments equipmentElements.Count={equipmentElements.Count}",
					   Colors.Green,
					   Level.Debug);
			foreach (var equipment in character.BattleEquipments)
				if (equipment.IsValid)
					foreach (var slot in Assignment.WeaponSlots) {
						var item = equipment.GetEquipmentFromSlot(slot);
						if (!item.IsEmpty && item.Item != null && Global.IsWeapon(item.Item)) {
							Global.Log($"GetRecruitEquipments {item.Item.StringId} Added to list",
									   Colors.Green,
									   Level.Debug);
							if (Global.IsConsumableWeapon(item.Item))
								equipmentElements.Add(item); // 直接添加消耗品类型武器
							else
								weaponList.Add(item); // 非消耗品类型武器添加到列表
						}
					}

			Global.Log($"GetRecruitEquipments weaponList.Count={weaponList.Count}", Colors.Green, Level.Debug);
			weaponList.Shuffle();
			foreach (var weapon in weaponList)
				if (!weaponSet.Contains(weapon)) {
					Global.Log($"GetRecruitEquipments new weapon {weapon.Item.StringId} Added to set",
							   Colors.Green,
							   Level.Debug);
					_ = weaponSet.Add(weapon);
				}
				else {
					Global.Log($"GetRecruitEquipments new weapon {weapon.Item.StringId} already exists",
							   Colors.Green,
							   Level.Debug);
				}

			Global.Log($"GetRecruitEquipments weaponSet.Count={weaponSet.Count}", Colors.Green, Level.Debug);
			equipmentElements.AddRange(weaponSet);
			return equipmentElements;
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