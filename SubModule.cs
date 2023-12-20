#region

using HarmonyLib;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

#endregion

namespace Bannerlord.DynamicTroop;

public class SubModule : MBSubModuleBase {
	protected override void OnSubModuleLoad() {
		base.OnSubModuleLoad();

		Harmony harmony = new Harmony("com.bannerlord.mod.dynamic_troop");
		harmony.PatchAll();
	}

	protected override void OnSubModuleUnloaded() { base.OnSubModuleUnloaded(); }

	protected override void OnBeforeInitialModuleScreenSetAsRoot() { base.OnBeforeInitialModuleScreenSetAsRoot(); }

	public override void OnGameLoaded(Game game, object initializerObject) {
		base.OnGameLoaded(game, initializerObject);
	}

	public override void OnNewGameCreated(Game game, object initializerObject) {
		base.OnNewGameCreated(game, initializerObject);
		Campaign.Current.CampaignBehaviorManager.AddBehavior(new ArmyArmoryBehavior());
	}

	public override void OnCampaignStart(Game game, object starterObject) { base.OnCampaignStart(game, starterObject); }

	public override void BeginGameStart(Game game) { base.BeginGameStart(game); }

	public override void OnGameInitializationFinished(Game game) { base.OnGameInitializationFinished(game); }
}