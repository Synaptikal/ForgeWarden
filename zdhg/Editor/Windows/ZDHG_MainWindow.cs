using System;
using System.Threading;
using LiveGameDev.Core.Editor;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

namespace LiveGameDev.ZDHG.Editor
{
    /// <summary>
    /// Main ZDHG EditorWindow.
    /// Open via: Tools > ForgeWarden > ZDHG > Heatmap Generator
    /// </summary>
    public class ZDHG_MainWindow : EditorWindow
    {
        private const string MenuPath = "Tools/ForgeWarden/ZDHG/Heatmap Generator";

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<ZDHG_MainWindow>("Zone Heatmap");
            window.minSize = new Vector2(600, 480);
            window.Show();
        }

        // ── Static regenerate request (from overlay) ──────────────
        private static bool _regenerateRequested;
        public static void RequestRegenerate() => _regenerateRequested = true;

        // ── State ─────────────────────────────────────────────────
        private HeatmapSettings _settings = new();
        private HeatmapResult   _result;
        private CancellationTokenSource _cts;

        // ── UI refs ───────────────────────────────────────────────
        private ProgressBar    _progressBar;
        private Button         _generateBtn;
        private Button         _cancelBtn;
        private Button         _exportPngBtn;
        private Button         _exportCsvBtn;
        private Label          _statusLabel;

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.forgegames.livegamedev.zdhg/Editor/UI/USS/ZDHG_Window.uss"));

            // ── Toolbar ───────────────────────────────────────────
            var toolbar = new VisualElement(); toolbar.AddToClassList("zdhg-toolbar");

            _generateBtn  = new Button(OnGenerateClicked)  { text = "▶ Generate" };
            _cancelBtn    = new Button(OnCancelClicked)    { text = "■ Cancel", visible = false };
            _exportPngBtn = new Button(OnExportPng)        { text = "Export PNG" };
            _exportCsvBtn = new Button(OnExportCsv)        { text = "Export CSV" };
            var exportMdBtn = new Button(OnExportMarkdown) { text = "Export MD"  };
            var snapshotBtn = new Button(OnTakeSnapshot)   { text = "📸"        };
            snapshotBtn.tooltip = "Take Heatmap Snapshot";
            _exportPngBtn.SetEnabled(false);
            _exportCsvBtn.SetEnabled(false);
            exportMdBtn.SetEnabled(false);
            snapshotBtn.SetEnabled(false);
            
            _statusLabel  = new Label("Ready.");

            toolbar.Add(_generateBtn);
            toolbar.Add(_cancelBtn);
            toolbar.Add(_exportPngBtn);
            toolbar.Add(_exportCsvBtn);
            toolbar.Add(exportMdBtn);
            toolbar.Add(snapshotBtn);
            toolbar.Add(_statusLabel);
            root.Add(toolbar);

            // ── Progress bar ──────────────────────────────────────
            _progressBar = new ProgressBar { title = "Generating…", value = 0f };
            _progressBar.style.display = DisplayStyle.None;
            root.Add(_progressBar);

            // ── Panels ────────────────────────────────────────────
            var split = new TwoPaneSplitView(0, 240f, TwoPaneSplitViewOrientation.Horizontal);

            var leftPane = new VisualElement();
            leftPane.Add(new ZDHG_LayersPanel(_settings));
            leftPane.Add(new ZDHG_ZonesPanel(_settings));

            var rightPane = new VisualElement();
            rightPane.Add(new ZDHG_ControlsPanel(_settings));

            split.Add(leftPane);
            split.Add(rightPane);
            root.Add(split);
        }

        private void Update()
        {
            if (_regenerateRequested)
            {
                _regenerateRequested = false;
                OnGenerateClicked();
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _result?.Dispose();
            ZDHG_TextureRenderer.Invalidate();
        }

        private void OnGenerateClicked()
        {
            _cts?.Cancel(); // Cancel any existing run
            _cts = new CancellationTokenSource();
            _generateBtn.SetEnabled(false);
            _cancelBtn.visible         = true;
            _progressBar.style.display = DisplayStyle.Flex;
            _progressBar.value         = 0f;
            _statusLabel.text          = "Generating…";

            EditorCoroutineUtility.StartCoroutine(RunGeneration(), this);
        }

        private void OnCancelClicked()
        {
            _cts?.Cancel();
            _statusLabel.text = "Cancelled.";
            ResetUI();
        }

        private IEnumerator RunGeneration()
        {
            var progress = new Progress<float>(v =>
            {
                _progressBar.value = v * 100f;
            });

            var task = ZDHG_Generator.GenerateHeatmapAsync(_settings, progress, _cts.Token);

            while (!task.IsCompleted) yield return null;

            ResetUI();

            if (task.IsCanceled)
            {
                _statusLabel.text = "Cancelled.";
                yield break;
            }

            if (task.IsFaulted)
            {
                _statusLabel.text = $"Error: {task.Exception?.InnerException?.Message}";
                Debug.LogException(task.Exception);
                yield break;
            }

            // Dispose old result before assigning new one to prevent leaks
            _result?.Dispose();
            
            _result = task.Result;
            ZDHG_SceneOverlay.CurrentResult   = _result;
            ZDHG_SceneOverlay.CurrentSettings = _settings;
            ZDHG_TextureRenderer.Invalidate();

            _exportPngBtn.SetEnabled(true);
            _exportCsvBtn.SetEnabled(true);
            _statusLabel.text = $"Done — {_result.TotalCells:N0} cells, " +
                                 $"{_result.DesertCellCount:N0} desert. " +
                                 $"Status: {_result.Report.OverallStatus}";
            SceneView.RepaintAll();
        }

        private void ResetUI()
        {
            _generateBtn.SetEnabled(true);
            _cancelBtn.visible         = false;
            _progressBar.style.display = DisplayStyle.None;
        }

        private void OnExportPng()
        {
            if (_result == null) return;
            var path = LGD_PathUtility.GetTimestampedFileName("Heatmap", ".png");
            ZDHG_Generator.ExportPng(_result, _settings,
                System.IO.Path.Combine(LGD_PathUtility.GetDefaultOutputPath(), path));
        }

        private void OnExportCsv()
        {
            if (_result == null) return;
            var path = LGD_PathUtility.GetTimestampedFileName("Heatmap", ".csv");
            ZDHG_Generator.ExportCsv(_result,
                System.IO.Path.Combine(LGD_PathUtility.GetDefaultOutputPath(), path));
        }

        private void OnExportMarkdown()
        {
            if (_result == null) return;
            var path = LGD_PathUtility.GetTimestampedFileName("Heatmap", ".md");
            ZDHG_Generator.ExportMarkdown(_result,
                System.IO.Path.Combine(LGD_PathUtility.GetDefaultOutputPath(), path));
        }

        private void OnTakeSnapshot()
        {
            if (_result == null) return;
            var path = AssetDatabase.GenerateUniqueAssetPath("Assets/ZDHG_Snapshot.asset");
            var snap = ScriptableObject.CreateInstance<HeatmapSnapshot>();
            snap.Capture(_result);
            AssetDatabase.CreateAsset(snap, path);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ZDHG] Snapshot saved to: {path}");
            _settings.DiffSnapshot = snap;
        }
    }
}
