using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    public partial class RSV_SchemaDesigner
    {
        // ── Node CRUD & Tree Building ────────────────────────────
        private void AddRootNode()
        {
            if (_target == null) return;

            var newNode = new RsvSchemaNode
            {
                Name = "newField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType  = RsvFieldType.String,
                    IsRequired = true
                }
            };

            _target.RootNodes.Add(newNode);
            EditorUtility.SetDirty(_target);
            RebuildTree();
            UpdatePreview();
            SelectNode(newNode);
        }

        private void RebuildTree()
        {
            _virtualizedTreeView?.LoadSchema(_target);
        }

        /// <summary>
        /// Builds a legacy non-virtualized tree node element.
        /// Kept for fallback rendering; primary path uses RSV_VirtualizedTreeView.
        /// </summary>
        private VisualElement BuildTreeNode(RsvSchemaNode node, int depth)
        {
            var row = new VisualElement { name = "tree-row" };
            row.AddToClassList("rsv-tree-row");
            row.AddToClassList(depth > 0 ? "rsv-tree-child" : "rsv-tree-root");

            var indent = new VisualElement { name = "indent" };
            indent.style.width = depth * 20;
            row.Add(indent);

            var expandBtn = new Button { text = node.Children?.Count > 0 ? "▼" : "•" };
            expandBtn.AddToClassList("rsv-expand-btn");
            expandBtn.style.width = 20;
            row.Add(expandBtn);

            var typeIcon = new Label(GetTypeIcon(node.Constraint.FieldType));
            typeIcon.AddToClassList("rsv-type-icon");
            row.Add(typeIcon);

            var nameLabel = new Label(node.Name);
            nameLabel.AddToClassList("rsv-node-name");
            row.Add(nameLabel);

            var requiredBadge = new Label(node.Constraint.IsRequired ? "*" : "");
            requiredBadge.AddToClassList("rsv-required-badge");
            row.Add(requiredBadge);

            row.RegisterCallback<ClickEvent>(evt => SelectNode(node));

            if (node.Children?.Count > 0)
            {
                var childrenContainer = new VisualElement { name = "children-container" };
                childrenContainer.AddToClassList("rsv-children-container");
                childrenContainer.style.display = DisplayStyle.None;

                expandBtn.clicked += () =>
                {
                    var isExpanded = childrenContainer.style.display == DisplayStyle.Flex;
                    childrenContainer.style.display = isExpanded ? DisplayStyle.None : DisplayStyle.Flex;
                    expandBtn.text = isExpanded ? "▼" : "▶";
                };

                foreach (var child in node.Children)
                    childrenContainer.Add(BuildTreeNode(child, depth + 1));

                var parentContainer = new VisualElement();
                parentContainer.Add(row);
                parentContainer.Add(childrenContainer);
                return parentContainer;
            }

            return row;
        }

        private void SelectNode(RsvSchemaNode node)
        {
            _nodeEditor?.CancelPendingUpdates();

            _selectedNode = node;
            _editorContainer.Clear();

            if (node == null)
            {
                AddEditorPlaceholder();
                return;
            }

            _nodeEditor = new RSV_NodeEditor();
            _nodeEditor.LoadNode(node);
            _nodeEditor.OnChanged += () =>
            {
                EditorUtility.SetDirty(_target);
                IncrementalTreeUpdate(node);
                UpdatePreview();
            };
            _nodeEditor.OnDeleteRequested  += () => DeleteNode(node);
            _nodeEditor.OnAddChildRequested += () => AddChildNode(node);

            _editorContainer.Add(_nodeEditor);
        }

        private void AddChildNode(RsvSchemaNode parentNode)
        {
            if (parentNode.Constraint.FieldType != RsvFieldType.Object &&
                parentNode.Constraint.FieldType != RsvFieldType.Array)
            {
                Debug.LogWarning("[RSV] Only Object and Array types can have children.");
                return;
            }

            if (parentNode.Children == null)
                parentNode.Children = new List<RsvSchemaNode>();

            var newNode = new RsvSchemaNode
            {
                Name = "newChildField",
                Constraint = new RsvFieldConstraint
                {
                    FieldType  = RsvFieldType.String,
                    IsRequired = true
                }
            };

            parentNode.Children.Add(newNode);
            EditorUtility.SetDirty(_target);
            RebuildTree();
            UpdatePreview();
            SelectNode(newNode);
        }

        private void DeleteNode(RsvSchemaNode node)
        {
            if (_target == null) return;

            bool removed = _target.RootNodes.Contains(node)
                ? _target.RootNodes.Remove(node)
                : RemoveFromChildren(_target.RootNodes, node);

            if (!removed) return;

            EditorUtility.SetDirty(_target);
            _selectedNode = null;
            _editorContainer.Clear();
            AddEditorPlaceholder();
            RebuildTree();
            UpdatePreview();
        }

        private bool RemoveFromChildren(List<RsvSchemaNode> nodes, RsvSchemaNode target)
        {
            foreach (var node in nodes)
            {
                if (node.Children?.Contains(target) == true)
                {
                    node.Children.Remove(target);
                    return true;
                }
                if (node.Children != null && RemoveFromChildren(node.Children, target))
                    return true;
            }
            return false;
        }

        private void IncrementalTreeUpdate(RsvSchemaNode changedNode)
        {
            _virtualizedTreeView?.Refresh();
        }

        private void AddEditorPlaceholder()
        {
            var placeholder = new Label("Select a field to edit its properties.");
            placeholder.AddToClassList("rsv-placeholder");
            _editorContainer.Add(placeholder);
        }
    }
}
