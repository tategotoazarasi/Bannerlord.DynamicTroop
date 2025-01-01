#region
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
#endregion
namespace DTES2.Comparer;

/// <summary>
///     A comparer class for comparing two ItemObjects based on their weapon properties.
///     Implements <see cref="IComparer{T}" /> to provide custom comparison logic.
/// </summary>
public class WeaponComparer : IComparer<ItemObject> {
	/// <summary>
	///     A dictionary mapping pairs of WeaponClass values to a similarity score.
	///     This is used to compare the relationship between different weapon classes
	///     and assign a predefined score based on their compatibility or similarity.
	/// </summary>
	/// <remarks>
	///     The keys in this dictionary are tuples of <see cref="WeaponClass" /> enums,
	///     and the values are floating-point numbers representing the similarity score
	///     between the two weapon classes. A higher score indicates greater similarity.
	///     Example:
	///     - (WeaponClass.OneHandedSword, WeaponClass.TwoHandedSword) -> 50
	///     This indicates a moderate similarity between one-handed and two-handed swords.
	///     - (WeaponClass.OneHandedPolearm, WeaponClass.OneHandedAxe) -> 20
	///     This indicates a lower similarity between one-handed polearms and axes.
	/// </remarks>
	private static readonly Dictionary<(WeaponClass, WeaponClass), float> ClassPairs =
		new Dictionary<(WeaponClass, WeaponClass), float> {
															  {
																  (WeaponClass.OneHandedSword,
																   WeaponClass.TwoHandedSword),
																  50
															  }, {
																  (WeaponClass.OneHandedAxe, WeaponClass.TwoHandedAxe),
																  50
															  },
															  { (WeaponClass.Mace, WeaponClass.TwoHandedMace), 50 }, {
																  (WeaponClass.OneHandedPolearm,
																   WeaponClass.OneHandedSword),
																  50
															  }, {
																  (WeaponClass.OneHandedPolearm,
																   WeaponClass.OneHandedAxe),
																  20
															  },
															  { (WeaponClass.OneHandedPolearm, WeaponClass.Mace), 20 }, {
																  (WeaponClass.TwoHandedPolearm,
																   WeaponClass.TwoHandedSword),
																  20
															  }, {
																  (WeaponClass.TwoHandedPolearm,
																   WeaponClass.TwoHandedAxe),
																  20
															  }, {
																  (WeaponClass.TwoHandedPolearm,
																   WeaponClass.TwoHandedMace),
																  20
															  },
															  { (WeaponClass.Pick, WeaponClass.OneHandedAxe), 50 },
															  { (WeaponClass.Pick, WeaponClass.TwoHandedAxe), 25 },
															  { (WeaponClass.Dagger, WeaponClass.OneHandedSword), 30 }
														  };

	/// <summary>
	///     Compares two ItemObjects and returns a value indicating their similarity.
	/// </summary>
	/// <param name="x">The first ItemObject to compare.</param>
	/// <param name="y">The second ItemObject to compare.</param>
	/// <returns>
	///     A similarity ranged from 0 to 100, where 100 indicates the highest similarity
	/// </returns>
	public int Compare(ItemObject x, ItemObject y) {
		if (!x.HasWeaponComponent || !y.HasWeaponComponent) {
			return 0;
		}
		float v = 0;
		v += this.CompareItemType(x, y);
		v += this.CompareWeaponClass(x, y);
		v += this.CompareWeaponDamageType(x, y);
		v += this.CompareFlags(x, y);
		return (int)(v / 4);
	}

	/// <summary>
	///     Compares the ItemType of two ItemObjects.
	/// </summary>
	/// <param name="x">The first ItemObject.</param>
	/// <param name="y">The second ItemObject.</param>
	/// <returns>A similarity score, 100 if the types match, otherwise 0.</returns>
	private float CompareItemType(ItemObject x, ItemObject y) => x.ItemType == y.ItemType ? 100 : 0;

	/// <summary>
	///     Compares the WeaponClass of two ItemObjects symmetrically.
	/// </summary>
	/// <param name="x">The first ItemObject.</param>
	/// <param name="y">The second ItemObject.</param>
	/// <returns>A similarity score between 0 and 100 based on the class relationship.</returns>
	private float CompareWeaponClass(ItemObject x, ItemObject y) {
		float xy = this.CompareWeaponClassOneWay(x, y);
		float yx = this.CompareWeaponClassOneWay(y, x);
		return (xy + yx) / 2;
	}

