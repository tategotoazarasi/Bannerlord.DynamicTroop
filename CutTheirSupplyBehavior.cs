#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bannerlord.ButterLib.SaveSystem.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

#endregion

namespace DynamicTroopEquipmentReupload;

public sealed class CutTheirSupplyBehavior : CampaignBehaviorBase {
	// Daily roll per AI town (player towns excluded).
	// Chance: 2000 => 1%, then +0.001% per 1 prosperity, capped at 12.5% daily.
	private const float MIN_PROSPERITY = 2000f;
	private const float BASE_CHANCE_AT_MIN_PROSPERITY = 0.01f;
	private const float CHANCE_PER_PROSPERITY_POINT = 0.000001f;
	private const float MAX_DAILY_CHANCE = 0.125f;

	private const int TARGET_PARTY_SIZE = 100;
	private const int GEAR_COUNT = 26;

	private const int GEAR_TIER_1_CAP = 4;
	private const float GEAR_TIER_1_CHANCE = 0.85f;

	private const int GEAR_TIER_2_CAP = 5;
	private const float GEAR_TIER_2_CHANCE = 0.75f;

	private const int GEAR_TIER_3_CAP = 8;
	private const float GEAR_TIER_3_CHANCE = 0.55f;

	private const int GEAR_TIER_4_CAP = 4;
	private const float GEAR_TIER_4_CHANCE = 0.45f;

	private const int GEAR_TIER_5_CAP = 2;
	private const float GEAR_TIER_5_CHANCE = 0.35f;

	private const int GEAR_TIER_6_CAP = 1;
	private const float GEAR_TIER_6_CHANCE = 0.25f;


	private const int EXTRA_TROOP_COUNT = 25;
	private const int FOOD_PER_TYPE_COUNT = 17;
	private const int SUMPTER_HORSE_COUNT = 17;
	private const string SUMPTER_HORSE_ITEM_ID = "sumpter_horse";

	private const float ESCORT_REFRESH_SECONDS = 0.25f;

	private const float ARRIVAL_DISTANCE_SQUARED = 4f; // distance ~2
	private const float WAIT_DISTANCE = 2f;
	private const float WAIT_DISTANCE_SQUARED = WAIT_DISTANCE * WAIT_DISTANCE;

	private const float THREAT_SCAN_RADIUS = 10f;
	private const float THREAT_SCAN_RADIUS_SQUARED = THREAT_SCAN_RADIUS * THREAT_SCAN_RADIUS;
	private const float FLEE_POINT_DISTANCE = 8f;
	private const float FLEE_IF_ENEMY_STRENGTH_MULTIPLIER = 1.20f;

