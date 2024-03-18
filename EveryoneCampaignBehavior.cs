#region

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
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using ItemPriorityQueue = TaleWorlds.Library.PriorityQueue<TaleWorlds.Core.ItemObject, (int, int)>;

#endregion

namespace Bannerlord.DynamicTroop;

public class EveryoneCampaignBehavior : CampaignBehaviorBase {
	public static Dictionary<MBGUID, Dictionary<ItemObject, int>> PartyArmories = new();

	private static Data _data = new();

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
		foreach (var itemType in Global.ItemTypes) {
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
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		InitializeItemListByType();

		//Global.InitializeCraftingTemplatesByItemType();
	}

	private void OnNewGameCreated(CampaignGameStarter starter) {
		Global.Debug("OnNewGameCreated() called");
		PartyArmories.Clear();
	}


	private void OnGameLoaded(CampaignGameStarter starter) {
		Global.Debug("OnGameLoaded() called");
		IEnumerable<MobileParty>? validParties = Campaign.Current?.MobileParties?.WhereQ(party => party.IsValid());
		if (validParties == null) return;

		foreach (var validParty in validParties)
			if (!PartyArmories.ContainsKey(validParty.Id))
				OnMobilePartyCreated(validParty);

		CharacterObjectExtension.Init();
	}

	public override void SyncData(IDataStore dataStore) {
		if (dataStore.IsSaving) {
			_data.PartyArmories = ConvertToUIntGuidDict(PartyArmories);
			var tempData = _data;
			if (dataStore.SyncDataAsJson("DynamicTroopPartyArmories", ref tempData) && tempData != null) {
				_data = tempData;
				Global.Debug($"saved {PartyArmories.Count} entries");
				_data.PartyArmories.Clear();
			}
			else { Global.Error("Save Error"); }
		}
		else if (dataStore.IsLoading) {
			_data.PartyArmories.Clear();
			var tempData = _data;
			if (dataStore.SyncDataAsJson("DynamicTroopPartyArmories", ref tempData) && tempData != null) {
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
		foreach (var party in parties)
			if (PartyArmories.TryGetValue(party.Party.MobileParty.Id, out var inventory))
				foreach (var item in inventory) {
					if (!ItemBlackList.Test(item.Key)) continue;
					if (itemsWithCount.ContainsKey(item.Key))
						itemsWithCount[item.Key] += item.Value;
					else
						itemsWithCount.Add(item.Key, item.Value);
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
			cumulativeStrength += party.Party.TotalStrength;
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

		// 确保PartyArmories包含party.Id键
		if (!PartyArmories.TryGetValue(party.Id, out var partyInventory)) {
			partyInventory          = new Dictionary<ItemObject, int>();
			PartyArmories[party.Id] = partyInventory;
		}

		var list = RecruitmentPatch.GetRecruitEquipments(troop);
		foreach (var element in list) {
			// 确保partyInventory包含element.Item键
			if (!partyInventory.TryGetValue(element.Item, out var count)) count = 0;

			partyInventory[element.Item] = count + amount;
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

	/// <summary>
	///     将包含MBGUID键和物品字典的字典转换为使用uint键和字符串ID的字典。
	/// </summary>
	/// <param name="guidDict"> 以MBGUID为键，以物品和数量的字典为值的字典。 </param>
	/// <returns> 转换后的字典，其中键为uint类型，值为以物品字符串ID和数量构成的字典。 </returns>
	private static Dictionary<uint, Dictionary<string, int>> ConvertToUIntGuidDict(
		Dictionary<MBGUID, Dictionary<ItemObject, int>> guidDict) {
		Dictionary<uint, Dictionary<string, int>> uintDict = new();
		foreach (var pair in guidDict) {
			var party = Campaign.Current.MobileParties.FirstOrDefault(party => party.Id == pair.Key);
			if (party is not {
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
							 }) continue;
			if (!uintDict.TryGetValue(pair.Key.InternalValue, out var innerDict)) {
				innerDict = new Dictionary<string, int>();
				uintDict.Add(pair.Key.InternalValue, innerDict);
			}

			foreach (var innerPair in pair.Value)
				if (innerPair is { Key: not null, Value: > 0 }) {
					if (innerDict.ContainsKey(innerPair.Key.StringId))
						innerDict[innerPair.Key.StringId] += innerPair.Value;
					else
						innerDict.Add(innerPair.Key.StringId, innerPair.Value);
				}
		}

		return uintDict;
	}

	/// <summary>
	///     将使用uint键和字符串ID的字典转换为包含MBGUID键和物品字典的字典。
	/// </summary>
	/// <param name="uintDict"> 以uint为键，以物品字符串ID和数量的字典为值的字典。 </param>
	/// <returns> 转换后的字典，其中键为MBGUID类型，值为以物品对象和数量构成的字典。 </returns>
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
				if (itemObject != null && innerPair.Value > 0) {
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