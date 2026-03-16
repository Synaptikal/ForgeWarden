using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Computes differences between two narrative beats.
    /// </summary>
    /// <remarks>
    /// Shows which objects change state between beats and what rules apply.
    /// Useful for understanding the impact of narrative progression.
    /// </remarks>
    public static class NLM_BeatDiff
    {
        /// <summary>
        /// Represents a single difference entry between two beats.
        /// </summary>
        public class DiffEntry
        {
            /// <summary>The GameObject that differs between beats</summary>
            public GameObject Target;

            /// <summary>Rules that apply at beat A</summary>
            public List<NarrativeObjectBinding.BindingRule> RulesAtA = new();

            /// <summary>Rules that apply at beat B</summary>
            public List<NarrativeObjectBinding.BindingRule> RulesAtB = new();

            /// <summary>Human-readable summary of the change</summary>
            public string Summary;
        }

        /// <summary>
        /// Computes the difference between two beats.
        /// </summary>
        /// <param name="beatA">The first beat to compare</param>
        /// <param name="beatB">The second beat to compare</param>
        /// <returns>List of objects that change between the beats</returns>
        /// <remarks>
        /// Compares all NarrativeObjectBindings in loaded scenes.
        /// Returns empty list if either beat or state is null.
        /// </remarks>
        public static List<DiffEntry> Compute(NarrativeBeat beatA, NarrativeBeat beatB)
        {
            var results = new List<DiffEntry>();
            if (beatA?.State == null || beatB?.State == null) return results;

            var bindings = CollectAll();
            foreach (var b in bindings)
            {
                if (b == null || b.HideInNLMList) continue;

                var ra = NLM_Evaluator.ResolveBinding(b, beatA.State);
                var rb = NLM_Evaluator.ResolveBinding(b, beatB.State);

                if (SetsEqual(ra, rb)) continue;

                results.Add(new DiffEntry
                {
                    Target = b.gameObject,
                    RulesAtA = ra,
                    RulesAtB = rb,
                    Summary = $"{b.gameObject.name}: " +
                             $"[{(ra.Count > 0 ? ra[0].RuleName : "none")}] → " +
                             $"[{(rb.Count > 0 ? rb[0].RuleName : "none")}]"
                });
            }
            return results;
        }

        /// <summary>
        /// Collects all NarrativeObjectBinding components from all loaded scenes.
        /// </summary>
        /// <returns>List of all bindings in the current editor session</returns>
        public static List<NarrativeObjectBinding> CollectAll()
        {
            var result = new List<NarrativeObjectBinding>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    result.AddRange(root.GetComponentsInChildren<NarrativeObjectBinding>(true));
            }
            return result;
        }

        #region Private Helpers

        private static bool SetsEqual(
            List<NarrativeObjectBinding.BindingRule> a,
            List<NarrativeObjectBinding.BindingRule> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (!ReferenceEquals(a[i], b[i])) return false;
            return true;
        }

        #endregion
    }
}
