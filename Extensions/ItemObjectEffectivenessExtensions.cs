using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using MathF = TaleWorlds.Library.MathF;

namespace TaleWorlds.MountAndBlade;

/// <summary>
///     Extension methods for <see cref="ItemObject" /> to calculate effectiveness, considering
///     character skills.
/// </summary>
public static class ItemObjectEffectivenessExtension {
	/// <summary>
	///     Calculates the effectiveness of an item, taking into account the character's skills.
	/// </summary>
	/// <param name="itemObject">      The item to calculate effectiveness for. </param>
	/// <param name="characterObject"> The character using the item. </param>
	/// <returns> The calculated effectiveness of the item. </returns>
	public static float CalculateEffectiveness(this ItemObject? itemObject, CharacterObject? characterObject) {
		if (itemObject == null) {
			return 0f;
		}

		float effectiveness = 1f;

		if (itemObject.HasArmorComponent) {
			effectiveness = CalculateArmorEffectiveness(itemObject);
		} else if (itemObject.HasWeaponComponent) {
			effectiveness = CalculateWeaponEffectiveness(itemObject, characterObject);
		} else if (itemObject.HasHorseComponent) {
			effectiveness = CalculateHorseEffectiveness(itemObject);
		}

		return effectiveness;
	}

	/// <summary>
	///     Calculates the effectiveness of an armor item.
	/// </summary>
	/// <param name="itemObject"> The armor item. </param>
	/// <returns> The calculated effectiveness. </returns>
	private static float CalculateArmorEffectiveness(ItemObject itemObject) {
		ArmorComponent armorComponent = itemObject.ArmorComponent;
		float          effectiveness;

		if (itemObject.ItemType == ItemObject.ItemTypeEnum.HorseHarness) {
			effectiveness = armorComponent.BodyArmor * 1.67f;
		} else {
			effectiveness = (armorComponent.HeadArmor * 34f +
							 armorComponent.BodyArmor * 42f +
							 armorComponent.LegArmor  * 12f +
							 armorComponent.ArmArmor  * 12f) *
							0.03f;
		}

		return effectiveness;
	}

	/// <summary>
	///     Calculates the effectiveness of a weapon item, considering character skills.
	/// </summary>
	/// <param name="itemObject">      The weapon item. </param>
	/// <param name="characterObject"> The character using the weapon. </param>
	/// <returns> The calculated effectiveness. </returns>
	private static float CalculateWeaponEffectiveness(ItemObject itemObject, CharacterObject? characterObject) {
		WeaponComponent     weaponComponent = itemObject.WeaponComponent;
		WeaponComponentData primaryWeapon   = weaponComponent.PrimaryWeapon;

		float weaponEffectivenessMultiplier = GetWeaponEffectivenessMultiplier(primaryWeapon.WeaponClass);
		float effectiveness;

		if (primaryWeapon.IsRangedWeapon) {
			effectiveness = CalculateRangedWeaponEffectiveness(
				primaryWeapon,
				weaponEffectivenessMultiplier,
				characterObject
			);
		} else if (primaryWeapon.IsMeleeWeapon) {
			effectiveness = CalculateMeleeWeaponEffectiveness(
				primaryWeapon,
				weaponEffectivenessMultiplier,
				characterObject,
				itemObject.Weight
			);
		} else if (primaryWeapon.IsConsumable) {
			effectiveness = CalculateConsumableEffectiveness(primaryWeapon, weaponEffectivenessMultiplier);
		} else if (primaryWeapon.IsShield) {
			effectiveness = CalculateShieldEffectiveness(primaryWeapon, weaponEffectivenessMultiplier);
		} else {
			effectiveness = 0f; // Handle unknown weapon types
		}

		return effectiveness;
	}

