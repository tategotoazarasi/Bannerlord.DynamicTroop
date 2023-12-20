#region

using HarmonyLib;

using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

#endregion

namespace Bannerlord.DynamicTroop;

[HarmonyPatch(typeof(Mission), "SpawnAgent")]
public class SpawnAgentPatch {
	private static void Prefix(ref AgentBuildData agentBuildData) {
		if (agentBuildData.AgentCharacter != null &&
			agentBuildData.AgentTeam.IsPlayerTeam &&
			!agentBuildData.AgentCharacter.IsHero) {
			// 判断是否骑马
			bool isMounted = agentBuildData.AgentCharacter != null && agentBuildData.AgentCharacter.IsMounted;
			Equipment equipment = agentBuildData.AgentCharacter.AllEquipments.GetRandomElement();

			// 获取原有的武器类别
			WeaponClass? weaponClass0 = ArmyArmory.GetWeaponClassFromEquipment(equipment, EquipmentIndex.Weapon0);
			WeaponClass? weaponClass1 = ArmyArmory.GetWeaponClassFromEquipment(equipment, EquipmentIndex.Weapon1);
			WeaponClass? weaponClass2 = ArmyArmory.GetWeaponClassFromEquipment(equipment, EquipmentIndex.Weapon2);
			WeaponClass? weaponClass3 = ArmyArmory.GetWeaponClassFromEquipment(equipment, EquipmentIndex.Weapon3);

			// 创建一个新的空装备实例
			Equipment newEquipment = ArmyArmory.CreateEmptyEquipment();

			// 从军械库中为每个装备槽位分配装备
			ArmyArmory.AssignEquipmentFromArmory(newEquipment,
												 weaponClass0,
												 weaponClass1,
												 weaponClass2,
												 weaponClass3,
												 isMounted);

			// 更新AgentBuildData的装备
			agentBuildData = agentBuildData.Equipment(newEquipment);
		}
	}
}