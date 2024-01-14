using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop;

public static class ArmorSelector {
	private static readonly Random Random = new();

	public static ItemObject? GetRandomArmorByBodyPart(List<ItemObject> armors, BoneBodyPartType? bodyPart) {
		if (!bodyPart.HasValue) return null;

		var weightedArmors = armors.WhereQ(armor => armor.HasArmorComponent)
								   .SelectQ(armor => new { Armor = armor, armor.ArmorComponent })
								   .WhereQ(ac => ac.ArmorComponent != null)
								   .SelectQ(ac => new {
														  ac.Armor,
														  Weight =
															  Helper.GetArmorValueForBodyPart(ac.ArmorComponent,
																  bodyPart.Value)
													  })
								   .WhereQ(aw => aw.Weight > 0)
								   .ToArrayQ();

		return SelectArmorBasedOnWeight(weightedArmors.SelectQ(aw => (aw.Armor, aw.Weight)).ToArrayQ());
	}

	private static ItemObject? SelectArmorBasedOnWeight((ItemObject Armor, int Weight)[] weightedArmors) {
		var totalWeight = weightedArmors.SumQ(a => a.Weight);
		var choice      = Random.Next(totalWeight);
		var sum         = 0;

		foreach (var (armor, weight) in weightedArmors) {
			sum += weight;
			if (choice < sum) return armor;
		}

		return null;
	}
}