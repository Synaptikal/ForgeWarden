# FORGEWARDEN — Technical Research & Gap Analysis
**Document ID:** 04_Research_And_Gap_Analysis  
**Version:** 0.1.0-draft  
**Date:** March 12, 2026  
**Author:** Justin Davis  
**Status:** Complete

---

## PURPOSE

This document consolidates all technical research conducted before implementing the Live Game Dev Suite. It audits every existing similar Unity tool, open-source repo, and academic reference, identifies exactly what gaps each PRD fills, and records all architectural decisions made as a result of findings.

This is the **authoritative pre-implementation reference** for all three tools.

---

## 1. EXISTING TOOL AUDIT — Runtime Schema Validator (RSV)

### 1.1 What Exists

| Tool | Repo | What It Does | Gap vs RSV |
|---|---|---|---|
| **maxartz15/Validator** | github.com/maxartz15/Validator | Validates Unity component fields/references via `IValidatable` interface and `[Required]` attribute. Produces a `Report` with `WarningType` categories. | Validates Unity OBJECT integrity (missing refs, null outlets), NOT JSON data content |
| **DarrenTsung/DTValidator** | github.com/DarrenTsung/DTValidator | Finds missing outlets, broken UnityEvents, missing MonoBehaviour scripts. Build-pipeline integration. | Same as above — component-level, not data-level |
| **Tri-Inspector** | github.com/codewriter-packages/Tri-Inspector | Inspector attribute-based validation UI (ShowIf, NotNull, etc.) | Inspector-only, no JSON parsing |
| **arimger/Unity-Editor-Toolbox** | github.com/arimger/Unity-Editor-Toolbox | Rich attribute framework, [ClampAttribute], [NotNull], etc. All IMGUI-based. Explicitly warns it may conflict with UI Toolkit projects. | No JSON schema support; IMGUI-only |
| **JSON Settings Manager** | assetstore.unity.com | Load/save JSON settings at runtime. No schema definition or validation. | No schema authoring, no validation |
| **Newtonsoft Online Validator** | jsonschemavalidator.net | Web-only JSON Schema validator | Not in Unity Editor |

### 1.2 RSV Differentiator
**RSV is the only tool that validates JSON DATA CONTENT against a UNITY-SIDE SCHEMA definition, in the Unity Editor or Play Mode, with no external tooling required.**

### 1.3 Architectural Decisions from Research

**Decision RSV-AD-001:** Adopt `IValidatable` pattern from maxartz15 for our `LGD_BaseDefinition.Validate()` contract but scope it to DATA content, not Unity object references.

```csharp
// maxartz15 pattern (Unity object-level):
public void Validate(Report report) { ... }

// Our pattern (data content-level):
public ValidationResult Validate() { ... }
```

**Decision RSV-AD-002:** Adopt `WarningType` / severity concept from maxartz15 but extend to match live-game needs:

```csharp
public enum ValidationSeverity {
    Pass,       // All checks passed
    Info,       // Informational only
    Warning,    // Non-blocking issue
    Error,      // Blocking, should prevent build
    Critical    // Data corruption risk
}
```

**Decision RSV-AD-003:** Differentiate clearly in Asset Store description from DTValidator/maxartz15 — RSV validates *incoming live data*, they validate *Unity project structure*. These are complementary, not competing.

**Decision RSV-AD-004:** JSON parsing uses `com.unity.nuget.newtonsoft-json` (official Unity fork). Do NOT use `jacobdufault/fullserializer` (outdated) or `mtschoen/JSONObject` (no schema support).

**Decision RSV-AD-005:** RSV-F-051 (`[RsvSchema]` attribute) mirrors Tri-Inspector's attribute pattern but applied to JSON strings specifically — this is well-precedented and will be familiar to developers.

---

## 2. EXISTING TOOL AUDIT — Zone Density Heatmap Generator (ZDHG)

### 2.1 What Exists

