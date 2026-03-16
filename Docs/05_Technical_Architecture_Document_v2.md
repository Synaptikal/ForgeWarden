# FORGEWARDEN — Technical Architecture Document
**Document ID:** 05_Technical_Architecture_Document  
**Version:** 0.1.0-draft  
**Date:** March 12, 2026  
**Author:** Justin Davis  
**Status:** In Progress

---

## PURPOSE

This document defines the complete technical structure of the Live Game Dev Suite before a single line of implementation code is written. It covers every file, class signature, namespace, assembly definition, package manifest, and dependency relationship across all four packages.

**The PRDs define WHAT to build. This document defines HOW it is structured.**

---

## 1. NAMESPACE CONVENTIONS

All packages share a root namespace `LiveGameDev` to avoid collision with user project code.

| Package | Runtime Namespace | Editor Namespace |
|---|---|---|
| core | `LiveGameDev.Core` | `LiveGameDev.Core.Editor` |
| rsv | `LiveGameDev.RSV` | `LiveGameDev.RSV.Editor` |
| zdhg | `LiveGameDev.ZDHG` | `LiveGameDev.ZDHG.Editor` |
| ess | `LiveGameDev.ESS` | `LiveGameDev.ESS.Editor` |

---

## 2. UNITY EDITOR MENU PATHS (ALL LOCKED)

```
Window/
  Live Game Dev/
    Runtime Schema Validator      ← RSV main window
    Zone Density Heatmap          ← ZDHG main window
    Economy Simulation Sandbox    ← ESS main window

Assets/
  Create/
    Live Game Dev/
      Core/
        Suite Settings            ← LGD_SuiteSettings
      Runtime Schema/
        Data Schema Definition    ← DataSchemaDefinition
        JSON Source Binding       ← JsonSourceBinding
      Zone Density/
        Zone Definition           ← ZoneDefinition
        Zone Weight Texture       ← ZoneWeightTexture
      Economy/
        Item Definition           ← ItemDefinition
        Source Definition         ← SourceDefinition
        Sink Definition           ← SinkDefinition
        Player Profile Definition ← PlayerProfileDefinition

Tools/
  Live Game Dev/
    Validate All Bindings (RSV)
    Generate Heatmap (ZDHG)
    Run Economy Simulation (ESS)

Project Settings/
  Live Game Dev Suite             ← LGD_SuiteSettingsProvider
```

---

## 3. PACKAGE: com.[publisher].livegamedev.core

### 3.1 package.json

```json
{
  "name": "com.[publisher].livegamedev.core",
  "version": "1.0.0",
  "displayName": "Live Game Dev Suite — Core",
  "description": "Shared foundational classes for the Live Game Dev Suite. Installed automatically as a dependency.",
  "unity": "6000.0",
  "unityRelease": "0f1",
  "type": "tool",
  "keywords": ["mmorpg", "live game", "validation", "editor tools"],
  "category": "Editor Extensions",
  "author": {
    "name": "[Publisher Name]",
    "url": "https://[publisher-url]"
  },
  "dependencies": {}
}
```

### 3.2 Folder & File Structure

```
com.[publisher].livegamedev.core/
  package.json
  README.md
  CHANGELOG.md
  LICENSE.md
  Runtime/
    LiveGameDev.Core.Runtime.asmdef
    Definitions/
      LGD_BaseDefinition.cs
      ValidationStatus.cs
      LGD_ValidationEntry.cs
      LGD_ValidationReport.cs
    Interfaces/
      ILgdValidatable.cs
    Events/
      LGD_EventBus.cs
      LGD_EventBase.cs
      Events/
        SchemaValidatedEvent.cs
        BindingValidatedEvent.cs
        SimulationCompleteEvent.cs
        HeatmapGeneratedEvent.cs
  Editor/
    LiveGameDev.Core.Editor.asmdef
    Settings/
      LGD_SuiteSettings.cs
      LGD_SuiteSettingsLoader.cs
      LGD_SuiteSettingsProvider.cs
    UI/
      Components/
        LGD_StatusBadge.cs
        LGD_ReportPanel.cs
        LGD_ExportButton.cs
        LGD_TagFilterBar.cs
      UXML/
        LGD_ReportPanel.uxml
        LGD_TagFilterBar.uxml
      USS/
        LGD_Common.uss
        LGD_ReportPanel.uss
    Utilities/
      LGD_AssetUtility.cs
      LGD_PathUtility.cs
  Tests/
    Runtime/
      LiveGameDev.Core.Tests.asmdef
      ValidationReportTests.cs
    Editor/
      LiveGameDev.Core.EditorTests.asmdef
      SettingsLoaderTests.cs
```

### 3.3 Assembly Definitions

**LiveGameDev.Core.Runtime.asmdef**
```json
{
  "name": "LiveGameDev.Core.Runtime",
  "rootNamespace": "LiveGameDev.Core",
  "references": [],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": true
}
```

**LiveGameDev.Core.Editor.asmdef**
```json
{
  "name": "LiveGameDev.Core.Editor",
  "rootNamespace": "LiveGameDev.Core.Editor",
  "references": ["GUID:LiveGameDev.Core.Runtime"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": false
}
```

### 3.4 Class Signatures — Runtime