	private static readonly MethodInfo? RemovePartyMethod =
		typeof(MobileParty).GetMethod("RemoveParty", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly HashSet<MBGUID> ReinforcementCaravanIds = new();

	// 1 per town
	private readonly Dictionary<MBGUID, MBGUID> _activeCaravanBySourceSettlementId = new();
	private readonly Dictionary<MBGUID, MBGUID> _ownerClanByCaravanId = new();


	// For no target
	private readonly HashSet<MBGUID> _returningCaravanIds = new();

	private readonly Dictionary<MBGUID, MBGUID> _sourceSettlementByCaravanId = new();

	private readonly Dictionary<MBGUID, MBGUID> _targetByCaravanId = new();


	private Data _data = new();
	private float _escortRefreshTimer;

	public static bool IsReinforcementCaravanParty(MobileParty? mobileParty) {
		return mobileParty != null && ReinforcementCaravanIds.Contains(mobileParty.Id);
	}

	public override void RegisterEvents() {
		CampaignEvents.DailyTickTownEvent.AddNonSerializedListener(this, OnDailyTickTown);
		CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
		CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
	}

	public override void SyncData(IDataStore dataStore) {
		if (dataStore.IsSaving) {
			_data.CaravanTargets.Clear();
			_data.CaravanSources.Clear();
			_data.CaravanOwnerClans.Clear();
			_data.ReturningCaravans.Clear();

			foreach (var entry in _targetByCaravanId) { _data.CaravanTargets[entry.Key.InternalValue] = entry.Value.InternalValue; }

			foreach (var entry in _sourceSettlementByCaravanId) { _data.CaravanSources[entry.Key.InternalValue] = entry.Value.InternalValue; }

			foreach (var entry in _ownerClanByCaravanId) { _data.CaravanOwnerClans[entry.Key.InternalValue] = entry.Value.InternalValue; }

			foreach (var caravanId in _returningCaravanIds) { _data.ReturningCaravans.Add(caravanId.InternalValue); }


			var tempData = _data;
			dataStore.SyncDataAsJson("DynamicTroopCutTheirSupply", ref tempData);
		}
		else if (dataStore.IsLoading) {
			var tempData = _data;

			if (dataStore.SyncDataAsJson("DynamicTroopCutTheirSupply", ref tempData) && tempData != null) {
				_data = tempData;

				_targetByCaravanId.Clear();
				foreach (var entry in _data.CaravanTargets) { _targetByCaravanId[new MBGUID(entry.Key)] = new MBGUID(entry.Value); }

				_sourceSettlementByCaravanId.Clear();
				foreach (var entry in _data.CaravanSources) { _sourceSettlementByCaravanId[new MBGUID(entry.Key)] = new MBGUID(entry.Value); }

				_ownerClanByCaravanId.Clear();
				foreach (var entry in _data.CaravanOwnerClans) { _ownerClanByCaravanId[new MBGUID(entry.Key)] = new MBGUID(entry.Value); }

				_returningCaravanIds.Clear();
				foreach (var caravanInternalId in _data.ReturningCaravans) { _returningCaravanIds.Add(new MBGUID(caravanInternalId)); }

				_activeCaravanBySourceSettlementId.Clear();
				ReinforcementCaravanIds.Clear();
				foreach (var caravanId in _targetByCaravanId.Keys) { ReinforcementCaravanIds.Add(caravanId); }

				foreach (var entry in _sourceSettlementByCaravanId) { _activeCaravanBySourceSettlementId[entry.Value] = entry.Key; }
			}
		}
	}

	private static bool ReinforcementCaravanBuyProductsDisabledOnCondition() {
		return IsReinforcementCaravanParty(MobileParty.ConversationParty);
	}

	private static bool ReinforcementCaravanBuyProductsDisabledOnClickableCondition(out TextObject explanation) {
		explanation = LocalizedTexts.ReinforcementCaravanTradeDisabled;
		return false;
	}


	private void OnDailyTickTown(Town town) {
		var ownerClan = town.OwnerClan;

		var sourceSettlement = town.Settlement;
		if (sourceSettlement == null)
			return;

		if (_activeCaravanBySourceSettlementId.TryGetValue(sourceSettlement.Id, out var existingCaravanId)) {
			var existingCaravan = FindMobilePartyById(existingCaravanId);
			if (existingCaravan != null && existingCaravan.IsActive)
				return;

			// Keep it clean
			_activeCaravanBySourceSettlementId.Remove(sourceSettlement.Id);
		}


		if (ownerClan == null || ownerClan == Clan.PlayerClan)
			return;

		var prosperity = town.Prosperity;

		if (prosperity < MIN_PROSPERITY)
			return;

		var chance = GetDailySpawnChance(prosperity);

		if (MBRandom.RandomFloat > chance)
			return;

		var targetParty = FindTargetLordParty(ownerClan);

		if (targetParty == null)
			return;

		SpawnReinforcementCaravan(town, ownerClan, targetParty);
	}


	private void OnTick(float dt) {
		_escortRefreshTimer += dt;
		if (_escortRefreshTimer < ESCORT_REFRESH_SECONDS) { return; }

		_escortRefreshTimer = 0f;

		// take a snapshot to avoid modifying the dictionary during iteration
		foreach (var pair in _targetByCaravanId.ToList()) {
			var caravanParty = FindMobilePartyById(pair.Key);
			if (caravanParty == null || !caravanParty.IsActive) {
				_targetByCaravanId.Remove(pair.Key);
				ReinforcementCaravanIds.Remove(pair.Key);
				continue;
			}

			if (TryRunAwayFromNearbyThreat(caravanParty)) { continue; }

			var targetParty = FindMobilePartyById(pair.Value);


			// guarantee target
			if (targetParty == null || !targetParty.IsActive) {
				if (_ownerClanByCaravanId.TryGetValue(caravanParty.Id, out var ownerClanId)) {
					var ownerClan = FindClanById(ownerClanId);
					if (ownerClan != null) {
						var newTarget = FindTargetLordParty(ownerClan, pair.Value);
						if (newTarget != null) {
							_targetByCaravanId[caravanParty.Id] = newTarget.Id;
							targetParty                         = newTarget;
						}
						else { _returningCaravanIds.Add(caravanParty.Id); }
					}
					else { _returningCaravanIds.Add(caravanParty.Id); }
				}
				else { _returningCaravanIds.Add(caravanParty.Id); }
			}

			// return and despawn
			if (_returningCaravanIds.Contains(caravanParty.Id)) {
				if (_sourceSettlementByCaravanId.TryGetValue(caravanParty.Id, out var sourceSettlementId)) {
					var sourceSettlement = FindSettlementById(sourceSettlementId);
					if (sourceSettlement != null) {
						// check twice
						if (caravanParty.CurrentSettlement != null && caravanParty.CurrentSettlement != sourceSettlement) { LeaveSettlementAction.ApplyForParty(caravanParty); }

						caravanParty.Ai.SetDoNotMakeNewDecisions(true);

						caravanParty.SetMoveGoToSettlement(sourceSettlement, MobileParty.NavigationType.All, false);
						caravanParty.RecalculateShortTermBehavior();

						if (caravanParty.CurrentSettlement == sourceSettlement) { CleanupCaravan(caravanParty); }
					}
					else { CleanupCaravan(caravanParty); }
				}
				else { CleanupCaravan(caravanParty); }

				continue;
			}

			// when targetparty is ensured
			if (targetParty == null) {
				_returningCaravanIds.Add(caravanParty.Id);
				continue;
			}

			var stashReceiverParty = targetParty;

			var movementTargetParty = targetParty;
			if (movementTargetParty.Army != null && movementTargetParty.Army.LeaderParty != null) { movementTargetParty = movementTargetParty.Army.LeaderParty; }

			// if they're inside a settlement
			var targetSettlement = movementTargetParty.CurrentSettlement;
			if (targetSettlement != null) {
				if (caravanParty.CurrentSettlement != null && caravanParty.CurrentSettlement != targetSettlement) { LeaveSettlementAction.ApplyForParty(caravanParty); }

				caravanParty.Ai.SetDoNotMakeNewDecisions(true);


				caravanParty.SetMoveGoToSettlement(targetSettlement, MobileParty.NavigationType.All, false);
				caravanParty.RecalculateShortTermBehavior();

				if (caravanParty.CurrentSettlement == targetSettlement) {
					TransferCargoToTargetStash(caravanParty, stashReceiverParty);
					CleanupCaravan(caravanParty);
				}

				continue;
			}

			// for sieges/battles, keep the distance
			if (movementTargetParty.MapEvent != null || movementTargetParty.SiegeEvent != null) {
				if (caravanParty.CurrentSettlement != null) { LeaveSettlementAction.ApplyForParty(caravanParty); }

				caravanParty.Ai.SetDoNotMakeNewDecisions(true);

				var delta = caravanParty.GetPosition2D - movementTargetParty.GetPosition2D;
				if (delta.LengthSquared <= WAIT_DISTANCE_SQUARED) { caravanParty.SetMoveModeHold(); }
				else {
					var waitPoint = GetWaitPointNear(caravanParty, movementTargetParty);
					caravanParty.SetMoveGoToPoint(waitPoint, MobileParty.NavigationType.Default);
				}

				caravanParty.RecalculateShortTermBehavior();
				continue;
			}


			// regularly
			if (caravanParty.CurrentSettlement != null) { LeaveSettlementAction.ApplyForParty(caravanParty); }

			caravanParty.Ai.SetDoNotMakeNewDecisions(true);

			var destination = new CampaignVec2(movementTargetParty.Position.ToVec2(), movementTargetParty.Position.IsOnLand);
			caravanParty.SetMoveGoToPoint(destination, MobileParty.NavigationType.Default);
			caravanParty.RecalculateShortTermBehavior();

			if (HasArrived(caravanParty, movementTargetParty)) {
				TransferCargoToTargetStash(caravanParty, stashReceiverParty);
				CleanupCaravan(caravanParty);
			}
		}
	}

	private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty) {
		ReinforcementCaravanIds.Remove(mobileParty.Id);
		_targetByCaravanId.Remove(mobileParty.Id);
		_returningCaravanIds.Remove(mobileParty.Id);

		if (_sourceSettlementByCaravanId.TryGetValue(mobileParty.Id, out var sourceSettlementId)) {
			_sourceSettlementByCaravanId.Remove(mobileParty.Id);

			if (_activeCaravanBySourceSettlementId.TryGetValue(sourceSettlementId, out var activeCaravanId) && activeCaravanId == mobileParty.Id) { _activeCaravanBySourceSettlementId.Remove(sourceSettlementId); }
		}

		_ownerClanByCaravanId.Remove(mobileParty.Id);
	}


