using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Collects all spawn point Transforms in the open scene(s).
    /// Uses Object.FindObjectsByType to avoid the deprecated FindObjectsOfType.
    /// </summary>
    internal static class ZDHG_SpawnPointCollector
    {
        /// <summary>
        /// Returns world positions and tags of all GameObjects whose tag
        /// matches one of the provided filterTags (or all if filterTags is empty).
        /// </summary>
        internal static List<(Vector3 Position, string Tag)> CollectSpawnPoints(
            string[] filterTags)
        {
            var results = new List<(Vector3, string)>();

            if (filterTags == null || filterTags.Length == 0)
            {
                var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var t in all)
                {
                    if (t == null) continue;
                    string tag = "Untagged";
                    try { tag = t.gameObject.tag; } catch { /* ignore invalid tags */ }

                    if (tag != "Untagged")
                        results.Add((t.position, tag));
                }
            }
            else
            {
                foreach (var tag in filterTags)
                {
                    if (string.IsNullOrEmpty(tag)) continue;
                    
                    try 
                    {
                        var tagged = GameObject.FindGameObjectsWithTag(tag);
                        foreach (var go in tagged)
                        {
                            if (go != null)
                                results.Add((go.transform.position, tag));
                        }
                    }
                    catch (UnityException)
                    {
                        // Tag doesn't exist in Tag Manager
                        Debug.LogWarning($"[ZDHG] Tag '{tag}' is not defined in the Tag Manager.");
                    }
                }
            }
            return results;
        }
    }
}
