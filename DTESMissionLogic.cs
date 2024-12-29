#region
using System.Collections.Concurrent;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
#endregion
namespace DTES2;

public class DTESMissionLogic : MissionLogic {
	private readonly Dictionary<MobileParty, ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>>> _tables =
		new Dictionary<MobileParty, ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>>>();
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
				ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>>
					table = armory.CreateDistributionTable();
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
		}
		else {
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
			if (agent.Origin is not PartyAgentOrigin pao) {
				return;
			}
			MobileParty? party = pao.Party?.MobileParty;
			if (party == null) {
				return;
			}
			if (!this._tables.TryGetValue(party,
										  out ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>> table)) {
				Logger.Instance.Warning("Table not found.");
				return;
			}
			if (agent.Character is not CharacterObject character) {
				return;
			}
			if (!table.TryGetValue(character, out ConcurrentBag<Equipment> bag)) {
				Logger.Instance.Warning("Bag not found.");
				return;
			}
			if (!bag.TryTake(out Equipment eq)) {
				return;
			}
			agent.UpdateSpawnEquipmentAndRefreshVisuals(eq);
			this.InitAgentLabel(agent, banner);
			_ = agent.AgentVisuals.CheckResources(true);
		}
	}
}