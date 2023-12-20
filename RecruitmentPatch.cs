#region

using HarmonyLib;

using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
using TaleWorlds.Library;

#endregion

namespace Bannerlord.DynamicTroop;

[HarmonyPatch(typeof(RecruitmentVM), "ExecuteDone")]
public class RecruitmentPatch {
	// Postfix方法
	public static void Prefix(RecruitmentVM __instance) {
		InformationManager.DisplayMessage(new InformationMessage("ExecuteDonePrefix", Colors.Green));
		foreach (RecruitVolunteerTroopVM? troop in __instance.TroopsInCart) {
			// 在这里实现将士兵基础装备添加到军火库的逻辑
			if (!troop.IsTroopEmpty) {
				InformationManager.DisplayMessage(new InformationMessage("SUCCESSFUL RECRUITMENT", Colors.Green));
				ArmyArmory.AddSoldierEquipmentToArmory(troop.Character);
			} else { InformationManager.DisplayMessage(new InformationMessage("FAILED RECRUITMENT", Colors.Red)); }
		}
	}
}