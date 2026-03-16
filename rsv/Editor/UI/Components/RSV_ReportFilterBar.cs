using System;
using System.Linq;
using LiveGameDev.Core;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Filter bar for validation reports.
    /// Allows filtering by status, schema, and text search.
    /// </summary>
    public class RSV_ReportFilterBar : VisualElement
    {
        private readonly EnumField _statusFilter;
        private readonly TextField _searchField;
        private readonly Button _clearBtn;

        public event Action OnFilterChanged;

        public ValidationStatus? StatusFilter
        {
            get => _statusFilter.value as ValidationStatus?;
            set => _statusFilter.value = value;
        }

        public string SearchText
        {
            get => _searchField.value;
            set => _searchField.value = value;
        }

        public RSV_ReportFilterBar()
        {
            AddToClassList("rsv-report-filter-bar");

            var container = new VisualElement { name = "container" };
            container.AddToClassList("rsv-filter-container");

            // Status filter
            var statusRow = new VisualElement { name = "status-row" };
            statusRow.AddToClassList("rsv-row");
            var statusLabel = new Label("Status:");
            _statusFilter = new EnumField("All");
            _statusFilter.RegisterValueChangedCallback(evt => OnFilterChanged?.Invoke());
            statusRow.Add(statusLabel);
            statusRow.Add(_statusFilter);
            container.Add(statusRow);

            // Search field
            var searchRow = new VisualElement { name = "search-row" };
            searchRow.AddToClassList("rsv-row");
            var searchLabel = new Label("Search:");
            _searchField = new TextField();
            _searchField.textEdition.placeholder = "Search in messages...";
            _searchField.RegisterValueChangedCallback(evt => OnFilterChanged?.Invoke());
            searchRow.Add(searchLabel);
            searchRow.Add(_searchField);
            container.Add(searchRow);

            // Clear button
            _clearBtn = new Button(ClearFilters) { text = "Clear Filters" };
            _clearBtn.AddToClassList("rsv-clear-filters-btn");
            container.Add(_clearBtn);

            Add(container);
        }

        private void ClearFilters()
        {
            _statusFilter.value = null;
            _searchField.value = "";
            OnFilterChanged?.Invoke();
        }

        /// <summary>
        /// Applies filters to a report and returns filtered entries.
        /// </summary>
        public System.Collections.Generic.IEnumerable<LGD_ValidationEntry> FilterEntries(LGD_ValidationReport report)
        {
            if (report == null) yield break;

            var status = StatusFilter;
            var search = SearchText?.ToLower();

            foreach (var entry in report.Entries)
            {
                // Status filter
                if (status.HasValue && entry.Status != status.Value)
                    continue;

                // Search filter
                if (!string.IsNullOrEmpty(search))
                {
                    var message = entry.Message?.ToLower() ?? "";
                    var category = entry.Category?.ToLower() ?? "";
                    var assetPath = entry.AssetPath?.ToLower() ?? "";

                    if (!message.Contains(search) &&
                        !category.Contains(search) &&
                        !assetPath.Contains(search))
                        continue;
                }

                yield return entry;
            }
        }
    }
}
