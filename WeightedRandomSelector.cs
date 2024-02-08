using System;
using System.Collections.Generic;
using System.Linq;
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
		// 计算均值
		var mean = items.Average(item => item.Effectiveness);

		// 计算方差
		var variance = CalculateVariance(items, mean);

		// 计算标准差
		var standardDeviation = (float)Math.Sqrt(variance);

		// 计算每个item的权重
		var weights = items.Select(item => CalculateProbabilityDensity(item.Value, targetValue, standardDeviation))
						   .ToList();

		// 归一化权重
		var totalWeight       = weights.Sum();
		var normalizedWeights = weights.Select(weight => weight / totalWeight).ToList();

		// 加权随机选择
		return WeightedRandomChoose(items, normalizedWeights);
	}

    /// <summary>
    ///     计算一组物品效能值的方差。
    /// </summary>
    /// <param name="items"> 物品列表。 </param>
    /// <param name="mean">  物品效能值的平均数。 </param>
    /// <returns> 效能值的方差。 </returns>
    private static float CalculateVariance(List<ItemObject> items, float mean) {
		return items.Sum(item => (item.Value - mean) * (item.Value - mean)) / items.Count;
	}

    /// <summary>
    ///     计算正态分布的概率密度函数值。
    /// </summary>
    /// <param name="x">     待计算的效能值。 </param>
    /// <param name="mu">    正态分布的均值，即目标效能值。 </param>
    /// <param name="sigma"> 正态分布的标准差，由物品效能值的方差计算得出。 </param>
    /// <returns> 给定效能值在正态分布中的概率密度。 </returns>
    private static float CalculateProbabilityDensity(float x, float mu, float sigma) {
		return (float)(1 / (sigma * Math.Sqrt(2 * Math.PI)) * Math.Exp(-Math.Pow(x - mu, 2) / (2 * sigma * sigma)));
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

		for (var i = 0; i < items.Count; i++)
			if (randomNumber < cumulativeWeights[i])
				return items[i];
		Global.Warn("nothing selected in WeightedRandomChoose, return the last item");
		// 默认返回最后一个item，理论上不应该执行到这里
		return items.Last();
	}
}