```csharp
// ValidationStatus.cs
namespace LiveGameDev.Core {
    public enum ValidationStatus {
        Pass,
        Info,
        Warning,
        Error,
        Critical
    }
}

// LGD_ValidationEntry.cs
namespace LiveGameDev.Core {
    [Serializable]
    public class LGD_ValidationEntry {
        public ValidationStatus Status { get; set; }
        public string Category { get; set; }
        public string Message { get; set; }
        public string AssetPath { get; set; }
        public int Line { get; set; }
        public string SuggestedFix { get; set; }
    }
}

// LGD_ValidationReport.cs
namespace LiveGameDev.Core {
    [Serializable]
    public class LGD_ValidationReport {
        public string ToolId { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string ProjectName { get; private set; }
        public IReadOnlyList<LGD_ValidationEntry> Entries { get; }
        public ValidationStatus OverallStatus { get; }
        public bool HasErrors { get; }
        public bool HasCritical { get; }
        public LGD_ValidationReport(string toolId);
        public void AddEntry(LGD_ValidationEntry entry);
        public string ToMarkdown();
        public string ToCsv();
        public string ToJson();
    }
}

// ILgdValidatable.cs
namespace LiveGameDev.Core {
    public interface ILgdValidatable {
        ValidationStatus Validate(LGD_ValidationReport report);
    }
}

// LGD_BaseDefinition.cs
namespace LiveGameDev.Core {
    public abstract class LGD_BaseDefinition : ScriptableObject, ILgdValidatable {
        [SerializeField] public string DisplayName;
        [SerializeField] public string Guid;
        [SerializeField] public string[] Tags;
        [SerializeField] public string Description;
        public abstract ValidationStatus Validate(LGD_ValidationReport report);
        protected virtual void OnEnable(); // assigns GUID if empty
    }
}

// LGD_EventBase.cs
namespace LiveGameDev.Core {
    public abstract class LGD_EventBase { }
}

// LGD_EventBus.cs
namespace LiveGameDev.Core {
    public static class LGD_EventBus {
        public static void Subscribe<T>(Action<T> handler) where T : LGD_EventBase;
        public static void Unsubscribe<T>(Action<T> handler) where T : LGD_EventBase;
        public static void Publish<T>(T evt) where T : LGD_EventBase;
        public static void Clear<T>() where T : LGD_EventBase;
        public static void ClearAll();
    }
}

// Events/
namespace LiveGameDev.Core.Events {
    public class SchemaValidatedEvent : LGD_EventBase {
        public string SchemaGuid { get; }
        public LGD_ValidationReport Report { get; }
    }
    public class BindingValidatedEvent : LGD_EventBase {
        public string BindingId { get; }
        public LGD_ValidationReport Report { get; }
    }
    public class SimulationCompleteEvent : LGD_EventBase {
        public string SimName { get; }
        public LGD_ValidationReport Report { get; }
    }
    public class HeatmapGeneratedEvent : LGD_EventBase {
        public string ZoneGuid { get; }
        public LGD_ValidationReport Report { get; }
    }
}
```

### 3.5 Class Signatures — Editor

```csharp
// LGD_SuiteSettings.cs
namespace LiveGameDev.Core.Editor {
    [CreateAssetMenu(menuName = "Live Game Dev/Core/Suite Settings")]
    public class LGD_SuiteSettings : ScriptableObject {
        [SerializeField] public bool AutoValidateOnPlay;
        [SerializeField] public bool VerboseLogging;
        [SerializeField] public string DefaultOutputPath;
        [SerializeField] public Gradient HeatmapGradient;
        [SerializeField] public string PublisherNamespace;
        public static LGD_SuiteSettings Instance { get; }
    }
}

// LGD_SuiteSettingsLoader.cs
namespace LiveGameDev.Core.Editor {
    [InitializeOnLoad]
    public static class LGD_SuiteSettingsLoader {
        static LGD_SuiteSettingsLoader();
        public static LGD_SuiteSettings FindOrCreateSettings();
        private static LGD_SuiteSettings CreateDefaultSettings();
    }
}

// ── IMPLEMENTATION NOTE: Settings Path Resilience ────────────────────────
// NEVER use a hardcoded path like:
//   AssetDatabase.LoadAssetAtPath<LGD_SuiteSettings>("Assets/LiveGameDevSuite/Settings/...");
// Users regularly rename and move folders. A hardcoded path silently returns null.
//
// CORRECT pattern — GUID-first, path fallback, create if missing:
//
//   public static LGD_SuiteSettings FindOrCreateSettings() {
//       // 1. Search by type across entire project — path-independent
//       var guids = AssetDatabase.FindAssets("t:LGD_SuiteSettings");
//       if (guids.Length > 0) {
//           var path = AssetDatabase.GUIDToAssetPath(guids[0]);
//           return AssetDatabase.LoadAssetAtPath<LGD_SuiteSettings>(path);
//       }
//       // 2. Not found anywhere — create at default path
//       return CreateDefaultSettings();
//   }
//
//   private static LGD_SuiteSettings CreateDefaultSettings() {
//       const string defaultPath = "Assets/LiveGameDevSuite/Settings/LGD_Settings.asset";
//       Directory.CreateDirectory(Path.GetDirectoryName(defaultPath));
//       var settings = ScriptableObject.CreateInstance<LGD_SuiteSettings>();
//       AssetDatabase.CreateAsset(settings, defaultPath);
//       AssetDatabase.SaveAssets();
//       return settings;
//   }
//
// If guids.Length > 1, log a Warning asking the user to remove the duplicate.
// ─────────────────────────────────────────────────────────────────────────────

// LGD_SuiteSettingsProvider.cs
namespace LiveGameDev.Core.Editor {
    public class LGD_SuiteSettingsProvider : SettingsProvider {
        [SettingsProvider]
        public static SettingsProvider CreateProvider();
        public override void OnGUI(string searchContext); // Renders via UI Toolkit CreateSettingsUI
        public override void OnActivate(string searchContext, VisualElement rootElement);
    }
}

// UI Components
namespace LiveGameDev.Core.Editor.UI {
    public class LGD_StatusBadge : VisualElement {
        public new class UxmlFactory : UxmlFactory<LGD_StatusBadge, UxmlTraits> { }
        public ValidationStatus Status { get; set; }
        public void SetStatus(ValidationStatus status);
    }
    public class LGD_ReportPanel : VisualElement {
        public new class UxmlFactory : UxmlFactory<LGD_ReportPanel, UxmlTraits> { }
        public void Populate(LGD_ValidationReport report);
        public void SetFilter(ValidationStatus? status, string searchText);
        public void Clear();
    }
    public class LGD_ExportButton : VisualElement {
        public new class UxmlFactory : UxmlFactory<LGD_ExportButton, UxmlTraits> { }
        public void Configure(LGD_ValidationReport report, string defaultPath);
    }
    public class LGD_TagFilterBar : VisualElement {
        public new class UxmlFactory : UxmlFactory<LGD_TagFilterBar, UxmlTraits> { }
        public event Action<string[]> OnTagsChanged;
        public void SetAvailableTags(string[] tags);
        public string[] GetSelectedTags();
    }
}

// LGD_AssetUtility.cs
namespace LiveGameDev.Core.Editor {
    public static class LGD_AssetUtility {
        public static T FindOrCreateAsset<T>(string path) where T : ScriptableObject;
        public static T[] FindAllAssetsOfType<T>() where T : ScriptableObject;
        public static string GetRelativePath(string absolutePath);
    }
}

// LGD_PathUtility.cs
namespace LiveGameDev.Core.Editor {
    public static class LGD_PathUtility {
        public static string GetDefaultOutputPath();
        public static string EnsureDirectoryExists(string path);
        public static string GetTimestampedFileName(string baseName, string extension);
    }
}
```

