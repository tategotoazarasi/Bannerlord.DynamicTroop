#region
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DTES2.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
#endregion
namespace DTES2;

public class DTESCampaignBehavior : CampaignBehaviorBase {
	private readonly GlobalArmories _data = new GlobalArmories();

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
	) {
		MobileParty? party = recruiterHero.PartyBelongedTo;
		if (recruiterHero.PartyBelongedTo == null) {
			return;
		}
		Armory? armory = this._data.GetArmory(party);
		if (armory == null) {
			return;
		}
		this.OnTroopRecruited(armory, troop, amount);
		Logger.Instance.Information($"Troop recruited: {troop.Name}, {amount} to {party.Name}");
	}

	private void OnTroopRecruited(Armory armory, CharacterObject troop, int amount) {
		ConcurrentDictionary<ItemObject, int> items = new ConcurrentDictionary<ItemObject, int>();

		Parallel.For(0,
					 amount,
					 _ => {
						 List<ItemObject> itemList = troop.GetRecruitmentEquipment();
						 foreach (ItemObject item in itemList) {
							 // 使用 AddOrUpdate 方法线程安全地更新字典
							 items.AddOrUpdate(item, 1, (key, oldValue) => oldValue + 1);
						 }
					 });
		Parallel.ForEach(items,
						 kv => {
							 armory.Store(kv.Key, kv.Value);
						 });
	}

	public override void SyncData(IDataStore dataStore) {
		Dictionary<MobileParty, List<SaveableArmoryEntry>> saveData = [];

		if (dataStore.IsSaving) {
			saveData = this._data.ToSavable();
			_        = dataStore.SyncData("dtes2data", ref saveData);
			this._data.DebugPrint();
		}
		else {
			_ = dataStore.SyncData("dtes2data", ref saveData);
			this._data.FromSavable(saveData);
			this._data.DebugPrint();
		}
	}

	public void OnMobilePartyCreated(MobileParty party) {
		_ = this._data.AddNewParty(party);
		Armory? armory = this._data.GetArmory(party);
		if (armory == null) {
			Logger.Instance.Warning("Armory not found.");
			return;
		}
		MBList<TroopRosterElement>? roster = party.MemberRoster.GetTroopRoster();
		foreach (TroopRosterElement element in roster) {
			this.OnTroopRecruited(armory, element.Character, element.Number);
		}
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
				Armory? armory = this._data.GetArmory(MobileParty.MainParty);
				if (armory == null) {
					Logger.Instance.Warning("Armory not found.");
					return;
				}
				InventoryManager.OpenScreenAsReceiveItems(
					armory.ToItemRoster(),
					new TextObject("军械库"),
					() => {
						ItemRosterElement[]? res =
							InventoryManager.
								InventoryLogic.
								GetElementsInRoster(
									InventoryLogic.InventorySide.OtherInventory
								)?.
								ToArray();
						if (res == null) {
							Logger.Instance.Warning("ItemRosterElement is null.");
							return;
						}

						foreach (ItemRosterElement ele in res) {
							Logger.Instance.Information($"{ele.EquipmentElement.Item.Name},{ele.Amount}");
						}

						armory.FillFromRoster(res);
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