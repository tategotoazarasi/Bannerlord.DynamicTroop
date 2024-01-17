using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using Bannerlord.DynamicTroop.Comparers;
using Bannerlord.DynamicTroop.Extensions;
using Bannerlord.DynamicTroop.Patches;
using HarmonyLib;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using ItemPriorityQueue = TaleWorlds.Library.PriorityQueue<TaleWorlds.Core.ItemObject, (int, int)>;

namespace Bannerlord.DynamicTroop;

public class EveryoneCampaignBehavior : CampaignBehaviorBase {
	public static Dictionary<MBGUID, Dictionary<ItemObject, int>> PartyArmories = new();

	private static Data _data = new();

	private static readonly Dictionary<ItemObject.ItemTypeEnum, Func<int, int>> EquipmentAndThresholds = new() {
		{ ItemObject.ItemTypeEnum.BodyArmor, memberCnt => Math.Max(2       * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.LegArmor, memberCnt => Math.Max(2        * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.HeadArmor, memberCnt => Math.Max(2       * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.HandArmor, memberCnt => Math.Max(2       * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.Cape, memberCnt => Math.Max(2            * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.Horse, memberCnt => Math.Max(2           * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.HorseHarness, memberCnt => Math.Max(2    * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.Bow, memberCnt => Math.Max(2             * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.Crossbow, memberCnt => Math.Max(2        * memberCnt, memberCnt     + 100) },
		{ ItemObject.ItemTypeEnum.OneHandedWeapon, memberCnt => Math.Max(8 * memberCnt, 4 * memberCnt + 400) },
		{ ItemObject.ItemTypeEnum.TwoHandedWeapon, memberCnt => Math.Max(8 * memberCnt, 4 * memberCnt + 400) },
		{ ItemObject.ItemTypeEnum.Polearm, memberCnt => Math.Max(8         * memberCnt, 4 * memberCnt + 400) }
	};

	public static readonly Dictionary<ItemObject.ItemTypeEnum, List<ItemObject>> ItemListByType = new();

	private static void InitializeItemListByType() {
		ItemObject.ItemTypeEnum[] itemTypes = {
												  ItemObject.ItemTypeEnum.BodyArmor,
												  ItemObject.ItemTypeEnum.LegArmor,
												  ItemObject.ItemTypeEnum.HeadArmor,
												  ItemObject.ItemTypeEnum.HandArmor,
												  ItemObject.ItemTypeEnum.Cape,
												  ItemObject.ItemTypeEnum.Horse,
												  ItemObject.ItemTypeEnum.HorseHarness,
												  ItemObject.ItemTypeEnum.Bow,
												  ItemObject.ItemTypeEnum.Crossbow,
												  ItemObject.ItemTypeEnum.OneHandedWeapon,
												  ItemObject.ItemTypeEnum.TwoHandedWeapon,
												  ItemObject.ItemTypeEnum.Polearm
											  };

		foreach (var itemType in itemTypes) {
			var items = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
									   ?.WhereQ(item => item != null && item.ItemType == itemType)
									   ?.OrderByQ(item => item.Tier)
									   ?.ThenBy(item => item.Value)
									   .ToListQ();

			ItemListByType[itemType] = items ?? new List<ItemObject>();
		}
	}

	public override void RegisterEvents() {
		CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
		CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
		CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
		CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
		CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
		InitializeItemListByType();

		//Global.InitializeCraftingTemplatesByItemType();
	}

	private void OnGameLoaded(CampaignGameStarter starter) {
		Global.Debug("OnGameLoaded() called");
		IEnumerable<MobileParty>? validParties = Campaign.Current?.MobileParties?.WhereQ(party => party.IsValid());
		if (validParties == null) return;

		foreach (var validParty in validParties)
			if (!PartyArmories.ContainsKey(validParty.Id))
				OnMobilePartyCreated(validParty);
	}

	public override void SyncData(IDataStore dataStore) {
		if (dataStore.IsSaving) {
			_data.PartyArmories = ConvertToUIntGuidDict(PartyArmories);

			var tempData = _data;
			_ = dataStore.SyncDataAsJson("DynamicTroopPartyArmories", ref tempData);

			if (tempData != null) {
				_data = tempData;
				Global.Debug($"saved {PartyArmories.Count} entries");
				_data.PartyArmories.Clear();
			}
			else { Global.Error("Save Error"); }
		}
		else if (dataStore.IsLoading) {
			_data.PartyArmories.Clear();

			var tempData = _data;
			_ = dataStore.SyncDataAsJson("DynamicTroopPartyArmories", ref tempData);

			if (tempData != null) {
				_data         = tempData;
				PartyArmories = ConvertToGuidDict(_data.PartyArmories);
				_data.PartyArmories.Clear();
				Global.Debug($"loaded {PartyArmories.Count} entries");
			}
			else { Global.Error("Load Error"); }
		}
	}

	private void WeeklyTick() {
		//GarbageCollectParties();
		//GarbageCollectEquipments();
	}

	public void GarbageCollectParties() {
		var keysToRemove = PartyArmories.Keys.WhereQ(id => {
														 var obj = MBObjectManager.Instance.GetObject(id);
														 if (obj == null) return true;

														 if (obj is MobileParty mobileParty) {
															 if (mobileParty.MemberRoster == null) return true;

															 if (mobileParty.MemberRoster.GetTroopRoster() == null)
																 return true;

															 if (mobileParty.MemberRoster.GetTroopRoster().IsEmpty())
																 return true;
														 }

														 return false;
													 })
										.ToArrayQ();

		foreach (var key in keysToRemove) _ = PartyArmories.Remove(key);

		Global.Debug($"Garbage collected {keysToRemove.Length} parties");
	}

	//public void GarbageCollectEquipments() { GarbageCollectArmors(); }

	private void GarbageCollectEquipments(MobileParty mobileParty) {
		if (!mobileParty.IsValid() || mobileParty.MemberRoster?.GetTroopRoster() == null) return;

		var memberCnt = mobileParty.MemberRoster.GetTroopRoster()
								   .WhereQ(element => element.Character is { IsHero: false })
								   .SumQ(element => element.Number);
		foreach (var equipmentAndThreshold in EquipmentAndThresholds) {
			var partyArmory = PartyArmories.GetValueSafe(mobileParty.Id);
			if (partyArmory == null || partyArmory.IsEmpty()) return;

			var armorTotalCount = partyArmory.WhereQ(kv => kv.Key?.ItemType == equipmentAndThreshold.Key)
											 .SumQ(kv => kv.Value);
			var surplusCount = armorTotalCount - equipmentAndThreshold.Value(memberCnt);
			if (surplusCount <= 0) continue;

			var surplusCountCpy = surplusCount;

			// 创建优先级队列
			ItemPriorityQueue armorQueue = new(new ArmorComparer());
			foreach (var kv in partyArmory.WhereQ(kv => kv.Key.ItemType == equipmentAndThreshold.Key))
				armorQueue.Enqueue(kv.Key, ((int)kv.Key.Tier, kv.Key.Value));

			// 移除多余的盔甲
			while (surplusCount > 0 && armorQueue.Count > 0) {
				var lowestArmor   = armorQueue.Dequeue();
				var countToRemove = Math.Min(partyArmory[lowestArmor.Key], surplusCount);
				partyArmory[lowestArmor.Key] -= countToRemove;
				surplusCount                 -= countToRemove;
				if (partyArmory[lowestArmor.Key] == 0) _ = partyArmory.Remove(lowestArmor.Key);
			}

			Global.Debug($"Garbage collected {surplusCountCpy - surplusCount}x{equipmentAndThreshold.Key} armors from party {mobileParty.Name}");
		}
	}

	private void ReplenishBasicTroopEquipments(MobileParty mobileParty) {
		if (!mobileParty.IsValid() || mobileParty.MemberRoster?.GetTroopRoster() == null) return;

		var partyArmory = PartyArmories[mobileParty.Id];
		var memberCnt   = CountNonHeroMembers(mobileParty.MemberRoster);

		foreach (var typeKv in ItemListByType) {
			var requiredNum = CalculateRequiredItemCount(mobileParty.MemberRoster, typeKv.Key, memberCnt);
			ReplenishItemTypeInArmory(mobileParty, partyArmory, typeKv.Key, requiredNum, memberCnt);
		}
	}

	private static int CountNonHeroMembers(TroopRoster roster) {
		return roster.GetTroopRoster()
					 .WhereQ(element => element.Character is { IsHero: false })
					 .SumQ(element => element.Number);
	}

	private static int CalculateRequiredItemCount(TroopRoster roster, ItemObject.ItemTypeEnum itemType, int memberCnt) {
		return itemType switch {
				   ItemObject.ItemTypeEnum.Horse or ItemObject.ItemTypeEnum.HorseHarness => roster.GetTroopRoster()
					   .WhereQ(element => element.Character.IsMounted)
					   .SumQ(element => element.Number),
				   ItemObject.ItemTypeEnum.Bow
					   or ItemObject.ItemTypeEnum.Crossbow
					   or ItemObject.ItemTypeEnum.OneHandedWeapon
					   or ItemObject.ItemTypeEnum.TwoHandedWeapon
					   or ItemObject.ItemTypeEnum.Polearm => roster.GetTroopRoster()
																   .WhereQ(element => element.Character.IsMounted)
																   .SumQ(element => element.Number *
																			 Global
																				 .CountCharacterEquipmentItemTypes(element
																						 .Character,
																					 itemType)),
				   _ => memberCnt
			   };
	}

	private void ReplenishItemTypeInArmory(MobileParty                 party,
										   Dictionary<ItemObject, int> armory,
										   ItemObject.ItemTypeEnum     itemType,
										   int                         requiredNum,
										   int                         memberCnt) {
		var culture         = GetPartyCulture(party);
		var armorTotalCount = armory.WhereQ(kv => kv.Key.ItemType == itemType).SumQ(kv => kv.Value);
		if (armorTotalCount < requiredNum)
			AddItemsToArmoryBasedOnItemType(party, itemType, culture, requiredNum, armorTotalCount, memberCnt);
	}

	private static CultureObject? GetPartyCulture(MobileParty? party) {
		return party?.Owner?.Culture ?? party?.LeaderHero?.Culture;
	}

	private void AddItemsToArmoryBasedOnItemType(MobileParty             party,
												 ItemObject.ItemTypeEnum itemType,
												 CultureObject?          culture,
												 int                     requiredNum,
												 int                     currentNum,
												 int                     memberCnt) {
		/*if (itemType == ItemObject.ItemTypeEnum.OneHandedWeapon ||
			itemType == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
			itemType == ItemObject.ItemTypeEnum.Polearm) {
			var craftedWeapons =
				Global.CreateRandomCraftedItemsByItemType(itemType, culture, Math.Max(requiredNum, memberCnt));
			foreach (var weapon in craftedWeapons)
				if (weapon != null) {
					AddItemToPartyArmory(partyId, weapon, 1);
					Global.Debug($"Replenished 1x{weapon.Name} weapon for party {partyId}");
				}
		}*/
		var itemToAdd = ItemListByType[itemType]
			?.FirstOrDefaultQ(item => item != null && (item.Culture == null || item.Culture == culture));
		if (itemToAdd == null) return;

		AddItemToPartyArmory(party.Id, itemToAdd, EquipmentAndThresholds[itemType](requiredNum) - currentNum);
		Global.Debug($"Replenished {EquipmentAndThresholds[itemType](requiredNum) - currentNum}x{itemToAdd.Name} armors for party {party.Name}");
	}

	private void DailyTickParty(MobileParty mobileParty) {
		if (!mobileParty.IsValid()) return;

		AllocateRandomEquipmentToPartyArmory(mobileParty);
		if (CampaignTime.Now.GetDayOfWeek != mobileParty.Id.InternalValue % 7) return;

		GarbageCollectEquipments(mobileParty);
		ReplenishBasicTroopEquipments(mobileParty);
		MoveRosterToArmory(mobileParty);
	}

	private void MoveRosterToArmory(MobileParty mobileParty) {
		var elements =
			mobileParty.ItemRoster.WhereQ(element => (element.EquipmentElement.Item?.HasArmorComponent  ?? false) ||
													 (element.EquipmentElement.Item?.HasWeaponComponent ?? false));
		foreach (var element in elements)
			if (element.EquipmentElement.Item != null) {
				AddItemToPartyArmory(mobileParty.Id, element.EquipmentElement.Item, element.Amount);
				Global.Debug($"equipment {element.EquipmentElement.Item.Name}x{element.Amount} moved to armory from {mobileParty.Name}");
				mobileParty.ItemRoster.Remove(element);
				var hero                    = mobileParty.LeaderHero ?? mobileParty.Owner;
				if (hero != null) hero.Gold += element.EquipmentElement.Item.Value * element.Amount;
			}
	}

	private void AllocateRandomEquipmentToPartyArmory(MobileParty mobileParty) {
		ItemObject.ItemTypeEnum[] itemTypes = {
												  ItemObject.ItemTypeEnum.BodyArmor,
												  ItemObject.ItemTypeEnum.LegArmor,
												  ItemObject.ItemTypeEnum.HeadArmor,
												  ItemObject.ItemTypeEnum.HandArmor,
												  ItemObject.ItemTypeEnum.Cape,
												  ItemObject.ItemTypeEnum.Horse,
												  ItemObject.ItemTypeEnum.HorseHarness,
												  ItemObject.ItemTypeEnum.Bow,
												  ItemObject.ItemTypeEnum.Crossbow,
												  ItemObject.ItemTypeEnum.OneHandedWeapon,
												  ItemObject.ItemTypeEnum.TwoHandedWeapon,
												  ItemObject.ItemTypeEnum.Polearm
											  };

		if (mobileParty.MemberRoster == null ||

			//mobileParty.MemberRoster.Count            <= 1    ||
			mobileParty.MemberRoster.GetTroopRoster() == null ||
			mobileParty.MemberRoster.GetTroopRoster().IsEmpty())
			return;

		var factor = mobileParty.CalculateClanProsperityFactor();
		for (var i = 0; i < factor; i++) {
			var randomMember = mobileParty.MemberRoster.GetTroopRoster().GetRandomElement();
			if (randomMember.Character == null                  ||
				randomMember.Character.IsHero                   ||
				randomMember.Character.BattleEquipments == null ||
				randomMember.Character.BattleEquipments.IsEmpty())
				return;

			var randomEquipment = randomMember.Character.BattleEquipments.GetRandomElementInefficiently();
			if (randomEquipment == null || randomEquipment.IsEmpty() || !randomEquipment.IsValid) return;

			var randomElement = randomEquipment.GetEquipmentFromSlot(Global.ArmourAndHorsesSlots.GetRandomElement());
			if (randomElement.IsEmpty || randomElement.Item == null) return;

			AddItemToPartyArmory(mobileParty.Id, randomElement.Item, 1);
			Global.Debug($"random equipment (troop) {randomElement.Item.Name} added to {mobileParty.Name}");
			/*var weapon = Crafting.CreateRandomCraftedItem(mobileParty.LeaderHero.Culture);
			if (weapon != null) {
				AddItemToPartyArmory(mobileParty.Id, weapon, 1);
				Global.Debug($"random weapon {weapon.Name} added to {mobileParty.Name}");
			}*/
			var randomByCultureAndTier =
				Cache.GetItemsByTierAndCulture(itemTypes.GetRandomElement(),
											   mobileParty.GetClanTier() +
											   (SubModule.Settings?.Difficulty.SelectedIndex ?? 0 + 1),
											   GetPartyCulture(mobileParty))
					 ?.GetRandomElement();
			if (randomByCultureAndTier == null) return;

			AddItemToPartyArmory(mobileParty.Id, randomByCultureAndTier, 1);
			Global.Debug($"random equipment (tier) {randomByCultureAndTier.Name} added to {mobileParty.Name}");
		}
	}

	private void OnMobilePartyCreated(MobileParty mobileParty) {
		if (!mobileParty.IsValid()) return;

		if (!PartyArmories.TryGetValue(mobileParty.Id, out var itemDict)) {
			itemDict                      = new Dictionary<ItemObject, int>();
			PartyArmories[mobileParty.Id] = itemDict;
		}

		var list = mobileParty.GetItems();
		foreach (var element in list) {
			if (!itemDict.TryGetValue(element.Item, out var count)) count = 0;

			itemDict[element.Item] = count + 1;
		}

		Global.Log($"Mobile party {mobileParty.Name} created, {list.Count} start equipment added",
				   Colors.Green,
				   Level.Debug);
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase partyBase) {
		if (mobileParty?.Id != null) _ = PartyArmories.Remove(mobileParty.Id);

		if (!mobileParty.IsValid()) return;

		if (mobileParty is { Name: not null } && partyBase is { Name: not null })
			Global.Log($"Mobile party {mobileParty.Name} destroyed, partyBase = {partyBase.Name}",
					   Colors.Green,
					   Level.Debug);
	}

	private void OnMapEventEnded(MapEvent mapEvent) {
		Global.Log($"Map Event ended with state {mapEvent.BattleState}", Colors.Green, Level.Debug);
		if (mapEvent.BattleState is not BattleState.AttackerVictory and not BattleState.DefenderVictory) return;

		var validWinnerParties = FilterValidParties(mapEvent.BattleState == BattleState.AttackerVictory
														? mapEvent.AttackerSide.Parties
														: mapEvent.DefenderSide.Parties)
			.ToArrayQ();
		var validLoserParties = FilterValidParties(mapEvent.BattleState == BattleState.AttackerVictory
													   ? mapEvent.DefenderSide.Parties
													   : mapEvent.AttackerSide.Parties);

		LogParties(validWinnerParties, "Winning");
		LogParties(validLoserParties,  "Defeated");

		var totalWinnerStrength = CalculateTotalStrength(validWinnerParties);
		DistributeLootRandomly(validWinnerParties, totalWinnerStrength, validLoserParties);
	}

	private MapEventParty[] FilterValidParties(IEnumerable<MapEventParty> parties) {
		return parties.WhereQ(IsMapEventPartyValid).ToArrayQ();
	}

	private static void LogParties(IEnumerable<MapEventParty> parties, string label) {
		foreach (var party in parties)
			Global.Log($"{label} party: {party.Party.Name}#{party.Party.MobileParty.Id}", Colors.Green, Level.Debug);
	}

	private static float CalculateTotalStrength(IEnumerable<MapEventParty> parties) {
		return parties.SumQ(party => party.Party.TotalStrength);
	}

	private void DistributeLootRandomly(MapEventParty[]            winnerParties,
										float                      totalWinnerStrength,
										IEnumerable<MapEventParty> loserParties) {
		Random                  random             = new();
		var                     lootItemsWithCount = GetAllLootItems(loserParties);
		Dictionary<MBGUID, int> partyLootCount     = new();

		foreach (var lootItem in lootItemsWithCount) {
			var item      = lootItem.Key;
			var itemCount = lootItem.Value;

			for (var i = 0; i < itemCount; i++) {
				var chosenParty = ChoosePartyRandomly(winnerParties, totalWinnerStrength, random);
				if (chosenParty == null) return;

				AddItemToPartyArmory(chosenParty.Id, item, 1);

				// 更新统计信息
				if (partyLootCount.ContainsKey(chosenParty.Id))
					partyLootCount[chosenParty.Id]++;
				else
					partyLootCount[chosenParty.Id] = 1;
			}
		}

		// 记录每个party获得的物品总数
		foreach (var partyCount in partyLootCount)
			Global.Debug($"Party {partyCount.Key} looted {partyCount.Value} items");
	}

	private static Dictionary<ItemObject, int> GetAllLootItems(IEnumerable<MapEventParty> parties) {
		Dictionary<ItemObject, int> itemsWithCount = new();
		foreach (var party in parties)
			if (PartyArmories.TryGetValue(party.Party.MobileParty.Id, out var inventory))
				foreach (var item in inventory)
					if (itemsWithCount.ContainsKey(item.Key))
						itemsWithCount[item.Key] += item.Value;
					else
						itemsWithCount.Add(item.Key, item.Value);

		return itemsWithCount;
	}

	private static MobileParty? ChoosePartyRandomly(IEnumerable<MapEventParty> parties,
													float                      totalStrength,
													Random                     random) {
		var randomValue        = random.NextDouble() * totalStrength;
		var cumulativeStrength = 0f;

		foreach (var party in parties) {
			cumulativeStrength += party.Party.TotalStrength;
			if (cumulativeStrength >= randomValue) return party.Party.MobileParty;
		}

		return null;
	}

	private static void OnTroopRecruited(Hero?           recruiterHero,
										 Settlement      recruitmentSettlement,
										 Hero            recruitmentSource,
										 CharacterObject troop,
										 int             amount) {
		if (recruiterHero == null) return;

		var party = recruiterHero.PartyBelongedTo;
		if (!party.IsValid()) return;

		// 确保PartyArmories包含party.Id键
		if (!PartyArmories.TryGetValue(party.Id, out var partyInventory)) {
			partyInventory          = new Dictionary<ItemObject, int>();
			PartyArmories[party.Id] = partyInventory;
		}

		var list = RecruitmentPatch.GetAllRecruitEquipments(troop);
		foreach (var element in list) {
			// 确保partyInventory包含element.Item键
			if (!partyInventory.TryGetValue(element.Item, out var count)) count = 0;

			partyInventory[element.Item] = count + amount;
		}

		Global.Log($"troop {troop.Name}x{amount} recruited by recruiterHero={recruiterHero.Name} in party {party.Name}, {list.Count * amount} start equipment added",
				   Colors.Green,
				   Level.Debug);
	}

	private static void AddItemToPartyArmory(MBGUID partyId, ItemObject item, int count) {
		if (!PartyArmories.TryGetValue(partyId, out var inventory)) {
			inventory              = new Dictionary<ItemObject, int>();
			PartyArmories[partyId] = inventory;
		}

		if (!inventory.TryGetValue(item, out var currentCount)) currentCount = 0;

		inventory[item] = currentCount + count;
	}

	private static bool IsMapEventPartyValid(MapEventParty? party) {
		return party is { Party.MobileParty: var mobileParty } && mobileParty.IsValid();
	}

	private static Dictionary<uint, Dictionary<string, int>> ConvertToUIntGuidDict(
		Dictionary<MBGUID, Dictionary<ItemObject, int>> guidDict) {
		Dictionary<uint, Dictionary<string, int>> uintDict = new();
		foreach (var pair in guidDict) {
			if (!uintDict.TryGetValue(pair.Key.InternalValue, out var innerDict)) {
				innerDict = new Dictionary<string, int>();
				uintDict.Add(pair.Key.InternalValue, innerDict);
			}

			foreach (var innerPair in pair.Value)
				if (innerDict.ContainsKey(innerPair.Key.StringId))
					innerDict[innerPair.Key.StringId] += innerPair.Value;
				else
					innerDict.Add(innerPair.Key.StringId, innerPair.Value);
		}

		return uintDict;
	}

	private static Dictionary<MBGUID, Dictionary<ItemObject, int>> ConvertToGuidDict(
		Dictionary<uint, Dictionary<string, int>> uintDict) {
		Dictionary<MBGUID, Dictionary<ItemObject, int>> guidDict = new();
		foreach (var pair in uintDict) {
			if (!guidDict.TryGetValue(new MBGUID(pair.Key), out var innerDict)) {
				innerDict = new Dictionary<ItemObject, int>();
				guidDict.Add(new MBGUID(pair.Key), innerDict);
			}

			foreach (var innerPair in pair.Value) {
				var itemObject = MBObjectManager.Instance.GetObject<ItemObject>(innerPair.Key) ??
								 ItemObject.GetCraftedItemObjectFromHashedCode(innerPair.Key);
				if (itemObject != null) {
					if (innerDict.ContainsKey(itemObject))
						innerDict[itemObject] += innerPair.Value;
					else
						innerDict.Add(itemObject, innerPair.Value);
				}
				else { Global.Warn($"cannot get object {innerPair.Key}"); }
			}
		}

		return guidDict;
	}

	[Serializable]
	private class Data {
		[SaveableField(2)] public Dictionary<uint, Dictionary<string, int>> PartyArmories = new();
	}
}