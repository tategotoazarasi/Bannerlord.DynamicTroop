using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.TroopEquipmentStrategies;

[Obsolete]
public class MountedTroopEquipmentStrategy : TroopEquipmentStrategy {
	public MountedTroopEquipmentStrategy(Armory armory) : base(armory) { }

	public override bool Matches(CharacterObject soldier) { return soldier.IsMounted; }

	protected override void AssignArmor(CharacterObject soldier, ItemObject.ItemTypeEnum type) {
		throw new NotImplementedException();
	}

	protected override void AssignHorseAndHarness(CharacterObject soldier) { throw new NotImplementedException(); }

	protected override void AssignWeapon(CharacterObject soldier, EquipmentIndex index) {
		throw new NotImplementedException();
	}
}