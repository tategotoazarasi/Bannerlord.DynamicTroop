using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Extensions {
	public static class SettlementExtension {
		public static List<ItemObject> GetRandomEquipment(this Settlement? settlement) {
			return new();
		}
	}
}
