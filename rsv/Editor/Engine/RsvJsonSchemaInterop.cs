using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Import and export standard JSON Schema (Draft 7 / 2020-12) files.
    /// Allows Unity teams to share schemas with backend/server teams.
    /// </summary>
    public static class RsvJsonSchemaInterop
    {
        /// <summary>Import a standard JSON Schema file and create a DataSchemaDefinition asset.</summary>
        public static DataSchemaDefinition ImportFromFile(string jsonSchemaFilePath)
        {
            if (!File.Exists(jsonSchemaFilePath))
            {
                Debug.LogError($"[RSV] Import failed — file not found: {jsonSchemaFilePath}");
                return null;
            }
            return ImportFromJson(File.ReadAllText(jsonSchemaFilePath));
        }

        /// <summary>Parse a JSON Schema string and return a DataSchemaDefinition (not yet saved as asset).</summary>
        public static DataSchemaDefinition ImportFromJson(string jsonSchemaText)
        {
            var root = JObject.Parse(jsonSchemaText);
            var def  = ScriptableObject.CreateInstance<DataSchemaDefinition>();

            def.SchemaId    = root["$id"]?.ToString() ?? "imported-schema";
            def.DisplayName = root["title"]?.ToString() ?? def.SchemaId;
            def.Description = root["description"]?.ToString();
            def.Version     = "1.0.0";

            def.RootNodes = ParseProperties(root["properties"] as JObject, root["required"] as JArray);
            return def;
        }

        /// <summary>Export a DataSchemaDefinition as a Draft 7 compliant JSON Schema string.</summary>
        public static string ExportToJson(DataSchemaDefinition schema)
        {
            var root = new JObject
            {
                ["$schema"]     = "http://json-schema.org/draft-07/schema#",
                ["$id"]         = schema.SchemaId,
                ["title"]       = schema.DisplayName,
                ["description"] = schema.Description,
                ["type"]        = "object",
                ["properties"]  = NodesToProperties(schema.RootNodes),
                ["required"]    = new JArray(GetRequiredFields(schema.RootNodes))
            };
            return root.ToString(Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>Export and write to a .json file on disk.</summary>
        public static void ExportToFile(DataSchemaDefinition schema, string outputPath)
        {
            File.WriteAllText(outputPath, ExportToJson(schema));
            AssetDatabase.Refresh();
            Debug.Log($"[RSV] Schema exported to: {outputPath}");
        }

        // ── Private helpers ───────────────────────────────────────

        private static List<RsvSchemaNode> ParseProperties(JObject properties, JArray requiredFields = null)
        {
            var nodes = new List<RsvSchemaNode>();
            if (properties == null) return nodes;

            var required = new System.Collections.Generic.HashSet<string>();
            if (requiredFields != null)
                foreach (var r in requiredFields)
                    required.Add(r.ToString());

            foreach (var prop in properties.Properties())
            {
                var node = new RsvSchemaNode { Name = prop.Name };
                node.Constraint.IsRequired = required.Contains(prop.Name);
                var def  = prop.Value as JObject;
                if (def != null)
                {
                    node.Constraint.FieldType   = ParseType(def["type"]?.ToString());
                    node.Constraint.Description = def["description"]?.ToString();
                    if (def["minimum"] != null) { node.Constraint.Min = def["minimum"].Value<double>(); node.Constraint.HasMinMax = true; }
                    if (def["maximum"] != null) { node.Constraint.Max = def["maximum"].Value<double>(); node.Constraint.HasMinMax = true; }
                    if (def["enum"] is JArray enumArr)
                        node.Constraint.EnumValues = enumArr.ToObject<string[]>();
                    if (def["properties"] is JObject childProps)
                        node.Children = ParseProperties(childProps, def["required"] as JArray);
                }
                nodes.Add(node);
            }
            return nodes;
        }

        private static RsvFieldType ParseType(string jsonType) => jsonType switch
        {
            "string"  => RsvFieldType.String,
            "integer" => RsvFieldType.Integer,
            "number"  => RsvFieldType.Number,
            "boolean" => RsvFieldType.Boolean,
            "object"  => RsvFieldType.Object,
            "array"   => RsvFieldType.Array,
            _         => RsvFieldType.String
        };

        private static JObject NodesToProperties(List<RsvSchemaNode> nodes)
        {
            var props = new JObject();
            if (nodes == null) return props;
            foreach (var node in nodes)
            {
                var def = new JObject { ["type"] = TypeToJsonType(node.Constraint.FieldType) };
                if (!string.IsNullOrEmpty(node.Constraint.Description))
                    def["description"] = node.Constraint.Description;
                if (node.Constraint.HasMinMax)
                {
                    def["minimum"] = node.Constraint.Min;
                    def["maximum"] = node.Constraint.Max;
                }
                if (node.Constraint.EnumValues?.Length > 0)
                    def["enum"] = new JArray((object[])node.Constraint.EnumValues);
                if (node.Children?.Count > 0)
                    def["properties"] = NodesToProperties(node.Children);
                props[node.Name] = def;
            }
            return props;
        }

        private static string TypeToJsonType(RsvFieldType t) => t switch
        {
            RsvFieldType.String  => "string",
            RsvFieldType.Integer => "integer",
            RsvFieldType.Number  => "number",
            RsvFieldType.Boolean => "boolean",
            RsvFieldType.Object  => "object",
            RsvFieldType.Array   => "array",
            _                    => "string"
        };

        private static string[] GetRequiredFields(List<RsvSchemaNode> nodes)
        {
            var required = new List<string>();
            if (nodes == null) return required.ToArray();
            foreach (var node in nodes)
                if (node.Constraint.IsRequired) required.Add(node.Name);
            return required.ToArray();
        }
    }
}
