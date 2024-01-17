using System.Collections.Generic;
using Bannerlord.DynamicTroop.Extensions;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Comparers;

public class ArmorComparer : IComparer<ItemObject> {
	public int Compare(ItemObject x, ItemObject y) { return x.CompareArmor(y); }
}