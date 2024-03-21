using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

public static class WeightedRandomSelector {
	private static readonly Random _random = new();

	/// <summary>
	///     根据目标值和物品效能的正态分布概率，从物品列表中随机选择一个物品。
	/// </summary>
	/// <param name="items">物品列表，每个物品都有一个效能值。</param>
	/// <param name="targetValue">目标效能值，用于确定正态分布的中心。</param>
	/// <returns>根据正态分布随机选中的物品。</returns>
	public static ItemObject SelectItem(List<ItemObject> items, float targetValue) {
		if (items == null || !items.Any()) { throw new ArgumentException("Items list cannot be null or empty."); }

		// 将物品根据Effectiveness分组并排序
		var groupedItems = items.GroupBy(item => item.Effectiveness).ToDictionary(group => group.Key, group => group.ToList());

		var distinctEffectiveness = groupedItems.Keys.OrderBy(eff => eff).ToList();

		// 计算所有物品效能值的标准差
		var standardDeviation = distinctEffectiveness.StandardDeviation();

		if (standardDeviation == 0) {
			// 如果所有物品的效能值相同，则随机返回一个物品
			return items[_random.Next(items.Count)];
		}

		// 生成一个目标效能值附近的正态分布随机值
		var distribution = new Normal(targetValue, standardDeviation);
		var randomValue  = distribution.Sample();

		// 通过二分查找找到最接近随机值的效能值
		var index = BinarySearchClosest(distinctEffectiveness, randomValue);

		// 获取对应的所有物品，并从中随机选择一个返回
		var effectiveItems = groupedItems[distinctEffectiveness[index]];
		return effectiveItems[_random.Next(effectiveItems.Count)];
	}

	/// <summary>
	///     二分查找最接近给定值的元素索引。
	/// </summary>
	/// <param name="sortedValues">已排序的列表。</param>
	/// <param name="value">要查找的值。</param>
	/// <returns>最接近给定值的元素索引。</returns>
	private static int BinarySearchClosest(List<float> sortedValues, double value) {
		int left = 0, right = sortedValues.Count - 1;
		while (left < right) {
			var mid      = (left + right) / 2;
			var midValue = sortedValues[mid];
			if (midValue      < value) { left  = mid; }
			else if (midValue > value) { right = mid; }
			else { return mid; }

			if (left == right - 1) { break; }
		}

		var absLeft  = Math.Abs(sortedValues[left]  - value);
		var absRight = Math.Abs(sortedValues[right] - value);
		if (absLeft < absRight) return left;
		if (absLeft > absRight) return right;
		return _random.Next(2) == 0 ? left : right;
	}
}