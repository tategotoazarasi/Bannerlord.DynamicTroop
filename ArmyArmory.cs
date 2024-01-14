using System.Collections.Generic;
using Bannerlord.DynamicTroop.Extensions;
using log4net.Core;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop;

public static class ArmyArmory {
	public static readonly ItemRoster Armory = new();

	public static void AddItemToArmory(ItemObject item, int count = 1) { _ = Armory.AddToCounts(item, count); }

	public static void ReturnEquipmentToArmoryFromAgents(IEnumerable<Agent> agents) {
		Global.Log("ReturnEquipmentToArmoryFromAgents", Colors.Green, Level.Debug);
		var count = 0;
		foreach (var agent in agents)
			if (agent.IsValid()) {
				Global.Log($"Returning equipment of agent {agent.Character.StringId}", Colors.Green, Level.Debug);

				Global.ProcessAgentEquipment(agent,
											 item => {
												 _ = Armory.AddToCounts(item, 1);
												 Global.Log($"equipment {item.StringId} returned",
															Colors.Green,
															Level.Debug);
												 count++;
											 });
			}

		Global.Log($"{count} equipment reclaimed", Colors.Green, Level.Debug);
	}

	public static void AssignEquipment(Equipment equipment) {
		foreach (var slot in Global.EquipmentSlots) {
			var element = equipment.GetEquipmentFromSlot(slot);

			// 使用模式匹配来检查条件，并反转if语句来减少嵌套
			if (element.IsEmpty || element.Item is null) continue;

			var itemToAssign = Armory.FirstOrDefaultQ(a => !a.IsEmpty                                                &&
														   a.EquipmentElement.Item.StringId == element.Item.StringId &&
														   a.Amount                         > 0);

			if (!itemToAssign.IsEmpty)
				_ = Armory.AddToCounts(itemToAssign.EquipmentElement, -1);
			else
				Global.Log($"Assigning Empty item {element.Item.StringId}", Colors.Red, Level.Warn);
		}
	}
}