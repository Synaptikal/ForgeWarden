# RSV Ability Schema Demo

This demo shows how to use the Runtime Schema Validator (RSV) to validate JSON data for an ability system.

## What's Included

- **AbilitySchema.asset**: A sample schema defining the structure of ability data
- **AbilityBinding.asset**: A binding that links the schema to a JSON file
- **abilities.json**: Sample ability data that validates against the schema
- **AbilityLoader.cs**: Example runtime code for loading and using validated JSON

## Quick Start

1. **Import the Sample**
   - Navigate to `Packages/com.forgegames.livegamedev.rsv/Samples~/Demo_AbilitySchema`
   - Copy the folder to your project's `Assets/` directory

2. **Open the RSV Window**
   - Go to `Window > Live Game Dev > Runtime Schema Validator`

3. **Validate the Sample**
   - Click "▶ Validate All" to see the validation results
   - The sample should pass validation

4. **Experiment**
   - Try modifying `abilities.json` to introduce errors
   - Re-validate to see how RSV catches the issues
   - Use the Schema Designer tab to modify the schema

## Schema Structure

The AbilitySchema defines the following structure:

```json
{
  "abilities": [
    {
      "id": "string",
      "name": "string",
      "description": "string",
      "type": "Active|Passive|Toggle",
      "cooldown": number,
      "manaCost": number,
      "damage": number,
      "range": number,
      "isUltimate": boolean,
      "effects": [
        {
          "type": "string",
          "value": number,
          "duration": number
        }
      ]
    }
  ]
}
```

## Field Constraints

- **id**: Required string, unique identifier
- **name**: Required string, display name
- **description**: Optional string
- **type**: Required enum (Active, Passive, Toggle)
- **cooldown**: Required number, min 0, max 300
- **manaCost**: Required number, min 0, max 1000
- **damage**: Optional number, min 0
- **range**: Optional number, min 0
- **isUltimate**: Required boolean
- **effects**: Optional array of effect objects

## Runtime Usage

See `AbilityLoader.cs` for an example of how to load and use the validated JSON data at runtime.

## Common Validation Errors

Try these to see RSV in action:

1. **Missing Required Field**: Remove `"name"` from an ability
2. **Type Mismatch**: Change `"cooldown"` from a number to a string
3. **Enum Violation**: Use an invalid `"type"` value like "Channel"
4. **Range Violation**: Set `"cooldown"` to -1 or 500

## Next Steps

- Create your own schemas for your game's data
- Set up bindings for your JSON files
- Enable Play Mode auto-validation in Project Settings
- Configure build-time validation to catch errors before shipping
