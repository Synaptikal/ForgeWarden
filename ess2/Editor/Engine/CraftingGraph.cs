using System;
using System.Collections.Generic;
using LiveGameDev.Core;
using LiveGameDev.ESS;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Builds a Directed Acyclic Graph from all CraftingRecipeDefinitions.
    ///
    /// Nodes = ItemDefinitions.
    /// Edges = RecipeInputSlot → OutputItem.
    ///
    /// Topological sort ensures each day tick processes raw materials first
    /// (mining → smelting → smithing) so upstream supply is available when
    /// downstream recipes run.
    ///
    /// Cycle detection raises ValidationStatus.Critical and aborts the sim.
    /// </summary>
    public class CraftingGraph
    {
        // node name → recipes that produce it
        private readonly Dictionary<string, List<CraftingRecipeDefinition>> _producedBy = new();

        // node name → all recipes that consume it
        private readonly Dictionary<string, List<CraftingRecipeDefinition>> _consumedBy = new();

        private readonly List<CraftingRecipeDefinition> _recipes;

        /// <summary>Recipes in topological order (raw materials first).</summary>
        public List<CraftingRecipeDefinition> TopologicalOrder { get; private set; }

        public CraftingGraph(List<CraftingRecipeDefinition> recipes)
        {
            _recipes = recipes ?? new List<CraftingRecipeDefinition>();
        }

        /// <summary>
        /// Build and validate the graph.
        /// Populates TopologicalOrder.
        /// Returns Critical if any cycle is detected.
        /// </summary>
        public ValidationStatus Build(LGD_ValidationReport report)
        {
            _producedBy.Clear();
            _consumedBy.Clear();

            // ── Index all recipes ─────────────────────────────────
            foreach (var recipe in _recipes)
            {
                if (recipe?.OutputItem == null) continue;
                string outName = recipe.OutputItem.name;

                if (!_producedBy.ContainsKey(outName))
                    _producedBy[outName] = new List<CraftingRecipeDefinition>();
                _producedBy[outName].Add(recipe);

                if (recipe.Inputs == null) continue;
                foreach (var slot in recipe.Inputs)
                {
                    if (slot.Item == null) continue;
                    string inName = slot.Item.name;
                    if (!_consumedBy.ContainsKey(inName))
                        _consumedBy[inName] = new List<CraftingRecipeDefinition>();
                    _consumedBy[inName].Add(recipe);
                }
            }

            // ── Kahn's algorithm topological sort ─────────────────
            // Build in-degree: how many distinct item types each recipe depends on
            var inDegree = new Dictionary<string, int>(); // recipe.name → in-degree
            var itemSources = new HashSet<string>(); // items that are sourced (no recipe)

            foreach (var recipe in _recipes)
            {
                if (recipe?.OutputItem == null) continue;
                inDegree.TryGetValue(recipe.name, out int deg);

                if (recipe.Inputs != null)
                {
                    foreach (var slot in recipe.Inputs)
                    {
                        if (slot.Item == null) continue;
                        // if nothing produces this item, it's a raw material source
                        if (!_producedBy.ContainsKey(slot.Item.name))
                            itemSources.Add(slot.Item.name);
                        else
                            inDegree[recipe.name] = deg + 1;
                    }
                }

                if (!inDegree.ContainsKey(recipe.name))
                    inDegree[recipe.name] = 0;
            }

            // Enqueue zero in-degree recipes (produce from raw materials)
            var queue  = new Queue<CraftingRecipeDefinition>();
            var sorted = new List<CraftingRecipeDefinition>();

            foreach (var recipe in _recipes)
            {
                if (recipe == null) continue;
                if (inDegree.TryGetValue(recipe.name, out int d) && d == 0)
                    queue.Enqueue(recipe);
            }

            while (queue.Count > 0)
            {
                var recipe = queue.Dequeue();
                sorted.Add(recipe);

                if (recipe.OutputItem == null) continue;
                string outName = recipe.OutputItem.name;

                // All recipes that consume outName can now have their in-degree reduced
                if (!_consumedBy.ContainsKey(outName)) continue;
                foreach (var downstream in _consumedBy[outName])
                {
                    if (downstream == null) continue;
                    inDegree[downstream.name]--;
                    if (inDegree[downstream.name] == 0)
                        queue.Enqueue(downstream);
                }
            }

            // ── Cycle detection ───────────────────────────────────
            if (sorted.Count != _recipes.Count)
            {
                var cycleRecipes = new List<string>();
                foreach (var r in _recipes)
                    if (r != null && !sorted.Contains(r))
                        cycleRecipes.Add(r.name);

                report.Add(ValidationStatus.Critical, "CraftingGraph",
                    $"Dependency cycle detected in recipes: [{string.Join(", ", cycleRecipes)}]. " +
                    "A recipe chain cannot produce its own inputs. Simulation aborted.",
                    suggestedFix: "Remove the circular dependency between the listed recipes.");

                TopologicalOrder = new List<CraftingRecipeDefinition>();
                return ValidationStatus.Critical;
            }

            TopologicalOrder = sorted;

            if (report.OverallStatus == ValidationStatus.Pass && sorted.Count > 0)
                report.Add(ValidationStatus.Info, "CraftingGraph",
                    $"Recipe graph built: {sorted.Count} recipes in topological order, " +
                    $"{itemSources.Count} raw material sources.");

            return ValidationStatus.Pass;
        }

        /// <summary>All recipes that produce the given item (can be multiple).</summary>
        public List<CraftingRecipeDefinition> GetRecipesProducing(string itemName)
            => _producedBy.TryGetValue(itemName, out var r) ? r : new List<CraftingRecipeDefinition>();

        /// <summary>All recipes that consume the given item.</summary>
        public List<CraftingRecipeDefinition> GetRecipesConsuming(string itemName)
            => _consumedBy.TryGetValue(itemName, out var r) ? r : new List<CraftingRecipeDefinition>();

        /// <summary>True if the item is a raw material (no recipe produces it).</summary>
        public bool IsRawMaterial(string itemName) => !_producedBy.ContainsKey(itemName);
    }
}