| Tool | Repo | What It Does | Gap vs ZDHG |
|---|---|---|---|
| **ksrivastava/HeatMap** | github.com/ksrivastava/HeatMap | Telemetry-driven heatmap from LIVE player event data via server. Grid-based rendering (cubes per cell colored by density). | Requires live server + player data — cannot run pre-launch |
| **kDanik/heatmap-unity** | github.com/kDanik/heatmap-unity | Particle-based heatmap from recorded Vector3 event positions. | Requires recorded data — cannot run pre-launch |
| **karl-/unity-heatmap** | github.com/karl-/unity-heatmap | Mesh-based heatmap from event positions. MeshFilter bounds used. | Requires recorded data — cannot run pre-launch |
| **Unity-Technologies/360-Video-Heatmaps** | github.com/Unity-Technologies/360-Video-Heatmaps | Analytics heatmap from Unity Analytics data (eye-tracking, gaze direction). | Requires Unity Analytics SDK + live session data |
| **Unity Terrain Heatmap** | docs.unity3d.com/Packages/com.unity.terrain-tools | Altitude-only heatmap (elevation visualization). | Terrain height only — not spawn/object density |

### 2.2 ZDHG Differentiator
**ZDHG is the ONLY heatmap tool that operates entirely on STATIC WORLD DATA (spawn point definitions, NavMesh bake, object layer density) requiring zero live players or recorded sessions. It is a pre-launch design tool, not a post-launch analytics tool.**

### 2.3 Architectural Decisions from Research

**Decision ZDHG-AD-001:** Use grid cell coloring approach (confirmed by ksrivastava) but render as `Handles.DrawAAConvexPolygon()` quads in Scene View, not cube GameObjects (avoids scene contamination).

**Decision ZDHG-AD-002:** `NavMesh.CalculateTriangulation()` returns `NavMeshTriangulation` with world-space `vertices[]` and `indices[]`. Per Unity 6.3 API, coverage per grid cell must be calculated by testing triangle centroids against cell bounds — NOT by sampling NavMesh.SamplePosition per cell (too slow at scale).

```csharp
// Efficient NavMesh coverage: 
NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
// For each triangle centroid: test if it falls within cell bounds
// Sum triangle areas per cell / total cell area = coverage %
```

**Decision ZDHG-AD-003:** Spawn point detection uses `GameObject.FindObjectsByType<Transform>()` with tag filtering (Unity 6 API — `FindObjectsOfType` is deprecated). Users register spawn point tags in `ZoneDefinition`.

**Decision ZDHG-AD-004:** Do NOT use particle system for heatmap rendering (kDanik approach). Particle systems in Scene View are fragile and require GameObject instances. Use `GL` or `Handles` drawing calls in `OnSceneGUI` / Scene View overlay.

**Decision ZDHG-AD-005:** Implement Scene View Overlay using Unity 6 `ToolbarOverlay` API (not legacy Handles-only approach). Unity 6.3 has stable `[Overlay]` attribute support.

```csharp
[Overlay(typeof(SceneView), "Zone Density Heatmap")]
public class ZDHGOverlay : ToolbarOverlay { ... }
```

**Decision ZDHG-AD-006:** Cell size default 10m works for typical MMO zones (1km² zone = 10,000 cells at 10m). At 5m cells = 40,000 cells. Set 5m as min with a performance warning above 50,000 cells.

---

## 3. EXISTING TOOL AUDIT — Economy Simulation Sandbox (ESS)

### 3.1 What Exists

| Tool | Repo | What It Does | Gap vs ESS |
|---|---|---|---|
| **Unity Economy Package** | needle-mirror/com.unity.services.economy | Live cloud service: currencies, inventory, purchases. Dashboard-only. | Requires live Unity services — no simulation |
| **Economy Manager (Asset Store)** | assetstore.unity.com/packages/128030 | Runtime UI for in-game shop/inventory. No simulation. | Runtime gameplay tool, not a design tool |
| **GameCult/Aetheria-Economy** | github.com/GameCult/Aetheria-Economy | Full MMO with client/server economy simulation in Unity 2020.3. Uses RethinkDB, real-time item editing. ARPG with full economy model (corporations, resources, market). | Full game not an Editor tool; requires server; Unity 2020.3 |
| **Helpsypoo/primereconomy** | github.com/Helpsypoo/primereconomy | Simulated economy in Unity for educational video. | Moved to private, no tooling |

