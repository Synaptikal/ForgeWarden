using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    public partial class ESS_MainWindow
    {
        // ── Library Panel ────────────────────────────────────────
        private void DrawLibraryPanel()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical(GUILayout.Width(150));
            DrawLibraryTabs();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            DrawLibraryContent();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLibraryTabs()
        {
            GUILayout.Label("Definitions", _subHeaderStyle);

            foreach (LibraryTab tab in Enum.GetValues(typeof(LibraryTab)))
            {
                bool isSelected = _currentLibraryTab == tab;
                GUIStyle style = isSelected ? EditorStyles.selectionRect : GUIStyle.none;

                EditorGUILayout.BeginHorizontal(style);
                if (GUILayout.Button(tab.ToString(), EditorStyles.label))
                {
                    _currentLibraryTab = tab;
                    _selectedAsset = null;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(20);
            GUILayout.Label("Create New", _subHeaderStyle);

            if (GUILayout.Button("+ Item Category")) CreateDefinition<ItemCategoryDefinition>("ItemCategory");
            if (GUILayout.Button("+ Item"))         CreateDefinition<ItemDefinition>("Item");
            if (GUILayout.Button("+ Source"))       CreateDefinition<SourceDefinition>("Source");
            if (GUILayout.Button("+ Sink"))         CreateDefinition<SinkDefinition>("Sink");
            if (GUILayout.Button("+ Recipe"))       CreateDefinition<CraftingRecipeDefinition>("CraftingRecipe");
            if (GUILayout.Button("+ Profile"))      CreateDefinition<PlayerProfileDefinition>("PlayerProfile");
        }

        private void DrawLibraryContent()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Search:", GUILayout.Width(50));
            _librarySearch = EditorGUILayout.TextField(_librarySearch);
            EditorGUILayout.EndHorizontal();

            _libraryScroll = EditorGUILayout.BeginScrollView(_libraryScroll);

            switch (_currentLibraryTab)
            {
                case LibraryTab.Categories: DrawDefinitionList(FindDefinitions<ItemCategoryDefinition>()); break;
                case LibraryTab.Items:      DrawDefinitionList(FindDefinitions<ItemDefinition>());         break;
                case LibraryTab.Sources:    DrawDefinitionList(FindDefinitions<SourceDefinition>());       break;
                case LibraryTab.Sinks:      DrawDefinitionList(FindDefinitions<SinkDefinition>());         break;
                case LibraryTab.Recipes:    DrawDefinitionList(FindDefinitions<CraftingRecipeDefinition>()); break;
                case LibraryTab.Profiles:   DrawDefinitionList(FindDefinitions<PlayerProfileDefinition>()); break;
            }

            EditorGUILayout.EndScrollView();

            if (_selectedAsset != null)
            {
                EditorGUILayout.Space(10);
                EditorGUILayout.LabelField("Inspector", _subHeaderStyle);
                UnityEditor.Editor.CreateEditor(_selectedAsset).OnInspectorGUI();
            }
        }

        private void DrawDefinitionList<T>(IEnumerable<T> definitions) where T : UnityEngine.Object
        {
            var filtered = definitions.Where(d =>
                string.IsNullOrEmpty(_librarySearch) ||
                d.name.ToLower().Contains(_librarySearch.ToLower()));

            foreach (var def in filtered)
            {
                EditorGUILayout.BeginHorizontal();

                bool isSelected = _selectedAsset == def;
                GUIStyle style = isSelected ? EditorStyles.selectionRect : GUIStyle.none;

                EditorGUILayout.BeginHorizontal(style);
                if (GUILayout.Button(def.name, EditorStyles.label))
                {
                    _selectedAsset = def;
                    EditorGUIUtility.PingObject(def);
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Edit", GUILayout.Width(50)))
                    Selection.activeObject = def;

                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
