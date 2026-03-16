# Live Game Dev Suite — Zone Density Heatmap Generator PRD
**Document ID:** 02_ZoneDensityHeatmap_PRD  
**Version:** 0.1.0-draft  
**Date:** March 12, 2026  
**Author:** Justin Davis  
**Status:** In Progress

---

## 1. Purpose & Problem Statement

Zone Density Heatmap Generator (ZDHG) enables MMORPG and open-world developers to **visualize and audit world balance** in the Unity Editor — before any playtesting. By combining spawn points, NavMesh coverage, static object density, and manual designer weights into a unified **density heatmap overlay**, designers can instantly identify resource deserts, overcrowded zones, and navmesh dead zones [web:99][web:100].

Current Unity tools only provide terrain altitude heatmaps or require live player data. ZDHG works on **static world data only**, making it ideal for pre-launch world building and content patch validation [web:27][web:66].

---

## 2. Target Users

### Primary

- Open-world / MMORPG world builders balancing:
  - Resource node spawns
  - Enemy/PvP spawn density
  - NavMesh coverage gaps
  - Static prop density (trees, rocks, structures)
- Designers who need **immediate visual feedback** on world balance without running simulations.

### Secondary

- Any Unity game with large scenes needing spawn density analysis (battle royale safe zones, roguelike room balancing).

---

## 3. High-Level Goals

- Generate **live Scene View heatmaps** from multiple density layers.
- **Auto-detect** spawn points, NavMesh, and object density.
- Provide **zone-aware** analysis and desert detection.
- Export **baked heatmaps** for documentation and design reviews.
- Work with **existing NavMesh** (component-based baking in Unity 6) [web:99][web:66].

---

## 4. Functional Requirements

Each requirement is labeled `ZDHG-F-###`.

### 4.1 Density Layers

- **ZDHG-F-001**: ZDHG shall support these **density input layers** (configurable weights 0.0–2.0):
  - **Spawn Points**: GameObjects (found via `Object.FindObjectsByType<Transform>()`) tagged "EnemySpawn", "ResourceSpawn", "PvPSpawn" (configurable tag set)
  - **NavMesh Coverage**: Percentage of scene area covered by baked NavMesh [web:100]
  - **Static Object Density**: Objects on "StaticProp" layer or with "Static" flag
  - **Manual Weight Paint**: Designer-painted brush weights (Scene View tool)
- **ZDHG-F-002**: Each layer shall support **per-layer tagging** for filtering (e.g., only "Ore" resource spawns).

### 4.2 Zone Definition

- **ZDHG-F-010**: Provide a `ZoneDefinition` ScriptableObject defining:
  - Zone name, GUID, description, tags
  - Boundary: BoxCollider (simple axis-aligned) or custom polygon (array of Vector3)
  - Target density range (min/max for balanced zones)
- **ZDHG-F-011**: Zone boundaries shall be **visualized** as Scene View overlays when active.

### 4.3 Grid-Based Analysis

- **ZDHG-F-020**: ZDHG shall divide the scene into a **configurable grid** (cell size 5m–100m).
- **ZDHG-F-021**: For each cell, compute **density score** (0.0–1.0 normalized):
  - Sum weighted layer contributions
  - Normalize against global scene max density
- **ZDHG-F-022**: Use `NavMesh.CalculateTriangulation()` to read **NavMesh coverage** as triangle area coverage per cell, by assigning each triangle's area to the grid cell containing its centroid [web:100].
- **ZDHG-F-023**: Spawn point density = count per cell × layer weight.

### 4.4 Scene View Overlay

- **ZDHG-F-030**: Render a **live heatmap overlay** in Scene View using Unity 6 Overlays (`[Overlay]` + `ToolbarOverlay`).
  - **Rendering method:** Draw the heatmap as a single **procedural texture** mapped to a scene-spanning quad via `Graphics.DrawMesh()` or `Graphics.DrawMeshInstanced()`. Do NOT use per-cell `Handles.DrawAAConvexPolygon()` or GL quads — rendering thousands of individual draw calls in `SceneView.duringSceneGui` causes severe Editor lag.
  - The procedural texture is rebuilt when heatmap data changes, then rendered as a single draw call per frame [web:105][web:109].
- **ZDHG-F-031**: Overlay shall support:
  - **Opacity** (0–100%)
  - **Custom gradient** (8–16 colors, editable)
  - **Cell grid lines** (toggle)
  - **Zone boundaries** overlay
- **ZDHG-F-032**: Clicking a cell shall show a **tooltip** with:
  - Density score
  - Breakdown by layer
  - Zone name (if in a zone)

### 4.5 Desert Detection & Reports