### 3.2 Academic / Research References

**MMOAgent Framework (2025)** — *Empowering Economic Simulation for MMOs Through Generative AI* (ACM 2025):
- Uses Agent-Based Modeling (ABM) with LLM-driven agents
- Defines 6 core MMO economic resources: **Experience, Material, Token, Currency, Capability, Labor**
- 3 resource density scenarios: Rich, Moderate, Scarce
- Confirmed: Monte Carlo + ABM hybrid is state-of-art for pre-launch economy simulation

**Economic Modeling for Game Design (Reddit/Academic consensus)**:
- Two simulation approaches: **Agent-Based (AB)** models micro-level (individual player actions); **System Dynamics (SD)** models macro-level flows
- For MMORPGs, AB is more realistic but heavier; SD is faster and more predictable
- Recommendation: **ESS v1.0 implements SD (flow-based) with AB-style player profiles; ABM reserved for v2.0**

**Gini Coefficient** — confirmed metric for detecting economy inequality (already in ESS PRD).

### 3.3 ESS Differentiator
**ESS is the ONLY Unity Editor tool that simulates game economy flows pre-launch using player profiles, item sources/sinks, and provides actionable inflation/deflation alerts with no server, no live data, and no external tool.**

### 3.4 Architectural Decisions from Research

**Decision ESS-AD-001:** Use **System Dynamics (SD)** simulation model for v1.0 (not full ABM):
- Represent economy as flows: items enter via sources, flow through player inventories, exit via sinks
- Player profiles define flow rates (session hours × efficiency × drop rate = items generated per day)
- This is computationally feasible for 100K+ virtual players in seconds

**Decision ESS-AD-002:** Adopt Aetheria-Economy's 6 resource categories as a REFERENCE (not dependency):
```
Experience → Player progression resource (affects unlock rates)
Material   → Raw crafting ingredient
Token      → Earned currency (drops, quests)
Currency   → Premium or traded currency
Capability → Stat-based resource (gear score equivalent)
Labor      → Time-gated resource (energy, stamina)
```
ESS `ItemDefinition` `Category` enum should map to these.

**Decision ESS-AD-003:** UI Toolkit `VectorImage` API for charts (line graphs) confirmed viable for Unity 6 via `MeshGenerationContext`. Do NOT use Unity GraphToolkit (still alpha/beta). Custom chart drawing in UI Toolkit is the correct approach.

**Decision ESS-AD-004:** ESS simulation engine must be **fully deterministic with seed**. Same config + same seed = identical results. This is critical for "patch diff mode" to work meaningfully.

**Decision ESS-AD-005:** Simulation results stored as `SimSnapshot` — JSON-serialized state file saved to `DefaultOutputPath`. Two snapshots can be diffed by comparing `ItemSupply` dictionaries.

---

## 4. SHARED CORE PACKAGE — Architectural Decisions

### 4.1 Settings Pattern
Confirmed by `arimger/Unity-Editor-Toolbox`: ScriptableObject-based settings with `[InitializeOnLoad]` auto-creation is the industry-standard approach. Their `EditorSettings.asset` model maps to our `LGD_Settings.asset`.

**Decision CORE-AD-001:** Settings ScriptableObject must handle null-safety via `AssetDatabase.FindAssets` fallback rather than hard-coded path:
```csharp
[InitializeOnLoad]
public static class LGD_SettingsLoader {
    static LGD_SettingsLoader() {
        // Find or create settings
        var guids = AssetDatabase.FindAssets("t:LGD_SuiteSettings");
        if (guids.Length == 0) CreateDefaultSettings();
    }
}
```

### 4.2 UI Framework
**Decision CORE-AD-002:** **UI Toolkit ONLY.** arimger/Unity-Editor-Toolbox (1.8k stars, last updated April 2025) explicitly warns: *"This package is fully IMGUI-based, which means it may conflict with pure UI Toolkit features."* Unity 6 is the inflection point — new tools MUST use UI Toolkit.

### 4.3 Event Bus
**Decision CORE-AD-003:** Adopt Unity SO-based event channel pattern from Unity's official "architect with ScriptableObjects" guide — but for Editor-only events, use a lightweight static generic bus (no SO overhead for Editor tools).

