using Flashtrace.Formatters;
using TaleWorlds.Core;

namespace Bannerlord.DynamicTroop.Formatters;

internal class WeaponComponentDataFormatter : Formatter<WeaponComponentData> {
	public WeaponComponentDataFormatter(IFormatterRepository repository) : base(repository) { }

	public override void Format(UnsafeStringBuilder stringBuilder, WeaponComponentData? value) {
		_ = stringBuilder.Append("WeaponComponentData");
		if (value == null) return;
		_ = stringBuilder.Append(JsonSerializerHasher.Serialize(value));
	}
}