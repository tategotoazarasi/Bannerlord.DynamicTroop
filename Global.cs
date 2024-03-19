using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Bannerlord.DynamicTroop.Extensions;
using log4net;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.DynamicTroop;

public static class Global {
	public static readonly EquipmentIndex[] EquipmentSlots = {
																 EquipmentIndex.Weapon0,
																 EquipmentIndex.Weapon1,
																 EquipmentIndex.Weapon2,
																 EquipmentIndex.Weapon3,
																 EquipmentIndex.Head,
																 EquipmentIndex.Body,
																 EquipmentIndex.Leg,
																 EquipmentIndex.Gloves,
																 EquipmentIndex.Cape,
																 EquipmentIndex.Horse,
																 EquipmentIndex.HorseHarness
															 };

	public static readonly EquipmentIndex[] ArmourAndHorsesSlots = {
																	   EquipmentIndex.Head,
																	   EquipmentIndex.Body,
																	   EquipmentIndex.Leg,
																	   EquipmentIndex.Gloves,
																	   EquipmentIndex.Cape,
																	   EquipmentIndex.Horse,
																	   EquipmentIndex.HorseHarness
																   };

	public static EquipmentIndex[] ArmourSlots = {
													 EquipmentIndex.Head,
													 EquipmentIndex.Body,
													 EquipmentIndex.Leg,
													 EquipmentIndex.Gloves,
													 EquipmentIndex.Cape
												 };

	public static ItemObject.ItemTypeEnum[] ItemTypes = {
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
															ItemObject.ItemTypeEnum.Polearm,
															ItemObject.ItemTypeEnum.Arrows,
															ItemObject.ItemTypeEnum.Bolts,
															ItemObject.ItemTypeEnum.Thrown
														};

	public static WeaponClass[] InvalidWeaponClasses = {
														   WeaponClass.Undefined,
														   WeaponClass.Boulder,
														   WeaponClass.Banner,
														   WeaponClass.Stone
													   };

	private static readonly Dictionary<ItemObject.ItemTypeEnum, CraftingTemplate[]> CraftingTemplatesByItemType = new();

	public static void InitializeCraftingTemplatesByItemType() {
		ItemObject.ItemTypeEnum[] itemTypes = {
												  ItemObject.ItemTypeEnum.OneHandedWeapon,
												  ItemObject.ItemTypeEnum.TwoHandedWeapon,
												  ItemObject.ItemTypeEnum.Polearm
											  };

		foreach (var itemType in itemTypes) {
			CraftingTemplate[] templates = CraftingTemplate.All.WhereQ(template => template.ItemType == itemType).ToArrayQ();
			CraftingTemplatesByItemType[itemType] = templates;
		}
	}

	public static List<WeaponClass> GetWeaponClass(ItemObject item) {
		return item is { HasWeaponComponent: true } ? item.WeaponComponent.Weapons.SelectQ(weapon => weapon.WeaponClass).Distinct().OrderByQ(weaponClass => weaponClass).ToListQ() : new List<WeaponClass>();
	}

	public static void Log(string message, Color color, Level level, int skipFrames = 1) {
		if (SubModule.Settings is { DebugMode: true } && (SubModule.Settings.LogLevel.SelectedValue == Level.All || level >= SubModule.Settings.LogLevel.SelectedValue)) {
			MessageDisplayService.EnqueueMessage(new InformationMessage(message, color));

			// 使用 log4net 记录日志
			StackFrame frame  = new(skipFrames, true); // 创建 StackFrame 对象，参数 1 表示上一个栈帧
			var        method = frame.GetMethod();     // 获取方法信息

			// 获取文件名而不是完整路径
			var fileName = Path.GetFileName(frame.GetFileName());

			var lineNumber = frame.GetFileLineNumber(); // 获取行号

			// 构造日志消息
			var logMessage = $"[{method.DeclaringType?.FullName}.{method.Name}] [{fileName}:{lineNumber}] {message}";

			// 获取 log4net 日志实例
			//LogManager.GetLogger(method.DeclaringType);
			var log = LogManager.GetLogger("dtes");

			// 根据指定的日志级别记录日志
			if (level == Level.Debug)
				log.Debug(logMessage);
			else if (level == Level.Info)
				log.Info(logMessage);
			else if (level == Level.Warn)
				log.Warn(logMessage);
			else if (level == Level.Error)
				log.Error(logMessage);
			else if (level == Level.Fatal) log.Fatal(logMessage);
		}
	}

	public static void Debug(string message) { Log(message, Colors.Green, Level.Debug, 2); }

	public static void Info(string message) { Log(message, Colors.Blue, Level.Info, 2); }

	public static void Warn(string message) { Log(message, Colors.Yellow, Level.Warn, 2); }

	public static void Error(string message) { Log(message, Colors.Red, Level.Error, 2); }

	public static void Fatal(string message) { Log(message, Colors.Magenta, Level.Fatal, 2); }

	public static bool HaveSameWeaponClass(List<WeaponClass> list1, List<WeaponClass> list2) {
		var thrown1 = list1.WhereQ(weaponClass => weaponClass is WeaponClass.ThrowingKnife or WeaponClass.ThrowingAxe or WeaponClass.Stone or WeaponClass.Javelin).ToArrayQ();
		if (!thrown1.IsEmpty()) return list2.AnyQ(weaponClass => thrown1.Contains(weaponClass));

		var thrown2 = list2.WhereQ(weaponClass => weaponClass is WeaponClass.ThrowingKnife or WeaponClass.ThrowingAxe or WeaponClass.Stone or WeaponClass.Javelin).ToArrayQ();

		// 直接返回判断条件的结果
		return !thrown2.IsEmpty() && list1.AnyQ(weaponClass => thrown2.Contains(weaponClass));
	}

