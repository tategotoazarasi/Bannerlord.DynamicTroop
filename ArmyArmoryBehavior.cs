#region

using System;
using System.Collections.Generic;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

#endregion

namespace DynamicTroopEquipmentReupload;

public class ArmyArmoryBehavior : CampaignBehaviorBase {
	private Data _data = new();

	public override void SyncData(IDataStore dataStore) {
		//InformationManager.DisplayMessage(new InformationMessage("Sync Data called", Colors.Green));
		if (dataStore.IsSaving) {
			_data.Armory.Clear();
			Save();
			var tempData = _data;
			if (dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref tempData) && tempData != null) {
				_data = tempData;
				_data.Armory.Clear();
			}
			else { Global.Error("null data on save"); }
		}
		else if (dataStore.IsLoading) {
			_data.Armory.Clear();
			ArmyArmory.Armory.Clear();
			var tempData = _data;
			if (dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref tempData) && tempData != null) {
				Load(tempData);
				_data = tempData;
				_data.Armory.Clear();
			}
			else { Global.Error("null data on load"); }
		}
	}

	public override void RegisterEvents() {
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
	}


	private void OnNewGameCreated(CampaignGameStarter starter) {
		Global.Debug("OnNewGameCreated() called");
		ArmyArmory.Armory.Clear();
	}

	private void OnDailyTick() {
		var settings = ModSettings.Instance;
		if (settings == null)
			return;

		// Only if Commander's Greed is OFF.
		if (settings.CommandersGreed)
			return;

		var currentDayNumber = (int)CampaignTime.Now.ToDays;

		// Run every 3 days.
		if (_data.LastScrapDayNumber >= 0 && currentDayNumber - _data.LastScrapDayNumber < 3)
			return;

		_data.LastScrapDayNumber = currentDayNumber;

		var cap                    = settings.ScrapCapPerCategory;
		var targetCountPerCategory = cap - 1;

		if (targetCountPerCategory <= 0)
			return;

		ScrapArmyArmoryByCategory(targetCountPerCategory);
	}


	private void Save() {
		var i = ArmyArmory.Armory.GetEnumerator();
		while (i.MoveNext())
			if (i.Current is { IsEmpty: false, EquipmentElement: { IsEmpty: false, Item: not null }, Amount: > 0 }) {
				if (!_data.Armory.ContainsKey(i.Current.EquipmentElement.Item.StringId))
					_data.Armory.Add(i.Current.EquipmentElement.Item.StringId, i.Current.Amount);
				else
					_data.Armory[i.Current.EquipmentElement.Item.StringId] += i.Current.Amount;
			}

		i.Dispose();
	}

	private void Load(Data tempData) {
		foreach (var item in tempData.Armory) {
			var equipment = MBObjectManager.Instance.GetObject<ItemObject>(item.Key) ??
							ItemObject.GetCraftedItemObjectFromHashedCode(item.Key);
			if (equipment != null && item.Value > 0)
				_ = ArmyArmory.Armory.AddToCounts(equipment, item.Value);
			else
				Global.Warn($"cannot get object {item.Key}");
		}

		Global.Debug($"loaded {tempData.Armory.Count} entries for player");
	}

	private static void ScrapArmyArmoryByCategory(int targetCountPerCategory) {
		var itemsByType = new Dictionary<ItemObject.ItemTypeEnum, List<(EquipmentElement Equipment, int Amount)>>();

		var enumerator = ArmyArmory.Armory.GetEnumerator();
		while (enumerator.MoveNext()) {
			var element = enumerator.Current;

			if (element is not { IsEmpty: false, EquipmentElement: { IsEmpty: false, Item: not null }, Amount: > 0 })
				continue;

			var equipmentElement = element.EquipmentElement;
			var itemType         = equipmentElement.Item.ItemType;

			if (!itemsByType.TryGetValue(itemType, out var list)) {
				list = new List<(EquipmentElement Equipment, int Amount)>();
				itemsByType.Add(itemType, list);
			}


			list.Add((equipmentElement, element.Amount));
		}

		enumerator.Dispose();

		foreach (var kvp in itemsByType) {
			var entries = kvp.Value;

			var totalCount = 0;
			for (var i = 0; i < entries.Count; i++)
				totalCount += entries[i].Amount;

			if (totalCount <= targetCountPerCategory)
				continue;

			var removeNeeded = totalCount - targetCountPerCategory;

			// lowest value first
			entries.Sort((a, b) => {
				var valueCompare = a.Equipment.ItemValue.CompareTo(b.Equipment.ItemValue);
				if (valueCompare != 0)
					return valueCompare;

				var itemCompare = string.CompareOrdinal(a.Equipment.Item.StringId, b.Equipment.Item.StringId);
				return itemCompare != 0
						   ? itemCompare
						   : string.CompareOrdinal(a.Equipment.ItemModifier?.StringId, b.Equipment.ItemModifier?.StringId);
			});

			for (var i = 0; i < entries.Count && removeNeeded > 0; i++) {
				(var equipment, var amount) = entries[i];
				var removeCount = Math.Min(amount, removeNeeded);

				ArmyArmory.Armory.AddToCounts(equipment, -removeCount);
				removeNeeded -= removeCount;
			}
		}
	}


	private void OnSessionLaunched(CampaignGameStarter starter) { AddTownMenuOptions(starter); }

	private void AddTownMenuOptions(CampaignGameStarter starter) {
		AddArmyArmorySubmenu(starter);
		starter.AddGameMenuOption("town",
								  "army_armory_manage",
								  LocalizedTexts.ArmoryManageOption.ToString(),
								  args => true,
								  args => {
									  // 打开子菜单
									  GameMenu.SwitchToMenu("army_armory_submenu");
								  },
								  false,
								  4);
	}

	private void AddArmyArmorySubmenu(CampaignGameStarter starter) {
		// 创建子菜单
		starter.AddGameMenu("army_armory_submenu", LocalizedTexts.ArmoryManageOption.ToString(), args => { });

		starter.AddGameMenuOption("army_armory_submenu",
								  "view_armory",
								  LocalizedTexts.ArmorViewOption.ToString(),
								  args => true,
								  args => {
									  // roster so the player can leave items
									  InventoryScreenHelper.OpenScreenAsStash(ArmyArmory.Armory);
								  });


		starter.AddGameMenuOption("army_armory_submenu",
								  "sell_for_throwing",
								  LocalizedTexts.SellForThrowing.ToString(),
								  args => true,
								  args => { ArmyArmory.SellExcessEquipmentForThrowingWeapons(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_clear_invalid_items",
								  "DEBUG: Clear invalid items",
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.DebugClearEmptyItem(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_remove_player_crafted_items",
								  "DEBUG: Remove player crafted items",
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.DebugRemovePlayerCraftedItems(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_rebuild_armory",
								  "DEBUG: Rebuild player armory",
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.RebuildArmory(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_export_armory",
								  "DEBUG: Export player armory",
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.Export(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_import_armory",
								  "DEBUG: Import player armory",
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.Import(); });

		// 返回上一级菜单的选项
		starter.AddGameMenuOption("army_armory_submenu",
								  "return_to_town",
								  LocalizedTexts.ReturnToTown.ToString(),
								  args => true,
								  args => { GameMenu.SwitchToMenu("town"); },
								  true);
	}

	[Serializable]
	private class Data {
		[SaveableField(1)] public Dictionary<string, int> Armory = new();
		[SaveableField(2)] public int LastScrapDayNumber = -1;
	}
}