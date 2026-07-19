#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DynamicTroopEquipmentReupload.Comparers;
using DynamicTroopEquipmentReupload.Extensions;
using log4net.Core;
using Newtonsoft.Json;
using SandBox.Missions.MissionLogics.Hideout;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using ItemPriorityQueue = TaleWorlds.Library.PriorityQueue<TaleWorlds.Core.EquipmentElement, int>;

#endregion

namespace DynamicTroopEquipmentReupload;

public static class ArmyArmory {
	public static readonly ItemRoster Armory = new();

	private static ItemObject[]? _cachedThrownWeapons;
	private static ItemObject[]? _cachedArrows;

	private static ItemObject[]? _cachedBolts;

	internal static ItemObject? ResolveArmoryItem(string? itemId) {
		if (string.IsNullOrEmpty(itemId))
			return null;

		var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId) ??
				   ItemObject.GetCraftedItemObjectFromHashedCode(itemId);

		return IsUsableArmoryItem(item) ? item : null;
	}

	internal static bool TryResolveArmoryItem(ItemObject? item, out ItemObject resolvedItem) {
		resolvedItem = null!;
		if (item == null || string.IsNullOrEmpty(item.StringId))
			return false;

		var canonicalItem = ResolveArmoryItem(item.StringId);
		if (canonicalItem == null)
			return false;

		resolvedItem = canonicalItem;
		return true;
	}

	internal static bool TryNormalizeArmoryElement(EquipmentElement equipmentElement, out EquipmentElement normalizedElement) {
		normalizedElement = default;
		if (!TryResolveArmoryItem(equipmentElement.Item, out var resolvedItem))
			return false;

		var itemModifier = ResolveArmoryModifier(equipmentElement.ItemModifier);
		var cosmeticItem = TryResolveArmoryItem(equipmentElement.CosmeticItem, out var resolvedCosmeticItem)
			? resolvedCosmeticItem
			: null;

		normalizedElement = new EquipmentElement(resolvedItem,
			itemModifier,
			cosmeticItem,
			equipmentElement.IsQuestItem);
		return true;
	}

	private static ItemModifier? ResolveArmoryModifier(ItemModifier? itemModifier) {
		if (itemModifier == null)
			return null;

		if (!itemModifier.IsReady ||
			string.IsNullOrEmpty(itemModifier.StringId) ||
			TextObject.IsNullOrEmpty(itemModifier.Name))
			return null;

		return MBObjectManager.Instance.GetObject<ItemModifier>(itemModifier.StringId);
	}

	private static bool IsUsableArmoryItem(ItemObject? item) {
		return item != null &&
			   item.IsReady &&
			   item.ItemType != ItemObject.ItemTypeEnum.Invalid &&
			   !string.IsNullOrEmpty(item.StringId) &&
			   !TextObject.IsNullOrEmpty(item.Name);
	}

	public static void AddItemToArmory(ItemObject item, int count = 1) {
		if (count <= 0 || !TryResolveArmoryItem(item, out var resolvedItem) || !ItemBlackList.Test(resolvedItem))
			return;

		_ = Armory.AddToCounts(resolvedItem, count);
	}

	public static void ResetForCampaign() {
		Armory.Clear();
		_cachedThrownWeapons = null;
		_cachedArrows = null;
		_cachedBolts = null;
	}

	public static int SanitizeInPlace(bool forceRebuild = false) {
		var discardedEntryCount = 0;
		var requiresRebuild = forceRebuild;
		var normalizedEntries = new HashSet<(ItemObject Item, ItemModifier? Modifier)>();

		foreach (var rosterElement in Armory) {
			if (rosterElement.Amount <= 0 ||
				rosterElement.IsEmpty ||
				!TryNormalizeArmoryElement(rosterElement.EquipmentElement, out var normalizedElement)) {
				discardedEntryCount++;
				requiresRebuild = true;
				continue;
			}

			var equipmentElement = rosterElement.EquipmentElement;
			if (!ReferenceEquals(equipmentElement.Item, normalizedElement.Item) ||
				!ReferenceEquals(equipmentElement.ItemModifier, normalizedElement.ItemModifier) ||
				!ReferenceEquals(equipmentElement.CosmeticItem, normalizedElement.CosmeticItem) ||
				equipmentElement.IsQuestItem != normalizedElement.IsQuestItem ||
				!normalizedEntries.Add((normalizedElement.Item, normalizedElement.ItemModifier)))
				requiresRebuild = true;
		}
		if (!requiresRebuild)
			return 0;

		var sanitizedEntries = new Dictionary<(ItemObject Item, ItemModifier? Modifier, ItemObject? CosmeticItem, bool IsQuestItem), int>();
		foreach (var rosterElement in Armory) {
			if (rosterElement.Amount <= 0 ||
				rosterElement.IsEmpty ||
				!TryNormalizeArmoryElement(rosterElement.EquipmentElement, out var normalizedElement))
				continue;

			var key = (normalizedElement.Item,
				normalizedElement.ItemModifier,
				normalizedElement.CosmeticItem,
				normalizedElement.IsQuestItem);
			sanitizedEntries[key] = sanitizedEntries.TryGetValue(key, out var currentCount)
				? currentCount + rosterElement.Amount
				: rosterElement.Amount;
		}

		Armory.Clear();
		foreach (var entry in sanitizedEntries) {
			var equipmentElement = new EquipmentElement(entry.Key.Item,
				entry.Key.Modifier,
				entry.Key.CosmeticItem,
				entry.Key.IsQuestItem);
			Armory.AddToCounts(equipmentElement, entry.Value);
		}

		return discardedEntryCount;
	}

	public static void ReturnEquipmentToArmoryFromAgents(IEnumerable<Agent> agents) {
		Global.Log("ReturnEquipmentToArmoryFromAgents", Colors.Green, Level.Debug);
		var count = 0;
		foreach (var agent in agents)
			if (agent.IsValid()) {
				Global.Log($"Returning equipment of agent {agent.Character.StringId}", Colors.Green, Level.Debug);

				Global.ProcessAgentEquipment(agent,
											 item => {
												 AddItemToArmory(item);
												 Global.Log($"equipment {item.StringId} returned",
															Colors.Green,
															Level.Debug);
												 count++;
											 });
			}

		Global.Log($"{count} equipment reclaimed", Colors.Green, Level.Debug);
	}

	public static void AssignEquipment(Equipment equipment) {
		foreach (var slot in Global.EquipmentSlots) {
			if ((Mission.Current?.IsNavalBattle == true ||
				 Mission.Current?.IsNavalRaidBattle == true ||
				 Mission.Current?.HasMissionBehavior<HideoutMissionController>() == true ||
				 Mission.Current?.HasMissionBehavior<MissionSiegeEnginesLogic>() == true) &&
				(slot == EquipmentIndex.Horse || slot == EquipmentIndex.HorseHarness))
				continue;

			// 使用模式匹配来检查条件，并反转if语句来减少嵌套
			var element = equipment.GetEquipmentFromSlot(slot);
			if (!TryResolveArmoryItem(element.Item, out var itemToConsume))
				continue;

			var armoryElement = Armory.FirstOrDefaultQ(rosterElement =>
				rosterElement.Amount > 0 &&
				TryResolveArmoryItem(rosterElement.EquipmentElement.Item, out var armoryItem) &&
				armoryItem.StringId == itemToConsume.StringId);

			if (!armoryElement.IsEmpty)
				_ = Armory.AddToCounts(armoryElement.EquipmentElement, -1);
			else
				Global.Log($"Assigning Empty item {itemToConsume.StringId}", Colors.Red, Level.Warn);
		}
	}

	private static ItemObject? GetRandomItem(ItemObject[]? items) {
		return items is { Length: > 0 } ? items.GetRandomElement() : null;
	}

	public static void SellExcessEquipmentForThrowingWeapons() {
		var soldValue = SellExcessEquipment();
		if (soldValue <= 0)
		{
			MessageDisplayService.EnqueueMessage(new InformationMessage(
				LocalizedTexts.GetSoldExcessEquipmentForThrowingWeapons(0, 0, 0),
				Colors.Green));
			return;
		}

		_cachedThrownWeapons ??= MBObjectManager.Instance
			.GetObjectTypeList<ItemObject>()
			?.WhereQ(item => TryResolveArmoryItem(item, out _) && item.Value > 0 && item.IsThrowingWeaponCanBeAcquired())
			.ToArrayQ() ?? Array.Empty<ItemObject>();

		_cachedArrows ??= MBObjectManager.Instance
			.GetObjectTypeList<ItemObject>()
			?.WhereQ(item => TryResolveArmoryItem(item, out _) && item.IsArrow() && item.Value > 0)
			.ToArrayQ() ?? Array.Empty<ItemObject>();

		_cachedBolts ??= MBObjectManager.Instance
			.GetObjectTypeList<ItemObject>()
			?.WhereQ(item => TryResolveArmoryItem(item, out _) && item.IsBolt() && item.Value > 0)
			.ToArrayQ() ?? Array.Empty<ItemObject>();

		var playerParty = MobileParty.MainParty;
		var troopRoster = playerParty?.MemberRoster?.GetTroopRoster();
		if (troopRoster == null) return;

		// (cavalry + infantry) = X, (horse archer + archers) = Y, X+Y=100
		var infantryAndCavalryCount = 0;
		var archerAndHorseArcherCount = 0;

		// split
		var bowUserCount = 0;
		var crossbowUserCount = 0;

		// weighting for type of ranged ammo / throwing weapon
		var preferredThrowingWeights = new Dictionary<ItemObject, int>();
		var preferredArrowWeights = new Dictionary<ItemObject, int>();
		var preferredBoltWeights = new Dictionary<ItemObject, int>();

		foreach (var troop in troopRoster)
		{
			if (troop.Character is not { IsHero: false } character) continue;

			var troopCount = troop.Number;
			if (troopCount <= 0) continue;

			var isRanged = character.IsRanged;
			if (isRanged)
				archerAndHorseArcherCount += troopCount;
			else
				infantryAndCavalryCount += troopCount;

			var referenceEquipment = character.RandomBattleEquipment;

			var usesBow = false;
			var usesCrossbow = false;

			ItemObject? preferredThrowing = null;
			ItemObject? preferredArrows = null;
			ItemObject? preferredBolts = null;

			foreach (var slot in Assignment.WeaponSlots)
			{
				var equipmentElement = referenceEquipment.GetEquipmentFromSlot(slot);
				if (equipmentElement is not { IsEmpty: false, Item: { } item }) continue;

				if (!usesBow && item.IsBow())
					usesBow = true;

				if (!usesCrossbow && item.IsCrossBow())
					usesCrossbow = true;

				if (preferredThrowing == null && item.IsThrowingWeaponCanBeAcquired())
					preferredThrowing = item;

				if (preferredArrows == null && item.IsArrow())
					preferredArrows = item;

				if (preferredBolts == null && item.IsBolt())
					preferredBolts = item;
			}
			if (isRanged)
			{
				if (usesCrossbow)
				{
					crossbowUserCount += troopCount;
					if (preferredBolts != null)
						preferredBoltWeights[preferredBolts] = preferredBoltWeights.TryGetValue(preferredBolts, out var count)
							? count + troopCount
							: troopCount;
				}
				else if (usesBow)
				{
					bowUserCount += troopCount;
					if (preferredArrows != null)
						preferredArrowWeights[preferredArrows] = preferredArrowWeights.TryGetValue(preferredArrows, out var count)
							? count + troopCount
							: troopCount;
				}
			}
			else if (preferredThrowing != null)
			{
				preferredThrowingWeights[preferredThrowing] = preferredThrowingWeights.TryGetValue(preferredThrowing, out var count)
					? count + troopCount
					: troopCount;
			}
		}

		static ItemObject? GetWeightedRandomItem(Dictionary<ItemObject, int> weightByItem) {
			if (weightByItem.Count == 0) return null;

			var totalWeight = 0;
			foreach (var kv in weightByItem)
				totalWeight += kv.Value;

			if (totalWeight <= 0) return null;

			var roll = MBRandom.RandomInt(totalWeight);
			foreach (var kv in weightByItem)
			{
				roll -= kv.Value;
				if (roll < 0)
					return kv.Key;
			}

			return null;
		}
		var totalTroopCount = infantryAndCavalryCount + archerAndHorseArcherCount;

		var throwingBudget = totalTroopCount > 0
			? (int)Math.Round(soldValue * (double)infantryAndCavalryCount / totalTroopCount)
			: soldValue;

		var ammoBudget = soldValue - throwingBudget;
		var totalRangedWeaponUsers = bowUserCount + crossbowUserCount;
		var arrowBudget = totalRangedWeaponUsers > 0
			? (int)Math.Round(ammoBudget * (double)bowUserCount / totalRangedWeaponUsers)
			: ammoBudget;

		var boltBudget = ammoBudget - arrowBudget;

		var throwingCount = 0;
		var ammoCount = 0;
		var remainingThrowingBudget = throwingBudget;
		while (remainingThrowingBudget > 0)
		{
			var item = GetWeightedRandomItem(preferredThrowingWeights) ?? GetRandomItem(_cachedThrownWeapons);
			if (item == null || item.Value <= 0) break;

			AddItemToArmory(item);
			remainingThrowingBudget -= item.Value;
			throwingCount++;
		}
		var remainingArrowBudget = arrowBudget;
		while (remainingArrowBudget > 0)
		{
			var item = GetWeightedRandomItem(preferredArrowWeights) ?? GetRandomItem(_cachedArrows);
			if (item == null || item.Value <= 0) break;

			AddItemToArmory(item);
			remainingArrowBudget -= item.Value;
			ammoCount++;
		}
		var remainingBoltBudget = boltBudget;
		while (remainingBoltBudget > 0)
		{
			var item = GetWeightedRandomItem(preferredBoltWeights) ?? GetRandomItem(_cachedBolts);
			if (item == null || item.Value <= 0) break;

			AddItemToArmory(item);
			remainingBoltBudget -= item.Value;
			ammoCount++;
		}

		MessageDisplayService.EnqueueMessage(new InformationMessage(
			LocalizedTexts.GetSoldExcessEquipmentForThrowingWeapons(soldValue, throwingCount, ammoCount),
			Colors.Green));
	}

	private static int SellExcessEquipment() {
		SanitizeInPlace();
		var excessValue = 0;
		var playerParty = MobileParty.MainParty;
		if (playerParty?.MemberRoster?.GetTroopRoster() == null) return 0;

		var memberCnt = playerParty.MemberRoster.GetTroopRoster()
								   .WhereQ(element => element.Character is { IsHero: false })
								   .SumQ(element => element.Number);

		foreach (var equipmentAndThreshold in EveryoneCampaignBehavior.EquipmentAndThresholds) {
			var armorTotalCount = Armory.WhereQ(kv => kv.EquipmentElement.Item?.ItemType == equipmentAndThreshold.Key)
										.SumQ(kv => kv.Amount);
			var surplusCount = armorTotalCount - equipmentAndThreshold.Value(memberCnt);
			if (surplusCount <= 0) continue;

			var surplusCountCpy = surplusCount;

			// 创建优先级队列
			ItemPriorityQueue armorQueue = new(new ArmorElementComparer());
			foreach (var kv in Armory.WhereQ(kv => kv.EquipmentElement.Item?.ItemType == equipmentAndThreshold.Key))
				armorQueue.Enqueue(kv.EquipmentElement, kv.EquipmentElement.ItemValue);

			// 移除多余的装备
			while (surplusCount > 0 && !armorQueue.IsEmpty) {
				var lowestArmor = armorQueue.Dequeue();
				var countToRemove = Math.Min(Armory.GetElementNumber(Armory.FindIndexOfElement(lowestArmor.Key)),
											 surplusCount);

				//Global.Debug($"countToRemove={countToRemove}, lowestArmorNumber={Armory.GetItemNumber(lowestArmor.Key)}");
				_            =  Armory.AddToCounts(lowestArmor.Key, -countToRemove); // 减少数量
				surplusCount -= countToRemove;
				excessValue  += countToRemove * lowestArmor.Key.ItemValue;
				Global.Debug($"Sold {countToRemove}x{lowestArmor.Key.ItemValue} from player's armory");
			}

			Global.Debug($"Sold {surplusCountCpy - surplusCount}x{equipmentAndThreshold.Key} items from player's armory");
		}

		Global.Debug($"Sold {excessValue} denars worth of equipment from player's armory");
		return excessValue;
	}

	public static void DebugClearEmptyItem() {
		var removedEntries = SanitizeInPlace();
		Global.Debug($"Removed {removedEntries} invalid entries from player's armory");
	}

	public static void RebuildArmory() {
		var removedEntries = SanitizeInPlace(forceRebuild: true);
		Global.Debug($"Armory rebuilt in place, removed {removedEntries} invalid entries");
	}

	public static void DebugRemovePlayerCraftedItems() {
		var toRemove = Armory
					   .WhereQ(kv => kv is {
											   IsEmpty         : false,
											   EquipmentElement: { IsEmpty: false, Item.IsCraftedByPlayer: true }
										   })
					   .ToArrayQ();
		if (toRemove == null) return;

		foreach (var item in toRemove) Armory.Remove(item);

		Global.Debug($"Removed {toRemove.Length} player crafted entries from player's armory");
	}

	public static void Import() {
		try {
			// 获取 armory.json 文件的完整路径
			var filePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../armory.json");

			// 读取文件内容
			var json = File.ReadAllText(filePath);

			// 使用 Newtonsoft.Json 的 JsonSerializer 进行反序列化
			var dict = JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
			if (dict == null) {
				Global.Warn("armory.json is empty or invalid");
				return;
			}

			Armory.Clear();
			foreach (var kpv in dict) {
				var item = ResolveArmoryItem(kpv.Key);
				if (item != null && kpv.Value > 0) { _ = Armory.AddToCounts(item, kpv.Value); }
				else { Global.Warn($"cannot get object {kpv.Key}"); }
			}

			Global.Debug($"Successfully imported armory from {filePath}");
		}
		catch (Exception e) { Global.Error(e.Message); }
	}

	public static void Export() {
		try {
			SanitizeInPlace();
			Dictionary<string, int> dict = new();
			foreach (var rosterElement in Armory) {
				var stringId = rosterElement.EquipmentElement.Item?.StringId;
				var cnt      = rosterElement.Amount;
				if (stringId == null || cnt <= 0) continue;
				if (dict.ContainsKey(stringId)) { dict[stringId] += cnt; }
				else { dict.Add(stringId, cnt); }
			}

			// 使用 Newtonsoft.Json 的 JsonSerializer 进行序列化
			var json = JsonConvert.SerializeObject(dict, Formatting.Indented);
			// 写入到 armory.json 文件
			var filename = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../armory.json");
			File.WriteAllText(filename, json);
			Global.Debug($"Successfully export armory to {filename}");
		}
		catch (Exception e) { Global.Error(e.Message); }
	}
}