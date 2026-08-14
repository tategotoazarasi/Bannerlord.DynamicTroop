using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace DynamicTroopEquipmentReupload.Patches;

public static class BrokenShieldPatch {
	private static void AgentShieldDamagedPrefix(Agent __instance, EquipmentIndex slotIndex, int inflictedDamage) {
		var shield = __instance.Equipment[slotIndex];
		var spawnShield = __instance.SpawnEquipment[slotIndex];
		if (shield.HitPoints > inflictedDamage ||
			spawnShield.Item != shield.Item ||
			spawnShield.ItemModifier != shield.ItemModifier)
			return;

		__instance.Mission?.GetMissionBehavior<DynamicTroopMissionLogic>()?.RegisterBrokenShield(__instance, slotIndex);
	}
}
