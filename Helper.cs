using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop;

public static class Helper {
	public static EquipmentIndex? ItemEnumTypeToEquipmentIndex(ItemObject.ItemTypeEnum itemType) {
		return itemType switch {
				   ItemObject.ItemTypeEnum.HeadArmor    => EquipmentIndex.Head,
				   ItemObject.ItemTypeEnum.HandArmor    => EquipmentIndex.Gloves,
				   ItemObject.ItemTypeEnum.BodyArmor    => EquipmentIndex.Body,
				   ItemObject.ItemTypeEnum.LegArmor     => EquipmentIndex.Leg,
				   ItemObject.ItemTypeEnum.Cape         => EquipmentIndex.Cape,
				   ItemObject.ItemTypeEnum.Horse        => EquipmentIndex.Horse,
				   ItemObject.ItemTypeEnum.HorseHarness => EquipmentIndex.HorseHarness,
				   _                                    => null
			   };
	}

	public static ItemObject.ItemTypeEnum? EquipmentIndexToItemEnumType(EquipmentIndex index) {
		return index switch {
				   EquipmentIndex.Head         => ItemObject.ItemTypeEnum.HeadArmor,
				   EquipmentIndex.Gloves       => ItemObject.ItemTypeEnum.HandArmor,
				   EquipmentIndex.Body         => ItemObject.ItemTypeEnum.BodyArmor,
				   EquipmentIndex.Leg          => ItemObject.ItemTypeEnum.LegArmor,
				   EquipmentIndex.Cape         => ItemObject.ItemTypeEnum.Cape,
				   EquipmentIndex.Horse        => ItemObject.ItemTypeEnum.Horse,
				   EquipmentIndex.HorseHarness => ItemObject.ItemTypeEnum.HorseHarness,
				   _                           => null
			   };
	}

	public static int GetArmorValueForBodyPart(ArmorComponent armorComponent, BoneBodyPartType bodyPart) {
		return bodyPart switch {
				   BoneBodyPartType.Head          => armorComponent.HeadArmor,
				   BoneBodyPartType.Neck          => armorComponent.HeadArmor,
				   BoneBodyPartType.Chest         => armorComponent.BodyArmor,
				   BoneBodyPartType.Abdomen       => armorComponent.BodyArmor,
				   BoneBodyPartType.ShoulderLeft  => armorComponent.BodyArmor,
				   BoneBodyPartType.ShoulderRight => armorComponent.BodyArmor,
				   BoneBodyPartType.ArmLeft       => armorComponent.ArmArmor,
				   BoneBodyPartType.ArmRight      => armorComponent.ArmArmor,
				   BoneBodyPartType.Legs          => armorComponent.LegArmor,
				   _                              => 0
			   };
	}

	public static ItemObject.ItemTypeEnum SkillObjectToItemEnumType(SkillObject skill) {
		if (skill == DefaultSkills.OneHanded) return ItemObject.ItemTypeEnum.OneHandedWeapon;

		if (skill == DefaultSkills.TwoHanded) return ItemObject.ItemTypeEnum.TwoHandedWeapon;

		if (skill == DefaultSkills.Polearm) return ItemObject.ItemTypeEnum.Polearm;

		return skill == DefaultSkills.Bow      ? ItemObject.ItemTypeEnum.Bow :
			   skill == DefaultSkills.Crossbow ? ItemObject.ItemTypeEnum.Crossbow :
			   skill == DefaultSkills.Throwing ? ItemObject.ItemTypeEnum.Thrown :
			   skill == DefaultSkills.Riding   ? ItemObject.ItemTypeEnum.Horse : ItemObject.ItemTypeEnum.Invalid;
	}

	public static ItemObject.ItemTypeEnum WeaponClassToItemEnumType(WeaponClass weaponClass) {
		return weaponClass switch {
				   WeaponClass.Dagger           => ItemObject.ItemTypeEnum.OneHandedWeapon,
				   WeaponClass.OneHandedSword   => ItemObject.ItemTypeEnum.OneHandedWeapon,
				   WeaponClass.TwoHandedSword   => ItemObject.ItemTypeEnum.TwoHandedWeapon,
				   WeaponClass.OneHandedAxe     => ItemObject.ItemTypeEnum.OneHandedWeapon,
				   WeaponClass.TwoHandedAxe     => ItemObject.ItemTypeEnum.TwoHandedWeapon,
				   WeaponClass.Mace             => ItemObject.ItemTypeEnum.OneHandedWeapon,
				   WeaponClass.Pick             => ItemObject.ItemTypeEnum.OneHandedWeapon,
				   WeaponClass.TwoHandedMace    => ItemObject.ItemTypeEnum.TwoHandedWeapon,
				   WeaponClass.OneHandedPolearm => ItemObject.ItemTypeEnum.Polearm,
				   WeaponClass.TwoHandedPolearm => ItemObject.ItemTypeEnum.Polearm,
				   WeaponClass.LowGripPolearm   => ItemObject.ItemTypeEnum.Polearm,
				   WeaponClass.Arrow            => ItemObject.ItemTypeEnum.Arrows,
				   WeaponClass.Bolt             => ItemObject.ItemTypeEnum.Bolts,
				   WeaponClass.Bow              => ItemObject.ItemTypeEnum.Bow,
				   WeaponClass.Crossbow         => ItemObject.ItemTypeEnum.Crossbow,
				   WeaponClass.Stone            => ItemObject.ItemTypeEnum.Thrown,
				   WeaponClass.ThrowingAxe      => ItemObject.ItemTypeEnum.Thrown,
				   WeaponClass.ThrowingKnife    => ItemObject.ItemTypeEnum.Thrown,
				   WeaponClass.Javelin          => ItemObject.ItemTypeEnum.Thrown,
				   WeaponClass.SmallShield      => ItemObject.ItemTypeEnum.Shield,
				   WeaponClass.LargeShield      => ItemObject.ItemTypeEnum.Shield,
				   _                            => ItemObject.ItemTypeEnum.Invalid
			   };
	}
}