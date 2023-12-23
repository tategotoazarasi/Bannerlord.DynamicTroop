#region

	using MCM.Abstractions.Attributes;
	using MCM.Abstractions.Attributes.v2;
	using MCM.Abstractions.Base.Global;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class ModSettings : AttributeGlobalSettings<ModSettings> {
		private bool _debugMode; // 默认禁用调试模式

		public override string Id => "bannerlord.dynamictroop";

		public override string FormatType => "json";

		public override string DisplayName => "Dynamic Troop";

		[SettingPropertyBool("Debug Mode", Order = 1, RequireRestart = false, HintText = "Debug mode. For Devs only.")]
		[SettingPropertyGroup("General")]
		public bool DebugMode {
			get => _debugMode;
			set {
				if (_debugMode != value) {
					_debugMode = value;
					OnPropertyChanged();
				}
			}
		}
	}