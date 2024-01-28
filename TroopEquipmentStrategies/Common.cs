using System;
using System.Linq;
using Bannerlord.DynamicTroop.Extensions;
using Metalama.Patterns.Caching.Aspects;
using Metalama.Patterns.Memoization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.TroopEquipmentStrategies;

public partial class Common {
	public enum WeaponFilterType {
		Bow,
		Arrow,
		CrossBow,
		Bolt,
		Throwing,
		Shield,
		Melee
	}

	private static Common? _instance;

	// 公共静态属性或方法提供全局访问点
	public static Common Instance {
		get {
			_instance ??= new Common();
			return _instance;
		}
	}

	//[Cache]
	public float CalcConsiderValue(CharacterObject soldier, EquipmentElement refEq, EquipmentElement eqToCalc) {
		var factor = eqToCalc.Item.IsCivilian ? 0 : 1;
		factor += 1 + (int)eqToCalc.Item.Tier;
		if (soldier.IsMounted && eqToCalc.Item.IsCouchable()) { factor++; }
		else if (!soldier.IsMounted) {
			if (eqToCalc.Item.IsCouchable())
				factor--;
			else if (eqToCalc.Item.IsBracable()) factor++;
		}

		if (eqToCalc.Item.Culture == null) factor += 1;

		if (soldier.Culture != null && soldier.Culture == eqToCalc.Item.Culture) factor += 1;

		var similarity = CalcWeaponSimilarity(refEq.Item, eqToCalc.Item);

		//var overallSkill = CalcOverallSkill(soldier, eqToCalc.Item);
		var effectiveness = CalcItemObjectEffectiveness(eqToCalc.Item, soldier);
		return factor * similarity * effectiveness;
	}

	[Cache]
	[Obsolete]
	public int CalcOverallSkill(CharacterObject character, ItemObject item) {
		if (!item.HasWeaponComponent) return 0;

		var totalWeightedSkill = 0;
		var totalWeight        = 0;

		foreach (var weaponComponent in item.Weapons) {
			var skillValue = character.GetSkillValue(weaponComponent.RelevantSkill);
			var weight     = 1 + (int)weaponComponent.WeaponTier;

			totalWeightedSkill += skillValue * weight;
			totalWeight        += weight;
		}

		return totalWeight > 0 ? totalWeightedSkill / totalWeight : 0;
	}

	//[Cache]
	private float CalcWeaponSimilarity(ItemObject refEq, ItemObject eqToCalc) {
		if (!refEq.HasWeaponComponent || !eqToCalc.HasWeaponComponent) return 0f;

		try {
			var totalSimilarityScore = refEq.Weapons.Average(weaponDataRef =>
																 eqToCalc.Weapons.Max(weaponDataCalc =>
																	 CompareWeaponData(weaponDataRef,
																		 weaponDataCalc)));
			return totalSimilarityScore;
		}
		catch (Exception e) {
			Global.Error(e.Message);
			return 0f;
		}
	}

	//[Cache]
	private float CalcDamageTypeSimilarity(DamageTypes damageType1, DamageTypes damageType2) {
		return damageType1     == DamageTypes.Invalid || damageType2 == DamageTypes.Invalid ?
				   damageType1 == damageType2 ? 1f : 0.5f :
				   damageType1 == damageType2 ? 1f : 0.75f;
	}

	//[Cache]
	private float CompareWeaponData(WeaponComponentData data1, WeaponComponentData data2) {
		var classSimilarity = data1.WeaponClass == data2.WeaponClass ? 0.5f : 0f;
		var typeSimilarity = Helper.WeaponClassToItemEnumType(data1.WeaponClass) ==
							 Helper.WeaponClassToItemEnumType(data2.WeaponClass)
								 ? 0.5f
								 : 0f;
		var thrustSimilarity     = CalcDamageTypeSimilarity(data1.ThrustDamageType, data2.ThrustDamageType);
		var swingSimilarity      = CalcDamageTypeSimilarity(data1.SwingDamageType,  data2.SwingDamageType);
		var weaponFlagSimilarity = CalcWeaponFlagSimilarity(data1.WeaponFlags, data2.WeaponFlags);

		return (classSimilarity + typeSimilarity) * thrustSimilarity * swingSimilarity * weaponFlagSimilarity;
	}