	/// <summary>
	///     Compares the WeaponClass of one ItemObject against another asymmetrically.
	/// </summary>
	/// <param name="x">The first ItemObject.</param>
	/// <param name="y">The second ItemObject.</param>
	/// <returns>A score representing the one-way similarity between the classes.</returns>
	private float CompareWeaponClassOneWay(ItemObject x, ItemObject y) {
		float                    v      = 0;
		IEnumerable<WeaponClass> class1 = x.Weapons.Select(w => w.WeaponClass);
		foreach (WeaponClass classx in class1) {
			IEnumerable<WeaponClass> class2 = y.Weapons.Select(w => w.WeaponClass);
			float                    max    = 0;
			foreach (WeaponClass classy in class2) {
				if (classx == classy) {
					max = MathF.Max(max, 100f);
				}
				if (ClassPairs.TryGetValue((classx, classy), out float o1)) {
					max = MathF.Max(max, o1);
				}
				if (ClassPairs.TryGetValue((classy, classx), out float o2)) {
					max = MathF.Max(max, o2);
				}
			}
			v += max;
		}
		return v / x.Weapons.Count;
	}

	/// <summary>
	///     Compares the damage types of two ItemObjects by their thrust and swing profiles.
	/// </summary>
	/// <param name="x">The first ItemObject.</param>
	/// <param name="y">The second ItemObject.</param>
	/// <returns>A similarity score based on matching damage types and their counts.</returns>
	private float CompareWeaponDamageType(ItemObject x, ItemObject y) {
		DamageProfile profileX = new DamageProfile();
		DamageProfile profileY = new DamageProfile();
		foreach (WeaponComponentData weapon in x.Weapons) {
			if (
				weapon is { ThrustDamageType: not DamageTypes.Invalid, ThrustDamage: > 0, ThrustSpeed: > 0 }
				) {
				profileX.Thrust.Add(weapon.ThrustDamageType);
			}
		}
		foreach (WeaponComponentData weapon in y.Weapons) {
			if (
				weapon is { SwingDamageType: not DamageTypes.Invalid, SwingDamage: > 0, SwingSpeed: > 0 }
				) {
				profileY.Swing.Add(weapon.SwingDamageType);
			}
		}
		float v = 0;
		if (profileX.Thrust.Count == 0 && profileY.Thrust.Count == 0) {
			v += 50;
		}
		else if (
			profileX.Thrust.Count != 0 && profileY.Thrust.Count == 0 ||
			profileX.Thrust.Count == 0 && profileY.Thrust.Count != 0
			) {
			v += 0;
		}
		else {
			v +=
				profileX.Thrust.Intersect(profileY.Thrust).Count() * 25 / (float)profileX.Thrust.Count;
			v +=
				profileX.Thrust.Intersect(profileY.Thrust).Count() * 25 / (float)profileY.Thrust.Count;
		}

		if (profileX.Swing.Count == 0 && profileY.Swing.Count == 0) {
			v += 50;
		}
		else if (
			profileX.Swing.Count != 0 && profileY.Swing.Count == 0 ||
			profileX.Swing.Count == 0 && profileY.Swing.Count != 0
			) {
			v += 0;
		}
		else {
			v +=
				profileX.Swing.Intersect(profileY.Swing).Count() * 25 / (float)profileX.Swing.Count;
			v +=
				profileX.Swing.Intersect(profileY.Swing).Count() * 25 / (float)profileY.Swing.Count;
		}
		return v;
	}

	/// <summary>
	///     Compares the WeaponFlags of two ItemObjects.
	/// </summary>
	/// <param name="x">The first ItemObject.</param>
	/// <param name="y">The second ItemObject.</param>
	/// <returns>
	///     A similarity score based on the bitwise intersection of flags, normalized by the bit counts of each flag set.
	/// </returns>
	private float CompareFlags(ItemObject x, ItemObject y) {
		WeaponFlags flagX = x.Weapons.Select(w => w.WeaponFlags).Aggregate((current, flag) => current | flag);
		WeaponFlags flagY = y.Weapons.Select(w => w.WeaponFlags).Aggregate((current, flag) => current | flag);
		int         bc    = BitCount(flagX & flagY);
		float       v     = 0;
		v += bc * 100 / (float)BitCount(flagX);
		v += bc * 100 / (float)BitCount(flagY);
		return v / 2;
	}

	/// <summary>
	///     Calculates the number of set bits (1s) in a WeaponFlags value.
	/// </summary>
	/// <param name="flags">The WeaponFlags value to analyze.</param>
	/// <returns>The number of bits set to 1 in the provided WeaponFlags.</returns>
	private static int BitCount(WeaponFlags flags) {
		ulong value = (ulong)flags;
		int   count = 0;

		// Use the classic bit-counting method
		while (value != 0) {
			count +=  (int)(value & 1); // Check the lowest bit
			value >>= 1;                // Shift right by one bit
		}

		return count;
	}

	/// <summary>
	///     Represents a profile of damage types for thrust and swing damage.
	///     Used to compare damage profiles of different weapons.
	/// </summary>
	private class DamageProfile {
		/// <summary>
		///     The set of damage types associated with swing attacks.
		/// </summary>
		public readonly HashSet<DamageTypes> Swing = new HashSet<DamageTypes>();

		/// <summary>
		///     The set of damage types associated with thrust attacks.
		/// </summary>
		public readonly HashSet<DamageTypes> Thrust = new HashSet<DamageTypes>();
	}
}