---

## 4. PACKAGE: com.[publisher].livegamedev.rsv

### 4.1 package.json

```json
{
  "name": "com.[publisher].livegamedev.rsv",
  "version": "1.0.0",
  "displayName": "Live Game Dev Suite — Runtime Schema Validator",
  "description": "Validates server-driven JSON data against Unity-side schemas during Editor and Play Mode.",
  "unity": "6000.0",
  "unityRelease": "0f1",
  "type": "tool",
  "keywords": ["mmorpg", "json", "schema", "validation", "live game"],
  "category": "Editor Extensions",
  "author": {
    "name": "[Publisher Name]",
    "url": "https://[publisher-url]"
  },
  "dependencies": {
    "com.[publisher].livegamedev.core": "1.0.0",
    "com.unity.nuget.newtonsoft-json": "3.0.2"
  },
  "samples": [
    {
      "displayName": "Demo: Ability Schema",
      "description": "Example schema, binding, and JSON for an ability system.",
      "path": "Samples~/Demo_AbilitySchema"
    }
  ]
}
```

### 4.2 Folder & File Structure

```
com.[publisher].livegamedev.rsv/
  package.json
  README.md
  CHANGELOG.md
  Runtime/
    LiveGameDev.RSV.Runtime.asmdef
    Attributes/
      RsvSchemaAttribute.cs
  Editor/
    LiveGameDev.RSV.Editor.asmdef
    Definitions/
      DataSchemaDefinition.cs
      RsvSchemaNode.cs
      RsvFieldConstraint.cs
      RsvFieldType.cs
      JsonSourceBinding.cs
      JsonSourceType.cs
    Engine/
      RsvValidator.cs
      RsvJsonParser.cs
      RsvSchemaCompiler.cs
    Windows/
      RSV_MainWindow.cs
      RSV_SchemaBrowser.cs
      RSV_SchemaDesigner.cs
      RSV_BindingInspector.cs
      RSV_PlaygroundTab.cs
    Inspector/
      JsonSourceBindingInspector.cs
      RsvAttributeProcessor.cs
    Hooks/
      RSV_PlayModeHook.cs
      RSV_BuildHook.cs
    UI/
      UXML/
        RSV_MainWindow.uxml
        RSV_SchemaBrowser.uxml
        RSV_SchemaDesigner.uxml
        RSV_BindingInspector.uxml
        RSV_PlaygroundTab.uxml
      USS/
        RSV_Window.uss
  Tests/
    Runtime/
      LiveGameDev.RSV.Tests.asmdef
      RsvAttributeTests.cs
    Editor/
      LiveGameDev.RSV.EditorTests.asmdef
      RsvValidatorTests.cs
      RsvSchemaCompilerTests.cs
      RsvJsonParserTests.cs
  Samples~/
    Demo_AbilitySchema/
      Schemas/
        AbilitySchema.asset
      Bindings/
        AbilityBinding.asset
      JSON/
        abilities.json
      Scripts/
        AbilityLoader.cs
      README.md
```

### 4.3 Class Signatures

