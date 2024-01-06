#region

	using System.Collections.Generic;
	using System.Linq;
	using log4net.Core;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.CampaignSystem.MapEvents;
	using TaleWorlds.CampaignSystem.Party;
	using TaleWorlds.CampaignSystem.Settlements;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.ObjectSystem;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class EveryoneCampaignBehavior : CampaignBehaviorBase {
		public static readonly Dictionary<MBGUID, Dictionary<ItemObject, int>> PartyArmories = new();

		public override void RegisterEvents() {
			CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
			CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
			CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
			CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
		}

		public override void SyncData(IDataStore dataStore) { }

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
			if (IsMobilePartyValid(mobileParty)) {
				_ = PartyArmories.Remove(mobileParty.Id);
				Global.Log($"Mobile party {mobileParty.Name} destroyed, partyBase = {partyBase.Name}",
						   Colors.Green,
						   Level.Debug);
			}
		}

		public void OnMapEventEnded(MapEvent mapEvent) {
			Global.Log($"Map Event ended with state {mapEvent.BattleState}", Colors.Green, Level.Debug);
			if (mapEvent.BattleState is BattleState.AttackerVictory or BattleState.DefenderVictory) {
				var winner = mapEvent.BattleState == BattleState.AttackerVictory
								 ? mapEvent.AttackerSide
								 : mapEvent.DefenderSide;
				var loser = mapEvent.BattleState == BattleState.AttackerVictory
								? mapEvent.DefenderSide
								: mapEvent.AttackerSide;

				// 记录胜方部队
				foreach (var party in winner.Parties)
					if (IsMapEventPartyValid(party))
						Global.Log($"Winning party: {party.Party.Name}#{party.Party.MobileParty.Id}",
								   Colors.Green,
								   Level.Debug);

				// 记录败方部队
				foreach (var party in loser.Parties)
					if (IsMapEventPartyValid(party))
						Global.Log($"Defeated party: {party.Party.Name}#{party.Party.MobileParty.Id}",
								   Colors.Green,
								   Level.Debug);

				var totalLoserValue     = CalculateTotalValue(loser.Parties);
				var totalWinnerStrength = CalculateTotalStrength(winner.Parties);

				var winnerLootShares = AllocateLootShares(winner.Parties, totalWinnerStrength, totalLoserValue);

				DistributeLoot(winnerLootShares, loser.Parties);
			}
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
			}
		}

		private List<EquipmentElement> GetItemsFromParty(MobileParty party) {
			List<EquipmentElement> listToReturn = new();
			foreach (var element in party.MemberRoster.GetTroopRoster())
				if (element.Character != null && !element.Character.IsHero) {
					var list = RecruitmentPatch.GetRecruitEquipments(element.Character);
					for (var i = 0; i < element.Number; i++) listToReturn.AddRange(list);
				}

			return listToReturn;
		}

		private int CalculateTotalValue(MBReadOnlyList<MapEventParty> parties) {
			if (parties == null) return 0;

			var totalValue = 0;

			// 确定未被摧毁的部队
			HashSet<MBGUID> undestroyedPartyIds =
				new(parties.Where(party => IsMapEventPartyValid(party) && IsPartyUndestroyed(party.Party))
						   .Select(party => party.Party.MobileParty.Id));

			foreach (var party in parties)
				if (IsMapEventPartyValid(party) && !undestroyedPartyIds.Contains(party.Party.MobileParty.Id))
					if (PartyArmories.TryGetValue(party.Party.MobileParty.Id, out var inventory))
						foreach (var item in inventory)
							if (item.Key != null)
								totalValue += item.Key.Value * item.Value; // item.Key是ItemObject，item.Value是数量

			return totalValue;
		}

		private float CalculateTotalStrength(MBReadOnlyList<MapEventParty> parties) {
			if (parties == null) return 0;

			float totalStrength = 0;
			foreach (var party in parties)
				if (IsMapEventPartyValid(party))
					totalStrength += party.Party.TotalStrength;

			return totalStrength;
		}

		private Dictionary<MBGUID, float> AllocateLootShares(MBReadOnlyList<MapEventParty> parties,
															 float                         totalStrength,
															 int                           totalValue) {
			Dictionary<MBGUID, float> shares = new();
			foreach (var party in parties)
				if (IsMapEventPartyValid(party)) {
					var share = party.Party.TotalStrength * totalValue / totalStrength; // 分配的份额
					shares[party.Party.MobileParty.Id] = share;
				}

			return shares;
		}

		private void DistributeLoot(Dictionary<MBGUID, float> lootShares, MBReadOnlyList<MapEventParty> loserParties) {
			Dictionary<MBGUID, Dictionary<ItemObject, int>> tempArmories = new(PartyArmories);

			// 获取战败但未被摧毁的部队的ID
			HashSet<MBGUID> undestroyedLoserPartyIds =
				new(loserParties.Where(party => IsMapEventPartyValid(party) && IsPartyUndestroyed(party.Party))
								.Select(party => party.Party.MobileParty.Id));

			foreach (var share in lootShares) {
				var partyId          = share.Key;
				var lootValueTarget  = share.Value;
				var currentLootValue = 0f;

				if (!PartyArmories.ContainsKey(partyId)) PartyArmories[partyId] = new Dictionary<ItemObject, int>();

				foreach (var loserPartyId in tempArmories.Keys) {
					// 跳过未被摧毁的败方部队
					if (undestroyedLoserPartyIds.Contains(loserPartyId)) continue;

					var loserInventory = tempArmories[loserPartyId];

					foreach (var item in loserInventory.ToList()) {
						var itemValue = item.Key.Value;
						var itemCount = item.Value;

						if (currentLootValue + itemValue <= lootValueTarget && itemCount > 0) {
							AddItemToPartyArmory(partyId, item.Key, 1);
							RemoveItemFromTempArmory(tempArmories, loserPartyId, item.Key, 1);

							currentLootValue += itemValue;

							if (currentLootValue >= lootValueTarget) break;
						}
					}

					if (currentLootValue >= lootValueTarget) break;
				}

				// 记录分配给该party的总价值
				Global.Log($"Party {partyId} looted total value of {currentLootValue}", Colors.Green, Level.Debug);
			}
		}

		private void AddItemToPartyArmory(MBGUID partyId, ItemObject item, int count) {
			if (!PartyArmories.TryGetValue(partyId, out var inventory)) {
				inventory              = new Dictionary<ItemObject, int>();
				PartyArmories[partyId] = inventory;
			}

			if (!inventory.TryGetValue(item, out var currentCount)) currentCount = 0;

			inventory[item] = currentCount + count;
		}

		private void RemoveItemFromTempArmory(Dictionary<MBGUID, Dictionary<ItemObject, int>> tempArmories,
											  MBGUID                                          partyId,
											  ItemObject                                      item,
											  int                                             count) {
			if (tempArmories.TryGetValue(partyId, out var inventory))
				if (inventory.TryGetValue(item, out var currentCount)) {
					var newCount = currentCount - count;
					if (newCount <= 0)
						_ = inventory.Remove(item);
					else
						inventory[item] = newCount;
				}
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
	}