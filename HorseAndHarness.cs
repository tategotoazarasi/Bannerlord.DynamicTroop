using System;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

public class HorseAndHarness : IComparable {
	public HorseAndHarness(EquipmentElement horse, EquipmentElement? harness) {
		Horse = horse;
		if (harness is { IsEmpty: false, Item: not null }) Harness = harness;
	}

	public EquipmentElement Horse { get; }

	public EquipmentElement? Harness { get; set; }

	public int Tier => (int)Horse.Item.Tier + (int)(Harness?.Item?.Tier ?? ItemObject.ItemTiers.Tier1);

	public int Value => Horse.Item.Value + (Harness?.Item?.Value ?? 0);

	public ArmorComponent.ArmorMaterialTypes MaterialType =>
		Harness?.Item?.ArmorComponent?.MaterialType ?? ArmorComponent.ArmorMaterialTypes.None;

	public BasicCultureObject Culture => Harness?.Item?.Culture ?? Horse.Item.Culture;

	public int CompareTo(object? obj) {
		if (obj == null) return 1;

		if (obj is not HorseAndHarness other) throw new ArgumentException("Object is not a HorseAndHarness");

		// 规则 1: 没有马甲的排在有马甲的前面
		if (Harness == null && other.Harness != null) return -1;

		if (Harness != null && other.Harness == null) return 1;

		// 规则 2: Tier 低的排在 Tier 高的前面
		var tierComparison = Tier.CompareTo(other.Tier);
		if (tierComparison != 0) return tierComparison;

		// 规则 3: ArmorMaterialTypes 低的排在 ArmorMaterialTypes 高的前面
		var materialTypeComparison = MaterialType.CompareTo(other.MaterialType);
		if (materialTypeComparison != 0) return materialTypeComparison;

		// 规则 4: Value 低的排在 Value 高的前面
		return Value.CompareTo(other.Value);
	}
}