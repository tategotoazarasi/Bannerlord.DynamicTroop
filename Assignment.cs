#region

	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.MountAndBlade;
	using static TaleWorlds.Core.ItemObject;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class Assignment {
		public static readonly EquipmentIndex[] WeaponSlots = {
																  EquipmentIndex.Weapon0,
																  EquipmentIndex.Weapon1,
																  EquipmentIndex.Weapon2,
																  EquipmentIndex.Weapon3
															  };

		private static int counter;

		public Equipment Equipment;

		public Assignment(CharacterObject character) {
			Index              = ++counter;
			Character          = character;
			Equipment          = ArmyArmory.CreateEmptyEquipment();
			ReferenceEquipment = character.RandomBattleEquipment.Clone();

			//Global.Log($"agent {Index} select {ReferenceEquipment.CalculateEquipmentCode()} as equipment");
		}

		public int Index { get; }

		public bool IsAssigned { get; set; }

		public CharacterObject Character { get; }

		public Equipment ReferenceEquipment { get; }

		public bool IsShielded {
			get {
				foreach (var slot in WeaponSlots) {
					var element = Equipment.GetEquipmentFromSlot(slot);
					if (!element.IsEmpty && element.Item != null && element.Item.ItemType == ItemTypeEnum.Shield)
						return true;
				}

				return false;
			}
		}

		public bool CanBeShielded {
			get {
				foreach (var slot in WeaponSlots) {
					var element = Equipment.GetEquipmentFromSlot(slot);
					if (!element.IsEmpty                                                                   &&
						element.Item                                       != null                         &&
						element.Item.ItemType                              == ItemTypeEnum.OneHandedWeapon &&
						MBItem.GetItemUsageSetFlags(element.Item.StringId) != ItemUsageSetFlags.RequiresNoShield)
						return true;
				}

				return false;
			}
		}

		public bool IsArcher {
			get {
				foreach (var slot in WeaponSlots) {
					var element = Equipment.GetEquipmentFromSlot(slot);
					if (!element.IsEmpty && element.Item != null && Global.IsBow(element.Item)) return true;
				}

				return false;
			}
		}

		public bool IsCrossBowMan {
			get {
				foreach (var slot in WeaponSlots) {
					var element = Equipment.GetEquipmentFromSlot(slot);
					if (!element.IsEmpty && element.Item != null && Global.IsCrossBow(element.Item)) return true;
				}

				return false;
			}
		}

		public bool HaveThrown {
			get {
				foreach (var slot in WeaponSlots) {
					var element = Equipment.GetEquipmentFromSlot(slot);
					if (!element.IsEmpty && element.Item != null && Global.IsThrowing(element.Item)) return true;
				}

				return false;
			}
		}

		public bool HaveTwoHandedWeaponOrPolearms {
			get {
				foreach (var slot in WeaponSlots) {
					var element = Equipment.GetEquipmentFromSlot(slot);
					if (!element.IsEmpty     &&
						element.Item != null &&
						(Global.IsTwoHanded(element.Item) || Global.IsPolearm(element.Item)))
						return true;
				}

				return false;
			}
		}

		public EquipmentIndex? EmptyWeaponSlot {
			get {
				foreach (var slot in WeaponSlots)
					if (Equipment.GetEquipmentFromSlot(slot).IsEmpty || Equipment.GetEquipmentFromSlot(slot).Item == null)
						return slot;

				return null;
			}
		}

		public bool IsMounted {
			get {
				var horse = ReferenceEquipment.GetEquipmentFromSlot(EquipmentIndex.Horse);
				return !horse.IsEmpty && horse.Item != null;
			}
		}

		public bool IsUnarmed() {
			return (Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon0).IsEmpty ||
					Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon0).Item == null) &&
				   (Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1).IsEmpty ||
					Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1).Item == null) &&
				   (Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2).IsEmpty ||
					Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2).Item == null) &&
				   (Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3).IsEmpty ||
					Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3).Item == null);
		}
	}