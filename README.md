# Bannerlord.DynamicTroop

The Dynamic Troop Equipment System mod introduces a sophisticated armory and equipment distribution system in Mount & Blade II: Bannerlord, focusing exclusively on player-controlled troops.

## Armory System

- Soldiers no longer magically receive new equipment upon leveling up.
- Troops access a dynamic army armory where equipment from defeated enemy soldiers (excluding hero units) is added.
- Soldiers choose their equipment from the armory based on availability, including recruits who add their initial gear to the armory.
- Armory management accessible from town menus; note that equipment crafting may have bugs.

## Equipment Distribution Logic

- Distribution occurs in four distinct rounds:
  - **First Round:** Requires an exact match in weapon type and function. For example, a slashing-only one-handed sword matches only another slashing-only one-handed sword. When distributing polearms to mounted troops, only lances suitable for mounted combat are considered.
  - **Second Round:** Matches one weapon subtype across categories. For example, both weapons categorized as “one-handed polearms” can match, even if one can also be used as a two-handed polearm. Throwing and melee weapons are not considered the same category.
  - **Third Round:** Broad type matching (e.g., both being “one-handed weapons”). For mounted troops, polearms must be suitable for mounted combat, and infantry cannot receive such lances.
  - **Fourth Round:** General matching based on broad weapon categories without specific subtype requirements.
  - Each round only fills slots left empty from the previous round.
- Soldiers without weapons are allocated a random melee weapon.
- Higher-tier soldiers are prioritized, with weapons sorted by tier, then price. For infantry, weapon attributes like bonus against shields, dismounting, knockdown, or bracing count as +1 tier.
- Surplus arrows, shields, throwing weapons, and two-handed/polearms are allocated based on existing equipment.
- Mounted units, including archers, won't receive weapons unsuitable for use on horseback.

## Additional Mechanics

- Consumable weapons (arrows, bolts, throwing weapons) are only recoverable if not completely used up.
- Cavalry upgrades do not require horses.
- Soldiers are limited to using weapons within their skill level.
- Broken shields and used ammunition are not collected.
- Armor receiving fatal or critical hits may not be salvageable.
- Standard loot system remains unaffected.

## Compatibility and Requirements

- Should be compatible with mods not introducing new equipment types.
- Recommended to use the BLSE launcher for optimal performance.
- Requires Harmony, UIExtenderEx, ButterLib and MCM.

## Links

- [NexusMods - Standard & Beta Version](https://www.nexusmods.com/mountandblade2bannerlord/mods/6223?tab=files) (Beta Version: Now extends to AI parties led by heroes. Requires starting a new game. For a more stable experience, download the standard version.)
- [GitHub Repository](https://github.com/tategotoazarasi/Bannerlord.DynamicTroop)
- [Discord](https://discord.gg/NybRg85KVK)
- [骑砍中文站](https://bbs.mountblade.com.cn/download_1436.html)
- QQ Group: 698615206

-------

这个Mod为《骑马与砍杀II：霸主》带来了一个先进的军械库和装备分配系统，专注于玩家控制的部队。

## 军械库系统

- 士兵升级时不再凭空获得新装备
- 部队现在有一个动态军械库，战场上杀死的敌人（除了英雄单位）的装备会加入到这里，战斗开始时，军械库会将其中的装备分配给士兵
- 新招募的士兵会将自己的初始装备加入军械库
- 可以从城镇菜单中访问和管理军械库；请注意，装备打造可能存在bug。

## 装备分配逻辑

- 分配过程分为四个不同的阶段：
  - **第一轮：** 要求武器类型和功能的精确匹配。例如，一把只能用于劈砍的单手剑只能与另一把同样的单手剑匹配。对于骑兵，分配长杆武器时只考虑骑枪，步兵不会获得骑枪。
  - **第二轮：** 在类别内匹配一种武器的子类型。例如，可以将归类为“单手长杆”的两种武器匹配，即使其中一种也可以作为双手长杆使用。投掷武器和近战武器不被视为同一类别。
  - **第三轮：** 广泛类型匹配（例如，都是“单手武器”）。对于骑兵，分配长杆武器时只考虑骑枪，步兵不会获得骑枪。
  - **第四轮：** 广泛类型匹配（例如，都是“单手武器”）。
  - 每一轮只填补上一轮留下的空缺。
- 没有武器的士兵将被分配一种随机近战武器。
- 优先考虑高等级士兵，武器按照等级和价格排序。对于步兵，特殊的武器属性（对盾加成、坠马、击倒、架矛）相当于增加一级。
- 如果四轮之后仍有空槽位，则将根据士兵已有的装备分配多余的箭矢、盾牌、投掷武器和双手/长杆武器。
- 骑兵单位不会获得无法在马上使用的武器。

## 额外机制

- 消耗性武器（箭矢、弩矢、投掷武器）如果用完则无法被回收，如果没用用完，哪怕只剩一支箭，也可以全部回收。
- 骑兵升级不需要马匹。
- 士兵只能使用要求不超过他们的技能等级的武器。
- 破损的盾牌不会被回收。
- 士兵被击晕/死亡时，给受到致命一击部位提供保护的护甲会损坏（如果有多个护甲对同一部位提供保护，则按照提供的护甲值加权随机一件损坏）。
- 原版战利品系统保持不变。

## 兼容性和要求

- 与不引入新装备类型的模组兼容。
- 推荐使用BLSE启动器以获得最佳性能。
- 需要Harmony、UIExtenderEx、ButterLib和MCM。

## 链接

- [NexusMods - 标准和测试版](https://www.nexusmods.com/mountandblade2bannerlord/mods/6223?tab=files)（测试版：现在扩展到由英雄领导的AI队伍。需要开始新游戏。为了更稳定的体验，请下载标准版本。）
- [Steam创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3119116807)
- [GitHub仓库](https://github.com/tategotoazarasi/Bannerlord.DynamicTroop)
- [Discord](https://discord.gg/NybRg85KVK)
- QQ群：698615206
