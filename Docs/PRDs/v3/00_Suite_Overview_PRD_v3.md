# Live Game Dev Suite — Suite Overview PRD
**Document ID:** 00_Suite_Overview_PRD
**Version:** 0.1.0-draft
**Date:** March 12, 2026
**Author:** Justin Davis
**Status:** In Progress

---

## 1. Product Vision

The **Live Game Dev Suite** is a collection of three Unity Editor tools purpose-built for developers making MMORPGs, live service games, and persistent open-world games. It addresses the complete pre-launch validation lifecycle: *Is your data correct? Is your world balanced? Will your economy survive players?* — all answerable inside the Unity Editor, before a single player logs in.

The suite is designed as a **standalone commercial asset** targeting all Unity MMORPG and live-game developers globally, not any single project. Each tool ships and installs independently but shares a common architecture, design language, and configuration system.

**Mission Statement:**
> *Give live game developers the confidence to ship — by surfacing data errors, world imbalances, and economic failures during development, not after launch.*

---

## 2. The Three Tools

| Tool | Package ID | Purpose |
|---|---|---|
| **Runtime Schema Validator (RSV)** | `com.[publisher].livegamedev.rsv` | Validates server-driven JSON data against Unity-side schemas during Editor and Play Mode |
| **Zone Density Heatmap Generator (ZDHG)** | `com.[publisher].livegamedev.zdhg` | Generates visual density maps from spawn points, NavMesh, and object data — no playtesting required |
| **Economy Simulation Sandbox (ESS)** | `com.[publisher].livegamedev.ess` | Simulates player-driven economies over time to detect inflation, deflation, and sink failures pre-launch |

---

## 3. Target Users

### Persona A — Solo MMORPG Developer
A developer building an MMORPG or open-world RPG alone or in a very small team. Uses Unity 6 with AI-assisted workflows. Has 50–200 scene assets, a growing item/ability database in JSON or ScriptableObjects, and no QA team. Needs automated validation everywhere to catch errors they'd otherwise miss for days.

**Primary pain:** No scalable way to audit data, world balance, or economy without running extensive playtests they don't have time for.

### Persona B — Mid-Size Live Game Studio (5–25 devs)
A studio shipping their first or second live MMORPG or battle royale. Has dedicated content designers who are not programmers. Needs tooling that non-technical designers can use directly in the Editor without writing code or reading logs.

**Primary pain:** Live patches break economy balance and world parity; caught only after player complaints.

### Persona C — Live Operations / Content Team
A team member at an established studio responsible for shipping content patches, balance updates, and new zones. Not the original game developer — needs tools that validate against an existing established schema/zone baseline.

**Primary pain:** Shipping a patch blind — no pre-validation, must rely on staging server playtests that take days to set up.

---

## 4. Shared Architecture

All three tools share the following foundational layer. Individual tool PRDs build on this; they do not re-define it.

### 4.1 Base ScriptableObject Hierarchy

```
LGD_BaseDefinition (abstract ScriptableObject)
├── DataSchemaDefinition          ← RSV
├── ZoneDefinition                ← ZDHG
├── ItemDefinition                ← ESS
├── SourceDefinition              ← ESS
├── SinkDefinition                ← ESS
└── PlayerProfileDefinition       ← ESS
```

All base SOs implement:
- `string DisplayName` — human-readable name shown in Editor windows
- `string Guid` — auto-assigned stable ID for cross-tool references
- `string[] Tags` — for filtering/search across the suite
- `ValidationResult Validate()` — returns a `ValidationStatus` for suite-level health checks

All tools may also implement a shared `ILgdValidatable` interface for project-wide validation entry points.

### 4.2 Shared Settings System

A single `LGD_SuiteSettings` ScriptableObject stored at `Assets/LiveGameDevSuite/Settings/LGD_Settings.asset`. Created automatically on first install via an `InitializeOnLoad` handler.

Fields:
- `string PublisherNamespace` — used in auto-generated asset GUIDs
- `bool AutoValidateOnPlay` — suite-wide Play Mode validation toggle
- `bool VerboseLogging` — toggles debug-level console output
- `Color[] HeatmapGradient` — shared gradient used by ZDHG
- `string DefaultOutputPath` — where reports and exports are written

