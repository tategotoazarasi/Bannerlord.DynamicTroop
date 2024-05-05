using System;
using System.Collections.Generic;
using System.Threading;
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
	
	private static   int                  _counter;
	private readonly ReaderWriterLockSlim _eqLock = new();
	private readonly ReaderWriterLockSlim _lock   = new();
	
	public readonly Equipment Equipment;
	
	private bool _isAssigned;
	
	public Assignment(CharacterObject character) {
		_lock.EnterWriteLock();
		try { Index = Interlocked.Increment(ref _counter); }
		finally { _lock.ExitWriteLock(); }
		
		Character          = character;
		Equipment          = CreateEmptyEquipment();
		ReferenceEquipment = character.RandomBattleEquipment.Clone();
	}
	
	public int Index { get; }
	
	public bool IsAssigned
	{
		get
		{
			//return _isAssigned;
			_lock.EnterReadLock();
			try { return _isAssigned; }
			finally { _lock.ExitReadLock(); }
		}
		set
		{
			//_isAssigned = value;
			_lock.EnterWriteLock();
			try { _isAssigned = value; }
			finally { _lock.ExitWriteLock(); }
		}
	}
	
	public CharacterObject Character { get; }
	
	public Equipment ReferenceEquipment { get; }
	
	public bool IsShielded =>
		WeaponSlots.AnyQ(slot => GetEquipmentFromSlot(slot) is {
																   IsEmpty      : false,
																   Item.ItemType: ItemTypeEnum.Shield
															   });
	
	public bool CanBeShielded =>
		WeaponSlots.AnyQ(slot => GetEquipmentFromSlot(slot) is {
																   IsEmpty: false,
																   Item: {
																			 ItemType: ItemTypeEnum.OneHandedWeapon
																		 } item
															   } &&
								 !item.CantUseWithShields());
	
	public bool IsArcher =>
		WeaponSlots.AnyQ(slot => GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: { } item } && item.IsBow());
	
	public bool IsCrossBowMan =>
		WeaponSlots.AnyQ(slot => GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: { } item } && item.IsCrossBow());
	
	public bool HaveThrown =>
		WeaponSlots.AnyQ(slot => GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: { } item } && item.IsThrowing());
	
	public bool HaveTwoHandedWeaponOrPolearms =>
		WeaponSlots.AnyQ(slot => GetEquipmentFromSlot(slot) is { IsEmpty: false, Item: { } item } && (item.IsTwoHanded() || item.IsPolearm()));
	
	public EquipmentIndex? EmptyWeaponSlot
	{
		get
		{
			foreach (var slot in WeaponSlots)
				if (GetEquipmentFromSlot(slot).IsEmpty || GetEquipmentFromSlot(slot).Item == null)
					return slot;
			
			return null;
		}
	}
	
	public bool IsMounted
	{
		get
		{
			var horse = ReferenceEquipment.GetEquipmentFromSlot(EquipmentIndex.Horse);
			return horse is { IsEmpty: false, Item: not null };
		}
	}
	
	public bool IsUnarmed =>
		(GetEquipmentFromSlot(EquipmentIndex.Weapon0).IsEmpty || GetEquipmentFromSlot(EquipmentIndex.Weapon0).Item == null) &&
		(GetEquipmentFromSlot(EquipmentIndex.Weapon1).IsEmpty || GetEquipmentFromSlot(EquipmentIndex.Weapon1).Item == null) &&
		(GetEquipmentFromSlot(EquipmentIndex.Weapon2).IsEmpty || GetEquipmentFromSlot(EquipmentIndex.Weapon2).Item == null) &&
		(GetEquipmentFromSlot(EquipmentIndex.Weapon3).IsEmpty || GetEquipmentFromSlot(EquipmentIndex.Weapon3).Item == null);
	
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
	
	public EquipmentElement GetEquipmentFromSlot(EquipmentIndex slot) {
		_eqLock.EnterReadLock();
		try { return Equipment.GetEquipmentFromSlot(slot); }
		finally { _eqLock.ExitReadLock(); }
	}
	
	public void SetEquipment(EquipmentIndex slot, EquipmentElement equipment) {
		_eqLock.EnterWriteLock();
		try { Equipment.AddEquipmentToSlotWithoutAgent(slot, equipment); }
		finally { _eqLock.ExitWriteLock(); }
	}
	
	private static Equipment CreateEmptyEquipment() {
		Equipment emptyEquipment = new();
		foreach (var slot in Global.EquipmentSlots)
			emptyEquipment.AddEquipmentToSlotWithoutAgent(slot, new EquipmentElement());
		
		return emptyEquipment;
	}
	
	public void FillEmptySlots() {
		foreach (var slot in Global.ArmourSlots) {
			var referenceEquipment = ReferenceEquipment.GetEquipmentFromSlot(slot);
			if (GetEquipmentFromSlot(slot) is not { IsEmpty: false, Item: not null }) {
				var itemType = Helper.EquipmentIndexToItemEnumType(slot);
				if (!itemType.HasValue) continue;
				
				ItemObject? item;
				if (referenceEquipment is { IsEmpty: false, Item: not null }) {
					List<ItemObject> itemList         = new() { referenceEquipment.Item };
					var              itemsByCharacter = Cache.GetItemsByTypeTierAndCulture(itemType.Value, Character.Tier, Character.Culture);
					if (itemsByCharacter != null) itemList.AddRange(itemsByCharacter);
					
					if (referenceEquipment.Item.Culture is CultureObject cultureObject) {
						var itemsByReference = Cache.GetItemsByTypeTierAndCulture(itemType.Value, (int)referenceEquipment.Item.Tier, cultureObject);
						if (itemsByReference != null) itemList.AddRange(itemsByReference);
					}
					
					item = WeightedRandomSelector.SelectItem(itemList, referenceEquipment.Item.Effectiveness);
				}
				else {
					item = Cache.GetItemsByTypeTierAndCulture(itemType.Value, Character.Tier, Character.Culture)?.GetRandomElement();
					if (item == null) continue;
				}
				
				SetEquipment(slot, new EquipmentElement(item));
			}
		}
		
		foreach (var slot in Global.EquipmentSlots)
			if (GetEquipmentFromSlot(slot) is not { IsEmpty: false, Item: not null })
				SetEquipment(slot, ReferenceEquipment.GetEquipmentFromSlot(slot));
	}
}