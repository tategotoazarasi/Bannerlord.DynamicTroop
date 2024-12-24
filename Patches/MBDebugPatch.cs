#region
using HarmonyLib;
using TaleWorlds.Engine;
using TaleWorlds.Library;
#endregion
namespace DTES2.Patches;

[HarmonyPatch(typeof(MBDebug))]
internal class MBDebugPatch {
	private static readonly Logger _logger = Logger.GetLogger("mbdebug");

	[HarmonyPrefix]
	[HarmonyPatch(nameof(MBDebug.Print))]
	private static bool PrefixPrint(
		string           message,
		int              logLevel,
		Debug.DebugColor color,
		ulong            debugFilter
	) {
		_logger.Debug(message);
		return false;
	}
}