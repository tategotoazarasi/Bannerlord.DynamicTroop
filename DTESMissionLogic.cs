using System.Collections.Concurrent;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace DTES2;

public class DTESMissionLogic : MissionLogic {
	private readonly Dictionary<MobileParty, ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>>>
		_tables = [];

	public override void AfterStart() {
		base.AfterStart();
		Logger.Instance.Information("AfterStart");
		if (!this.Mission.DoesMissionRequireCivilianEquipment              &&
			this.Mission.CombatType    == Mission.MissionCombatType.Combat &&
			Campaign.Current.MainParty != null                             &&
			MapEvent.PlayerMapEvent    != null) {
			foreach (PartyBase? party in MapEvent.PlayerMapEvent.InvolvedParties) {
				if (party == null) {
					continue;
				}

				Logger.Instance.Information(party.Name.ToString());
				MobileParty? mobileParty = party.MobileParty;
				if (mobileParty == null) {
					continue;
				}

				Armory? armory = GlobalArmories.GetArmory(mobileParty);
				if (armory == null) {
					continue;
				}

				ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>> table =
					armory.CreateDistributionTable();
				this._tables.Add(mobileParty, table);
			}
		}
	}

	// Token: 0x06000317 RID: 791 RVA: 0x0001B920 File Offset: 0x00019B20
	private void InitAgentLabel(Agent agent, Banner? banner) {
		if (banner == null) {
			return;
		}

		MetaMesh copy            = MetaMesh.GetCopy("troop_banner_selection", false, true);
		Material tableauMaterial = Material.GetFromResource("agent_label_with_tableau");
		if (copy            != null &&
			tableauMaterial != null) {
			Texture fromResource = Texture.GetFromResource("banner_top_of_head");
			tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, fromResource);
		} else {
			return;
		}

		copy.SetMaterial(tableauMaterial);
		copy.SetVectorArgument(
			0.5f,
			0.5f,
			0.25f,
			0.25f
		);
		copy.SetVectorArgument2(
			30f,
			0.4f,
			0.44f,
			-1f
		);
		agent.AgentVisuals.AddMultiMesh(copy, BodyMeshTypes.Label);
	}

	public override void OnAgentBuild(Agent agent, Banner banner) {
		base.OnAgentBuild(agent, banner);
		if (agent is { IsHuman: true, Character: not null }) {
			Logger.Instance.Information($"OnAgentBuild:{agent.Character.Name}");
			if (agent.Origin is BasicBattleAgentOrigin) {
				Logger.Instance.Debug($"{agent.Character.Name} have a BasicBattleAgentOrigin");
			}

			if (agent.Origin is CustomBattleAgentOrigin) {
				Logger.Instance.Debug($"{agent.Character.Name} have a CustomBattleAgentOrigin");
			}

			if (agent.Origin is PartyAgentOrigin) {
				Logger.Instance.Debug($"{agent.Character.Name} have a PartyAgentOrigin");
			}

			if (agent.Origin is PartyGroupAgentOrigin) {
				Logger.Instance.Debug($"{agent.Character.Name} have a PartyGroupAgentOrigin");
			}

			if (agent.Origin is SimpleAgentOrigin) {
				Logger.Instance.Debug($"{agent.Character.Name} have a SimpleAgentOrigin");
			}

			if (agent.Origin is not PartyGroupAgentOrigin pgao) {
				Logger.Instance.Warning($"{agent.Character.Name} do not have a SimpleAgentOrigin");
				return;
			}

			MobileParty? party = pgao.Party?.MobileParty;
			if (party == null) {
				Logger.Instance.Warning($"Cannot find party for {agent.Character.Name}");
				return;
			}

			if (!this._tables.TryGetValue(
					party,
					out ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>>? table
				)) {
				Logger.Instance.Warning("Table not found.");
				return;
			}

			if (agent.Character is not CharacterObject character) {
				Logger.Instance.Warning($"{agent.Character.Name} is not a CharacterObject");
				return;
			}

			if (!table.TryGetValue(character, out ConcurrentBag<Equipment>? bag)) {
				Logger.Instance.Warning("Bag not found.");
				return;
			}

			if (!bag.TryTake(out Equipment? eq)) {
				return;
			}

			agent.UpdateSpawnEquipmentAndRefreshVisuals(eq);
			this.InitAgentLabel(agent, banner);
			_ = agent.AgentVisuals.CheckResources(true);
		}
	}
}