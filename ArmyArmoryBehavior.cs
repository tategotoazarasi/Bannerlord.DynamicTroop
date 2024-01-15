using System;
using System.Collections.Generic;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Bannerlord.DynamicTroop;

public class ArmyArmoryBehavior : CampaignBehaviorBase {
	private Data _data = new();

	public override void SyncData(IDataStore dataStore) {
		//InformationManager.DisplayMessage(new InformationMessage("Sync Data called", Colors.Green));
		if (dataStore.IsSaving) {
			_data.Armory.Clear();
			Save();

			var tempData = _data;
			_ = dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref tempData);

			if (tempData != null) {
				_data = tempData;
				_data.Armory.Clear();
			}
			else { Global.Error("null data on save"); }
		}
		else if (dataStore.IsLoading) {
			_data.Armory.Clear();
			ArmyArmory.Armory.Clear();

			var tempData = _data;
			_ = dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref tempData);

			if (tempData != null) {
				Load(tempData);
				_data = tempData;
				_data.Armory.Clear();
			}
			else { Global.Error("null data on load"); }
		}
	}

	public override void RegisterEvents() {
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
	}

	private void Save() {
		var i = ArmyArmory.Armory.GetEnumerator();
		while (i.MoveNext())
			if (!i.Current.IsEmpty) {
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
			if (equipment != null)
				_ = ArmyArmory.Armory.AddToCounts(equipment, item.Value);
			else
				Global.Warn($"cannot get object {item.Key}");
		}
		Global.Debug($"loaded {tempData.Armory.Count} entries for player");
	}

	private void OnSessionLaunched(CampaignGameStarter starter) { AddTownMenuOptions(starter); }

	private void AddTownMenuOptions(CampaignGameStarter starter) {
		starter.AddGameMenuOption("town",             // Town menu
								  "army_armory_view", // Unique identifier for this menu item
								  new TextObject("{=armory_view_option}View Army Armory")
									  .ToString(), // Localized text for the menu item
								  args => true,    // Conditions for showing this option
								  args => {
									  // Action to execute when this option is selected
									  InventoryManager.OpenScreenAsStash(ArmyArmory.Armory);
								  },
								  false, // Is this option shown at the bottom of the menu?
								  4      // Order in the menu
								 );
	}

	[Serializable]
	private class Data {
		[SaveableField(1)] public Dictionary<string, int> Armory = new();
	}
}