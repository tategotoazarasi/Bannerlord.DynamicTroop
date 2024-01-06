#region

	using System.Collections.Generic;
	using System.Linq;
	using log4net.Core;
	using TaleWorlds.CampaignSystem.Roster;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public static class ArmyArmory {
		public static ItemRoster Armory = new();

		public static void AddItemToArmory(ItemObject item, int count = 1) { _ = Armory.AddToCounts(item, count); }

		public static void ReturnEquipmentToArmoryFromAgents(IEnumerable<Agent> agents) {
			Global.Log("ReturnEquipmentToArmoryFromAgents", Colors.Green, Level.Debug);
			var count = 0;
			foreach (var agent in agents)
				if (Global.IsAgentValid(agent)) {
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

		public static Equipment CreateEmptyEquipment() {
			Equipment emptyEquipment = new();
			foreach (var slot in Global.EquipmentSlots)
				emptyEquipment.AddEquipmentToSlotWithoutAgent(slot, new EquipmentElement());

			return emptyEquipment;
		}

		public static void AssignEquipment(Equipment equipment) {
			foreach (var slot in Global.EquipmentSlots) {
				var element = equipment.GetEquipmentFromSlot(slot);
				if (!element.IsEmpty && element.Item != null) {
					var itemToAssign = Armory.FirstOrDefault(a => !a.IsEmpty &&
																  a.EquipmentElement.Item.StringId ==
																  element.Item.StringId &&
																  a.Amount > 0);
					if (itemToAssign.IsEmpty)
						Global.Log($"Assigning Empty item {element.Item.StringId}", Colors.Red, Level.Warn);
					else
						_ = Armory.AddToCounts(itemToAssign.EquipmentElement, -1);
				}
			}
		}
	}