```csharp
// RsvFieldType.cs
namespace LiveGameDev.RSV {
    public enum RsvFieldType { String, Integer, Number, Boolean, Object, Array }
}

// JsonSourceType.cs
namespace LiveGameDev.RSV.Editor {
    public enum JsonSourceType { FilePath, StreamingAssets, Resources, Url }
}

// RsvFieldConstraint.cs
namespace LiveGameDev.RSV.Editor {
    [Serializable]
    public class RsvFieldConstraint {
        [SerializeField] public bool IsRequired;
        [SerializeField] public RsvFieldType FieldType;
        [SerializeField] public double Min;
        [SerializeField] public double Max;
        [SerializeField] public bool HasMinMax;
        [SerializeField] public string[] EnumValues;
        [SerializeField] public string RefId;
        [SerializeField] public string Description;
        [SerializeField] public string DefaultValue;
    }
}

// RsvSchemaNode.cs
namespace LiveGameDev.RSV.Editor {
    [Serializable]
    public class RsvSchemaNode {
        [SerializeField] public string Name;
        [SerializeField] public RsvFieldConstraint Constraint;
        [SerializeField] public List<RsvSchemaNode> Children;
        public bool IsLeaf { get; }
    }
}

// DataSchemaDefinition.cs
namespace LiveGameDev.RSV.Editor {
    [CreateAssetMenu(menuName = "Live Game Dev/Runtime Schema/Data Schema Definition")]
    public class DataSchemaDefinition : LGD_BaseDefinition {
        [SerializeField] public string SchemaId;
        [SerializeField] public string Version;
        [SerializeField] public List<RsvSchemaNode> RootNodes;
        public override ValidationStatus Validate(LGD_ValidationReport report);
        public string GenerateExampleJson();
    }
}

// JsonSourceBinding.cs
namespace LiveGameDev.RSV.Editor {
    [CreateAssetMenu(menuName = "Live Game Dev/Runtime Schema/JSON Source Binding")]
    public class JsonSourceBinding : ScriptableObject {
        [SerializeField] public DataSchemaDefinition Schema;
        [SerializeField] public JsonSourceType SourceType;
        [SerializeField] public string SourcePathOrUrl;
        [SerializeField] public bool ValidateOnPlay;
        [SerializeField] public bool ValidateOnBuild;
        [SerializeField] public string BindingId;
        public string ResolveJson();
    }
}

// RsvValidator.cs
namespace LiveGameDev.RSV.Editor {
    public static class RsvValidator {
        public static LGD_ValidationReport Validate(
            DataSchemaDefinition schema, string jsonText);
        public static LGD_ValidationReport ValidateBinding(JsonSourceBinding binding);
        public static LGD_ValidationReport[] ValidateAllBindings();
        public static LGD_ValidationReport ValidateFolder(
            DataSchemaDefinition schema, string folderPath);
    }
}

// RsvJsonParser.cs
namespace LiveGameDev.RSV.Editor {
    internal static class RsvJsonParser {
        internal static JToken Parse(string jsonText, out string parseError);
        internal static bool TryGetValue(JToken token, string path, out JToken value);
    }
}

// RsvSchemaCompiler.cs
namespace LiveGameDev.RSV.Editor {
    internal static class RsvSchemaCompiler {
        internal static CompiledSchema Compile(DataSchemaDefinition definition);
        internal static List<LGD_ValidationEntry> EvaluateNode(
            JToken token, RsvSchemaNode node, string path);
    }
    internal class CompiledSchema {
        internal List<RsvSchemaNode> Nodes { get; }
    }
}

// RsvJsonSchemaInterop.cs — NEW: Standard JSON Schema import/export
namespace LiveGameDev.RSV.Editor {
    public static class RsvJsonSchemaInterop {
        // Import: parse Draft 7 / 2020-12 .json schema file → DataSchemaDefinition asset
        public static DataSchemaDefinition ImportFromFile(string jsonSchemaFilePath);
        public static DataSchemaDefinition ImportFromJson(string jsonSchemaText);
        // Export: DataSchemaDefinition → Draft 7 compliant .json schema string
        public static string ExportToJson(DataSchemaDefinition schema);
        public static void ExportToFile(DataSchemaDefinition schema, string outputPath);
    }
}

// RsvSchemaAttribute.cs
namespace LiveGameDev.RSV {
    [AttributeUsage(AttributeTargets.Field)]
    public class RsvSchemaAttribute : Attribute {
        public string SchemaId { get; }
        public RsvSchemaAttribute(string schemaId);
    }
}

// RSV_MainWindow.cs
namespace LiveGameDev.RSV.Editor {
    public class RSV_MainWindow : EditorWindow {
        [MenuItem("Window/Live Game Dev/Runtime Schema Validator")]
        public static void Open();
        public void CreateGUI();
        private void OnSelectionChange();
    }
}

// RSV_PlayModeHook.cs
namespace LiveGameDev.RSV.Editor {
    [InitializeOnLoad]
    internal static class RSV_PlayModeHook {
        static RSV_PlayModeHook();
        private static void OnPlayModeStateChanged(PlayModeStateChange state);
        private static void RunValidationAndReport();
    }
}

// RSV_BuildHook.cs
namespace LiveGameDev.RSV.Editor {
    internal class RSV_BuildHook : IPreprocessBuildWithReport {
        public int callbackOrder { get; }
        public void OnPreprocessBuild(BuildReport report);
    }
}
```

---

## 5. PACKAGE: com.[publisher].livegamedev.zdhg

### 5.1 package.json

```json
{
  "name": "com.[publisher].livegamedev.zdhg",
  "version": "1.0.0",
  "displayName": "Live Game Dev Suite — Zone Density Heatmap",
  "description": "Generates pre-launch visual density maps from spawn points, NavMesh, and object density.",
  "unity": "6000.0",
  "unityRelease": "0f1",
  "type": "tool",
  "keywords": ["mmorpg", "heatmap", "spawn", "navmesh", "zone", "density"],
  "category": "Editor Extensions",
  "author": {
    "name": "[Publisher Name]",
    "url": "https://[publisher-url]"
  },
  "dependencies": {
    "com.[publisher].livegamedev.core": "1.0.0",
    "com.unity.ai.navigation": "2.0.0",
    "com.unity.burst": "1.8.19",
    "com.unity.collections": "2.5.1",
    "com.unity.editorcoroutines": "1.0.0"
  },
  "samples": [
    {
      "displayName": "Demo: Zone Setup",
      "description": "Sample scene with NavMesh, spawn points, and zone definitions.",
      "path": "Samples~/Demo_ZoneSetup"
    }
  ]
}
```

### 5.2 Folder & File Structure

```
com.[publisher].livegamedev.zdhg/
  package.json
  README.md
  CHANGELOG.md
  Runtime/
    LiveGameDev.ZDHG.Runtime.asmdef
    Definitions/
      ZoneDefinition.cs
  Editor/
    LiveGameDev.ZDHG.Editor.asmdef
    Definitions/
      ZoneWeightTexture.cs
    Engine/
      ZDHG_Generator.cs
      ZDHG_NavMeshReader.cs
      ZDHG_SpawnPointCollector.cs
      ZDHG_ObjectDensityReader.cs
      ZDHG_GridBuilder.cs
      DensityCell.cs
      HeatmapResult.cs
      HeatmapSettings.cs
      LayerDefinition.cs
    Rendering/
      ZDHG_SceneRenderer.cs
      ZDHG_GradientBuilder.cs
    Overlays/
      ZDHG_SceneOverlay.cs
      ZDHG_BrushOverlay.cs
    Windows/
      ZDHG_MainWindow.cs
      ZDHG_LayersPanel.cs
      ZDHG_ZonesPanel.cs
      ZDHG_ControlsPanel.cs
    UI/
      UXML/
        ZDHG_MainWindow.uxml
        ZDHG_LayersPanel.uxml
        ZDHG_ZonesPanel.uxml
        ZDHG_ControlsPanel.uxml
      USS/
        ZDHG_Window.uss
  Tests/
    Editor/
      LiveGameDev.ZDHG.EditorTests.asmdef
      GridBuilderTests.cs
      NavMeshReaderTests.cs
  Samples~/
    Demo_ZoneSetup/
      Scenes/
        DemoZone.unity
      Definitions/
        DemoZone1.asset
        DemoZone2.asset
      README.md
```

