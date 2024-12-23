#region
using System.Reflection;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.MBSubModuleBaseExtended;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Extensions.Logging;
using TaleWorlds.Library;
#endregion
namespace DTES2;

public class SubModule : MBSubModuleBaseEx {
	protected override void OnSubModuleLoad() {
		base.OnSubModuleLoad();
		var harmony = new Harmony("com.bannerlord.mod.dynamic_troop.v2");
		harmony.PatchAll(Assembly.GetExecutingAssembly());
#if DEBUG
		TestOn();
#endif
	}

	protected override void OnSubModuleUnloaded() {
		base.OnSubModuleUnloaded();

	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot() {
		base.OnBeforeInitialModuleScreenSetAsRoot();

	}

	public void TestOn() {
		Harmony.DEBUG     = true;
		FileLog.LogWriter = Logger.GetLogger("harmony");
		Debug.DebugManager.SetTestModeEnabled(true);
		this.GetServices().AddSingleton(_ => new SerilogLoggerProvider(Logger.GetLogger("butterlib"), true));
	}

	public void TestOff() {
		Harmony.DEBUG = false;
		Debug.DebugManager.SetTestModeEnabled(false);
	}
}