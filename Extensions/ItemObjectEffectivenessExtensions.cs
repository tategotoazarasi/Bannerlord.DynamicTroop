#region
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
#endregion
namespace DTES2.Extensions;

/// <summary>
///     为 <see cref="ItemObject" /> 提供扩展方法，用于根据角色技能计算物品的综合有效性。
/// </summary>
public static class ItemObjectEffectivenessExtension {
	/// <summary>
	///     计算物品（盔甲、武器或马具等）的综合有效性，并纳入角色技能对伤害、速度、精度等属性的修正。
	/// </summary>
	/// <param name="itemObject">待计算的物品（可为空）。</param>
	/// <param name="characterObject">使用该物品的角色（可为空）。</param>
	/// <returns>返回计算后的有效性数值。</returns>
	public static float CalculateEffectiveness(
		this ItemObject? itemObject,
		CharacterObject? characterObject
	) {
		// 若物品为空，直接返回 0。
		if (itemObject == null) {
			return 0f;
		}

		// 整体的计算结果，根据原逻辑，初始为 1f。
		float finalEffectiveness = 1f;

		// 1. 若有盔甲部分则进行盔甲计算
		if (itemObject.ArmorComponent != null) {
			finalEffectiveness = CalculateArmorEffectiveness(
				itemObject.ArmorComponent,
				itemObject.Type
			);
		}

		// 2. 若存在武器组件，则进行武器计算
		if (itemObject.WeaponComponent != null) {
			finalEffectiveness = CalculateWeaponEffectiveness(
				itemObject.WeaponComponent.PrimaryWeapon,
				itemObject.Weight,
				characterObject
			);
		}

		// 3. 若存在马匹组件，则进行马具计算
		if (itemObject.HorseComponent != null) {
			finalEffectiveness = CalculateHorseEffectiveness(
				itemObject.HorseComponent,
				itemObject.Weight
			);
		}

		return finalEffectiveness;
	}

	/// <summary>
	///     计算盔甲物品的有效性。
	/// </summary>
	/// <param name="armorComponent">盔甲组件。</param>
	/// <param name="itemType">物品类型。</param>
	/// <returns>盔甲有效性数值。</returns>
	private static float CalculateArmorEffectiveness(
		ArmorComponent          armorComponent,
		ItemObject.ItemTypeEnum itemType
	) {
		// 参考原方法对盔甲的处理逻辑
		if (itemType == ItemObject.ItemTypeEnum.HorseHarness) {
			// 马鞍
			return armorComponent.BodyArmor * 1.67f;
		}
		// 通常的盔甲计算方式
		float totalArmorValue =
			armorComponent.HeadArmor * 34f +
			armorComponent.BodyArmor * 42f +
			armorComponent.LegArmor  * 12f +
			armorComponent.ArmArmor  * 12f;

		// 原逻辑里乘以 0.03f
		return totalArmorValue * 0.03f;
	}

	/// <summary>
	///     根据武器类型（近战、远程、消耗品、盾牌等）和角色技能，计算武器的有效性。
	/// </summary>
	/// <param name="weaponData">武器主组件。</param>
	/// <param name="itemWeight">物品重量。</param>
	/// <param name="characterObject">使用武器的角色。</param>
	/// <returns>武器有效性数值。</returns>
	private static float CalculateWeaponEffectiveness(
		WeaponComponentData weaponData,
		float               itemWeight,
		CharacterObject?    characterObject
	) {
		// 先根据 WeaponClass 做一个基础系数（原代码用 num2）
		float weaponClassMultiplier = GetWeaponClassMultiplier(weaponData.WeaponClass);

		// 根据原逻辑不同武器类型的计算差异
		float baseValue;

		// 如果是远程武器
		if (weaponData.IsRangedWeapon) {
			if (weaponData.IsConsumable) {
				// 如箭矢、弹药类物品
				baseValue = CalculateRangedConsumableEffectiveness(weaponData);
			}
			else {
				// 如弓、弩（非消耗品）
				baseValue = CalculateRangedWeaponEffectiveness(weaponData);
			}
		}
		else if (weaponData.IsMeleeWeapon) {
			baseValue = CalculateMeleeWeaponEffectiveness(weaponData, itemWeight);
		}
		else if (weaponData.IsConsumable) {
			// 可能是投掷标枪之类的？
			baseValue = CalculatePureConsumableEffectiveness(weaponData);
		}
		else if (weaponData.IsShield) {
			baseValue = CalculateShieldEffectiveness(weaponData);
		}
		else {
			// 兜底
			baseValue = 1f;
		}

		// 合并武器类型修正
		float weaponEffectiveness = baseValue * weaponClassMultiplier;

		// 处理技能对武器最终效果（伤害、速度、准确度等）的加成
		weaponEffectiveness = ApplyWeaponSkillAdjustments(
			weaponData,
			characterObject,
			weaponEffectiveness
		);

		return weaponEffectiveness;
	}

