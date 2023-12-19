#region

	using HarmonyLib;
	using TaleWorlds.Core;
	using TaleWorlds.Library;
	using TaleWorlds.MountAndBlade;

#endregion

	namespace Bannerlord.DynamicTroop;

	public class SubModule : MBSubModuleBase {
		protected override void OnSubModuleLoad() {
			base.OnSubModuleLoad();

			// 显示加载成功的消息
			InformationManager.DisplayMessage(new InformationMessage("Dynamic Troop loaded", Colors.Green));

			var harmony = new Harmony("com.bannerlord.mod.dynamic_troop");
			harmony.PatchAll();
		}

		protected override void OnSubModuleUnloaded() { base.OnSubModuleUnloaded(); }

		protected override void OnBeforeInitialModuleScreenSetAsRoot() { base.OnBeforeInitialModuleScreenSetAsRoot(); }

		public override void OnGameLoaded(Game game, object initializerObject) {
			base.OnGameLoaded(game, initializerObject);
			InformationManager.DisplayMessage(new InformationMessage("Hello World!", Colors.Green));
		}
	}