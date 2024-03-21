#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Bannerlord.DynamicTroop.Comparers;
using Bannerlord.DynamicTroop.Extensions;
using log4net.Core;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using ItemPriorityQueue = TaleWorlds.Library.PriorityQueue<TaleWorlds.Core.EquipmentElement, int>;

#endregion

namespace Bannerlord.DynamicTroop;

public static class ArmyArmory {
	public static ItemRoster Armory = new();

	private static ItemObject[]? _cachedThrownWeapons;

	public static void AddItemToArmory(ItemObject item, int count = 1) {
		if (!ItemBlackList.Test(item)) return;
		_ = Armory.AddToCounts(item, count);
	}

	public static void ReturnEquipmentToArmoryFromAgents(IEnumerable<Agent> agents) {
		Global.Log("ReturnEquipmentToArmoryFromAgents", Colors.Green, Level.Debug);
		var count = 0;
		foreach (var agent in agents)
			if (agent.IsValid()) {
				Global.Log($"Returning equipment of agent {agent.Character.StringId}", Colors.Green, Level.Debug);

				Global.ProcessAgentEquipment(agent,
											 item => {
												 _ = Armory.AddToCounts(item, 1);
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
			var element = equipment.GetEquipmentFromSlot(slot);

			// 使用模式匹配来检查条件，并反转if语句来减少嵌套
			if (element.IsEmpty || element.Item is null) continue;

			var itemToAssign =
				Armory.FirstOrDefaultQ(a => !a.IsEmpty                                                &&
											a.EquipmentElement.Item.StringId == element.Item.StringId &&
											a.Amount                         > 0);

			if (!itemToAssign.IsEmpty)
				_ = Armory.AddToCounts(itemToAssign.EquipmentElement, -1);
			else
				Global.Log($"Assigning Empty item {element.Item.StringId}", Colors.Red, Level.Warn);
		}
	}

	public static void SellExcessEquipmentForThrowingWeapons() {
		var value         = SellExcessEquipment();
		var originalValue = value;
		_cachedThrownWeapons ??= MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
												?.WhereQ(item => item.IsThrowingWeaponCanBeAcquired())
												.ToArrayQ();
		var cnt = 0;
		while (value > 0) {
			var item = _cachedThrownWeapons.GetRandomElement();
			if (item != null) {
				AddItemToArmory(item);
				value -= item.Value;
				cnt++;
			}
		}

		MessageDisplayService.EnqueueMessage(new InformationMessage(LocalizedTexts
																		.GetSoldExcessEquipmentForThrowingWeapons(originalValue -
																												  value,
																												  cnt),
																	Colors.Green));
	}

	private static int SellExcessEquipment() {
		RebuildArmory();
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
		var toRemove = Armory
					   .WhereQ(kv => kv is not {
												   IsEmpty         : false,
												   EquipmentElement: { IsEmpty: false, Item: not null },
												   Amount          : > 0
											   }                                                          ||
									 kv.EquipmentElement.Item.ItemType == ItemObject.ItemTypeEnum.Invalid ||
									 kv.EquipmentElement.Item.StringId == null                            ||
									 kv.EquipmentElement.Item.StringId.IsEmpty())
					   .ToArrayQ();
		if (toRemove == null) return;

		foreach (var item in toRemove) Armory.Remove(item);

		Global.Debug($"Removed {toRemove.Length} empty entries from player's armory");
	}

	public static void RebuildArmory() {
		var toAdd = Armory
					.WhereQ(kv => kv is {
											IsEmpty         : false,
											EquipmentElement: { IsEmpty: false, Item: not null },
											Amount          : > 0
										})
					.ToArrayQ();
		Armory = new ItemRoster();
		if (toAdd == null) return;

		Armory.Add(toAdd);
		Global.Debug($"Armory has been rebuilt with {toAdd.Length} entries");
	}

	public static void DebugRemovePlayerCraftedItems() {
		var toRemove = Armory
					   .WhereQ(kv => kv is not {
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
			Armory.Clear();
			foreach (var kpv in dict) {
				var item = MBObjectManager.Instance.GetObject<ItemObject>(kpv.Key);
				if (item != null) { _ = Armory.AddToCounts(item, kpv.Value); }
				else { Global.Warn($"cannot get object {kpv.Key}"); }
			}

			Global.Debug($"Successfully export armory from {filePath}");
		}
		catch (Exception e) { Global.Error(e.Message); }
	}

	public static void Export() {
		try {
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