	/// <summary>
	///     近战武器的基础有效性计算。
	/// </summary>
	/// <param name="weaponData">武器组件。</param>
	/// <param name="itemWeight">武器重量。</param>
	/// <returns>近战武器的基础数值。</returns>
	private static float CalculateMeleeWeaponEffectiveness(
		WeaponComponentData weaponData,
		float               itemWeight
	) {
		// 原逻辑：
		// thrustDamage * thrustSpeed * 0.01f
		// swingDamage  * swingSpeed  * 0.01f
		float thrustValue = weaponData.ThrustSpeed * weaponData.ThrustDamage * 0.01f;
		float swingValue  = weaponData.SwingSpeed  * weaponData.SwingDamage  * 0.01f;

		float maxValue = MathF.Max(thrustValue, swingValue);
		float minValue = MathF.Min(thrustValue, swingValue);

		// 原逻辑里：num = ((maxValue + minValue * minValue / maxValue) * 120f
		//     + handling * 15f + weaponLength * 20f + Weight * 5f) * 0.01f * num2;
		// 其中 num2 已在外部处理。这里先算出 * 0.01f 之前的值
		float combined =
			(maxValue + minValue * minValue / maxValue) * 120f +
			weaponData.Handling                         * 15f  +
			weaponData.WeaponLength                     * 20f  +
			itemWeight                                  * 5f;
		float result = combined * 0.01f;
		return result;
	}

	/// <summary>
	///     非消耗型远程武器的基础有效性计算（如弓、弩）。
	/// </summary>
	private static float CalculateRangedWeaponEffectiveness(WeaponComponentData weaponData) {
		// 原逻辑：
		// 如果不是消耗品的远程武器：
		// num = ((MissileSpeed * MissileDamage) * 1.75f + (ThrustSpeed * Accuracy) * 0.3f) * 0.01f * MaxDataValue
		float baseValue =
			weaponData.MissileSpeed * weaponData.MissileDamage * 1.75f +
			weaponData.ThrustSpeed  * weaponData.Accuracy      * 0.3f;
		float final = baseValue * 0.01f * weaponData.MaxDataValue;
		return final;
	}

	/// <summary>
	///     远程武器的可消耗型基础有效性计算（如箭矢、弹药）。
	/// </summary>
	private static float CalculateRangedConsumableEffectiveness(WeaponComponentData weaponData) {
		// 原逻辑：
		// num = ((MissileDamage * MissileSpeed) * 1.775f + (Accuracy * MaxDataValue) * 25f + WeaponLength * 4f)
		//      * 0.006944f * MaxDataValue
		// 解释：0.006944f = 1/144
		float part1 = weaponData.MissileDamage * weaponData.MissileSpeed * 1.775f;
		float part2 = weaponData.Accuracy      * weaponData.MaxDataValue * 25f;
		float part3 = weaponData.WeaponLength  * 4f;

		float combined = (part1 + part2 + part3) * 0.006944f * weaponData.MaxDataValue;
		return combined;
	}

	/// <summary>
	///     纯消耗品（不具备远程射击特性，比如可能是投掷石块或其他道具）的有效性。
	/// </summary>
	private static float CalculatePureConsumableEffectiveness(WeaponComponentData weaponData) {
		// 原逻辑：
		// num = ((MissileDamage * 550f) + (MissileSpeed * 15f) + (MaxDataValue * 60f)) * 0.01f
		float baseValue =
			weaponData.MissileDamage * 550f + weaponData.MissileSpeed * 15f + weaponData.MaxDataValue * 60f;

		return baseValue * 0.01f;
	}

