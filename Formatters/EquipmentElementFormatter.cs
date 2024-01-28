using Flashtrace.Formatters;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Formatters;

internal class EquipmentElementFormatter : Formatter<EquipmentElement> {
	public EquipmentElementFormatter(IFormatterRepository repository) : base(repository) { }

	public override void Format(UnsafeStringBuilder stringBuilder, EquipmentElement value) {
		_ = stringBuilder.Append("EquipmentElement");
		if (value.Item != null) _ = stringBuilder.Append(value.Item.StringId);

		if (value.ItemModifier != null) _ = stringBuilder.Append(value.ItemModifier.StringId);
	}
}