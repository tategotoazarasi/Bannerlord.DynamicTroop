#region

	using HarmonyLib;
	using TaleWorlds.Core;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(Mission), "SpawnAgent")]
	public class SpawnAgentPatch {
		/*private static void Prefix(ref AgentBuildData agentBuildData) {
			InformationManager.DisplayMessage(new InformationMessage("PREFIX PATCH START", Colors.Red));

			// 寻找帝国具装骑兵的Character对象
			if (agentBuildData.AgentCharacter.IsHero) {
				var imperialCataphract = CharacterObject.Find("imperial_cataphract");
				if (imperialCataphract != null) {

					// 更改AgentBuildData以生成帝国具装骑兵
					agentBuildData = agentBuildData.Character(imperialCataphract);
					InformationManager.DisplayMessage(new InformationMessage("PREFIX REPLACED ONE", Colors.Red));
				}
				else {
					InformationManager.DisplayMessage(new InformationMessage("imperial_cataphract NOT FOUND",
																			 Colors.Red));
				}
			}
		}*/

		private static void Prefix(ref AgentBuildData agentBuildData) {
			if (agentBuildData.AgentTeam.IsPlayerTeam) {
				// 创建一个新的空装备实例
				var newEquipment = CreateEmptyEquipment();

				// 从军械库中为每个装备槽位分配装备
				AssignEquipmentFromArmory(newEquipment);

				// 更新AgentBuildData的装备
				agentBuildData = agentBuildData.Equipment(newEquipment);
			}
		}

		private static Equipment CreateEmptyEquipment() {
			var emptyEquipment = new Equipment();
			EquipmentIndex[] slots = {
										 EquipmentIndex.Weapon0,
										 EquipmentIndex.Weapon1,
										 EquipmentIndex.Weapon2,
										 EquipmentIndex.Weapon3,
										 EquipmentIndex.Head,
										 EquipmentIndex.Body,
										 EquipmentIndex.Leg,
										 EquipmentIndex.Gloves,
										 EquipmentIndex.Cape,
										 EquipmentIndex.Horse,
										 EquipmentIndex.HorseHarness
									 };

			foreach (var slot in slots) emptyEquipment.AddEquipmentToSlotWithoutAgent(slot, new EquipmentElement());

			return emptyEquipment;
		}

		private static void AssignEquipmentFromArmory(Equipment equipment) {
			// 从军械库中为每个装备槽位分配装备
			EquipmentIndex[] slots = {
										 EquipmentIndex.Weapon0,
										 EquipmentIndex.Weapon1,
										 EquipmentIndex.Weapon2,
										 EquipmentIndex.Weapon3,
										 EquipmentIndex.Head,
										 EquipmentIndex.Body,
										 EquipmentIndex.Leg,
										 EquipmentIndex.Gloves,
										 EquipmentIndex.Cape,
										 EquipmentIndex.Horse,
										 EquipmentIndex.HorseHarness
									 };

			foreach (var slot in slots) {
				var bestItem = ArmyArmory.FindBestItemForSlot(slot); // 实现这个方法以选择最佳装备

				//if (bestItem!=null && !bestItem.IsEmpty) equipment.AddEquipmentToSlotWithoutAgent(slot, bestItem);
				equipment.AddEquipmentToSlotWithoutAgent(slot, bestItem);
			}
		}
	}