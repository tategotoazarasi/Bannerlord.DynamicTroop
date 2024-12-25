using System.Collections.Generic;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace DTES2;

public struct SaveableArmoryEntry {
	[SaveableField(1)] public EquipmentElement Element;

	[SaveableField(2)] public int Amount;

	public SaveableArmoryEntry(EquipmentElement element, int num) {
		this.Element = element;
		this.Amount  = num;
	}
}

public class DTESSaveableTypeDefiner : SaveableTypeDefiner {
	public DTESSaveableTypeDefiner() : base(734649) { }

	protected override void DefineClassTypes() => this.AddStructDefinition(typeof(SaveableArmoryEntry), 1);

	protected override void DefineContainerDefinitions() {
		this.ConstructContainerDefinition(typeof(List<SaveableArmoryEntry>));
		this.ConstructContainerDefinition(typeof(Dictionary<MobileParty, List<SaveableArmoryEntry>>));
	}
}