	//[Cache]
	private float CalcWeaponFlagSimilarity(WeaponFlags flags1, WeaponFlags flags2) {
		var diff                = (ulong)flags1 ^ (ulong)flags2;
		var numberOfDifferences = CountBits(diff);
		var totalFlags          = CountIndividualFlags();
		return 1f - (float)numberOfDifferences / totalFlags;
	}

	[Memoize]
	private int CountIndividualFlags() {
		var count = 0;
		foreach (ulong flag in Enum.GetValues(typeof(WeaponFlags)))
			if (flag != 0 && (flag & (flag - 1)) == 0) // Check if the flag is a power of two
				count++;

		return count;
	}

	//[Cache]
	private int CountBits(ulong number) {
		var count = 0;
		while (number != 0) {
			count  +=  (int)(number & 1);
			number >>= 1;
		}

		return count;
	}

	private float GetWeaponClassEffectivenessModifer(WeaponClass weaponClass) {
		var weaponClassModifier = 1f;
		switch (weaponClass) {
			case WeaponClass.Dagger:
				weaponClassModifier = 0.4f;
				break;

			case WeaponClass.OneHandedSword:
				weaponClassModifier = 0.55f;
				break;

			case WeaponClass.TwoHandedSword:
				weaponClassModifier = 0.6f;
				break;

			case WeaponClass.OneHandedAxe:
				weaponClassModifier = 0.5f;
				break;

			case WeaponClass.TwoHandedAxe:
				weaponClassModifier = 0.55f;
				break;

			case WeaponClass.Mace:
				weaponClassModifier = 0.5f;
				break;

			case WeaponClass.Pick:
				weaponClassModifier = 0.4f;
				break;

			case WeaponClass.TwoHandedMace:
				weaponClassModifier = 0.55f;
				break;

			case WeaponClass.OneHandedPolearm:
				weaponClassModifier = 0.4f;
				break;

			case WeaponClass.TwoHandedPolearm:
				weaponClassModifier = 0.4f;
				break;

			case WeaponClass.LowGripPolearm:
				weaponClassModifier = 0.4f;
				break;

			case WeaponClass.Arrow:
				weaponClassModifier = 3f;
				break;

			case WeaponClass.Bolt:
				weaponClassModifier = 3f;
				break;

			case WeaponClass.Cartridge:
				weaponClassModifier = 3f;
				break;

			case WeaponClass.Bow:
				weaponClassModifier = 0.55f;
				break;

			case WeaponClass.Crossbow:
				weaponClassModifier = 0.57f;
				break;

			case WeaponClass.Stone:
				weaponClassModifier = 0.1f;
				break;

			case WeaponClass.Boulder:
				weaponClassModifier = 0.1f;
				break;

			case WeaponClass.ThrowingAxe:
				weaponClassModifier = 0.25f;
				break;

			case WeaponClass.ThrowingKnife:
				weaponClassModifier = 0.2f;
				break;

			case WeaponClass.Javelin:
				weaponClassModifier = 0.28f;
				break;

			case WeaponClass.Pistol:
				weaponClassModifier = 1f;
				break;

			case WeaponClass.Musket:
				weaponClassModifier = 1f;
				break;

			case WeaponClass.SmallShield:
				weaponClassModifier = 0.4f;
				break;

			case WeaponClass.LargeShield:
				weaponClassModifier = 0.5f;
				break;
		}

		return weaponClassModifier;
	}

	//[Cache]
	private float CalcItemObjectEffectiveness(ItemObject item, CharacterObject character) {
		return item.HasWeaponComponent
				   ? item.Weapons.Sum(weaponComponent =>
										  CalcWeaponComponentDataEffectiveness(weaponComponent,
																			   item.Weight,
																			   character)) /
					 item.Weapons.Count
				   : item.Effectiveness;
	}

