#region

	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using log4net;
	using log4net.Core;
	using TaleWorlds.CampaignSystem.ViewModelCollection;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.Localization;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public static class Global {
		public static EquipmentIndex[] EquipmentSlots = {
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

		public static EquipmentIndex[] ArmourAndHorsesSlots = {
																  EquipmentIndex.Head,
																  EquipmentIndex.Body,
																  EquipmentIndex.Leg,
																  EquipmentIndex.Gloves,
																  EquipmentIndex.Cape,
																  EquipmentIndex.Horse,
																  EquipmentIndex.HorseHarness
															  };

		public static bool IsWeapon(ItemObject item) { return item.HasWeaponComponent; }

		public static List<WeaponClass> GetWeaponClass(ItemObject item) {
			return IsWeapon(item)
					   ? item.WeaponComponent.Weapons.Select(weapon => weapon.WeaponClass).ToList()
					   : new List<WeaponClass>();
		}

		public static void Log(string message, Color color, Level level) {
			if (SubModule.settings.DebugMode) {
				// 显示信息
				InformationManager.DisplayMessage(new InformationMessage(message, color));

				// 使用 log4net 记录日志
				StackFrame frame = new(1, true); // 创建 StackFrame 对象，参数 1 表示上一个栈帧


				/* 项目“Bannerlord.DynamicTroop (netcoreapp3.1)”的未合并的更改
				在此之前:
							var method = frame.GetMethod(); // 获取方法信息
				在此之后:
							System.Reflection.MethodBase? method = frame.GetMethod(); // 获取方法信息
				*/
				var method = frame.GetMethod(); // 获取方法信息

				// 获取文件名而不是完整路径

				/* 项目“Bannerlord.DynamicTroop (netcoreapp3.1)”的未合并的更改
				在此之前:
							var fileName = Path.GetFileName(frame.GetFileName());
				在此之后:
							string? fileName = Path.GetFileName(frame.GetFileName());
				*/
				var fileName = Path.GetFileName(frame.GetFileName());

				var lineNumber = frame.GetFileLineNumber(); // 获取行号

				// 构造日志消息
				var logMessage = $"[{method.DeclaringType.FullName}.{method.Name}] [{fileName}:{lineNumber}] {message}";

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

		public static bool IsAgentValid(Agent? agent) {
			return agent           != null &&
				   agent.Formation != null &&

				   //agent.IsHuman           &&
				   agent.Character   != null &&
				   agent.Team        != null &&
				   agent.Team.MBTeam != null &&
				   agent.Origin      != null &&
				   agent.Team.IsValid;
		}

		public static bool HaveSameWeaponClass(List<WeaponClass> list1, List<WeaponClass> list2) {
			foreach (var weaponClass1 in list1)
				if (list2.Any(weaponClass2 => weaponClass2 == weaponClass1))
					return true;

			return false;
		}

		public static bool IsWeaponCouchable(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("couchable"));
		}

		public static bool IsSuitableForMount(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => !flag.Contains("cant_use_on_horseback"));
		}

		public static bool IsWeaponBracable(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("braceable"));
		}

		public static bool IsPolearm(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("polearm"));
		}

		public static bool IsOneHanded(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("one_handed"));
		}

		public static bool IsTwoHanded(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("two_handed"));
		}

		public static bool IsThrowing(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("throwing"));
		}

		public static bool IsBow(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("str_inventory_flag_bow"));
		}

		public static bool IsCrossBow(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("crossbow"));
		}

		public static bool IsBonusAgainstShield(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("bonus_against_shield"));
		}

		public static bool CanKnockdown(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("can_knockdown"));
		}

		public static bool CanDismount(ItemObject weapon) {
			return CheckWeaponFlag(weapon, flag => flag.Contains("can_dismount"));
		}

		private static bool CheckWeaponFlag(ItemObject weapon, Func<string, bool> flagCondition) {
			foreach (var weaponComponentData in weapon.Weapons) {
				List<(string, TextObject)> flagDetails = CampaignUIHelper.GetFlagDetailsForWeapon(weaponComponentData,
					MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage));
				if (flagDetails.Any(flagDetail => !flagDetail.Item1.IsEmpty() && flagCondition(flagDetail.Item1)))
					return true;
			}

			return false;
		}
	}