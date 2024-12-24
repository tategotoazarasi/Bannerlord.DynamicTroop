using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace DTES2;

public class DTESMissionLogic : MissionLogic {
	public override void AfterStart() {
		base.AfterStart();
		Logger.Instance.Information("AfterStart");
		if (!this.Mission.DoesMissionRequireCivilianEquipment              &&
			this.Mission.CombatType    == Mission.MissionCombatType.Combat &&
			Campaign.Current.MainParty != null                             &&
			MapEvent.PlayerMapEvent    != null) {
			foreach (PartyBase? party in MapEvent.PlayerMapEvent.InvolvedParties) {
				Logger.Instance.Information(party.Name.ToString());
			}
			// TODO
		}
	}

	// Token: 0x06000317 RID: 791 RVA: 0x0001B920 File Offset: 0x00019B20
	private void InitAgentLabel(Agent agent, Banner? banner) {
		if (banner == null) {
			return;
		}

		MetaMesh copy            = MetaMesh.GetCopy("troop_banner_selection", false, true);
		Material tableauMaterial = Material.GetFromResource("agent_label_with_tableau");
		Texture  texture         = banner.GetTableauTextureSmall(null);
		if (copy            != null &&
			tableauMaterial != null) {
			Texture  fromResource = Texture.GetFromResource("banner_top_of_head");
			Material material;

			tableauMaterial = tableauMaterial.CreateCopy();
			Action<Texture> action = delegate(Texture tex) {
										 tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap, tex);
									 };
			texture = banner.GetTableauTextureSmall(action);
			tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, fromResource);
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
		//this._agentMeshes.Add(agent, copy);
		//this.UpdateVisibilityOfAgentMesh(agent);
		//this.UpdateSelectionVisibility(agent, this._agentMeshes[agent], new bool?(false));
	}


	public override void OnAgentBuild(Agent agent, Banner banner) {
		base.OnAgentBuild(agent, banner);
		if (agent is { IsHuman: true, Character: not null }) {
			Logger.Instance.Information($"OnAgentBuild:{agent.Character.Name}");
			Equipment eq = agent.Character.Equipment.Clone();
			agent.UpdateSpawnEquipmentAndRefreshVisuals(eq);
			this.InitAgentLabel(agent, banner);
			agent.AgentVisuals.CheckResources(true);
		}
	}
}