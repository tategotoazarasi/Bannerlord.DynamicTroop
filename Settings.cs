#region

using MCM.Abstractions.Base.Global;

#endregion

namespace Bannerlord.DynamicTroop;

internal class Settings : AttributeGlobalSettings<Settings> {
	public override string Id => "DynamicTroopSettings";

	public override string DisplayName => "Dynamic Troop";

	public override string FolderName => "DynamicTroop";

	public override string FormatType => "json";

	// 在这里添加您的设置项
	public bool MyToggle { get; set; } = true;
}