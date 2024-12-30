using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace DTES2.Extensions;

public static class ItemObjectExtension {
	public static bool IsArrow(this ItemObject? equipment)
		=> equipment != null &&
		   CacheManager.GetOrAdd(
			   () => equipment.ItemType == ItemObject.ItemTypeEnum.Arrows ||
					 equipment.Weapons != null &&
					 equipment.Weapons.AnyQ(
						 weaponComponentData => weaponComponentData is { WeaponClass: WeaponClass.Arrow }
					 ),
			   equipment.StringId
		   );

	public static bool IsBolt(this ItemObject? equipment)
		=> equipment != null &&
		   CacheManager.GetOrAdd(
			   () => equipment.ItemType == ItemObject.ItemTypeEnum.Bolts ||
					 equipment.Weapons != null &&
					 equipment.Weapons.Any(
						 weaponComponentData => weaponComponentData is { WeaponClass: WeaponClass.Bolt }
					 ),
			   equipment.StringId
		   );

	public static bool IsConsumable(this ItemObject? item)
		=> item != null &&
		   CacheManager.GetOrAdd(
			   () => {
				   return item.ItemType is ItemObject.ItemTypeEnum.Arrows
										   or ItemObject.ItemTypeEnum.Bolts
										   or ItemObject.ItemTypeEnum.Thrown;
			   },
			   item.StringId
		   );

