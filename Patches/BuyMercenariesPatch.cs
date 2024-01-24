using System.Diagnostics;
using HarmonyLib;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace Bannerlord.DynamicTroop.Patches;

[HarmonyPatch(typeof(TroopRoster), "AddToCounts")]
public static class BuyMercenariesPatch {
	public static void Prefix(TroopRoster     __instance,
							  CharacterObject character,
							  int             count,
							  bool            insertAtFront  = false,
							  int             woundedCount   = 0,
							  int             xpChange       = 0,
							  bool            removeDepleted = true,
							  int             index          = -1) {
		if (__instance != Campaign.Current.MainParty.MemberRoster || count <= 0) return;

		// 获取当前方法的 StackTrace
		StackTrace stackTrace = new();
		var        frames     = stackTrace.GetFrames();

		// 检查是否存在调用堆栈中的 buy_mercenaries_on_consequence 方法
		foreach (var frame in frames) {
			var method = frame.GetMethod();
			if (method?.Name is "buy_mercenaries_on_consequence" or "BuyMercenaries" &&
				method.DeclaringType?.Name == "RecruitmentCampaignBehavior") {
				Global.Log($"recruiting {count}x{character.Name}", Colors.Green, Level.Debug);
				var equipments = RecruitmentPatch.GetRecruitEquipments(character);
				Global.Debug($"{equipments.Count * count} starting equipments added");
				foreach (var equipment in equipments)
					if (equipment is { IsEmpty: false, Item: not null })
						ArmyArmory.AddItemToArmory(equipment.Item, count);

				return;
			}
		}
	}
}