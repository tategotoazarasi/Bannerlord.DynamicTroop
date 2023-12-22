#region

	using System.Linq;
	using HarmonyLib;
	using TaleWorlds.Core;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(Mission), "SpawnAgent")]
	public class SpawnAgentPatch {
		private static void Prefix(Mission __instance, ref AgentBuildData agentBuildData) {
			// 确保当前任务是战斗类型的，并且AgentBuildData及其属性已初始化
			if (__instance.CombatType         == Mission.MissionCombatType.Combat &&
				__instance.PlayerTeam         != null                             &&
				agentBuildData.AgentCharacter != null                             &&
				agentBuildData.AgentFormation != null                             &&
				agentBuildData.AgentTeam      != null                             &&
				agentBuildData.AgentOrigin    != null                             &&
				agentBuildData.AgentOrigin.IsUnderPlayersCommand                  &&

				//agentBuildData.AgentOrigin is PartyAgentOrigin origin &&
				//origin.Party.MobileParty == Hero.MainHero.PartyBelongedTo &&
				agentBuildData.AgentTeam.IsValid      &&
				agentBuildData.AgentTeam.IsPlayerTeam &&
				!agentBuildData.AgentCharacter.IsHero) {
				// 判断是否骑马
				var isMounted = agentBuildData.AgentCharacter != null && agentBuildData.AgentCharacter.IsMounted;
				var equipment = agentBuildData.AgentCharacter.AllEquipments.Where(e => !e.IsCivilian)
											  .ToList()
											  .GetRandomElement();

				// 获取原有的武器类别
				var weaponClass0 = ArmyArmory.GetWeaponClassFromEquipment(equipment, EquipmentIndex.Weapon0);
				var weaponClass1 = ArmyArmory.GetWeaponClassFromEquipment(equipment, EquipmentIndex.Weapon1);
				var weaponClass2 = ArmyArmory.GetWeaponClassFromEquipment(equipment, EquipmentIndex.Weapon2);
				var weaponClass3 = ArmyArmory.GetWeaponClassFromEquipment(equipment, EquipmentIndex.Weapon3);

				// 创建一个新的空装备实例
				var newEquipment = ArmyArmory.CreateEmptyEquipment();

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