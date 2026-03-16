# ForgeWarden: Live Game Dev Suite

**Give live game developers the confidence to ship.**

ForgeWarden is a collection of three professional-grade Unity Editor tools purpose-built for developers of MMORPGs, live service games, and persistent open-world games. It addresses the complete pre-launch validation lifecycle by surfacing data errors, world imbalances, and economic failures early in development.

---

## 🛠 The ForgeWarden Suite

| Tool | Package ID | Purpose |
|:--- |:--- |:--- |
| **Runtime Schema Validator (RSV)** | `com.synaptikal.livegamedev.rsv` | Validates server-driven JSON data against Unity-side schemas during Editor and Play Mode. |
| **Zone Density Heatmap Generator (ZDHG)** | `com.synaptikal.livegamedev.zdhg` | Generates visual density maps from spawn points, NavMesh, and object data — no playtesting required. |
| **Economy Simulation Sandbox (ESS)** | `com.synaptikal.livegamedev.ess` | Simulates player-driven economies over time to detect inflation, deflation, and sink failures pre-launch. |

---

## 🚀 Key Features

- **Automated Validation:** Catch data and logic errors before they reach production.
- **Visual Balance Analysis:** Understand your world's density and layout at a glance.
- **Economic Resilience:** Stress-test your game's economy with Monte Carlo simulations.
- **Unity 6 Native:** Built exclusively with UI Toolkit and modern Unity 6 features.
- **Micro-Service Architecture:** Tools install independently but share a common core and event bus.

## 🎯 Target Users

- **Solo MMORPG Developers:** Automate QA tasks to save hundreds of hours of manual playtesting.
- **Live Service Studios:** Empower designers to validate content without programmer intervention.
- **Live Ops Teams:** Pre-validate patches and balance updates before shipping to players.

## 📋 Technical Requirements

- **Unity Version:** Unity 6.0 LTS (Minimum), 6.3 LTS+ (Recommended).
- **Dependencies:** 
  - `Newtonsoft.Json` (com.unity.nuget.newtonsoft-json)
  - `AI Navigation` (com.unity.ai.navigation) for ZDHG.
- **Render Pipeline:** Pipeline-agnostic (Editor-only tools).

## 🖱 Getting Started

1. **Install Core:** Clone this repository or add the `.core` package via Unity Package Manager.
2. **Add Modules:** Install the desired ForgeWarden modules (RSV, ZDHG, ESS) from their respective folders.
3. **Configure Suite:** Open **Project Settings -> Live Game Dev Suite** to set your shared preferences.

## 📖 Documentation

Detailed documentation for each module can be found in the `Docs` directory:
- [Suite Overview](Docs/PRDs/v3/00_Suite_Overview_PRD_v3.md)
- [Runtime Schema Validator (RSV)](Docs/PRDs/v3/01_RuntimeSchemaValidator_PRD_v3.md)
- [Zone Density Heatmap Generator (ZDHG)](Docs/PRDs/v3/02_ZoneDensityHeatmap_PRD_v3.md)
- [Economy Simulation Sandbox (ESS)](Docs/PRDs/v3/03_EconomySimulationSandbox_PRD_v3.md)

---

Developed by **Synaptikal**.
