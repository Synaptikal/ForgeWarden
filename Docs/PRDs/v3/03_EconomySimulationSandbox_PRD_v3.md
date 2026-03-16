# Live Game Dev Suite — Economy Simulation Sandbox PRD
**Document ID:** 03_EconomySimulationSandbox_PRD  
**Version:** 0.1.0-draft  
**Date:** March 12, 2026  
**Author:** Justin Davis  
**Status:** In Progress

---

## 1. Purpose & Problem Statement

The Economy Simulation Sandbox (ESS) enables MMORPG developers to **simulate player-driven economies** entirely within the Unity Editor — no live players or server data required. By modeling item sources, sinks, drop rates, and player progression curves, ESS runs **Monte Carlo simulations** to predict inflation, deflation, and balance issues before launch [web:112][web:114].

Unity's Economy package only connects to live dashboard data [web:23]. Asset Store economy tools are runtime UIs, not pre-launch simulators [web:26]. ESS is **Editor-only**, designed for pre-alpha balancing and patch validation.

---

## 2. Target Users

### Primary

- MMORPG economy designers modeling:
  - Item supply from drops, crafting, vendors
  - Gold/currency sinks (upgrades, vendors, destruction)
  - Rarity pressure across item tiers
- Solo devs or small teams without economy analysts.

### Secondary

- Live service games balancing currencies and progression loops.

---

## 3. High-Level Goals

- Define **economy elements** (items, sources, sinks, player profiles) as ScriptableObjects.
- Run **Monte Carlo simulations** over configurable days/players [web:112].
- Visualize **supply curves, price indices, inflation alerts** in Editor UI.
- Support **what-if scenarios** and patch diffing.
- Generate **exportable reports** for design docs.

---

## 4. Functional Requirements

Each requirement is labeled `ESS-F-###`.

### 4.1 Economy Elements

- **ESS-F-001**: `ItemDefinition` ScriptableObject:
  - Name, rarity tier, base value, stack size
  - `Category` — references an `ItemCategoryDefinition` ScriptableObject (NOT a hardcoded enum). Users can create custom categories. Suite ships with 6 defaults: Experience, Material, Token, Currency, Capability, Labor.
- **ESS-F-002**: `SourceDefinition` ScriptableObject:
  - Source type (drop, crafting, vendor, quest)
  - Base drop rate, player level scaling curve
  - Affected zones/tags (integrates with ZDHG)
- **ESS-F-003**: `SinkDefinition` ScriptableObject:
  - Sink type (crafting cost, vendor buy, upgrade, destruction)
  - Input items + quantities
  - Output efficiency (e.g., 80% material return)
- **ESS-F-004**: `PlayerProfileDefinition` ScriptableObject:
  - Playtime per day (hours)
  - Session frequency (days/week)
  - Progression speed (levels per week)
  - Efficiency multipliers (casual: 0.8, hardcore: 1.5)

### 4.2 Simulation Engine

- **ESS-F-010**: Run a **System Dynamics-style economy simulation** (flow-based) with deterministic random seed support, configured by:

> **Scope Note (v1.0):** ESS models systemic item injection (sources) and removal (sinks) flows — it does not simulate granular player-to-player trade velocity, hoarding behavior, or auction house price discovery. These are v2.0 additions (Agent-Based Model layer). All v1.0 documentation and marketing must clearly state this boundary so users understand what is and is not being modeled.
  - N simulated players (100–100,000)
  - Simulation duration (1–365 days)
  - Random seed for reproducibility
- **ESS-F-011**: Each simulation tick (1 day):
  - Each player runs sessions based on profile
  - Players generate items via sources (weighted by zone density from ZDHG if available)
  - Players consume items via sinks
  - Auction house logic: items above/below target supply adjust simulated price
- **ESS-F-012**: Track per-item:
  - Total supply in circulation
  - Simulated market price (supply/demand curve)
  - Player average wealth
- **ESS-F-013**: Support **player archetypes** mix (e.g., 40% casual, 35% mid-core, 25% hardcore).

### 4.3 Results Visualization

