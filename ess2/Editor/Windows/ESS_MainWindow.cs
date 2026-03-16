using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LiveGameDev.Core;
using LiveGameDev.ESS;
using UnityEditor;
using UnityEngine;

namespace LiveGameDev.ESS.Editor
{
    /// <summary>
    /// Main window for the ForgeWarden Economy Simulation Sandbox.
    /// Provides a complete interface for configuring, running, and analyzing economy simulations.
    /// Split into partial classes by concern:
    ///   ESS_MainWindow.cs          — Core: fields, lifecycle, toolbar, actions, helpers
    ///   ESS_MainWindow.Library.cs  — Library panel (definitions browser)
    ///   ESS_MainWindow.Controls.cs — Controls panel (simulation config)
    ///   ESS_MainWindow.Results.cs  — Results panel (charts and metrics)
    ///   ESS_MainWindow.Alerts.cs   — Alerts panel
    /// </summary>
    public partial class ESS_MainWindow : EditorWindow
    {
        [MenuItem("Tools/ForgeWarden/ESS/Economy Sandbox")]
        public static void ShowWindow()
        {
            var window = GetWindow<ESS_MainWindow>("Economy Simulation");
            window.minSize = new Vector2(1000, 700);
            window.Show();
        }

        // ── Panel Enums ──────────────────────────────────────────
        private enum MainPanel { Library, Controls, Results, Alerts }
        private enum LibraryTab { Items, Categories, Sources, Sinks, Recipes, Profiles }
        private enum ResultsTab { Overview, Prices, Supply, Wealth, Metrics }

        // ── State ────────────────────────────────────────────────
        private MainPanel _currentPanel = MainPanel.Controls;
        private LibraryTab _currentLibraryTab = LibraryTab.Items;
        private ResultsTab _currentResultsTab = ResultsTab.Overview;

        // ── Simulation Config ────────────────────────────────────
        private SimConfig _config = new SimConfig();
        private List<CraftingRecipeDefinition> _recipes = new List<CraftingRecipeDefinition>();
        private SimulationResultV2 _lastResult;
        private bool _isSimulating;
        private float _simulationProgress;
        private CancellationTokenSource _simulationCts;

        // ── Library State ────────────────────────────────────────
        private Vector2 _libraryScroll;
        private string _librarySearch = "";
        private UnityEngine.Object _selectedAsset;

        // ── Results State ────────────────────────────────────────
        private Vector2 _resultsScroll;
        private ESS_ChartRenderer.ChartState _chartState = new ESS_ChartRenderer.ChartState();
        private int _selectedDay = -1;
        private string _selectedItemFilter = "";

        // ── Alert State ──────────────────────────────────────────
        private Vector2 _alertsScroll;
        private ValidationStatus _alertFilter = ValidationStatus.Warning;

        // ── GUI Styles ───────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _subHeaderStyle;
        private GUIStyle _boxStyle;
        private bool _stylesInitialized;

        // ── Unity Lifecycle ──────────────────────────────────────
        private void OnEnable()
        {
            LoadDefinitions();
            InitializeDefaultConfig();
        }

        private void OnDisable()
        {
            _simulationCts?.Cancel();
            _simulationCts?.Dispose();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                margin = new RectOffset(10, 10, 10, 10)
            };

