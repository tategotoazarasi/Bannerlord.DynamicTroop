using System;
using System.Collections.Generic;
using DTES2.Extensions;
using TaleWorlds.Core;

namespace DTES2.Comparer;

public class MeleeWeaponComparer : IComparer<ItemObject> {
	/// <summary>
	///     Compares two melee weapons and returns a value indicating their similarity.
	/// </summary>
	/// <param name="weaponA"> The first melee weapon. </param>
	/// <param name="weaponB"> The second melee weapon. </param>
	/// <returns>
	///     A value indicating the similarity of the two weapons. Higher values indicate greater
	///     similarity. Returns 0 if either weapon is null or not a melee weapon.
	/// </returns>
	public int Compare(ItemObject? weaponA, ItemObject? weaponB) {
		if (weaponA == null             ||
			weaponB == null             ||
			!weaponA.HasWeaponComponent ||
			!weaponB.HasWeaponComponent) {
			return 0;
		}

		// Calculate similarity based on various factors
		int similarityScore = 0;

		similarityScore += this.CompareItemTypes(weaponA, weaponB);
		similarityScore += this.CompareWeaponClasses(weaponA, weaponB);
		similarityScore += this.CompareWeaponFlags(weaponA, weaponB);
		similarityScore += this.CompareWeaponStats(weaponA, weaponB);
		similarityScore += this.CompareExtendedProperties(weaponA, weaponB);

		return similarityScore;
	}

	/// <summary>
	///     Compares the ItemTypeEnum of two weapons.
	/// </summary>
	private int CompareItemTypes(ItemObject weaponA, ItemObject weaponB) {
		int score = 0;
		if (weaponA.ItemType == weaponB.ItemType) {
			score += 10;
		} else {
			// Partial score for related item types (e.g., OneHandedWeapon and TwoHandedWeapon)
			if (weaponA.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon &&
				weaponB.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
				weaponA.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon &&
				weaponB.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon) {
				score += 5;
			} else if (weaponA.ItemType == ItemObject.ItemTypeEnum.Polearm &&
					   weaponB.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon ||
					   weaponA.ItemType == ItemObject.ItemTypeEnum.OneHandedWeapon &&
					   weaponB.ItemType == ItemObject.ItemTypeEnum.Polearm ||
					   weaponA.ItemType == ItemObject.ItemTypeEnum.Polearm &&
					   weaponB.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
					   weaponA.ItemType == ItemObject.ItemTypeEnum.TwoHandedWeapon &&
					   weaponB.ItemType == ItemObject.ItemTypeEnum.Polearm) {
				score += 3;
			}
		}

		return score;
	}

	/// <summary>
	///     Compares the WeaponClass of two weapons, considering all WeaponComponentData entries.
	/// </summary>
	private int CompareWeaponClasses(ItemObject weaponA, ItemObject weaponB) {
		int score = 0;
		foreach (WeaponComponentData wcdA in weaponA.Weapons) {
			foreach (WeaponComponentData wcdB in weaponB.Weapons) {
				if (wcdA.WeaponClass == wcdB.WeaponClass) {
					score += 10;
				} else {
					// Partial score for related weapon classes
					if (this.IsRelatedWeaponClass(wcdA.WeaponClass, wcdB.WeaponClass)) {
						score += 5;
					}
				}
			}
		}

		return score;
	}

	/// <summary>
	///     Checks if two WeaponClasses are related (e.g., Dagger and OneHandedSword).
	/// </summary>
	private bool IsRelatedWeaponClass(WeaponClass classA, WeaponClass classB) {
		// Define related weapon classes
		HashSet<(WeaponClass, WeaponClass)> relatedClasses = [
			(WeaponClass.Dagger, WeaponClass.OneHandedSword),
			(WeaponClass.OneHandedSword, WeaponClass.TwoHandedSword),
			(WeaponClass.OneHandedAxe, WeaponClass.TwoHandedAxe),
			(WeaponClass.Mace, WeaponClass.TwoHandedMace),
			(WeaponClass.OneHandedPolearm, WeaponClass.TwoHandedPolearm),
			(WeaponClass.OneHandedPolearm, WeaponClass.LowGripPolearm),
			(WeaponClass.TwoHandedPolearm, WeaponClass.LowGripPolearm)
		];

		return relatedClasses.Contains((classA, classB)) || relatedClasses.Contains((classB, classA));
	}

