using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.DynamicTroop.Extensions;

public static class CharacterObjectExtension {
	public enum TroopType {
		Militia,
		Bandit,
		None,
		Basic,
		Mercenary,
		Elite
	}

	private static readonly Dictionary<CharacterObject, TroopType> TypeDict = new();

	private static readonly Queue<(CharacterObject, TroopType)> TroopQueue = new();

	private static MBReadOnlyList<SkillObject> _cachedSkillValue = new();

	private static readonly ConcurrentDictionary<CharacterObject, int> CachedCharacterEquipmentValue = new();

	private static readonly ConcurrentDictionary<CharacterObject, int> CachedCharacterSkillValue = new();

	public static TroopType GetTroopType(this CharacterObject? character) {
		return character == null                            ? TroopType.None :
			   TypeDict.TryGetValue(character, out var typ) ? typ : TroopType.None;
	}

	private static int CalculateEquipmentValue(this CharacterObject? character) {
		if (character == null) return 0;

		var                    totalValue = 0;
		IEnumerable<Equipment> equipments = character.BattleEquipments;
		if (equipments == null) return 0;

		var cnt = character.BattleEquipments.Count();
		foreach (var equipment in equipments) {
			foreach (var slot in Global.EquipmentSlots) {
				var element = equipment.GetEquipmentFromSlot(slot);
				if (element.IsEmpty || element.Item == null) continue;

				totalValue += equipment.GetEquipmentFromSlot(slot).ItemValue;
			}
		}

		return totalValue / cnt;
	}

	private static int CalculateSkillValue(this CharacterObject? character) {
		return character != null ? _cachedSkillValue.SumQ(character.GetSkillValue) : 0;
	}

	public static int EquipmentValue(this CharacterObject? character) {
		if (character == null) return 0;

		if (CachedCharacterEquipmentValue.TryGetValue(character, out var value)) return value;

		value = CalculateEquipmentValue(character);
		CachedCharacterEquipmentValue.TryAdd(character, value);
		return value;
	}

	public static int SkillValue(this CharacterObject? character) {
		if (character == null) return 0;

		if (CachedCharacterSkillValue.TryGetValue(character, out var value)) return value;

		value = CalculateSkillValue(character);
		CachedCharacterSkillValue.TryAdd(character, value);
		return value;
	}

	public static void Init() {
		var cultureList = MBObjectManager.Instance.GetObjectTypeList<CultureObject>();
		_cachedSkillValue = MBObjectManager.Instance.GetObjectTypeList<SkillObject>();
		foreach (var culture in cultureList) {
			Enqueue(culture?.EliteBasicTroop, TroopType.Elite);

			MBReadOnlyList<CharacterObject>? basicMercenaryTroops = culture?.BasicMercenaryTroops;
			if (basicMercenaryTroops != null)
				foreach (var troop in basicMercenaryTroops)
					Enqueue(troop, TroopType.Mercenary);

			Enqueue(culture?.BasicTroop, TroopType.Basic);

			Enqueue(culture?.BanditChief,  TroopType.Bandit);
			Enqueue(culture?.BanditRaider, TroopType.Bandit);
			Enqueue(culture?.BanditBandit, TroopType.Bandit);
			Enqueue(culture?.BanditBoss,   TroopType.Bandit);

			Enqueue(culture?.MeleeMilitiaTroop,       TroopType.Militia);
			Enqueue(culture?.MeleeEliteMilitiaTroop,  TroopType.Militia);
			Enqueue(culture?.RangedEliteMilitiaTroop, TroopType.Militia);
			Enqueue(culture?.RangedMilitiaTroop,      TroopType.Militia);
			Enqueue(culture?.MilitiaArcher,           TroopType.Militia);
			Enqueue(culture?.MilitiaVeteranArcher,    TroopType.Militia);
			Enqueue(culture?.MilitiaSpearman,         TroopType.Militia);
			Enqueue(culture?.MilitiaVeteranSpearman,  TroopType.Militia);
		}

		while (!TroopQueue.IsEmpty()) {
			(var character, var typ) = TroopQueue.Dequeue();
			if (TypeDict.ContainsKey(character)) continue;

			TypeDict.Add(character, typ);
			foreach (var next in character.UpgradeTargets) Enqueue(next, typ);
		}
	}

	private static void Enqueue(CharacterObject? character, TroopType typ) {
		if (character == null) return;

		TroopQueue.Enqueue((character, typ));
	}
}