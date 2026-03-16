# Changelog

## [1.0.1] - 2026-03-15

### Fixed
- **RsvFieldConstraint** — `EnumValues` default changed from `Array.Empty<string>()` to `null`. The previous default caused a "enum constraint with no values" `Debug.LogError` on every schema node during validation, flooding the console and causing test runner failures.
- **RsvValidator.Validate()** — was silently discarding failure results and returning a blank Pass report for null schemas, empty JSON, and parse errors. Now correctly returns the populated error report in all failure cases.
- **RsvRuntimeValidator** — added `MeasureJsonDepth()` pre-parse check to enforce `MaxNestingDepth` before calling `JToken.Parse()`. Previously the depth limit was only applied during schema traversal, making the guard ineffective against deeply nested payloads.
- **RsvJsonSchemaInterop.ImportFromJson()** — now correctly parses the `"required"` array from imported JSON Schema documents. Previously all imported fields defaulted to `IsRequired = true`, making it impossible to distinguish required from optional fields after import.
- **RsvSecurityTests** — reduced test payload sizes to realistic security limits (60 000-char strings, 30-deep nesting, 600-item arrays) to prevent memory exhaustion in the test runner.

## [1.0.0] - 2026-03-12
### Added
- Initial release
- DataSchemaDefinition ScriptableObject with hierarchical field tree
- JsonSourceBinding with FilePath, StreamingAssets, Resources, and URL sources
- RsvValidator static API: Validate, ValidateBinding, ValidateAllBindings, ValidateFolder
- JSON Schema Draft 7 import/export via RsvJsonSchemaInterop
- RSV_MainWindow with Schema Browser, Schema Designer, Binding Inspector, Playground tabs
- Play Mode auto-validation hook via RSV_PlayModeHook
- Pre-build validation via RSV_BuildHook (IPreprocessBuildWithReport)
- [RsvSchema] attribute for field-level JSON string validation

### Enhanced
- **Schema Designer UI**: Full tree-view editor with node editing, live JSON preview, and constraint configuration
- **Schema Browser**: Search by name/ID/description, tag-based filtering, double-click to edit
- **Playground Tab**: Schema picker dialog, real-time validation, clear controls
- **Custom Inspectors**: Validation status badges, quick actions for schemas and bindings
- **Schema Versioning**: Migration hints system, version comparison, migration path detection
- **Report Filtering**: Filter by status, text search in messages/categories
- **Sample Content**: Complete ability system demo with schema, binding, JSON data, and runtime loader

### Features
- Hierarchical schema node editing with add/edit/delete operations
- Live JSON example preview that updates as you edit the schema
- Type constraints (String, Integer, Number, Boolean, Object, Array)
- Min/max range constraints for numeric types
- Enum value constraints
- Required/optional field marking
- Schema validation with detailed error reporting
- Migration script support for automatic data migration
- 5-level severity reporting (Pass/Info/Warning/Error/Critical)
- Export reports as .md, .csv, or .json
- In-Inspector validation with status badges
- Quick actions: Validate, Generate Example, Import/Export JSON Schema
- Tag-based organization and filtering
- Breaking change detection between schema versions
- Migration documentation and hints

### UI Components
- RSV_NodeEditor: Individual field node editor
- RSV_SchemaPicker: Modal dialog for schema selection
- RSV_ReportFilterBar: Report filtering controls
- Enhanced USS styling for all components

### Documentation
- Comprehensive README for demo sample
- Step-by-step quick start guide
- Common validation error examples
- Runtime usage examples

### Technical
- Unity 6000.0+ compatibility
- Newtonsoft.Json integration for JSON parsing
- UI Toolkit-based editor windows
- Event bus integration for cross-tool communication
- Core package dependencies for shared infrastructure
