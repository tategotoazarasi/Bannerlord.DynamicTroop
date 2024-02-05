using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.DynamicTroop.Patches;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.DynamicTroop;

[Obsolete]
public class Armory {
	private readonly ConcurrentDictionary<EquipmentElement, int> _dict;

	private MobileParty _party;

	public Armory(MobileParty party) {
		_dict = new ConcurrentDictionary<EquipmentElement, int>();
		Party = party;
	}

	public Armory(MobileParty party, Dictionary<(uint, uint), int> loadedData) {
		_dict = new ConcurrentDictionary<EquipmentElement, int>();
		try {
			foreach (var kvp in loadedData) {
				var itemId      = kvp.Key.Item1;
				var modifierId  = kvp.Key.Item2;
				var count       = kvp.Value;
				var itemObj     = MBObjectManager.Instance.GetObject(new MBGUID(itemId));
				var modifierObj = MBObjectManager.Instance.GetObject(new MBGUID(modifierId));
				if (itemObj == null || count <= 0) continue;

				var           item                = (ItemObject)itemObj;
				ItemModifier? modifier            = null;
				if (modifierObj != null) modifier = (ItemModifier)modifierObj;

				EquipmentElement equipmentElement = new(item, modifier);
				AddEquipmentElement(equipmentElement, count);
			}
		}
		catch (Exception e) { Global.Error(e.Message); }

		_party = party;
	}

	public Armory(MobileParty party, ItemRoster itemRoster) {
		_dict      = new ConcurrentDictionary<EquipmentElement, int>();
		ItemRoster = itemRoster;
		_party     = party;
	}

	public MobileParty Party {
		get => _party;
		private set {
			try {
				_party = value;
				var troopRoster = _party.MemberRoster?.GetTroopRoster();
				if (troopRoster == null) return;

				foreach (var troopRosterElement in troopRoster) {
					if (troopRosterElement.Number <= 0) continue;

					var character = troopRosterElement.Character;
					if (character == null) continue;

					var equipmentList = RecruitmentPatch.GetRecruitEquipments(character);
					foreach (var equipment in equipmentList) {
						if (equipment is not { IsEmpty: false, Item: not null }) continue;

						AddEquipmentElement(equipment, troopRosterElement.Number);
					}
				}
			}
			catch (Exception e) { Global.Error(e.Message); }
		}
	}

	public ItemRoster ItemRoster {
		get {
			ItemRoster roster = new();
			try {
				foreach (var kvp in _dict)
					if (kvp.Key.Item != null && kvp.Value > 0)
						_ = roster.AddToCounts(kvp.Key, kvp.Value);
			}
			catch (Exception e) { Global.Error(e.Message); }

			return roster;
		}
		set {
			try {
				_dict.Clear();
				foreach (var item in value)
					if (item.EquipmentElement is { IsEmpty: false, Item: not null })
						AddEquipmentElement(item.EquipmentElement, item.Amount);
			}
			catch (Exception e) { Global.Error(e.Message); }
		}
	}

	public Dictionary<(uint, uint), int> GenerateDataForSave() {
		Dictionary<(uint, uint), int> data = new();
		try {
			foreach (var kvp in _dict) {
				var item     = kvp.Key.Item;
				var modifier = kvp.Key.ItemModifier;
				var count    = kvp.Value;
				if (item == null || count <= 0) continue;

				data.Add((item.Id.InternalValue, modifier.Id.InternalValue), count);
			}
		}
		catch (Exception e) { Global.Error(e.Message); }

		return data;
	}

	public void AddItem(ItemObject item, int count = 1) {
		try { AddEquipmentElement(new EquipmentElement(item), count); }
		catch (Exception e) { Global.Error(e.Message); }
	}

	public void AddEquipmentElement(EquipmentElement equipmentElement, int count = 1) {
		try {
			if (equipmentElement is { IsEmpty: false, Item: not null })
				_ = _dict.AddOrUpdate(equipmentElement, count, (_, v) => v + count);
		}
		catch (Exception e) { Global.Error(e.Message); }
	}

	public void RemoveItem(ItemObject item, int cnt = 1) {
		try {
			ConcurrentDictionary<EquipmentElement, int> toRemove = new();
			foreach (var kvp in _dict)
				if (kvp.Key.Item == item) {
					var cntToRemove = Math.Min(cnt, kvp.Value);
					_   =  toRemove.AddOrUpdate(kvp.Key, cntToRemove, (_, v) => v + cntToRemove);
					cnt -= cntToRemove;
					if (cnt <= 0) break;
				}

			foreach (var kvp in toRemove) RemoveEquipmentElement(kvp.Key, kvp.Value);
		}
		catch (Exception e) { Global.Error(e.Message); }
	}

	public void RemoveEquipmentElement(EquipmentElement equipmentElement, int cnt = 1) {
		try {
			if (equipmentElement is { IsEmpty: false, Item: not null })
				if (_dict.ContainsKey(equipmentElement)) {
					if (_dict[equipmentElement] < cnt) Global.Warn("greater than");

					_dict[equipmentElement] -= cnt;
					if (_dict[equipmentElement] <= 0) _ = _dict.TryRemove(equipmentElement, out _);
				}
		}
		catch (Exception e) { Global.Error(e.Message); }
	}

	public bool HaveEquipmentElement(EquipmentElement equipmentElement) {
		try { return _dict.ContainsKey(equipmentElement) && _dict[equipmentElement] > 0; }
		catch (Exception e) {
			Global.Error(e.Message);
			return false;
		}
	}

	public bool HaveItem(ItemObject item) {
		try { return _dict.Any(kvp => kvp.Key.Item == item && kvp.Value > 0); }
		catch (Exception e) {
			Global.Error(e.Message);
			return false;
		}
	}
}