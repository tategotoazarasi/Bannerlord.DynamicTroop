#region

	using System;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;

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

		public static EquipmentIndex[] ArmourAndHorsesSlots = {
																  EquipmentIndex.Head,
																  EquipmentIndex.Body,
																  EquipmentIndex.Leg,
																  EquipmentIndex.Gloves,
																  EquipmentIndex.Cape
															  };

		public static bool IsWeapon(ItemObject item) { return item.HasWeaponComponent; }

		public static WeaponClass? GetWeaponClass(ItemObject item) {
			return IsWeapon(item) ? item.WeaponComponent.PrimaryWeapon.WeaponClass : null;
		}

		public static void Log(string str) {
			if (SubModule.settings.DebugMode) InformationManager.DisplayMessage(new InformationMessage(str, Colors.Red));
		}

		public static bool IsAgentValid(Agent? agent) {
			return agent           != null &&
				   agent.Formation != null &&
				   agent.IsHuman           &&
				   agent.Character != null &&
				   agent.Team      != null &&
				   agent.Origin    != null &&
				   agent.Team.IsValid;
		}

		public static bool IsWeaponCouchable(ItemObject weapon) {
			foreach (var weaponComponentData in weapon.Weapons) {
				var weaponDescriptionId = weaponComponentData.WeaponDescriptionId;
				if (weaponDescriptionId                                                      != null &&
					weaponDescriptionId.IndexOf("couch", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			return false;
		}

		public static bool IsSuitableForMount(ItemObject weapon) {
			foreach (var weaponComponentData in weapon.Weapons) {
				var weaponDescriptionId = weaponComponentData.WeaponDescriptionId;
				if (weaponDescriptionId != null &&
					MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage)
						  .HasFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount))
					return false;
			}

			return true;
		}

		public static bool IsSuitableForInfantry(ItemObject weapon) {
			foreach (var weaponComponentData in weapon.Weapons) {
				// 获取当前武器组件的WeaponFlags
				var weaponFlags = weaponComponentData.WeaponFlags;

				// 使用HasFlag检查武器是否含有所需的任一WeaponFlag
				if ((weaponFlags.HasFlag(WeaponFlags.BonusAgainstShield)  ||
					 weaponFlags.HasFlag(WeaponFlags.CanKnockDown)        ||
					 weaponFlags.HasFlag(WeaponFlags.CanDismount)         ||
					 weaponFlags.HasFlag(WeaponFlags.MultiplePenetration) ||
					 IsWeaponBracable(weapon)) &&
					!IsWeaponCouchable(weapon))
					return true;
			}

			return false;
		}

		public static bool IsWeaponBracable(ItemObject weapon) {
			foreach (var weaponComponentData in weapon.Weapons) {
				var weaponDescriptionId = weaponComponentData.WeaponDescriptionId;
				if (weaponDescriptionId                                                        != null &&
					weaponDescriptionId.IndexOf("bracing", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			return false;
		}
	}