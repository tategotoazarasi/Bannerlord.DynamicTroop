#region

	using HarmonyLib;
	using TaleWorlds.CampaignSystem;
	using TaleWorlds.Core;

#endregion

	namespace Bannerlord.DynamicTroop;

	[HarmonyPatch(typeof(CampaignEvents), "OnMissionStarted")]
	public class MissionStartPatch {
		private static void Prefix(IMission mission) {
			AgentDeathLootPatch.LootedItems.Clear();
			AgentDeathLootPatch.ProcessedAgents.Clear();
		}
	}