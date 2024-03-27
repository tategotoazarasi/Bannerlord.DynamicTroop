using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.DynamicTroop.Benchmark {
	//[MemoryDiagnoser]
	//[SimpleJob(RuntimeMoniker.Net472)]
	//[SimpleJob(RuntimeMoniker.Net60)]
	[RPlotExporter]
	public class WeightedRandomSelectorBenchmarks {
		private List<ItemObject> items;
		private float            targetValue;

		[Params(20, 50,100,200)]
		public int N;

		[GlobalSetup]
		public void Setup() {
			if (MBObjectManager.Instance?.GetObjectTypeList<ItemObject>() == null) {
				Global.Error("instance is null");
				MBObjectManager.Init();
			}
			else {
				Global.Debug($"not null, {MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Count}");
			}
			var list = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(item => item.Effectiveness > 0).ToListQ();
			targetValue = list.Average(item => item.Effectiveness);
			list.Shuffle();
			items = list.Take(N).ToList();
		}

		[Benchmark]
		public void SelectItemBeforeOptimization() {
			// 调用“优化前”的方法
			WeightedRandomSelectorLegacy.SelectItem(items, targetValue);
		}

		[Benchmark]
		public void SelectItemAfterOptimization() {
			// 调用“优化后”的方法
			WeightedRandomSelector.SelectItem(items, targetValue);
		}
	}

}
