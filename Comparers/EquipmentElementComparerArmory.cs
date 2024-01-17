using System.Collections.Generic;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Comparers;

public class EquipmentElementComparerArmory : IEqualityComparer<EquipmentElement> {
	public bool Equals(EquipmentElement x, EquipmentElement y) {
		return x.Item.Id == y.Item.Id && x.ItemModifier.Id == y.ItemModifier.Id;
	}

	public int GetHashCode(EquipmentElement obj) {
		return obj.Item.Id.GetHashCode() ^ obj.ItemModifier.Id.GetHashCode();
	}
}