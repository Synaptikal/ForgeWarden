# Live Game Dev Suite — Runtime Schema Validator PRD
**Document ID:** 01_RuntimeSchemaValidator_PRD  
**Version:** 0.1.0-draft  
**Date:** March 12, 2026  
**Author:** Justin Davis  
**Status:** In Progress

---

## 1. Purpose & Problem Statement

Modern MMORPGs and live games rely heavily on **server-driven JSON data** for items, abilities, quests, rewards, and configuration. A single malformed payload (missing field, wrong type, invalid enum) can cause subtle bugs or crashes that surface only in production.

RSV provides a **Unity-native JSON schema validation layer** that runs in the Editor and during Play Mode, catching structural and semantic data errors *before* they hit gameplay logic. It is designed as a **generic asset** for any Unity game that consumes JSON, with specific attention to MMORPG patterns like large item/ability tables and frequent live patches [web:86][web:90][web:91]. Unlike existing Unity validators (which focus on missing references and component integrity), RSV targets **JSON data content** specifically.

---

## 2. Target Users

### Primary

- MMORPG / live game devs using:
  - JSON for item, ability, quest, progression, and config data
  - Addressables, StreamingAssets, or remote config endpoints
- Mid-size studios with non-programmer content designers editing JSON via external tools or web dashboards.

### Secondary

- Any Unity 6 project consuming structured JSON from an external service (roguelites, GaaS mobile, simulation games).

---

## 3. High-Level Goals

- Define **Unity-side schemas** for JSON payloads in a friendly Editor UI.
- Validate JSON **on demand, on build, and on Play Mode start**.
- Produce **actionable error messages** with paths, types, and fix hints, using 5-level severity (Pass/Info/Warning/Error/Critical) [web:86][web:91].
- Support **versioned schemas** for evolving live games.
- Require **no server changes**; purely a Unity Editor/package solution.

---

## 4. Functional Requirements

Each requirement is labeled `RSV-F-###`.

### 4.1 Schema Definition

- **RSV-F-001**: The tool shall provide a `DataSchemaDefinition` ScriptableObject representing a JSON schema.
- **RSV-F-002**: A schema shall support:
  - Primitive types: `string`, `number`, `integer`, `boolean`
  - Structures: `object`, `array`
  - Enum constraints (fixed set of allowed strings or integers)
  - Required vs optional fields
  - Default values (for documentation, not automatic mutation)
  - Min/max range constraints for numeric types [web:90][web:93]
- **RSV-F-003**: Schemas shall support **nested properties** (object-of-object) and arrays-of-objects.
- **RSV-F-004**: Schemas shall support **references** to reusable field groups (similar to `$ref`), implemented as local schema fragments within the same asset [web:90].
- **RSV-F-005**: Schemas shall have:
  - `SchemaId` (string)
  - `Version` (semantic version)
  - `Description`
  - `Tags` (string[] for filtering)
- **RSV-F-006**: RSV shall support **importing standard JSON Schema files** (Draft 7 or 2020-12) and converting them to a `DataSchemaDefinition` ScriptableObject automatically.
- **RSV-F-007**: RSV shall support **exporting** a `DataSchemaDefinition` as a standard JSON Schema `.json` file (Draft 7 compliant), allowing backend and frontend teams to share one source of truth.
- **RSV-F-008**: An `[Import JSON Schema]` and `[Export JSON Schema]` button shall appear in the Schema Designer toolbar.

### 4.2 Schema Editor UI

- **RSV-F-010**: Provide an Editor window **"Schema Browser"** listing all `DataSchemaDefinition` assets, filterable by tag and search.
- **RSV-F-011**: Provide a **schema designer** UI (UI Toolkit) that allows:
  - Hierarchical tree editing of fields
  - Selecting type from dropdown
  - Marking fields as required/optional
  - Entering min/max, enum entries, and description
- **RSV-F-012**: Provide a **JSON example preview** pane that renders a minimal valid JSON sample.
- **RSV-F-013**: Provide **schema validation** to guard against impossible schemas (e.g., `min > max`, empty enum) [web:91].

### 4.3 Validation Engine

- **RSV-F-020**: RSV shall use `com.unity.nuget.newtonsoft-json` to parse JSON [web:51][web:22].
- **RSV-F-021**: RSV shall validate JSON instances against a `DataSchemaDefinition` using:
  - Type checks
  - Required field checks
  - Enum membership checks
  - Numeric range constraints
  - Object and array structural checks
- **RSV-F-022**: The engine shall support **interactive and non-interactive** validation modes [web:93].
- **RSV-F-023**: The engine shall produce a `LGD_ValidationReport` with entries including:
  - `Status` (Pass/Info/Warning/Error/Critical)
  - JSON path (e.g., `abilities[3].damage`)
  - Expected vs actual type/value
  - Suggested fix text when possible.

### 4.4 JSON Source Binding

- **RSV-F-030**: Developer can create a `JsonSourceBinding` asset linking:
  - A `DataSchemaDefinition`
  - A JSON source location:
    - Local file path(s) under `Assets/` or `StreamingAssets/`
    - A Resources path
    - A remote URL (Editor-only fetch)
