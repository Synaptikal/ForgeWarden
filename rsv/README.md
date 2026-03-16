# Live Game Dev Suite — Runtime Schema Validator (RSV)

Validates server-driven JSON data against Unity-side schemas during Editor and Play Mode.

## Features

### Core Functionality
- **Define schemas as ScriptableObjects** - No code required, use the visual Schema Designer
- **Import/Export JSON Schema** - Support for Draft 7 / 2020-12 standard schemas
- **Multiple validation sources** - Local files, StreamingAssets, Resources, or remote URLs
- **Auto-validation** - On Play Mode entry and pre-build
- **5-level severity reporting** - Pass / Info / Warning / Error / Critical
- **Export reports** - As .md, .csv, or .json

### Schema Designer
- **Visual tree editor** - Hierarchical node-based schema editing
- **Live JSON preview** - See example JSON update as you edit
- **Full constraint support**:
  - Type constraints (String, Integer, Number, Boolean, Object, Array)
  - Min/max range constraints for numeric types
  - Enum value constraints
  - Required/optional field marking
- **Add/Edit/Delete nodes** - Full CRUD operations on schema fields
- **Schema validation** - Catch impossible schemas (e.g., min > max)

### Schema Browser
- **Search functionality** - Find schemas by name, ID, or description
- **Tag-based filtering** - Organize and filter schemas by tags
- **Quick actions** - Create new schemas, refresh list
- **Double-click to edit** - Opens schema in Schema Designer

### Validation Playground
- **Paste-and-validate** - Test JSON against any schema
- **Schema picker** - Easy schema selection dialog
- **Real-time feedback** - Instant validation results
- **Clear controls** - Quickly reset schema or JSON

### In-Inspector Integration
- **Validation status badges** - See Pass/Warn/Fail status at a glance
- **Quick actions** - Validate, Generate Example, Import/Export
- **Direct links** - Open in RSV window with one click

### Schema Versioning & Migration
- **Semantic versioning** - Track schema versions (e.g., 1.0.0, 2.0.0)
- **Migration hints** - Document breaking changes and migration steps
- **Breaking change detection** - Automatic detection of major version changes
- **Migration scripts** - Optional C# scripts for automatic data migration
- **Migration reports** - Generate detailed migration documentation

### Report Filtering
- **Status filtering** - Filter by Pass/Info/Warning/Error/Critical
- **Text search** - Search in messages, categories, and asset paths
- **Real-time updates** - Filters apply instantly

## Requirements
- Unity 6000.0+
- com.forgegames.livegamedev.core 1.0.0
- com.unity.nuget.newtonsoft-json 3.0.2

## Quick Start

### 1. Create a Schema
1. Open RSV Window: `Window > Live Game Dev > Runtime Schema Validator`
2. Go to **Schema Browser** tab
3. Click **+ New Schema** to create a new schema asset
4. Switch to **Schema Designer** tab to edit the schema

### 2. Define Schema Structure
1. Click **+ Add Root Field** to add top-level fields
2. Select a field and edit its properties:
   - **Name**: JSON key name
   - **Type**: String, Integer, Number, Boolean, Object, or Array
   - **Required**: Whether the field must be present
   - **Constraints**: Min/max, enum values, description
3. For Object/Array types, click **+ Add Child** to define nested fields
4. Watch the **JSON Example Preview** update in real-time

### 3. Create a Binding
1. In the Project window, right-click: `Create > Live Game Dev > Runtime Schema > JSON Source Binding`
2. Assign your schema to the binding
3. Choose the source type:
   - **FilePath**: Local file under Assets/
   - **StreamingAssets**: File in StreamingAssets folder
   - **Resources**: Load via Resources.Load
   - **Url**: Fetch from HTTP/HTTPS endpoint (Editor-only)
4. Set the source path or URL
5. Enable **Validate on Play** and/or **Validate on Build**

### 4. Validate
1. Open RSV Window
2. Click **▶ Validate All** to validate all bindings
3. Review results in the report panel
4. Use filters to find specific issues
5. Export reports if needed

