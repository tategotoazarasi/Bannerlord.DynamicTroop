using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace DTES2.Comparer;

public class ArmorComparer(CharacterObject character) : IComparer<ItemObject> {
	private readonly CharacterObject _character = character;

	public int Compare(ItemObject x, ItemObject y) {
		float xEffectiveness = x.Effectiveness;
		float yEffectiveness = y.Effectiveness;
		if (this._character.Culture == x.Culture) {
			xEffectiveness *= 1.5f;
		}

		if (this._character.Culture == y.Culture) {
			yEffectiveness *= 1.5f;
		}

		return (int)(xEffectiveness - yEffectiveness);
	}
}