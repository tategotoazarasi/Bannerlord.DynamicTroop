#region

using log4net.Core;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

#endregion

namespace DynamicTroopEquipmentReupload;

public class ModSettings : AttributeGlobalSettings<ModSettings> {
	private readonly bool _culturalPreference;

	public override string Id => "DynamicTroopSettings";

	public override string DisplayName => "Dynamic Troop";

	public override string FolderName => "DynamicTroop";

	public override string FormatType => "json2";

	[SettingPropertyBool("{=debug_mode}Debug mode", RequireRestart = false, HintText = "{=debug_mode_description}Enables detailed debug logs for troubleshooting.", Order = 1)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool DebugMode { get; set; }

	[SettingPropertyFloatingInteger("{=drop_rate}Drop Rate", 0, 5, "#0.00", RequireRestart = false, HintText = "{=drop_rate_description}Adjusts the drop rate multiplier for looted equipment.", Order = 2)]
	[SettingPropertyGroup("{=settings}Settings")]
	public float DropRate { get; set; } = 1f;

	[SettingPropertyFloatingInteger("{=difficulty}Difficulty", 0, 5, "#0.00", RequireRestart = false, HintText = "{=difficulty_description}Adjusts the difficulty multiplier for equipment assignment.", Order = 3)]
	[SettingPropertyGroup("{=settings}Settings")]
	public float Difficulty { get; set; } = 1f;

	[SettingPropertyBool("{=randomize_ai_equipment}Randomize non-hero-led party",
						 RequireRestart = false,
						 HintText = "{=randomize_ai_equipment_description}If enabled, agents whose parties are not processed by the armory system may receive randomized equipment.",
						 Order = 4)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool RandomizeNonHeroLedAiPartiesArmor { get; set; }

	[SettingPropertyBool("{=use_vanilla_looting_system}Use vanilla looting system", RequireRestart = false, HintText = "{=use_vanilla_looting_system_description}Use Bannerlord's vanilla loot calculation instead of the armory-based loot.", Order = 5)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool UseVanillaLootingSystem { get; set; }

	//RecruitmentPatch now it actually randomizes recruit start gear
	[SettingPropertyBool("{=randomize_starting_equipment}Randomize recruit starting equipment",
						 RequireRestart = false,
						 HintText = "{=randomize_starting_equipment_description}If enabled, recruits may receive randomized starting equipment when they are recruited.",
						 Order = 6)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool RandomizeStartingEquipment { get; set; } = true;

	[SettingPropertyBool("{=remove_civilian_equipments}Remove civilian equipments in random",
						 RequireRestart = false,
						 HintText = "{=remove_civilian_equipments_description}If enabled, civilian equipment will be excluded from random equipment selection.",
						 Order = 7)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool RemoveCivilianEquipmentsInRandom { get; set; } = true;

	[SettingPropertyBool("{=loyal_equipments}Loyal Equipments",
						 RequireRestart = false,
						 HintText =
							 "{=loyal_equipments_desc}ON (default): Soldiers will prioritize their vanilla equipments and the closest equipment to their vanilla equipments, up to +2 tier of what they had before. OFF: Soldiers will take the best gear possible, following the +2 tier rule.",
						 Order = 8)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool PreferDefaultEquipmentThenClosest { get; set; } = true;

	[SettingPropertyBool("{=emergency_loadout}Emergency Loadout",
						 RequireRestart = false,
						 HintText = "{=emergency_loadout_desc}ON (default): If the soldiers are going to be missing a weapon/armor at the beginning of the battle, they will have it from a t1 unit of their culture.",
						 Order = 9)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool EnableEmergencyLoadout { get; set; } = true;

	[SettingPropertyBool("{=underequipped}Underequipped", RequireRestart = false, HintText = "{=underequipped_desc}ON (default): If soldiers have a worse overall gear than what they had by default, they will lose morale.", Order = 10)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool Underequipped { get; set; } = true;

	[SettingPropertyBool("{=commanders_greed}Commander's Greed", RequireRestart = false, HintText = "{=commanders_greed_desc}OFF (default): the Player can't take stuff from the troop equipment pool.", Order = 11)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool CommandersGreed { get; set; }

	[SettingPropertyInteger("{=scrap_cap_per_category}Scrap cap per category",
							200,
							3500,
							RequireRestart = false,
							HintText = "{=scrap_cap_per_category_desc}Only active when Commander's Greed is OFF. Every 3 days, if any item category in the troop stash exceeds this cap, the lowest-value items are deleted until the category drops to (cap - 1).",
							Order = 11)]
	[SettingPropertyGroup("{=settings}Settings")]
	public int ScrapCapPerCategory { get; set; } = 600;


	[SettingPropertyBool("{=assign_extra_equipments}Assign Extra Equipments", RequireRestart = false, HintText = "{=assign_extra_equipments_description}If enabled, leftover equipment will be distributed to fill empty slots when possible.", Order = 12)]
	[SettingPropertyGroup("{=settings}Settings")]
	public bool AssignExtraEquipments { get; set; } = true;

	[SettingPropertyInteger("{=log_level}Log Level", 0, 5, RequireRestart = false, HintText = "{=log_level_description}0=Debug, 1=Info, 2=Warn, 3=Error, 4=Fatal, 5=All", Order = 13)]
	[SettingPropertyGroup("{=settings}Settings")]
	public int LogLevelIndex { get; set; }

	public Level MinimumLogLevel => GetMinimumLogLevel(LogLevelIndex);

	private static Level GetMinimumLogLevel(int index) {
		// 0=Debug, 1=Info, 2=Warn, 3=Error, 4=Fatal, 5=All
		return index switch {
				   0 => Level.Debug,
				   1 => Level.Info,
				   2 => Level.Warn,
				   3 => Level.Error,
				   4 => Level.Fatal,
				   _ => Level.All
			   };
	}

	private void LogLevelDropdownChanged(int selectedIndex) {
		LogLevelIndex = selectedIndex;
	}
}