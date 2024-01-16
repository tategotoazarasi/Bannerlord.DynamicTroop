using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop;

public class HorseAndHarness {
	public HorseAndHarness(EquipmentElement horse, EquipmentElement? harness) {
		Horse   = horse;
		if(harness is {IsEmpty:false, Item: not null})
			Harness = harness;
	}

	public EquipmentElement  Horse   { get; }
	public EquipmentElement? Harness { get; }

	public int Tier => (int)Horse.Item.Tier + (int)(Harness?.Item?.Tier ?? ItemObject.ItemTiers.Tier1);

	public int Value => Horse.Item.Value + (Harness?.Item?.Value ?? 0);

	public ArmorComponent.ArmorMaterialTypes MaterialType =>
		Harness?.Item?.ArmorComponent?.MaterialType ?? ArmorComponent.ArmorMaterialTypes.None;

	public BasicCultureObject Culture => Harness?.Item?.Culture ?? Horse.Item.Culture;
}