	//[Cache]
	private float CalcWeaponComponentDataEffectiveness(WeaponComponentData wcd,
													   float               weight,
													   CharacterObject     character) {
		// Copy from ItemObject.CalculateEffectiveness
		var finalEffectiveness  = 1f;
		var weaponClassModifier = GetWeaponClassEffectivenessModifer(wcd.WeaponClass);
		if (wcd.IsRangedWeapon) {
			finalEffectiveness = wcd.IsConsumable
									 ? (GetModifiedMissileDamage(wcd, character) * wcd.MissileSpeed * 1.775f +
										GetModifiedAccuracy(wcd, character)      * wcd.MaxDataValue * 25f    +
										wcd.WeaponLength                         * 4f) *
									   0.006944f                                       *
									   wcd.MaxDataValue                                *
									   weaponClassModifier
									 : (wcd.MissileSpeed * GetModifiedMissileDamage(wcd, character) * 1.75f +
										GetModifiedMelee(wcd, character, true, true) *
										GetModifiedAccuracy(wcd, character)          *
										0.3f)           *
									   0.01f            *
									   wcd.MaxDataValue *
									   weaponClassModifier;
		}
		else if (wcd.IsMeleeWeapon) {
			var thrustEffectiveness = GetModifiedMelee(wcd, character, true,  true) *
									  GetModifiedMelee(wcd, character, false, true) *
									  0.01f;
			var swingEffectiveness = GetModifiedMelee(wcd, character, true,  false) *
									 GetModifiedMelee(wcd, character, false, false) *
									 0.01f;
			var maxCombatEffectiveness = Math.Max(swingEffectiveness, thrustEffectiveness);
			var minCombatEffectiveness = Math.Min(swingEffectiveness, thrustEffectiveness);
			finalEffectiveness =
				((maxCombatEffectiveness + minCombatEffectiveness * minCombatEffectiveness / maxCombatEffectiveness) *
				 120f                   +
				 wcd.Handling     * 15f +
				 wcd.WeaponLength * 20f +
				 weight           * 5f) *
				0.01f                   *
				weaponClassModifier;
		}
		else if (wcd.IsConsumable) {
			finalEffectiveness =
				(GetModifiedMissileDamage(wcd, character) * 550f +
				 GetModifiedMissileSpeed(wcd, character)  * 15f  +
				 wcd.MaxDataValue                         * 60f) *
				0.01f                                            *
				weaponClassModifier;
		}
		else if (wcd.IsShield) {
			finalEffectiveness =
				(wcd.BodyArmor * 60f + wcd.ThrustSpeed * 10f + wcd.MaxDataValue * 40f + wcd.WeaponLength * 20f) *
				0.01f                                                                                           *
				weaponClassModifier;
		}

		return finalEffectiveness;
	}

	private SkillEffect? GetSpeedSkillEffectByWeaponClass(WeaponComponentData wcd) {
		return Helper.WeaponClassToItemEnumType(wcd.WeaponClass) switch {
				   ItemObject.ItemTypeEnum.OneHandedWeapon => DefaultSkillEffects.OneHandedSpeed,
				   ItemObject.ItemTypeEnum.TwoHandedWeapon => DefaultSkillEffects.TwoHandedSpeed,
				   ItemObject.ItemTypeEnum.Polearm         => DefaultSkillEffects.PolearmSpeed,
				   ItemObject.ItemTypeEnum.Thrown          => DefaultSkillEffects.ThrowingSpeed,
				   _                                       => null
			   };
	}