- **RSV-F-031**: RSV shall support multiple bindings per schema and multiple schemas per project.
- **RSV-F-032**: RSV shall allow marking bindings as:
  - Editor-only
  - Editor + runtime (Play Mode validation hook enabled)

### 4.5 Editor Validation Workflows

- **RSV-F-040**: Provide **manual validation** from the RSV window:
  - "Validate Selected Binding"
  - "Validate All Bindings"
- **RSV-F-041**: Provide **Play Mode auto-validation** when `AutoValidateOnPlay` is enabled:
  - On entering Play Mode, RSV validates all bindings flagged as Editor + runtime
  - If any Fail entries exist, show a non-modal summary with "View Details" button.
- **RSV-F-042**: Provide **Build-time validation**:
  - Optional integration into pre-build steps
  - If enabled and any Fail entries exist, cancel the build and present a report.

### 4.6 In-Inspector Utilities

- **RSV-F-050**: When an asset has an associated `JsonSourceBinding`, the Inspector shall show:
  - A **validation status badge** (Pass/Warn/Fail)
  - A "Validate Now" button
  - A link to open the RSV report window filtered to this asset.
- **RSV-F-051**: If a field is marked with `[RsvSchema("SchemaId")]`, RSV shall validate any JSON string assigned to that field when the asset is saved.

### 4.7 Schema Versioning & Migration

- **RSV-F-060**: `DataSchemaDefinition` shall include a semantic `Version`.
- **RSV-F-061**: RSV shall allow multiple versions of a schema to coexist.
- **RSV-F-062**: RSV shall support a **migration hint** system:
  - Document-level description of how to upgrade from version N to N+1
  - Optional link to a user script for auto-migration.

### 4.8 Reports & Export

- **RSV-F-070**: All validation runs shall produce an in-Editor **Results panel** using `LGD_ReportPanel`.
- **RSV-F-071**: Results panel shall support:
  - Filtering by status (Pass/Warn/Fail)
  - Filtering by schema
  - Filtering by asset path
  - Text search in message
- **RSV-F-072**: User can export a report as:
  - `.md` summarizing errors by schema/source
  - `.csv` (rows of entries)
  - `.json` (serialized `LGD_ValidationReport`)

---

## 5. Non-Functional Requirements

- **RSV-NF-001**: Validation of a typical MMORPG dataset (10–50 JSON files, each ~1–2 MB) must complete under 5 seconds.
- **RSV-NF-002**: Play Mode auto-validation must not stall the editor for more than 2 seconds on projects with up to 20 bindings.
- **RSV-NF-003**: No runtime dependencies in player builds; RSV is Editor-only unless explicit runtime usage is coded.
- **RSV-NF-004**: Compatible with Unity 6000.0+; tested on 6000.3.11f1 [web:45].
- **RSV-NF-005**: Zero GC spikes during routine Editor usage beyond JSON parse/validation.

---

## 6. Editor UI Specification

**Menu:** `Window → Live Game Dev → Runtime Schema Validator`

**Layout** (UI Toolkit):
- **Left pane:** Schemas & Bindings (tabs, search, tag filter)
- **Right pane:** Details & Results
  - Schema Designer (tree view of fields)
  - JSON example preview
  - Results panel (filterable)

**Key buttons:**
- `New Schema` | `New Binding`
- `Validate All` | `Validate Selected`
- `Generate Example JSON`

---

## 7. Data Architecture

### 7.1 ScriptableObjects

```csharp
public enum RsvFieldType { String, Integer, Number, Boolean, Object, Array }

[CreateAssetMenu(menuName = "LiveGameDev/Runtime Schema/Data Schema")]
public class DataSchemaDefinition : LGD_BaseDefinition {
    public string SchemaId;
    public string Version;
    public string Description;
    public List<RsvSchemaNode> RootNodes;
}

[CreateAssetMenu(menuName = "LiveGameDev/Runtime Schema/JSON Source Binding")]
public class JsonSourceBinding : ScriptableObject {
    public DataSchemaDefinition Schema;
    public JsonSourceType SourceType;
    public string SourcePathOrUrl;
    public bool ValidateOnPlay;
    public bool ValidateOnBuild;
}
```

---

## 8. Public API

```csharp
public static class RsvValidator {
    public static LGD_ValidationReport Validate(
        DataSchemaDefinition schema, string jsonText);
    public static LGD_ValidationReport ValidateBinding(JsonSourceBinding binding);
}
```

---

## 9. Demo Scene Requirements

`Samples~/Demo_AbilitySchema/` with:
- Example `DataSchemaDefinition` for "Ability"
- Example `JsonSourceBinding` 
- MonoBehaviour that loads JSON at runtime
- README with step-by-step validation workflow

---

## 10. Acceptance Criteria

- Create schema + binding
- Introduce 4 error types → see 4 Fail entries with JSON paths
- Play Mode auto-validation shows summary
- Build validation blocks failing builds

---

*Next Document: [02_ZoneDensityHeatmap_PRD.md]*

