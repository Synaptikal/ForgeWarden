using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Virtualized tree view for displaying large schema hierarchies.
    /// Only renders visible items for improved performance with 100+ nodes.
    /// </summary>
    public class RSV_VirtualizedTreeView : VisualElement
    {
        private ListView _listView;
        private List<TreeNodeItem> _flatNodes;
        private Dictionary<string, TreeNodeItem> _nodeMap;
        private DataSchemaDefinition _schema;
        private RsvSchemaNode _selectedNode;

        // Events
        public event Action<RsvSchemaNode> OnNodeSelected;
        public event Action<RsvSchemaNode> OnNodeExpanded;
        public event Action<RsvSchemaNode> OnNodeCollapsed;

        // Constants
        private const int ITEM_HEIGHT = 24;
        private const int VIRTUALIZATION_THRESHOLD = 50;

        public RSV_VirtualizedTreeView()
        {
            AddToClassList("rsv-virtualized-tree");

            _flatNodes = new List<TreeNodeItem>();
            _nodeMap = new Dictionary<string, TreeNodeItem>();

            // Create the ListView with virtualization
            _listView = new ListView
            {
                makeItem = MakeTreeItem,
                bindItem = BindTreeItem,
                selectionType = SelectionType.Single,
                fixedItemHeight = ITEM_HEIGHT,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight
            };

            _listView.AddToClassList("rsv-tree-listview");
            _listView.RegisterCallback<ChangeEvent<int>>(OnSelectionChanged);

            Add(_listView);
        }

        /// <summary>
        /// Loads a schema into the tree view.
        /// </summary>
        public void LoadSchema(DataSchemaDefinition schema)
        {
            _schema = schema;
            _selectedNode = null;
            RebuildFlatList();
        }

        /// <summary>
        /// Rebuilds the flat list from the schema hierarchy.
        /// </summary>
        private void RebuildFlatList()
        {
            _flatNodes.Clear();
            _nodeMap.Clear();

            if (_schema?.RootNodes == null || _schema.RootNodes.Count == 0)
            {
                _listView.itemsSource = _flatNodes;
                _listView.Rebuild();
                return;
            }

            // Build flat list with depth information
            foreach (var node in _schema.RootNodes)
            {
                AddNodeToFlatList(node, 0, null, true);
            }

            _listView.itemsSource = _flatNodes;
            _listView.Rebuild();
        }

        /// <summary>
        /// Adds a node and its visible children to the flat list.
        /// </summary>
        private void AddNodeToFlatList(RsvSchemaNode node, int depth, TreeNodeItem parent, bool isVisible)
        {
            if (node == null) return;

            var item = new TreeNodeItem
            {
                Node = node,
                Depth = depth,
                Parent = parent,
                IsVisible = isVisible,
                IsExpanded = false,
                HasChildren = node.Children?.Count > 0
            };

            _flatNodes.Add(item);
            item.UniqueId = Guid.NewGuid().ToString();
            _nodeMap[item.UniqueId] = item;

            // Add children if expanded
            if (isVisible && item.IsExpanded && node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    AddNodeToFlatList(child, depth + 1, item, true);
                }
            }
        }

        /// <summary>
        /// Creates a visual element for a tree item.
        /// </summary>
        private VisualElement MakeTreeItem()
        {
            var element = new VisualElement { name = "tree-item" };
            element.AddToClassList("rsv-tree-item");

            // Expand/collapse button
            var expandBtn = new Button { name = "expand-btn", text = "•" };
            expandBtn.AddToClassList("rsv-expand-btn");
            element.Add(expandBtn);

            // Type icon
            var typeIcon = new Label { name = "type-icon" };
            typeIcon.AddToClassList("rsv-type-icon");
            element.Add(typeIcon);

            // Node name
            var nameLabel = new Label { name = "name-label" };
            nameLabel.AddToClassList("rsv-node-name");
            element.Add(nameLabel);

            // Required badge
            var requiredBadge = new Label { name = "required-badge" };
            requiredBadge.AddToClassList("rsv-required-badge");
            element.Add(requiredBadge);

            return element;
        }

        /// <summary>
        /// Binds data to a tree item visual element.
        /// </summary>
        private void BindTreeItem(VisualElement element, int index)
        {
            if (index < 0 || index >= _flatNodes.Count) return;

            var item = _flatNodes[index];
            var node = item.Node;

            // Indent based on depth
            element.style.paddingLeft = item.Depth * 20;

            // Expand/collapse button
            var expandBtn = element.Q<Button>("expand-btn");
            if (item.HasChildren)
            {
                expandBtn.text = item.IsExpanded ? "▼" : "▶";
                expandBtn.style.display = DisplayStyle.Flex;
                if (item.ExpandHandler == null)
                    item.ExpandHandler = () => ToggleNodeExpansion(item);
                expandBtn.clicked -= item.ExpandHandler;
                expandBtn.clicked += item.ExpandHandler;
            }
            else
            {
                expandBtn.text = "•";
                expandBtn.style.display = DisplayStyle.Flex;
                expandBtn.clicked -= null;
                expandBtn.clicked += null;
            }

            // Type icon
            var typeIcon = element.Q<Label>("type-icon");
            typeIcon.text = GetTypeIcon(node.Constraint?.FieldType ?? RsvFieldType.String);

            // Name
            var nameLabel = element.Q<Label>("name-label");
            nameLabel.text = node.Name ?? "unnamed";

            // Required badge
            var requiredBadge = element.Q<Label>("required-badge");
            requiredBadge.text = node.Constraint?.IsRequired == true ? "*" : "";

            // Selection state
            if (_selectedNode == node)
            {
                element.AddToClassList("rsv-tree-item-selected");
            }
            else
            {
                element.RemoveFromClassList("rsv-tree-item-selected");
            }

            // Click handler for selection
            element.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                SelectNode(item);
            });
        }

        /// <summary>
        /// Toggles expansion state of a node.
        /// </summary>
        private void ToggleNodeExpansion(TreeNodeItem item)
        {
            if (item == null || !item.HasChildren) return;

            item.IsExpanded = !item.IsExpanded;

            if (item.IsExpanded)
            {
                OnNodeExpanded?.Invoke(item.Node);
            }
            else
            {
                OnNodeCollapsed?.Invoke(item.Node);
            }

            // Rebuild flat list to show/hide children
            RebuildFlatList();
        }

        /// <summary>
        /// Selects a node.
        /// </summary>
        private void SelectNode(TreeNodeItem item)
        {
            if (item == null) return;

            _selectedNode = item.Node;
            OnNodeSelected?.Invoke(item.Node);

            // Update visual selection
            _listView.Rebuild();
        }

        /// <summary>
        /// Handles selection change from ListView.
        /// </summary>
        private void OnSelectionChanged(ChangeEvent<int> evt)
        {
            if (evt.newValue >= 0 && evt.newValue < _flatNodes.Count)
            {
                SelectNode(_flatNodes[evt.newValue]);
            }
        }

        /// <summary>
        /// Expands a specific node.
        /// </summary>
        public void ExpandNode(RsvSchemaNode node)
        {
            // Find node by searching flat list (hash collision safe)
            var item = _flatNodes.FirstOrDefault(n => n.Node == node);
            if (item != null)
            {
                item.IsExpanded = true;
                RebuildFlatList();
            }
        }

        /// <summary>
        /// Collapses a specific node.
        /// </summary>
        public void CollapseNode(RsvSchemaNode node)
        {
            // Find node by searching flat list (hash collision safe)
            var item = _flatNodes.FirstOrDefault(n => n.Node == node);
            if (item != null)
            {
                item.IsExpanded = false;
                RebuildFlatList();
            }
        }

        /// <summary>
        /// Expands all nodes.
        /// </summary>
        public void ExpandAll()
        {
            foreach (var item in _flatNodes)
            {
                item.IsExpanded = true;
            }
            RebuildFlatList();
        }

        /// <summary>
        /// Collapses all nodes.
        /// </summary>
        public void CollapseAll()
        {
            foreach (var item in _flatNodes)
            {
                item.IsExpanded = false;
            }
            RebuildFlatList();
        }

        /// <summary>
        /// Gets the currently selected node.
        /// </summary>
        public RsvSchemaNode GetSelectedNode()
        {
            return _selectedNode;
        }

        /// <summary>
        /// Refreshes the view without rebuilding the entire list.
        /// Use after minor changes like renaming a node.
        /// </summary>
        public void Refresh()
        {
            _listView.Rebuild();
        }

        /// <summary>
        /// Gets the icon for a field type.
        /// </summary>
        private string GetTypeIcon(RsvFieldType type) => type switch
        {
            RsvFieldType.String => "📝",
            RsvFieldType.Integer => "🔢",
            RsvFieldType.Number => "🔢",
            RsvFieldType.Boolean => "✓",
            RsvFieldType.Object => "📦",
            RsvFieldType.Array => "📋",
            _ => "?"
        };

        /// <summary>
        /// Represents an item in the flat tree list.
        /// </summary>
        private class TreeNodeItem
        {
            public RsvSchemaNode Node { get; set; }
            public int Depth { get; set; }
            public TreeNodeItem Parent { get; set; }
            public bool IsVisible { get; set; }
            public bool IsExpanded { get; set; }
            public bool HasChildren { get; set; }
            public Action ExpandHandler { get; set; }
            public string UniqueId { get; set; }
        }
    }

    /// <summary>
    /// Extension methods for tree view operations.
    /// </summary>
    public static class VirtualizedTreeViewExtensions
    {
        /// <summary>
        /// Finds a node by name in the schema.
        /// </summary>
        public static RsvSchemaNode FindNodeByName(this DataSchemaDefinition schema, string name)
        {
            if (schema?.RootNodes == null) return null;

            foreach (var node in schema.RootNodes)
            {
                var found = FindNodeRecursive(node, name);
                if (found != null) return found;
            }

            return null;
        }

        private static RsvSchemaNode FindNodeRecursive(RsvSchemaNode node, string name)
        {
            if (node.Name == name) return node;

            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    var found = FindNodeRecursive(child, name);
                    if (found != null) return found;
                }
            }

            return null;
        }
    }
}
