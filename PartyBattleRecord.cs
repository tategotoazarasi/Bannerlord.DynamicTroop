using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop {
	public class PartyBattleRecord {
		public Dictionary<ItemObject, int> ItemsToRecover = new();

		public Dictionary<ItemObject, int> LootedItems = new();
	}
}
