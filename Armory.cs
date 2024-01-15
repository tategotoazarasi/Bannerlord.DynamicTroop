using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bannerlord.DynamicTroop.Comparers;
using HarmonyLib;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.DynamicTroop {
	/// <summary>
	/// unimplemented
	/// </summary>
	public class Armory {
		//private ItemRoster                            _itemRoster;
		private ConcurrentDictionary<EquipmentElement, int> _dict;

		public Armory() {
			//_itemRoster = new ItemRoster();
			_dict       = new(new EquipmentElementComparerArmory());
		}

		public Armory(Dictionary<(uint,uint), int> loadedData) {
			/*foreach (var ((itemId,modifierId),count) in loadedData) {
				var itemObj     = MBObjectManager.Instance.GetObject(new MBGUID(itemId));
				var modifierObj = MBObjectManager.Instance.GetObject(new MBGUID(modifierId));
				if (itemObj == null || modifierObj == null || count==0) continue;
				var item     = (ItemObject)itemObj;
				var modifier = (ItemModifier)modifierObj;
				_dict = new(new EquipmentElementComparerArmory());
				var equipmentElement = new EquipmentElement(item, modifier);
				_dict.AddOrUpdate(equipmentElement, count, (_, v) => v + count);
			}*/
		}
	}
}
