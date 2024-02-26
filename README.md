# Dynamic Troop Equipment System 动态部队装备

The Dynamic Troop Equipment System mod revolutionizes the game's troop upgrade and equipment system by replacing it with a dynamic army armory and equipment distribution system.

**In versions 1.1.10 and later, when launching the game and entering the grand map, you may experience brief lagging lasting from a few seconds to about ten seconds. This is considered normal. Please be patient and wait a few seconds to ten seconds, and the lag will resolve itself automatically.**

## Armory System

- Soldiers no longer magically receive new equipment upon leveling up.
- Parties now have dynamic armory. Equipment from defeated non-hero enemy units is added to this armory.
- Each soldier's equipment is no longer fixed; instead, they select the most suitable gear from the army's armory when entering the battlefield, and return it to the armory upon leaving the battlefield or falling in battle.
- Newly recruited soldiers contribute their initial gear to the armory.
- The armory can be accessed and managed from the town menus.

## Equipment Distribution Logic

- Before battle, the armory evaluates each soldier's equipment slots. It then equips them with the highest quality weapon available that matches their original equipment from the vanilla game.
- Weapon matching and distribution occur in four distinct rounds:
  - **1st Round:** Requires an exact match in weapon type and function. For example, a one-handed sword only for slashing can only be matched with another one-handed sword of the same type. A one-handed sword that can both stab and slash, or a one-handed sword that is slash-only but can also be wielded with two hands, does not qualify for a match. *In this round, when allocating polearms to cavalry, only lances specifically for mounted combat are considered, and infantry will not be assigned such lances.*
  - **2nd Round:** Subtype matching. For instance, a weapon that can be used as a one-handed polearm can be matched with another that functions as a one-handed polearm, even if the latter can also be used as a two-handed polearm, or if one is for slashing and the other for stabbing. Throwing weapons and melee weapons are not regarded as the same category.
  - **3rd Round:** Broad type matching (e.g., both are “one-handed weapons”). For example, a one-handed sword can be matched with a one-handed axe. *In this round, when allocating polearms to cavalry, only lances specifically for mounted combat are considered, and infantry will not be assigned such lances.*
  - **4th Round:** Broad type matching (e.g., both are “one-handed weapons”).
  - Each round only fills slots left empty from the previous round.
- Soldiers without weapons are allocated a random melee weapon.
- Higher-tier soldiers are prioritized.
- Surplus arrows, shields, throwing weapons, and two-handed/polearms are allocated based on existing equipment.
- Mounted units, including archers, won't receive weapons unsuitable for use on horseback.

## Functionality for AI Parties

- AI parties led by heroes will have access to an armory mechanism.
- Upon creation, AI parties receive original equipment consistent with the soldiers in their party, similar to when the player recruits new troops.
- AI parties can loot enemy armories through battle.
- Daily, AI parties receive random equipment, as detailed below:
  - They obtain equipment that their soldiers would normally have in the vanilla game, with the quantity depending on the total number of troops in the party.
  - They randomly receive equipment matching their clan's culture. The quantity is influenced by the clan tier, which also serves as the upper limit for the level of equipment received.
  - Equipment is received based on their fiefs, with each town, castle, or village providing one piece of equipment daily, aligned with the culture of the settlement. The tier of equipment provided is capped by the settlement's prosperity level.

## Additional Mechanics

- Consumable weapons (arrows, bolts, throwing weapons) are only recoverable if not completely used up.
- Cavalry upgrades do not require horses.
- Soldiers are limited to using weapons within their skill level.
- Broken shields and used ammunition are not collected.
- Armor receiving fatal or critical hits may not be salvageable.
- After defeating an enemy army with an armory, the standard loot system is replaced with the remaining contents of the enemy's armory.
- Upon the initial launch of this mod, a blacklist.json file will be generated in the mod folder. You can edit the item blacklist within this file. When editing, you can use either regular expressions or exact matching to filter items, based on their stringId and localized name. Items that meet these matching criteria will not appear randomly in the game or be lootable.

## Compatibility and Requirements

- Should be compatible with mods not introducing new equipment types.
  - Tested and confirmed compatibility with RBM, OSA, DRM, and BannerKings.
- Note that there may be bugs related to equipment crafting.
- Recommended to use the BLSE launcher for optimal performance.
- Requires Harmony, UIExtenderEx, ButterLib and MCM.

## FAQ

**Q: Why is my army naked?**

A: If you load a save that wasn't previously using this mod, your armory will be empty. You need to manually purchase weapons and put them in the armory for your soldiers. Ensure your soldiers have enough equipment; otherwise, they will appear naked or unarmed.

**Q: My equipment is missing!**

A: Armor that protects a fatally or critically hit area can be damaged when soldiers are killed or knocked out. If multiple armors protect the same area, one is randomly chosen to be damaged based on their protection value.

Used arrows, bolts, throwing weapons, and damaged shields are also not recoverable. 

If you use mods that cause weapons to drop (like RBM) or other mods leading to soldiers discarding their weapons, dropped weapons won't be collected unless picked up again by soldiers.

