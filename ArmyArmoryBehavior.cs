#region

using Bannerlord.ButterLib.SaveSystem.Extensions;
using Helpers;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

#endregion

namespace DynamicTroopEquipmentReupload;

public class ArmyArmoryBehavior : CampaignBehaviorBase {
	private readonly Dictionary<string, int> _unresolvedArmoryItemCounts = new();
	private Data _data = new();

	public override void SyncData(IDataStore dataStore) {
		if (dataStore.IsSaving) {
			SaveArmory();
			var tempData = _data;
			if (dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref tempData) && tempData != null)
				_data = tempData;
			else
				Global.Error("null data on save");
		}
		else if (dataStore.IsLoading) {
			ArmyArmory.ResetForCampaign();
			_unresolvedArmoryItemCounts.Clear();

			var tempData = new Data();
			if (dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref tempData) && tempData != null) {
				_data = tempData;
				_data.Armory ??= new Dictionary<string, int>();
				foreach (var entry in _data.Armory) {
					if (!string.IsNullOrEmpty(entry.Key) && entry.Value > 0)
						_unresolvedArmoryItemCounts[entry.Key] = entry.Value;
				}
			}
			else
				Global.Error("null data on load");
		}
	}

	public override void RegisterEvents() {
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
	}


	private void OnNewGameCreated(CampaignGameStarter starter) {
		Global.Debug("OnNewGameCreated() called");
		_unresolvedArmoryItemCounts.Clear();
		ArmyArmory.ResetForCampaign();
	}

	private void OnGameLoaded(CampaignGameStarter starter) {
		RestoreReadyArmoryItems();
		ArmyArmory.SanitizeInPlace();
	}

	private void OnDailyTick() {
		RestoreReadyArmoryItems();
		ArmyArmory.SanitizeInPlace();
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

	private void OnHeroPrisonerTaken(PartyBase capturerParty, Hero prisonerHero) {
		if (prisonerHero != Hero.MainHero)
			return;

		ApplyCapturedArmoryLoss();
	}

	private static void ApplyCapturedArmoryLoss() {
		ArmyArmory.SanitizeInPlace();

		const int LOSS_NUMERATOR = 4;
		const int LOSS_DENOMINATOR = 5;

		var removals = new List<(EquipmentElement Equipment, int RemoveCount)>();

		var removedItemCount = 0;
		long removedValue = 0;

		var enumerator = ArmyArmory.Armory.GetEnumerator();
		while (enumerator.MoveNext())
		{
			var element = enumerator.Current;

			if (element is not { IsEmpty: false, EquipmentElement: { IsEmpty: false, Item: not null }, Amount: > 0 })
				continue;

			var amount = element.Amount;

			// exact 80% with integer math + randomized remainder (vanilla-like)
			var weightedAmount = (long)amount * LOSS_NUMERATOR;
			var baseLoss = (int)(weightedAmount / LOSS_DENOMINATOR);
			var remainder = (int)(weightedAmount % LOSS_DENOMINATOR);

			var extraLoss = remainder > 0 && MBRandom.RandomInt(LOSS_DENOMINATOR) < remainder ? 1 : 0;
			var removeCount = Math.Min(amount, baseLoss + extraLoss);

			if (removeCount <= 0)
				continue;

			removals.Add((element.EquipmentElement, removeCount));
		}
		enumerator.Dispose();

		for (var i = 0; i < removals.Count; i++)
		{
			(var equipment, var removeCount) = removals[i];

			ArmyArmory.Armory.AddToCounts(equipment, -removeCount);

			removedItemCount += removeCount;
			removedValue += (long)equipment.ItemValue * removeCount;
		}

		if (removedItemCount > 0)
		{
			MessageDisplayService.EnqueueMessage(new InformationMessage(
				$"You were captured and lost {removedItemCount} items from the Army Armory (value: {removedValue}).",
				Colors.Red));
		}
	}

	private void SaveArmory() {
		ArmyArmory.SanitizeInPlace();
		_data.Armory ??= new Dictionary<string, int>();
		_data.Armory.Clear();

		foreach (var unresolvedItem in _unresolvedArmoryItemCounts)
			_data.Armory[unresolvedItem.Key] = unresolvedItem.Value;

		foreach (var rosterElement in ArmyArmory.Armory) {
			if (rosterElement.Amount <= 0 ||
				!ArmyArmory.TryResolveArmoryItem(rosterElement.EquipmentElement.Item, out var item))
				continue;

			_data.Armory[item.StringId] = _data.Armory.TryGetValue(item.StringId, out var currentCount)
				? currentCount + rosterElement.Amount
				: rosterElement.Amount;
		}
	}

	internal static void OpenArmoryScreen() {
		Campaign.Current?.GetCampaignBehavior<ArmyArmoryBehavior>()?.RestoreReadyArmoryItems();
		ArmyArmory.SanitizeInPlace();
		InventoryScreenHelper.OpenScreenAsStash(ArmyArmory.Armory);
	}
	private void RestoreReadyArmoryItems() {
		if (_unresolvedArmoryItemCounts.Count == 0)
			return;

		foreach (var pendingItem in new Dictionary<string, int>(_unresolvedArmoryItemCounts)) {
			var item = ArmyArmory.ResolveArmoryItem(pendingItem.Key);
			if (item == null)
				continue;

			ArmyArmory.Armory.AddToCounts(item, pendingItem.Value);
			_unresolvedArmoryItemCounts.Remove(pendingItem.Key);
		}
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


	private void OnSessionLaunched(CampaignGameStarter starter) {
		RestoreReadyArmoryItems();
		ArmyArmory.SanitizeInPlace();
		AddTownMenuOptions(starter);
	}

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
								  args => { OpenArmoryScreen(); });


		starter.AddGameMenuOption("army_armory_submenu",
								  "sell_for_throwing",
								  LocalizedTexts.SellForThrowing.ToString(),
								  args => true,
								  args => { ArmyArmory.SellExcessEquipmentForThrowingWeapons(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_clear_invalid_items",
								  LocalizedTexts.DebugClearInvalidItems.ToString(),
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.DebugClearEmptyItem(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_remove_player_crafted_items",
								  LocalizedTexts.DebugRemovePlayerCraftedItems.ToString(),
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.DebugRemovePlayerCraftedItems(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_rebuild_armory",
								  LocalizedTexts.DebugRebuildArmory.ToString(),
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.RebuildArmory(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_export_armory",
								  LocalizedTexts.DebugExportArmory.ToString(),
								  args => ModSettings.Instance?.DebugMode ?? false,
								  args => { ArmyArmory.Export(); });

		starter.AddGameMenuOption("army_armory_submenu",
								  "debug_import_armory",
								  LocalizedTexts.DebugImportArmory.ToString(),
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