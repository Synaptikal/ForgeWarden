# Live Game Dev Suite — Economy Simulation Sandbox (ESS) v2

Simulate your MMORPG economy inside Unity before launch with advanced player agent AI, auction house price discovery, and crafting dependency graphs.

## Features

### v2.0 Enhancements
- **Individual Player Agents** - Each simulated player has independent state (inventory, currency, skills, AH listings)
- **Auction House Model** - Price discovery from supply/demand with listing fees, transaction taxes, and price inertia
- **Crafting Dependency Graph** - Topological sort ensures upstream items are available before downstream recipes run
- **Advanced Metrics** - Money velocity, inflation velocity, supply ratios, wealth gap tracking
- **Comprehensive Alerts** - 9 alert types including InflationSpiral, DeflationCrash, GoldFlooding, OverfarmingPressure
- **Population Scaling** - Simulate 10,000+ players using representative samples (50 per archetype)

### v2.0.1 UI & Demo
- **Complete Editor UI** - Full-featured window with Library, Controls, Results, and Alerts panels
- **Interactive Charts** - Real-time visualization of prices, supply, wealth distribution
- **Demo Economy** - Ready-to-use sample with intentional inflation bug for learning
- **One-Click Setup** - Menu items to create demo assets and load configurations

### Core Features
- System Dynamics flow model (sources inject, sinks remove)
- Deterministic simulation with seed support
- Player archetypes (casual / hardcore / mixed ratios)
- Gini coefficient tracking for wealth inequality
- Custom item categories (no hardcoded enum)
- Async simulation with progress bar and cancel button
- Export reports as .md / .csv / .json

## Requirements
- Unity 6000.0+
- com.forgegames.livegamedev.core 1.0.0

## Quick Start

### 1. Create Definitions
```
Assets > Create > Live Game Dev > Economy > Item Category Definition
Assets > Create > Live Game Dev > Economy > Item Definition
Assets > Create > Live Game Dev > Economy > Source Definition
Assets > Create > Live Game Dev > Economy > Sink Definition
Assets > Create > Live Game Dev > Economy > Player Profile Definition
Assets > Create > Live Game Dev > Economy > Crafting Recipe Definition
```

### 2. Configure Your Economy
- **Item Categories**: Define groups (e.g., "Raw Materials", "Crafted Goods", "Consumables")
- **Items**: Set base value, target circulation per player, and category
- **Sources**: Define item injection rates (mob drops, quest rewards, gathering)
- **Sinks**: Define item consumption (crafting, vendor sell-back, event entry)
- **Player Profiles**: Configure archetype behavior (play hours, efficiency, AH participation)
- **Crafting Recipes**: Define input/output relationships with success rates and craft times

### 3. Run Simulation
```
Window > Live Game Dev > Economy Simulation Sandbox
```
- Configure simulation parameters (days, player count, seed)
- Click "Run Simulation"
- Monitor progress and view real-time alerts
- Analyze results with charts and metrics

## Architecture

### Simulation Pipeline (Per Day)
1. **Player Behavior** - Each agent executes daily decisions:
   - Basic needs (repair, consumables)
   - Crafting queue (profitable recipes)
   - AH sells (list excess inventory)
   - AH buys (purchase cheaper than farming)
   - Farming (fill inventory gaps)

2. **Auction House Resolution** - Aggregates supply/demand and discovers prices

3. **Economy Metrics** - Computes Gini, money velocity, inflation, supply ratios

4. **Alert Evaluation** - Detects balance issues and escalates warnings

### Key Components

#### Engine
- `ESS_SimulatorV2` - Main simulation orchestrator with async API
- `AuctionHouseModel` - Price discovery with listing fees and taxes
- `CraftingGraph` - DAG builder with cycle detection and topological sort
- `EconomyMetrics` - Time-series metrics computation
- `ESS_PlayerAgentV2` - Individual player decision AI
- `ESS_AlertEvaluatorV2` - Economy balance alert detection
- `SimPlayerState` - Per-player runtime state

#### Definitions
- `ItemCategoryDefinition` - Extensible category system
- `ItemDefinition` - Base value, circulation targets
- `SourceDefinition` - Item injection rates
- `SinkDefinition` - Item consumption rates
- `PlayerProfileDefinition` - Archetype behavior
- `CraftingRecipeDefinition` - Crafting with success rates and byproducts

## Alert Types

| Alert | Description | Threshold |
|-------|-------------|-----------|
| InflationSpiral | Price rising for 5+ consecutive days | 3% daily |
| DeflationCrash | Price falling for 5+ consecutive days | -3% daily |
| GoldFlooding | Currency supply grew >20% in 7 days | 20% growth |
| WealthInequalityHigh | Gini > 0.7 for 3+ days | Gini 0.7 |
| OverfarmingPressure | Supply > 3x expected for 3+ days | 3x ratio |
| SupplyCollapse | Supply < 10% of expected | 10% ratio |
| MoneyVelocityHigh | Currency circulating too rapidly | Velocity 2.5 |
| AHFeesTooLow | Gold sinks < 50% of injection | 50% deficit |
| DeadItem | Supply exists but zero transactions | 7 days |

## Performance

- **10,000 players / 90 days**: ~60 seconds
- **Population scaling**: Uses 50 representative players per archetype, scales results linearly
- **Memory**: ~2MB per 1,000 players

## Demo Economy

A complete example economy is included to help you get started:

```
Live Game Dev > ESS > Create Demo Economy
Live Game Dev > ESS > Load Demo Config
```

The demo includes:
- **15 items** (raw materials → refined → crafted goods)
- **8 sources** (mining, gathering, mob drops, quests)
- **6 sinks** (repair, vendor, consumption, events)
- **3 player profiles** (casual, regular, hardcore)
- **5 crafting recipes** (mining → smelting → smithing chain)

### The Inflation Bug Challenge

The demo contains an intentional economy imbalance. Can you find it?

**Hint:** Compare gold injection rates (mob drops + quest rewards) vs gold removal rates (event entry + repair).

**Solution:** MobDrops injects 50g/hr at 90% engagement, QuestRewards injects 100g at 40% engagement, but EventEntry only removes 10g at 20% engagement. Net result: ~48,000g inflation per day with 1000 players!

## Migration from v1.0

ESS v2 is a complete rewrite with breaking changes:

1. **Player simulation** changed from aggregate to individual agents
2. **Alert system** expanded from 3 to 9 alert types
3. **Crafting** added as new feature
4. **Auction house** added for price discovery
5. **Metrics** expanded with money velocity and inflation tracking

To migrate:
- Move your ItemCategory, Item, Source, Sink, and PlayerProfile definitions
- Add CraftingRecipeDefinition for crafting chains
- Update alert thresholds in your simulation config
- Review new metrics in results

## License

Copyright © 2026 Forge Games. All rights reserved.