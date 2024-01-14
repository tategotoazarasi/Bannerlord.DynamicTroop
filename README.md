# Dynamic Troop Equipment System 动态部队装备

The Dynamic Troop Equipment System mod revolutionizes the game's troop upgrade and equipment system by replacing it with a dynamic army armory and equipment distribution system.

## Armory System

- Soldiers no longer magically receive new equipment upon leveling up.
- Parties now have dynamic armory. Equipment from defeated non-hero enemy units is added to this armory.
- Each soldier's equipment is no longer fixed; instead, they select the most suitable gear from the army's armory when entering the battlefield, and return it to the armory upon leaving the battlefield or falling in battle.
- Newly recruited soldiers contribute their initial gear to the armory.
- The armory can be accessed and managed from the town menus. Note that there may be bugs related to equipment crafting.

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

## Functionality for AI Parties

- AI parties led by heroes will have access to an armory mechanism.
- Daily, AI parties receive random equipment that their soldiers would normally have.
- Additionally, AI parties daily receive random equipment up to their clan's tier, favoring gear matching the leader's culture or neutral culture.
- The quantity of equipment AI parties receive each day is influenced by the clan's tier, the number of their fiefs, and the prosperity of those fiefs.

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

- [NexusMods](https://www.nexusmods.com/mountandblade2bannerlord/mods/6223?tab=files)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3119116807)
- [GitHub Repository](https://github.com/tategotoazarasi/Bannerlord.DynamicTroop)
- [Discord](https://discord.gg/NybRg85KVK)
- [骑砍中文站](https://bbs.mountblade.com.cn/download_1436.html)
- QQ Group: 698615206

-------

这个Mod重置了游戏的兵种升级和兵种装备系统，取而代之的是部队军械库和装备分配系统。

## 军械库系统

- 士兵升级时不再凭空获得新装备
- 部队现在有一个动态军械库，战场上杀死的敌人（除了英雄单位）的装备会加入到这里。
- 士兵不再拥有固定的装备；战斗开始时，士兵会从军械库中选取最合适的装备，战斗结束或阵亡时会将装备归还到军械库。
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

## AI机制

- 由英雄领导的AI部队都拥有军械库机制。
- AI部队每日会随机获得其部队中士兵本应有的装备。
- AI部队每日会随机获得不超过其所属家族等级且同文化（或无文化）的装备。
- AI部队每日获得的装备数量由家族等级、封地数量和繁荣度共同决定。

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

- [NexusMods](https://www.nexusmods.com/mountandblade2bannerlord/mods/6223?tab=files)
- [Steam创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3119116807)
- [GitHub仓库](https://github.com/tategotoazarasi/Bannerlord.DynamicTroop)
- [Discord](https://discord.gg/NybRg85KVK)
- [骑砍中文站](https://bbs.mountblade.com.cn/download_1436.html)
- QQ群：698615206
