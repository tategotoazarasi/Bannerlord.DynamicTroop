using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using TaleWorlds.Core;

namespace DynamicTroopEquipmentReupload;

/// <summary>
///     提供基于权重的随机选择功能，用于从一组物品中根据特定规则选择单个物品。
/// </summary>
public static class WeightedRandomSelector {
	private static readonly Random _random = new();

	/// <summary>
	///     根据目标值和物品效能的正态分布概率，从物品列表中随机选择一个物品。
	/// </summary>
	/// <param name="items">       物品列表，每个物品都有一个效能值。 </param>
	/// <param name="targetValue"> 目标效能值，用于确定正态分布的中心。 </param>
	/// <returns> 根据加权概率选中的物品。 </returns>
	public static ItemObject SelectItem(List<ItemObject> items, float targetValue) {
		if (items == null || items.Count == 0)
			throw new ArgumentException("items cannot be null or empty", nameof(items));

		if (items.Count == 1)
			return items[0];

		var effectiveness     = items.Select(item => (double)item.Effectiveness).ToArray();
		var standardDeviation = effectiveness.StandardDeviation();

		// Uniform fallback
		if (double.IsNaN(standardDeviation) || standardDeviation <= 0.0001)
			return items[_random.Next(items.Count)];

		var mean = (double)targetValue;

		// Normal.PDF(mean, stddev, x)
		var weights = items.Select(item => (float)Normal.PDF(mean, standardDeviation, item.Effectiveness)).ToList();

		var totalWeight = weights.Sum();
		if (totalWeight <= 0 || float.IsNaN(totalWeight) || float.IsInfinity(totalWeight))
			return items[_random.Next(items.Count)];

		var normalizedWeights = weights.Select(weight => weight / totalWeight).ToList();
		return WeightedRandomChoose(items, normalizedWeights);
	}

	/// <summary>
	///     根据给定的权重列表，从物品列表中随机选择一个物品。
	/// </summary>
	/// <param name="items">   物品列表。 </param>
	/// <param name="weights"> 与物品列表对应的权重列表，表示每个物品被选中的相对概率。 </param>
	/// <returns> 根据权重随机选中的物品。 </returns>
	private static ItemObject WeightedRandomChoose(List<ItemObject> items, List<float> weights) {
		List<float> cumulativeWeights = new();
		float       total             = 0;
		foreach (var weight in weights) {
			total += weight;
			cumulativeWeights.Add(total);
		}

		var randomNumber = (float)_random.NextDouble() * total;
		for (var i = 0; i < items.Count; i++) {
			if (randomNumber < cumulativeWeights[i]) { return items[i]; }
		}

		return items.Last();
	}
}