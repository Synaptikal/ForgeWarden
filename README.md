# ForgeWarden: Live Game Dev Suite

**Give live game developers the confidence to ship.**

ForgeWarden is a collection of four professional-grade Unity Editor tools purpose-built for developers of MMORPGs, live service games, and persistent open-world games. It addresses the complete pre-launch validation lifecycle by surfacing data errors, world imbalances, economic failures, and narrative inconsistencies early in development.

---

## 🛠 The ForgeWarden Suite

| Tool | Package ID | Purpose |
|:--- |:--- |:--- |
| **Runtime Schema Validator (RSV)** | `com.forgegames.livegamedev.rsv` | Validates server-driven JSON data against Unity-side schemas during Editor and Play Mode. |
| **Zone Density Heatmap Generator (ZDHG)** | `com.forgegames.livegamedev.zdhg` | Generates visual density maps from spawn points, NavMesh, and object data — no playtesting required. |
| **Economy Simulation Sandbox (ESS)** | `com.forgegames.livegamedev.ess` | Simulates player-driven economies over time to detect inflation, deflation, and sink failures pre-launch. |
| **Narrative Layer Manager (NLM)** | `com.narrativelayermanager.nlm` | Preview any story beat, quest flag, or game phase in your scenes — without entering Play Mode. |

---

## 🚀 Key Features

- **Automated Validation:** Catch data and logic errors before they reach production.
- **Visual Balance Analysis:** Understand your world's density and layout at a glance.
- **Economic Resilience:** Stress-test your game's economy with Monte Carlo simulations.
- **Narrative Preview:** Scrub through story beats and visualize scene state changes instantly.
- **Unity 6 Native:** Built exclusively with UI Toolkit and modern Unity 6 features.
- **Micro-Service Architecture:** Tools install independently but share a common core and event bus.

## 🎯 Target Users

- **Solo MMORPG Developers:** Automate QA tasks to save hundreds of hours of manual playtesting.
- **Live Service Studios:** Empower designers to validate content without programmer intervention.
- **Live Ops Teams:** Pre-validate patches and balance updates before shipping to players.

## 📋 Technical Requirements

- **Unity Version:** Unity 6.0 LTS (Minimum), 6.3 LTS+ (Recommended).
- **Dependencies:**
  - `Newtonsoft.Json` (`com.unity.nuget.newtonsoft-json`) — required by RSV and Core.
  - `AI Navigation` (`com.unity.ai.navigation`) — required by ZDHG.
  - `Burst`, `Collections`, `Mathematics` — required by ZDHG for Job System heatmap generation.
- **Render Pipeline:** Pipeline-agnostic (Editor-only tools, NLM is pipeline-agnostic).

## 🖱 Getting Started

Install each package via **Window → Package Manager → + → Add package from git URL**, using the `?path=` suffix to target the correct subfolder:

```
# Shared core (installed automatically as a dependency — install manually first if needed)
https://github.com/Synaptikal/ForgeWarden.git?path=/core

# Runtime Schema Validator
https://github.com/Synaptikal/ForgeWarden.git?path=/rsv

# Zone Density Heatmap Generator
https://github.com/Synaptikal/ForgeWarden.git?path=/zdhg

# Economy Simulation Sandbox
https://github.com/Synaptikal/ForgeWarden.git?path=/ess2

# Narrative Layer Manager
https://github.com/Synaptikal/ForgeWarden.git?path=/NLM
```

After installation, open **Project Settings → Live Game Dev Suite** to configure shared preferences.

## 📖 Documentation

Detailed documentation for each module can be found in the `Docs` directory:
- [Suite Overview](Docs/PRDs/v3/00_Suite_Overview_PRD_v3.md)
- [Runtime Schema Validator (RSV)](Docs/PRDs/v3/01_RuntimeSchemaValidator_PRD_v3.md)
- [Zone Density Heatmap Generator (ZDHG)](Docs/PRDs/v3/02_ZoneDensityHeatmap_PRD_v3.md)
- [Economy Simulation Sandbox (ESS)](Docs/PRDs/v3/03_EconomySimulationSandbox_PRD_v3.md)
- [Narrative Layer Manager (NLM)](NLM/README.md)

---

Developed by **Forge Games** — a [Kodaxa Studios](https://github.com/Synaptikal) label.
