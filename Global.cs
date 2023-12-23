#region

	using TaleWorlds.Core;
	using TaleWorlds.Library;

#endregion

	namespace Bannerlord.DynamicTroop;

	public static class Global {
		public static EquipmentIndex[] EquipmentSlots = {
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

		public static bool IsWeapon(ItemObject item) { return item.HasWeaponComponent; }

		public static WeaponClass? GetWeaponClass(ItemObject item) {
			return IsWeapon(item) ? item.WeaponComponent.PrimaryWeapon.WeaponClass : null;
		}

		public static void Log(string str) {
			if (SubModule.settings.DebugMode) InformationManager.DisplayMessage(new InformationMessage(str, Colors.Red));
		}
	}