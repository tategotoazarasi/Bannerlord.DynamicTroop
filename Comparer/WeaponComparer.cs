#region
using System.Collections.Generic;
using System.Linq;
using DTES2.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Library;
#endregion
namespace DTES2.Comparer;

/// <summary>
///     A comparer class for comparing two ItemObjects based on their weapon properties.
///     Implements <see cref="IComparer{T}" /> to provide custom comparison logic.
///     <para>
///         该版本在所有仅有 <see cref="ItemObject" /> 参数的私有函数上增加了缓存机制，
///         以提升重复调用时的性能。
///     </para>
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
	///     between the two weapon classes.
	///     A higher score indicates greater similarity. Example:
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
	///     比较两个 <see cref="ItemObject" />，返回一个相似度（0 - 100）。
	///     这里也进行缓存，以避免重复计算。
	/// </summary>
	/// <param name="x">The first ItemObject to compare.</param>
	/// <param name="y">The second ItemObject to compare.</param>
	/// <returns>A similarity ranged from 0 to 100, where 100 indicates the highest similarity.</returns>
	public int Compare(ItemObject x, ItemObject y) {
		// 缓存Key需要标识 x 和 y
		string cacheKey =
			$"{nameof(WeaponComparer)}.{nameof(this.Compare)}:{x?.StringId ?? ""}:{y?.StringId ?? ""}";

		return CacheManager.GetOrAdd(
			() => {
				if (!x.HasWeaponComponent || !y.HasWeaponComponent) {
					return 0;
				}

				float v = 0;
				v += this.CompareItemType(x, y);
				v += this.CompareWeaponClass(x, y);
				v += this.CompareWeaponDamageType(x, y);
				v += this.CompareFlags(x, y);

				// 最终再除以3，保持原逻辑不变
				return (int)(v / 3);
			},
			cacheKey
		);
	}

	/// <summary>
	///     Compares the ItemType of two ItemObjects.
	///     <para>对两个 <see cref="ItemObject" /> 的 <see cref="ItemObject.ItemType" /> 进行缓存化比对。</para>
	/// </summary>
	/// <param name="x">The first ItemObject.</param>
	/// <param name="y">The second ItemObject.</param>
	/// <returns>A similarity score, 100 if the types match, otherwise 0.</returns>
	private float CompareItemType(ItemObject x, ItemObject y) {
		string cacheKey =
			$"{nameof(WeaponComparer)}.{nameof(this.CompareItemType)}:{x?.StringId ?? ""}:{y?.StringId ?? ""}";

		return CacheManager.GetOrAdd(() => x.ItemType == y.ItemType ? 100 : 0, cacheKey);
	}

	/// <summary>
	///     比较两个 <see cref="ItemObject" /> 的武器类型（WeaponClass）。
	///     <para>因内部需要再次分两次比较，这里同样做缓存。</para>
	/// </summary>
	private float CompareWeaponClass(ItemObject x, ItemObject y) {
		string cacheKey =
			$"{nameof(WeaponComparer)}.{nameof(this.CompareWeaponClass)}:{x?.StringId ?? ""}:{y?.StringId ?? ""}";

		return CacheManager.GetOrAdd(
			() => {
				// 如果ItemType相同，原逻辑直接返回0
				if (x.ItemType == y.ItemType) {
					return 0f;
				}

				float xy = this.CompareWeaponClassOneWay(x, y);
				float yx = this.CompareWeaponClassOneWay(y, x);
				return (xy + yx) / 2;
			},
			cacheKey
		);
	}

	/// <summary>
	///     单向比较 x 到 y 的武器类型相似度。
	///     <para>也做缓存。</para>
	/// </summary>
	private float CompareWeaponClassOneWay(ItemObject x, ItemObject y) {
		string cacheKey =
			$"{nameof(WeaponComparer)}.{nameof(this.CompareWeaponClassOneWay)}:{x?.StringId ?? ""}:{y?.StringId ?? ""}";

		return CacheManager.GetOrAdd(
			() => {
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
			},
			cacheKey
		);
	}

	/// <summary>
	///     比较两个 <see cref="ItemObject" /> 的伤害类型（Thrust/Swing）。
	///     <para>结果同样进行缓存。</para>
	/// </summary>
	private float CompareWeaponDamageType(ItemObject x, ItemObject y) {
		string cacheKey =
			$"{nameof(WeaponComparer)}.{nameof(this.CompareWeaponDamageType)}:{x?.StringId ?? ""}:{y?.StringId ?? ""}";

		return CacheManager.GetOrAdd(
			() => {
				DamageProfile profileX = new DamageProfile();
				DamageProfile profileY = new DamageProfile();

				foreach (WeaponComponentData weapon in x.Weapons) {
					if (
						weapon is { ThrustDamageType: not DamageTypes.Invalid, ThrustDamage : > 0, ThrustSpeed : > 0 }
						) {
						profileX.Thrust.Add(weapon.ThrustDamageType);
					}

					if (
						weapon is { SwingDamageType: not DamageTypes.Invalid, SwingDamage : > 0, SwingSpeed : > 0 }
						) {
						profileX.Swing.Add(weapon.SwingDamageType);
					}
				}

				foreach (WeaponComponentData weapon in y.Weapons) {
					if (
						weapon is { ThrustDamageType: not DamageTypes.Invalid, ThrustDamage : > 0, ThrustSpeed : > 0 }
						) {
						profileY.Thrust.Add(weapon.ThrustDamageType);
					}

					if (
						weapon is { SwingDamageType: not DamageTypes.Invalid, SwingDamage : > 0, SwingSpeed : > 0 }
						) {
						profileY.Swing.Add(weapon.SwingDamageType);
					}
				}

				float v = 0;

				// Thrust
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

				// Swing
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
			},
			cacheKey
		);
	}

	/// <summary>
	///     比较两个 <see cref="ItemObject" /> 的 WeaponFlags 以及扩展标志。
	///     <para>也进行缓存处理。</para>
	/// </summary>
	private float CompareFlags(ItemObject x, ItemObject y) {
		string cacheKey =
			$"{nameof(WeaponComparer)}.{nameof(this.CompareFlags)}:{x?.StringId ?? ""}:{y?.StringId ?? ""}";

		return CacheManager.GetOrAdd(
			() => {
				WeaponFlags flagX = x.Weapons.Select(w => w.WeaponFlags).Aggregate((current, flag) => current | flag);
				WeaponFlags flagY = y.Weapons.Select(w => w.WeaponFlags).Aggregate((current, flag) => current | flag);

				int   bc  = BitCount(flagX & flagY);
				float v   = 0;
				int   bcx = BitCount(flagX);
				int   bcy = BitCount(flagY);

				// 先比较 flag 位数交集
				if (bcx != 0) {
					v += bc * 100 / (float)bcx;
				}
				else {
					v += 100;
				}

				if (bcy != 0) {
					v += bc * 100 / (float)bcy;
				}
				else {
					v += 100;
				}

				// 再比较扩展标志
				v += x.IsSuitableForMount()   == y.IsSuitableForMount() ? 100 : 0;
				v += x.IsBracable()           == y.IsBracable() ? 100 : 0;
				v += x.IsBonusAgainstShield() == y.IsBonusAgainstShield() ? 100 : 0;
				v += x.CanKnockdown()         == y.CanKnockdown() ? 100 : 0;
				v += x.CanDismount()          == y.CanDismount() ? 100 : 0;
				v += x.CantUseWithShields()   == y.CantUseWithShields() ? 100 : 0;
				v += x.IsCouchable()          == y.IsCouchable() ? 800 : 0;

				return v / 16;
			},
			cacheKey
		);
	}

	/// <summary>
	///     计算一个 WeaponFlags 中被置位 (bit=1) 的数量。
	///     <para>注意这里并不只有 <see cref="ItemObject" /> 参数，所以无需 cache 化。</para>
	/// </summary>
	private static int BitCount(WeaponFlags flags) {
		ulong value = (ulong)flags;
		int   count = 0;

		while (value != 0) {
			count +=  (int)(value & 1);
			value >>= 1;
		}

		return count;
	}

	/// <summary>
	///     Represents a profile of damage types for thrust and swing damage.
	///     Used to compare damage profiles of different weapons.
	/// </summary>
	private class DamageProfile {
		/// <summary> The set of damage types associated with swing attacks. </summary>
		public readonly HashSet<DamageTypes> Swing = new HashSet<DamageTypes>();

		/// <summary> The set of damage types associated with thrust attacks. </summary>
		public readonly HashSet<DamageTypes> Thrust = new HashSet<DamageTypes>();
	}
}