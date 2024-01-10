#region

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Bannerlord.ButterLib.SaveSystem.Extensions;
	using log4net.Core;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.CampaignSystem.MapEvents;
	using TaleWorlds.CampaignSystem.Party;
	using TaleWorlds.CampaignSystem.Settlements;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.ObjectSystem;
	using TaleWorlds.SaveSystem;
	using ItemPriorityQueue = TaleWorlds.Library.PriorityQueue<TaleWorlds.Core.ItemObject, (int, int)>;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class EveryoneCampaignBehavior : CampaignBehaviorBase {
		public static Dictionary<MBGUID, Dictionary<ItemObject, int>> PartyArmories = new();

		private static Data data = new();

		public static readonly Dictionary<ItemObject.ItemTypeEnum, Func<int, int>> EquipmentAndThresholds = new() {
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
			var itemTypes = new[] {
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
										   ?.Where(item => item != null && item.ItemType == itemType)
										   ?.OrderBy(item => item.Tier)
										   ?.ThenBy(item => item.Value)
										   .ToList();

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
			InitializeItemListByType();
		}

		public override void SyncData(IDataStore dataStore) {
			if (dataStore.IsSaving) {
				data.PartyArmories = ConvertToUIntGuidDict(PartyArmories);
				_                  = dataStore.SyncDataAsJson("DynamicTroopPartyArmories", ref data);
				if (data != null) {
					Global.Debug($"saved {PartyArmories.Count} entries");
					data.PartyArmories.Clear();
				}
				else { Global.Error("Save Error"); }
			}
			else if (dataStore.IsLoading) {
				data.PartyArmories.Clear();
				_ = dataStore.SyncDataAsJson("DynamicTroopPartyArmories", ref data);
				if (data != null) {
					PartyArmories = ConvertToGuidDict(data.PartyArmories);
					data.PartyArmories.Clear();
					Global.Debug($"loaded {PartyArmories.Count} entries");
				}
				else { Global.Error("Load Error"); }
			}
		}


		public void WeeklyTick() {
			//GarbageCollectParties();
			//GarbageCollectEquipments();
		}

		public void GarbageCollectParties() {
			var keysToRemove = PartyArmories.Keys.Where(id => {
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
											.ToList();

			foreach (var key in keysToRemove) PartyArmories.Remove(key);
			Global.Debug($"Garbage collected {keysToRemove.Count} parties");
		}

		//public void GarbageCollectEquipments() { GarbageCollectArmors(); }

		public void GarbageCollectArmors(MobileParty mobileParty) {
			foreach (var equipmentAndThreshold in EquipmentAndThresholds)
				if (IsMobilePartyValid(mobileParty) && mobileParty.MemberRoster?.GetTroopRoster() != null) {
					var partyArmory = PartyArmories[mobileParty.Id];
					if (partyArmory == null) return;
					var memberCnt = mobileParty.MemberRoster.GetTroopRoster()
											   .Where(element => element.Character != null && !element.Character.IsHero)
											   .Sum(element => element.Number);
					var armorTotalCount = partyArmory.Where(kv => kv.Key.ItemType == equipmentAndThreshold.Key)
													 .Sum(kv => kv.Value);
					var surplusCount = armorTotalCount - equipmentAndThreshold.Value(memberCnt);
					if (surplusCount <= 0) continue;
					var surplusCountCpy = surplusCount;

					// 创建优先级队列
					var armorQueue = new ItemPriorityQueue(new ArmorComparer());
					foreach (var kv in partyArmory.Where(kv => kv.Key.ItemType == equipmentAndThreshold.Key))
						armorQueue.Enqueue(kv.Key, ((int)kv.Key.Tier, kv.Key.Value));

					// 移除多余的盔甲
					while (surplusCount > 0 && armorQueue.Count > 0) {
						var lowestArmor   = armorQueue.Dequeue();
						var countToRemove = Math.Min(partyArmory[lowestArmor.Key], surplusCount);
						partyArmory[lowestArmor.Key] -= countToRemove;
						surplusCount                 -= countToRemove;
						if (partyArmory[lowestArmor.Key] == 0) partyArmory.Remove(lowestArmor.Key);
					}

					Global.Debug($"Garbage collected {surplusCountCpy - surplusCount}x{equipmentAndThreshold.Key} armors from party {mobileParty.Name}");
				}
		}

		public void ReplenishBasicTroopEquipments(MobileParty mobileParty) {
			foreach (var typeKv in ItemListByType)
				if (IsMobilePartyValid(mobileParty) && mobileParty.MemberRoster?.GetTroopRoster() != null) {
					var partyArmory = PartyArmories[mobileParty.Id];
					int requiredNum;
					var memberCnt = mobileParty.MemberRoster.GetTroopRoster()
											   .Where(element => element.Character != null && !element.Character.IsHero)
											   .Sum(element => element.Number);
					var validRoster = mobileParty.MemberRoster.GetTroopRoster()
												 .Where(element => element.Character != null && !element.Character.IsHero);

					switch (typeKv.Key) {
						case ItemObject.ItemTypeEnum.Horse:
						case ItemObject.ItemTypeEnum.HorseHarness:
							requiredNum = validRoster.Where(element => element.Character.IsMounted)
													 .Sum(element => element.Number);
							break;
						case ItemObject.ItemTypeEnum.Bow:
						case ItemObject.ItemTypeEnum.Crossbow:
						case ItemObject.ItemTypeEnum.OneHandedWeapon:
						case ItemObject.ItemTypeEnum.TwoHandedWeapon:
						case ItemObject.ItemTypeEnum.Polearm:
							requiredNum = validRoster.Where(element => element.Character.IsMounted)
													 .Sum(element => element.Number *
																	 Global.CountCharacterEquipmentItemTypes(element
																			 .Character,
																		 typeKv.Key));
							break;
						default:
							requiredNum = memberCnt;
							break;
					}

					var armorTotalCount = partyArmory.Where(kv => kv.Key.ItemType == typeKv.Key).Sum(kv => kv.Value);
					var ownerCulture    = mobileParty.Owner?.Culture;
					var leaderCulture   = mobileParty.LeaderHero?.Culture;
					if (ownerCulture == null && leaderCulture == null) continue;
					if (armorTotalCount < requiredNum) {
						if (typeKv.Key == ItemObject.ItemTypeEnum.OneHandedWeapon ||
							typeKv.Key == ItemObject.ItemTypeEnum.TwoHandedWeapon ||
							typeKv.Key == ItemObject.ItemTypeEnum.Polearm) {
							var craftedWeapons = Global.CreateRandomCraftedItemsByItemType(typeKv.Key,
								ownerCulture ?? leaderCulture,
								Math.Max(requiredNum, memberCnt));
							foreach (var weapon in craftedWeapons)
								if (weapon != null) {
									AddItemToPartyArmory(mobileParty.Id, weapon, 1);
									Global.Debug($"Replenished 1x{weapon.Name} weapon for party {mobileParty.Name}");
								}
						}
						else {
							var itemToAdd = ItemListByType[typeKv.Key]
											?.Where(item => item != null &&
															(item.Culture == null                                    ||
															 (ownerCulture  != null && item.Culture == ownerCulture) ||
															 (leaderCulture != null && item.Culture == leaderCulture)))
											.FirstOrDefault();
							if (itemToAdd != null) {
								AddItemToPartyArmory(mobileParty.Id,
													 itemToAdd,
													 EquipmentAndThresholds[typeKv.Key](requiredNum) - armorTotalCount);
								Global.Debug($"Replenished {EquipmentAndThresholds[typeKv.Key](requiredNum) - armorTotalCount}x{itemToAdd.Name} armors for party {mobileParty.Name}");
							}
						}
					}
				}
		}


		public void DailyTickParty(MobileParty mobileParty) {
			if (IsMobilePartyValid(mobileParty)) {
				AllocateRandomEquipmentToPartyArmory(mobileParty);
				if (CampaignTime.Now.GetDayOfWeek == mobileParty.Id.InternalValue % 7) {
					GarbageCollectArmors(mobileParty);
					ReplenishBasicTroopEquipments(mobileParty);
				}
			}
		}

		public void AllocateRandomEquipmentToPartyArmory(MobileParty mobileParty) {
			if (mobileParty.MemberRoster == null ||
				//mobileParty.MemberRoster.Count            <= 1    ||
				mobileParty.MemberRoster.GetTroopRoster() == null ||
				mobileParty.MemberRoster.GetTroopRoster().IsEmpty())
				return;

			var factor = Global.CalculateClanProsperityFactor(mobileParty);
			for (var i = 0; i < factor; i++) {
				var randomMember = mobileParty.MemberRoster.GetTroopRoster().GetRandomElement();
				if (randomMember.Character == null                  ||
					randomMember.Character.IsHero                   ||
					randomMember.Character.BattleEquipments == null ||
					randomMember.Character.BattleEquipments.IsEmpty())
					return;
				var randomEquipment = randomMember.Character.BattleEquipments.GetRandomElementInefficiently();
				if (randomEquipment == null || randomEquipment.IsEmpty() || !randomEquipment.IsValid) return;
				var randomElement = randomEquipment.GetEquipmentFromSlot(Global.ArmourSlots.GetRandomElement());
				if (randomElement.IsEmpty || randomElement.Item == null) return;
				AddItemToPartyArmory(mobileParty.Id, randomElement.Item, 1);
				Global.Debug($"random armor {randomElement.Item.Name} added to {mobileParty.Name}");
				var weapon = Crafting.CreateRandomCraftedItem(mobileParty.LeaderHero.Culture);
				if (weapon != null) {
					AddItemToPartyArmory(mobileParty.Id, weapon, 1);
					Global.Debug($"random weapon {weapon.Name} added to {mobileParty.Name}");
				}
			}
		}

		public void OnMobilePartyCreated(MobileParty mobileParty) {
			if (IsMobilePartyValid(mobileParty)) {
				if (!PartyArmories.TryGetValue(mobileParty.Id, out var itemDict)) {
					itemDict                      = new Dictionary<ItemObject, int>();
					PartyArmories[mobileParty.Id] = itemDict;
				}

				var list = GetItemsFromParty(mobileParty);
				foreach (var element in list) {
					if (!itemDict.TryGetValue(element.Item, out var count)) count = 0;

					itemDict[element.Item] = count + 1;
				}

				Global.Log($"Mobile party {mobileParty.Name} created, {list.Count} start equipment added",
						   Colors.Green,
						   Level.Debug);
			}
		}

		public void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase partyBase) {
			if (mobileParty != null && mobileParty.Id != null) PartyArmories.Remove(mobileParty.Id);
			if (IsMobilePartyValid(mobileParty))
				if (mobileParty.Name != null && partyBase != null && partyBase.Name != null)
					Global.Log($"Mobile party {mobileParty.Name} destroyed, partyBase = {partyBase.Name}",
							   Colors.Green,
							   Level.Debug);
		}

		public void OnMapEventEnded(MapEvent mapEvent) {
			Global.Log($"Map Event ended with state {mapEvent.BattleState}", Colors.Green, Level.Debug);
			if (mapEvent.BattleState is BattleState.AttackerVictory or BattleState.DefenderVictory) {
				var validWinnerParties = FilterValidParties(mapEvent.BattleState == BattleState.AttackerVictory
																? mapEvent.AttackerSide.Parties
																: mapEvent.DefenderSide.Parties);
				var validLoserParties = FilterValidParties(mapEvent.BattleState == BattleState.AttackerVictory
															   ? mapEvent.DefenderSide.Parties
															   : mapEvent.AttackerSide.Parties);

				LogParties(validWinnerParties, "Winning");
				LogParties(validLoserParties,  "Defeated");

				var totalWinnerStrength = CalculateTotalStrength(validWinnerParties);
				DistributeLootRandomly(validWinnerParties, totalWinnerStrength, validLoserParties);
			}
		}

		private List<MapEventParty> FilterValidParties(IEnumerable<MapEventParty> parties) {
			return parties.Where(IsMapEventPartyValid).ToList();
		}

		private void LogParties(IEnumerable<MapEventParty> parties, string label) {
			foreach (var party in parties)
				Global.Log($"{label} party: {party.Party.Name}#{party.Party.MobileParty.Id}", Colors.Green, Level.Debug);
		}

		private float CalculateTotalStrength(IEnumerable<MapEventParty> parties) {
			return parties.Sum(party => party.Party.TotalStrength);
		}

		private void DistributeLootRandomly(IEnumerable<MapEventParty> winnerParties,
											float                      totalWinnerStrength,
											IEnumerable<MapEventParty> loserParties) {
			Random                  random             = new();
			var                     lootItemsWithCount = GetAllLootItems(loserParties);
			Dictionary<MBGUID, int> partyLootCount     = new(); // 用于统计每个party获得的物品数量

			foreach (var lootItem in lootItemsWithCount) {
				var item      = lootItem.Key;
				var itemCount = lootItem.Value;

				for (var i = 0; i < itemCount; i++) {
					var chosenParty = ChoosePartyRandomly(winnerParties, totalWinnerStrength, random);
					if (chosenParty != null) {
						AddItemToPartyArmory(chosenParty.Id, item, 1);

						// 更新统计信息
						if (partyLootCount.ContainsKey(chosenParty.Id))
							partyLootCount[chosenParty.Id]++;
						else
							partyLootCount[chosenParty.Id] = 1;
					}
				}
			}

			// 记录每个party获得的物品总数
			foreach (var partyCount in partyLootCount)
				Global.Debug($"Party {partyCount.Key} looted {partyCount.Value} items");
		}

		private Dictionary<ItemObject, int> GetAllLootItems(IEnumerable<MapEventParty> parties) {
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

		private MobileParty? ChoosePartyRandomly(IEnumerable<MapEventParty> parties, float totalStrength, Random random) {
			var randomValue        = random.NextDouble() * totalStrength;
			var cumulativeStrength = 0f;

			foreach (var party in parties) {
				cumulativeStrength += party.Party.TotalStrength;
				if (cumulativeStrength >= randomValue) return party.Party.MobileParty;
			}

			return null;
		}

		public void OnTroopRecruited(Hero            recruiterHero,
									 Settlement      recruitmentSettlement,
									 Hero            recruitmentSource,
									 CharacterObject troop,
									 int             amount) {
			if (recruiterHero != null) {
				var party = recruiterHero.PartyBelongedTo;
				if (IsMobilePartyValid(party)) {
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
			}
		}

		private List<EquipmentElement> GetItemsFromParty(MobileParty party) {
			List<EquipmentElement> listToReturn = new();
			foreach (var element in party.MemberRoster.GetTroopRoster())
				if (element.Character != null && !element.Character.IsHero) {
					var list = RecruitmentPatch.GetAllRecruitEquipments(element.Character);
					for (var i = 0; i < element.Number; i++) listToReturn.AddRange(list);
				}

			return listToReturn;
		}

		private void AddItemToPartyArmory(MBGUID partyId, ItemObject item, int count) {
			if (!PartyArmories.TryGetValue(partyId, out var inventory)) {
				inventory              = new Dictionary<ItemObject, int>();
				PartyArmories[partyId] = inventory;
			}

			if (!inventory.TryGetValue(item, out var currentCount)) currentCount = 0;

			inventory[item] = currentCount + count;
		}

		private bool IsMapEventPartyValid(MapEventParty? party) {
			return party != null && party.Party != null && IsMobilePartyValid(party.Party.MobileParty);
		}

		public static bool IsMobilePartyValid(MobileParty? party) {
			return party    != null &&
				   party.Id != null &&
				   (PartyArmories.ContainsKey(party.Id) ||
					(party.Owner                 != null      &&
					 party.Owner.CharacterObject != null      &&
					 party.Owner.CharacterObject.IsHero       &&
					 party.Owner.IsPartyLeader                &&
					 party.LeaderHero                 != null &&
					 party.LeaderHero.CharacterObject != null &&
					 party.LeaderHero.CharacterObject.IsHero  &&
					 party.LeaderHero.IsPartyLeader           &&
					 party.MemberRoster != null               &&
					 !party.Owner.IsHumanPlayerCharacter));
		}

		private bool IsPartyUndestroyed(PartyBase? party) {
			// 根据游戏逻辑判断部队是否未被摧毁，例如检查其兵力数量等
			return party             != null                            &&
				   party.MobileParty != null                            &&
				   party.MobileParty.IsActive                           &&
				   party.MobileParty.MemberRoster               != null &&
				   party.MobileParty.MemberRoster.TotalManCount > 0;
		}

		private bool IsPlayerInvolved(MapEvent? mapEvent) {
			return mapEvent != null &&
				   ((mapEvent.AttackerSide != null &&
					 mapEvent.AttackerSide.Parties.Any(party => party.Party.LeaderHero == Hero.MainHero)) ||
					(mapEvent.DefenderSide != null &&
					 mapEvent.DefenderSide.Parties.Any(party => party.Party.LeaderHero == Hero.MainHero)));
		}

		public Dictionary<uint, Dictionary<string, int>> ConvertToUIntGuidDict(
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

		public Dictionary<MBGUID, Dictionary<ItemObject, int>> ConvertToGuidDict(
			Dictionary<uint, Dictionary<string, int>> uintDict) {
			Dictionary<MBGUID, Dictionary<ItemObject, int>> guidDict = new();
			foreach (var pair in uintDict) {
				if (!guidDict.TryGetValue(new MBGUID(pair.Key), out var innerDict)) {
					innerDict = new Dictionary<ItemObject, int>();
					guidDict.Add(new MBGUID(pair.Key), innerDict);
				}

				foreach (var innerPair in pair.Value) {
					var itemObject = MBObjectManager.Instance.GetObject<ItemObject>(innerPair.Key);
					if (itemObject != null) {
						if (innerDict.ContainsKey(itemObject))
							innerDict[itemObject] += innerPair.Value;
						else
							innerDict.Add(itemObject, innerPair.Value);
					}
				}
			}

			return guidDict;
		}

		public class ArmorComparer : IComparer<ItemObject> {
			public int Compare(ItemObject x, ItemObject y) {
				var tierComparison = x.Tier.CompareTo(y.Tier);
				if (tierComparison != 0) return tierComparison;
				return x.Value.CompareTo(y.Value);
			}
		}

		[Serializable]
		private class Data {
			[SaveableField(2)] public Dictionary<uint, Dictionary<string, int>> PartyArmories = new();
		}
	}