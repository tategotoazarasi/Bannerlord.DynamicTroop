#region

using System;
using System.Collections.Generic;

using Bannerlord.ButterLib.SaveSystem.Extensions;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

#endregion

namespace Bannerlord.DynamicTroop;

public class ArmyArmoryBehavior : CampaignBehaviorBase {
	private Data data = new();

	public override void SyncData(IDataStore dataStore) {
		InformationManager.DisplayMessage(new InformationMessage("Sync Data called", Colors.Green));
		if (dataStore.IsSaving) {
			data.Armory.Clear();
			Save();
			dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref data);
			data.Armory.Clear();
		} else if (dataStore.IsLoading) {
			data.Armory.Clear();
			ArmyArmory.Armory.Clear();
			dataStore.SyncDataAsJson("DynamicTroopArmyArmory", ref data);
			Load();
			data.Armory.Clear();
		}
	}

	public override void RegisterEvents() { }

	private void Save() {
		InformationManager.DisplayMessage(new InformationMessage("Saving Started", Colors.Green));
		IEnumerator<ItemRosterElement> i = ArmyArmory.Armory.GetEnumerator();
		while (i.MoveNext()) {
			if (!i.Current.IsEmpty) {
				data.Armory.Add(i.Current.EquipmentElement.Item.StringId, i.Current.Amount);
				InformationManager
					.DisplayMessage(new
										InformationMessage($"Saving {i.Current.EquipmentElement.Item.StringId} x{i.Current.Amount}",
														   Colors.Green));
			}
		}
	}

	private void Load() {
		InformationManager.DisplayMessage(new InformationMessage("Loading Started", Colors.Green));
		foreach (KeyValuePair<string, int> item in data.Armory) {
			ArmyArmory.Armory.AddToCounts(MBObjectManager.Instance.GetObject<ItemObject>(item.Key), item.Value);
			InformationManager.DisplayMessage(new InformationMessage($"Loading {item.Key} x{item.Value}",
																	 Colors.Green));
		}
	}

	[Serializable]
	private class Data {
		[SaveableField(1)] public Dictionary<string, int> Armory = new();
	}
}