### 5. Try the Demo
1. Go to `Packages/com.forgegames.livegamedev.rsv/Samples~/Demo_AbilitySchema`
2. Copy the folder to your project's `Assets/` directory
3. Open `RSV_DemoAssetCreator.cs` and run `Live Game Dev > RSV > Create Demo Assets`
4. Copy `abilities.json` to your `StreamingAssets` folder
5. Open RSV Window and validate!

## Advanced Features

### JSON Schema Import/Export
RSV supports standard JSON Schema Draft 7 / 2020-12:

**Import:**
1. Open Schema Designer
2. Click **Import JSON Schema**
3. Select a .json schema file
4. RSV converts it to a DataSchemaDefinition

**Export:**
1. Open Schema Designer
2. Click **Export JSON Schema**
3. Choose save location
4. RSV generates a Draft 7 compliant schema

### Schema Versioning
Track schema evolution with migration hints:

1. In Schema Designer, click **+ Add Migration Hint**
2. Set the **Target Version** (e.g., "2.0.0")
3. Describe the breaking changes
4. Optionally provide a migration script path
5. Mark as **Required** or **Optional**

### Migration Scripts
Create automatic data migration:

```csharp
public static class AbilitySchemaMigration
{
    public static string Migrate(string json)
    {
        // Parse old JSON
        var data = JsonUtility.FromJson<OldAbilityData>(json);

        // Transform to new format
        var newData = new NewAbilityData
        {
            // ... transformation logic ...
        };

        // Return new JSON
        return JsonUtility.ToJson(newData);
    }
}
```

### Play Mode Auto-Validation
Configure automatic validation:

1. Go to `Edit > Project Settings > Live Game Dev Suite`
2. Enable **Auto Validate On Play**
3. Bindings with **Validate on Play** enabled will validate on Play Mode entry
4. Validation errors will block Play Mode

### Build-Time Validation
Ensure data integrity before shipping:

1. Enable **Validate on Build** on your bindings
2. If validation fails, the build is blocked
3. Review the error report in the console

## API Reference

### Static Validation API

```csharp
// Validate JSON string against a schema
LGD_ValidationReport report = RsvValidator.Validate(schema, jsonText);

// Validate a binding
LGD_ValidationReport report = RsvValidator.ValidateBinding(binding);

// Validate all bindings in the project
LGD_ValidationReport[] reports = RsvValidator.ValidateAllBindings();

// Validate all JSON files in a folder
LGD_ValidationReport report = RsvValidator.ValidateFolder(schema, folderPath);
```

### Runtime Attribute

```csharp
public class MyScript : MonoBehaviour
{
    [RsvSchema("MySchemaId")]
    public string MyJsonData;

    // RSV will validate this field when the asset is saved
}
```

## Common Use Cases

### MMORPG Item System
Define schemas for items, abilities, quests, and rewards. Validate server-provided JSON before it reaches gameplay logic.

### Live Game Configuration
Create schemas for game settings, balance parameters, and feature flags. Catch configuration errors before they affect players.

### Data-Driven Systems
Validate any JSON data your game consumes, from enemy definitions to dialogue trees.

## Troubleshooting

### Validation Fails But JSON Looks Correct
- Check for extra commas or missing quotes
- Verify field names match exactly (case-sensitive)
- Ensure required fields are present
- Check numeric ranges and enum values

### Binding Can't Resolve JSON
- Verify the file path is correct
- For StreamingAssets, ensure files are in the correct folder
- For Resources, check the path doesn't include file extension
- For URLs, test the endpoint in a browser first

### Play Mode Blocked by Validation
- Open RSV Window to see detailed errors
- Fix the issues in your JSON or schema
- Re-validate to confirm fixes
- Play Mode will unblock automatically

## Performance

RSV is designed for performance:
- Typical MMORPG dataset (10-50 JSON files, ~1-2 MB each): < 5 seconds
- Play Mode auto-validation: < 2 seconds for up to 20 bindings
- Zero GC spikes during routine Editor usage

## License

Part of the Live Game Dev Suite. See package license for details.

## Support

For issues, questions, or feature requests, please refer to the main Live Game Dev Suite documentation.
