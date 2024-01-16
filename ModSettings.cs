using log4net.Core;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using MCM.Common;

namespace Bannerlord.DynamicTroop;

public class ModSettings : AttributeGlobalSettings<ModSettings> {
	private bool _debugMode; // 默认禁用调试模式

	public override string Id => "bannerlord.dynamictroop";

	public override string FormatType => "json";

	public override string DisplayName => LocalizedTexts.ModName.ToString();

	[SettingPropertyBool("{=same_cultural_preference}Same Cultural Preference",
						 Order = 1,
						 RequireRestart = false,
						 HintText =
							 "{=same_cultural_preference_hint}Soldiers will prefer weapons from their own culture but can still use other cultural weapons.")]
	[SettingPropertyGroup("{=settings}Settings", GroupOrder = 1)]
	public bool CulturalPreference { get; set; } = false;

	[SettingPropertyFloatingInteger("{=drop_rate}Drop Rate",
									0f,
									1f,
									"#0%",
									Order = 2,
									RequireRestart = false,
									HintText = "{=drop_rate_hint}Equipment drop rate on enemy down.")]
	[SettingPropertyGroup("{=settings}Settings", GroupOrder = 1)]
	public float DropRate { get; set; } = 1f;

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