- **ZDHG-F-040**: Auto-flag cells below a **threshold** (user-configurable) as "Deserts".
- **ZDHG-F-041**: Generate `LGD_ValidationReport` with:
  - Per-zone average density
  - Count of desert cells
  - Top 10 highest/lowest density cells
- **ZDHG-F-042**: Zone-level validation:
  - Fail if average density < zone target min
  - Warn if > zone target max

### 4.6 Export & Bake

- **ZDHG-F-050**: Export heatmap as:
  - **PNG texture** (grid texture matching scene bounds)
  - **CSV report** (cell x,y,density)
  - **Markdown summary** with zone stats
- **ZDHG-F-051**: Save **snapshots** for diffing (compare two heatmaps).

### 4.7 Weight Painting

- **ZDHG-F-060**: Provide a **Scene View brush tool** for manual density weights:
  - Brush size, strength
  - Brush modes: Add, Subtract, Smooth
  - Baked to a `ZoneWeightTexture` asset

---

## 5. Non-Functional Requirements

- **ZDHG-NF-001**: Heatmap generation for 10km² scene (10m grid = up to 1,000,000 cells) shall complete in < 3 seconds.
  - **Mandate:** `ZDHG_Generator` and `ZDHG_NavMeshReader` MUST use the **Unity C# Job System** (`IJob` / `IJobParallelFor`) and **Burst Compiler** (`[BurstCompile]`) for all spatial analysis passes. Main-thread processing of 1M+ cells is not acceptable and will freeze the Editor.
- **ZDHG-NF-002**: Overlay rendering < 1ms per frame in Scene View.
- **ZDHG-NF-003**: Compatible with Unity 6.0+; requires `com.unity.ai.navigation` 2.0+ [web:66].
- **ZDHG-NF-004**: Works with **component-based NavMesh** (`NavMeshSurface`) [web:99].
- **ZDHG-NF-005**: When total grid cells exceed 50,000, show a performance warning and recommend increasing cell size.

---

## 6. Editor UI Specification

**Menu:** `Window → Live Game Dev → Zone Density Heatmap`

**Layout:**
- **Layers Panel** (left):
  - Checkbox + weight slider per layer
  - Tag filter dropdowns
- **Zones Panel** (middle):
  - List of `ZoneDefinition` assets
  - Add New Zone button
- **Controls Panel** (right):
  - Cell Size: [10m] slider
  - Threshold: [0.15] slider
  - Opacity: [80%]
  - Gradient Editor (color picker array)
  - `🔥 Generate Heatmap`
  - `💾 Bake to Texture`
  - `📊 Generate Report`

**Scene View Toolbar Overlay:**
- Toggle heatmap on/off
- Brush mode toggle
- Desert highlight toggle

---

## 7. Data Architecture

### 7.1 ScriptableObjects

```csharp
[CreateAssetMenu(menuName = "LiveGameDev/Zone Density/Zone Definition")]
public class ZoneDefinition : LGD_BaseDefinition {
    public string ZoneId;
    public Bounds ZoneBounds;        // or Vector3[] CustomPolygon
    public float TargetDensityMin;
    public float TargetDensityMax;
    public string[] RelevantTags;
}

[CreateAssetMenu(menuName = "LiveGameDev/Zone Density/Zone Weight Texture")]
public class ZoneWeightTexture : Texture2D { /* Procedural texture */ }
```

### 7.2 Core Engine

```csharp
public class DensityCell {
    public Vector2Int GridPos;
    public float DensityScore;
    public Dictionary<string, float> LayerContributions;
    public bool IsDesert;
}

public class HeatmapResult {
    public float CellSize;
    public Bounds SceneBounds;
    public List<DensityCell> Cells;
    public LGD_ValidationReport Report;
}
```

---

## 8. Public API

```csharp
public static class ZdghGenerator {
    public static HeatmapResult GenerateHeatmap(HeatmapSettings settings);
    public static void RenderOverlay(HeatmapResult result, bool showGrid = true);
}
```

---

## 9. Demo Scene Requirements

`Samples~/Demo_ZoneSetup/`:
- 2km × 2km scene with NavMeshSurface baked
- 3 zones with spawn points (enemy, resource)
- Static props on layer
- README showing desert detection workflow

---

## 10. Acceptance Criteria

- Generate heatmap → see live Scene View overlay with 5 colors
- Toggle layers → see density change live
- Click cell → see tooltip with layer breakdown
- Set threshold 0.15 → report flags 14 desert cells
- Export PNG → opens in external viewer
- Add zone → see zone average density calculated

---

*Next Document: [03_EconomySimulationSandbox_PRD.md]*