            _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12,
                margin = new RectOffset(5, 5, 5, 5)
            };

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10),
                margin = new RectOffset(5, 5, 5, 5)
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitStyles();

            EditorGUILayout.BeginVertical();
            DrawToolbar();
            EditorGUILayout.Space(5);

            switch (_currentPanel)
            {
                case MainPanel.Library:   DrawLibraryPanel();  break;
                case MainPanel.Controls:  DrawControlsPanel(); break;
                case MainPanel.Results:   DrawResultsPanel();  break;
                case MainPanel.Alerts:    DrawAlertsPanel();   break;
            }

            EditorGUILayout.EndVertical();

            if (_isSimulating) Repaint();
        }

        // ── Toolbar ──────────────────────────────────────────────
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(30));

            GUILayout.Label("ESS v2.0", EditorStyles.boldLabel, GUILayout.Width(80));
            GUILayout.Space(10);

            if (GUILayout.Toggle(_currentPanel == MainPanel.Library, "Library", EditorStyles.toolbarButton))
                _currentPanel = MainPanel.Library;

            if (GUILayout.Toggle(_currentPanel == MainPanel.Controls, "Controls", EditorStyles.toolbarButton))
                _currentPanel = MainPanel.Controls;

            GUI.enabled = _lastResult != null;
            if (GUILayout.Toggle(_currentPanel == MainPanel.Results, "Results", EditorStyles.toolbarButton))
                _currentPanel = MainPanel.Results;

            if (GUILayout.Toggle(_currentPanel == MainPanel.Alerts, "Alerts", EditorStyles.toolbarButton))
                _currentPanel = MainPanel.Alerts;
            GUI.enabled = true;

            GUILayout.FlexibleSpace();

            if (_isSimulating)
            {
                if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    _simulationCts?.Cancel();
            }
            else if (_lastResult != null)
            {
                if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    ShowExportMenu();
            }

            EditorGUILayout.EndHorizontal();

            if (_isSimulating)
            {
                Rect progressRect = GUILayoutUtility.GetRect(position.width, 20);
                EditorGUI.ProgressBar(progressRect, _simulationProgress,
                    $"Simulating... {_simulationProgress:P0}");
            }
        }

        // ── Actions ──────────────────────────────────────────────
        private void ValidateConfiguration()
        {
            var report = _config.Validate();

            if (report.HasErrors)
            {
                var errors = string.Join("\n", report.Entries
                    .Where(e => e.Status == ValidationStatus.Error)
                    .Select(e => e.Message));
                EditorUtility.DisplayDialog("Validation Failed",
                    $"Configuration has errors:\n\n{errors}", "OK");
            }
            else if (report.Entries.Any(e => e.Status == ValidationStatus.Warning))
            {
                var warnings = string.Join("\n", report.Entries
                    .Where(e => e.Status == ValidationStatus.Warning)
                    .Select(e => e.Message));
                EditorUtility.DisplayDialog("Validation Passed with Warnings",
                    $"Configuration is valid but has warnings:\n\n{warnings}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Validation Passed", "Configuration is valid!", "OK");
            }
        }

        private async void RunSimulation()
        {
            _isSimulating = true;
            _simulationProgress = 0f;
            _simulationCts = new CancellationTokenSource();

            try
            {
                var progress = new Progress<float>(p => _simulationProgress = p);
                _lastResult = await ESS_SimulatorV2.RunAsync(
                    _config, _recipes, progress, _simulationCts.Token);

                _currentPanel = MainPanel.Results;
                _currentResultsTab = ResultsTab.Overview;

                EditorUtility.DisplayDialog("Simulation Complete",
                    $"Simulation finished with {_lastResult.Alerts.Count} alerts.", "OK");
            }
            catch (OperationCanceledException)
            {
                EditorUtility.DisplayDialog("Simulation Cancelled",
                    "Simulation was cancelled by user.", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Simulation Error",
                    $"Error during simulation:\n{ex.Message}", "OK");
                Debug.LogException(ex);
            }
            finally
            {
                _isSimulating = false;
                _simulationCts?.Dispose();
                _simulationCts = null;
            }
        }

        private void ShowExportMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Export to CSV"), false, () =>
            {
                string path = EditorUtility.SaveFilePanel("Export CSV", "", "simulation_results", "csv");
                if (!string.IsNullOrEmpty(path))
                    ESS_ExportUtility.ExportToCsv(_lastResult, path);
            });
            menu.AddItem(new GUIContent("Export to JSON"), false, () =>
            {
                string path = EditorUtility.SaveFilePanel("Export JSON", "", "simulation_results", "json");
                if (!string.IsNullOrEmpty(path))
                    ESS_ExportUtility.ExportToJson(_lastResult, path);
            });
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Export Alerts to CSV"), false, () =>
            {
                string path = EditorUtility.SaveFilePanel("Export Alerts CSV", "", "simulation_alerts", "csv");
                if (!string.IsNullOrEmpty(path))
                    ESS_ExportUtility.ExportAlertsToCsv(_lastResult.Alerts, path);
            });
            menu.ShowAsContext();
        }

        // ── Helpers ──────────────────────────────────────────────
        private void LoadDefinitions()
        {
            // Called on Enable to refresh the library.
        }

        private void InitializeDefaultConfig()
        {
            _config.SimulationDays = 90;
            _config.PlayerCount = 1000;
            _config.Seed = 42;
        }

        private IEnumerable<T> FindDefinitions<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null) yield return asset;
            }
        }

        private void CreateDefinition<T>(string defaultName) where T : ScriptableObject
        {
            string path = EditorUtility.SaveFilePanelInProject(
                $"Create {typeof(T).Name}", $"New{defaultName}", "asset",
                $"Enter file name for the new {typeof(T).Name}");

            if (string.IsNullOrEmpty(path)) return;

            T asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }
    }
}
