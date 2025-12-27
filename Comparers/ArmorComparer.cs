using System.Collections.Generic;
using DynamicTroopEquipmentReupload.Extensions;
using TaleWorlds.Core;

namespace DynamicTroopEquipmentReupload.Comparers;

public class ArmorComparer : IComparer<ItemObject> {
	public int Compare(ItemObject x, ItemObject y) { return y.CompareArmor(x); }
}

public class ArmorElementComparer : IComparer<EquipmentElement> {
	public int Compare(EquipmentElement x, EquipmentElement y) { return y.Item.CompareArmor(x.Item); }
}

public class EquipmentEffectivenessComparer : IComparer<ItemObject> {
	public int Compare(ItemObject x, ItemObject y) { return y.Effectiveness.CompareTo(x.Effectiveness); }
}