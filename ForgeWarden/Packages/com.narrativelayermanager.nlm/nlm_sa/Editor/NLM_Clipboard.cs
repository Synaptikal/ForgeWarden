using System;
using UnityEditor;
using UnityEngine;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Clipboard system for copying and pasting narrative rules.
    /// </summary>
    /// <remarks>
    /// Uses JSON serialization to store copied rules in EditorPrefs.
    /// Supports copying rules between different NarrativeObjectBindings.
    /// </remarks>
    public static class NLM_Clipboard
    {
        private const string ClipboardKey = "NLM_Clipboard_Rule";

        /// <summary>
        /// Copies a binding rule to the clipboard.
        /// </summary>
        /// <param name="rule">The rule to copy</param>
        public static void CopyRule(NarrativeObjectBinding.BindingRule rule)
        {
            if (rule == null) return;
            var json = JsonUtility.ToJson(rule);
            EditorPrefs.SetString(ClipboardKey, json);
        }

        /// <summary>
        /// Checks if there's a rule in the clipboard.
        /// </summary>
        /// <returns>True if a rule is available to paste</returns>
        public static bool HasRule()
        {
            return !string.IsNullOrEmpty(EditorPrefs.GetString(ClipboardKey, ""));
        }

        /// <summary>
        /// Pastes a rule from the clipboard.
        /// </summary>
        /// <returns>A new BindingRule instance, or null if clipboard is empty</returns>
        public static NarrativeObjectBinding.BindingRule PasteRule()
        {
            var json = EditorPrefs.GetString(ClipboardKey, "");
            if (string.IsNullOrEmpty(json)) return null;

            try
            {
                var rule = JsonUtility.FromJson<NarrativeObjectBinding.BindingRule>(json);
                // Generate new unique name
                rule.RuleName = $"{rule.RuleName} (Copy)";
                return rule;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Clears the clipboard.
        /// </summary>
        public static void Clear()
        {
            EditorPrefs.DeleteKey(ClipboardKey);
        }
    }
}
