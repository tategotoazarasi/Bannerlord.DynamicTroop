#region
using System;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
#endregion
namespace DTES2.Extensions;

public static class ItemObjectExtension {
	private static readonly MemoryCache _cache = new MemoryCache(new MemoryCacheOptions());
	private static readonly MemoryCacheEntryOptions _cacheEntryOptions = new MemoryCacheEntryOptions {
																										 SlidingExpiration = TimeSpan.FromMinutes(5)
																									 };

	public static T GetOrAdd<T>(string key, Func<T> valueFactory) {
		if (_cache.TryGetValue(key, out var cachedValue)) {
			return (T)cachedValue!;
		}
		var value = valueFactory();
		_cache.Set(key, value, _cacheEntryOptions);
		return value;
	}

	public static bool IsArrow(this ItemObject? equipment) {
		if (equipment == null) return false;
		return GetOrAdd("IsArrow" + equipment.StringId,
						() => equipment.ItemType == ItemObject.ItemTypeEnum.Arrows ||
							  equipment.Weapons != null &&
							  equipment.Weapons.AnyQ(weaponComponentData =>
														 weaponComponentData is { WeaponClass: WeaponClass.Arrow }));
	}

	public static bool IsBolt(this ItemObject? equipment) {
		if (equipment == null) return false;
		return GetOrAdd("IsBolt" + equipment.StringId,
						() => equipment.ItemType == ItemObject.ItemTypeEnum.Bolts ||
							  equipment.Weapons != null &&
							  equipment.Weapons.Any(weaponComponentData =>
														weaponComponentData is { WeaponClass: WeaponClass.Bolt }));
	}

	public static bool IsConsumable(this ItemObject? item) {
		if (item == null) return false;
		return GetOrAdd("IsConsumable" + item.StringId,
						() => item.ItemType is ItemObject.ItemTypeEnum.Arrows
											   or ItemObject.ItemTypeEnum.Bolts
											   or ItemObject.ItemTypeEnum.Thrown);
	}

	public static bool IsCouchable(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsCouchable" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("can_couchable")));
	}

	public static bool IsSuitableForMount(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsSuitableForMount" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true, Weapons: not null } &&
							  !weapon.Weapons.AnyQ(weaponComponentData =>
													   weaponComponentData != null &&
													   (MBItem
														.GetItemUsageSetFlags(weaponComponentData.ItemUsage)
														.HasFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount) ||
														weaponComponentData.WeaponFlags.HasFlag(WeaponFlags.CantReloadOnHorseback))));
	}

	public static bool IsBracable(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsBracable" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("braceable")));
	}

	public static bool IsPolearm(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsPolearm" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("polearm")));
	}

	public static bool IsOneHanded(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsOneHanded" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("one_handed")));
	}

	public static bool IsTwoHanded(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsTwoHanded" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("two_handed")));
	}

	public static bool IsThrowing(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsThrowing" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("throwing")));
	}

	public static bool IsBow(this ItemObject? weapon) {
		if (weapon == null) return false;

		return GetOrAdd("IsBow" + weapon.StringId,
						() => {
							if (weapon.ItemType == ItemObject.ItemTypeEnum.Bow) return true;

							if (!weapon.HasWeaponComponent) return false;

							if (weapon.Weapons == null) return false;

							foreach (var weaponComponentData in weapon.Weapons) {
								if (weaponComponentData is { WeaponClass: WeaponClass.Bow })
									return true;
							}

							return false;
						});
	}

	public static bool IsCrossBow(this ItemObject? weapon) {
		if (weapon == null) return false;

		return GetOrAdd("IsCrossBow" + weapon.StringId,
						() => {
							if (weapon.ItemType == ItemObject.ItemTypeEnum.Crossbow) return true;

							if (!weapon.HasWeaponComponent) return false;

							if (weapon.Weapons == null) return false;

							foreach (var weaponComponentData in weapon.Weapons) {
								if (weaponComponentData is { WeaponClass: WeaponClass.Crossbow })
									return true;
							}

							return false;
						});
	}

	public static bool IsThrowingWeaponCanBeAcquired(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsThrowingWeaponCanBeAcquired" + weapon.StringId,
						() => {
							if (weapon.ItemType != ItemObject.ItemTypeEnum.Thrown) return false;

							if (!weapon.HasWeaponComponent) return false;

							if (weapon.Weapons == null) return false;

							foreach (var weaponComponentData in weapon.Weapons) {
								if (weaponComponentData is {
															   WeaponClass: WeaponClass.ThrowingAxe
																			or WeaponClass.ThrowingKnife
																			or WeaponClass.Javelin
														   })
									return true;
							}

							return false;
						});
	}

	public static bool IsBonusAgainstShield(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("IsBonusAgainstShield" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } &&
							  weapon.CheckWeaponFlag(flag => flag.Contains("bonus_against_shield")));
	}

	public static bool CanKnockdown(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("CanKnockdown" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("can_knockdown")));
	}

	public static bool CanDismount(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("CanDismount" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } && weapon.CheckWeaponFlag(flag => flag.Contains("can_dismount")));
	}

	public static bool CantUseWithShields(this ItemObject? weapon) {
		if (weapon == null) return false;
		return GetOrAdd("CantUseWithShields" + weapon.StringId,
						() => weapon is { HasWeaponComponent: true } &&
							  weapon.CheckWeaponFlag(flag => !flag.Contains("cant_use_with_shields")));
	}

	private static bool CheckWeaponFlag(this ItemObject? weapon, Func<string, bool> flagCondition) {
		if (weapon == null) return false;
		return weapon?.Weapons != null &&
			   weapon
				   .Weapons
				   .WhereQ(weaponComponentData => weaponComponentData != null)
				   .SelectMany(weaponComponentData =>
								   CampaignUIHelper.GetFlagDetailsForWeapon(weaponComponentData,
																			MBItem
																				.GetItemUsageSetFlags(weaponComponentData
																										  .ItemUsage)))
				   .AnyQ(flagDetail => flagDetail.Item1 != null    &&
									   !flagDetail.Item1.IsEmpty() &&
									   flagCondition(flagDetail.Item1));
	}

	public static bool IsSuitableForCharacter(this ItemObject? item, CharacterObject? character) {
		if (item == null || character == null) return false;
		return GetOrAdd("IsSuitableForCharacter" + item.StringId + character.StringId,
						() => item.Difficulty <= 0 || item.Difficulty <= character.GetSkillValue(item.RelevantSkill));
	}

	public static bool MatchHarness(this ItemObject? horse, ItemObject? harness) {
		if (horse == null || harness == null) return false;
		return GetOrAdd("MatchHarness" + horse.StringId + harness.StringId,
						() =>
							horse is { HasHorseComponent  : true, ItemType: ItemObject.ItemTypeEnum.Horse }        &&
							harness is { HasArmorComponent: true, ItemType: ItemObject.ItemTypeEnum.HorseHarness } &&
							horse.HorseComponent?.Monster?.FamilyType == harness.ArmorComponent?.FamilyType
		);
	}
}