### 5.3 Class Signatures

```csharp
// ZoneDefinition.cs (Runtime)
namespace LiveGameDev.ZDHG {
    [CreateAssetMenu(menuName = "Live Game Dev/Zone Density/Zone Definition")]
    public class ZoneDefinition : LGD_BaseDefinition {
        [SerializeField] public string ZoneId;
        [SerializeField] public Bounds ZoneBounds;
        [SerializeField] public Vector3[] CustomPolygon;
        [SerializeField] public bool UseCustomPolygon;
        [SerializeField] public float TargetDensityMin;
        [SerializeField] public float TargetDensityMax;
        [SerializeField] public string[] RelevantTags;
        public override ValidationStatus Validate(LGD_ValidationReport report);
        public bool ContainsPoint(Vector3 worldPoint);
    }
}

// DensityCell.cs
namespace LiveGameDev.ZDHG.Editor {
    [Serializable]
    public class DensityCell {
        public Vector2Int GridPos { get; }
        public Bounds WorldBounds { get; }
        public float DensityScore { get; set; }
        public Dictionary<string, float> LayerContributions { get; }
        public bool IsDesert { get; set; }
        public string ZoneName { get; set; }
    }
}

// HeatmapSettings.cs
namespace LiveGameDev.ZDHG.Editor {
    [Serializable]
    public class HeatmapSettings {
        public float CellSize;
        public float DesertThreshold;
        public List<LayerDefinition> Layers;
        public Gradient ColorGradient;
        public List<ZoneDefinition> Zones;
        public bool ShowGridLines;
        public float OverlayOpacity;
    }
}

// LayerDefinition.cs
namespace LiveGameDev.ZDHG.Editor {
    [Serializable]
    public class LayerDefinition {
        public string LayerName;
        public LayerType Type; // SpawnPoints, NavMesh, StaticObjects, ManualPaint
        public float Weight;
        public string[] FilterTags;
        public bool IsVisible;
    }
}

// HeatmapResult.cs
namespace LiveGameDev.ZDHG.Editor {
    public class HeatmapResult {
        public float CellSize { get; }
        public Bounds SceneBounds { get; }
        public List<DensityCell> Cells { get; }
        public LGD_ValidationReport Report { get; }
        public int DesertCellCount { get; }
        public DensityCell GetCell(Vector2Int gridPos);
        public DensityCell GetCellAtWorldPoint(Vector3 worldPoint);
    }
}

// ZDHG_Generator.cs
namespace LiveGameDev.ZDHG.Editor {
    public static class ZDHG_Generator {
        // PRIMARY async API — use this in all UI-facing calls
        // Returns control to Editor UI during heavy Burst job scheduling;
        // use with EditorCoroutineUtility or async/await + Task.Yield()
        public static Task<HeatmapResult> GenerateHeatmapAsync(
            HeatmapSettings settings,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default);

        // Synchronous fallback — for scripting/batch use only, never call from UI thread
        public static HeatmapResult GenerateHeatmap(HeatmapSettings settings);

        public static void ExportPng(HeatmapResult result, string outputPath);
        public static void ExportCsv(HeatmapResult result, string outputPath);
        // Renders heatmap to single procedural Texture2D for single-drawcall Scene View overlay
        public static Texture2D BakeToTexture(HeatmapResult result, int resolution = 1024);
    }
}

// ── IMPLEMENTATION NOTE: Async ZDHG ───────────────────────────────────────
// ZDHG_MainWindow calls GenerateHeatmapAsync(), not GenerateHeatmap().
// Pattern using EditorCoroutineUtility (com.unity.editorcoroutines):
//
//   private void OnGenerateClicked() {
//       _generateButton.SetEnabled(false);
//       _progressBar.style.display = DisplayStyle.Flex;
//       EditorCoroutineUtility.StartCoroutine(RunGeneration(), this);
//   }
//   private IEnumerator RunGeneration() {
//       var task = ZDHG_Generator.GenerateHeatmapAsync(_settings, _progress);
//       while (!task.IsCompleted) yield return null;   // yields each frame
//       _result = task.Result;
//       _progressBar.style.display = DisplayStyle.None;
//       _generateButton.SetEnabled(true);
//       SceneView.RepaintAll();
//   }
//
// IProgress<float> drives a ProgressBar VisualElement in the window (0.0–1.0).
// Add com.unity.editorcoroutines to ZDHG package.json dependencies.
// ─────────────────────────────────────────────────────────────────────────────

// ZDHG_NavMeshReader.cs
namespace LiveGameDev.ZDHG.Editor {
    internal static class ZDHG_NavMeshReader {
        // Uses NavMesh.CalculateTriangulation() + IJobParallelFor for centroid-per-cell coverage
        // NativeArray<Vector3> for Burst-compatible triangle data
        internal static Dictionary<Vector2Int, float> GetNavMeshCoverage(
            Bounds sceneBounds, float cellSize);
    }
    // Burst-compiled job struct (internal)
    [BurstCompile]
    internal struct NavMeshCoverageJob : IJobParallelFor {
        [ReadOnly] public NativeArray<Vector3> TriangleCentroids;
        [ReadOnly] public NativeArray<float> TriangleAreas;
        public NativeArray<float> CellCoverage;
        public void Execute(int index);
    }
}

// ── IMPLEMENTATION NOTE: NavMesh Managed-to-Native Bridge ──────────────────
// NavMesh.CalculateTriangulation() returns MANAGED arrays:
//   NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
//   Vector3[] tri.vertices  (managed)
//   int[]     tri.indices   (managed)
//
// Burst jobs require NativeArray<T>. You MUST copy managed → native before scheduling:
//
//   var nativeVerts = new NativeArray<Vector3>(tri.vertices, Allocator.TempJob);
//   var nativeIdx   = new NativeArray<int>(tri.indices,    Allocator.TempJob);
//
// This incurs a ONE-TIME memory allocation and copy cost at heatmap generation time only
// (not per-frame). This is acceptable and expected — the allocation is bounded by
// NavMesh complexity, not scene object count. Always Dispose() NativeArrays after
// JobHandle.Complete() to avoid memory leaks:
//
//   handle.Complete();
//   nativeVerts.Dispose();
//   nativeIdx.Dispose();
//
// Allocator.TempJob is correct here (lifetime > 1 frame, < 4 frames).
// Do NOT use Allocator.Temp (1-frame only) or Allocator.Persistent (long-lived only).
// ─────────────────────────────────────────────────────────────────────────────

// ZDHG_SpawnPointCollector.cs
namespace LiveGameDev.ZDHG.Editor {
    internal static class ZDHG_SpawnPointCollector {
        // Uses Object.FindObjectsByType<Transform>(FindObjectsSortMode.None)
        internal static List<(Vector3 Position, string Tag)> CollectSpawnPoints(
            string[] filterTags);
    }
}

// ZDHG_GridBuilder.cs
namespace LiveGameDev.ZDHG.Editor {
    internal static class ZDHG_GridBuilder {
        internal static List<DensityCell> BuildGrid(Bounds sceneBounds, float cellSize);
        internal static Vector2Int WorldToGrid(Vector3 worldPos, Bounds sceneBounds, float cellSize);
        internal static Bounds GridToWorld(Vector2Int gridPos, Bounds sceneBounds, float cellSize);
    }
}

// ZDHG_SceneRenderer.cs
namespace LiveGameDev.ZDHG.Editor {
    internal static class ZDHG_SceneRenderer {
        internal static void DrawHeatmap(HeatmapResult result, float opacity, bool showGrid);
        internal static void DrawZoneBoundaries(List<ZoneDefinition> zones);
        internal static void DrawCellTooltip(DensityCell cell, Vector2 mousePosition);
    }
}

// ZDHG_SceneOverlay.cs
namespace LiveGameDev.ZDHG.Editor {
    [Overlay(typeof(SceneView), "Zone Density Heatmap")]
    public class ZDHG_SceneOverlay : ToolbarOverlay {
        public override VisualElement CreatePanelContent();
        public static bool IsVisible { get; }
        public static HeatmapResult CurrentResult { get; set; }
    }
}

// ZDHG_MainWindow.cs
namespace LiveGameDev.ZDHG.Editor {
    public class ZDHG_MainWindow : EditorWindow {
        [MenuItem("Window/Live Game Dev/Zone Density Heatmap")]
        public static void Open();
        public void CreateGUI();
    }
}
```