Accessible via **Project Settings → Live Game Dev Suite**.

### 4.3 Shared Editor UI Framework

- **Framework:** UI Toolkit exclusively — zero IMGUI
- **Layout files:** `.uxml` stored in `Editor/UI/` per-tool folder
- **Stylesheets:** `.uss` files; one shared `LGD_Common.uss` + per-tool overrides
- **Data binding:** Unity 6 native UI Toolkit data binding for all SO-backed fields

Common UI components (shared library):
- `LGD_StatusBadge` — renders ✅ / ⚠️ / ❌ inline with text
- `LGD_ReportPanel` — scrollable results list with filter/sort toolbar
- `LGD_ExportButton` — standardized export to `.md`, `.csv`, or `.json`
- `LGD_TagFilterBar` — tag-based filtering for any SO list

### 4.4 Shared Event Bus

A lightweight static C# event bus `LGD_EventBus` for cross-tool communication:

```csharp
// Example: ESS notifies RSV that an ItemDefinition schema changed
LGD_EventBus.Publish<SchemaChangedEvent>(new SchemaChangedEvent(schemaGuid));
```

Tools subscribe only to relevant event types. No hard dependencies between tools — each compiles and runs independently even if the other two packages are not installed.

### 4.5 Shared Validation Report Format

```csharp
public enum ValidationStatus {
    Pass,
    Info,
    Warning,
    Error,
    Critical
}

public class LGD_ValidationReport {
    public string ToolId;               // "RSV" | "ZDHG" | "ESS"
    public DateTime Timestamp;
    public string ProjectName;
    public List<LGD_ValidationEntry> Entries;
    public ValidationStatus OverallStatus; // Pass | Info | Warning | Error | Critical
}

public class LGD_ValidationEntry {
    public ValidationStatus Status;     // Pass | Info | Warning | Error | Critical
    public string Category;
    public string Message;
    public string AssetPath;            // optional — links to offending asset
    public int Line;                    // optional — for JSON field-level errors
}
```

Export formats: Markdown (`.md`), CSV (`.csv`), JSON (`.json`).

---

## 5. UPM Package Structure

```
com.[publisher].livegamedev.core/
  Runtime/
    LGD_BaseDefinition.cs
    LGD_ValidationReport.cs
    LGD_EventBus.cs
  Editor/
    LGD_SuiteSettings.cs
    LGD_SuiteSettingsProvider.cs
    UI/
      LGD_Common.uxml
      LGD_Common.uss
      Components/
  Tests/
  package.json

com.[publisher].livegamedev.rsv/
  Runtime/
  Editor/
  Tests/
  Samples~/
    Demo_AbilitySchema/
  package.json                 ← depends on .core

com.[publisher].livegamedev.zdhg/
  Runtime/
  Editor/
  Tests/
  Samples~/
    Demo_ZoneSetup/
  package.json                 ← depends on .core

com.[publisher].livegamedev.ess/
  Runtime/
  Editor/
  Tests/
  Samples~/
    Demo_EconomyBaseline/
  package.json                 ← depends on .core
```

Each tool is installable independently via Package Manager. The `.core` package installs automatically as a dependency. A bundle listing on the Asset Store lets buyers purchase all three at a discount.

---

## 6. Unity Version & Render Pipeline Compatibility

| Requirement | Value |
|---|---|
| Minimum Unity Version | **6000.0 (Unity 6.0 LTS)** |
| Target Unity Version | **6000.3 (Unity 6.3 LTS)** — as of 6000.3.11f1 |
| Render Pipeline | Pipeline-agnostic — all tools are Editor-only or use `GL`/`Handles` for overlays |
| Scripting Backend | Mono and IL2CPP supported |
| .NET Standard | 2.1 (Unity 6 default) |
| Newtonsoft.Json | `com.unity.nuget.newtonsoft-json` latest via Package Manager |
| AI Navigation | `com.unity.ai.navigation` 2.0+ required for ZDHG NavMesh reads |

---

## 7. Inter-Tool Integration Points

