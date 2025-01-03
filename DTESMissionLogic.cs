using System.Collections.Concurrent;
using System.Collections.Generic;
using DTES2.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;

namespace DTES2;

public class DTESMissionLogic : MissionLogic {
	/// <summary>
	///     记录所有参战部队 (MobileParty) 对应的分配表。 在 OnAgentBuild 时需要拿到表中的装备分给 Agent。
	/// </summary>
	private readonly Dictionary<MobileParty, ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>>>
		_tables = [];

	public override void AfterStart() {
		base.AfterStart();
		Logger.Instance.Information("AfterStart");

		// 判断此场战斗是否属于可分配装备的类型
		if (!this.Mission.DoesMissionRequireCivilianEquipment              &&
			this.Mission.CombatType    == Mission.MissionCombatType.Combat &&
			Campaign.Current.MainParty != null                             &&
			MapEvent.PlayerMapEvent    != null) {
			foreach (PartyBase? party in MapEvent.PlayerMapEvent.InvolvedParties) {
				if (party == null) {
					Logger.Instance.Warning("party is null.");
					continue;
				}

				Logger.Instance.Information(party.Name.ToString());
				MobileParty? mobileParty = party.MobileParty;
				if (mobileParty == null) {
					Logger.Instance.Warning($"mobileParty not found for {party.Name}.");
					continue;
				}

				// 获取对应的 Armory
				Armory? armory = GlobalArmories.GetArmory(mobileParty);
				if (armory == null) {
					Logger.Instance.Warning($"Armory not found for {mobileParty.Name}.");
					continue;
				}

				// 使用 DistrubutionTable 来完成分配
				DistrubutionTable distributionTable = new(armory);
				distributionTable.RefreshTable(); // 核心分配逻辑（原先在 Armory.CreateDistributionTable 中的内容）
				distributionTable.DebugPrint();
				armory.DebugPrint();

				// 将分配后的结果存入 _tables
				this._tables.Add(mobileParty, distributionTable.Table);
			}
		}
	}

	/// <summary>
	///     初始化 Agent 的标签（如头顶旗帜等）。
	/// </summary>
	/// <param name="agent">  要设置标签的 Agent。 </param>
	/// <param name="banner"> 旗帜。 </param>
	private void InitAgentLabel(Agent agent, Banner? banner) {
		if (agent.IsHuman &&
			banner != null) {
			MetaMesh copy            = MetaMesh.GetCopy("troop_banner_selection", false, true);
			Material tableauMaterial = Material.GetFromResource("agent_label_with_tableau");
			Texture  texture         = banner.GetTableauTextureSmall(null);
			if (copy            != null &&
				tableauMaterial != null) {
				Texture fromResource = Texture.GetFromResource("banner_top_of_head");

				tableauMaterial = tableauMaterial.CreateCopy();

				void action(Texture tex) {
					tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap, tex);
				}

				texture = banner.GetTableauTextureSmall(action);
				tableauMaterial.SetTexture(Material.MBTextureType.DiffuseMap2, fromResource);

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
		}
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

			// 只有 PartyGroupAgentOrigin 才能拿到对应的 MobileParty
			if (agent.Origin is not PartyGroupAgentOrigin pgao) {
				Logger.Instance.Warning($"{agent.Character.Name} do not have a PartyGroupAgentOrigin");
				return;
			}

			MobileParty? party = pgao.Party?.MobileParty;
			if (party == null) {
				Logger.Instance.Warning($"Cannot find party for {agent.Character.Name}");
				return;
			}

			// 到这里我们可以根据 MobileParty 找到对应的分配表（_tables）
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

			// 从该表里拿到角色对应的装备包
			if (!table.TryGetValue(character, out ConcurrentBag<Equipment>? bag)) {
				Logger.Instance.Warning("Bag not found.");
				return;
			}

			// 如果还能拿到一套装备，就替换当前 Agent 的装备
			if (!bag.TryTake(out Equipment? eq)) {
				Logger.Instance.Warning("Cannot find equipment.");
				return;
			}

			EquipmentElement weapon0        = agent.SpawnEquipment[EquipmentIndex.Weapon0];
			float            effectiveness1 = weapon0.Item.Effectiveness;
			if (agent.Character is CharacterObject co) {
				float effectiveness3 = weapon0.CalculateEffectiveness(co);
				Logger.Instance.Information(
					$"Character:{
						co.Name
					} Weapon:{
						weapon0.GetModifiedItemName()
					} i:{
						effectiveness1
					}, ec:{
						effectiveness3
					}"
				);
			}

			agent.UpdateSpawnEquipmentAndRefreshVisuals(eq);
			this.InitAgentLabel(agent, banner);
		}
	}
}