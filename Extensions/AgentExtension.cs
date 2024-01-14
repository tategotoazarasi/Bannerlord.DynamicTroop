using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop.Extensions;

public static class AgentExtension {
	public static bool IsValid(this Agent? agent) {
		return agent is {
							Formation: not null,
							Character: not null,
							Team     : { MBTeam: { }, IsValid: true },
							Origin   : not null
						};
	}

	public static List<ItemObject> GetAgentArmors(this Agent? agent) {
		List<ItemObject> armors = new();
		if (agent == null) return armors;

		foreach (var slot in Global.ArmourAndHorsesSlots) {
			var element = agent.SpawnEquipment.GetEquipmentFromSlot(slot);
			if (element is { IsEmpty: false, Item: { HasArmorComponent: true } item }) armors.Add(item);
		}

		return armors;
	}
}