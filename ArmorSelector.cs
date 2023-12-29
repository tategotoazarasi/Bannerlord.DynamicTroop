#region

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using TaleWorlds.Core;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class ArmorSelector {
		private static readonly Random Random = new();

		public static ItemObject? GetRandomArmorByBodyPart(List<ItemObject> armors, BoneBodyPartType? bodyPart) {
			if (!bodyPart.HasValue) return null;

			List<(ItemObject Armor, int Weight)> weightedArmors = new();

			foreach (var armor in armors)
				if (armor.HasArmorComponent) {
					var armorComponent = armor.ArmorComponent;
					if (armorComponent != null) {
						var armorValue = GetArmorValueForBodyPart(armorComponent, bodyPart.Value);
						if (armorValue > 0) weightedArmors.Add((Armor: armor, Weight: armorValue));
					}
				}

			return SelectArmorBasedOnWeight(weightedArmors);
		}

		private static int GetArmorValueForBodyPart(ArmorComponent armorComponent, BoneBodyPartType bodyPart) {
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

		private static ItemObject? SelectArmorBasedOnWeight(List<(ItemObject Armor, int Weight)> weightedArmors) {
			var totalWeight = weightedArmors.Sum(a => a.Weight);
			var choice      = Random.Next(totalWeight);
			var sum         = 0;

			foreach ((var armor, var weight) in weightedArmors) {
				sum += weight;
				if (choice < sum) return armor;
			}

			return null;
		}
	}