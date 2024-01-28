using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Bannerlord.DynamicTroop.Formatters;
using HarmonyLib;
using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Metalama.Patterns.Caching;
using Metalama.Patterns.Caching.Locking;
using SandBox.Tournaments.MissionLogics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop;

public class SubModule : MBSubModuleBase {
	public static ModSettings? Settings;

	protected override void OnSubModuleLoad() {
		base.OnSubModuleLoad();

		Harmony harmony = new("com.bannerlord.mod.dynamic_troop");
		harmony.PatchAll(Assembly.GetExecutingAssembly());

		// 获取 Mod 目录的路径
		var modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		// 构建 log4net 配置文件的完整路径
		var logConfigPath = Path.Combine(modDirectory, "log4net.config");

		// 设置日志文件的路径
		GlobalContext.Properties["LogDir"] = modDirectory;

		// 初始化 log4net
		var loggerRepository = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
		_ = XmlConfigurator.Configure(loggerRepository, new FileInfo(logConfigPath));

		// 动态设置日志文件路径
		var fileAppender = loggerRepository.GetAppenders().OfType<FileAppender>().FirstOrDefault();
		if (fileAppender != null) {
			fileAppender.File = Path.Combine(modDirectory, "log.txt");
			fileAppender.ActivateOptions(); // 应用新的配置
		}

		CachingService.Default = CachingService.Create(caching => {
														   _ = caching.ConfigureFormatters(formatters => {
															   formatters.AddFormatter(r =>
																   new
																	   EquipmentElementFormatter(r));
															   formatters.AddFormatter(r =>
																   new
																	   WeaponComponentDataFormatter(r));
														   });
														   _ = caching.AddProfile(new CachingProfile("profile") {
															   LockingStrategy =
																   new LocalLockingStrategy(),

															   //Capacity = -1, // 设置为-1表示无限空间
															   SlidingExpiration =
																   Timeout
																	   .InfiniteTimeSpan, // 设置滑动过期为无限
															   AbsoluteExpiration =
																   Timeout
																	   .InfiniteTimeSpan // 设置绝对过期为无限
														   });
													   });
	}

	protected override void OnSubModuleUnloaded() { base.OnSubModuleUnloaded(); }

	protected override void OnBeforeInitialModuleScreenSetAsRoot() {
		base.OnBeforeInitialModuleScreenSetAsRoot();
		Settings = ModSettings.Instance;
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
		/*if (game.GameType is Campaign)
			CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this,
																		  m => {
																			  if (m is Mission instance)
																				  instance
																					  .AddMissionBehavior(new
																						  MyMissionBehavior());
																		  });
		CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this,
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
		gameStarterObject?.AddBehavior(new EveryoneCampaignBehavior());
	}

	public override void OnMissionBehaviorInitialize(Mission mission) {
		if (mission.CombatType == Mission.MissionCombatType.Combat &&
			!mission.HasMissionBehavior<TournamentBehavior>()      &&
			!mission.HasMissionBehavior<CustomBattleAgentLogic>())
			mission.AddMissionBehavior(new DynamicTroopMissionLogic());

		base.OnMissionBehaviorInitialize(mission);
	}
}