	private static float GetDailySpawnChance(float prosperity) {
		var extraProsperity = prosperity                    - MIN_PROSPERITY;
		var chance          = BASE_CHANCE_AT_MIN_PROSPERITY + extraProsperity * CHANCE_PER_PROSPERITY_POINT;
		return Math.Min(chance, MAX_DAILY_CHANCE);
	}

	private static MobileParty? FindTargetLordParty(Clan clan) {
		MobileParty? bestParty    = null;
		var          bestDistance = int.MaxValue;

		foreach (var party in Campaign.Current.MobileParties) {
			if (party == null || !party.IsActive)
				continue;

			var leaderHero = party.LeaderHero;
			if (leaderHero == null || leaderHero.Clan != clan)
				continue;

			// lords
			if (party.IsCaravan || party.IsVillager || party.IsMilitia || party.IsBandit)
				continue;

			var partySize = party.MemberRoster.TotalManCount;
			var distance  = Math.Abs(partySize - TARGET_PARTY_SIZE);

			if (distance < bestDistance) {
				bestDistance = distance;
				bestParty    = party;
			}
		}

		return bestParty;
	}


	private static MobileParty? FindMobilePartyById(MBGUID partyId) {
		var campaign = Campaign.Current;

		if (campaign == null)
			return null;

		foreach (var party in campaign.MobileParties) {
			if (party.Id == partyId)
				return party;
		}

		return null;
	}

