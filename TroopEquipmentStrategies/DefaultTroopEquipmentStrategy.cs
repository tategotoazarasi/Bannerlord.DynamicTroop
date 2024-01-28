using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.DynamicTroop.Comparers;
using Bannerlord.DynamicTroop.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.TroopEquipmentStrategies;

public class DefaultTroopEquipmentStrategy : TroopEquipmentStrategy {
	private readonly Dictionary<ItemObject.ItemTypeEnum, Queue<EquipmentElement>> _armorQueues = new();

	private readonly Queue<HorseAndHarness> _horseAndHarnesses;

	private readonly Dictionary<(CharacterObject, EquipmentElement), (EquipmentElement[] List, int Index)>
		_weaponListCache = new();

	public DefaultTroopEquipmentStrategy(Dictionary<EquipmentElement, int> armoryDict,
										 Queue<HorseAndHarness>            horseAndHarnesses) : base(armoryDict) {
		foreach (var slot in Global.ArmourSlots) {
			var typ = Helper.EquipmentIndexToItemEnumType(slot);
			if (typ.HasValue) _armorQueues.Add(typ.Value, new Queue<EquipmentElement>());
		}

		_horseAndHarnesses = horseAndHarnesses;
		InitializeArmorQueues();
	}

	public override int Priority => 1;

	private void InitializeArmorQueues() {
		foreach (var slot in Global.ArmourSlots) {
			var typ = Helper.EquipmentIndexToItemEnumType(slot);
			if (!typ.HasValue) continue;

			var sortedArmors = ArmoryDict
							   .Where(kvp => kvp.Key is { IsEmpty: false, Item.HasArmorComponent: true } &&
											 kvp.Value             > 0                                   &&
											 kvp.Key.Item.ItemType == typ.Value)
							   .Select(kvp => kvp.Key)
							   .OrderBy(armor => armor, new ArmorElementComparer())
							   .ToArray();

			_armorQueues[typ.Value] = new Queue<EquipmentElement>(sortedArmors);
		}
	}

	public override bool Matches(CharacterObject soldier) { return true; }

	protected override EquipmentElement? AssignArmor(CharacterObject soldier, ItemObject.ItemTypeEnum type) {
		if (!_armorQueues.TryGetValue(type, out var queue) || queue.IsEmpty()) return null;

		while (!queue.IsEmpty()) {
			var armorElement = queue.Peek();
			if (ArmoryDict[armorElement] < 1)
				_ = queue.Dequeue();
			else
				return armorElement;
		}

		return null;
	}

	protected override HorseAndHarness? AssignHorseAndHarness(CharacterObject soldier) {
		return soldier.IsMounted ? _horseAndHarnesses.IsEmpty() ? null : _horseAndHarnesses.Dequeue() : null;
	}

	protected override EquipmentElement? AssignWeapon(CharacterObject soldier, EquipmentElement refWeapon) {
		if (refWeapon is not { IsEmpty: false, Item.HasWeaponComponent: true }) return null;

		var cacheKey = (soldier, refWeapon);
		(var weapons, var index) = GetWeaponList(cacheKey);
		return index < weapons.Length ? weapons[index] : null;
	}

	private (EquipmentElement[] List, int Index) GetWeaponList(
		(CharacterObject soldier, EquipmentElement refWeapon) cacheKey) {
		if (_weaponListCache.TryGetValue(cacheKey, out var cacheEntry)) {
			UpdateCacheEntry(cacheKey, ref cacheEntry);
			return cacheEntry;
		}

		// 如果缓存中没有，计算新的列表
		var weapons = CalculateWeaponList(cacheKey.soldier, cacheKey.refWeapon);
		var index   = FindFirstNonZeroIndex(weapons);
		_weaponListCache[cacheKey] = (weapons, index);
		return (weapons, index);
	}

	private void UpdateCacheEntry((CharacterObject, EquipmentElement)      cacheKey,
								  ref (EquipmentElement[] List, int Index) cacheEntry) {
		(var weapons, var index) = cacheEntry;
		index                    = FindFirstNonZeroIndex(weapons, index);
		cacheEntry               = (weapons, index);
	}

	private int FindFirstNonZeroIndex(EquipmentElement[] weapons, int startIndex = 0) {
		for (var i = startIndex; i < weapons.Length; i++)
			if (ArmoryDict[weapons[i]] > 0)
				return i;

		return weapons.Length; // 如果没有非零项，返回长度
	}

	private EquipmentElement[] CalculateWeaponList(CharacterObject soldier, EquipmentElement refWeapon) {
		var weapons = ArmoryDict
					  .Where(kvp => kvp is { Key: { IsEmpty: false, Item.HasWeaponComponent: true }, Value: > 0 } &&
									Common.Instance.GetWeaponFilterType(kvp.Key.Item) ==
									Common.Instance.GetWeaponFilterType(refWeapon.Item) &&
									(!soldier.IsMounted || kvp.Key.Item.IsSuitableForMount()))
					  .Select(kvp => kvp.Key)
					  .ToArray();
		Array.Sort(weapons,
				   (x, y) => Common.Instance.CalcConsiderValue(soldier, refWeapon, y)
								   .CompareTo(Common.Instance.CalcConsiderValue(soldier, refWeapon, x)));
		return weapons;
	}
}

public class CacheKeyComparer : IEqualityComparer<(CharacterObject, EquipmentElement)> {
	public bool Equals((CharacterObject, EquipmentElement) x, (CharacterObject, EquipmentElement) y) {
		return x.Item1 == y.Item1 && x.Item2.Equals(y.Item2);
	}

	public int GetHashCode((CharacterObject, EquipmentElement) obj) {
		unchecked {
			var hash = 17;
			hash = hash * 23 + obj.Item1.GetHashCode();
			hash = hash * 23 + obj.Item2.GetHashCode();
			return hash;
		}
	}
}