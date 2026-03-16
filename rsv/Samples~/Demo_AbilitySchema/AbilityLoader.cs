using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.RSV.Samples
{
    /// <summary>
    /// Example runtime loader for ability data.
    /// Demonstrates how to load and use JSON data that has been validated by RSV.
    /// </summary>
    public class AbilityLoader : MonoBehaviour
    {
        [Tooltip("Path to the abilities JSON file (relative to StreamingAssets).")]
        public string jsonPath = "abilities.json";

        [Header("Runtime Data")]
        [SerializeField, ReadOnly] private List<AbilityData> _loadedAbilities = new();

        /// <summary>
        /// Gets all loaded abilities.
        /// </summary>
        public List<AbilityData> Abilities => _loadedAbilities;

        /// <summary>
        /// Gets an ability by ID.
        /// </summary>
        public AbilityData GetAbility(string id)
        {
            return _loadedAbilities.Find(a => a.Id == id);
        }

        private void Start()
        {
            LoadAbilities();
        }

        /// <summary>
        /// Loads abilities from the JSON file.
        /// In production, this would be called after RSV validation passes.
        /// </summary>
        public void LoadAbilities()
        {
            var fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, jsonPath);

            if (!System.IO.File.Exists(fullPath))
            {
                Debug.LogError($"[AbilityLoader] File not found: {fullPath}");
                return;
            }

            try
            {
                var json = System.IO.File.ReadAllText(fullPath);
                var container = JsonUtility.FromJson<AbilityContainer>(json);

                _loadedAbilities.Clear();
                _loadedAbilities.AddRange(container.abilities);

                Debug.Log($"[AbilityLoader] Loaded {_loadedAbilities.Count} abilities from {jsonPath}");

                // Log ability details for demonstration
                foreach (var ability in _loadedAbilities)
                {
                    Debug.Log($"  - {ability.Name} ({ability.Type}): {ability.Description}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AbilityLoader] Failed to load abilities: {ex.Message}");
            }
        }

        /// <summary>
        /// Example method to use an ability.
        /// </summary>
        public void UseAbility(string abilityId, GameObject target)
        {
            var ability = GetAbility(abilityId);
            if (ability == null)
            {
                Debug.LogWarning($"[AbilityLoader] Ability not found: {abilityId}");
                return;
            }

            Debug.Log($"[AbilityLoader] Using {ability.Name} on {target.name}");

            // Apply effects
            foreach (var effect in ability.Effects)
            {
                ApplyEffect(effect, target);
            }
        }

        private void ApplyEffect(AbilityEffect effect, GameObject target)
        {
            Debug.Log($"  Applying effect: {effect.Type} (value: {effect.Value}, duration: {effect.Duration}s)");

            // In a real implementation, you would apply the effect to the target
            // For example:
            // var health = target.GetComponent<Health>();
            // if (health != null && effect.Type == "health_restore")
            //     health.Restore(effect.Value);
        }

        /// <summary>
        /// Container class for JSON deserialization.
        /// </summary>
        [System.Serializable]
        private class AbilityContainer
        {
            public List<AbilityData> abilities;
        }

        /// <summary>
        /// Represents a single ability.
        /// </summary>
        [System.Serializable]
        public class AbilityData
        {
            public string Id;
            public string Name;
            public string Description;
            public string Type;
            public float Cooldown;
            public float ManaCost;
            public float Damage;
            public float Range;
            public bool IsUltimate;
            public List<AbilityEffect> Effects = new();
        }

        /// <summary>
        /// Represents an ability effect.
        /// </summary>
        [System.Serializable]
        public class AbilityEffect
        {
            public string Type;
            public float Value;
            public float Duration;
        }
    }

    /// <summary>
    /// ReadOnly attribute for inspector fields.
    /// </summary>
    public class ReadOnlyAttribute : PropertyAttribute { }
}