---

## 6. PACKAGE: com.[publisher].livegamedev.ess

### 6.1 package.json

```json
{
  "name": "com.[publisher].livegamedev.ess",
  "version": "1.0.0",
  "displayName": "Live Game Dev Suite — Economy Simulation Sandbox",
  "description": "Simulates player-driven economies over time to detect inflation, deflation, and balance failures pre-launch.",
  "unity": "6000.0",
  "unityRelease": "0f1",
  "type": "tool",
  "keywords": ["mmorpg", "economy", "simulation", "inflation", "balance", "live game"],
  "category": "Editor Extensions",
  "author": {
    "name": "[Publisher Name]",
    "url": "https://[publisher-url]"
  },
  "dependencies": {
    "com.[publisher].livegamedev.core": "1.0.0",
    "com.unity.editorcoroutines": "1.0.0"
  },
  "samples": [
    {
      "displayName": "Demo: Economy Baseline",
      "description": "15 items, 8 sources, 6 sinks, 3 player profiles. Find the inflation bug!",
      "path": "Samples~/Demo_EconomyBaseline"
    }
  ]
}
```

### 6.2 Folder & File Structure

```
com.[publisher].livegamedev.ess/
  package.json
  README.md
  CHANGELOG.md
  Runtime/
    LiveGameDev.ESS.Runtime.asmdef
    Definitions/
      ItemDefinition.cs
      SourceDefinition.cs
      SinkDefinition.cs
      PlayerProfileDefinition.cs
      ItemCategory.cs
      SourceType.cs
      SinkType.cs
  Editor/
    LiveGameDev.ESS.Editor.asmdef
    Engine/
      ESS_Simulator.cs
      ESS_PlayerAgent.cs
      ESS_MarketModel.cs
      ESS_AlertEvaluator.cs
      SimConfig.cs
      SimState.cs
      SimSnapshot.cs
      SimulationResult.cs
    Windows/
      ESS_MainWindow.cs
      ESS_LibraryPanel.cs
      ESS_ControlsPanel.cs
      ESS_ResultsPanel.cs
      ESS_WhatIfPanel.cs
    Charts/
      ESS_LineChart.cs
      ESS_HistogramChart.cs
    UI/
      UXML/
        ESS_MainWindow.uxml
        ESS_LibraryPanel.uxml
        ESS_ControlsPanel.uxml
        ESS_ResultsPanel.uxml
      USS/
        ESS_Window.uss
        ESS_Charts.uss
  Tests/
    Runtime/
      LiveGameDev.ESS.Tests.asmdef
      ItemDefinitionTests.cs
    Editor/
      LiveGameDev.ESS.EditorTests.asmdef
      SimulatorTests.cs
      MarketModelTests.cs
      AlertEvaluatorTests.cs
  Samples~/
    Demo_EconomyBaseline/
      Definitions/
        Items/
          IronOre.asset
          GoldCoin.asset
          EpicSword.asset
        Sources/
          MiningSource.asset
          QuestGoldSource.asset
        Sinks/
          SmithingSink.asset
          VendorSink.asset
        Profiles/
          CasualPlayer.asset
          HardcorePlayer.asset
      README.md
```

