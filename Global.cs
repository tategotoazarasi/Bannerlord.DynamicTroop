using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using log4net;
using log4net.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection;
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

	private static readonly Dictionary<ItemObject.ItemTypeEnum, CraftingTemplate[]> CraftingTemplatesByItemType = new();

	public static void InitializeCraftingTemplatesByItemType() {
		ItemObject.ItemTypeEnum[] itemTypes = {
												  ItemObject.ItemTypeEnum.OneHandedWeapon,
												  ItemObject.ItemTypeEnum.TwoHandedWeapon,
												  ItemObject.ItemTypeEnum.Polearm
											  };

		foreach (var itemType in itemTypes) {
			CraftingTemplate[] templates =
				CraftingTemplate.All.WhereQ(template => template.ItemType == itemType).ToArrayQ();
			CraftingTemplatesByItemType[itemType] = templates;
		}
	}

	public static bool IsWeapon(ItemObject? item) { return item is { HasWeaponComponent: true }; }

	public static List<WeaponClass> GetWeaponClass(ItemObject item) {
		return IsWeapon(item)
				   ? item.WeaponComponent.Weapons.SelectQ(weapon => weapon.WeaponClass)
						 .Distinct()
						 .OrderByQ(weaponClass => weaponClass)
						 .ToListQ()
				   : new List<WeaponClass>();
	}

	public static void Log(string message, Color color, Level level) {
		if (SubModule.settings is { DebugMode: true }) {
			// 显示信息
			InformationManager.DisplayMessage(new InformationMessage(message, color));

			// 使用 log4net 记录日志
			StackFrame frame = new(1, true); // 创建 StackFrame 对象，参数 1 表示上一个栈帧

			var method = frame.GetMethod(); // 获取方法信息

			// 获取文件名而不是完整路径
			var fileName = Path.GetFileName(frame.GetFileName());

			var lineNumber = frame.GetFileLineNumber(); // 获取行号

			// 构造日志消息
			var logMessage = $"[{method.DeclaringType?.FullName}.{method.Name}] [{fileName}:{lineNumber}] {message}";

			// 获取 log4net 日志实例
			var log = LogManager.GetLogger(method.DeclaringType);

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

	public static void Debug(string message) { Log(message, Colors.Green, Level.Debug); }

	public static void Info(string message) { Log(message, Colors.Blue, Level.Info); }

	public static void Warn(string message) { Log(message, Colors.Yellow, Level.Warn); }

	public static void Error(string message) { Log(message, Colors.Red, Level.Error); }

	public static void Fatal(string message) { Log(message, Colors.Magenta, Level.Fatal); }

	public static bool IsAgentValid(Agent? agent) {
		return agent is {
							Formation: not null,
							Character: not null,
							Team     : { MBTeam: { }, IsValid: true },
							Origin   : not null
						};
	}

	public static bool HaveSameWeaponClass(List<WeaponClass> list1, List<WeaponClass> list2) {
		var thrown1 = list1.WhereQ(weaponClass =>
									   weaponClass is WeaponClass.ThrowingKnife
													  or WeaponClass.ThrowingAxe
													  or WeaponClass.Stone
													  or WeaponClass.Javelin)
						   .ToListQ();
		if (!thrown1.IsEmpty()) return list2.AnyQ(weaponClass => thrown1.Contains(weaponClass));

		var thrown2 = list2.WhereQ(weaponClass =>
									   weaponClass is WeaponClass.ThrowingKnife
													  or WeaponClass.ThrowingAxe
													  or WeaponClass.Stone
													  or WeaponClass.Javelin)
						   .ToListQ();

		// 直接返回判断条件的结果
		return !thrown2.IsEmpty() && list1.AnyQ(weaponClass => thrown2.Contains(weaponClass));
	}

	public static bool FullySameWeaponClass(ItemObject weapon1, ItemObject weapon2) {
		//var list1 = GetWeaponClass(weapon1);
		//var list2 = GetWeaponClass(weapon2);
		//if (list1.Count != list2.Count) return false;
		if (weapon1.Weapons == null   ||
			weapon2.Weapons == null   ||
			weapon1.Weapons.IsEmpty() ||
			weapon2.Weapons.IsEmpty() ||
			weapon1.Weapons.Count != weapon2.Weapons.Count)
			return false;

		weapon1.Weapons.Sort((x, y) => x.WeaponClass - y.WeaponClass);
		weapon2.Weapons.Sort((x, y) => x.WeaponClass - y.WeaponClass);
		return !Enumerable.Range(0, weapon1.Weapons.Count)
						  .AnyQ(i => weapon1.Weapons[i].WeaponClass      != weapon2.Weapons[i].WeaponClass     ||
									 weapon1.Weapons[i].SwingDamageType  != weapon2.Weapons[i].SwingDamageType ||
									 weapon1.Weapons[i].ThrustDamageType != weapon2.Weapons[i].ThrustDamageType);
	}

	public static bool IsWeaponCouchable(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("can_couchable"));
	}

	public static bool IsSuitableForMount(ItemObject weapon) {
		return IsWeapon(weapon)       &&
			   weapon.Weapons != null &&
			   !weapon.Weapons.AnyQ(weaponComponentData =>
										weaponComponentData != null &&
										(MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage)
											   .HasFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount) ||
										 weaponComponentData.WeaponFlags.HasFlag(WeaponFlags.CantReloadOnHorseback)));
	}

	public static bool IsWeaponBracable(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("braceable"));
	}

	public static bool IsPolearm(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("polearm"));
	}

	public static bool IsOneHanded(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("one_handed"));
	}

	public static bool IsTwoHanded(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("two_handed"));
	}

	public static bool IsThrowing(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("throwing"));
	}

	public static bool IsBow(ItemObject? weapon) {
		if (weapon == null) return false;

		if (weapon.ItemType == ItemObject.ItemTypeEnum.Bow) return true;

		if (!IsWeapon(weapon)) return false;

		if (weapon.Weapons == null) return false;

		foreach (var weaponComponentData in weapon.Weapons)
			if (weaponComponentData is { WeaponClass: WeaponClass.Bow })
				return true;

		return false;
	}

	public static bool IsCrossBow(ItemObject? weapon) {
		if (weapon == null) return false;

		if (weapon.ItemType == ItemObject.ItemTypeEnum.Crossbow) return true;

		if (!IsWeapon(weapon)) return false;

		if (weapon.Weapons == null) return false;

		foreach (var weaponComponentData in weapon.Weapons)
			if (weaponComponentData is { WeaponClass: WeaponClass.Crossbow })
				return true;

		return false;
	}

	public static bool IsBonusAgainstShield(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("bonus_against_shield"));
	}

	public static bool CanKnockdown(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("can_knockdown"));
	}

	public static bool CanDismount(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => flag.Contains("can_dismount"));
	}

	public static bool CantUseWithShields(ItemObject weapon) {
		return IsWeapon(weapon) && CheckWeaponFlag(weapon, flag => !flag.Contains("cant_use_with_shields"));
	}

	private static bool CheckWeaponFlag(ItemObject? weapon, Func<string, bool> flagCondition) {
		return weapon?.Weapons != null &&
			   weapon.Weapons.WhereQ(weaponComponentData => weaponComponentData != null)
					 .SelectMany(weaponComponentData =>
									 CampaignUIHelper.GetFlagDetailsForWeapon(weaponComponentData,
																			  MBItem
																				  .GetItemUsageSetFlags(weaponComponentData
																					  .ItemUsage)))
					 .AnyQ(flagDetail => flagDetail.Item1 != null    &&
										 !flagDetail.Item1.IsEmpty() &&
										 flagCondition(flagDetail.Item1));
	}

	public static void ProcessAgentEquipment(Agent agent, Action<ItemObject> processEquipmentItem) {
		if (!IsAgentValid(agent)) return;

		var missionEquipment = agent.Equipment;
		var spawnEquipment   = agent.SpawnEquipment;

		if (missionEquipment == null || spawnEquipment == null) return;

		// 处理武器槽装备
		foreach (var slot in Assignment.WeaponSlots) {
			var element = missionEquipment[slot];
			if (element is not { IsEmpty: false, Item: not null }) return;

			if (IsAmmoAndEmpty(element)) {
				Log($"Empty Ammo {element.Item.StringId}", Colors.Green, Level.Debug);
				continue;
			}

			processEquipmentItem(element.Item);
		}

		// 处理装甲和马匹槽装备
		foreach (var slot in ArmourAndHorsesSlots) {
			var element = spawnEquipment[slot];
			if (element is not { IsEmpty: false, Item: not null }) continue;

			processEquipmentItem(element.Item);
		}
	}

	private static bool IsAmmoAndEmpty(MissionWeapon? mw) {
		return mw != null                                          &&
			   !mw.Value.IsEmpty                                   &&
			   mw.Value.Item != null                               &&
			   IsWeapon(mw.Value.Item)                             &&
			   (mw.Value.IsAnyAmmo() || IsThrowing(mw.Value.Item)) &&
			   mw.Value.Amount == 0;
	}

	public static List<ItemObject> GetAgentArmors(Agent agent) {
		List<ItemObject> armors = new();
		foreach (var slot in ArmourAndHorsesSlots) {
			var element = agent.SpawnEquipment.GetEquipmentFromSlot(slot);
			if (element is { IsEmpty: false, Item: { HasArmorComponent: true } item }) armors.Add(item);
		}

		return armors;
	}

	public static bool IsConsumableWeapon(ItemObject item) {
		return item.ItemType is ItemObject.ItemTypeEnum.Arrows
								or ItemObject.ItemTypeEnum.Bolts
								or ItemObject.ItemTypeEnum.Thrown;
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

	public static bool IsSuitableForCharacter(ItemObject? item, CharacterObject? character) {
		return item      != null &&
			   character != null &&
			   (item.Difficulty <= 0 || item.Difficulty <= character.GetSkillValue(item.RelevantSkill));
	}

	public static bool IsArrow(ItemObject? equipment) {
		return equipment != null &&
			   (equipment.ItemType == ItemObject.ItemTypeEnum.Arrows ||
				(equipment.Weapons != null &&
				 equipment.Weapons.AnyQ(weaponComponentData =>
										   weaponComponentData is { WeaponClass: WeaponClass.Arrow })));
	}

	public static bool IsBolt(ItemObject? equipment) {
		return equipment != null &&
			   (equipment.ItemType == ItemObject.ItemTypeEnum.Bolts ||
				(equipment.Weapons != null &&
				 equipment.Weapons.Any(weaponComponentData =>
										   weaponComponentData is { WeaponClass: WeaponClass.Bolt })));
	}

	public static int CalculateClanProsperityFactor(MobileParty mobileParty) {
		if (!EveryoneCampaignBehavior.IsMobilePartyValid(mobileParty) || mobileParty.LeaderHero?.Clan == null) return 0;

		var clan = mobileParty.LeaderHero.Clan;

		// 计算领地繁荣度总和
		var prosperitySum = clan.Fiefs?.SumQ(fief => (int)(fief?.GetProsperityLevel() + 1 ?? 0)) ?? 0;

		// 计算因子：氏族等级 + 繁荣度加权
		var factor = (clan.Tier + 1) * Math.Max(1, prosperitySum);

		return factor;
	}

	public static int CountCharacterEquipmentItemTypes(CharacterObject? character, ItemObject.ItemTypeEnum? itemType) {
		return character == null || itemType == null || character.BattleEquipments == null
				   ? 0
				   : character.BattleEquipments.MaxQ(equipment =>
														 Assignment.WeaponSlots.CountQ(slot =>
															 equipment.GetEquipmentFromSlot(slot)
																	  .Item?.ItemType ==
															 itemType));
	}

	public static List<ItemObject> CreateRandomCraftedItemsByItemType(ItemObject.ItemTypeEnum? type,
																	  BasicCultureObject?      culture,
																	  int                      num = 0) {
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

	public static int GetPartyClanTier(MobileParty? party) {
		if (!EveryoneCampaignBehavior.IsMobilePartyValid(party)) return 0;

		var hero = party?.Owner ?? party?.LeaderHero;
		return hero != null ? hero.Clan?.Tier ?? 0 : 0;
	}
}