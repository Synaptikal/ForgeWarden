using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Narrative Layer Manager — main Editor window.
    /// </summary>
    /// <remarks>
    /// Open via: Tools > ForgeWarden > NLM > Narrative Layer Manager
    ///       or: Window > Narrative Layer Manager
    /// 
    /// <para>3-column layout:</para>
    /// <list type="bullet">
    /// <item>Left: Layer List</item>
    /// <item>Center: Timeline + Beat Inspector</item>
    /// <item>Right: Object List / Diff</item>
    /// </list>
    /// 
    /// <para>Toolbar: Preview toggle | Validate | Conflicts | Diff | Export</para>
    /// <para>Bottom: Collapsible report panel</para>
    /// </remarks>
    public class NLM_MainWindow : EditorWindow
    {
        [MenuItem("Tools/ForgeWarden/NLM/Narrative Layer Manager", priority = 300)]
        [MenuItem("Window/Narrative Layer Manager", priority = 500)]
        public static void Open()
        {
            var w = GetWindow<NLM_MainWindow>("Narrative Layers");
            w.minSize = new Vector2(920, 540);
            w.Show();
        }

        #region State

        private NarrativeLayerDefinition _layer;
        private int _beatIndex = 0;
        private int _diffIndex = -1;
        private bool _diffMode;
        private string _searchQuery = "";
        private List<NLM_BeatDiff.DiffEntry> _diffResults = new();
        private List<NLM_ConflictDetector.ConflictEntry> _conflicts = new();

        #endregion

        #region UI Panels

        private NLM_LayerListPanel _layerList;
        private NLM_TimelineTrack _timeline;
        private NLM_BeatInspector _beatInspector;
        private NLM_ObjectListPanel _objectList;
        private NLM_ReportPanel _reportPanel;
        private Toggle _previewToggle;
        private Toggle _diffToggle;
        private TextField _searchField;
        private Label _statusBar;

        #endregion

        #region Unity Callbacks

        public void CreateGUI()
        {
            // Load USS
            var ussPath = "Packages/com.narrativelayermanager/Editor/UI/USS/NLM_Window.uss";
            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            if (uss == null)
            {
                // Try alternative path
                ussPath = "Assets/NLM/Editor/UI/USS/NLM_Window.uss";
                uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(ussPath);
            }
            if (uss != null) rootVisualElement.styleSheets.Add(uss);

            BuildUI();
            NLM_ShortcutHandler.RegisterWindow(this);
        }

        private void OnDestroy()
        {
            if (NLM_ScenePreviewApplicator.IsPreviewActive)
                NLM_ScenePreviewApplicator.EndPreview();
        }

        #endregion

        #region UI Building

        private void BuildUI()
        {
            var root = rootVisualElement;

            // Toolbar
            BuildToolbar(root);

            // Body
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            root.Add(body);

            // Left: Layer List
            _layerList = new NLM_LayerListPanel(OnLayerSelected);
            body.Add(_layerList);

            // Center: Timeline + Inspector
            var center = new VisualElement();
            center.style.flexGrow = 1;
            center.style.flexDirection = FlexDirection.Column;

            _timeline = new NLM_TimelineTrack(OnBeatSelected, OnDiffBeatSelected);
            _beatInspector = new NLM_BeatInspector();
            _beatInspector.style.flexGrow = 1;

            center.Add(_timeline);
            center.Add(_beatInspector);
            body.Add(center);

            // Right: Object List
            _objectList = new NLM_ObjectListPanel();
            body.Add(_objectList);

            // Bottom: Report Panel
            _reportPanel = new NLM_ReportPanel();
            root.Add(_reportPanel);

            _layerList.Refresh();
        }

        private void BuildToolbar(VisualElement root)
        {
            var tb = new VisualElement();
            tb.AddToClassList("nlm-toolbar");
            tb.style.flexDirection = FlexDirection.Row;
            tb.style.alignItems = Align.Center;
            tb.style.flexWrap = Wrap.Wrap;

            // Preview Toggle
            _previewToggle = new Toggle("Preview") { value = false };
            _previewToggle.AddToClassList("nlm-toolbar-button");
            _previewToggle.RegisterValueChangedCallback(OnPreviewToggled);

            // Diff Toggle
            _diffToggle = new Toggle("Diff Mode") { value = false };
            _diffToggle.AddToClassList("nlm-toolbar-button");
            _diffToggle.RegisterValueChangedCallback(OnDiffModeToggled);

            // Action Buttons
            var validateBtn = new Button(OnValidate) { text = "✓ Validate" };
            validateBtn.AddToClassList("nlm-toolbar-button");

            var conflictBtn = new Button(OnCheckConflicts) { text = "⚠ Conflicts" };
            conflictBtn.AddToClassList("nlm-toolbar-button");

            var exportMdBtn = new Button(() => OnExport("md")) { text = "Export .md" };
            exportMdBtn.AddToClassList("nlm-toolbar-button");

            var exportCsvBtn = new Button(() => OnExport("csv")) { text = "Export .csv" };
            exportCsvBtn.AddToClassList("nlm-toolbar-button");

            var settingsBtn = new Button(OnOpenSettings) { text = "⚙" };
            settingsBtn.AddToClassList("nlm-toolbar-button");

            // Search Field
            _searchField = new TextField();
            _searchField.AddToClassList("nlm-search-field");
            _searchField.style.width = 150;
            _searchField.RegisterValueChangedCallback(OnSearchChanged);

            // Status Bar
            _statusBar = new Label("Open a Narrative Layer from the left panel.");
            _statusBar.style.flexGrow = 1;
            _statusBar.style.marginLeft = 12;
            _statusBar.style.color = new Color(0.65f, 0.65f, 0.65f);

            // Add all to toolbar
            tb.Add(_previewToggle);
            tb.Add(_diffToggle);
            tb.Add(validateBtn);
            tb.Add(conflictBtn);
            tb.Add(exportMdBtn);
            tb.Add(exportCsvBtn);
            tb.Add(settingsBtn);
            tb.Add(_searchField);
            tb.Add(_statusBar);

            root.Add(tb);
        }

        #endregion

        #region Event Handlers

        private void OnLayerSelected(NarrativeLayerDefinition layer)
        {
            if (NLM_ScenePreviewApplicator.IsPreviewActive)
            {
                NLM_ScenePreviewApplicator.EndPreview();
                _previewToggle.SetValueWithoutNotify(false);
            }

            _layer = layer;
            _beatIndex = 0;
            _diffIndex = -1;
            _diffResults.Clear();
            _conflicts.Clear();

            _timeline.SetLayer(layer);
            SelectBeat(0);
            _statusBar.text = $"Layer '{layer?.name}' — {layer?.BeatCount ?? 0} beats.";
        }

        private void OnBeatSelected(int i)
        {
            _beatIndex = i;
            SelectBeat(i);
        }

        private void OnDiffBeatSelected(int i)
        {
            _diffIndex = i;
            if (!_diffMode) return;
            RunDiff();
        }

        private void OnPreviewToggled(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
            {
                var beat = _layer?.GetBeat(_beatIndex);
                if (beat != null)
                    NLM_ScenePreviewApplicator.BeginPreview(beat);
                _statusBar.text = "Preview ACTIVE — scene reflects selected beat. Scene NOT dirtied.";
            }
            else
            {
                NLM_ScenePreviewApplicator.EndPreview();
                _statusBar.text = "Preview ended — scene fully restored.";
            }
        }

        private void OnDiffModeToggled(ChangeEvent<bool> evt)
        {
            _diffMode = evt.newValue;
            _statusBar.text = _diffMode
                ? "Diff Mode: Shift+click a second beat on the timeline to compare."
                : "Diff mode off.";
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _searchQuery = evt.newValue.ToLower();
            RefreshObjectList();
        }

        private void OnValidate()
        {
            if (_layer == null) { _statusBar.text = "No layer selected."; return; }
            var report = _layer.Validate();
            _reportPanel.ShowReport(report);
            _statusBar.text = $"Validation complete: {report.OverallStatus} — {report.Entries.Count} entries.";
        }

        private void OnCheckConflicts()
        {
            if (_layer == null) { _statusBar.text = "No layer selected."; return; }
            var bindings = NLM_BeatDiff.CollectAll();
            _conflicts = NLM_ConflictDetector.Analyze(_layer, bindings);
            _reportPanel.ShowConflicts(_conflicts);
            _statusBar.text = $"Conflict check complete: {_conflicts.Count} issue(s) across {bindings.Count} binding(s).";
            foreach (var c in _conflicts)
                Debug.LogWarning($"[NLM] {c.Type}: {c.Message}", c.AffectedObject);
        }

        private void OnExport(string format)
        {
            if (_layer == null) { _statusBar.text = "No layer selected."; return; }
            var report = _layer.Validate();
            var content = format == "csv" ? report.ToCsv() : report.ToMarkdown();
            var file = NLM_EditorPathUtility.WriteReport(content, "NLM_Report", $".{format}");
            _statusBar.text = $"Report exported: {file}";
        }

        private void OnOpenSettings()
        {
            var current = NLM_EditorPathUtility.GetOutputPath();
            var newPath = EditorUtility.OpenFolderPanel("NLM Export Folder", current, "");
            if (!string.IsNullOrEmpty(newPath))
            {
                NLM_EditorPathUtility.SetOutputPath(newPath);
                _statusBar.text = $"Export path set: {newPath}";
            }
        }

        #endregion

        #region Private Methods

        private void SelectBeat(int index)
        {
            var beat = _layer?.GetBeat(index);
            _beatInspector.Show(beat);
            _timeline.SetSelected(index);

            if (_previewToggle.value && beat != null)
            {
                if (NLM_ScenePreviewApplicator.IsPreviewActive)
                    NLM_ScenePreviewApplicator.TransitionTo(beat);
                else
                    NLM_ScenePreviewApplicator.BeginPreview(beat);
            }
            RefreshObjectList();
        }

        private void RunDiff()
        {
            var beatA = _layer?.GetBeat(_beatIndex);
            var beatB = _layer?.GetBeat(_diffIndex);
            if (beatA == null || beatB == null) return;
            _diffResults = NLM_BeatDiff.Compute(beatA, beatB);
            _objectList.ShowDiff(_diffResults);
            _timeline.SetDiff(_diffIndex);
            _statusBar.text = $"Diff: {_diffResults.Count} object(s) change between '{beatA.BeatName}' and '{beatB.BeatName}'.";
        }

        private void RefreshObjectList()
        {
            var beat = _layer?.GetBeat(_beatIndex);
            var bindings = NLM_BeatDiff.CollectAll();

            // Filter by search query
            if (!string.IsNullOrEmpty(_searchQuery))
            {
                bindings = bindings.FindAll(b =>
                    b != null &&
                    b.gameObject.name.ToLower().Contains(_searchQuery));
            }

            _objectList.ShowBindings(bindings, beat?.State);
        }

        #endregion

        #region Public API for Shortcuts

        /// <summary>
        /// Toggles the preview mode.
        /// </summary>
        public void TogglePreview()
        {
            _previewToggle.value = !_previewToggle.value;
        }

        /// <summary>
        /// Copies the currently selected rule.
        /// </summary>
        public void CopySelected()
        {
            // Implementation depends on selection system
            _statusBar.text = "Copy: Select a rule in the Inspector to copy.";
        }

        /// <summary>
        /// Pastes a copied rule.
        /// </summary>
        public void Paste()
        {
            // Implementation depends on selection system
            _statusBar.text = "Paste: Select a binding in the Inspector to paste.";
        }

        /// <summary>
        /// Deletes the currently selected item.
        /// </summary>
        public void DeleteSelected()
        {
            // Implementation depends on selection system
        }

        /// <summary>
        /// Focuses the search field.
        /// </summary>
        public void FocusSearch()
        {
            _searchField.Focus();
        }

        #endregion
    }
}
