﻿using System;
using Bannerlord.DynamicTroop.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using static TaleWorlds.Core.ItemObject;

namespace Bannerlord.DynamicTroop;

public class Assignment : IComparable {
	public static readonly EquipmentIndex[] WeaponSlots = {
															  EquipmentIndex.Weapon0,
															  EquipmentIndex.Weapon1,
															  EquipmentIndex.Weapon2,
															  EquipmentIndex.Weapon3
														  };

	private static int _counter;

	public readonly Equipment Equipment;

	public Assignment(CharacterObject character) {
		Index              = ++_counter;
		Character          = character;
		Equipment          = CreateEmptyEquipment();
		ReferenceEquipment = character.RandomBattleEquipment.Clone();
	}

	public int Index { get; }

	public bool IsAssigned { get; set; }

	public CharacterObject Character { get; }

	public Equipment ReferenceEquipment { get; }

	public bool IsShielded =>
		WeaponSlots.AnyQ(slot => Equipment.GetEquipmentFromSlot(slot) is {
																			 IsEmpty      : false,
																			 Item.ItemType: ItemTypeEnum.Shield
																		 });

	public bool CanBeShielded =>
		WeaponSlots.AnyQ(slot => Equipment.GetEquipmentFromSlot(slot) is {
																			 IsEmpty: false,
																			 Item: {
																				 ItemType: ItemTypeEnum
																					 .OneHandedWeapon
																			 } item
																		 } &&
								 !item.CantUseWithShields());

	public bool IsArcher =>
		WeaponSlots.AnyQ(slot => Equipment.GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: { } item } &&
								 item.IsBow());

	public bool IsCrossBowMan =>
		WeaponSlots.AnyQ(slot => Equipment.GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: { } item } &&
								 item.IsCrossBow());

	public bool HaveThrown =>
		WeaponSlots.AnyQ(slot => Equipment.GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: { } item } &&
								 item.IsThrowing());

	public bool HaveTwoHandedWeaponOrPolearms =>
		WeaponSlots.AnyQ(slot => Equipment.GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: { } item } &&
								 (item.IsTwoHanded() || item.IsPolearm()));

	public EquipmentIndex? EmptyWeaponSlot {
		get {
			foreach (var slot in WeaponSlots)
				if (Equipment.GetEquipmentFromSlot(slot).IsEmpty || Equipment.GetEquipmentFromSlot(slot).Item == null)
					return slot;

			return null;
		}
	}

	public bool IsMounted {
		get {
			var horse = ReferenceEquipment.GetEquipmentFromSlot(EquipmentIndex.Horse);
			return horse is { IsEmpty: false, Item: not null };
		}
	}

	public bool IsUnarmed =>
		(Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon0).IsEmpty ||
		 Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon0).Item == null) &&
		(Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1).IsEmpty ||
		 Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon1).Item == null) &&
		(Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2).IsEmpty ||
		 Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon2).Item == null) &&
		(Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3).IsEmpty ||
		 Equipment.GetEquipmentFromSlot(EquipmentIndex.Weapon3).Item == null);

	public int CompareTo(object? obj) {
		if (obj == null) return 1;

		if (obj is not Assignment other) throw new ArgumentException("Object is not an Assignment");

		var tierComparison = Character.Tier.CompareTo(other.Character.Tier);
		if (tierComparison != 0) return tierComparison;

		// TroopType 比较，小的放在前面
		var thisTroopType       = Character.GetTroopType();
		var otherTroopType      = other.Character.GetTroopType();
		var troopTypeComparison = thisTroopType.CompareTo(otherTroopType);
		if (troopTypeComparison != 0) return troopTypeComparison;

		// 按 IsRanged 比较，IsRanged 排在 !IsRanged 前面
		if (!Character.IsRanged && other.Character.IsRanged) return 1;

		if (Character.IsRanged && !other.Character.IsRanged) return -1;

		// 按 IsMounted 比较，!IsMounted 排在 IsMounted 前面
		if (Character.IsMounted && !other.Character.IsMounted) return 1;

		if (!Character.IsMounted && other.Character.IsMounted) return -1;

		var skillValueComparison = Character.SkillValue().CompareTo(other.Character.SkillValue());
		if (skillValueComparison != 0) return skillValueComparison;

		var equipmentValueComparison = Character.EquipmentValue().CompareTo(other.Character.EquipmentValue());
		if (equipmentValueComparison != 0) return equipmentValueComparison;

		var levelComparison = Character.Level.CompareTo(other.Character.Level);
		if (levelComparison != 0) return levelComparison;

		return 0; // 如果所有条件都相等，则认为两者相等
	}

	private static Equipment CreateEmptyEquipment() {
		Equipment emptyEquipment = new();
		foreach (var slot in Global.EquipmentSlots)
			emptyEquipment.AddEquipmentToSlotWithoutAgent(slot, new EquipmentElement());

		return emptyEquipment;
	}

	public void FillEmptySlots() {
		foreach (var slot in Global.ArmourSlots)
			if (Equipment.GetEquipmentFromSlot(slot) is not { IsEmpty: false, Item: not null }) {
				if (slot is EquipmentIndex.Horse or EquipmentIndex.HorseHarness &&
					ReferenceEquipment.GetEquipmentFromSlot(slot) is not { IsEmpty: false, Item: not null })
					continue;

				var itemType = Helper.EquipmentIndexToItemEnumType(slot);
				if (!itemType.HasValue) continue;

				var item = Cache.GetItemsByTierAndCulture(itemType.Value, Character.Tier, Character.Culture)
								.GetRandomElement();
				if (item == null) continue;

				Equipment.AddEquipmentToSlotWithoutAgent(slot, new EquipmentElement(item));
			}

		foreach (var slot in Global.EquipmentSlots)
			if (Equipment.GetEquipmentFromSlot(slot) is not { IsEmpty: false, Item: not null })
				Equipment.AddEquipmentToSlotWithoutAgent(slot, ReferenceEquipment.GetEquipmentFromSlot(slot));
	}

	public void AddEquipment(EquipmentIndex index, EquipmentElement equipment) {
		if (equipment.IsEmpty || equipment.Item == null) return;

		Equipment.AddEquipmentToSlotWithoutAgent(index, equipment);
	}

	public void AddHorseAndHarness(HorseAndHarness hah) {
		if (hah.Horse.IsEmpty || hah.Horse.Item == null) return;

		Equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Horse, hah.Horse);
		if (hah.Harness is not { IsEmpty: false, Item: not null }) return;

		Equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.HorseHarness, hah.Harness.Value);
	}
}