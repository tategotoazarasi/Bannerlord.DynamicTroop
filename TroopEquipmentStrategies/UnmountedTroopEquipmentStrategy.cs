using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.DynamicTroop.Comparers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.TroopEquipmentStrategies;

[Obsolete]
public class UnmountedTroopEquipmentStrategy : TroopEquipmentStrategy {
	private Common common = new();
	public UnmountedTroopEquipmentStrategy(Dictionary<EquipmentElement, int> armory) : base(armory) { }

	public override bool Matches(CharacterObject soldier) { throw new NotImplementedException(); }

	protected override void AssignArmor(CharacterObject soldier, ItemObject.ItemTypeEnum type) {
		var armors = _armory.Keys.Where(e => e is { IsEmpty: false, Item: not null } && e.Item.ItemType == type)
							.ToArray();
		Array.Sort(armors, new ArmorElementComparer());
		
	}

	protected override void AssignHorseAndHarness(CharacterObject soldier) { throw new NotImplementedException(); }

	protected override void AssignWeapon(CharacterObject soldier, EquipmentIndex index) {
		throw new NotImplementedException();
	}
}