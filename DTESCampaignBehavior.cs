using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace DTES2;

public class DTESCampaignBehavior : CampaignBehaviorBase {
	private readonly GlobalArmories _data = new();

	public override void RegisterEvents() {
		CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, this.OnTroopRecruited);
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, this.OnMobilePartyCreated);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, this.OnMobilePartyDestroyed);
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, this.OnSessionLaunched);
	}

	public void OnTroopRecruited(
		Hero            recruiterHero,
		Settlement      recruitmentSettlement,
		Hero            recruitmentSource,
		CharacterObject troop,
		int             amount
	) { }

	public override void SyncData(IDataStore dataStore) {
		Dictionary<MobileParty, List<SaveableArmoryEntry>> saveData = [];

		if (dataStore.IsSaving) {
			saveData = this._data.ToSavable();
			_        = dataStore.SyncData("dtes2data", ref saveData);
			this._data.DebugPrint();
		} else {
			_ = dataStore.SyncData("dtes2data", ref saveData);
			this._data.FromSavable(saveData);
			this._data.DebugPrint();
		}
	}

	public void OnMobilePartyCreated(MobileParty party) {
		_ = this._data.AddNewParty(party);
		Logger.Instance.Information($"Party {party.Name} created.");
	}

	public void OnMobilePartyDestroyed(MobileParty party, PartyBase partyBase) {
		_ = this._data.RemoveParty(party);
		Logger.Instance.Information($"Party {party.Name} destroyed.");
	}

	private void OnSessionLaunched(CampaignGameStarter starter) => this.AddTownMenuOptions(starter);

	private void AddTownMenuOptions(CampaignGameStarter starter) {
		this.AddArmyArmorySubmenu(starter);
		starter.AddGameMenuOption(
			"town",
			"player_armory_manage",
			"军械库",
			args => true,
			args => {
				// 打开子菜单
				GameMenu.SwitchToMenu("player_armory_submenu");
			},
			false,
			4
		);
	}

	private void AddArmyArmorySubmenu(CampaignGameStarter starter) {
		// 创建子菜单
		starter.AddGameMenu("player_armory_submenu", "军械库", args => { });

		// 在子菜单中添加选项
		starter.AddGameMenuOption(
			"player_armory_submenu",
			"view_armory",
			"查看军械库",
			args => true,
			args => {
				InventoryManager.OpenScreenAsReceiveItems(
					this._data.GetArmory(MobileParty.MainParty).ToItemRoster(),
					new TextObject("军械库"),
					() => {
						IEnumerable<ItemRosterElement>? res =
							InventoryManager.InventoryLogic.GetElementsInRoster(
								InventoryLogic.InventorySide.OtherInventory
							);
						foreach (ItemRosterElement ele in res) {
							Logger.Instance.Information($"{ele.EquipmentElement.Item.Name},{ele.Amount}");
						}

						this._data.GetArmory(MobileParty.MainParty).FillFromRoster(res);
					}
				);
			}
		);

		// 返回上一级菜单的选项
		starter.AddGameMenuOption(
			"player_armory_submenu",
			"return_to_town",
			"回到城镇",
			args => true,
			args => {
				GameMenu.SwitchToMenu("town");
			},
			true
		);
	}
}