#region
using System;
using System.Collections.Concurrent;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
#endregion
namespace DTES2;

[Serializable]
public class Armory {
	private readonly ConcurrentDictionary<EquipmentElement, int> _data = new ConcurrentDictionary<EquipmentElement, int>();
	private MobileParty _party;

	public void Store(EquipmentElement equipmentElement, int amount = 1) {
		_data.TryGetValue(equipmentElement, out var currentAmount);
		if (currentAmount + amount < 0) {
			Logger.Instance.Warning("Armory.Store: Attempted to store negative amount of equipment.");
			return;
		}
		_data.AddOrUpdate(equipmentElement, amount, (equipment, count) => count + amount);
	}

	public void Store(ItemObject item, int amount = 1) {
		if (amount >= 0) {
			var equipment = new EquipmentElement(item);
			Store(equipment, amount);
		}
		// TODO: remove item from armory
	}

	public int GetAmount(EquipmentElement equipment) {
		_data.TryGetValue(equipment, out var amount);
		return 0;
	}

	public int GetAmount(ItemObject item) {
		return _data.AsParallel().SumQ(equipment => equipment.Key.Item == item ? equipment.Value : 0);
	}
}