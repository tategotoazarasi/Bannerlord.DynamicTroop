#region
using System.Reflection;
using Bannerlord.ButterLib.Common.Extensions;
using Bannerlord.ButterLib.MBSubModuleBaseExtended;
using HarmonyLib;
using Microsoft.Extensions.DependencyInjection;
using SandBox.Tournaments.MissionLogics;
using Serilog.Extensions.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
#endregion
namespace DTES2;

public class SubModule : MBSubModuleBaseEx {
	private readonly Harmony harmony = new Harmony("com.bannerlord.mod.dynamic_troop.v2");

	protected override void OnSubModuleLoad() {
		base.OnSubModuleLoad();
		harmony.PatchAll(Assembly.GetExecutingAssembly());
#if DEBUG
		TestOn();
#endif
	}

	protected override void OnSubModuleUnloaded() {
		base.OnSubModuleUnloaded();
		harmony.UnpatchAll();
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot() {
		base.OnBeforeInitialModuleScreenSetAsRoot();
	}

	protected override void OnGameStart(Game game, IGameStarter gameStarterObject) {
		base.OnGameStart(game, gameStarterObject);
		if (game.GameType is Campaign && gameStarterObject is CampaignGameStarter gso) {
			gso.AddBehavior(new DTESCampaignBehavior());
		}
	}

	public override void OnMissionBehaviorInitialize(Mission mission) {
		if (
			mission.CombatType == Mission.MissionCombatType.Combat && !mission.HasMissionBehavior<TournamentBehavior>() && !mission.HasMissionBehavior<CustomBattleAgentLogic>()
			)
			mission.AddMissionBehavior(new DTESMissionLogic());

		base.OnMissionBehaviorInitialize(mission);
	}

	public void TestOn() {
		Harmony.DEBUG     = true;
		FileLog.LogWriter = Logger.GetLogger("harmony");
		Debug.DebugManager.SetTestModeEnabled(true);
		this
			.GetServices()
			.AddSingleton(_ => new SerilogLoggerProvider(Logger.GetLogger("butterlib"), true));
	}

	public void TestOff() {
		Harmony.DEBUG = false;
		Debug.DebugManager.SetTestModeEnabled(false);
	}
}