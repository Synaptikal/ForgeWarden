using System.Collections.Generic;
using UnityEngine.UIElements;
using LiveGameDev.Core;

namespace LiveGameDev.Core.Editor.UI
{
    /// <summary>
    /// Scrollable UI Toolkit panel displaying a filtered list of LGD_ValidationEntry items.
    /// Bind to any LGD_ValidationReport via Populate().
    /// </summary>
    public class LGD_ReportPanel : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<LGD_ReportPanel, UxmlTraits> { }

        private LGD_ValidationReport _report;
        private ValidationStatus? _statusFilter;
        private string _searchText;
        private readonly ScrollView _scrollView;

        public LGD_ReportPanel()
        {
            _scrollView = new ScrollView();
            Add(_scrollView);
            AddToClassList("lgd-report-panel");
        }

        /// <summary>Populate the panel with a new report. Clears previous entries.</summary>
        public void Populate(LGD_ValidationReport report)
        {
            _report = report;
            Refresh();
        }

        /// <summary>Apply a filter. Pass null status to show all.</summary>
        public void SetFilter(ValidationStatus? status, string searchText)
        {
            _statusFilter = status;
            _searchText   = searchText;
            Refresh();
        }

        /// <summary>Clear all entries from the panel.</summary>
        public void Clear()
        {
            _report = null;
            _scrollView.Clear();
        }

        private void Refresh()
        {
            _scrollView.Clear();
            if (_report == null) return;

            foreach (var entry in FilteredEntries())
            {
                var row = BuildRow(entry);
                _scrollView.Add(row);
            }
        }

        private IEnumerable<LGD_ValidationEntry> FilteredEntries()
        {
            foreach (var e in _report.Entries)
            {
                if (_statusFilter.HasValue && e.Status != _statusFilter.Value) continue;
                if (!string.IsNullOrEmpty(_searchText) &&
                    !e.Message.Contains(_searchText) &&
                    !(e.Category?.Contains(_searchText) ?? false)) continue;
                yield return e;
            }
        }

        private VisualElement BuildRow(LGD_ValidationEntry entry)
        {
            var row   = new VisualElement();
            row.AddToClassList("lgd-report-row");
            row.AddToClassList($"lgd-{entry.Status.ToString().ToLower()}");
            row.Add(new LGD_StatusBadge(entry.Status));
            row.Add(new Label(entry.Category) { name = "category" });
            row.Add(new Label(entry.Message)  { name = "message" });
            if (!string.IsNullOrEmpty(entry.AssetPath))
                row.Add(new Label(entry.AssetPath) { name = "asset" });
            return row;
        }
    }
}