	/// <summary>
	///     Compares the WeaponFlags of two weapons, considering all WeaponComponentData entries.
	/// </summary>
	private int CompareWeaponFlags(ItemObject weaponA, ItemObject weaponB) {
		int score = 0;
		foreach (WeaponComponentData wcdA in weaponA.Weapons) {
			foreach (WeaponComponentData wcdB in weaponB.Weapons) {
				// Iterate through all flags and compare
				foreach (WeaponFlags flag in Enum.GetValues(typeof(WeaponFlags))) {
					bool hasFlagA = wcdA.WeaponFlags.HasFlag(flag);
					bool hasFlagB = wcdB.WeaponFlags.HasFlag(flag);

					if (hasFlagA && hasFlagB) {
						score += 2; // Both have the flag
					} else if (!hasFlagA &&
							   !hasFlagB) {
						score += 1; // Neither has the flag (less significant)
					}

					// No score if only one has the flag
				}
			}
		}

		return score;
	}

	/// <summary>
	///     Compares the weapon stats (damage, speed, length) of two weapons, considering all
	///     WeaponComponentData entries.
	/// </summary>
	private int CompareWeaponStats(ItemObject weaponA, ItemObject weaponB) {
		int score = 0;
		foreach (WeaponComponentData wcdA in weaponA.Weapons) {
			foreach (WeaponComponentData wcdB in weaponB.Weapons) {
				// Compare Thrust Damage
				score += this.CalculateStatSimilarity(wcdA.ThrustDamage, wcdB.ThrustDamage);

				// Compare Swing Damage
				score += this.CalculateStatSimilarity(wcdA.SwingDamage, wcdB.SwingDamage);

				// Compare Thrust Speed
				score += this.CalculateStatSimilarity(wcdA.ThrustSpeed, wcdB.ThrustSpeed);

				// Compare Swing Speed
				score += this.CalculateStatSimilarity(wcdA.SwingSpeed, wcdB.SwingSpeed);

				// Compare Weapon Length
				score += this.CalculateStatSimilarity(wcdA.WeaponLength, wcdB.WeaponLength);

				// Compare Handling
				score += this.CalculateStatSimilarity(wcdA.Handling, wcdB.Handling);
			}
		}

		return score;
	}

	/// <summary>
	///     Calculates a similarity score for two integer stats based on their proximity.
	/// </summary>
	private int CalculateStatSimilarity(int statA, int statB) {
		int diff = Math.Abs(statA - statB);
		if (diff == 0) {
			return 5;
		}

		if (diff <= 5) {
			return 4;
		}

		if (diff <= 10) {
			return 3;
		}

		if (diff <= 20) {
			return 2;
		}

		if (diff <= 30) {
			return 1;
		}

		return 0;
	}

	/// <summary>
	///     Compares extended properties (e.g., IsCouchable, IsBonusAgainstShield) of two weapons.
	/// </summary>
	private int CompareExtendedProperties(ItemObject weaponA, ItemObject weaponB) {
		int score = 0;

		if (weaponA.IsCouchable() == weaponB.IsCouchable()) {
			score += 5;
		}

		if (weaponA.IsBonusAgainstShield() == weaponB.IsBonusAgainstShield()) {
			score += 5;
		}

		if (weaponA.CanKnockdown() == weaponB.CanKnockdown()) {
			score += 5;
		}

		if (weaponA.CanDismount() == weaponB.CanDismount()) {
			score += 5;
		}

		if (weaponA.CantUseWithShields() == weaponB.CantUseWithShields()) {
			score += 5;
		}

		if (weaponA.IsSuitableForMount() == weaponB.IsSuitableForMount()) {
			score += 5;
		}

		if (weaponA.IsBracable() == weaponB.IsBracable()) {
			score += 5;
		}

		return score;
	}
}