using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace Bannerlord.DynamicTroop.TroopEquipmentStrategies;

public abstract class TroopEquipmentStrategy {
	protected readonly Dictionary<EquipmentElement, int> ArmoryDict;

	protected TroopEquipmentStrategy(Dictionary<EquipmentElement, int> armoryDict) { ArmoryDict = armoryDict; }

	public virtual int Priority => 0;

	public abstract bool Matches(CharacterObject soldier);

	public virtual Assignment AssignEquipment(CharacterObject soldier) {
		Assignment assignment = new(soldier);
		foreach (var slot in Global.ArmourSlots) {
			var index = Helper.EquipmentIndexToItemEnumType(slot);
			if (!index.HasValue) continue;

			var armor = AssignArmor(soldier, index.Value);
			if (armor.HasValue) {
				assignment.AddEquipment(slot, armor.Value);
				ArmoryDict[armor.Value]--;
				Global.Debug($"Assign {armor.Value.ItemModifier?.Name ?? new TextObject()} {armor.Value.Item.Name} to {soldier.Name}#{assignment.Index}");
			}
		}

		var hoh = AssignHorseAndHarness(soldier);
		if (hoh != null) assignment.AddHorseAndHarness(hoh);

		//Global.Debug($"Assign {hoh.Value.ItemModifier.Name} {armor.Value.Item.Name} to {soldier.Name}#{assignment.Index}");
		foreach (var slot in Global.WeaponSLots) {
			var weapon = AssignWeapon(soldier, assignment.ReferenceEquipment.GetEquipmentFromSlot(slot));
			if (weapon.HasValue) {
				assignment.AddEquipment(slot, weapon.Value);
				ArmoryDict[weapon.Value]--;
				Global.Debug($"Assign {weapon.Value.ItemModifier?.Name ?? new TextObject()} {weapon.Value.Item.Name} to {soldier.Name}#{assignment.Index}");
			}
		}

		return assignment;
	}

	protected abstract HorseAndHarness? AssignHorseAndHarness(CharacterObject soldier);

	protected abstract EquipmentElement? AssignWeapon(CharacterObject soldier, EquipmentElement refWeapon);

	protected abstract EquipmentElement? AssignArmor(CharacterObject soldier, ItemObject.ItemTypeEnum type);
}