	public static bool IsCouchable(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => flag.Contains("can_couchable")),
			   weapon.StringId
		   );

	public static bool IsSuitableForMount(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true, Weapons: not null } &&
					 !weapon.Weapons.AnyQ(
						 weaponComponentData => weaponComponentData != null &&
												(MBItem.
												 GetItemUsageSetFlags(weaponComponentData.ItemUsage).
												 HasFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount) ||
												 weaponComponentData.WeaponFlags.HasFlag(
													 WeaponFlags.CantReloadOnHorseback
												 ))
					 ),
			   weapon.StringId
		   );

	public static bool IsBracable(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => flag.Contains("braceable")),
			   weapon.StringId
		   );

	public static bool IsPolearm(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("polearm")),
			   weapon.StringId
		   );

	public static bool IsOneHanded(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => flag.Contains("one_handed")),
			   weapon.StringId
		   );

	public static bool IsTwoHanded(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => flag.Contains("two_handed")),
			   weapon.StringId
		   );

	public static bool IsThrowing(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => flag.Contains("throwing")),
			   weapon.StringId
		   );

	public static bool IsBow(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => {
				   if (weapon.ItemType == ItemObject.ItemTypeEnum.Bow) {
					   return true;
				   }

				   if (!weapon.HasWeaponComponent) {
					   return false;
				   }

				   if (weapon.Weapons == null) {
					   return false;
				   }

				   foreach (WeaponComponentData? weaponComponentData in weapon.Weapons) {
					   if (weaponComponentData is { WeaponClass: WeaponClass.Bow }) {
						   return true;
					   }
				   }

				   return false;
			   },
			   weapon.StringId
		   );

	public static bool IsCrossBow(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => {
				   if (weapon.ItemType == ItemObject.ItemTypeEnum.Crossbow) {
					   return true;
				   }

				   if (!weapon.HasWeaponComponent) {
					   return false;
				   }

				   if (weapon.Weapons == null) {
					   return false;
				   }

				   foreach (WeaponComponentData? weaponComponentData in weapon.Weapons) {
					   if (weaponComponentData is { WeaponClass: WeaponClass.Crossbow }) {
						   return true;
					   }
				   }

				   return false;
			   },
			   weapon.StringId
		   );

	public static bool IsThrowingWeaponCanBeAcquired(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => {
				   if (weapon.ItemType != ItemObject.ItemTypeEnum.Thrown) {
					   return false;
				   }

				   if (!weapon.HasWeaponComponent) {
					   return false;
				   }

				   if (weapon.Weapons == null) {
					   return false;
				   }

				   foreach (WeaponComponentData? weaponComponentData in weapon.Weapons) {
					   if (weaponComponentData is {
													  WeaponClass: WeaponClass.ThrowingAxe
																   or WeaponClass.ThrowingKnife
																   or WeaponClass.Javelin
												  }) {
						   return true;
					   }
				   }

				   return false;
			   },
			   weapon.StringId
		   );

	public static bool IsBonusAgainstShield(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => flag.Contains("bonus_against_shield")),
			   weapon.StringId
		   );

	public static bool CanKnockdown(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => flag.Contains("can_knockdown")),
			   weapon.StringId
		   );

	public static bool CanDismount(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => flag.Contains("can_dismount")),
			   weapon.StringId
		   );

	public static bool CantUseWithShields(this ItemObject? weapon)
		=> weapon != null &&
		   CacheManager.GetOrAdd(
			   () => weapon is { HasWeaponComponent: true } &&
					 weapon.CheckWeaponFlag(flag => !flag.Contains("cant_use_with_shields")),
			   weapon.StringId
		   );

	private static bool CheckWeaponFlag(this ItemObject? weapon, Func<string, bool> flagCondition)
		=> weapon          != null &&
		   weapon?.Weapons != null &&
		   weapon.
			   Weapons.
			   WhereQ(weaponComponentData => weaponComponentData != null).
			   SelectMany(
				   weaponComponentData
					   => CampaignUIHelper.GetFlagDetailsForWeapon(
						   weaponComponentData,
						   MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage)
					   )
			   ).
			   AnyQ(
				   flagDetail => flagDetail.Item1 != null    &&
								 !flagDetail.Item1.IsEmpty() &&
								 flagCondition(flagDetail.Item1)
			   );

	public static bool IsSuitableForCharacter(this ItemObject? item, CharacterObject? character)
		=> item      != null &&
		   character != null &&
		   CacheManager.GetOrAdd(
			   () => item.Difficulty <= 0 || item.Difficulty <= character.GetSkillValue(item.RelevantSkill),
			   item.StringId + character.StringId
		   );

	public static bool MatchHarness(this ItemObject? horse, ItemObject? harness)
		=> horse   != null &&
		   harness != null &&
		   CacheManager.GetOrAdd(
			   () => horse is { HasHorseComponent  : true, ItemType: ItemObject.ItemTypeEnum.Horse }        &&
					 harness is { HasArmorComponent: true, ItemType: ItemObject.ItemTypeEnum.HorseHarness } &&
					 horse.HorseComponent?.Monster?.FamilyType == harness.ArmorComponent?.FamilyType,
			   horse.StringId + harness.StringId
		   );

	public static float CalculateEffectiveness(this ItemObject? itemObject) {
		float          effectiveness  = 1f;
		ArmorComponent armorComponent = itemObject.ArmorComponent;
		if (armorComponent != null) {
			if (itemObject.Type == ItemObject.ItemTypeEnum.HorseHarness) {
				effectiveness = armorComponent.BodyArmor * 1.67f;
			} else {
				effectiveness = (armorComponent.HeadArmor * 34f +
								 armorComponent.BodyArmor * 42f +
								 armorComponent.LegArmor  * 12f +
								 armorComponent.ArmArmor  * 12f) *
								0.03f;
			}
		}

		if (itemObject.WeaponComponent != null) {
			WeaponComponentData primaryWeapon                      = itemObject.WeaponComponent.PrimaryWeapon;
			float               weaponClassEffectivenessMultiplier = 1f;
			switch (primaryWeapon.WeaponClass) {
				case WeaponClass.Dagger:
					weaponClassEffectivenessMultiplier = 0.4f;
					break;

				case WeaponClass.OneHandedSword:
					weaponClassEffectivenessMultiplier = 0.55f;
					break;

				case WeaponClass.TwoHandedSword:
					weaponClassEffectivenessMultiplier = 0.6f;
					break;

				case WeaponClass.OneHandedAxe:
					weaponClassEffectivenessMultiplier = 0.5f;
					break;

				case WeaponClass.TwoHandedAxe:
					weaponClassEffectivenessMultiplier = 0.55f;
					break;

				case WeaponClass.Mace:
					weaponClassEffectivenessMultiplier = 0.5f;
					break;

				case WeaponClass.Pick:
					weaponClassEffectivenessMultiplier = 0.4f;
					break;

				case WeaponClass.TwoHandedMace:
					weaponClassEffectivenessMultiplier = 0.55f;
					break;

				case WeaponClass.OneHandedPolearm:
					weaponClassEffectivenessMultiplier = 0.4f;
					break;

				case WeaponClass.TwoHandedPolearm:
					weaponClassEffectivenessMultiplier = 0.4f;
					break;

				case WeaponClass.LowGripPolearm:
					weaponClassEffectivenessMultiplier = 0.4f;
					break;

				case WeaponClass.Arrow:
					weaponClassEffectivenessMultiplier = 3f;
					break;

				case WeaponClass.Bolt:
					weaponClassEffectivenessMultiplier = 3f;
					break;

				case WeaponClass.Cartridge:
					weaponClassEffectivenessMultiplier = 3f;
					break;

				case WeaponClass.Bow:
					weaponClassEffectivenessMultiplier = 0.55f;
					break;

				case WeaponClass.Crossbow:
					weaponClassEffectivenessMultiplier = 0.57f;
					break;

				case WeaponClass.Stone:
					weaponClassEffectivenessMultiplier = 0.1f;
					break;

				case WeaponClass.Boulder:
					weaponClassEffectivenessMultiplier = 0.1f;
					break;

				case WeaponClass.ThrowingAxe:
					weaponClassEffectivenessMultiplier = 0.25f;
					break;

				case WeaponClass.ThrowingKnife:
					weaponClassEffectivenessMultiplier = 0.2f;
					break;

				case WeaponClass.Javelin:
					weaponClassEffectivenessMultiplier = 0.28f;
					break;

				case WeaponClass.Pistol:
					weaponClassEffectivenessMultiplier = 1f;
					break;

				case WeaponClass.Musket:
					weaponClassEffectivenessMultiplier = 1f;
					break;

				case WeaponClass.SmallShield:
					weaponClassEffectivenessMultiplier = 0.4f;
					break;

				case WeaponClass.LargeShield:
					weaponClassEffectivenessMultiplier = 0.5f;
					break;
			}

			if (primaryWeapon.IsRangedWeapon) {
				if (primaryWeapon.IsConsumable) {
					effectiveness =
						(primaryWeapon.MissileDamage * primaryWeapon.MissileSpeed * 1.775f +
						 primaryWeapon.Accuracy      * primaryWeapon.MaxDataValue * 25f    +
						 primaryWeapon.WeaponLength  * 4f) *
						0.006944f                          *
						primaryWeapon.MaxDataValue         *
						weaponClassEffectivenessMultiplier;
				} else {
					effectiveness =
						(primaryWeapon.MissileSpeed * primaryWeapon.MissileDamage * 1.75f +
						 primaryWeapon.ThrustSpeed  * primaryWeapon.Accuracy      * 0.3f) *
						0.01f                                                             *
						primaryWeapon.MaxDataValue                                        *
						weaponClassEffectivenessMultiplier;
				}
			} else if (primaryWeapon.IsMeleeWeapon) {
				float thrustEffectiveness       = primaryWeapon.ThrustSpeed * primaryWeapon.ThrustDamage * 0.01f;
				float swingEffectiveness        = primaryWeapon.SwingSpeed  * primaryWeapon.SwingDamage  * 0.01f;
				float higherDamageEffectiveness = MathF.Max(swingEffectiveness, thrustEffectiveness);
				float lowerDamageEffectiveness  = MathF.Min(swingEffectiveness, thrustEffectiveness);
				effectiveness =
					((higherDamageEffectiveness +
					  lowerDamageEffectiveness * lowerDamageEffectiveness / higherDamageEffectiveness) *
					 120f                             +
					 primaryWeapon.Handling     * 15f +
					 primaryWeapon.WeaponLength * 20f +
					 itemObject.Weight          * 5f) *
					0.01f                             *
					weaponClassEffectivenessMultiplier;
			} else if (primaryWeapon.IsConsumable) {
				effectiveness =
					(primaryWeapon.MissileDamage * 550f +
					 primaryWeapon.MissileSpeed  * 15f  +
					 primaryWeapon.MaxDataValue  * 60f) *
					0.01f                               *
					weaponClassEffectivenessMultiplier;
			} else if (primaryWeapon.IsShield) {
				effectiveness =
					(primaryWeapon.BodyArmor    * 60f +
					 primaryWeapon.ThrustSpeed  * 10f +
					 primaryWeapon.MaxDataValue * 40f +
					 primaryWeapon.WeaponLength * 20f) *
					0.01f                              *
					weaponClassEffectivenessMultiplier;
			}
		}

		if (itemObject.HorseComponent != null) {
			effectiveness =
				(itemObject.HorseComponent.ChargeDamage * itemObject.HorseComponent.Speed +
				 itemObject.HorseComponent.Maneuver     * itemObject.HorseComponent.Speed +
				 itemObject.HorseComponent.BodyLength   * itemObject.Weight * 0.025f)           *
				(itemObject.HorseComponent.HitPoints + itemObject.HorseComponent.HitPointBonus) *
				0.0001f;
		}

		return effectiveness;
	}
}