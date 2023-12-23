#region

	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class Assignment {
		private static int counter;

		public Equipment Equipment;

		public Assignment(CharacterObject character) {
			Index              = ++counter;
			Character          = character;
			Equipment          = ArmyArmory.CreateEmptyEquipment();
			ReferenceEquipment = character.RandomBattleEquipment.Clone();

			//Global.Log($"agent {Index} select {ReferenceEquipment.CalculateEquipmentCode()} as equipment");
		}

		public int Index { get; }

		public bool IsAssigned { get; set; }

		public CharacterObject Character { get; }

		public Equipment ReferenceEquipment { get; }

		public bool IsUnarmed() {
			return (Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon0).IsEmpty ||
					Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon0).Item == null) &&
				   (Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1).IsEmpty ||
					Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1).Item == null) &&
				   (Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2).IsEmpty ||
					Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2).Item == null) &&
				   (Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3).IsEmpty ||
					Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3).Item == null);
		}

		public void EquipAnother(Equipment equipment) {
			foreach (var slot in Global.EquipmentSlots)
				//var toAdd = Equipment.GetEquipmentFromSlot(slot);
				equipment.AddEquipmentToSlotWithoutAgent(slot, Equipment.GetEquipmentFromSlot(slot));
		}
	}