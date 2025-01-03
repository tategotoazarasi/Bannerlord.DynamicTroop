using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace DTES2.Extensions;

/// <summary>
///     为 <see cref="EquipmentElement" /> 提供扩展方法，用于根据角色技能和物品修正值（ItemModifier）计算综合有效性。
/// </summary>
public static class EquipmentElementExtension {
	/// <summary>
	///     计算装备（盔甲、武器或马等）的综合有效性，并纳入角色技能与 <see cref="ItemModifier" /> 对伤害、速度、防御、精度等属性的修正。
	/// </summary>
	/// <param name="equipmentElement">要计算的装备元素（可为空）。</param>
	/// <param name="characterObject">使用该装备的角色（可为空）。</param>
	/// <returns>最终计算得到的有效性数值。</returns>
	public static float CalculateEffectiveness(
		this EquipmentElement equipmentElement,
		CharacterObject?      characterObject
	) {
		string cacheKey = $"{
			nameof(EquipmentElementExtension)
		}.{
			nameof(CalculateEffectiveness)
		}:{
			equipmentElement.Item?.StringId ?? ""
		}:{
			equipmentElement.ItemModifier?.StringId ?? ""
		}:{
			equipmentElement.IsQuestItem
		}:{
			equipmentElement.CosmeticItem?.StringId ?? ""
		}";
		return CacheManager.GetOrAdd(
			() => {
				// 基础有效性初始为 1f
				float finalEffectiveness = 1f;

				// 根据物品类型，执行相应的计算逻辑
				ItemObject itemObj = equipmentElement.Item;
				if (itemObj.HasArmorComponent) {
					// 盔甲的有效性
					finalEffectiveness = CalculateArmorEffectiveness(equipmentElement, itemObj.Type);
				} else if (itemObj.WeaponComponent != null) {
					// 武器的有效性
					// 注意：WeaponComponentData 有 PrimaryWeapon、SecondaryWeapon 等，这里仅示例 PrimaryWeapon
					WeaponComponentData weaponData = itemObj.WeaponComponent.PrimaryWeapon;
					if (weaponData != null) {
						finalEffectiveness = CalculateWeaponEffectiveness(
							equipmentElement,
							weaponData,
							characterObject
						);
					}
				} else if (itemObj.HorseComponent != null) {
					// 马匹（或马具）的有效性
					finalEffectiveness = CalculateHorseEffectiveness(equipmentElement);
				}

				return finalEffectiveness;
			},
			cacheKey
		);
	}

	/// <summary>
	///     计算盔甲的基础有效性，并应用 ItemModifier 修正后的防御值。
	/// </summary>
	/// <param name="equipmentElement">当前装备元素。</param>
	/// <param name="itemType">物品类型。</param>
	/// <returns>盔甲的有效性数值。</returns>
	private static float CalculateArmorEffectiveness(
		EquipmentElement        equipmentElement,
		ItemObject.ItemTypeEnum itemType
	) {
		// 判断是否是马具
		if (itemType == ItemObject.ItemTypeEnum.HorseHarness) {
			// 原逻辑：num = armorComponent.BodyArmor * 1.67f
			// 但要用经过 modifier 修正后的马身防御
			int mountBodyArmor = equipmentElement.GetModifiedMountBodyArmor();
			return mountBodyArmor * 1.67f;
		}

		// 普通人体盔甲
		// 原逻辑：
		// armorValue = (HeadArmor*34 + BodyArmor*42 + LegArmor*12 + ArmArmor*12) * 0.03f
		// 这里都要使用修正后的数值
		int headArmor = equipmentElement.GetModifiedHeadArmor();
		int bodyArmor = equipmentElement.GetModifiedBodyArmor();
		int legArmor  = equipmentElement.GetModifiedLegArmor();
		int armArmor  = equipmentElement.GetModifiedArmArmor();

		float totalArmorValue = headArmor * 34f + bodyArmor * 42f + legArmor * 12f + armArmor * 12f;

		return totalArmorValue * 0.03f;
	}

	/// <summary>
	///     计算武器的有效性，并考虑技能与 <see cref="ItemModifier" /> 的加成。
	/// </summary>
	/// <param name="eq">当前装备元素。</param>
	/// <param name="weaponData">武器组件数据。</param>
	/// <param name="characterObject">使用该武器的角色。</param>
	/// <returns>武器的有效性数值。</returns>
	private static float CalculateWeaponEffectiveness(
		EquipmentElement    eq,
		WeaponComponentData weaponData,
		CharacterObject?    characterObject
	) {
		// 1. 获取武器类型的修正倍率
		float weaponClassMultiplier = GetWeaponClassMultiplier(weaponData.WeaponClass);

		// 2. 根据远程 / 近战 / 消耗品 / 盾牌等差异，先算一个 baseValue
		float baseValue;
		if (weaponData.IsRangedWeapon) {
			if (weaponData.IsConsumable) {
				// 远程 + 消耗品（例如箭、弹药）
				baseValue = CalculateRangedConsumableEffectiveness(eq, weaponData);
			} else {
				// 远程 + 非消耗品（例如弓、弩）
				baseValue = CalculateRangedWeaponEffectiveness(eq, weaponData);
			}
		} else if (weaponData.IsMeleeWeapon) {
			// 近战武器
			baseValue = CalculateMeleeWeaponEffectiveness(eq, weaponData);
		} else if (weaponData.IsConsumable) {
			// 非射击类的消耗品（如石块、特殊投掷道具等）
			baseValue = CalculatePureConsumableEffectiveness(eq, weaponData);
		} else if (weaponData.IsShield) {
			// 盾牌
			baseValue = CalculateShieldEffectiveness(eq, weaponData);
		} else {
			// 兜底
			baseValue = 1f;
		}

		// 3. weaponBaseEffectiveness = baseValue * weaponClassMultiplier
		float weaponBaseEffectiveness = baseValue * weaponClassMultiplier;

		// 4. 叠加角色技能影响：伤害 / 速度 / 精度等
		float finalValue = ApplyWeaponSkillAdjustments(
			eq,
			weaponData,
			characterObject,
			weaponBaseEffectiveness
		);

		return finalValue;
	}

	/// <summary>
	///     近战武器基础有效性计算，使用装备元素的修正属性（伤害、速度、长度、Handling、重量等）。
	/// </summary>
	private static float CalculateMeleeWeaponEffectiveness(EquipmentElement eq, WeaponComponentData weaponData) {
		// 原逻辑（对于 PrimaryWeapon 的 thrust / swing）：
		// float thrustVal = (thrustSpeed * thrustDamage) * 0.01f;
		// float swingVal  = (swingSpeed  * swingDamage ) * 0.01f;
		// float maxVal = MathF.Max(thrustVal, swingVal);
		// float minVal = MathF.Min(thrustVal, swingVal);
		// 结果： (maxVal + minVal * minVal / maxVal) * 120f + (Handling * 15f) + (WeaponLength * 20f) + (Weight * 5f)
		// 之后再 * 0.01f

		// 先获取经过 ItemModifier 修正后的数值：
		int   thrustSpeed  = eq.GetModifiedThrustSpeedForUsage(0);
		int   thrustDamage = eq.GetModifiedThrustDamageForUsage(0);
		int   swingSpeed   = eq.GetModifiedSwingSpeedForUsage(0);
		int   swingDamage  = eq.GetModifiedSwingDamageForUsage(0);
		int   handling     = eq.GetModifiedHandlingForUsage(0);
		int   weaponLength = weaponData.WeaponLength; // weaponLength 本身就保存于 weaponData，不会被 itemModifier 改变（但可自行扩展）
		float weight       = eq.GetEquipmentElementWeight(); // eq 自身重量

		float thrustVal = thrustSpeed * thrustDamage * 0.01f;
		float swingVal  = swingSpeed  * swingDamage  * 0.01f;
		float maxVal    = MathF.Max(thrustVal, swingVal);
		float minVal    = MathF.Min(thrustVal, swingVal);

		float combined = (maxVal + minVal * minVal / (maxVal == 0 ? 1 : maxVal)) * 120f +
						 handling                                                * 15f  +
						 weaponLength                                            * 20f  +
						 weight                                                  * 5f;

		float result = combined * 0.01f;
		return result;
	}

	/// <summary>
	///     非消耗型远程武器基础有效性（如弓、弩），使用装备元素的修正属性。
	/// </summary>
	private static float CalculateRangedWeaponEffectiveness(EquipmentElement eq, WeaponComponentData weaponData) {
		// 原逻辑：
		// result = ((MissileSpeed * MissileDamage) * 1.75f + (ThrustSpeed * Accuracy) * 0.3f) * 0.01f * MaxDataValue
		int missileSpeed  = eq.GetModifiedMissileSpeedForUsage(0);
		int missileDamage = eq.GetModifiedMissileDamageForUsage(0);
		int thrustSpeed   = eq.GetModifiedThrustSpeedForUsage(0);
		int accuracy      = weaponData.Accuracy; // Accuracy 不会被 itemModifier 改变（可自行扩展）
		int maxDataValue  = weaponData.MaxDataValue;

		float baseCalc = missileSpeed * missileDamage * 1.75f + thrustSpeed * accuracy * 0.3f;

		float result = baseCalc * 0.01f * maxDataValue;
		return result;
	}

	/// <summary>
	///     远程 + 消耗品（例如箭、弹药等）基础有效性计算。
	/// </summary>
	private static float CalculateRangedConsumableEffectiveness(EquipmentElement eq, WeaponComponentData weaponData) {
		// 原逻辑：
		// result = ((MissileDamage * MissileSpeed) * 1.775f + (Accuracy * MaxDataValue) * 25f + (WeaponLength * 4f))
		//          * 0.006944f * MaxDataValue
		int missileDamage = eq.GetModifiedMissileDamageForUsage(0);
		int missileSpeed  = eq.GetModifiedMissileSpeedForUsage(0);
		int accuracy      = weaponData.Accuracy;
		int maxDataValue  = weaponData.MaxDataValue;
		int weaponLength  = weaponData.WeaponLength;

		float part1 = missileDamage * missileSpeed * 1.775f;
		float part2 = accuracy      * maxDataValue * 25f;
		float part3 = weaponLength  * 4f;

		float combined = (part1 + part2 + part3) * 0.006944f * maxDataValue; // 0.006944f ≈ 1/144
		return combined;
	}

	/// <summary>
	///     非射击类的消耗品（如投掷石块、特殊道具）基础有效性计算。
	/// </summary>
	private static float CalculatePureConsumableEffectiveness(EquipmentElement eq, WeaponComponentData weaponData) {
		// 原逻辑：
		// result = ((MissileDamage * 550f) + (MissileSpeed * 15f) + (MaxDataValue * 60f)) * 0.01f
		int missileDamage = eq.GetModifiedMissileDamageForUsage(0);
		int missileSpeed  = eq.GetModifiedMissileSpeedForUsage(0);
		int maxDataValue  = weaponData.MaxDataValue;

		float baseValue = missileDamage * 550f + missileSpeed * 15f + maxDataValue * 60f;

		return baseValue * 0.01f;
	}

	/// <summary>
	///     盾牌的基础有效性计算。
	/// </summary>
	private static float CalculateShieldEffectiveness(EquipmentElement eq, WeaponComponentData weaponData) {
		// 原逻辑：
		// result = ((BodyArmor * 60f) + (ThrustSpeed * 10f) + (MaxDataValue * 40f) + (WeaponLength * 20f)) * 0.01f
		int bodyArmor    = eq.GetModifiedBodyArmor(); // 盾牌的 BodyArmor 在 WeaponComponent 的 PrimaryWeapon.BodyArmor 中
		int thrustSpeed  = eq.GetModifiedThrustSpeedForUsage(0);
		int maxDataValue = weaponData.MaxDataValue;
		int weaponLength = weaponData.WeaponLength;

		float baseValue = bodyArmor * 60f + thrustSpeed * 10f + maxDataValue * 40f + weaponLength * 20f;

		return baseValue * 0.01f;
	}

	/// <summary>
	///     根据 WeaponClass 返回原逻辑中的修正倍率（weaponClassMultiplier）。
	/// </summary>
	private static float GetWeaponClassMultiplier(WeaponClass weaponClass) {
		// 对应原代码中 num2 = ...
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

			case WeaponClass.Arrow:
			case WeaponClass.Bolt:
			case WeaponClass.Cartridge:
				return 3f;

			case WeaponClass.Bow: return 0.55f;

			case WeaponClass.Crossbow: return 0.57f;

			case WeaponClass.Stone:
			case WeaponClass.Boulder:
				return 0.1f;

			case WeaponClass.ThrowingAxe: return 0.25f;

			case WeaponClass.ThrowingKnife: return 0.2f;

			case WeaponClass.Javelin: return 0.28f;

			case WeaponClass.Pistol:
			case WeaponClass.Musket:
				return 1f;

			case WeaponClass.SmallShield: return 0.4f;

			case WeaponClass.LargeShield: return 0.5f;

			default: return 1f;
		}
	}

	/// <summary>
	///     根据武器关联的技能，为最终武器有效性进行加成。
	///     例如：单手武器加 OneHandedDamage, OneHandedSpeed；弓箭加 BowDamage, BowAccuracy 等。
	/// </summary>
	private static float ApplyWeaponSkillAdjustments(
		EquipmentElement    eq,
		WeaponComponentData weaponData,
		CharacterObject?    characterObject,
		float               weaponEffectiveness
	) {
		if (characterObject == null ||
			weaponData      == null) {
			return weaponEffectiveness;
		}

		// 获取武器关联的技能
		SkillObject? skill = weaponData.RelevantSkill;
		if (skill == null) {
			return weaponEffectiveness;
		}

		// 使用 ExplainedNumber 来叠加说明
		ExplainedNumber explained = new(weaponEffectiveness);

		// 根据不同的技能对象，加不同的 perk 效果（伤害 / 速度 / 精度）
		if (skill == DefaultSkills.OneHanded) {
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.OneHandedDamage,
				characterObject,
				ref explained
			);
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.OneHandedSpeed,
				characterObject,
				ref explained
			);
		} else if (skill == DefaultSkills.TwoHanded) {
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.TwoHandedDamage,
				characterObject,
				ref explained
			);
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.TwoHandedSpeed,
				characterObject,
				ref explained
			);
		} else if (skill == DefaultSkills.Polearm) {
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.PolearmDamage,
				characterObject,
				ref explained
			);
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.PolearmSpeed,
				characterObject,
				ref explained
			);
		} else if (skill == DefaultSkills.Bow) {
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.BowDamage,
				characterObject,
				ref explained
			);
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.BowAccuracy,
				characterObject,
				ref explained
			);
		} else if (skill == DefaultSkills.Throwing) {
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.ThrowingDamage,
				characterObject,
				ref explained
			);
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.ThrowingSpeed,
				characterObject,
				ref explained
			);
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.ThrowingAccuracy,
				characterObject,
				ref explained
			);
		} else if (skill == DefaultSkills.Crossbow) {
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.CrossbowAccuracy,
				characterObject,
				ref explained
			);
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.CrossbowReloadSpeed,
				characterObject,
				ref explained
			);
		}
		// 如有更多技能需求，可继续扩展

		return explained.ResultNumber;
	}

	/// <summary>
	///     计算马匹或马具的有效性，并使用修正后的速度、冲撞伤害、生命值等。
	/// </summary>
	private static float CalculateHorseEffectiveness(EquipmentElement eq) {
		// 原逻辑：
		// num = ((ChargeDamage * Speed + Maneuver * Speed) + (BodyLength * Weight * 0.025f))
		//       * (HitPoints + HitPointBonus) * 0.0001f
		// 此处要用修正后的冲撞、速度、maneuver、HP 等
		HorseComponent horseComp = eq.Item?.HorseComponent;
		if (horseComp == null) {
			return 0f;
		}

		int   chargeDamage = eq.GetModifiedMountCharge(in eq);
		int   speed        = eq.GetModifiedMountSpeed(in eq);
		int   maneuver     = eq.GetModifiedMountManeuver(in eq);
		int   hitPoints    = eq.GetModifiedMountHitPoints();
		float bodyLength   = horseComp.BodyLength;
		float finalWeight  = eq.GetEquipmentElementWeight(); // 实际上马鞍不一定有 stackCount，但这里使用 eq 重量以保证统一处理

		float part1 = chargeDamage * speed + maneuver * speed;
		float part2 = bodyLength * finalWeight * 0.025f;
		float sum   = part1 + part2;

		float totalHP = hitPoints; // 马的额外 HitPointBonus 已在 eq.GetModifiedMountHitPoints() 内考虑

		return sum * totalHP * 0.0001f;
	}
}