	/// <summary>
	///     Gets the base effectiveness multiplier for a given weapon class.
	/// </summary>
	/// <param name="weaponClass"> The weapon class. </param>
	/// <returns> The base effectiveness multiplier. </returns>
	private static float GetWeaponEffectivenessMultiplier(WeaponClass weaponClass) {
		switch (weaponClass) {
			case WeaponClass.Dagger:
			case WeaponClass.Pick:
			case WeaponClass.OneHandedPolearm:
			case WeaponClass.TwoHandedPolearm:
			case WeaponClass.LowGripPolearm:
			case WeaponClass.SmallShield:
				return 0.4f;

			case WeaponClass.OneHandedAxe:
			case WeaponClass.Mace:
				return 0.5f;

			case WeaponClass.OneHandedSword:
			case WeaponClass.TwoHandedAxe:
			case WeaponClass.TwoHandedMace:
			case WeaponClass.Bow:
			case WeaponClass.LargeShield:
				return 0.55f;

			case WeaponClass.TwoHandedSword: return 0.6f;

			case WeaponClass.Crossbow: return 0.57f;

			case WeaponClass.Stone:
			case WeaponClass.Boulder:
				return 0.1f;

			case WeaponClass.ThrowingAxe: return 0.25f;

			case WeaponClass.Javelin: return 0.28f;

			case WeaponClass.ThrowingKnife: return 0.2f;

			case WeaponClass.Arrow:
			case WeaponClass.Bolt:
			case WeaponClass.Cartridge:
				return 3f;

			case WeaponClass.Pistol:
			case WeaponClass.Musket:
				return 1f;

			default: return 1f;
		}
	}

	/// <summary>
	///     Calculates the effectiveness of a ranged weapon, considering character skills.
	/// </summary>
	/// <param name="primaryWeapon">                 The primary weapon data. </param>
	/// <param name="weaponEffectivenessMultiplier"> The weapon effectiveness multiplier. </param>
	/// <param name="characterObject">               The character using the weapon. </param>
	/// <returns> The calculated effectiveness. </returns>
	private static float CalculateRangedWeaponEffectiveness(
		WeaponComponentData primaryWeapon,
		float               weaponEffectivenessMultiplier,
		CharacterObject?    characterObject
	) {
		float missileDamage = primaryWeapon.MissileDamage;
		float missileSpeed  = primaryWeapon.MissileSpeed;
		float accuracy      = primaryWeapon.Accuracy;

		if (characterObject != null) {
			SkillObject relevantSkill = primaryWeapon.RelevantSkill;
			if (relevantSkill != null) {
				if (relevantSkill == DefaultSkills.Bow) {
					missileDamage *= GetSkillBasedDamageMultiplier(
						characterObject,
						DefaultSkills.Bow,
						DefaultSkillEffects.BowDamage
					);
					accuracy *= GetSkillBasedAccuracyMultiplier(
						characterObject,
						DefaultSkills.Bow,
						DefaultSkillEffects.BowAccuracy
					);
				} else if (relevantSkill == DefaultSkills.Crossbow) {
					accuracy *= GetSkillBasedAccuracyMultiplier(
						characterObject,
						DefaultSkills.Crossbow,
						DefaultSkillEffects.CrossbowAccuracy
					);
				} else if (relevantSkill == DefaultSkills.Throwing) {
					missileDamage *= GetSkillBasedDamageMultiplier(
						characterObject,
						DefaultSkills.Throwing,
						DefaultSkillEffects.ThrowingDamage
					);
					accuracy *= GetSkillBasedAccuracyMultiplier(
						characterObject,
						DefaultSkills.Throwing,
						DefaultSkillEffects.ThrowingAccuracy
					);
				}
			}
		}

		float effectiveness;
		if (primaryWeapon.IsConsumable) {
			effectiveness =
				(missileDamage              * missileSpeed               * 1.775f +
				 accuracy                   * primaryWeapon.MaxDataValue * 25f    +
				 primaryWeapon.WeaponLength * 4f) *
				0.006944f                         *
				primaryWeapon.MaxDataValue        *
				weaponEffectivenessMultiplier;
		} else {
			effectiveness = (missileSpeed * missileDamage * 1.75f + primaryWeapon.ThrustSpeed * accuracy * 0.3f) *
							0.01f                                                                                *
							primaryWeapon.MaxDataValue                                                           *
							weaponEffectivenessMultiplier;
		}

		return effectiveness;
	}

