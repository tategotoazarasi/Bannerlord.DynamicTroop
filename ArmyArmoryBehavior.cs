#region

	using System;
	using System.Collections.Generic;
	using Bannerlord.ButterLib.SaveSystem.Extensions;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.CampaignSystem.Inventory;
	using TaleWorlds.Core;
	using TaleWorlds.Localization;
	using TaleWorlds.ObjectSystem;
	using TaleWorlds.SaveSystem;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class ArmyArmoryBehavior : CampaignBehaviorBase {
		private Data data = new();

		public override void SyncData(IDataStore dataStore) {
			//InformationManager.DisplayMessage(new InformationMessage("Sync Data called", Colors.Green));
			if (dataStore.IsSaving) {
				data.Armory.Clear();
				Save();
				_ = dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref data);
				data.Armory.Clear();
			}
			else if (dataStore.IsLoading) {
				data.Armory.Clear();
				ArmyArmory.Armory.Clear();
				_ = dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref data);
				Load();
				data.Armory.Clear();
			}
		}

		public override void RegisterEvents() {
			CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		}

		private void Save() {
			//InformationManager.DisplayMessage(new InformationMessage("Saving Started", Colors.Green));
			var i = ArmyArmory.Armory.GetEnumerator();
			while (i.MoveNext())
				if (!i.Current.IsEmpty) {
					if (!data.Armory.ContainsKey(i.Current.EquipmentElement.Item.StringId))
						data.Armory.Add(i.Current.EquipmentElement.Item.StringId, i.Current.Amount);
					else
						data.Armory[i.Current.EquipmentElement.Item.StringId] += i.Current.Amount;
				}

			i.Dispose();
			/*InformationManager
	.DisplayMessage(new
	InformationMessage($"Saving {i.Current.EquipmentElement.Item.StringId} x{i.Current.Amount}",
			   Colors.Green));*/
		}

		private void Load() {
			//InformationManager.DisplayMessage(new InformationMessage("Loading Started", Colors.Green));
			foreach (var item in data.Armory)
				_ = ArmyArmory.Armory.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>(item.Key), item.Value);
			/*InformationManager.DisplayMessage(new InformationMessage($"Loading {item.Key} x{item.Value}",
	Colors.Green));*/
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