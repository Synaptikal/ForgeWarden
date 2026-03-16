using System.Linq;
using LiveGameDev.Core;
using LiveGameDev.Core.Editor.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Main RSV EditorWindow.
    /// Hosts a tab bar linking to: Schema Browser | Schema Designer | Binding Inspector | Playground.
    /// Open via: Tools > ForgeWarden > RSV > Main Window
    /// </summary>
    public class RSV_MainWindow : EditorWindow
    {
        private const string MenuPath = "Tools/ForgeWarden/RSV/Main Window";

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<RSV_MainWindow>("Schema Validator");
            window.minSize = new Vector2(700, 480);
            window.Show();
        }

        // ── Tab state ─────────────────────────────────────────────
        private int _activeTab;
        private readonly string[] _tabLabels =
            { "Schema Browser", "Schema Designer", "Binding Inspector", "Playground" };

        // ── Sub-panels ────────────────────────────────────────────
        private RSV_SchemaBrowser      _schemaBrowser;
        private RSV_SchemaDesigner     _schemaDesigner;
        private RSV_BindingInspector   _bindingInspector;
        private RSV_PlaygroundTab      _playground;

        // ── Shared ────────────────────────────────────────────────
        private LGD_ReportPanel  _reportPanel;
        private LGD_ExportButton _exportButton;
        private LGD_StatusBadge  _statusBadge;
        private RSV_ReportFilterBar _reportFilterBar;
        private LGD_ValidationReport _currentReport;

        public void CreateGUI()
        {
            // ── Root layout ───────────────────────────────────────
            var root = rootVisualElement;
            root.styleSheets.Add(
                AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Packages/com.forgegames.livegamedev.rsv/Editor/UI/USS/RSV_Window.uss"));

            // ── Top bar ───────────────────────────────────────────
            var topBar = new VisualElement();
            topBar.AddToClassList("rsv-topbar");

            var validateAllBtn = new Button(OnValidateAll) { text = "▶ Validate All" };
            validateAllBtn.AddToClassList("rsv-validate-btn");

            _statusBadge  = new LGD_StatusBadge(ValidationStatus.Pass);
            _exportButton = new LGD_ExportButton();

            topBar.Add(validateAllBtn);
            topBar.Add(_statusBadge);
            topBar.Add(_exportButton);
            root.Add(topBar);

            // ── Tab bar ───────────────────────────────────────────
            var tabBar = new VisualElement();
            tabBar.AddToClassList("rsv-tabbar");
            for (int i = 0; i < _tabLabels.Length; i++)
            {
                var idx = i;
                var btn = new Button(() => SwitchTab(idx)) { text = _tabLabels[i] };
                btn.AddToClassList("rsv-tab-btn");
                tabBar.Add(btn);
            }
            root.Add(tabBar);

            // ── Content area ──────────────────────────────────────
            var content = new VisualElement();
            content.AddToClassList("rsv-content");

            _schemaBrowser    = new RSV_SchemaBrowser();
            _schemaDesigner   = new RSV_SchemaDesigner();
            _bindingInspector = new RSV_BindingInspector();
            _playground       = new RSV_PlaygroundTab();

            content.Add(_schemaBrowser);
            content.Add(_schemaDesigner);
            content.Add(_bindingInspector);
            content.Add(_playground);
            root.Add(content);

            // ── Report panel (bottom) ─────────────────────────────
            _reportFilterBar = new RSV_ReportFilterBar();
            _reportFilterBar.AddToClassList("rsv-report-filter-bar");
            _reportFilterBar.OnFilterChanged += OnReportFilterChanged;
            root.Add(_reportFilterBar);

            _reportPanel = new LGD_ReportPanel();
            _reportPanel.AddToClassList("rsv-report-panel");
            root.Add(_reportPanel);

            SwitchTab(0);

            // Register for selection changes
            Selection.selectionChanged += OnSelectionChange;
        }

        private void OnDestroy()
        {
            Selection.selectionChanged -= OnSelectionChange;
        }

        private void SwitchTab(int idx)
        {
            _activeTab = idx;
            _schemaBrowser.style.display    = idx == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _schemaDesigner.style.display   = idx == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            _bindingInspector.style.display = idx == 2 ? DisplayStyle.Flex : DisplayStyle.None;
            _playground.style.display       = idx == 3 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnValidateAll()
        {
            var results = RsvValidator.ValidateAllBindings();
            var merged  = new LGD_ValidationReport("RSV");
            foreach (var r in results)
                foreach (var e in r.Entries)
                    merged.AddEntry(e);

            _currentReport = merged;
            ApplyReportFilters();

            _statusBadge.SetStatus(merged.OverallStatus);
            _exportButton.Configure(merged,
                LiveGameDev.Core.Editor.LGD_PathUtility.GetDefaultOutputPath());
        }

        private void OnReportFilterChanged()
        {
            ApplyReportFilters();
        }

        private void ApplyReportFilters()
        {
            if (_currentReport == null) return;

            var filteredEntries = _reportFilterBar.FilterEntries(_currentReport).ToList();
            var filteredReport = new LGD_ValidationReport("RSV");
            foreach (var entry in filteredEntries)
                filteredReport.AddEntry(entry);

            _reportPanel.Populate(filteredReport);
        }

        private void OnSelectionChange()
        {
            _bindingInspector.OnSelectionChanged(Selection.activeObject);

            // Also handle schema selection
            if (Selection.activeObject is DataSchemaDefinition schema)
            {
                _schemaDesigner.LoadSchema(schema);
                SwitchTab(1); // Switch to Schema Designer tab
            }
        }
    }
}
