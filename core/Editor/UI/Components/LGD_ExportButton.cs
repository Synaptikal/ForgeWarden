using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using LiveGameDev.Core;

namespace LiveGameDev.Core.Editor.UI
{
    /// <summary>
    /// Button group for exporting an LGD_ValidationReport to .md / .csv / .json.
    /// Configure via Configure(report, defaultPath) before display.
    /// </summary>
    public class LGD_ExportButton : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<LGD_ExportButton, UxmlTraits> { }

        private LGD_ValidationReport _report;
        private string _defaultPath;

        public LGD_ExportButton()
        {
            var mdBtn   = new Button(ExportMarkdown) { text = "Export .md"  };
            var csvBtn  = new Button(ExportCsv)      { text = "Export .csv" };
            var jsonBtn = new Button(ExportJson)     { text = "Export .json" };
            Add(mdBtn); Add(csvBtn); Add(jsonBtn);
            AddToClassList("lgd-export-button-group");
        }

        public void Configure(LGD_ValidationReport report, string defaultPath)
        {
            _report      = report;
            _defaultPath = defaultPath;
        }

        private void ExportMarkdown() => Export(_report?.ToMarkdown(), ".md");
        private void ExportCsv()      => Export(_report?.ToCsv(),      ".csv");
        private void ExportJson()     => Export(_report?.ToJson(),      ".json");

        private void Export(string content, string extension)
        {
            if (string.IsNullOrEmpty(content)) return;
            LGD_PathUtility.EnsureDirectoryExists(_defaultPath);
            var filename = LGD_PathUtility.GetTimestampedFileName(
                $"{_report?.ToolId ?? "Report"}_Report", extension);
            var fullPath = Path.Combine(_defaultPath, filename);
            File.WriteAllText(fullPath, content);
            AssetDatabase.Refresh();
            Debug.Log($"[LiveGameDev] Report exported to: {fullPath}");
        }
    }
}
