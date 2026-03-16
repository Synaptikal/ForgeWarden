using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Handles keyboard shortcuts for the Narrative Layer Manager window.
    /// </summary>
    /// <remarks>
    /// Shortcuts:
    /// - Space: Toggle preview on/off
    /// - Left/Right Arrows: Navigate between beats
    /// - Ctrl+C: Copy selected rule
    /// - Ctrl+V: Paste rule
    /// - Delete: Remove selected rule
    /// - Ctrl+Z: Undo
    /// - Ctrl+Y: Redo
    /// - F: Focus search field
    /// </remarks>
    public static class NLM_ShortcutHandler
    {
        private static NLM_MainWindow _window;

        /// <summary>
        /// Registers the main window for shortcut handling.
        /// </summary>
        public static void RegisterWindow(NLM_MainWindow window)
        {
            _window = window;
        }

        /// <summary>
        /// Processes a key event for shortcuts.
        /// </summary>
        /// <param name="evt">The key event to process</param>
        /// <returns>True if the event was handled</returns>
        public static bool ProcessKeyEvent(KeyDownEvent evt)
        {
            if (_window == null) return false;

            // Space - Toggle preview
            if (evt.keyCode == KeyCode.Space && !evt.ctrlKey && !evt.shiftKey && !evt.altKey)
            {
                _window.TogglePreview();
                return true;
            }

            // Ctrl+C - Copy
            if (evt.keyCode == KeyCode.C && evt.ctrlKey)
            {
                _window.CopySelected();
                return true;
            }

            // Ctrl+V - Paste
            if (evt.keyCode == KeyCode.V && evt.ctrlKey)
            {
                _window.Paste();
                return true;
            }

            // Delete - Remove selected
            if (evt.keyCode == KeyCode.Delete && !evt.ctrlKey)
            {
                _window.DeleteSelected();
                return true;
            }

            // F - Focus search
            if (evt.keyCode == KeyCode.F && !evt.ctrlKey)
            {
                _window.FocusSearch();
                return true;
            }

            return false;
        }
    }
}
