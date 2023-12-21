#region

using HarmonyLib;

using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;

#endregion

namespace Bannerlord.DynamicTroop;

[HarmonyPatch(typeof(RecruitmentVM), "ExecuteDone")]
public class RecruitmentPatch {
	public static void Prefix(RecruitmentVM __instance) {
		//InformationManager.DisplayMessage(new InformationMessage("ExecuteDonePrefix", Colors.Green));
		foreach (RecruitVolunteerTroopVM? troop in __instance.TroopsInCart) {

			// 在这里实现将士兵基础装备添加到军火库的逻辑
			if (!troop.IsTroopEmpty) {

				//InformationManager.DisplayMessage(new InformationMessage("SUCCESSFUL RECRUITMENT", Colors.Green));
				ArmyArmory
						.AddSoldierEquipmentToArmory(troop.Character); //else { InformationManager.DisplayMessage(new InformationMessage("FAILED RECRUITMENT", Colors.Red)); }
			}
		}
	}

	//public static void Postfix() { InventoryManager.OpenScreenAsStash(ArmyArmory.Armory); }
}