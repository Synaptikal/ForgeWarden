# Demo: Economy Baseline

A complete example economy for the Economy Simulation Sandbox (ESS v2).

## Contents

- **15 Items** across 4 categories (Raw Materials, Refined Materials, Crafted Goods, Consumables)
- **8 Sources** (mining, gathering, mob drops, quest rewards)
- **6 Sinks** (repair, vendor buyback, consumption, event entry)
- **3 Player Profiles** (Casual, Regular, Hardcore)
- **5 Crafting Recipes** (mining → smelting → smithing chain)

## The Inflation Bug Challenge

This demo contains an intentional economy imbalance that causes runaway inflation.

**Can you find it?**

### Hints

1. Look at gold injection vs removal rates
2. Compare total sources vs sinks for currency
3. Check the engagement rates

### Solution (Spoiler)

<details>
<summary>Click to reveal</summary>

The **MobDrops** and **QuestRewards** sources inject gold at rates of 50-100 per hour with high engagement (40-90%), but the **EventEntry** sink only removes 10 gold per engagement with 20% engagement rate.

**Math:**
- Gold injected per day: ~50,000g (1000 players × avg 50g)
- Gold removed per day: ~2,000g (1000 players × 20% × 10g)
- **Net inflation: ~48,000g/day**

**Fix:** Increase event entry cost to 50g, add more gold sinks (repair costs, AH fees), or reduce quest reward amounts.

</details>

## Usage

1. Import the sample into your project
2. Open `Window > Live Game Dev > Economy Simulation Sandbox`
3. Use the Library panel to browse the definitions
4. In Controls, load the demo configuration
5. Run the simulation and watch the inflation spiral!

## Files

- `DemoEconomySetup.cs` - Editor script to generate all assets
- `DemoConfig.asset` - Pre-configured simulation settings
- Individual `.asset` files for all definitions

## License

Copyright © 2026 Forge Games. All rights reserved.