	/// <summary>
	///     Calculates the effectiveness of a melee weapon, considering character skills.
	/// </summary>
	/// <param name="primaryWeapon">                 The primary weapon data. </param>
	/// <param name="weaponEffectivenessMultiplier"> The weapon effectiveness multiplier. </param>
	/// <param name="characterObject">               The character using the weapon. </param>
	/// <param name="weaponWeight">                  The weight of the weapon. </param>
	/// <returns> The calculated effectiveness. </returns>
	private static float CalculateMeleeWeaponEffectiveness(
		WeaponComponentData primaryWeapon,
		float               weaponEffectivenessMultiplier,
		CharacterObject?    characterObject,
		float               weaponWeight
	) {
		float thrustDamage = primaryWeapon.ThrustDamage;
		float thrustSpeed  = primaryWeapon.ThrustSpeed;
		float swingDamage  = primaryWeapon.SwingDamage;
		float swingSpeed   = primaryWeapon.SwingSpeed;

		if (characterObject != null) {
			SkillObject relevantSkill = primaryWeapon.RelevantSkill;
			if (relevantSkill != null) {
				if (relevantSkill == DefaultSkills.OneHanded) {
					thrustDamage *= GetSkillBasedDamageMultiplier(
						characterObject,
						DefaultSkills.OneHanded,
						DefaultSkillEffects.OneHandedDamage
					);
					swingDamage *= GetSkillBasedDamageMultiplier(
						characterObject,
						DefaultSkills.OneHanded,
						DefaultSkillEffects.OneHandedDamage
					);
					thrustSpeed *= GetSkillBasedSpeedMultiplier(
						characterObject,
						DefaultSkills.OneHanded,
						DefaultSkillEffects.OneHandedSpeed
					);
					swingSpeed *= GetSkillBasedSpeedMultiplier(
						characterObject,
						DefaultSkills.OneHanded,
						DefaultSkillEffects.OneHandedSpeed
					);
				} else if (relevantSkill == DefaultSkills.TwoHanded) {
					thrustDamage *= GetSkillBasedDamageMultiplier(
						characterObject,
						DefaultSkills.TwoHanded,
						DefaultSkillEffects.TwoHandedDamage
					);
					swingDamage *= GetSkillBasedDamageMultiplier(
						characterObject,
						DefaultSkills.TwoHanded,
						DefaultSkillEffects.TwoHandedDamage
					);
					thrustSpeed *= GetSkillBasedSpeedMultiplier(
						characterObject,
						DefaultSkills.TwoHanded,
						DefaultSkillEffects.TwoHandedSpeed
					);
					swingSpeed *= GetSkillBasedSpeedMultiplier(
						characterObject,
						DefaultSkills.TwoHanded,
						DefaultSkillEffects.TwoHandedSpeed
					);
				} else if (relevantSkill == DefaultSkills.Polearm) {
					thrustDamage *= GetSkillBasedDamageMultiplier(
						characterObject,
						DefaultSkills.Polearm,
						DefaultSkillEffects.PolearmDamage
					);
					swingDamage *= GetSkillBasedDamageMultiplier(
						characterObject,
						DefaultSkills.Polearm,
						DefaultSkillEffects.PolearmDamage
					);
					thrustSpeed *= GetSkillBasedSpeedMultiplier(
						characterObject,
						DefaultSkills.Polearm,
						DefaultSkillEffects.PolearmSpeed
					);
					swingSpeed *= GetSkillBasedSpeedMultiplier(
						characterObject,
						DefaultSkills.Polearm,
						DefaultSkillEffects.PolearmSpeed
					);
				}
			}
		}

		// Use the original formula from ItemObject.cs
		float thrustEffectiveness = thrustSpeed * thrustDamage * 0.01f;
		float swingEffectiveness  = swingSpeed  * swingDamage  * 0.01f;
		float maxEffectiveness    = MathF.Max(swingEffectiveness, thrustEffectiveness);
		float minEffectiveness    = MathF.Min(swingEffectiveness, thrustEffectiveness);
		float effectiveness =
			((maxEffectiveness + minEffectiveness * minEffectiveness / maxEffectiveness) * 120f +
			 primaryWeapon.Handling                                                      * 15f  +
			 primaryWeapon.WeaponLength                                                  * 20f  +
			 weaponWeight                                                                * 5f) *
			0.01f                                                                              *
			weaponEffectivenessMultiplier;

		return effectiveness;
	}