| Integration | Description |
|---|---|
| **ESS → RSV** | ESS ItemDefinitions can register a DataSchema with RSV — any JSON item data loaded at runtime is auto-validated against its schema |
| **ZDHG → ESS** | Zone spawn density data from ZDHG can feed as a weight modifier into ESS simulation — denser zones get higher simulation activity |
| **RSV → Any** | RSV can validate any ScriptableObject-backed data file, including ZDHG ZoneDefinitions and ESS ItemDefinitions |

Integration is detected via `#if LGD_RSV_INSTALLED` / `#if LGD_ZDHG_INSTALLED` scripting define symbols, auto-set by each package's `InitializeOnLoad` handler.

---

## 8. Publishing & Commercial Strategy

### 8.1 Asset Store Listing Structure
- **Individual listings:** RSV, ZDHG, ESS each as separate UPM packages
- **Bundle listing:** "Live Game Dev Suite (All 3 Tools)" at a ~25% discount
- **Publisher namespace:** Applied for via Unity UPM early access program

### 8.2 Pricing Model (Target)

| Product | Price (USD) |
|---|---|
| Runtime Schema Validator | $39.99 |
| Zone Density Heatmap Generator | $49.99 |
| Economy Simulation Sandbox | $59.99 |
| Full Suite Bundle | $124.99 |

> **Pricing rationale:** These tools replace hours of playtesting, manual QA, and spreadsheet-based economy modeling. Enterprise and mid-size studio pricing ($100–$250) reflects time-saved value. Solo devs may find the individual tool pricing more accessible.


### 8.3 Documentation Requirements
- Each tool: inline XML doc comments on all public APIs
- Each tool: a `README.md` in package root
- Each tool: a `Samples~/Demo_[ToolName]/` demo scene with working example
- Suite: hosted online documentation site (GitHub Pages acceptable)

### 8.4 Support & Versioning
- Semantic versioning: `MAJOR.MINOR.PATCH`
- Breaking API changes increment `MAJOR`
- Each tool versioned independently; `.core` version pinned in each tool's `package.json`
- Bug reports via GitHub Issues; feature requests via GitHub Discussions

---

## 9. Development Roadmap

### Phase 1 — Foundation (Weeks 1–2)
- [ ] `.core` package: base SOs, EventBus, ValidationReport, Settings system, shared UI components

### Phase 2 — RSV (Weeks 3–5)
- [ ] `DataSchemaDefinition` SO and schema editor
- [ ] JSON validator engine (Newtonsoft-backed)
- [ ] RSV Editor window (UXML/USS)
- [ ] Play Mode auto-validation hook
- [ ] Demo scene + documentation

### Phase 3 — ZDHG (Weeks 6–9)
- [ ] `ZoneDefinition` SO and zone editor
- [ ] Grid-based spatial analysis engine
- [ ] NavMesh triangulation reader
- [ ] Scene View overlay renderer
- [ ] Heatmap bake + export
- [ ] Demo scene + documentation

### Phase 4 — ESS (Weeks 10–15)
- [ ] `ItemDefinition`, `SourceDefinition`, `SinkDefinition`, `PlayerProfileDefinition` SOs
- [ ] Monte Carlo simulation engine
- [ ] Results visualization (line charts, alerts)
- [ ] What-If / patch diff mode
- [ ] Demo scene + documentation

### Phase 5 — Integration & Polish (Weeks 16–18)
- [ ] Cross-tool integration (ESS→RSV, ZDHG→ESS)
- [ ] Full suite settings window
- [ ] Asset Store submission and review

---

## 10. Out of Scope (v1.0)

- **Runtime player analytics** — Editor-only suite; no production telemetry
- **Multiplayer/server infrastructure** — no server-side validation components
- **Non-JSON data formats** — v1.0 supports JSON only (XML, YAML are v2 backlog)
- **Mobile platform-specific tooling** — targets PC/console game development
- **AI content generation** — no generative AI features; validation and simulation only
- **Custom NavMesh baking** — ZDHG reads existing baked NavMesh data, does not replace or extend the baking pipeline

---

*Next Document: [01_RuntimeSchemaValidator_PRD.md]*
