using System.Collections.Generic;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Comparers;

public class ArmorComparer : IComparer<ItemObject> {
	public int Compare(ItemObject x, ItemObject y) {
		var tierComparison = x.Tier.CompareTo(y.Tier);
		return tierComparison != 0 ? tierComparison : x.Value.CompareTo(y.Value);
	}
}