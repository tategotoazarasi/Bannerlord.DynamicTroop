#region

using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using DynamicTroopEquipmentReupload.Comparers;
using DynamicTroopEquipmentReupload.Extensions;
using DynamicTroopEquipmentReupload.Patches;
using HarmonyLib;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using ItemPriorityQueue = TaleWorlds.Library.PriorityQueue<TaleWorlds.Core.ItemObject, (int, int)>;

#endregion

namespace DynamicTroopEquipmentReupload;

public class EveryoneCampaignBehavior : CampaignBehaviorBase {
	public static readonly Dictionary<MBGUID, Dictionary<ItemObject, int>> PartyArmories = new();

	private readonly Dictionary<uint, Dictionary<string, int>> _unresolvedPartyArmories = new();
	private Data _data = new();

	public static readonly Dictionary<ItemObject.ItemTypeEnum, Func<int, int>> EquipmentAndThresholds =
		new() {
				  { ItemObject.ItemTypeEnum.BodyArmor, memberCnt => Math.Max(2    * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.LegArmor, memberCnt => Math.Max(2     * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.HeadArmor, memberCnt => Math.Max(2    * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.HandArmor, memberCnt => Math.Max(2    * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.Cape, memberCnt => Math.Max(2         * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.Shield, memberCnt => Math.Max(2       * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.Horse, memberCnt => Math.Max(2        * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.HorseHarness, memberCnt => Math.Max(2 * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.Bow, memberCnt => Math.Max(2          * memberCnt, memberCnt + 100) },
				  { ItemObject.ItemTypeEnum.Crossbow, memberCnt => Math.Max(2     * memberCnt, memberCnt + 100) }, {
																													   ItemObject.ItemTypeEnum.OneHandedWeapon, memberCnt => Math.Max(8 * memberCnt, 4 * memberCnt + 400)
																												   }, {
																														  ItemObject.ItemTypeEnum.TwoHandedWeapon, memberCnt => Math.Max(8 * memberCnt, 4 * memberCnt + 400)
																													  },
				  { ItemObject.ItemTypeEnum.Polearm, memberCnt => Math.Max(8 * memberCnt, 4 * memberCnt + 400) }
			  };

	public static readonly Dictionary<ItemObject.ItemTypeEnum, List<ItemObject>> ItemListByType = new();

	private static void InitializeItemListByType() {
		ItemListByType.Clear();
		Cache.Clear();
		ItemBlackList.ResetCache();

		foreach (var itemType in Global.ItemTypes) {
			var items = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
									   ?.WhereQ(item => ArmyArmory.TryResolveArmoryItem(item, out _) && item.ItemType == itemType)
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
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);

		//Global.InitializeCraftingTemplatesByItemType();
	}

	private void OnNewGameCreated(CampaignGameStarter starter) {
		Global.Debug("OnNewGameCreated() called");
		_unresolvedPartyArmories.Clear();
		PartyArmories.Clear();

		InitializeItemListByType();
		CharacterObjectExtension.Init();
	}


	private void OnGameLoaded(CampaignGameStarter starter) {
		Global.Debug("OnGameLoaded() called");
		InitializeItemListByType();
		RestoreReadyPartyArmories();

		IEnumerable<MobileParty>? validParties = Campaign.Current?.MobileParties?.WhereQ(party => party.IsValid());
		if (validParties != null) {
			foreach (var validParty in validParties) {
				if (!PartyArmories.ContainsKey(validParty.Id))
					OnMobilePartyCreated(validParty);
				else
					SanitizePartyArmory(validParty.Id);
			}
		}

		CharacterObjectExtension.Init();
	}

	public override void SyncData(IDataStore dataStore) {
		if (dataStore.IsSaving) {
			_data.PartyArmories = CreateSerializedPartyArmories();
			var tempData = _data;
			if (dataStore.SyncDataAsJson("DynamicTroopPartyArmories", ref tempData) && tempData != null) {
				_data = tempData;
				Global.Debug($"saved {PartyArmories.Count} entries");
			}
			else
				Global.Error("Save Error");
		}
		else if (dataStore.IsLoading) {
			PartyArmories.Clear();
			_unresolvedPartyArmories.Clear();

			var tempData = new Data();
			if (dataStore.SyncDataAsJson("DynamicTroopPartyArmories", ref tempData) && tempData != null) {
				_data = tempData;
				_data.PartyArmories ??= new Dictionary<uint, Dictionary<string, int>>();
				foreach (var partyEntry in _data.PartyArmories) {
					if (partyEntry.Value == null)
						continue;
					var itemCounts = new Dictionary<string, int>();
					foreach (var itemEntry in partyEntry.Value) {
						if (!string.IsNullOrEmpty(itemEntry.Key) && itemEntry.Value > 0)
							itemCounts[itemEntry.Key] = itemEntry.Value;
					}

					if (itemCounts.Count > 0)
						_unresolvedPartyArmories[partyEntry.Key] = itemCounts;
				}
			}
			else
				Global.Error("Load Error");
		}
	}

	private void WeeklyTick() {
		//GarbageCollectParties();
		//GarbageCollectEquipments();
	}

	private void GarbageCollectEquipments(MobileParty mobileParty) {
		if (!mobileParty.IsValid() || mobileParty.MemberRoster?.GetTroopRoster() == null) return;

		var memberCnt = mobileParty.MemberRoster.GetTroopRoster()
								   .WhereQ(element => element.Character is { IsHero: false })
								   .SumQ(element => element.Number);
		foreach (var equipmentAndThreshold in EquipmentAndThresholds) {
			var partyArmory = PartyArmories.GetValueSafe(mobileParty.Id);
			if (partyArmory == null || partyArmory.IsEmpty()) return;

			var armorTotalCount = partyArmory.WhereQ(kv => kv.Key.ItemType == equipmentAndThreshold.Key)
											 .SumQ(kv => kv.Value);
			var surplusCount = armorTotalCount - memberCnt;
			if (surplusCount <= 0) continue;

			var surplusCountCpy = surplusCount;

			// 创建优先级队列
			ItemPriorityQueue armorQueue = new(new EquipmentEffectivenessComparer());
			foreach (var kv in partyArmory.WhereQ(kv => kv.Key.ItemType == equipmentAndThreshold.Key))
				armorQueue.Enqueue(kv.Key, ((int)kv.Key.Tier, kv.Key.Value));

			// 移除多余的装备
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

	private void DailyTickParty(MobileParty mobileParty) {
		if (!mobileParty.IsValid()) return;

		if (!GarbageCollectParties(mobileParty))
			AllocateRandomEquipmentToPartyArmory(mobileParty);
		else
			Global.Debug($"garbage collect party {mobileParty.Name}");

		if (CampaignTime.Now.GetDayOfWeek != mobileParty.Id.InternalValue % 7) return;

		GarbageCollectEquipments(mobileParty);

		//ReplenishBasicTroopEquipments(mobileParty);
		MoveRosterToArmory(mobileParty);
	}

	private bool GarbageCollectParties(MobileParty mobileParty) {
		if (mobileParty is not {
								   LeaderHero: {
												   CharacterObject       : { IsHero: true, IsPlayerCharacter: false },
												   IsHumanPlayerCharacter: false,
												   IsPartyLeader         : true,
												   IsAlive               : true,
												   IsActive              : true
											   },
								   Owner: {
											  CharacterObject       : { IsHero: true, IsPlayerCharacter: false },
											  IsHumanPlayerCharacter: false,
											  IsActive              : true,
											  IsAlive               : true
										  },
								   MemberRoster: { TotalHeroes: > 0, TotalManCount: > 0 },
								   IsDisbanding: false
							   })
			return PartyArmories.Remove(mobileParty.Id);
		return false;
	}

	private void MoveRosterToArmory(MobileParty mobileParty) {
		var elements = mobileParty.ItemRoster.WhereQ(element =>
														 (element.EquipmentElement.Item?.HasArmorComponent  ?? false) ||
														 (element.EquipmentElement.Item?.HasWeaponComponent ?? false))
								 .ToArrayQ();
		foreach (var element in elements)
			if (element.Amount > 0 && ArmyArmory.TryResolveArmoryItem(element.EquipmentElement.Item, out var item)) {
				AddItemToPartyArmory(mobileParty.Id, item, element.Amount);
				Global.Debug($"equipment {item.Name}x{element.Amount} moved to armory from {mobileParty.Name}");
				mobileParty.ItemRoster.Remove(element);
				var hero                    = mobileParty.LeaderHero ?? mobileParty.Owner;
				if (hero != null) hero.Gold += item.Value * element.Amount;
			}
	}

	private void AllocateRandomEquipmentToPartyArmory(MobileParty mobileParty) {
		if (mobileParty.MemberRoster                  == null ||
			mobileParty.MemberRoster.GetTroopRoster() == null ||
			mobileParty.MemberRoster.GetTroopRoster().IsEmpty())
			return;

		var randomEquipmentElementsFromTroop = mobileParty.GetRandomEquipmentsFromTroop();
		foreach (var equipmentElement in randomEquipmentElementsFromTroop) {
			AddItemToPartyArmory(mobileParty.Id, equipmentElement.Item, 1);
			Global.Debug($"random equipment (troop) {equipmentElement.Item.Name} added to {mobileParty.Name}");
		}

		var randomItemsFromFiefs = mobileParty.GetDailyEquipmentFromFiefs();
		foreach (var item in randomItemsFromFiefs) {
			AddItemToPartyArmory(mobileParty.Id, item, 1);
			Global.Debug($"random equipment (fief) {item.Name} added to {mobileParty.Name}");
		}

		var randomItemsFromClan = mobileParty.GetRandomEquipmentsFromClan();
		foreach (var item in randomItemsFromClan) {
			AddItemToPartyArmory(mobileParty.Id, item, 1);
			Global.Debug($"random equipment (clan) {item.Name} added to {mobileParty.Name}");
		}
	}

	private void OnMobilePartyCreated(MobileParty mobileParty) {
		if (!mobileParty.IsValid()) return;

		if (!PartyArmories.ContainsKey(mobileParty.Id))
			PartyArmories[mobileParty.Id] = new Dictionary<ItemObject, int>();

		var list = mobileParty.GetItems();
		foreach (var element in list) {
			if (element.Item != null)
				AddItemToPartyArmory(mobileParty.Id, element.Item, 1);
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
		return parties.SumQ(party => party.Party.EstimatedStrength);
	}

	/// <summary>
	///     根据战斗结果，随机分配战败方部队的战利品给胜利方的各个部队。
	/// </summary>
	/// <param name="winnerParties">       胜利方的部队数组。 </param>
	/// <param name="totalWinnerStrength"> 胜利方部队的总力量。 </param>
	/// <param name="loserParties">        战败方的部队集合。 </param>
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
		foreach (var party in parties) {
			var partyId = party.Party.MobileParty.Id;
			var inventory = SanitizePartyArmory(partyId);
			if (inventory == null)
				continue;

			foreach (var item in inventory) {
				if (item.Value <= 0 || !ItemBlackList.Test(item.Key))
					continue;

				itemsWithCount[item.Key] = itemsWithCount.TryGetValue(item.Key, out var currentCount)
					? currentCount + item.Value
					: item.Value;
			}
		}

		return itemsWithCount;
	}

	/// <summary>
	///     从一系列部队中根据其总力量随机选择一个部队。
	/// </summary>
	/// <param name="parties">       参与选择的部队集合。 </param>
	/// <param name="totalStrength"> 所有部队的总力量。 </param>
	/// <param name="random">        用于生成随机数的Random实例。 </param>
	/// <returns> 根据力量加权随机选中的移动部队，如果没有选中则返回null。 </returns>
	private static MobileParty? ChoosePartyRandomly(IEnumerable<MapEventParty> parties,
													float                      totalStrength,
													Random                     random) {
		var randomValue        = random.NextDouble() * totalStrength;
		var cumulativeStrength = 0f;

		foreach (var party in parties) {
			cumulativeStrength += party.Party.EstimatedStrength;
			if (cumulativeStrength >= randomValue) return party.Party.MobileParty;
		}

		return null;
	}

	/// <summary>
	///     当兵种被招募时的处理逻辑。
	/// </summary>
	/// <param name="recruiterHero">         进行招募的英雄。 </param>
	/// <param name="recruitmentSettlement"> 招募发生的定居点。 </param>
	/// <param name="recruitmentSource">     提供招募的英雄。 </param>
	/// <param name="troop">                 被招募的兵种。 </param>
	/// <param name="amount">                招募数量。 </param>
	private static void OnTroopRecruited(Hero?           recruiterHero,
										 Settlement      recruitmentSettlement,
										 Hero            recruitmentSource,
										 CharacterObject troop,
										 int             amount) {
		if (recruiterHero == null) return;

		var party = recruiterHero.PartyBelongedTo;
		if (!party.IsValid()) return;

		var list = RecruitmentPatch.GetRecruitEquipments(troop);
		foreach (var element in list) {
			if (element.Item != null)
				AddItemToPartyArmory(party.Id, element.Item, amount);
		}

		Global.Log($"troop {troop.Name}x{amount} recruited by recruiterHero={recruiterHero.Name} in party {party.Name}, {list.Count * amount} start equipment added",
				   Colors.Green,
				   Level.Debug);
	}

	/// <summary>
	///     向指定部队的装备库中添加物品。
	/// </summary>
	/// <param name="partyId"> 部队的唯一标识符。 </param>
	/// <param name="item">    要添加的物品。 </param>
	/// <param name="count">   添加的物品数量。 </param>
	public static Dictionary<ItemObject, int>? SanitizePartyArmory(MBGUID partyId) {
		if (!PartyArmories.TryGetValue(partyId, out var armory))
			return null;

		var sanitizedItems = new Dictionary<ItemObject, int>();
		foreach (var entry in armory) {
			if (entry.Value <= 0 || !ArmyArmory.TryResolveArmoryItem(entry.Key, out var item))
				continue;

			sanitizedItems[item] = sanitizedItems.TryGetValue(item, out var currentCount)
				? currentCount + entry.Value
				: entry.Value;
		}

		armory.Clear();
		foreach (var entry in sanitizedItems)
			armory[entry.Key] = entry.Value;

		return armory;
	}

	public static void AddItemToPartyArmory(MBGUID partyId, ItemObject item, int count) {
		if (count <= 0 || !ArmyArmory.TryResolveArmoryItem(item, out var resolvedItem) || !ItemBlackList.Test(resolvedItem))
			return;

		if (!PartyArmories.TryGetValue(partyId, out var inventory)) {
			inventory = new Dictionary<ItemObject, int>();
			PartyArmories[partyId] = inventory;
		}

		inventory[resolvedItem] = inventory.TryGetValue(resolvedItem, out var currentCount)
			? currentCount + count
			: count;
	}

	private static bool IsMapEventPartyValid(MapEventParty? party) {
		return party is { Party.MobileParty: var mobileParty } && mobileParty.IsValid();
	}

	private Dictionary<uint, Dictionary<string, int>> CreateSerializedPartyArmories() {
		var serializedArmories = new Dictionary<uint, Dictionary<string, int>>();

		foreach (var pendingParty in _unresolvedPartyArmories) {
			var party = FindActiveParty(new MBGUID(pendingParty.Key));
			if (!ShouldPersistParty(party))
				continue;

			serializedArmories[pendingParty.Key] = new Dictionary<string, int>(pendingParty.Value);
		}

		foreach (var partyEntry in PartyArmories) {
			var party = FindActiveParty(partyEntry.Key);
			if (!ShouldPersistParty(party))
				continue;

			var serializedItems = serializedArmories.TryGetValue(partyEntry.Key.InternalValue, out var existingItems)
				? existingItems
				: new Dictionary<string, int>();

			foreach (var itemEntry in partyEntry.Value) {
				if (itemEntry.Value <= 0 || !ArmyArmory.TryResolveArmoryItem(itemEntry.Key, out var item))
					continue;

				serializedItems[item.StringId] = serializedItems.TryGetValue(item.StringId, out var currentCount)
					? currentCount + itemEntry.Value
					: itemEntry.Value;
			}

			if (serializedItems.Count > 0)
				serializedArmories[partyEntry.Key.InternalValue] = serializedItems;
		}

		return serializedArmories;
	}

	private void RestoreReadyPartyArmories() {
		foreach (var pendingParty in new Dictionary<uint, Dictionary<string, int>>(_unresolvedPartyArmories)) {
			var partyId = new MBGUID(pendingParty.Key);
			if (FindActiveParty(partyId) == null)
				continue;

			if (!PartyArmories.TryGetValue(partyId, out var armory)) {
				armory = new Dictionary<ItemObject, int>();
				PartyArmories[partyId] = armory;
			}

			foreach (var pendingItem in new Dictionary<string, int>(pendingParty.Value)) {
				var item = ArmyArmory.ResolveArmoryItem(pendingItem.Key);
				if (item == null)
					continue;

				armory[item] = armory.TryGetValue(item, out var currentCount)
					? currentCount + pendingItem.Value
					: pendingItem.Value;
				pendingParty.Value.Remove(pendingItem.Key);
			}

			if (pendingParty.Value.Count == 0)
				_unresolvedPartyArmories.Remove(pendingParty.Key);
			else
				_unresolvedPartyArmories[pendingParty.Key] = pendingParty.Value;
		}
	}

	private static bool ShouldPersistParty(MobileParty? mobileParty) {
		return mobileParty is {
			LeaderHero: {
				CharacterObject       : { IsHero: true, IsPlayerCharacter: false },
				IsHumanPlayerCharacter: false,
				IsPartyLeader         : true,
				IsAlive               : true,
				IsActive              : true
			},
			Owner: {
				CharacterObject       : { IsHero: true, IsPlayerCharacter: false },
				IsHumanPlayerCharacter: false,
				IsActive              : true,
				IsAlive               : true
			},
			MemberRoster: { TotalHeroes: > 0, TotalManCount: > 0 },
			IsDisbanding: false
		};
	}

	private static MobileParty? FindActiveParty(MBGUID partyId) {
		return Campaign.Current?.MobileParties?.FirstOrDefault(party => party.Id == partyId && party.IsActive);
	}

	[Serializable]
	private class Data {
		[SaveableField(2)] public Dictionary<uint, Dictionary<string, int>> PartyArmories = new();
	}
}