	/// <summary>
	///     Calculates the effectiveness of a consumable item.
	/// </summary>
	/// <param name="primaryWeapon">                 The primary weapon data. </param>
	/// <param name="weaponEffectivenessMultiplier"> The weapon effectiveness multiplier. </param>
	/// <returns> The calculated effectiveness. </returns>
	private static float CalculateConsumableEffectiveness(
		WeaponComponentData primaryWeapon,
		float               weaponEffectivenessMultiplier
	) {
		float effectiveness =
			(primaryWeapon.MissileDamage * 5.5f  +
			 primaryWeapon.MissileSpeed  * 0.15f +
			 primaryWeapon.MaxDataValue  * 0.6f) *
			weaponEffectivenessMultiplier;
		return effectiveness;
	}

	/// <summary>
	///     Calculates the effectiveness of a shield.
	/// </summary>
	/// <param name="primaryWeapon">                 The primary weapon data. </param>
	/// <param name="weaponEffectivenessMultiplier"> The weapon effectiveness multiplier. </param>
	/// <returns> The calculated effectiveness. </returns>
	private static float CalculateShieldEffectiveness(
		WeaponComponentData primaryWeapon,
		float               weaponEffectivenessMultiplier
	) {
		float effectiveness =
			(primaryWeapon.BodyArmor    * 0.6f +
			 primaryWeapon.ThrustSpeed  * 0.1f +
			 primaryWeapon.MaxDataValue * 0.4f +
			 primaryWeapon.WeaponLength * 0.2f) *
			weaponEffectivenessMultiplier;
		return effectiveness;
	}

	/// <summary>
	///     Calculates the effectiveness of a horse item.
	/// </summary>
	/// <param name="itemObject"> The horse item. </param>
	/// <returns> The calculated effectiveness. </returns>
	private static float CalculateHorseEffectiveness(ItemObject itemObject) {
		HorseComponent horseComponent = itemObject.HorseComponent;
		float effectiveness =
			(horseComponent.ChargeDamage * horseComponent.Speed +
			 horseComponent.Maneuver     * horseComponent.Speed +
			 horseComponent.BodyLength   * itemObject.Weight * 0.025f) *
			(horseComponent.HitPoints + horseComponent.HitPointBonus)  *
			0.0001f;
		return effectiveness;
	}

	private static float GetSkillBasedDamageMultiplier(
		CharacterObject characterObject,
		SkillObject     skill,
		SkillEffect     damageEffect
	)
		=> GetWeaponDamageMultiplier(characterObject, skill, damageEffect);

	private static float GetSkillBasedSpeedMultiplier(
		CharacterObject characterObject,
		SkillObject     skill,
		SkillEffect     speedEffect
	) {
		ExplainedNumber explainedNumber = new(1f);
		int             effectiveSkill  = characterObject.GetSkillValue(skill);
		SkillHelper.AddSkillBonusForCharacter(
			skill,
			speedEffect,
			characterObject,
			ref explainedNumber,
			effectiveSkill,
			false
		);
		return Math.Max(0f, explainedNumber.ResultNumber);
	}

	private static float GetSkillBasedAccuracyMultiplier(
		CharacterObject characterObject,
		SkillObject     skill,
		SkillEffect     accuracyEffect
	) {
		ExplainedNumber explainedNumber = new(1f);
		int             effectiveSkill  = characterObject.GetSkillValue(skill);
		SkillHelper.AddSkillBonusForCharacter(
			skill,
			accuracyEffect,
			characterObject,
			ref explainedNumber,
			effectiveSkill,
			false
		);
		return Math.Max(0f, explainedNumber.ResultNumber);
	}

	/// <summary>
	///     Calculates the weapon damage multiplier based on character skills.
	/// </summary>
	/// <param name="characterObject"> The character. </param>
	/// <param name="skill">           The relevant skill. </param>
	/// <param name="damageEffect">    The damage effect of the skill. </param>
	/// <returns> The weapon damage multiplier. </returns>
	private static float GetWeaponDamageMultiplier(
		CharacterObject characterObject,
		SkillObject     skill,
		SkillEffect     damageEffect
	) {
		ExplainedNumber explainedNumber = new(1f);
		int             effectiveSkill  = characterObject.GetSkillValue(skill);
		SkillHelper.AddSkillBonusForCharacter(
			skill,
			damageEffect,
			characterObject,
			ref explainedNumber,
			effectiveSkill
		);
		return Math.Max(0f, explainedNumber.ResultNumber);
	}
}