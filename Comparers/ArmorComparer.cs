using System.Collections.Generic;
using Bannerlord.DynamicTroop.Extensions;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Comparers;

public class ArmorComparer : IComparer<ItemObject> {
	//[Cache]
	public int Compare(ItemObject x, ItemObject y) { return y.CompareArmor(x); }
}

public class ArmorElementComparer : IComparer<EquipmentElement> {
	//[Cache]
	public int Compare(EquipmentElement x, EquipmentElement y) { return y.Item.CompareArmor(x.Item); }
}