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
		if (skill == DefaultSkills.Bow) return ItemObject.ItemTypeEnum.Bow;
		if (skill == DefaultSkills.Crossbow) return ItemObject.ItemTypeEnum.Crossbow;
		if (skill == DefaultSkills.Throwing) return ItemObject.ItemTypeEnum.Thrown;
		if (skill == DefaultSkills.Riding) return ItemObject.ItemTypeEnum.Horse;
		return ItemObject.ItemTypeEnum.Invalid;
	}
}