### 4.4 Validation Report
**Decision CORE-AD-004:** Expand `ValidationStatus` from Pass/Warn/Fail to `Pass / Info / Warning / Error / Critical` (5 states). Consistent across all three tools. `Error` = must fix. `Critical` = data corruption risk.

---

## 5. PRD AMENDMENTS REQUIRED

Based on all research, the following changes are needed to the existing PRDs:

### 5.1 Suite Overview PRD (00)
- [ ] Update `ValidationStatus` enum to 5 states (Pass/Info/Warning/Error/Critical)
- [ ] Add `ILgdValidatable` interface to shared architecture section
- [ ] Note competitor differentiation in Product Vision

### 5.2 RSV PRD (01)
- [ ] RSV-F-023: Update severity levels to match 5-state enum
- [ ] Add RSV-AD-005 to attributes section
- [ ] Add competitive differentiation from DTValidator/maxartz15 to Problem Statement

### 5.3 ZDHG PRD (02)
- [ ] ZDHG-F-022: Update NavMesh coverage algorithm to triangle centroid method (ZDHG-AD-002)
- [ ] ZDHG-F-030: Update to use Unity 6 `[Overlay]` API (ZDHG-AD-005)
- [ ] ZDHG-F-001: Update spawn point detection to use `FindObjectsByType<Transform>()` (ZDHG-AD-003)
- [ ] Add performance note: warn when cells > 50,000 (ZDHG-AD-006)

### 5.4 ESS PRD (03)
- [ ] ESS-F-010: Update to SD model (not Monte Carlo) with deterministic seed (ESS-AD-001, ESS-AD-004)
- [ ] ESS-F-001: Update `ItemDefinition.Category` enum to 6 Aetheria categories (ESS-AD-002)
- [ ] ESS-F-020: Remove Unity GraphToolkit chart reference, use custom `VectorImage` drawing (ESS-AD-003)
- [ ] Add `SimSnapshot` to Data Architecture section (ESS-AD-005)

---

## 6. DEPENDENCY MATRIX (FINAL, VERIFIED)

| Package | Direct Dependencies | Notes |
|---|---|---|
| `core` | None | Auto-installs with any tool |
| `rsv` | `core`, `com.unity.nuget.newtonsoft-json` | Newtonsoft required for JSON parse |
| `zdhg` | `core`, `com.unity.ai.navigation 2.0+` | NavMesh triangulation API |
| `ess` | `core` | No external deps |
| Optional cross-tool | RSV ↔ ESS, ZDHG → ESS | Via scripting defines |

---

## 7. UNITY 6 API GOTCHAS (CONFIRMED)

1. `GameObject.FindObjectsOfType<T>()` → DEPRECATED. Use `FindObjectsByType<T>(FindObjectsSortMode.None)` 
2. `OnGUI()` in EditorWindows → AVOID. Use UI Toolkit `CreateGUI()` override
3. `NavMeshSurface.BuildNavMesh()` → Component-baked only in Unity 6. Legacy `NavMesh.bake` window still works but component approach is standard
4. `Handles.DrawAAConvexPolygon()` → Still valid in Unity 6.3 for Scene View drawing
5. `EditorApplication.playModeStateChanged` → Use for Play Mode hook, not `OnPlayModeStateChanged` (different)
6. `AssetDatabase.FindAssets()` → Returns GUIDs, always pair with `AssetDatabase.GUIDToAssetPath()`
7. UI Toolkit `UxmlElement` attribute → New Unity 6 way to register custom elements; replaces `UxmlFactory`

---

## 8. RESEARCH CONCLUSION

All three tools represent **verified market gaps** with no existing Unity Asset Store or open-source equivalent. The closest competitors either:
- Require live player data (all heatmap tools)
- Operate on Unity object structure not data content (all validator tools)
- Are full games or server-dependent systems, not Editor tools (economy tools)

The architectural decisions recorded in this document supersede any implicit decisions in the individual PRDs where conflicts exist. Before any scaffolding begins, apply all amendments listed in Section 5.

---

*All PRDs now ready for implementation.*
