using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

public static class WeightedRandomSelector {
	private static readonly Random _random = new();

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

	private static float CalculateVariance(List<ItemObject> items, float mean) {
		return items.Sum(item => (item.Value - mean) * (item.Value - mean)) / items.Count;
	}

	private static float CalculateProbabilityDensity(float x, float mu, float sigma) {
		return (float)(1 / (sigma * Math.Sqrt(2 * Math.PI)) * Math.Exp(-Math.Pow(x - mu, 2) / (2 * sigma * sigma)));
	}

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

		// 默认返回最后一个item，理论上不应该执行到这里
		return items.Last();
	}
}