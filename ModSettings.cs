using log4net.Core;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;

namespace Bannerlord.DynamicTroop;

public class ModSettings : AttributeGlobalSettings<ModSettings> {
	private readonly bool _culturalPreference;

	private bool _debugMode; // 默认禁用调试模式

	private float _dropRate = 1f;

	private bool _randomizeNonHeroLedAiPartiesArmor;

	private bool _use_vanilla_looting_system;

	public override string Id => "bannerlord.dynamictroop";

	public override string FormatType => "json";

	public override string DisplayName => LocalizedTexts.ModName.ToString();

	/*[SettingPropertyBool("{=same_cultural_preference}Same Cultural Preference",
						 Order = 1,
						 RequireRestart = false,
						 HintText =
							 "{=same_cultural_preference_hint}Soldiers will prefer weapons from their own culture but can still use other cultural weapons.")]
	[SettingPropertyGroup("{=settings}Settings", GroupOrder = 1)]
	public bool CulturalPreference {
		get => _culturalPreference;
		set {
			if (_culturalPreference != value) {
				_culturalPreference = value;
				OnPropertyChanged();
			}
		}
	}*/

	[SettingPropertyFloatingInteger("{=drop_rate}Drop Rate",
									0f,
									1f,
									"#0%",
									Order = 2,
									RequireRestart = false,
									HintText = "{=drop_rate_hint}Equipment drop rate on enemy down.")]
	[SettingPropertyGroup("{=settings}Settings", GroupOrder = 1)]
	public float DropRate {
		get => _dropRate;
		set {
			_dropRate = value;
			OnPropertyChanged();
		}
	}

	[SettingPropertyDropdown("{=difficulty}Difficulty",
							 Order = 3,
							 RequireRestart = false,
							 HintText =
								 "{=difficulty_hint}Adjusts the quantity and quality of daily equipment received by all AI troops.")]
	[SettingPropertyGroup("{=settings}Settings", GroupOrder = 1)]
	public Dropdown<string> Difficulty { get; set; } = new(new[] {
																	 LocalizedTexts.SettingEasy.ToString(),
																	 LocalizedTexts.SettingNormal.ToString(),
																	 LocalizedTexts.SettingHard.ToString(),
																	 LocalizedTexts.SettingVeryHard.ToString()
																 },
														   1);

	[SettingPropertyBool("{=randomized_non_hero_led_ai_parties_armor}Randomize Non-Hero-Led AI Parties Armor",
						 Order = 4,
						 RequireRestart = false,
						 HintText =
							 "{=randomized_non_hero_led_ai_parties_armor_hint}Soldiers in AI parties without armory system will be equipped with randomized armor in battle, determined by their tier and cultural origins.")]
	[SettingPropertyGroup("{=settings}Settings", GroupOrder = 1)]
	public bool RandomizeNonHeroLedAiPartiesArmor {
		get => _randomizeNonHeroLedAiPartiesArmor;
		set {
			if (_randomizeNonHeroLedAiPartiesArmor != value) {
				_randomizeNonHeroLedAiPartiesArmor = value;
				OnPropertyChanged();
			}
		}
	}

	[SettingPropertyBool("{=use_vanilla_looting_system}Use Vanilla Looting System",
						 Order = 5,
						 RequireRestart = false,
						 HintText =
							 "{=use_vanilla_looting_system_hint}Enable this option to use the game's default looting system instead of looting from the enemy's armory.")]
	[SettingPropertyGroup("{=settings}Settings", GroupOrder = 1)]
	public bool UseVanillaLootingSystem {
		get => _use_vanilla_looting_system;
		set {
			if (_use_vanilla_looting_system != value) {
				_use_vanilla_looting_system = value;
				OnPropertyChanged();
			}
		}
	}

	[SettingPropertyBool("{=toggle_debug_mode}Debug Mode",
						 Order = 1,
						 RequireRestart = false,
						 HintText = "{=toggle_debug_mode_hint}For Devs only.",
						 IsToggle = true)]
	[SettingPropertyGroup("{=debug}Debug", GroupOrder = 2)]
	public bool DebugMode {
		get => _debugMode;
		set {
			if (_debugMode != value) {
				_debugMode = value;
				OnPropertyChanged();
			}
		}
	}

	[SettingPropertyDropdown("{=log_level}Log Level", Order = 2, RequireRestart = false, HintText = "")]
	[SettingPropertyGroup("{=debug}Debug")]
	public Dropdown<Level> LogLevel { get; set; } = new(new[] {
																  Level.Debug,
																  Level.Info,
																  Level.Warn,
																  Level.Error,
																  Level.Fatal,
																  Level.All
															  },
														5);
}