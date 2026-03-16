using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Preferences window for RSV configuration settings.
    /// Provides UI for editing all RSV settings with auto-save support.
    /// </summary>
    public class RSV_PreferencesWindow : EditorWindow
    {
        private RsvConfigurationAsset _config;
        private VisualElement _root;
        private bool _isDirty;
        private double _lastChangeTime;
        private const float AUTO_SAVE_DELAY = 1f;

        // UI Elements
        private SliderInt _maxRemoteResponseSlider;
        private SliderInt _maxLocalFileSlider;
        private SliderInt _streamingThresholdSlider;
        private SliderInt _maxNestingDepthSlider;
        private SliderInt _maxSchemaNodesSlider;
        private SliderInt _maxEnumValuesSlider;
        private SliderInt _schemaCacheDurationSlider;
        private SliderInt _urlCacheDurationSlider;
        private SliderInt _maxSchemaCacheSizeSlider;
        private SliderInt _maxUrlCacheSizeSlider;
        private SliderInt _httpTimeoutSlider;
        private SliderInt _maxHttpRetriesSlider;
        private Slider _parallelismRatioSlider;
        private TextField _historyFilePathField;
        private TextField _urlWhitelistField;
        private TextField _urlBlacklistField;
        private Toggle _autoSaveToggle;
        private Label _statusLabel;
        private Button _saveButton;
        private Button _resetButton;

        [MenuItem("Tools/ForgeWarden/RSV/Preferences")]
        public static void ShowWindow()
        {
            var window = GetWindow<RSV_PreferencesWindow>("RSV Preferences");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        private void OnEnable()
        {
            _config = RsvConfigurationAsset.GetOrCreate();
            _config.LoadFromStaticConfiguration();
        }

        public void CreateGUI()
        {
            _root = rootVisualElement;
            _root.AddToClassList("rsv-preferences-window");

            // Load USS styles if available
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Packages/com.forgegames.livegamedev.rsv/Editor/UI/USS/RSV_Styles.uss");
            if (styleSheet != null)
            {
                _root.styleSheets.Add(styleSheet);
            }

            BuildUI();
            BindValues();
        }

        private void Update()
        {
            // Auto-save check
            if (_isDirty && _config.autoSave)
            {
                var elapsed = EditorApplication.timeSinceStartup - _lastChangeTime;
                if (elapsed >= AUTO_SAVE_DELAY)
                {
                    SaveConfiguration();
                }
            }
        }

        private void BuildUI()
        {
            // Header
            var header = new Label("RSV Preferences") { name = "header" };
            header.AddToClassList("rsv-panel-header");
            _root.Add(header);

            // Status bar
            _statusLabel = new Label("Ready") { name = "status-label" };
            _statusLabel.AddToClassList("rsv-status-label");
            _root.Add(_statusLabel);

            // Scroll view for content
            var scrollView = new ScrollView { name = "content-scroll" };
            scrollView.AddToClassList("rsv-preferences-scroll");

            // File Size Limits Section
            scrollView.Add(CreateSectionHeader("File Size Limits"));
            
            _maxRemoteResponseSlider = CreateSliderInt("Max Remote Response Size", 1, 100, "MB");
            scrollView.Add(CreateSliderRow(_maxRemoteResponseSlider, 
                "Maximum allowed size for remote HTTP responses"));

            _maxLocalFileSlider = CreateSliderInt("Max Local File Size", 1, 500, "MB");
            scrollView.Add(CreateSliderRow(_maxLocalFileSlider, 
                "Maximum allowed size for local JSON files"));

            _streamingThresholdSlider = CreateSliderInt("Streaming Threshold", 1, 50, "MB");
            scrollView.Add(CreateSliderRow(_streamingThresholdSlider, 
                "Threshold for using streaming JSON parser"));

            // Validation Limits Section
            scrollView.Add(CreateSectionHeader("Validation Limits"));

            _maxNestingDepthSlider = CreateSliderInt("Max Nesting Depth", 1, 100, "levels");
            scrollView.Add(CreateSliderRow(_maxNestingDepthSlider, 
                "Maximum nesting depth allowed in JSON structures"));

            _maxSchemaNodesSlider = CreateSliderInt("Max Schema Nodes", 100, 50000, "nodes");
            scrollView.Add(CreateSliderRow(_maxSchemaNodesSlider, 
                "Maximum number of nodes allowed in a schema"));

            _maxEnumValuesSlider = CreateSliderInt("Max Enum Values", 10, 5000, "values");
            scrollView.Add(CreateSliderRow(_maxEnumValuesSlider, 
                "Maximum number of enum values per field"));

            // Cache Settings Section
            scrollView.Add(CreateSectionHeader("Cache Settings"));

            _schemaCacheDurationSlider = CreateSliderInt("Schema Cache Duration", 1, 120, "min");
            scrollView.Add(CreateSliderRow(_schemaCacheDurationSlider, 
                "Duration for caching compiled schemas"));

            _urlCacheDurationSlider = CreateSliderInt("URL Cache Duration", 1, 60, "min");
            scrollView.Add(CreateSliderRow(_urlCacheDurationSlider, 
                "Duration for caching URL responses"));

            _maxSchemaCacheSizeSlider = CreateSliderInt("Max Schema Cache Size", 10, 5000, "entries");
            scrollView.Add(CreateSliderRow(_maxSchemaCacheSizeSlider, 
                "Maximum number of entries in schema cache"));

            _maxUrlCacheSizeSlider = CreateSliderInt("Max URL Cache Size", 10, 500, "entries");
            scrollView.Add(CreateSliderRow(_maxUrlCacheSizeSlider, 
                "Maximum number of entries in URL cache"));

            // HTTP Settings Section
            scrollView.Add(CreateSectionHeader("HTTP Settings"));

            _httpTimeoutSlider = CreateSliderInt("HTTP Timeout", 5, 300, "sec");
            scrollView.Add(CreateSliderRow(_httpTimeoutSlider, 
                "Timeout for HTTP requests"));

            _maxHttpRetriesSlider = CreateSliderInt("Max HTTP Retries", 0, 10, "attempts");
            scrollView.Add(CreateSliderRow(_maxHttpRetriesSlider, 
                "Maximum retry attempts for failed requests"));

            // Parallel Validation Section
            scrollView.Add(CreateSectionHeader("Parallel Validation"));

            _parallelismRatioSlider = new Slider("Parallelism Ratio", 0.1f, 1f) 
            { 
                showInputField = true,
                name = "parallelism-slider"
            };
            _parallelismRatioSlider.AddToClassList("rsv-slider");
            scrollView.Add(CreateSliderRow(_parallelismRatioSlider, 
                "Ratio of CPU cores to use for parallel validation"));

            // URL Validation Section
            scrollView.Add(CreateSectionHeader("URL Validation"));

            _urlWhitelistField = new TextField("URL Whitelist") 
            { 
                multiline = true,
                tooltip = "One pattern per line. Empty = allow all HTTPS."
            };
            _urlWhitelistField.AddToClassList("rsv-text-area");
            scrollView.Add(CreateFieldRow(_urlWhitelistField, 
                "Allowed URL patterns (one per line)"));

            _urlBlacklistField = new TextField("URL Blacklist") 
            { 
                multiline = true,
                tooltip = "One pattern per line."
            };
            _urlBlacklistField.AddToClassList("rsv-text-area");
            scrollView.Add(CreateFieldRow(_urlBlacklistField, 
                "Blocked URL patterns (one per line)"));

            // History Settings Section
            scrollView.Add(CreateSectionHeader("History Settings"));

            _historyFilePathField = new TextField("History File Path");
            _historyFilePathField.AddToClassList("rsv-text-field");
            scrollView.Add(CreateFieldRow(_historyFilePathField, 
                "Path for validation history storage"));

            // Auto-Save Section
            scrollView.Add(CreateSectionHeader("Auto-Save"));

            _autoSaveToggle = new Toggle("Enable Auto-Save") { name = "autosave-toggle" };
            _autoSaveToggle.AddToClassList("rsv-toggle");
            scrollView.Add(_autoSaveToggle);

            _root.Add(scrollView);

            // Button row
            var buttonRow = new VisualElement { name = "button-row" };
            buttonRow.AddToClassList("rsv-button-row");

            _saveButton = new Button(SaveConfiguration) { text = "Save" };
            _saveButton.AddToClassList("rsv-save-btn");

            _resetButton = new Button(ResetToDefaults) { text = "Reset to Defaults" };
            _resetButton.AddToClassList("rsv-reset-btn");

            buttonRow.Add(_saveButton);
            buttonRow.Add(_resetButton);
            _root.Add(buttonRow);
        }

        private void BindValues()
        {
            // File Size Limits
            _maxRemoteResponseSlider.value = _config.maxRemoteResponseSizeMB;
            _maxLocalFileSlider.value = _config.maxLocalFileSizeMB;
            _streamingThresholdSlider.value = _config.streamingThresholdMB;

            // Validation Limits
            _maxNestingDepthSlider.value = _config.maxNestingDepth;
            _maxSchemaNodesSlider.value = _config.maxSchemaNodes;
            _maxEnumValuesSlider.value = _config.maxEnumValues;

            // Cache Settings
            _schemaCacheDurationSlider.value = _config.schemaCacheDurationMinutes;
            _urlCacheDurationSlider.value = _config.urlCacheDurationMinutes;
            _maxSchemaCacheSizeSlider.value = _config.maxSchemaCacheSize;
            _maxUrlCacheSizeSlider.value = _config.maxUrlCacheSize;

            // HTTP Settings
            _httpTimeoutSlider.value = _config.httpTimeoutSeconds;
            _maxHttpRetriesSlider.value = _config.maxHttpRetries;

            // Parallel Validation
            _parallelismRatioSlider.value = _config.defaultParallelismRatio;

            // URL Validation
            _urlWhitelistField.value = _config.urlWhitelist;
            _urlBlacklistField.value = _config.urlBlacklist;

            // History
            _historyFilePathField.value = _config.historyFilePath;

            // Auto-Save
            _autoSaveToggle.value = _config.autoSave;

            // Register change callbacks
            RegisterChangeCallbacks();
        }

        private void RegisterChangeCallbacks()
        {
            _maxRemoteResponseSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _maxLocalFileSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _streamingThresholdSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _maxNestingDepthSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _maxSchemaNodesSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _maxEnumValuesSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _schemaCacheDurationSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _urlCacheDurationSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _maxSchemaCacheSizeSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _maxUrlCacheSizeSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _httpTimeoutSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _maxHttpRetriesSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _parallelismRatioSlider.RegisterValueChangedCallback(_ => OnValueChanged());
            _urlWhitelistField.RegisterValueChangedCallback(_ => OnValueChanged());
            _urlBlacklistField.RegisterValueChangedCallback(_ => OnValueChanged());
            _historyFilePathField.RegisterValueChangedCallback(_ => OnValueChanged());
            _autoSaveToggle.RegisterValueChangedCallback(_ => OnValueChanged());
        }

        private void OnValueChanged()
        {
            _isDirty = true;
            _lastChangeTime = EditorApplication.timeSinceStartup;
            UpdateStatus("Unsaved changes...");
        }

        private void SaveConfiguration()
        {
            // Update config from UI
            _config.maxRemoteResponseSizeMB = _maxRemoteResponseSlider.value;
            _config.maxLocalFileSizeMB = _maxLocalFileSlider.value;
            _config.streamingThresholdMB = _streamingThresholdSlider.value;
            _config.maxNestingDepth = _maxNestingDepthSlider.value;
            _config.maxSchemaNodes = _maxSchemaNodesSlider.value;
            _config.maxEnumValues = _maxEnumValuesSlider.value;
            _config.schemaCacheDurationMinutes = _schemaCacheDurationSlider.value;
            _config.urlCacheDurationMinutes = _urlCacheDurationSlider.value;
            _config.maxSchemaCacheSize = _maxSchemaCacheSizeSlider.value;
            _config.maxUrlCacheSize = _maxUrlCacheSizeSlider.value;
            _config.httpTimeoutSeconds = _httpTimeoutSlider.value;
            _config.maxHttpRetries = _maxHttpRetriesSlider.value;
            _config.defaultParallelismRatio = _parallelismRatioSlider.value;
            _config.urlWhitelist = _urlWhitelistField.value;
            _config.urlBlacklist = _urlBlacklistField.value;
            _config.historyFilePath = _historyFilePathField.value;
            _config.autoSave = _autoSaveToggle.value;

            // Apply to static configuration
            _config.ApplyToStaticConfiguration();

            // Save asset
            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();

            // Save to EditorPrefs as fallback
            _config.SaveToEditorPrefs();

            _isDirty = false;
            UpdateStatus("Settings saved successfully!");
        }

        private void ResetToDefaults()
        {
            if (!EditorUtility.DisplayDialog("Reset Settings", 
                "Are you sure you want to reset all settings to defaults?", 
                "Reset", "Cancel"))
            {
                return;
            }

            _config.ResetToDefaults();
            BindValues();
            OnValueChanged();
            UpdateStatus("Settings reset to defaults");
        }

        private void UpdateStatus(string message)
        {
            _statusLabel.text = message;
        }

        // UI Helper Methods
        private Label CreateSectionHeader(string text)
        {
            var label = new Label(text) { name = $"header-{text.ToLower().Replace(" ", "-")}" };
            label.AddToClassList("rsv-section-header");
            return label;
        }

        private SliderInt CreateSliderInt(string label, int min, int max, string unit)
        {
            var slider = new SliderInt(label, min, max) 
            { 
                showInputField = true,
                name = $"slider-{label.ToLower().Replace(" ", "-")}"
            };
            slider.AddToClassList("rsv-slider");
            return slider;
        }

        private VisualElement CreateSliderRow(SliderInt slider, string tooltip)
        {
            var row = new VisualElement { name = $"row-{slider.name}" };
            row.AddToClassList("rsv-preference-row");
            row.tooltip = tooltip;
            row.Add(slider);
            return row;
        }

        private VisualElement CreateSliderRow(Slider slider, string tooltip)
        {
            var row = new VisualElement { name = $"row-{slider.name}" };
            row.AddToClassList("rsv-preference-row");
            row.tooltip = tooltip;
            row.Add(slider);
            return row;
        }

        private VisualElement CreateFieldRow(TextField field, string tooltip)
        {
            var row = new VisualElement { name = $"row-{field.name}" };
            row.AddToClassList("rsv-preference-row");
            row.tooltip = tooltip;
            row.Add(field);
            return row;
        }
    }

    /// <summary>
    /// Settings provider for Unity Preferences window integration.
    /// </summary>
    public class RSV_SettingsProvider : SettingsProvider
    {
        private RSV_PreferencesWindow _preferencesWindow;

        public RSV_SettingsProvider(string path, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            _preferencesWindow = ScriptableObject.CreateInstance<RSV_PreferencesWindow>();
            _preferencesWindow.CreateGUI();
        }

        public override void OnGUI(string searchContext)
        {
            // Unity IMGUI fallback - not used with UI Toolkit
        }

        [SettingsProvider]
        public static SettingsProvider CreateRSVSettingsProvider()
        {
            var provider = new RSV_SettingsProvider("Preferences/ForgeWarden/RSV", SettingsScope.User)
            {
                keywords = new[] { "RSV", "Schema", "Validation", "JSON", "Migration" }
            };
            return provider;
        }
    }
}