	public static bool FullySameWeaponClass(ItemObject weapon1, ItemObject weapon2) {
		//var list1 = GetWeaponClass(weapon1);
		//var list2 = GetWeaponClass(weapon2);
		//if (list1.Count != list2.Count) return false;
		if (weapon1.Weapons == null || weapon2.Weapons == null || weapon1.Weapons.IsEmpty() || weapon2.Weapons.IsEmpty() || weapon1.Weapons.Count != weapon2.Weapons.Count)
			return false;

		weapon1.Weapons.Sort((x, y) => x.WeaponClass - y.WeaponClass);
		weapon2.Weapons.Sort((x, y) => x.WeaponClass - y.WeaponClass);
		return !Enumerable
				.Range(0, weapon1.Weapons.Count)
				.AnyQ(i => weapon1.Weapons[i].WeaponClass != weapon2.Weapons[i].WeaponClass || weapon1.Weapons[i].SwingDamageType != weapon2.Weapons[i].SwingDamageType || weapon1.Weapons[i].ThrustDamageType != weapon2.Weapons[i].ThrustDamageType);
	}

	public static void ProcessAgentEquipment(Agent agent, Action<ItemObject> processEquipmentItem) {
		if (!agent.IsValid()) return;

		var missionEquipment = agent.Equipment;
		var spawnEquipment   = agent.SpawnEquipment;

		if (missionEquipment == null || spawnEquipment == null) return;

		// 处理武器槽装备
		foreach (var slot in Assignment.WeaponSlots) {
			var element = missionEquipment[slot];
			if (element.IsEmpty || element.Item == null) continue;

			if (IsAmmoAndEmpty(element)) {
				Log($"Empty Ammo {element.Item.StringId}", Colors.Green, Level.Debug);
				continue;
			}

			processEquipmentItem(element.Item);
		}

		// 处理装甲和马匹槽装备
		foreach (var slot in ArmourAndHorsesSlots) {
			var element = spawnEquipment[slot];
			if (element.IsEmpty || element.Item == null) continue;

			processEquipmentItem(element.Item);
		}
	}

	private static bool IsAmmoAndEmpty(MissionWeapon? mw) {
		return mw is { IsEmpty: false, Item.HasWeaponComponent: true, Amount: 0 } && (mw.Value.IsAnyAmmo() || mw.Value.Item.IsThrowing());
	}

	public static bool IsInPlayerParty(IAgentOriginBase? agentOrigin) {
		if (agentOrigin == null) return false;

		var party = agentOrigin switch {
						PartyAgentOrigin partyAgentOrigin           => partyAgentOrigin.Party,
						PartyGroupAgentOrigin partyGroupAgentOrigin => partyGroupAgentOrigin.Party,
						SimpleAgentOrigin simpleAgentOrigin         => simpleAgentOrigin.Party,
						_                                           => null
					};

		return party != null ? IsPartyInPlayerCommand(party) : agentOrigin.IsUnderPlayersCommand;
	}

	public static MobileParty? GetAgentParty(IAgentOriginBase? origin) {
		return origin switch {
				   null                                        => null,
				   PartyAgentOrigin partyAgentOrigin           => partyAgentOrigin.Party?.MobileParty,
				   PartyGroupAgentOrigin partyGroupAgentOrigin => partyGroupAgentOrigin.Party?.MobileParty,
				   SimpleAgentOrigin simpleAgentOrigin         => simpleAgentOrigin.Party?.MobileParty,
				   _                                           => null
			   };
	}

	private static bool IsPartyInPlayerCommand(PartyBase? party) {
		return party != null && party == PartyBase.MainParty;
	}

	public static int CountCharacterEquipmentItemTypes(CharacterObject? character, ItemObject.ItemTypeEnum? itemType) {
		return character == null || itemType == null || character.BattleEquipments == null ? 0 : character.BattleEquipments.MaxQ(equipment => Assignment.WeaponSlots.CountQ(slot => equipment.GetEquipmentFromSlot(slot).Item?.ItemType == itemType));
	}

	public static List<ItemObject> CreateRandomCraftedItemsByItemType(ItemObject.ItemTypeEnum? type, BasicCultureObject? culture, int num = 0) {
		List<ItemObject> items  = new();
		Random           random = new();
		if (type == null || culture == null) return items;

		var templates = CraftingTemplatesByItemType[type.Value];
		for (var i = 0; i < num; i++) {
			var        randomElement = templates[random.Next() % templates.Length];
			TextObject textObject    = new("{=uZhHh7pm}Crafted {CURR_TEMPLATE_NAME}");
			_ = textObject.SetTextVariable("CURR_TEMPLATE_NAME", randomElement.TemplateName);
			Crafting crafting = new(randomElement, culture, textObject);
			crafting.Init();
			crafting.Randomize();
			var hashedCode = crafting.CurrentWeaponDesign.HashedCode;
			crafting.GetCurrentCraftedItemObject().StringId = hashedCode;
			var itemObject = MBObjectManager.Instance.GetObject<ItemObject>(hashedCode);
			if (itemObject == null) {
				itemObject = MBObjectManager.Instance.RegisterObject(crafting.GetCurrentCraftedItemObject());
				items.Add(itemObject);
			}
		}

		return items;
	}
}