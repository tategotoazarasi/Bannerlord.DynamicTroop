using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.DynamicTroop.Comparers;
using Bannerlord.DynamicTroop.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.TroopEquipmentStrategies;

[Obsolete]
public class MountedTroopEquipmentStrategy : TroopEquipmentStrategy {
	private readonly Queue<HorseAndHarness> _horseAndHarnesses;

	public MountedTroopEquipmentStrategy(Dictionary<EquipmentElement, int> armoryDict,
										 Queue<HorseAndHarness>            horseAndHarnesses) : base(armoryDict) {
		_horseAndHarnesses = horseAndHarnesses;
	}

	public override int Priority => 2;

	public override bool Matches(CharacterObject soldier) { return soldier.IsMounted; }

	protected override EquipmentElement? AssignArmor(CharacterObject soldier, ItemObject.ItemTypeEnum type) {
		var armors = ArmoryDict
					 .Where(kvp => kvp is { Key: { IsEmpty: false, Item: not null } } &&
								   kvp.Key.Item.ItemType == type                      &&
								   kvp.Value             > 0)
					 .Select(kvp => kvp.Key)
					 .ToArray();
		if (armors.Length == 0) return null;

		Array.Sort(armors, new ArmorElementComparer());
		return armors.First();
	}

	protected override HorseAndHarness? AssignHorseAndHarness(CharacterObject soldier) {
		return _horseAndHarnesses.IsEmpty() ? null : _horseAndHarnesses.Dequeue();
	}

	protected override EquipmentElement? AssignWeapon(CharacterObject soldier, EquipmentElement refWeapon) {
		if (refWeapon is not { IsEmpty: false, Item.HasWeaponComponent: true }) return null;

		var weapons = ArmoryDict
					  .Where(kvp => kvp is { Key: { IsEmpty: false, Item.HasWeaponComponent: true }, Value: > 0 } &&
									kvp.Key.Item.IsSuitableForMount()                                             &&
									Common.Instance.GetWeaponFilterType(kvp.Key.Item) ==
									Common.Instance.GetWeaponFilterType(refWeapon.Item))
					  .Select(kvp => kvp.Key)
					  .ToArray();
		if (weapons.Length == 0) return null;

		Array.Sort(weapons,
				   (x, y) => Common.Instance.CalcConsiderValue(soldier, refWeapon, y)
								   .CompareTo(Common.Instance.CalcConsiderValue(soldier, refWeapon, x)));
		return weapons.First();
	}
}