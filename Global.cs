#region

using TaleWorlds.Core;

#endregion

namespace Bannerlord.DynamicTroop;

public static class Global {
	public static bool IsWeapon(ItemObject item) { return item.HasWeaponComponent; }

	public static WeaponClass? GetWeaponClass(ItemObject item) {
		if (IsWeapon(item)) {
			return item.WeaponComponent.PrimaryWeapon.WeaponClass;
		}

		return null;
	}
}