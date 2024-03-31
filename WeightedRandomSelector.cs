using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Distributions;
using MathNet.Numerics.Statistics;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

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
		// 使用 MathNet.Numerics 计算均值和标准差
		var effectiveness     = items.Select(item => (double)item.Effectiveness).ToArray();
		var mean              = effectiveness.Average();
		var standardDeviation = effectiveness.StandardDeviation();

		// 计算每个item的权重
		var weights = items.Select(item => (float)Normal.PDF(item.Effectiveness, mean, standardDeviation)).ToList();

		// 归一化权重
		var totalWeight       = weights.Sum();
		var normalizedWeights = weights.Select(weight => weight / totalWeight).ToList();

		// 加权随机选择
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