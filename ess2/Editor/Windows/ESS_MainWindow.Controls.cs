using System;
using LiveGameDev.ESS;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    public partial class ESS_MainWindow
    {
        // ── Controls Panel ───────────────────────────────────────
        private void DrawControlsPanel()
        {
            _resultsScroll = EditorGUILayout.BeginScrollView(_resultsScroll);

            EditorGUILayout.LabelField("Simulation Configuration", _headerStyle);

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Basic Settings", _subHeaderStyle);
            _config.SimulationDays = EditorGUILayout.IntField("Simulation Days", _config.SimulationDays);
            _config.PlayerCount    = EditorGUILayout.IntField("Player Count",    _config.PlayerCount);
            _config.Seed           = EditorGUILayout.IntField("Random Seed",     _config.Seed);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Economy Definitions", _subHeaderStyle);
            DrawArrayField("Tracked Items", ref _config.TrackedItems);
            DrawArrayField("Sources",       ref _config.Sources);
            DrawArrayField("Sinks",         ref _config.Sinks);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Player Mix", _subHeaderStyle);
            DrawPlayerMixEditor();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("Crafting Recipes", _subHeaderStyle);
            DrawRecipeList();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(20);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Validate Config", GUILayout.Width(150), GUILayout.Height(30)))
                ValidateConfiguration();

            GUI.enabled = !_isSimulating;
            if (GUILayout.Button("Run Simulation", GUILayout.Width(150), GUILayout.Height(30)))
                RunSimulation();
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private void DrawArrayField<T>(string label, ref T[] array) where T : UnityEngine.Object
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);

            int count    = array?.Length ?? 0;
            int newCount = EditorGUILayout.IntField("Size", count);

            if (newCount != count)
                Array.Resize(ref array, newCount);

            for (int i = 0; i < newCount; i++)
            {
                array[i] = (T)EditorGUILayout.ObjectField(
                    $"Element {i}", array[i], typeof(T), false);
            }
        }

        private void DrawPlayerMixEditor()
        {
            EditorGUILayout.HelpBox(
                "Player Mix defines the distribution of player archetypes. " +
                "Percentages should sum to 100%.", MessageType.Info);
        }

        private void DrawRecipeList()
        {
            EditorGUILayout.LabelField($"Recipes: {_recipes.Count}", EditorStyles.miniLabel);

            for (int i = 0; i < _recipes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _recipes[i] = (CraftingRecipeDefinition)EditorGUILayout.ObjectField(
                    _recipes[i], typeof(CraftingRecipeDefinition), false);

                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    _recipes.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+ Add Recipe"))
                _recipes.Add(null);
        }
    }
}
