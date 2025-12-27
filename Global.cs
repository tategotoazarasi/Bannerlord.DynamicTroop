using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DynamicTroopEquipmentReupload.Extensions;
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

namespace DynamicTroopEquipmentReupload;

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
															ItemObject.ItemTypeEnum.Shield,
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

	public static bool IsTroopPoolStashSessionActive { get; private set; }

	public static void BeginTroopPoolStashSession() {
		IsTroopPoolStashSessionActive = true;
	}

	public static void EndTroopPoolStashSession() {
		IsTroopPoolStashSessionActive = false;
	}

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

	public static EquipmentIndex? GetEquipmentIndexByItemType(ItemObject.ItemTypeEnum type) {
		return type switch {
				   ItemObject.ItemTypeEnum.HeadArmor => EquipmentIndex.Head,
				   ItemObject.ItemTypeEnum.BodyArmor => EquipmentIndex.Body,
				   ItemObject.ItemTypeEnum.LegArmor  => EquipmentIndex.Leg,
				   ItemObject.ItemTypeEnum.HandArmor => EquipmentIndex.Gloves,
				   ItemObject.ItemTypeEnum.Cape      => EquipmentIndex.Cape,
				   _                                 => null
			   };
	}


	public static List<WeaponClass> GetWeaponClass(ItemObject item) {
		return item is { HasWeaponComponent: true } ? item.WeaponComponent.Weapons.SelectQ(weapon => weapon.WeaponClass).Distinct().OrderByQ(weaponClass => weaponClass).ToListQ() : new List<WeaponClass>();
	}

	public static void Log(string message, Color color, Level level, int skipFrames = 1) {
		if (SubModule.Settings is { DebugMode: true } &&
			(SubModule.Settings.MinimumLogLevel == Level.All || level >= SubModule.Settings.MinimumLogLevel)) {
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
		if (list1.IsEmpty() || list2.IsEmpty()) { return false; }

		static bool IsThrown(WeaponClass wc) {
			return wc is WeaponClass.ThrowingKnife or WeaponClass.ThrowingAxe or WeaponClass.Stone or WeaponClass.Javelin;
		}

		var thrown1 = list1.WhereQ(IsThrown).ToArrayQ();
		var thrown2 = list2.WhereQ(IsThrown).ToArrayQ();

		if (!thrown1.IsEmpty() || !thrown2.IsEmpty()) {
			var lhs = !thrown1.IsEmpty() ? thrown1 : list1.ToArrayQ();
			var rhs = !thrown2.IsEmpty() ? thrown2 : list2.ToArrayQ();
			return lhs.AnyQ(wc => rhs.Contains(wc));
		}

		return list1.AnyQ(wc => list2.Contains(wc));
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
		ProcessAgentEquipment(agent, processEquipmentItem, null);
	}

	public static void ProcessAgentEquipment(
		Agent                                                        agent,
		Action<ItemObject>                                           processEquipmentItem,
		Func<EquipmentIndex, MissionWeapon, EquipmentElement, bool>? shouldProcessSlot) {
		var missionEquipment = agent.Equipment;
		var spawnEquipment   = agent.SpawnEquipment;

		foreach (var slot in EquipmentSlots) {
			var spawnElement = spawnEquipment[slot];
			if (spawnElement.IsEmpty || spawnElement.Item is null)
				continue;

			var isWeaponSlot =
				slot == EquipmentIndex.Weapon0 ||
				slot == EquipmentIndex.Weapon1 ||
				slot == EquipmentIndex.Weapon2 ||
				slot == EquipmentIndex.Weapon3;

			var missionWeapon = default(MissionWeapon);
			if (isWeaponSlot) { missionWeapon = missionEquipment[slot]; }

			if (spawnElement.Item.ItemType == ItemObject.ItemTypeEnum.Banner || spawnElement.Item.IsBannerItem)
				continue;

			if (isWeaponSlot) {
				var isAmmoOrThrown =
					spawnElement.Item.ItemType == ItemObject.ItemTypeEnum.Arrows ||
					spawnElement.Item.ItemType == ItemObject.ItemTypeEnum.Bolts  ||
					spawnElement.Item.ItemType == ItemObject.ItemTypeEnum.Thrown;

				if (isAmmoOrThrown && (missionWeapon.IsEmpty || IsAmmoAndEmpty(missionWeapon)))
					continue;

				if (spawnElement.Item.ItemType == ItemObject.ItemTypeEnum.Shield && !missionWeapon.IsEmpty && missionWeapon.HitPoints <= 0)
					continue;
			}

			if (shouldProcessSlot != null && !shouldProcessSlot(slot, missionWeapon, spawnElement))
				continue;

			processEquipmentItem(spawnElement.Item);
		}
	}


	private static bool IsAmmoAndEmpty(MissionWeapon? mw) {
		return mw is { IsEmpty: false, Item.HasWeaponComponent: true, Amount: 0 } && (mw.Value.IsAnyAmmo() || mw.Value.Item.IsThrowing());
	}

	public static bool IsInPlayerParty(IAgentOriginBase agentOrigin) {
		var party = agentOrigin switch {
						PartyAgentOrigin partyAgentOrigin           => partyAgentOrigin.Party,
						PartyGroupAgentOrigin partyGroupAgentOrigin => partyGroupAgentOrigin.Party,
						SimpleAgentOrigin simpleAgentOrigin         => simpleAgentOrigin.Party,
						_                                           => null
					};

		if (agentOrigin.IsUnderPlayersCommand) { return true; }

		return party != null && IsPartyInPlayerCommand(party);
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
		var mainParty = Campaign.Current?.MainParty;
		return party?.MobileParty != null && mainParty != null && party.MobileParty == mainParty;
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