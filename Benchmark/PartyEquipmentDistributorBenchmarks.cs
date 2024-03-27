using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.DynamicTroop.Extensions;
using BenchmarkDotNet.Attributes;
using Moq;
using SandBox.Missions.MissionLogics;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.DynamicTroop.Benchmark;

//[ThreadingDiagnoser]
//[MemoryDiagnoser]
[RPlotExporter]
public class PartyEquipmentDistributorBenchmarks {
	private Dictionary<ItemObject, int> armory;
	private MobileParty                 party;

	[GlobalSetup]
	public void Setup() {
		try {
			Global.Debug("start setup PartyEquipmentDistributorBenchmarks");
			party   = MobileParty.AllLordParties.Where(party => party.IsValid() && EveryoneCampaignBehavior.PartyArmories.ContainsKey(party.Id)).GetRandomElementInefficiently();
			armory  = EveryoneCampaignBehavior.PartyArmories[party.Id];
			Global.Debug("end setup PartyEquipmentDistributorBenchmarks");
		}
		catch (Exception e) {
			Global.Error(e.ToString());
			throw e;
		}
	}

	[Benchmark]
	public void NonParallel() {
		PartyEquipmentDistributor1 instance = new(party,armory);
	}

	[Benchmark]
	public void Parallel() {
		PartyEquipmentDistributor instance = new( party, armory);
		instance.Run();
	}
}