	private SkillEffect? GetDamageSkillEffectByWeaponClass(WeaponComponentData wcd) {
		return Helper.WeaponClassToItemEnumType(wcd.WeaponClass) switch {
				   ItemObject.ItemTypeEnum.OneHandedWeapon => DefaultSkillEffects.OneHandedDamage,
				   ItemObject.ItemTypeEnum.TwoHandedWeapon => DefaultSkillEffects.TwoHandedDamage,
				   ItemObject.ItemTypeEnum.Polearm         => DefaultSkillEffects.PolearmDamage,
				   ItemObject.ItemTypeEnum.Thrown          => DefaultSkillEffects.ThrowingDamage,
				   ItemObject.ItemTypeEnum.Bow             => DefaultSkillEffects.BowDamage,
				   ItemObject.ItemTypeEnum.Arrows          => DefaultSkillEffects.BowDamage,
				   _                                       => null
			   };
	}

	//[Cache]
	private float GetModifiedMelee(WeaponComponentData wcd, CharacterObject character, bool isSpeed, bool isThrust) {
		var skillEffect = isSpeed ? GetSpeedSkillEffectByWeaponClass(wcd) : GetDamageSkillEffectByWeaponClass(wcd);
		if (skillEffect == null) return isThrust ? wcd.ThrustSpeed : wcd.SwingSpeed;

		var skillValue = character.GetSkillValue(wcd.RelevantSkill);
		var bonus = (100 +
					 (skillEffect.PrimaryRole == SkillEffect.PerkRole.Personal
						  ? skillEffect.GetPrimaryValue(skillValue)
						  : skillEffect.GetSecondaryValue(skillValue))) /
					100;
		return bonus * (isThrust ? wcd.ThrustSpeed : wcd.SwingSpeed);
	}

	//[Cache]
	private float GetModifiedMissileDamage(WeaponComponentData wcd, CharacterObject character) {
		var damageSkillEffect = GetDamageSkillEffectByWeaponClass(wcd);
		if (damageSkillEffect == null) return wcd.MissileDamage;

		var skillValue  = character.GetSkillValue(wcd.RelevantSkill);
		var damageBonus = (100 + damageSkillEffect.GetPrimaryValue(skillValue)) / 100;
		return wcd.MissileDamage * damageBonus;
	}

	//[Cache]
	private float GetModifiedMissileSpeed(WeaponComponentData wcd, CharacterObject character) {
		var speedSkillEffect = GetSpeedSkillEffectByWeaponClass(wcd);
		if (speedSkillEffect == null) return wcd.MissileSpeed;

		var skillValue = character.GetSkillValue(wcd.RelevantSkill);
		var speedBonus = (100 + speedSkillEffect.GetPrimaryValue(skillValue)) / 100;
		return wcd.MissileSpeed * speedBonus;
	}

	//[Cache]
	private float GetModifiedAccuracy(WeaponComponentData wcd, CharacterObject character) {
		var accuracySkillEffect = wcd.WeaponClass switch {
									  WeaponClass.Bow      => DefaultSkillEffects.BowAccuracy,
									  WeaponClass.Arrow    => DefaultSkillEffects.BowAccuracy,
									  WeaponClass.Crossbow => DefaultSkillEffects.CrossbowAccuracy,
									  WeaponClass.Bolt     => DefaultSkillEffects.CrossbowAccuracy,
									  _                    => null
								  };
		if (wcd.RelevantSkill == DefaultSkills.Throwing) accuracySkillEffect = DefaultSkillEffects.ThrowingAccuracy;

		if (accuracySkillEffect == null) return wcd.Accuracy;

		var skillValue    = character.GetSkillValue(wcd.RelevantSkill);
		var accuracyBonus = (100 + accuracySkillEffect.GetPrimaryValue(skillValue)) / 100;
		return wcd.Accuracy * accuracyBonus;
	}

	//[Cache]
	public WeaponFilterType GetWeaponFilterType(ItemObject item) {
		if (item.IsBow()) return WeaponFilterType.Bow;

		if (item.IsArrow()) return WeaponFilterType.Arrow;

		return item.IsCrossBow() ? WeaponFilterType.CrossBow :
			   item.IsBolt()     ? WeaponFilterType.Bolt :
			   item.IsThrowing() ? WeaponFilterType.Throwing :
			   item.IsShield()   ? WeaponFilterType.Shield : WeaponFilterType.Melee;
	}
}