When equipment is assigned from or returned to the armory, its modifier (prefix) is removed. Consequently, your equipment with prefixes may no longer exist as distinct items after a battle; instead, they merge with the unmodified versions of that same equipment.

Additionally, two known bugs can lead to equipment loss:

first, allowing cavalry to participate in hideout battles may result in the loss of their mounts and horse harnesses, so avoid using cavalry in hideout battles;

second, soldiers carrying banners may lose weapons replaced by banners.

If your case doesn't fit any of these scenarios, please let me know.

**Q: I've encountered a bug!**

A: If you're using Steam Workshop, please report the bug in the dedicated bug report thread [here](https://steamcommunity.com/workshop/filedetails/discussion/3119116807/4203615689068118291/). If you're on Nexus Mods, head to the bug report section [here](https://www.nexusmods.com/mountandblade2bannerlord/mods/6223/?tab=bugs). For users on the Mount & Blade Chinese site, please report in the QQ group. When reporting, please follow these guidelines:

- **Description of the Bug:** Briefly describe the issue you're experiencing.
- **Context and Conditions:** Explain where and under what conditions the bug occurs. Include information on the bug's frequency (always, often, sometimes, or rarely).
- **Expected vs. Actual Behavior:** Describe what you expected to happen and how it differed from what actually occurred.
- **Crash Reports:** If the bug results in a crash, please attach the complete crash report.
- **Steps to Reproduce:** If the bug can be reliably reproduced, enable 'Debug Mode' in the mod's settings menu before the bug occurs. After the bug happens, if you subscribed through the Steam Workshop, send me the `log.txt` file located in `steamapps\workshop\content\261550\3119116807\bin\Win64_Shipping_Client`. For manual installations, find this file in `Mount & Blade II Bannerlord\Modules\Bannerlord.DynamicTroop\bin\Win64_Shipping_Client`.
- **Other Mods:** List any other mods you are using, as this can help identify potential conflicts.

## Credits

- **Russian localization:** UmarKot
- **German localization:** pandory

## Links

- [NexusMods](https://www.nexusmods.com/mountandblade2bannerlord/mods/6223?tab=files)
- [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3119116807)
- [GitHub Repository](https://github.com/tategotoazarasi/Bannerlord.DynamicTroop)
- [Discord](https://discord.gg/NybRg85KVK)
- [骑砍中文站](https://bbs.mountblade.com.cn/download_1436.html)
- QQ Group: 698615206

-------

这个Mod重置了游戏的兵种升级和兵种装备系统，取而代之的是部队军械库和装备分配系统。

**在1.1.10及以后的版本中，启动游戏进入大地图时可能会遇到几秒至十几秒的短暂卡顿，这属于正常现象。只需耐心等待几秒至十几秒，卡顿现象便会自动消失。**

## 军械库系统

- 士兵升级时不再凭空获得新装备
- 部队现在有一个动态军械库，战场上杀死的敌人（除了英雄单位）的装备会加入到这里。
- 士兵不再拥有固定的装备；战斗开始时，士兵会从军械库中选取最合适的装备，战斗结束或阵亡时会将装备归还到军械库。
- 新招募的士兵会将自己的初始装备加入军械库
- 可以从城镇菜单中访问和管理军械库

## 装备分配逻辑

- 战斗开始前，军械库系统会检查每个士兵的装备槽位，从军械库中挑选出能够与这个槽位在原本游戏中士兵的原始装备相匹配的装备中最好的一件分配给士兵。
- 武器的匹配和分配共有四轮：
  - **第一轮：** 精确匹配武器子类型和功能。例如，一把只能用于劈砍的单手剑只能与另一把同样只能用于劈砍的单手剑匹配；而既可以刺又可以砍的单手剑，或只能劈砍但也可以双手使用的单手剑则不能匹配。*在这一轮中，对于骑兵，分配长杆武器时只考虑骑枪，步兵不会获得骑枪。*
  - **第二轮：** 匹配武器的一个子类型。例如，可以将归类为“单手长杆”的两种武器匹配，即使其中一种也可以作为双手长杆使用，或者一种只能劈砍而另一种只能刺击。投掷武器和近战武器不被视为同一类别。
  - **第三轮：** 广泛类型匹配（例如，都是“单手武器”）。例如，一把单手剑可以与单手斧匹配。*在这一轮中，对于骑兵，分配长杆武器时只考虑骑枪，步兵不会获得骑枪。*
  - **第四轮：** 广泛类型匹配（例如，都是“单手武器”）。
  - 每一轮只填补上一轮留下的空缺。
- 没有武器的士兵将被分配一件随机近战武器。
- 优先考虑高等级士兵。
- 如果四轮之后仍有空槽位，则将根据士兵已有的装备分配多余的箭矢、盾牌、投掷武器和双手/长杆武器。
- 骑兵单位不会获得无法在马上使用的武器。

## AI机制

- 由英雄领导的AI部队都拥有军械库机制。
- AI部队在出生时会自带与其部队构成匹配的军械库，这与玩家在招募新兵时获得装备类似。
- AI部队可以相互抢夺军械库。
- AI部队每日会随机获得装备：
  - AI部队每天会随机获得一件其部队中士兵本应有的装备。数量取决于部队规模。
  - AI部队每天会随机获得与其同文化的装备。数量和装备等级上限取决于家族等级。
  - AI部队所属家族的每个封地（城镇、城堡和村庄）每天会随机提供一件与该封地文化相同的装备，质量上限取决于该封地的繁荣度。

## 额外机制

- 消耗性武器（箭矢、弩矢、投掷武器）如果用完则无法被回收，如果没用用完，哪怕只剩一支箭，也可以全部回收。
- 骑兵升级不需要马匹。
- 士兵只能使用要求不超过他们的技能等级的武器。
- 破损的盾牌不会被回收。
- 士兵被击晕/死亡时，给受到致命一击部位提供保护的护甲会损坏（如果有多个护甲对同一部位提供保护，则按照提供的护甲值加权随机一件损坏）。
- 在击败了有军械库的对手之后，原版战利品会被替换为对手的剩余军械库。
- 在首次启动本mod后，`blacklist.json`文件将会在mod文件夹中生成。你可以在此文件中编辑物品的黑名单。编辑时，你可以采用基于正则表达式或完全匹配的方式来过滤，过滤条件包括物品的stringId和其本地化后的名称。符合这些匹配条件的物品将不会在游戏中随机出现或被缴获。

## 常见问题解答

**问：我的部队都是裸体！**
答：如果您加载了一个之前没有使用此mod的存档，您的军械库将是空的。因此，您需要手动为您的士兵购买武器并将它们放入军械库。请确保为您的士兵提供足够的装备，否则他们将出现裸体或未携带武器的情况。

**问：我的装备丢失了！**
答：当您的士兵被杀死或击晕时，为受到致命一击部位提供保护的护甲会损坏。如果有多件护甲保护同一部位，则会根据各护甲的防护值加权，随机选择一件护甲损坏。

用尽的箭、弩矢、投掷武器和损坏的盾牌也不会被回收。

如果您使用了导致武器掉落的mod（例如RBM），或其他导致士兵丢弃武器的mod，掉落的武器将不会被回收，除非士兵再次捡起这些武器。

当装备从军械库分配出去或回收回库时，其前缀将被抹除。因此，如果你的装备有前缀，在战斗结束后，它在物品栏中会与相同的无前缀装备合并。

另外，还有两个已知的bug可能会导致装备丢失：

一是让骑兵参加藏身处战斗可能导致他们的坐骑和马甲丢失，因此尽量避免在藏身处战斗中使用骑兵；

二是士兵携带旗帜可能会导致被旗帜替换的武器丢失。

如果您的情况不属于以上任何一种，请告知我们。

**Q：我遇到了一个bug！**

A：如果您通过Steam创意工坊使用本mod，请在专门的bug报告帖[这里](https://steamcommunity.com/workshop/filedetails/discussion/3119116807/4203615689068118291/)报告问题。如果您是在Nexus Mods上，请前往bug报告区[这里](https://www.nexusmods.com/mountandblade2bannerlord/mods/6223/?tab=bugs)进行报告。对于骑砍中文站的用户，请在QQ群内报告。报告时，请遵循以下指南：

- **Bug描述：** 简要描述您遇到的问题。
- **发生环境及条件：** 说明bug发生的地点和条件。请包括bug发生的频率（总是、经常、有时或很少）。
- **期望与实际行为：** 描述您期望发生的情况以及与实际发生的情况的差异。
- **崩溃报告：** 如果bug导致游戏崩溃，请附上完整的崩溃报告。
- **重现步骤：** 如果bug可以可靠地重现，请在bug发生前在mod的设置菜单中启用“调试模式”。发生bug后，如果您是通过Steam创意工坊订阅的，发送位于`steamapps\workshop\content\261550\3119116807\bin\Win64_Shipping_Client`的`log.txt`文件给我。对于手动安装的用户，可以在`Mount & Blade II Bannerlord\Modules\Bannerlord.DynamicTroop\bin\Win64_Shipping_Client`找到此文件。
- **其他Mods：** 列出您正在使用的任何其他mods，这有助于识别潜在的冲突。

## 鸣谢

- **俄语本地化:** UmarKot
- **德语本地化:** pandory

## 兼容性和要求

- 与不引入新装备类型的模组兼容。
  - 经测试与真实战斗（RBM）、开源军械库（OSA）、真实军队大修（DRM）、旗帜之王（Banner Kings）兼容。
- 装备打造可能存在bug。
- 推荐使用BLSE启动器以获得最佳性能。
- 需要Harmony、UIExtenderEx、ButterLib和MCM。

## 链接

- [NexusMods](https://www.nexusmods.com/mountandblade2bannerlord/mods/6223?tab=files)
- [Steam创意工坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3119116807)
- [GitHub仓库](https://github.com/tategotoazarasi/Bannerlord.DynamicTroop)
- [Discord](https://discord.gg/NybRg85KVK)
- [骑砍中文站](https://bbs.mountblade.com.cn/download_1436.html)
- QQ群：698615206