### 6.3 Class Signatures

```csharp
// ItemCategoryDefinition.cs (Runtime) — replaces hardcoded enum
namespace LiveGameDev.ESS {
    [CreateAssetMenu(menuName = "Live Game Dev/Economy/Item Category Definition")]
    public class ItemCategoryDefinition : LGD_BaseDefinition {
        [SerializeField] public string CategoryName;     // e.g. "Material", "Currency"
        [SerializeField] public Color EditorColor;       // for visual filtering in ESS window
        [SerializeField] public string Description;
        public override ValidationStatus Validate(LGD_ValidationReport report);
    }
    // Suite ships 6 defaults in Samples~/Defaults/Categories/:
    //   Experience.asset, Material.asset, Token.asset,
    //   Currency.asset, Capability.asset, Labor.asset
}

// ItemDefinition.cs (Runtime)
namespace LiveGameDev.ESS {
    [CreateAssetMenu(menuName = "Live Game Dev/Economy/Item Definition")]
    public class ItemDefinition : LGD_BaseDefinition {
        [SerializeField] public ItemCategoryDefinition Category; // fully user-customizable
        [SerializeField] public float BaseValue;
        [SerializeField] public int MaxStack;
        [SerializeField] public float RarityWeight;
        [SerializeField] public float TargetCirculationPerPlayer;
        public override ValidationStatus Validate(LGD_ValidationReport report);
    }
}

// SourceDefinition.cs (Runtime)
namespace LiveGameDev.ESS {
    [CreateAssetMenu(menuName = "Live Game Dev/Economy/Source Definition")]
    public class SourceDefinition : LGD_BaseDefinition {
        [SerializeField] public SourceType Type;
        [SerializeField] public AnimationCurve LevelScalingCurve;
        [SerializeField] public float BaseRatePerHour;
        [SerializeField] public ItemDefinition[] Outputs;
        [SerializeField] public float[] OutputWeights;
        [SerializeField] public string[] ZoneTags;
        public float GetRateAtLevel(float level);
        public override ValidationStatus Validate(LGD_ValidationReport report);
    }
}

// SinkDefinition.cs (Runtime)
namespace LiveGameDev.ESS {
    [CreateAssetMenu(menuName = "Live Game Dev/Economy/Sink Definition")]
    public class SinkDefinition : LGD_BaseDefinition {
        [SerializeField] public SinkType Type;
        [SerializeField] public ItemDefinition[] InputItems;
        [SerializeField] public int[] InputQuantities;
        [SerializeField] public float OutputEfficiency;
        [SerializeField] public float PlayerEngagementRate;
        public override ValidationStatus Validate(LGD_ValidationReport report);
    }
}

// PlayerProfileDefinition.cs (Runtime)
namespace LiveGameDev.ESS {
    [CreateAssetMenu(menuName = "Live Game Dev/Economy/Player Profile Definition")]
    public class PlayerProfileDefinition : LGD_BaseDefinition {
        [SerializeField] public float DailyPlayHours;
        [SerializeField] public float SessionsPerWeek;
        [SerializeField] public float LevelsPerWeek;
        [SerializeField] public float EfficiencyMultiplier;
        [SerializeField] public float AuctionHouseParticipationRate;
        public override ValidationStatus Validate(LGD_ValidationReport report);
    }
}

// SimConfig.cs
namespace LiveGameDev.ESS.Editor {
    [Serializable]
    public class SimConfig {
        public int Seed;
        public int PlayerCount;
        public int SimulationDays;
        public List<(PlayerProfileDefinition Profile, float Percentage)> PlayerMix;
        public List<ItemDefinition> TrackedItems;
        public List<SourceDefinition> Sources;
        public List<SinkDefinition> Sinks;
        public float AuctionHouseInfluence;
    }
}

// SimState.cs
namespace LiveGameDev.ESS.Editor {
    [Serializable]
    public class SimState {
        public int Day { get; set; }
        public Dictionary<string, float> ItemSupply { get; }
        public Dictionary<string, float> ItemPrices { get; }
        public float TotalCurrency { get; set; }
        public float GiniCoefficient { get; set; }
    }
}

// SimSnapshot.cs
namespace LiveGameDev.ESS.Editor {
    [Serializable]
    public class SimSnapshot {
        public string SimulationName { get; }
        public DateTime Timestamp { get; }
        public int Seed { get; }
        public SimConfig Config { get; }
        public SimState FinalState { get; }
        public List<SimState> DailyHistory { get; }
        public string ToJson();
        public static SimSnapshot FromJson(string json);
    }
}

// SimulationResult.cs
namespace LiveGameDev.ESS.Editor {
    public class SimulationResult {
        public SimConfig Config { get; }
        public List<SimState> DailyHistory { get; }
        public SimState FinalState { get; }
        public List<EssAlert> Alerts { get; }
        public LGD_ValidationReport Report { get; }
        public SimSnapshot ToSnapshot(string name);
        public SimSnapshot DiffWith(SimSnapshot other);
    }
}

// EssAlert.cs
namespace LiveGameDev.ESS.Editor {
    public class EssAlert {
        public string ItemName { get; }
        public string AlertType { get; }  // "WeakSink" | "MoneyPrinter" | "Overfarming"
        public ValidationStatus Severity { get; }
        public string Message { get; }
        public int DayTriggered { get; }
    }
}

// ESS_Simulator.cs
namespace LiveGameDev.ESS.Editor {
    public static class ESS_Simulator {
        // PRIMARY async API — use this in all UI-facing calls
        // Each day tick yields control back to Editor via await Task.Yield()
        // allowing smooth progress bar updates without freezing the Editor UI
        public static Task<SimulationResult> RunSimulationAsync(
            SimConfig config,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default);

        // Synchronous fallback — for scripting/batch use only, never call from UI thread
        public static SimulationResult RunSimulation(SimConfig config);

        private static SimState TickDay(SimState current, SimConfig config, System.Random rng);
        private static float CalculateGini(Dictionary<string, float> supply);
    }
}

// ── IMPLEMENTATION NOTE: Async ESS ────────────────────────────────────────
// ESS_ControlsPanel calls RunSimulationAsync() on button press.
// Task.Yield() after each day tick keeps the Editor responsive:
//
//   public static async Task<SimulationResult> RunSimulationAsync(
//       SimConfig config, IProgress<float> progress, CancellationToken ct) {
//       var state = InitialState(config);
//       for (int day = 0; day < config.SimulationDays; day++) {
//           ct.ThrowIfCancellationRequested();
//           state = TickDay(state, config, rng);
//           progress?.Report((float)day / config.SimulationDays);
//           await Task.Yield();   // ← returns control to Editor UI each tick
//       }
//       return BuildResult(config, history, alerts);
//   }
//
// A CancellationTokenSource wired to a [Cancel] button lets designers
// abort long-running sims mid-way. Dispose the CTS after completion.
// ─────────────────────────────────────────────────────────────────────────────

// ESS_AlertEvaluator.cs
namespace LiveGameDev.ESS.Editor {
    internal static class ESS_AlertEvaluator {
        internal static List<EssAlert> EvaluateDailyState(
            SimState state, SimState previousState, SimConfig config);
    }
}

// ESS_MainWindow.cs
namespace LiveGameDev.ESS.Editor {
    public class ESS_MainWindow : EditorWindow {
        [MenuItem("Window/Live Game Dev/Economy Simulation Sandbox")]
        public static void Open();
        public void CreateGUI();
    }
}

// ESS_LineChart.cs
namespace LiveGameDev.ESS.Editor.Charts {
    public class ESS_LineChart : VisualElement {
        public new class UxmlFactory : UxmlFactory<ESS_LineChart, UxmlTraits> { }
        public void SetData(string[] labels, Dictionary<string, float[]> series);
        public void SetHighlight(int dayIndex);
        protected override void ImmediateRepaint();  // Uses MeshGenerationContext
    }
}

// ESS_HistogramChart.cs
namespace LiveGameDev.ESS.Editor.Charts {
    public class ESS_HistogramChart : VisualElement {
        public new class UxmlFactory : UxmlFactory<ESS_HistogramChart, UxmlTraits> { }
        public void SetData(float[] values, int binCount = 20);
        protected override void ImmediateRepaint();
    }
}
```

