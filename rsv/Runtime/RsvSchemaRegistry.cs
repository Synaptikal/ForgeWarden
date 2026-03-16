using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.RSV
{
    /// <summary>
    /// Runtime schema registry for managing and looking up compiled schema assets.
    /// Supports loading schemas from Resources, Addressables, or manual registration.
    /// </summary>
    public static class RsvSchemaRegistry
    {
        private static readonly Dictionary<string, RsvCompiledSchemaAsset> _registry =
            new Dictionary<string, RsvCompiledSchemaAsset>();

        private static bool _isInitialized = false;

        /// <summary>
        /// Gets the number of schemas currently registered.
        /// </summary>
        public static int Count => _registry.Count;

        /// <summary>
        /// Gets whether the registry has been initialized.
        /// </summary>
        public static bool IsInitialized => _isInitialized;

        /// <summary>
        /// Initializes the registry by loading all compiled schemas from Resources.
        /// Call this once at application startup (e.g., in a MonoBehaviour's Awake).
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized)
                return;

            // Load all RsvCompiledSchemaAsset from Resources
            var schemas = Resources.LoadAll<RsvCompiledSchemaAsset>("");

            foreach (var schema in schemas)
            {
                if (schema != null && schema.IsValid())
                {
                    Register(schema);
                }
            }

            _isInitialized = true;
            Debug.Log($"[RSV] SchemaRegistry initialized with {_registry.Count} schemas.");
        }

        /// <summary>
        /// Registers a compiled schema asset with the registry.
        /// </summary>
        /// <param name="schema">The schema to register.</param>
        /// <returns>True if registration succeeded, false if the schema is invalid or already registered.</returns>
        public static bool Register(RsvCompiledSchemaAsset schema)
        {
            if (schema == null)
            {
                Debug.LogWarning("[RSV] Cannot register null schema.");
                return false;
            }

            if (!schema.IsValid())
            {
                Debug.LogWarning($"[RSV] Cannot register invalid schema: {schema.SchemaId}");
                return false;
            }

            if (_registry.ContainsKey(schema.SchemaId))
            {
                Debug.LogWarning($"[RSV] Schema already registered: {schema.SchemaId}. Overwriting.");
                _registry[schema.SchemaId] = schema;
            }
            else
            {
                _registry.Add(schema.SchemaId, schema);
            }

            return true;
        }

        /// <summary>
        /// Unregisters a schema by its ID.
        /// </summary>
        /// <param name="schemaId">The ID of the schema to unregister.</param>
        /// <returns>True if the schema was unregistered, false if it wasn't found.</returns>
        public static bool Unregister(string schemaId)
        {
            if (string.IsNullOrWhiteSpace(schemaId))
                return false;

            return _registry.Remove(schemaId);
        }

        /// <summary>
        /// Gets a compiled schema by its ID.
        /// </summary>
        /// <param name="schemaId">The ID of the schema to retrieve.</param>
        /// <returns>The compiled schema, or null if not found.</returns>
        public static RsvCompiledSchemaAsset Get(string schemaId)
        {
            if (string.IsNullOrWhiteSpace(schemaId))
                return null;

            _registry.TryGetValue(schemaId, out var schema);
            return schema;
        }

        /// <summary>
        /// Checks if a schema is registered.
        /// </summary>
        /// <param name="schemaId">The ID of the schema to check.</param>
        /// <returns>True if the schema is registered, false otherwise.</returns>
        public static bool Contains(string schemaId)
        {
            if (string.IsNullOrWhiteSpace(schemaId))
                return false;

            return _registry.ContainsKey(schemaId);
        }

        /// <summary>
        /// Gets all registered schema IDs.
        /// </summary>
        /// <returns>An array of all registered schema IDs.</returns>
        public static string[] GetAllSchemaIds()
        {
            var ids = new string[_registry.Count];
            _registry.Keys.CopyTo(ids, 0);
            return ids;
        }

        /// <summary>
        /// Gets all registered schemas.
        /// </summary>
        /// <returns>An array of all registered schemas.</returns>
        public static RsvCompiledSchemaAsset[] GetAllSchemas()
        {
            var schemas = new RsvCompiledSchemaAsset[_registry.Count];
            _registry.Values.CopyTo(schemas, 0);
            return schemas;
        }

        /// <summary>
        /// Clears all registered schemas.
        /// </summary>
        public static void Clear()
        {
            _registry.Clear();
            _isInitialized = false;
            Debug.Log("[RSV] SchemaRegistry cleared.");
        }

        /// <summary>
        /// Loads a schema from Resources by path.
        /// </summary>
        /// <param name="resourcePath">The Resources path to the schema asset (without extension).</param>
        /// <returns>The loaded schema, or null if not found.</returns>
        public static RsvCompiledSchemaAsset LoadFromResources(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
                return null;

            var schema = Resources.Load<RsvCompiledSchemaAsset>(resourcePath);
            if (schema != null && schema.IsValid())
            {
                Register(schema);
                return schema;
            }

            Debug.LogWarning($"[RSV] Failed to load schema from Resources: {resourcePath}");
            return null;
        }

        /// <summary>
        /// Loads multiple schemas from a Resources folder.
        /// </summary>
        /// <param name="resourceFolderPath">The Resources folder path (without extension).</param>
        /// <returns>The number of schemas loaded.</returns>
        public static int LoadAllFromResources(string resourceFolderPath)
        {
            if (string.IsNullOrWhiteSpace(resourceFolderPath))
                return 0;

            var schemas = Resources.LoadAll<RsvCompiledSchemaAsset>(resourceFolderPath);
            int loaded = 0;

            foreach (var schema in schemas)
            {
                if (schema != null && schema.IsValid())
                {
                    Register(schema);
                    loaded++;
                }
            }

            Debug.Log($"[RSV] Loaded {loaded} schemas from Resources/{resourceFolderPath}");
            return loaded;
        }

        /// <summary>
        /// Validates that all registered schemas are properly configured.
        /// </summary>
        /// <returns>True if all schemas are valid, false otherwise.</returns>
        public static bool ValidateAllSchemas()
        {
            bool allValid = true;
            var invalidSchemas = new List<string>();

            foreach (var kvp in _registry)
            {
                if (!kvp.Value.IsValid())
                {
                    invalidSchemas.Add(kvp.Key);
                    allValid = false;
                }
            }

            if (!allValid)
            {
                Debug.LogWarning($"[RSV] Found {invalidSchemas.Count} invalid schemas: {string.Join(", ", invalidSchemas)}");
            }

            return allValid;
        }

        /// <summary>
        /// Gets statistics about the registry.
        /// </summary>
        /// <returns>A string containing registry statistics.</returns>
        public static string GetStatistics()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("RSV Schema Registry Statistics:");
            sb.AppendLine($"  Total Schemas: {_registry.Count}");
            sb.AppendLine($"  Initialized: {_isInitialized}");

            if (_registry.Count > 0)
            {
                sb.AppendLine("  Registered Schemas:");
                foreach (var kvp in _registry)
                {
                    sb.AppendLine($"    - {kvp.Key} (v{kvp.Value.Version})");
                }
            }

            return sb.ToString();
        }
    }
}