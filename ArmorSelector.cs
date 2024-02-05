using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop;

public static class ArmorSelector {
	private static readonly Random Random = new();

    /// <summary>
    ///     根据身体部位从装甲列表中随机选择一件装甲。
    /// </summary>
    /// <param name="armors">   装甲列表。 </param>
    /// <param name="bodyPart"> 要保护的身体部位。 </param>
    /// <returns> 选中的装甲物品，如果没有合适的装甲则返回null。 </returns>
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

    /// <summary>
    ///     基于权重从装甲列表中选择一件装甲。
    /// </summary>
    /// <param name="weightedArmors"> 包含装甲及其权重的数组。 </param>
    /// <returns> 根据权重选中的装甲物品，如果没有选中任何装甲则返回null。 </returns>
    private static ItemObject? SelectArmorBasedOnWeight((ItemObject Armor, int Weight)[] weightedArmors) {
		var totalWeight = weightedArmors.SumQ(a => a.Weight);
		var choice      = Random.Next(totalWeight);
		var sum         = 0;

		foreach ((var armor, var weight) in weightedArmors) {
			sum += weight;
			if (choice < sum) return armor;
		}

		return null;
	}
}