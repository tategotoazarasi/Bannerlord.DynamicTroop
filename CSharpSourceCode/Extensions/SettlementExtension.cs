#region

using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

#endregion

namespace DynamicTroopEquipmentReupload.Extensions;

public static class SettlementExtension {
	public static List<ItemObject> GetRandomEquipment(this Settlement? settlement) { return new List<ItemObject>(); }
}