- **ESS-F-020**: Editor window with:
  - **Supply curves** (line charts per item over time)
  - **Price index** chart (normalized across all items)
  - **Inflation rate** (7-day rolling average)
  - **Wealth distribution** histogram (Gini coefficient)
- **ESS-F-021**: **Alert system**:
  - Item supply growth >200% in 30 days → "Weak Sink"
  - Gold per player > threshold → "Money Printer"
  - Rare item < target rarity → "Overfarming"
- **ESS-F-022**: Use **custom UI Toolkit vector drawing** (no GraphToolkit dependency) for charts (line graphs, histograms), via `MeshGenerationContext` [web:117].

### 4.4 What-If & Diffing

- **ESS-F-030**: Save simulation **snapshots** (JSON of final state).
- **ESS-F-031**: **Patch diff mode**: Compare two snapshots (pre/post patch).
- **ESS-F-032**: **What-if sliders**: Adjust source rates, sink efficiency, player profiles and re-run.

### 4.5 Reports & Export

- **ESS-F-040**: Generate `LGD_ValidationReport` with:
  - Per-item supply/price trends
  - Alert list
  - Zone-level economy health (if ZDHG integration active)
- **ESS-F-041**: Export:
  - Charts as PNG
  - Data as CSV
  - Markdown executive summary

---

## 5. Non-Functional Requirements

- **ESS-NF-001**: 10,000 player × 90-day simulation < 10 seconds.
- **ESS-NF-002**: Chart rendering < 16ms per frame.
- **ESS-NF-003**: Compatible Unity 6.0+; Editor-only.
- **ESS-NF-004**: Scalable to 1M+ players via optimized aggregation.

---

## 6. Editor UI Specification

**Menu:** `Window → Live Game Dev → Economy Simulation`

**Layout:**
- **Left:** Element Library (Items, Sources, Sinks, Profiles — drag/drop)
- **Middle:** Simulation Controls
  - Players: [5,000]
  - Days: [90]
  - Profiles: Casual [40%] Mid [35%] Hardcore [25%]
  - `▶ Run Simulation`
- **Right:** Results
  - Charts tab (supply, price, inflation)
  - Alerts tab (colored list)
  - Report tab (export buttons)

**What-If Mode:** Overlay sliders over controls for live parameter tweaking.

---

## 7. Data Architecture

### 7.1 ScriptableObjects

```csharp
[CreateAssetMenu(menuName = "LiveGameDev/Economy/Item Definition")]
public class ItemDefinition : LGD_BaseDefinition {
    public ItemCategoryDefinition Category; // ScriptableObject — fully customizable by user
    // Built-in defaults provided: Experience, Material, Token, Currency, Capability, Labor
    public float BaseValue;
    public int MaxStack;
    public float RarityWeight;
}

[CreateAssetMenu(menuName = "LiveGameDev/Economy/Source Definition")]
public class SourceDefinition : LGD_BaseDefinition {
    public SourceType Type;
    public AnimationCurve LevelScaling;
    public float BaseRatePerHour;
    public ItemDefinition[] Outputs;
    public float[] OutputWeights;
}
```

### 7.2 Simulation State

```csharp
public struct SimState {
    public Dictionary<string, float> ItemSupply;
    public Dictionary<string, float> ItemPrices;
    public float TotalCurrency;
    public float GiniCoefficient;
}

public class SimSnapshot {
    public string SimulationName;
    public int Seed;
    public SimState FinalState;
}
```

---

## 8. Public API

```csharp
public static class EssSimulator {
    public static SimulationResult RunSimulation(SimConfig config);
    public static void RenderCharts(SimulationResult result);
}
```

---

## 9. Demo Scene Requirements

`Samples~/Demo_EconomyBaseline/`:
- 15 items across 3 tiers
- 8 sources, 6 sinks
- 3 player profiles
- README with "find the inflation bug" tutorial

---

## 10. Acceptance Criteria

- Run 5K player × 90 day sim → charts render with supply curves
- Adjust source rate → re-run shows inflation spike
- Alerts flag "Weak Sink" on gold
- Export PNG opens in viewer
- Diff two sims → highlights changed items

---

**Live Game Dev Suite PRDs COMPLETE** 🎉

