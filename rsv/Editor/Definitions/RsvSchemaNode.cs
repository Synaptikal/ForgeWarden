using System;
using System.Collections.Generic;
using UnityEngine;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// One node in a DataSchemaDefinition tree.
    /// A node with FieldType == Object or Array may have child nodes.
    /// </summary>
    [Serializable]
    public class RsvSchemaNode
    {
        [Tooltip("JSON key name for this field.")]
        [SerializeField] public string Name;

        [SerializeField] public RsvFieldConstraint Constraint = new();

        [SerializeField] public List<RsvSchemaNode> Children = new();

        /// <summary>True when this node has no children (leaf value node).</summary>
        public bool IsLeaf => Children == null || Children.Count == 0;

        /// <summary>Full dot-notation path built during validation traversal.</summary>
        public string FullPath { get; set; }
    }
}
