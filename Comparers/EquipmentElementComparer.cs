using System.Collections.Generic;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Comparers;

public class EquipmentElementComparer : IEqualityComparer<EquipmentElement> {
	//[Cache]
	public bool Equals(EquipmentElement x, EquipmentElement y) {
		/*// 检查是否为同一个对象的引用或都为 null
		if (ReferenceEquals(x, y)) return true;

		if (ReferenceEquals(x, null) || ReferenceEquals(y, null)) return false;

		// 在这里添加比较逻辑 例如，比较 EquipmentElement 的某些特定属性*/
		return x.Item.Id == y.Item.Id;
	}

	//[Cache]
	public int GetHashCode(EquipmentElement obj) {
		//if (ReferenceEquals(obj, null)) return 0;

		// 计算哈希码 例如，结合 EquipmentElement 的某些属性
		return (int)((obj.Item         == null ? 0 : obj.Item.Id.InternalValue) ^
					 (obj.ItemModifier == null ? 0 : obj.ItemModifier.Id.InternalValue));
	}
}