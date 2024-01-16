using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop.Extensions;

public static class ItemObjectExtension {
	public static bool IsArrow(this ItemObject? equipment) {
		return equipment != null &&
			   (equipment.ItemType == ItemObject.ItemTypeEnum.Arrows ||
				(equipment.Weapons != null &&
				 equipment.Weapons.AnyQ(weaponComponentData =>
											weaponComponentData is { WeaponClass: WeaponClass.Arrow })));
	}

	public static bool IsBolt(this ItemObject? equipment) {
		return equipment != null &&
			   (equipment.ItemType == ItemObject.ItemTypeEnum.Bolts ||
				(equipment.Weapons != null &&
				 equipment.Weapons.Any(weaponComponentData =>
										   weaponComponentData is { WeaponClass: WeaponClass.Bolt })));
	}

	public static bool IsConsumable(this ItemObject? item) {
		return item?.ItemType is ItemObject.ItemTypeEnum.Arrows
								 or ItemObject.ItemTypeEnum.Bolts
								 or ItemObject.ItemTypeEnum.Thrown;
	}

	public static bool IsCouchable(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("can_couchable"));
	}

	public static bool IsSuitableForMount(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true, Weapons: not null } &&
			   !weapon.Weapons.AnyQ(weaponComponentData =>
										weaponComponentData != null &&
										(MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage)
											   .HasFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount) ||
										 weaponComponentData.WeaponFlags.HasFlag(WeaponFlags.CantReloadOnHorseback)));
	}

	public static bool IsBracable(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("braceable"));
	}

	public static bool IsPolearm(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("polearm"));
	}

	public static bool IsOneHanded(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("one_handed"));
	}

	public static bool IsTwoHanded(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("two_handed"));
	}

	public static bool IsThrowing(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("throwing"));
	}

	public static bool IsBow(this ItemObject? weapon) {
		if (weapon == null) return false;

		if (weapon.ItemType == ItemObject.ItemTypeEnum.Bow) return true;

		if (!weapon.HasWeaponComponent) return false;

		if (weapon.Weapons == null) return false;

		foreach (var weaponComponentData in weapon.Weapons)
			if (weaponComponentData is { WeaponClass: WeaponClass.Bow })
				return true;

		return false;
	}

	public static bool IsCrossBow(this ItemObject? weapon) {
		if (weapon == null) return false;

		if (weapon.ItemType == ItemObject.ItemTypeEnum.Crossbow) return true;

		if (!weapon.HasWeaponComponent) return false;

		if (weapon.Weapons == null) return false;

		foreach (var weaponComponentData in weapon.Weapons)
			if (weaponComponentData is { WeaponClass: WeaponClass.Crossbow })
				return true;

		return false;
	}

	public static bool IsBonusAgainstShield(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } &&
			   weapon.CheckWeaponFlag(flag => flag.Contains("bonus_against_shield"));
	}

	public static bool CanKnockdown(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("can_knockdown"));
	}

	public static bool CanDismount(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("can_dismount"));
	}

	public static bool CantUseWithShields(this ItemObject? weapon) {
		return weapon is { HasWeaponComponent: true } &&
			   weapon.CheckWeaponFlag(flag => !flag.Contains("cant_use_with_shields"));
	}

	private static bool CheckWeaponFlag(this ItemObject? weapon, Func<string, bool> flagCondition) {
		return weapon?.Weapons != null &&
			   weapon.Weapons.WhereQ(weaponComponentData => weaponComponentData != null)
					 .SelectMany(weaponComponentData =>
									 CampaignUIHelper.GetFlagDetailsForWeapon(weaponComponentData,
																			  MBItem
																				  .GetItemUsageSetFlags(weaponComponentData
																					  .ItemUsage)))
					 .AnyQ(flagDetail => flagDetail.Item1 != null    &&
										 !flagDetail.Item1.IsEmpty() &&
										 flagCondition(flagDetail.Item1));
	}

	public static int CalculateWeaponTierBonus(this ItemObject? weapon, bool mounted) {
		if (weapon == null || mounted) return 0; // 如果骑马，则不应用任何加成

		var bonus = 0;
		MBReadOnlyList<WeaponComponentData> weaponFlags = weapon.WeaponComponent.Weapons;
		var weaponFlag = (WeaponFlags)weaponFlags.Aggregate(0u, (current, flag) => current | (uint)flag.WeaponFlags);

		// 为每个匹配的WeaponFlag增加加成
		if (weaponFlag.HasFlag(WeaponFlags.BonusAgainstShield)) bonus++;

		if (weaponFlag.HasFlag(WeaponFlags.CanKnockDown)) bonus++;

		if (weaponFlag.HasFlag(WeaponFlags.CanDismount)) bonus++;

		if (weaponFlag.HasFlag(WeaponFlags.MultiplePenetration)) bonus++;

		if (weapon.IsBracable()) bonus++;

		return bonus;
	}

	public static bool IsSuitableForCharacter(this ItemObject? item, CharacterObject? character) {
		return item      != null &&
			   character != null &&
			   (item.Difficulty <= 0 || item.Difficulty <= character.GetSkillValue(item.RelevantSkill));
	}

	public static bool MatchHarness(this ItemObject? horse, ItemObject? harness) {
		if (horse is { HasHorseComponent  : true, ItemType: ItemObject.ItemTypeEnum.Horse } &&
			harness is { HasArmorComponent: true, ItemType: ItemObject.ItemTypeEnum.HorseHarness })
			return horse.HorseComponent?.Monster?.FamilyType == harness.ArmorComponent?.FamilyType;
		return false;
	}
}