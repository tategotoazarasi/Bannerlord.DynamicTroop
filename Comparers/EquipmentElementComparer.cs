using System.Collections.Generic;
using TaleWorlds.Core;

namespace DynamicTroopEquipmentReupload.Comparers;

public class EquipmentElementComparer : IEqualityComparer<EquipmentElement> {
	public bool Equals(EquipmentElement x, EquipmentElement y) {
		if (x.Item        == null || y.Item == null)
			return x.Item == y.Item;

		return x.Item.StringId == y.Item.StringId;
	}

	public int GetHashCode(EquipmentElement obj) {
		//if (ReferenceEquals(obj, null)) return 0;

		// 计算哈希码 例如，结合 EquipmentElement 的某些属性
		return obj.Item == null ? 0 : obj.Item.StringId.GetHashCode();
	}
}