	private static Clan? FindClanById(MBGUID clanId) {
		foreach (var clan in Campaign.Current.Clans) {
			if (clan.Id == clanId)
				return clan;
		}

		return null;
	}

	private static Settlement? FindSettlementById(MBGUID settlementId) {
		foreach (var settlement in Settlement.All) {
			if (settlement.Id == settlementId)
				return settlement;
		}

		return null;
	}

	private static MobileParty? FindTargetLordParty(Clan clan, MBGUID excludedPartyId) {
		MobileParty? bestParty    = null;
		var          bestDistance = int.MaxValue;

		foreach (var party in Campaign.Current.MobileParties) {
			if (party == null || !party.IsActive || party.Id == excludedPartyId)
				continue;

			var leaderHero = party.LeaderHero;
			if (leaderHero == null || leaderHero.Clan != clan)
				continue;

			if (party.IsCaravan || party.IsVillager || party.IsMilitia || party.IsBandit)
				continue;

			var partySize = party.MemberRoster.TotalManCount;
			var distance  = Math.Abs(partySize - TARGET_PARTY_SIZE);

			if (distance < bestDistance) {
				bestDistance = distance;
				bestParty    = party;
			}
		}

		return bestParty;
	}

	private static bool HasArrived(MobileParty caravan, MobileParty target) {
		if (caravan.CurrentSettlement != null && caravan.CurrentSettlement == target.CurrentSettlement)
			return true;

		var delta = caravan.GetPosition2D - target.GetPosition2D;
		return delta.LengthSquared <= ARRIVAL_DISTANCE_SQUARED;
	}


	private static CampaignVec2 GetWaitPointNear(MobileParty caravanParty, MobileParty movementTargetParty) {
		var targetPos  = movementTargetParty.Position.ToVec2();
		var caravanPos = caravanParty.Position.ToVec2();

		var away = caravanPos - targetPos;

		if (away.LengthSquared < 0.0001f) { away = new Vec2(1f, 0f); }
		else { away                              = away.Normalized(); }

		var waitPos = targetPos + away * WAIT_DISTANCE;
		return new CampaignVec2(waitPos, movementTargetParty.Position.IsOnLand);
	}

