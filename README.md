# Dynamic Troop Equipment System (Reupload)

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/tategotoazarasi/Bannerlord.DynamicTroop)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Nexus Mods](https://img.shields.io/badge/Nexus%20Mods-Dynamic%20Troop-orange)](https://www.nexusmods.com/mountandblade2bannerlord/mods/9537)

**Dynamic Troop Equipment System** revolutionizes *Mount & Blade II: Bannerlord*'s troop upgrade and equipment mechanics. Gone are the days when soldiers magically conjure armor upon leveling up. Instead, this mod introduces a persistent, dynamic army armory where every piece of equipment must be looted, purchased, or scavenged.

## 📖 Table of Contents
- [Overview](#overview)
- [Key Features](#key-features)
    - [The Armory System](#the-armory-system)
    - [Battle Equipment Distribution](#battle-equipment-distribution)
    - [Looting & Recovery](#looting--recovery)
    - [AI & Logistics](#ai--logistics-cut-their-supply)
- [Configuration (MCM)](#configuration)
- [Advanced Customization](#advanced-customization)
- [Installation](#installation)
- [Compatibility](#compatibility)
- [FAQ](#faq)
- [Credits](#credits)
- [License](#license)

---

## Overview

In the vanilla game, upgrading a troop instantly grants them high-tier gear. With this mod, **soldiers are only as good as the gear you have in your stash.** You must manage a stockpile of weapons, armor, and horses. If you run out of Tier 5 armor, your Tier 5 troops will fight with whatever lower-tier gear is available—or go into battle naked.

This mod adds a layer of strategic depth, making looting essential and economic management crucial for maintaining an elite fighting force.

---

## Key Features

### The Armory System
*   **Persistent Inventory:** Your party has a dedicated "Army Armory" (separate from your personal inventory).
*   **Recruitment:** New recruits bring their starting gear into the armory.
*   **Management:** Access the armory via Town Menus (`Manage Armory`) or view it while in settlements.
*   **Scrapping:** To prevent save bloat, the mod includes an automatic "Scrap" system. If "Commander's Greed" is disabled, the armory automatically sells/deletes the lowest-tier excess items every 3 days if a category exceeds the configured cap.
*   **Trade-in:** You can exchange excess low-tier equipment for throwing weapons via the town menu to keep your skirmishers supplied.

### Battle Equipment Distribution
Before every battle, the mod dynamically assigns gear to your troops based on their class requirements and what is available in the armory.

1.  **Priority Assignment:** Higher-tier troops get first pick of the best gear.
2.  **Matching Logic:**
    *   **Round 1 (Strict):** Exact match of weapon class and usage (e.g., a slash-only 1H sword looks for a slash-only 1H sword).
    *   **Round 2 (Subtype):** Matches weapon functionality (e.g., any 1H polearm).
    *   **Round 3 (Broad):** Broad category matching (e.g., any 1H weapon).
    *   **Round 4 (Fill):** Fills remaining empty slots with any valid broad match.
3.  **Loyal Equipments:** (Configurable) Troops prefer their "vanilla" default gear or gear closest to it (up to +2 tiers higher), rather than simply grabbing the most expensive item available.
4.  **Emergency Loadout:** If the armory is empty, troops will fallback to a "Tier 1" emergency kit based on their culture to avoid being completely unarmed (Configurable).
5.  **Underequipped Penalty:** If a high-tier soldier is forced to wear low-tier gear due to shortages, they suffer a morale penalty in battle.

### Looting & Recovery
*   **Scavenging:** Equipment is not static. When an enemy falls, their gear is added to the "Loot" pool. When your soldier falls, their gear is added to the "Recovery" pool.
*   **Drop Rates:** Looting is calculated based on a configurable drop rate.
*   **Durability:**
    *   Armor protecting a body part that receives a fatal blow has a chance to be destroyed.
    *   Shields that break in battle are lost.
    *   Spent ammunition (arrows/bolts) is lost; unused ammo is recovered.
*   **Map Events:** Post-battle loot is shared dynamically between allied parties based on contribution.

### AI & Logistics ("Cut Their Supply")
*   **AI Armories:** AI Lord parties also utilize this system. They generate equipment daily based on their clan tier, fiefs, and prosperity.
*   **Reinforcement Caravans:** Enemy towns will dispatch physical "Reinforcement Caravan" parties carrying gear and troops to their Lords.
    *   **Strategic Interception:** You can attack these caravans to deny the enemy lord their equipment supply, effectively weakening their armies over time.
    *   *Note: These caravans do not trade with the player.*

---

## Configuration

This mod relies on **Mod Configuration Menu (MCM)**. You can tweak the following settings in-game:

*   **Difficulty:** Scales the equipment generation for AI and shops.
*   **Drop Rate:** Multiplier for how much loot you get from enemies.
*   **Commander's Greed:**
    *   *OFF (Default):* You cannot take items *out* of the Army Armory for personal use (selling/smelting). Keeps the economy balanced.
    *   *ON:* You can treat the Army Armory like a personal stash.
*   **Loyal Equipments:** Toggles whether troops prioritize their cultural/default gear style over raw stats.
*   **Underequipped:** Toggles the morale penalty for soldiers with gear below their rank.
*   **Emergency Loadout:** Toggles whether troops get free T1 gear if the armory is empty.
*   **Randomize Recruit Gear:** Recruits start with varied equipment within their tier/culture.
*   **Scrap Cap:** Sets the limit for item stacks before the auto-cleaner deletes junk (optimization).

---

## Advanced Customization

### Item Blacklist
You can prevent specific items from being used by the system (e.g., overpowered modded weapons or bugged items).
1.  Navigate to the module folder: `Modules/DynamicTroopEquipmentReupload/`
2.  Edit `blacklist.json`.
3.  You can block items by `string_id`, `name`, or using Regex patterns.

### Debugging
The mod includes extensive logging capabilities via `log4net`.
*   Check `log.txt` in the module folder for detailed operations.
*   In-game menu options allow exporting/importing the armory state for testing.

---

## Installation

1.  Download the mod.
2.  Ensure you have the dependencies installed:
    *   [Harmony](https://www.nexusmods.com/mountandblade2bannerlord/mods/2006)
    *   [ButterLib](https://www.nexusmods.com/mountandblade2bannerlord/mods/2018)
    *   [UIExtenderEx](https://www.nexusmods.com/mountandblade2bannerlord/mods/2102)
    *   [Mod Configuration Menu (MCM)](https://www.nexusmods.com/mountandblade2bannerlord/mods/612)
3.  Extract the `DynamicTroopEquipmentReupload` folder into your game's `Modules` directory.
4.  Enable the mod in the Bannerlord Launcher.
5.  **Recommended:** Use [BLSE (Bannerlord Software Extender)](https://www.nexusmods.com/mountandblade2bannerlord/mods/1) for better stability.

---

## Compatibility

*   **Save Game:** Safe to add to an existing save (your armory will start empty). **Do not remove** from an active save without backing up; troops may revert to default or lose gear depending on how the game handles the transition.
*   **Item Mods (OSA, RBM, etc.):** Highly compatible. The mod reads item stats dynamically.
    *   *Confirmed working:* Realistic Battle Mod (RBM), Open Source Armory (OSA), BannerKings.
*   **Dismemberment Mods:** May have compatibility issues depending on how the death event is handled.
*   **Total Conversions:** Generally compatible if they use standard equipment sets.

---

## FAQ

**Q: My soldiers are spawning naked!**
**A:** Your armory is empty. You must loot enemies or buy gear in towns and deposit it into the Army Armory (via the Town Menu). If you are mid-game, you start with nothing.

**Q: Why do my troops keep losing their gear?**
**A:** Gear is destroyed on death (armor breaking mechanics) or lost if it breaks (shields). If you use "Commander's Greed = OFF", the system also scraps low-tier junk automatically to prevent save file lag.

**Q: Can I use this with RBM?**
**A:** Yes, it is fully compatible.

**Q: Do horses consume the armory stock?**
**A:** Upgrading a troop to cavalry requires a horse item (standard game mechanic), but *deploying* them into battle requires a horse in the Army Armory. Avoid sending cavalry into Hideouts, as a game bug can sometimes cause them to lose their mounts.

**Q: Can I take items out of the armory to sell?**
**A:** Only if you enable "Commander's Greed" in the Mod Options. By default, this is disabled to prevent the player from exploiting the army pool for infinite money.

---

## Credits

This project is an open-source continuation and reupload of the original Dynamic Troop Equipment System.

*   **Original Author:** [@tategotoazarasi](https://github.com/tategotoazarasi) - Created the core concept and logic.
*   **Current Maintainer:** [@alemreM](https://github.com/alemreM) - Updates, fixes, and reupload.
*   **Russian Localization:** UmarKot
*   **German Localization:** pandory
*   **Chinese Localization:** Included in the base module.

## License

This project is licensed under the MIT License.

---

### Links

*   [**GitHub Repository**](https://github.com/tategotoazarasi/Bannerlord.DynamicTroop)
*   [**Nexus Mods**](https://www.nexusmods.com/mountandblade2bannerlord/mods/9537)
*   [**Steam Workshop**](https://steamcommunity.com/sharedfiles/filedetails/?id=3119116807)
*   [**Discord**](https://discord.gg/NybRg85KVK)