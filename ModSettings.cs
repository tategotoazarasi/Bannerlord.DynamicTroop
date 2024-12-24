#region
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
#endregion
namespace DTES2;

public class ModSettings {
	private bool _debugMode; // 默认禁用调试模式

	[SettingPropertyBool(
		"{=toggle_debug_mode}Debug Mode",
		Order = 1,
		RequireRestart = false,
		HintText = "{=toggle_debug_mode_hint}For Devs only.",
		IsToggle = true
	)]
	[SettingPropertyGroup("{=debug}Debug", GroupOrder = 1)]
	public bool DebugMode
	{
		get { return _debugMode; }
		set
		{
			if (_debugMode != value) {
				_debugMode = value;
			}
			if (_debugMode) {
				// TODO: test on
			}
			// TODO: test off
		}
	}
}