---

## 7. CROSS-TOOL SCRIPTING DEFINE SYMBOLS

Set by each package's `[InitializeOnLoad]` class on install:

```
LGD_CORE_INSTALLED
LGD_RSV_INSTALLED
LGD_ZDHG_INSTALLED
LGD_ESS_INSTALLED
```

Usage pattern for optional integration:
```csharp
#if LGD_RSV_INSTALLED && LGD_ESS_INSTALLED
    // Wire ESS ItemDefinitions to RSV schema validation
#endif
```

---

## 8. DEPENDENCY GRAPH (FULL)

```
[Unity Engine]
      |
      +──[AI.Navigation 2.0]──[Newtonsoft.Json]──[Burst 1.8+]──[Collections 2.5+]──[EditorCoroutines]
      |                              |                 |                |                    |
      |                              |                 +────────────────+────────────────────+
      |                              |                              (ZDHG only)
      |                              |
      +──────────────────────────────+
                     |
        [livegamedev.core]  (no external deps)
          /           |           \
      [.rsv]       [.zdhg]       [.ess]
        |              |             |
    .core           .core          .core
    Newtonsoft     AI.Nav         (none)
                   Burst
                   Collections
                   EditorCoroutines
```

**Full dependency table:**

| Package | Unity | External Dependencies |
|---|---|---|
| `.core` | 6000.0+ | None |
| `.rsv` | 6000.0+ | `.core`, `com.unity.nuget.newtonsoft-json 3.0.2` |
| `.zdhg` | 6000.0+ | `.core`, `com.unity.ai.navigation 2.0.0`, `com.unity.burst 1.8.19`, `com.unity.collections 2.5.1`, `com.unity.editorcoroutines 1.0.0` |
| `.ess` | 6000.0+ | `.core`, `com.unity.editorcoroutines 1.0.0` |

---

## 9. CRITICAL IMPLEMENTATION NOTES

1. **Editor-only classes** must NEVER be referenced from Runtime assemblies.
2. `DataSchemaDefinition`, `ZoneDefinition`, `ItemDefinition`, `SourceDefinition`, `SinkDefinition`, `PlayerProfileDefinition` must be in **Runtime** assemblies so user project scripts can reference them at runtime if needed.
3. Validation engines (`RsvValidator`, `ZDHG_Generator`, `ESS_Simulator`) are **Editor-only**.
4. All `[CreateAssetMenu]` ScriptableObjects that may be needed at runtime go in Runtime assembly — window/tool classes stay in Editor assembly.
5. All `EditorWindow` subclasses must override `CreateGUI()`, never `OnGUI()`.
6. All `FindObjectsOfType<T>()` calls must use `FindObjectsByType<T>(FindObjectsSortMode.None)`.
7. UXML `UxmlFactory` pattern uses Unity 6 `UxmlElement` attribute preferred over legacy factory class where possible.

---

*Next: Implementation begins with com.[publisher].livegamedev.core*
