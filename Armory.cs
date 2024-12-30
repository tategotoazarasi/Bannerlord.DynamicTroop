#region
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
#endregion
namespace DTES2;

[Serializable]
public class Armory {
	private readonly ConcurrentDictionary<EquipmentElement, int> _data =
		new ConcurrentDictionary<EquipmentElement, int>();

	private readonly MobileParty? _party;

	public Armory() { }

	public Armory(MobileParty party) => this._party = party;

	public MobileParty Party => this._party ?? MobileParty.MainParty;

	/// <summary>
	///     将 Roster 中的所有物品信息加载进 Armory。
	/// </summary>
	/// <param name="roster">一个包含物品的 Roster 集合。</param>
	public void FillFromRoster(IEnumerable<ItemRosterElement> roster) {
		this._data.Clear();
		roster.
			AsParallel().
			ForAll(element => {
				this.Store(element.EquipmentElement, element.Amount);
			});
	}

	/// <summary>
	///     存储或移除某个装备元素。
	///     例如：Store(equipmentElement, 2) 表示向 Armory 增加此装备 2 个；
	///     Store(equipmentElement, -1) 表示移除此装备 1 个。
	/// </summary>
	/// <param name="equipmentElement">要存储的装备元素。</param>
	/// <param name="amount">数量（可以为负）。</param>
	public void Store(EquipmentElement equipmentElement, int amount = 1) {
		_ = this._data.TryGetValue(equipmentElement, out int currentAmount);
		if (currentAmount + amount < 0) {
			Logger.Instance.Warning("Armory.Store: Attempted to store negative amount of equipment.");
			return;
		}

		_ = this._data.AddOrUpdate(
			equipmentElement,
			amount,
			(equipment, count) => count + amount
		);
	}

	/// <summary>
	///     存储或移除某个 ItemObject。
	/// </summary>
	/// <param name="item">要存储的物品。</param>
	/// <param name="amount">数量（可正可负）。</param>
	public void Store(ItemObject item, int amount = 1) {
		if (amount >= 0) {
			EquipmentElement equipment = new EquipmentElement(item);
			this.Store(equipment, amount);
		}

		// TODO: remove item from armory (如果需要支持负数时补充逻辑)
	}

	/// <summary>
	///     获取某个装备元素的总数量。
	/// </summary>
	/// <param name="equipment">装备元素。</param>
	/// <returns>该装备在 Armory 中的数量。</returns>
	public int GetAmount(EquipmentElement equipment) {
		this._data.TryGetValue(equipment, out int amount);
		return amount;
	}

	/// <summary>
	///     获取某个物品（ItemObject）的总数量。
	/// </summary>
	/// <param name="item">物品。</param>
	/// <returns>该物品在 Armory 中的数量。</returns>
	public int GetAmount(ItemObject item)
		=> this._data.AsParallel().SumQ(equipment => equipment.Key.Item == item ? equipment.Value : 0);

	/// <summary>
	///     将内部的 ConcurrentDictionary 全部转成 ItemRoster，以便游戏其他接口使用。
	/// </summary>
	/// <returns>转换后的 ItemRoster。</returns>
	public ItemRoster ToItemRoster() {
		ItemRoster itemRoster = new ItemRoster();
		foreach (KeyValuePair<EquipmentElement, int> pair in this._data) {
			_ = itemRoster.AddToCounts(pair.Key, pair.Value);
		}

		return itemRoster;
	}

	/// <summary>
	///     将 Armory 内部的数据转换成可序列化保存的形式。
	/// </summary>
	/// <returns>可保存的列表。</returns>
	public List<SaveableArmoryEntry> ToSavable() {
		List<SaveableArmoryEntry> data = new List<SaveableArmoryEntry>();
		foreach (KeyValuePair<EquipmentElement, int> pair in this._data) {
			data.Add(new SaveableArmoryEntry(pair.Key, pair.Value));
		}

		return data;
	}

	/// <summary>
	///     从保存的数据中恢复到当前 Armory。
	/// </summary>
	/// <param name="data">之前保存过的 Armory 数据。</param>
	public void FromSavable(List<SaveableArmoryEntry> data) {
		this._data.Clear();
		foreach (SaveableArmoryEntry entry in data) {
			this.Store(entry.Element, entry.Amount);
		}
	}

	/// <summary>
	///     Debug 调试输出当前 Armory 所有内容。
	/// </summary>
	public void DebugPrint() {
		foreach (KeyValuePair<EquipmentElement, int> pair in this._data) {
			Logger.Instance.Information($"{pair.Key.Item?.StringId ?? "unknown"}, {pair.Value}");
		}
	}

	/// <summary>
	///     为了在 DistrubutionTable 中进行分发时访问到 Armory 的全部物品，我们增加一个公共方法来获取所有 Key（即全部 EquipmentElement）。
	/// </summary>
	/// <returns>所有装备元素的集合。</returns>
	public IEnumerable<EquipmentElement> GetAllEquipmentElements() => this._data.Keys;
}