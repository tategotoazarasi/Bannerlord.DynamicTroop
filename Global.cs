#region

	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using log4net;
	using TaleWorlds.CampaignSystem.ViewModelCollection;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.Localization;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public static class Global {
		private static readonly ILog log = LogManager.GetLogger(typeof(Global));

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

		public static void Log(string message) {
			if (SubModule.settings.DebugMode) {
				// 显示信息
				InformationManager.DisplayMessage(new InformationMessage(message, Colors.Red));

				// 使用 log4net 记录日志
				StackFrame frame = new(1, true); // 创建 StackFrame 对象，参数 1 表示上一个栈帧

				/* 项目“Bannerlord.DynamicTroop (netcoreapp3.1)”的未合并的更改
				在此之前:
							var method = frame.GetMethod();       // 获取方法信息
				在此之后:
							System.Reflection.MethodBase? method = frame.GetMethod();       // 获取方法信息
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
				log.Debug(logMessage);
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

		public static bool IsWeaponCouchable(ItemObject weapon) {
			foreach (var weaponComponentData in weapon.Weapons) {
				Localization.TextObject)> flagDetails = CampaignUIHelper.GetFlagDetailsForWeapon(weaponComponentData,
					MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage));
				if (flagDetails.Any(flagDetail => !flagDetail.Item1.IsEmpty() && flagDetail.Item1.Contains("couchable")))
					return true;
			}

			return false;
			/*foreach (var weaponComponentData in weapon.Weapons) {
				var weaponDescriptionId = weaponComponentData.WeaponDescriptionId;
				if (weaponDescriptionId                                                      != null &&
					weaponDescriptionId.IndexOf("couch", StringComparison.OrdinalIgnoreCase) >= 0)
					return true;
			}

			return false;*/
		}

		public static bool IsSuitableForMount(ItemObject weapon) {
			foreach (var weaponComponentData in weapon.Weapons) {
				List<(string, TextObject)> flagDetails = CampaignUIHelper.GetFlagDetailsForWeapon(weaponComponentData,
					MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage));
				if (!flagDetails.Any(flagDetail =>
										 !flagDetail.Item1.IsEmpty() && flagDetail.Item1.Contains("cant_use_on_horseback")))
					return true;
			}

			return false;

			/*foreach (var weaponComponentData in weapon.Weapons) {
				var weaponDescriptionId = weaponComponentData.WeaponDescriptionId;
				if (weaponDescriptionId != null &&
					!MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage)
						   .HasFlag(ItemObject.ItemUsageSetFlags.RequiresNoMount))
					return true;
			}

			return false;*/
		}

		public static bool IsSuitableForInfantry(ItemObject weapon) {
			foreach (var weaponComponentData in weapon.Weapons) {
				// 获取当前武器组件的WeaponFlags
				var weaponFlags = weaponComponentData.WeaponFlags;

				// 使用HasFlag检查武器是否含有所需的任一WeaponFlag
				if ((weaponFlags.HasFlag(WeaponFlags.BonusAgainstShield)  ||
					 weaponFlags.HasFlag(WeaponFlags.CanKnockDown)        ||
					 weaponFlags.HasFlag(WeaponFlags.CanDismount)         ||
					 weaponFlags.HasFlag(WeaponFlags.MultiplePenetration) ||
					 IsWeaponBracable(weapon)) &&
					!IsWeaponCouchable(weapon))
					return true;
			}

			return false;
		}

		public static bool IsWeaponBracable(ItemObject weapon) {
			foreach (var weaponComponentData in weapon.Weapons) {
				List<(string, TextObject)> flagDetails = CampaignUIHelper.GetFlagDetailsForWeapon(weaponComponentData,
					MBItem.GetItemUsageSetFlags(weaponComponentData.ItemUsage));
				if (flagDetails.Any(flagDetail => !flagDetail.Item1.IsEmpty() && flagDetail.Item1.Contains("braceable")))
					return true;
				/*var weaponDescriptionId = weaponComponentData.WeaponDescriptionId;
	if (weaponDescriptionId                                                        != null &&
		weaponDescriptionId.IndexOf("bracing", StringComparison.OrdinalIgnoreCase) >= 0)
		return true;*/
			}

			return false;
		}

		public static bool HaveSameWeaponClass(List<WeaponClass> list1, List<WeaponClass> list2) {
			foreach (var weaponClass1 in list1)
				if (list2.Any(weaponClass2 => weaponClass2 == weaponClass1))
					return true;

			return false;
		}
	}