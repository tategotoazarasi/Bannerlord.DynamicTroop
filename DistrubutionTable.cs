#region
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
#endregion
namespace DTES2;

/// <summary>
///     用于根据 Armory 中的装备，为一支队伍（Party）中的各角色分配装备。
///     线程安全、多线程并行处理分配逻辑。
/// </summary>
public class DistrubutionTable {
	private readonly Armory _armory;

	/// <summary>
	///     分配完成后的结果表：
	///     Key: 角色（CharacterObject）
	///     Value: 该角色拥有的全部装备（一个并发安全的包）。
	/// </summary>
	private readonly ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>> _table =
		new ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>>();

	public DistrubutionTable(Armory armory) => this._armory = armory;

	/// <summary>
	///     提供只读访问，用于让外部获取分配结果。
	/// </summary>
	public ConcurrentDictionary<CharacterObject, ConcurrentBag<Equipment>> Table => this._table;

	/// <summary>
	///     重新生成（刷新）分配表。
	///     会清空当前表，然后基于 Armory 和对应的 Party 成员重新计算并分配装备。
	/// </summary>
	public void RefreshTable() {
		this._table.Clear();

		MBList<TroopRosterElement>? troopRoster = this._armory.Party.MemberRoster.GetTroopRoster();
		if (troopRoster == null) {
			// 如果没有有效的成员名单，则无法做分配，直接返回。
			return;
		}

		// 1. 根据当前队伍的角色数量，为 _table 初始化。
		troopRoster.
			AsParallel().
			ForAll(element => {
				CharacterObject? character     = element.Character;
				int              healthyNumber = element.Number - element.WoundedNumber;
				if (healthyNumber > 0) {
					// 为该角色创建一个 ConcurrentBag<Equipment>
					this._table[character] = new ConcurrentBag<Equipment>();
					_ = Parallel.For(
						0,
						healthyNumber,
						_ => {
							// 给每个“健康”的 Troop 预先放一个空 Equipment
							this._table[character].Add(new Equipment());
						}
					);
				}
			});

		// 2. 收集可分配的各类防具装备：头盔、身体、手、腿、披风等
		ConcurrentDictionary<ItemObject.ItemTypeEnum, ConcurrentBag<EquipmentElement>> itemBags
			= new ConcurrentDictionary<ItemObject.ItemTypeEnum, ConcurrentBag<EquipmentElement>>();

		this.
			_armory.
			GetAllEquipmentElements() // 获取全部的装备元素
			.
			AsParallel().
			ForAll(element => {
				switch (element.Item.ItemType) {
					case ItemObject.ItemTypeEnum.BodyArmor:
					case ItemObject.ItemTypeEnum.HeadArmor:
					case ItemObject.ItemTypeEnum.HandArmor:
					case ItemObject.ItemTypeEnum.ChestArmor:
					case ItemObject.ItemTypeEnum.LegArmor:
					case ItemObject.ItemTypeEnum.Cape:
						if (!itemBags.ContainsKey(element.Item.ItemType)) {
							_ = itemBags.TryAdd(element.Item.ItemType, new ConcurrentBag<EquipmentElement>());
						}

						itemBags[element.Item.ItemType].Add(element);
						break;
				}
			});

		// 3. 将这些防具根据功效排序后，依次分配给表中的每个角色
		itemBags.
			AsParallel().
			ForAll(kv => {
				// 取出当前一类防具
				List<EquipmentElement> sorted = kv.Value.AsParallel().OrderBy(item => item.Item.Effectiveness).ToList();

				int i = 0;
				foreach (KeyValuePair<CharacterObject, ConcurrentBag<Equipment>> pair in this._table) {
					ConcurrentBag<Equipment> bag = pair.Value;
					foreach (Equipment eq in bag) {
						if (i >= sorted.Count) {
							// 如果该类防具已经分配完，就退出循环
							return;
						}

						EquipmentIndex slot = EquipmentIndex.None;
						switch (sorted[i].Item.ItemType) {
							case ItemObject.ItemTypeEnum.BodyArmor: slot = EquipmentIndex.Body; break;

							case ItemObject.ItemTypeEnum.HeadArmor: slot = EquipmentIndex.Head; break;

							case ItemObject.ItemTypeEnum.HandArmor: slot = EquipmentIndex.Gloves; break;

							case ItemObject.ItemTypeEnum.ChestArmor:
								// TODO: 根据具体游戏逻辑来分配。暂时留空。
								break;

							case ItemObject.ItemTypeEnum.LegArmor: slot = EquipmentIndex.Leg; break;

							case ItemObject.ItemTypeEnum.Cape: slot = EquipmentIndex.Cape; break;
						}

						if (slot != EquipmentIndex.None) {
							eq[slot] = sorted[i];
						}

						i++;
						// 如果还想让更多角色也共享同一物品，可以在这里加特殊逻辑，
						// 例如不递增 i；否则就是一个物品给一个角色。
					}
				}
			});
	}

	/// <summary>
	///     Debug 调试输出当前分配表的情况。
	///     每个角色会对应一批装备。
	/// </summary>
	public void DebugPrint() {
		foreach (KeyValuePair<CharacterObject, ConcurrentBag<Equipment>> kvp in this._table) {
			CharacterObject          character    = kvp.Key;
			ConcurrentBag<Equipment> equipmentBag = kvp.Value;
			Logger.Instance.Information($"=== Character: {character.Name} ===");
			Logger.Instance.Information($"Equipment count: {equipmentBag.Count}");

			// 这里如果需要更详细信息，可以遍历每一个 Equipment 的每个 Slot。
			// 先简单打印每个 Equipment 的概览。
			foreach (Equipment eq in equipmentBag) {
				string eqInfo = "Slots -> [";
				for (EquipmentIndex i = EquipmentIndex.Head; i <= EquipmentIndex.Cape; i++) {
					EquipmentElement e = eq.GetEquipmentFromSlot(i);
					if (e.Item != null) {
						eqInfo += $" {i}:{e.Item.Name},";
					}
				}

				eqInfo =  eqInfo.TrimEnd(',');
				eqInfo += " ]";
				Logger.Instance.Information(eqInfo);
			}
		}
	}
}