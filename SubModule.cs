#region

	using HarmonyLib;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class SubModule : MBSubModuleBase {
		public static ModSettings? settings;

		protected override void OnSubModuleLoad() {
			base.OnSubModuleLoad();

			Harmony harmony = new("com.bannerlord.mod.dynamic_troop");
			harmony.PatchAll();
		}

		protected override void OnSubModuleUnloaded() { base.OnSubModuleUnloaded(); }

		protected override void OnBeforeInitialModuleScreenSetAsRoot() {
			base.OnBeforeInitialModuleScreenSetAsRoot();
			settings = ModSettings.Instance;
		}

		public override void OnGameLoaded(Game game, object initializerObject) {
			base.OnGameLoaded(game, initializerObject);
		}

		public override void OnNewGameCreated(Game game, object initializerObject) {
			base.OnNewGameCreated(game, initializerObject);

			//Campaign.Current.CampaignBehaviorManager.AddBehavior(new ArmyArmoryBehavior());
		}

		public override void OnCampaignStart(Game game, object starterObject) { base.OnCampaignStart(game, starterObject); }

		public override void BeginGameStart(Game game) {
			base.BeginGameStart(game);
			if (game.GameType is Campaign)
				CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this,
																			  m => {
																				  if (m is Mission instance)
																					  instance
																						  .AddMissionBehavior(new
																							  MyMissionBehavior());
																			  });
			/*CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this,
																	m => {
																	});*/
		}

		public override void OnGameInitializationFinished(Game game) { base.OnGameInitializationFinished(game); }

		protected override void OnGameStart(Game game, IGameStarter gameStarterObject) {
			base.OnGameStart(game, gameStarterObject);
			if (game.GameType is Campaign) AddBehaviors(gameStarterObject as CampaignGameStarter);
		}

		private void AddBehaviors(CampaignGameStarter gameStarterObject) {
			gameStarterObject?.AddBehavior(new ArmyArmoryBehavior());
		}
	}