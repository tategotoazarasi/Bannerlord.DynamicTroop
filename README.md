---------- WIP HERE ----------

\# Dynamic Troop Equipment System

The Dynamic Troop Equipment System mod revolutionizes the game's troop upgrade and equipment system by replacing it with
a dynamic army armory and equipment distribution system.

New features, as of 1.3.11:

Loyal Equipments , MCM Tweakable, if ON (default): Soldiers will prioritize their vanilla equipments and the closest
equipment to their vanilla equipments, up to +2 tier of what they had before. OFF: Soldiers will take the best gear
possible, following the +2 tier rule.

Emergency Loadout, MCM Tweakable, if ON (default): If a soldier is missing equipment, they will be issued Tier 1 gear
from their culture as a substitute. OFF: Missing equipment slots will remain empty if no matching item is found in the
stash.

Underequipped, MCM Tweakable, if ON (default): If soldiers have a worse overall gear than what they had by default, they
will lose morale.

Commander's Greed, MCM Tweakable, if OFF (default): the Player can't take stuff from the troop equipment pool.

Scrap (Active only if "Commander's Greed" is disabled):

Every 3 days, the mod cleans up the player party's equipment stash. If the item count in any specific category (e.g.,
Body Armor, Helmets, 1H Weapons, 2H Weapons, etc.) exceeds 600, the system automatically deletes the lowest-value items
in that category until the count drops back to 599.

Cut Their Supply:

Every day, each AI-controlled town has a variable chance to send a ‘Reinforcement Caravan’ to one of its ruling clan’s
parties, supplying equipment to their stash. The chance scales with the town’s prosperity. These can be attacked to
prevent your enemies from strengthening their forces.

And countless of bug fixes..

\## Armory System

\- Soldiers no longer magically receive new equipment upon leveling up.

\- Parties now have dynamic armory. Equipment from defeated non-hero enemy units is added to this armory.

\- Each soldier's equipment is no longer fixed; instead, they select the most suitable gear from the army's armory when
entering the battlefield, and return it to the armory upon leaving the battlefield or falling in battle.

\- Newly recruited soldiers contribute their initial gear to the armory.

\- The armory can be accessed and managed from the town menus.

\## Equipment Distribution Logic

\- Before battle, the armory evaluates each soldier's equipment slots. It then equips them with the highest quality
weapon available that matches their original equipment from the vanilla game.

\- Weapon matching and distribution occur in four distinct rounds:

&nbsp; - \*\*1st Round:\*\* Requires an exact match in weapon type and function. For example, a one-handed sword only
for slashing can only be matched with another one-handed sword of the same type. A one-handed sword that can both stab
and slash, or a one-handed sword that is slash-only but can also be wielded with two hands, does not qualify for a
match. \*In this round, when allocating polearms to cavalry, only lances specifically for mounted combat are considered,
and infantry will not be assigned such lances.\*

&nbsp; - \*\*2nd Round:\*\* Subtype matching. For instance, a weapon that can be used as a one-handed polearm can be
matched with another that functions as a one-handed polearm, even if the latter can also be used as a two-handed
polearm, or if one is for slashing and the other for stabbing. Throwing weapons and melee weapons are not regarded as
the same category.

&nbsp; - \*\*3rd Round:\*\* Broad type matching (e.g., both are “one-handed weapons”). For example, a one-handed sword
can be matched with a one-handed axe. \*In this round, when allocating polearms to cavalry, only lances specifically for
mounted combat are considered, and infantry will not be assigned such lances.\*

&nbsp; - \*\*4th Round:\*\* Broad type matching (e.g., both are “one-handed weapons”).

&nbsp; - Each round only fills slots left empty from the previous round.

\- Soldiers without weapons are allocated a random melee weapon.

\- Higher-tier soldiers are prioritized.

\- Surplus arrows, shields, throwing weapons, and two-handed/polearms are allocated based on existing equipment.

\- Mounted units, including archers, won't receive weapons unsuitable for use on horseback.

\## Functionality for AI Parties

\- AI parties led by heroes will have access to an armory mechanism.

\- Upon creation, AI parties receive original equipment consistent with the soldiers in their party, similar to when the
player recruits new troops.

\- AI parties can loot enemy armories through battle.

\- Daily, AI parties receive random equipment, as detailed below:

&nbsp; - They obtain equipment that their soldiers would normally have in the vanilla game, with the quantity depending
on the total number of troops in the party.

&nbsp; - They randomly receive equipment matching their clan's culture. The quantity is influenced by the clan tier,
which also serves as the upper limit for the level of equipment received.

&nbsp; - Equipment is received based on their fiefs, with each town, castle, or village providing one piece of equipment
daily, aligned with the culture of the settlement. The tier of equipment provided is capped by the settlement's
prosperity level.

\## Additional Mechanics

\- Consumable weapons (arrows, bolts, throwing weapons) are only recoverable if not completely used up.

\- Cavalry upgrades do not require horses.

\- Soldiers are limited to using weapons within their skill level.

\- Broken shields and used ammunition are not collected.

\- Armor receiving fatal or critical hits may not be salvageable.

\- After defeating an enemy army with an armory, the standard loot system is replaced with the remaining contents of the
enemy's armory.

\- Upon the initial launch of this mod, a blacklist.json file will be generated in the mod folder. You can edit the item
blacklist within this file. When editing, you can use either regular expressions or exact matching to filter items,
based on their stringId and localized name. Items that meet these matching criteria will not appear randomly in the game
or be lootable.

\## Compatibility and Requirements

\- Should be compatible with mods not introducing new equipment types.

\- Note that there may be bugs related to crafted equipments.

\- Requires Harmony, UIExtenderEx, ButterLib and MCM.

\## FAQ

\*\*Q: I have some feedbacks!\*\*

A: You can share it with me the same way you send bug reports, as explained below.

\*\*Q: Why is my army naked?\*\*

A: If you load a save that wasn't previously using this mod, your armory will be empty. You need to manually purchase
weapons and put them in the armory for your soldiers. Ensure your soldiers have enough equipment; otherwise, they will
appear naked or unarmed.

\*\*Q: My equipment is missing!\*\*

A: Armor that protects a fatally or critically hit area can be damaged when soldiers are killed or knocked out. If
multiple armors protect the same area, one is randomly chosen to be damaged based on their protection value.

Used arrows, bolts, throwing weapons, and damaged shields are also not recoverable.

If you use mods that cause weapons to drop (like RBM) or other mods leading to soldiers discarding their weapons,
dropped weapons won't be collected unless picked up again by soldiers.

When equipment is assigned from or returned to the armory, its modifier (prefix) is removed. Consequently, your
equipment with prefixes may no longer exist as distinct items after a battle; instead, they merge with the unmodified
versions of that same equipment.

\*\*Q: I've encountered a bug!\*\*

A: Please send your save, steps to reproduce it and BetterExceptionWindow .htm report if it's a crash.

You can either use Discord: aliemirbehar or bugs section in the nexus page.

\## Credits

\- \*\*The original owner of the mod:\*\* tategotoazarasi

\- \*\*Russian localization:\*\* UmarKot

\- \*\*German localization:\*\* pandory

\## Links

\- \[GitHub Repository](https://github.com/tategotoazarasi/DynamicTroopEquipmentReupload) - source for the version 1.1



