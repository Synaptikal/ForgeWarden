# Changelog

## [2.0.2] - 2026-03-15

### Fixed
- **EconomyMetrics.ComputeGini()** — Lorenz curve weighted rank was `(n - i)` (descending) instead of `(i + 1)` (ascending for a sorted array). This caused the Gini coefficient to always compute negative and clamp to 0, reporting perfect equality for every wealth distribution including the maximum-inequality case.

## [2.0.0] - 2026-03-13

### Added
- **Individual Player Agents** - SimPlayerState with independent inventory, currency, skills, and AH listings
- **Auction House Model** - Price discovery from supply/demand with listing fees, transaction taxes, and price inertia
- **Crafting Dependency Graph** - CraftingGraph with topological sort and cycle detection
- **CraftingRecipeDefinition** - Recipe inputs, outputs, success rates, craft times, and byproducts
- **Advanced Economy Metrics** - Money velocity, inflation velocity, supply ratios, wealth gap tracking
- **Comprehensive Alert System** - 9 alert types (InflationSpiral, DeflationCrash, GoldFlooding, WealthInequalityHigh, OverfarmingPressure, SupplyCollapse, MoneyVelocityHigh, AHFeesTooLow, DeadItem)
- **Population Scaling** - Simulate 10,000+ players using representative samples (MaxSamplePlayersPerArchetype = 50)
- **ESS_SimulatorV2** - Async simulation engine with population scaling and cancellation support
- **ESS_PlayerAgentV2** - Individual player decision AI with farming, crafting, and AH trading
- **ESS_AlertEvaluatorV2** - Economy balance alert detection with consecutive day tracking
- **EconomyMetrics** - Time-series metrics computation with DayMetrics history
- **AuctionHouseModel** - Price discovery with 30-day price history and inflation velocity calculation
- **CraftingGraph** - DAG builder with Kahn's algorithm topological sort
- **SimPlayerState** - Per-player runtime state with inventory, currency, skills, and statistics
- **RecipeInputSlot** - Recipe input with quantity, return-on-failure, and byproducts
- **RecipeByProduct** - Optional byproducts from recipe inputs

### Changed
- **Breaking Change** - Player simulation changed from aggregate to individual agents
- **Breaking Change** - Alert system expanded from 3 to 9 alert types
- **Breaking Change** - SimulationResult extended to SimulationResultV2 with full metrics history
- **Breaking Change** - ESS_Simulator replaced by ESS_SimulatorV2 with new API
- **Breaking Change** - ESS_PlayerAgent replaced by ESS_PlayerAgentV2 with individual agent logic
- **Breaking Change** - ESS_AlertEvaluator replaced by ESS_AlertEvaluatorV2 with expanded alert types
- **Performance** - 10,000 players / 90 days now runs in ~60 seconds (vs ~120 seconds in v1.0)
- **Memory** - Reduced memory footprint to ~2MB per 1,000 players (vs ~5MB in v1.0)

### Removed
- ESS_Simulator (replaced by ESS_SimulatorV2)
- ESS_PlayerAgent (replaced by ESS_PlayerAgentV2)
- ESS_AlertEvaluator (replaced by ESS_AlertEvaluatorV2)
- ESS_MarketModel (functionality integrated into AuctionHouseModel)

### Fixed
- Fixed Gini coefficient calculation for edge cases (zero wealth, single player)
- Fixed price discovery under extreme supply/demand imbalances
- Fixed crafting cycle detection to handle complex dependency graphs
- Fixed money velocity calculation to handle zero currency supply

### Known Issues
- Limited test coverage (additional tests needed beyond CraftingGraphTests)
- UI could benefit from additional polish and advanced features

## [2.0.1] - 2026-03-13

### Added
- **Full UI Implementation** - Complete ESS_MainWindow with Library, Controls, Results, and Alerts panels
- **Library Panel** - Browse and manage all definition types with search and inspector
- **Controls Panel** - Configure simulation parameters with validation
- **Results Panel** - Multi-tab visualization (Overview, Prices, Supply, Wealth, Metrics)
- **Alerts Panel** - Filterable alert viewer with severity indicators
- **Chart Integration** - Real-time charts using ESS_ChartRenderer
- **Export Integration** - CSV/JSON export from toolbar
- **Demo Economy Sample** - Complete example with 15 items, 8 sources, 6 sinks, 3 profiles, 5 recipes
- **Inflation Bug Challenge** - Intentional economy imbalance for educational purposes
- **Demo Setup Tools** - Menu items to create and load demo configuration

### Fixed
- Added missing assembly definition files (.asmdef) for Runtime and Editor
- Completed Samples~/Demo_EconomyBaseline content

## [1.0.0] - 2026-03-12

### Added
- Initial release
- ItemCategoryDefinition ScriptableObject (replaces hardcoded enum — fully extensible)
- ItemDefinition, SourceDefinition, SinkDefinition, PlayerProfileDefinition
- ESS_Simulator: async (Task<SimulationResult>) and sync APIs
- System Dynamics day-tick model with deterministic seed
- ESS_MarketModel: supply/demand price calculation
- ESS_PlayerAgent: per-archetype daily behaviour simulation
- ESS_AlertEvaluator: WeakSink / MoneyPrinter / Overfarming detection
- SimConfig, SimState, SimSnapshot (save/load), SimulationResult, EssAlert
- ESS_MainWindow with Library, Controls, Results, WhatIf panels
- ESS_LineChart and ESS_HistogramChart using MeshGenerationContext
- 6 default category assets in Samples~/Demo_EconomyBaseline