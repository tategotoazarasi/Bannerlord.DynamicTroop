using TaleWorlds.Core;
using TaleWorlds.Library;

namespace DTES2.Extensions;

public static class ItemObjectEffectivenessExtension {
	public static float CalculateEffectiveness(this ItemObject? itemObject) {
		if (itemObject == null) {
			return 1f;
		}

		if (itemObject.HasArmorComponent) {
			return CalculateArmorEffectiveness(itemObject.ArmorComponent, itemObject.Type);
		}

		if (itemObject.HasWeaponComponent) {
			return CalculateWeaponEffectiveness(itemObject.WeaponComponent, itemObject.Weight);
		}

		if (itemObject.HasHorseComponent) {
			return CalculateHorseEffectiveness(itemObject.HorseComponent, itemObject.Weight);
		}

		return 1f;
	}

	private static float CalculateArmorEffectiveness(ArmorComponent? armorComponent, ItemObject.ItemTypeEnum itemType) {
		if (armorComponent == null) {
			return 1f;
		}

		if (itemType == ItemObject.ItemTypeEnum.HorseHarness) {
			return armorComponent.BodyArmor * 1.67f;
		}

		return (armorComponent.HeadArmor * 34f +
				armorComponent.BodyArmor * 42f +
				armorComponent.LegArmor  * 12f +
				armorComponent.ArmArmor  * 12f) *
			   0.03f;
	}

	private static float CalculateWeaponEffectiveness(WeaponComponent? weaponComponent, float weight) {
		if (weaponComponent == null) {
			return 1f;
		}

		WeaponComponentData primaryWeapon = weaponComponent.PrimaryWeapon;
		float weaponClassEffectivenessMultiplier = GetWeaponClassEffectivenessMultiplier(primaryWeapon.WeaponClass);

		if (primaryWeapon.IsRangedWeapon) {
			return CalculateRangedWeaponEffectiveness(primaryWeapon, weaponClassEffectivenessMultiplier);
		}

		if (primaryWeapon.IsMeleeWeapon) {
			return CalculateMeleeWeaponEffectiveness(primaryWeapon, weight, weaponClassEffectivenessMultiplier);
		}

		if (primaryWeapon.IsConsumable) {
			return CalculateConsumableWeaponEffectiveness(primaryWeapon, weaponClassEffectivenessMultiplier);
		}

		if (primaryWeapon.IsShield) {
			return CalculateShieldEffectiveness(primaryWeapon, weaponClassEffectivenessMultiplier);
		}

		return 1f;
	}

	private static float CalculateRangedWeaponEffectiveness(
		WeaponComponentData primaryWeapon,
		float               weaponClassEffectivenessMultiplier
	) {
		if (primaryWeapon.IsConsumable) {
			return (primaryWeapon.MissileDamage * primaryWeapon.MissileSpeed * 1.775f +
					primaryWeapon.Accuracy      * primaryWeapon.MaxDataValue * 25f    +
					primaryWeapon.WeaponLength  * 4f) *
				   0.006944f                          *
				   primaryWeapon.MaxDataValue         *
				   weaponClassEffectivenessMultiplier;
		}

		return (primaryWeapon.MissileSpeed * primaryWeapon.MissileDamage * 1.75f +
				primaryWeapon.ThrustSpeed  * primaryWeapon.Accuracy      * 0.3f) *
			   0.01f                                                             *
			   primaryWeapon.MaxDataValue                                        *
			   weaponClassEffectivenessMultiplier;
	}

	private static float CalculateMeleeWeaponEffectiveness(
		WeaponComponentData primaryWeapon,
		float               weight,
		float               weaponClassEffectivenessMultiplier
	) {
		float thrustEffectiveness       = primaryWeapon.ThrustSpeed * primaryWeapon.ThrustDamage * 0.01f;
		float swingEffectiveness        = primaryWeapon.SwingSpeed  * primaryWeapon.SwingDamage  * 0.01f;
		float higherDamageEffectiveness = MathF.Max(swingEffectiveness, thrustEffectiveness);
		float lowerDamageEffectiveness  = MathF.Min(swingEffectiveness, thrustEffectiveness);
		return ((higherDamageEffectiveness +
				 lowerDamageEffectiveness * lowerDamageEffectiveness / higherDamageEffectiveness) *
				120f                             +
				primaryWeapon.Handling     * 15f +
				primaryWeapon.WeaponLength * 20f +
				weight                     * 5f) *
			   0.01f                             *
			   weaponClassEffectivenessMultiplier;
	}

	private static float CalculateConsumableWeaponEffectiveness(
		WeaponComponentData primaryWeapon,
		float               weaponClassEffectivenessMultiplier
	)
		=> (primaryWeapon.MissileDamage * 550f + primaryWeapon.MissileSpeed * 15f + primaryWeapon.MaxDataValue * 60f) *
		   0.01f                                                                                                      *
		   weaponClassEffectivenessMultiplier;

	private static float CalculateShieldEffectiveness(
		WeaponComponentData primaryWeapon,
		float               weaponClassEffectivenessMultiplier
	)
		=> (primaryWeapon.BodyArmor    * 60f +
			primaryWeapon.ThrustSpeed  * 10f +
			primaryWeapon.MaxDataValue * 40f +
			primaryWeapon.WeaponLength * 20f) *
		   0.01f                              *
		   weaponClassEffectivenessMultiplier;

	private static float CalculateHorseEffectiveness(HorseComponent? horseComponent, float weight) {
		if (horseComponent == null) {
			return 1f;
		}

		return (horseComponent.ChargeDamage * horseComponent.Speed +
				horseComponent.Maneuver     * horseComponent.Speed +
				horseComponent.BodyLength   * weight * 0.025f)           *
			   (horseComponent.HitPoints + horseComponent.HitPointBonus) *
			   0.0001f;
	}

	private static float GetWeaponClassEffectivenessMultiplier(WeaponClass weaponClass) {
		switch (weaponClass) {
			case WeaponClass.Dagger: return 0.4f;

			case WeaponClass.OneHandedSword: return 0.55f;

			case WeaponClass.TwoHandedSword: return 0.6f;

			case WeaponClass.OneHandedAxe: return 0.5f;

			case WeaponClass.TwoHandedAxe: return 0.55f;

			case WeaponClass.Mace: return 0.5f;

			case WeaponClass.Pick: return 0.4f;

			case WeaponClass.TwoHandedMace: return 0.55f;

			case WeaponClass.OneHandedPolearm: return 0.4f;

			case WeaponClass.TwoHandedPolearm: return 0.4f;

			case WeaponClass.LowGripPolearm: return 0.4f;

			case WeaponClass.Arrow: return 3f;

			case WeaponClass.Bolt: return 3f;

			case WeaponClass.Cartridge: return 3f;

			case WeaponClass.Bow: return 0.55f;

			case WeaponClass.Crossbow: return 0.57f;

			case WeaponClass.Stone: return 0.1f;

			case WeaponClass.Boulder: return 0.1f;

			case WeaponClass.ThrowingAxe: return 0.25f;

			case WeaponClass.ThrowingKnife: return 0.2f;

			case WeaponClass.Javelin: return 0.28f;

			case WeaponClass.Pistol: return 1f;

			case WeaponClass.Musket: return 1f;

			case WeaponClass.SmallShield: return 0.4f;

			case WeaponClass.LargeShield: return 0.5f;

			default: return 1f;
		}
	}
}