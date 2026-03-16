using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Samples
{
    /// <summary>
    /// Editor utility for creating the demo ability schema and binding assets.
    /// Run this from the menu: Live Game Dev > RSV > Create Demo Assets
    /// </summary>
    public class RSV_DemoAssetCreator
    {
        [MenuItem("Tools/ForgeWarden/RSV/Demos/Create Assets")]
        public static void CreateDemoAssets()
        {
            var basePath = "Assets/LiveGameDevSuite/RSV/Demo_AbilitySchema";
            var schemaPath = $"{basePath}/AbilitySchema.asset";
            var bindingPath = $"{basePath}/AbilityBinding.asset";

            // Ensure directory exists
            System.IO.Directory.CreateDirectory(basePath);

            // Create the schema
            var schema = ScriptableObject.CreateInstance<DataSchemaDefinition>();
            schema.SchemaId = "ability-schema-v1";
            schema.Version = "1.0.0";
            schema.DisplayName = "Ability Schema";
            schema.Description = "Schema for game ability definitions including stats, effects, and metadata.";
            schema.Tags = new[] { "ability", "combat", "mmorpg" };

            // Build the schema structure
            schema.RootNodes = new System.Collections.Generic.List<RsvSchemaNode>
            {
                // Root: abilities array
                new RsvSchemaNode
                {
                    Name = "abilities",
                    Constraint = new RsvFieldConstraint
                    {
                        FieldType = RsvFieldType.Array,
                        IsRequired = true,
                        Description = "Array of ability definitions"
                    },
                    Children = new System.Collections.Generic.List<RsvSchemaNode>
                    {
                        // Ability object fields
                        new RsvSchemaNode
                        {
                            Name = "id",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.String,
                                IsRequired = true,
                                Description = "Unique identifier for the ability"
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "name",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.String,
                                IsRequired = true,
                                Description = "Display name of the ability"
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "description",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.String,
                                IsRequired = false,
                                Description = "Detailed description of what the ability does"
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "type",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.String,
                                IsRequired = true,
                                Description = "Type of ability",
                                EnumValues = new[] { "Active", "Passive", "Toggle" }
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "cooldown",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.Number,
                                IsRequired = true,
                                HasMinMax = true,
                                Min = 0,
                                Max = 300,
                                Description = "Cooldown time in seconds"
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "manaCost",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.Number,
                                IsRequired = true,
                                HasMinMax = true,
                                Min = 0,
                                Max = 1000,
                                Description = "Mana cost to use the ability"
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "damage",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.Number,
                                IsRequired = false,
                                HasMinMax = true,
                                Min = 0,
                                Description = "Base damage dealt by the ability"
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "range",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.Number,
                                IsRequired = false,
                                HasMinMax = true,
                                Min = 0,
                                Description = "Range of the ability in units"
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "isUltimate",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.Boolean,
                                IsRequired = true,
                                Description = "Whether this is an ultimate ability"
                            }
                        },
                        new RsvSchemaNode
                        {
                            Name = "effects",
                            Constraint = new RsvFieldConstraint
                            {
                                FieldType = RsvFieldType.Array,
                                IsRequired = false,
                                Description = "Array of effects applied by the ability"
                            },
                            Children = new System.Collections.Generic.List<RsvSchemaNode>
                            {
                                new RsvSchemaNode
                                {
                                    Name = "type",
                                    Constraint = new RsvFieldConstraint
                                    {
                                        FieldType = RsvFieldType.String,
                                        IsRequired = true,
                                        Description = "Type of effect (e.g., burn, heal, stun)"
                                    }
                                },
                                new RsvSchemaNode
                                {
                                    Name = "value",
                                    Constraint = new RsvFieldConstraint
                                    {
                                        FieldType = RsvFieldType.Number,
                                        IsRequired = true,
                                        Description = "Magnitude of the effect"
                                    }
                                },
                                new RsvSchemaNode
                                {
                                    Name = "duration",
                                    Constraint = new RsvFieldConstraint
                                    {
                                        FieldType = RsvFieldType.Number,
                                        IsRequired = true,
                                        HasMinMax = true,
                                        Min = 0,
                                        Description = "Duration of the effect in seconds"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Save the schema
            AssetDatabase.CreateAsset(schema, schemaPath);
            AssetDatabase.SaveAssets();

            // Create the binding
            var binding = ScriptableObject.CreateInstance<JsonSourceBinding>();
            binding.Schema = schema;
            binding.SourceType = JsonSourceType.StreamingAssets;
            binding.SourcePathOrUrl = "abilities.json";
            binding.ValidateOnPlay = true;
            binding.ValidateOnBuild = true;

            // Save the binding
            AssetDatabase.CreateAsset(binding, bindingPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[RSV] Demo assets created at: {basePath}");
            EditorUtility.DisplayDialog("Demo Assets Created",
                $"Schema and binding assets have been created at:\n{basePath}\n\n" +
                "Make sure to copy abilities.json to your StreamingAssets folder.",
                "OK");
        }
    }
}
