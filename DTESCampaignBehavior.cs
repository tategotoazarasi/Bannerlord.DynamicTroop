#region
using Bannerlord.ButterLib.SaveSystem.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
#endregion
namespace DTES2;

public class DTESCampaignBehavior : CampaignBehaviorBase {
	private GlobalArmories _data = new GlobalArmories();

	public override void RegisterEvents() {
		CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, this.OnTroopRecruited);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, this.OnMobilePartyCreated);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, this.OnMobilePartyDestroyed);
	}

	public void OnTroopRecruited(
		Hero            recruiterHero,
		Settlement      recruitmentSettlement,
		Hero            recruitmentSource,
		CharacterObject troop,
		int             amount
	) { }

	public override void SyncData(IDataStore dataStore) => dataStore.SyncDataAsJson("dtes2armory", ref this._data);

	public void OnMobilePartyCreated(MobileParty party) => this._data.AddNewParty(party);

	public void OnMobilePartyDestroyed(MobileParty party, PartyBase partyBase) => this._data.RemoveParty(party);

	private void OnSessionLaunched(CampaignGameStarter starter) => this.AddTownMenuOptions(starter);

	private void AddTownMenuOptions(CampaignGameStarter starter) {
		this.AddArmyArmorySubmenu(starter);
		starter.AddGameMenuOption("town",
								  "player_armory_manage",
								  "军械库",
								  args => true,
								  args => {
									  // 打开子菜单
									  GameMenu.SwitchToMenu("player_armory_submenu");
								  },
								  false,
								  4);
	}

	private void AddArmyArmorySubmenu(CampaignGameStarter starter) {
		// 创建子菜单
		starter.AddGameMenu("player_armory_submenu", "军械库", args => { });

		// 在子菜单中添加选项
		starter.AddGameMenuOption("player_armory_submenu",
								  "view_armory",
								  "查看军械库",
								  args => true,
								  args => {
									  InventoryManager.OpenScreenAsStash(
										  this._data.GetArmory(MobileParty.MainParty).ToItemRoster());
								  });

		// 返回上一级菜单的选项
		starter.AddGameMenuOption("player_armory_submenu",
								  "return_to_town",
								  "回到城镇",
								  args => true,
								  args => {
									  GameMenu.SwitchToMenu("town");
								  },
								  true);
	}
}