	// dm me for more info about this decision
	private static bool TryRunAwayFromNearbyThreat(MobileParty caravanParty) {
		MobileParty? nearestThreat                = null;
		var          nearestThreatDistanceSquared = float.MaxValue;

		foreach (var otherParty in Campaign.Current.MobileParties) {
			if (otherParty == null || !otherParty.IsActive)
				continue;

			if (otherParty == caravanParty)
				continue;

			if (!caravanParty.MapFaction.IsAtWarWith(otherParty.MapFaction))
				continue;

			var delta           = caravanParty.GetPosition2D - otherParty.GetPosition2D;
			var distanceSquared = delta.LengthSquared;

			if (distanceSquared > THREAT_SCAN_RADIUS_SQUARED)
				continue;

			if (distanceSquared < nearestThreatDistanceSquared) {
				nearestThreatDistanceSquared = distanceSquared;
				nearestThreat                = otherParty;
			}
		}

		if (nearestThreat == null)
			return false;

		var caravanManCount = caravanParty.MemberRoster.TotalManCount;
		var enemyManCount   = nearestThreat.MemberRoster.TotalManCount;

		if (enemyManCount <= caravanManCount * FLEE_IF_ENEMY_STRENGTH_MULTIPLIER)
			return false;


		var away = caravanParty.GetPosition2D - nearestThreat.GetPosition2D;
		if (away.LengthSquared < 0.0001f)
			away = new Vec2(1f, 0f);
		else
			away = away.Normalized();

		var fleePos = caravanParty.GetPosition2D + away * FLEE_POINT_DISTANCE;
		caravanParty.SetMoveGoToPoint(new CampaignVec2(fleePos, caravanParty.Position.IsOnLand), MobileParty.NavigationType.Default);
		caravanParty.RecalculateShortTermBehavior();

		return true;
	}

	private static void AddExtraCultureTroops(MobileParty caravanParty, BasicCultureObject culture, int count) {
		var cultureTroops = CharacterObject.All
										   .Where(t =>
													  t != null && !t.IsHero && t.Culture == culture && t.Occupation == Occupation.Soldier && t.Tier >= 1 && t.Tier <= 6)
										   .ToList();

		if (cultureTroops.Count == 0)
			return;

		var troopsByTier = cultureTroops
						   .GroupBy(t => t.Tier)
						   .ToDictionary(g => g.Key, g => g.ToList());

		for (var i = 0; i < count; i++) {
			var randomTier = MBRandom.RandomInt(1, 7); // 1..6
			if (!troopsByTier.TryGetValue(randomTier, out var tierPool) || tierPool.Count == 0)
				tierPool = cultureTroops;

			var troop = tierPool.GetRandomElement();
			caravanParty.MemberRoster.AddToCounts(troop, 1);
		}
	}

	private static void AddReinforcementSupplies(MobileParty caravanParty) {
		var allItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>();

		foreach (var item in allItems) {
			if (item != null && item.IsFood) { caravanParty.ItemRoster.AddToCounts(item, FOOD_PER_TYPE_COUNT); }
		}

		var sumpterHorse = MBObjectManager.Instance.GetObject<ItemObject>(SUMPTER_HORSE_ITEM_ID);
		if (sumpterHorse != null) { caravanParty.ItemRoster.AddToCounts(sumpterHorse, SUMPTER_HORSE_COUNT); }
	}


	private void SpawnReinforcementCaravan(Town sourceTown, Clan ownerClan, MobileParty targetParty) {
		var caravanOwner = ownerClan.Leader;

		if (caravanOwner == null)
			return;

		var spawnSettlement = sourceTown.Settlement;

		if (spawnSettlement == null)
			return;

		var culture = spawnSettlement.Culture;

		if (culture == null)
			return;

		var templateId     = "caravan_template_" + culture.StringId;
		var templateObject = MBObjectManager.Instance.GetObject<PartyTemplateObject>(templateId);

		if (templateObject == null)
			return;

		var caravanParty = CaravanPartyComponent.CreateCaravanParty(caravanOwner, spawnSettlement, templateObject);
		if (caravanParty == null)
			return;

		caravanParty.Party.SetCustomName(LocalizedTexts.ReinforcementCaravan);
		ReinforcementCaravanIds.Add(caravanParty.Id);
		caravanParty.Ai.SetDoNotMakeNewDecisions(true);

		// fix jitter
		LeaveSettlementAction.ApplyForParty(caravanParty);
		if (caravanParty == null)
			return;

		_sourceSettlementByCaravanId[caravanParty.Id]          = spawnSettlement.Id;
		_ownerClanByCaravanId[caravanParty.Id]                 = ownerClan.Id;
		_activeCaravanBySourceSettlementId[spawnSettlement.Id] = caravanParty.Id;


		var destination = new CampaignVec2(targetParty.Position.ToVec2(), targetParty.Position.IsOnLand);
		caravanParty.SetMoveGoToPoint(destination, MobileParty.NavigationType.Default);
		caravanParty.RecalculateShortTermBehavior();

		// +20 culture troop tree troop:
		AddExtraCultureTroops(caravanParty, culture, EXTRA_TROOP_COUNT);
		AddReinforcementSupplies(caravanParty);

		// +27 (?) gear:
		var gearItems = GetRandomGearItems(culture);


		foreach (var item in gearItems) {
			if (item != null)
				caravanParty.ItemRoster.AddToCounts(item, 1);
		}

		_targetByCaravanId[caravanParty.Id] = targetParty.Id;
	}

