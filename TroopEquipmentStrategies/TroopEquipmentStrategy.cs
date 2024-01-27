using System;
using System.Collections.Generic;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.TroopEquipmentStrategies;

[Obsolete]
public abstract class TroopEquipmentStrategy {
	protected Dictionary<EquipmentElement, int> _armory;

	public TroopEquipmentStrategy(Dictionary<EquipmentElement, int> armory) { _armory = armory; }

	public int Priority => 0;

	public abstract bool Matches(CharacterObject soldier);

	public Assignment AssignEquipment(CharacterObject soldier) {
		foreach (var slot in Global.ArmourSlots) {
			var index = Helper.EquipmentIndexToItemEnumType(slot);
			if (!index.HasValue) continue;
			AssignArmor(soldier, index.Value);
		}

		AssignHorseAndHarness(soldier);

		foreach (var slot in Global.WeaponSLots) AssignWeapon(soldier, slot);
	}

	protected abstract HorseAndHarness AssignHorseAndHarness(CharacterObject soldier);
	protected abstract void AssignWeapon(CharacterObject          soldier, EquipmentIndex          index);
	protected abstract void AssignArmor(CharacterObject           soldier, ItemObject.ItemTypeEnum type);
}