	/// <summary>
	///     盾牌的基础有效性计算。
	/// </summary>
	private static float CalculateShieldEffectiveness(WeaponComponentData weaponData) {
		// 原逻辑：
		// num = ((BodyArmor * 60f) + (ThrustSpeed * 10f) + (MaxDataValue * 40f) + (WeaponLength * 20f))
		//       * 0.01f
		float baseValue =
			weaponData.BodyArmor    * 60f +
			weaponData.ThrustSpeed  * 10f +
			weaponData.MaxDataValue * 40f +
			weaponData.WeaponLength * 20f;
		return baseValue * 0.01f;
	}

	/// <summary>
	///     根据 <see cref="WeaponClass" /> 返回原逻辑中的修正倍率（原变量 num2）。
	/// </summary>
	/// <param name="weaponClass">武器类型枚举。</param>
	/// <returns>对应的倍率。</returns>
	private static float GetWeaponClassMultiplier(WeaponClass weaponClass) {
		// 对应原代码大段 switch (primaryWeapon.WeaponClass) { ... num2 = ... }
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
	///     将技能对武器伤害、速度、精度等属性的修正应用到最终计算数值上。
	///     若角色或技能无效，则直接返回 weaponEffectiveness。
	/// </summary>
	/// <param name="weaponData">武器组件数据。</param>
	/// <param name="characterObject">角色。</param>
	/// <param name="weaponEffectiveness">基础的武器有效性（已含 WeaponClassMultiplier）。</param>
	/// <returns>应用技能后最终的武器有效性。</returns>
	private static float ApplyWeaponSkillAdjustments(
		WeaponComponentData weaponData,
		CharacterObject?    characterObject,
		float               weaponEffectiveness
	) {
		if (characterObject == null || weaponData == null) {
			return weaponEffectiveness;
		}

		// 获取武器关联的技能
		SkillObject? skill = weaponData.RelevantSkill;
		if (skill == null) {
			return weaponEffectiveness;
		}

		// 我们创建一个 ExplainedNumber，用于让技能加成叠加在当前基础效果之上
		ExplainedNumber explained = new ExplainedNumber(weaponEffectiveness);

		// 根据武器类型判断要加的“伤害”加成、还是“速度”或“精度”加成
		// 示例：若是 OneHanded，则加 OneHandedDamage 与 OneHandedSpeed
		// 若是 Bow 则加 BowDamage 与 BowAccuracy
		// 可按需组合
		if (skill == DefaultSkills.OneHanded) {
			// 伤害
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.OneHandedDamage,
				characterObject,
				ref explained
			);
			// 速度
			SkillHelper.AddSkillBonusForCharacter(
				skill,
				DefaultSkillEffects.OneHandedSpeed,
				characterObject,
				ref explained
			);
		}
		else if (skill == DefaultSkills.TwoHanded) {
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
		}
		else if (skill == DefaultSkills.Polearm) {
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
		}
		else if (skill == DefaultSkills.Bow) {
			// 弓：伤害与精度
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
		}
		else if (skill == DefaultSkills.Throwing) {
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
		}
		else if (skill == DefaultSkills.Crossbow) {
			// 这里原生只有 ReloadSpeed、Accuracy
			// 若想兼容伤害，需要自行在 DefaultSkillEffects 等处扩展
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
		// 若有其他技能需求，可在此处继续扩展

		// 应用后结果
		return explained.ResultNumber;
	}

	/// <summary>
	///     计算马匹组件的有效性（原逻辑中若物品为马具、战马等）。
	/// </summary>
	/// <param name="horseComponent">马匹组件。</param>
	/// <param name="itemWeight">物品重量。</param>
	/// <returns>马匹有效性数值。</returns>
	private static float CalculateHorseEffectiveness(
		HorseComponent horseComponent,
		float          itemWeight
	) {
		// 原逻辑：
		// num = ((ChargeDamage * Speed + Maneuver * Speed) + BodyLength * Weight * 0.025f)
		//       * (HitPoints + HitPointBonus) * 0.0001f
		float part1 =
			horseComponent.ChargeDamage * horseComponent.Speed + horseComponent.Maneuver * horseComponent.Speed;
		float part2 = horseComponent.BodyLength * itemWeight * 0.025f;
		float sum   = part1 + part2;

		float totalHP = horseComponent.HitPoints + horseComponent.HitPointBonus;

		return sum * totalHP * 0.0001f;
	}
}