	private static List<ItemObject> GetRandomGearItems(BasicCultureObject culture) {
		var results = new List<ItemObject>(GEAR_COUNT);

		AddRandomGearItems(results, culture, 1, GEAR_TIER_1_CAP, GEAR_TIER_1_CHANCE);
		AddRandomGearItems(results, culture, 2, GEAR_TIER_2_CAP, GEAR_TIER_2_CHANCE);
		AddRandomGearItems(results, culture, 3, GEAR_TIER_3_CAP, GEAR_TIER_3_CHANCE);
		AddRandomGearItems(results, culture, 4, GEAR_TIER_4_CAP, GEAR_TIER_4_CHANCE);
		AddRandomGearItems(results, culture, 5, GEAR_TIER_5_CAP, GEAR_TIER_5_CHANCE);
		AddRandomGearItems(results, culture, 6, GEAR_TIER_6_CAP, GEAR_TIER_6_CHANCE);

		return results;
	}

	private static void AddRandomGearItems(List<ItemObject> results, BasicCultureObject culture, int tier, int cap, float chance) {
		var tierCandidates = Cache.GetItemsByTierAndCulture(tier, culture)
								  .Where(i => i != null && ItemBlackList.Test(i) && (int)i.Tier == tier)
								  .ToList();

		if (tierCandidates.Count == 0) {
			tierCandidates = Cache.GetItemsByTierAndCulture(tier, culture)
								  .Where(i => i != null && ItemBlackList.Test(i))
								  .ToList();
		}

		if (tierCandidates.Count == 0) { return; }

		for (var attemptIndex = 0; attemptIndex < cap && results.Count < GEAR_COUNT; attemptIndex++) {
			if (MBRandom.RandomFloat > chance) {
				break; // stop upon fail
			}

			results.Add(tierCandidates.GetRandomElement());
		}
	}


	private static void TransferCargoToTargetStash(MobileParty caravanParty, MobileParty targetParty) {
		if (!EveryoneCampaignBehavior.PartyArmories.TryGetValue(targetParty.Id, out var targetArmory)) {
			targetArmory = new Dictionary<ItemObject, int>();
			EveryoneCampaignBehavior.PartyArmories.Add(targetParty.Id, targetArmory);
		}

		var roster     = caravanParty.ItemRoster;
		var enumerator = roster.GetEnumerator();

		while (enumerator.MoveNext()) {
			var element = enumerator.Current;

			if (element is not { IsEmpty: false, EquipmentElement: { IsEmpty: false, Item: not null }, Amount: > 0 })
				continue;

			var item = element.EquipmentElement.Item;

			if (!targetArmory.TryGetValue(item, out var currentAmount))
				targetArmory.Add(item, element.Amount);
			else
				targetArmory[item] = currentAmount + element.Amount;
		}

		enumerator.Dispose();
	}

	private void CleanupCaravan(MobileParty caravanParty) {
		ReinforcementCaravanIds.Remove(caravanParty.Id);
		_targetByCaravanId.Remove(caravanParty.Id);
		_returningCaravanIds.Remove(caravanParty.Id);

		if (_sourceSettlementByCaravanId.TryGetValue(caravanParty.Id, out var sourceSettlementId)) {
			_sourceSettlementByCaravanId.Remove(caravanParty.Id);

			if (_activeCaravanBySourceSettlementId.TryGetValue(sourceSettlementId, out var activeCaravanId) && activeCaravanId == caravanParty.Id) { _activeCaravanBySourceSettlementId.Remove(sourceSettlementId); }
		}

		_ownerClanByCaravanId.Remove(caravanParty.Id);
		RemoveMobileParty(caravanParty);
	}


	private static void RemoveMobileParty(MobileParty mobileParty) {
		if (RemovePartyMethod != null) {
			RemovePartyMethod.Invoke(mobileParty, null);
			return;
		}

		DestroyPartyAction.Apply(null, mobileParty);
	}


	[Serializable]
	private sealed class Data {
		[SaveableField(3)] public Dictionary<uint, uint> CaravanOwnerClans = new();
		[SaveableField(2)] public Dictionary<uint, uint> CaravanSources = new();
		[SaveableField(1)] public Dictionary<uint, uint> CaravanTargets = new();
		[SaveableField(4)] public List<uint> ReturningCaravans = new();
	}
}