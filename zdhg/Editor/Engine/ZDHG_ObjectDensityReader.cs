using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Reads static scene objects (Renderers) to build a density contribution layer.
    /// Filters by tag; counts objects per cell.
    /// </summary>
    internal static class ZDHG_ObjectDensityReader
    {
        /// <summary>
        /// Returns world positions of all static Renderers matching filterTags.
        /// Empty filterTags = all static renderers.
        /// </summary>
        internal static List<Vector3> CollectObjectPositions(string[] filterTags)
        {
            var results   = new List<Vector3>();
            var renderers = Object.FindObjectsByType<Renderer>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            bool filterAll = filterTags == null || filterTags.Length == 0;
            var tagSet = filterAll ? null : new HashSet<string>(filterTags);

            foreach (var r in renderers)
            {
                if (!r.gameObject.isStatic) continue;

                if (filterAll || tagSet.Contains(r.gameObject.tag))
                {
                    results.Add(r.bounds.center);
                }
            }
            return results;
        }
    }
}
