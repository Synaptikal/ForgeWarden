using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace LiveGameDev.RSV.Editor
{
    /// <summary>
    /// Utility class for debouncing UI field changes in the Schema Designer.
    /// Prevents excessive updates during rapid user input.
    /// </summary>
    public static class RSV_DebouncedField
    {
        private static readonly Dictionary<string, float> s_pendingUpdates = new();
        private static readonly Dictionary<string, Action> s_pendingActions = new();

        /// <summary>
        /// Debounces a field change callback. The callback will only execute after the specified delay
        /// with no additional changes.
        /// </summary>
        /// <param name="fieldId">Unique identifier for the field</param>
        /// <param name="delayMs">Delay in milliseconds</param>
        /// <param name="action">Action to execute</param>
        public static void Debounce(string fieldId, int delayMs, Action action)
        {
            // Cancel any pending update for this field
            if (s_pendingUpdates.ContainsKey(fieldId))
            {
                s_pendingUpdates[fieldId] = Time.realtimeSinceStartup + (delayMs / 1000f);
                s_pendingActions[fieldId] = action;
            }
            else
            {
                s_pendingUpdates[fieldId] = Time.realtimeSinceStartup + (delayMs / 1000f);
                s_pendingActions[fieldId] = action;
            }
        }

        /// <summary>
        /// Updates debounced fields. Call this from EditorApplication.update.
        /// </summary>
        public static void Update()
        {
            var now = Time.realtimeSinceStartup;
            var toRemove = new List<string>();

            // Prevent memory leak: clear all if too many pending entries
            if (s_pendingUpdates.Count > 100)
            {
                Debug.LogWarning("[RSV] DebouncedField: Too many pending updates, clearing all.");
                s_pendingUpdates.Clear();
                s_pendingActions.Clear();
                return;
            }

            foreach (var kvp in s_pendingUpdates)
            {
                if (now >= kvp.Value)
                {
                    toRemove.Add(kvp.Key);
                }
            }

            foreach (var key in toRemove)
            {
                s_pendingUpdates.Remove(key);
                if (s_pendingActions.TryGetValue(key, out var action))
                {
                    s_pendingActions.Remove(key);
                    try
                    {
                        action?.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[RSV] Error in debounced action: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Cancels a pending debounced action.
        /// </summary>
        /// <param name="fieldId">The field identifier</param>
        public static void Cancel(string fieldId)
        {
            s_pendingUpdates.Remove(fieldId);
            s_pendingActions.Remove(fieldId);
        }

        /// <summary>
        /// Cancels all pending debounced actions.
        /// </summary>
        public static void CancelAll()
        {
            s_pendingUpdates.Clear();
            s_pendingActions.Clear();
        }
    }

    /// <summary>
    /// Extension methods for debouncing UI Toolkit fields.
    /// </summary>
    public static class DebouncedFieldExtensions
    {
        /// <summary>
        /// Registers a debounced value changed callback on a TextField.
        /// </summary>
        /// <param name="field">The text field</param>
        /// <param name="delayMs">Delay in milliseconds</param>
        /// <param name="callback">Callback to invoke</param>
        /// <returns>The event callback registration</returns>
        public static EventCallback<ChangeEvent<string>> RegisterDebouncedValueChangedCallback(
            this TextField field, 
            int delayMs, 
            Action<string> callback)
        {
            var fieldId = $"textfield_{field.GetHashCode()}_{Guid.NewGuid()}";
            
            EventCallback<ChangeEvent<string>> handler = evt =>
            {
                RSV_DebouncedField.Debounce(fieldId, delayMs, () => callback?.Invoke(evt.newValue));
            };
            
            field.RegisterValueChangedCallback(handler);
            return handler;
        }

        /// <summary>
        /// Registers a debounced value changed callback on a DoubleField.
        /// </summary>
        /// <param name="field">The double field</param>
        /// <param name="delayMs">Delay in milliseconds</param>
        /// <param name="callback">Callback to invoke</param>
        /// <returns>The event callback registration</returns>
        public static EventCallback<ChangeEvent<double>> RegisterDebouncedValueChangedCallback(
            this DoubleField field, 
            int delayMs, 
            Action<double> callback)
        {
            var fieldId = $"doublefield_{field.GetHashCode()}_{Guid.NewGuid()}";
            
            EventCallback<ChangeEvent<double>> handler = evt =>
            {
                RSV_DebouncedField.Debounce(fieldId, delayMs, () => callback?.Invoke(evt.newValue));
            };
            
            field.RegisterValueChangedCallback(handler);
            return handler;
        }

        /// <summary>
        /// Registers a debounced value changed callback on an EnumField.
        /// </summary>
        /// <param name="field">The enum field</param>
        /// <param name="delayMs">Delay in milliseconds</param>
        /// <param name="callback">Callback to invoke</param>
        /// <returns>The event callback registration</returns>
        public static EventCallback<ChangeEvent<Enum>> RegisterDebouncedValueChangedCallback(
            this EnumField field, 
            int delayMs, 
            Action<Enum> callback)
        {
            var fieldId = $"enumfield_{field.GetHashCode()}_{Guid.NewGuid()}";
            
            EventCallback<ChangeEvent<Enum>> handler = evt =>
            {
                RSV_DebouncedField.Debounce(fieldId, delayMs, () => callback?.Invoke(evt.newValue));
            };
            
            field.RegisterValueChangedCallback(handler);
            return handler;
        }

        /// <summary>
        /// Registers a debounced value changed callback on a Toggle.
        /// </summary>
        /// <param name="toggle">The toggle</param>
        /// <param name="delayMs">Delay in milliseconds</param>
        /// <param name="callback">Callback to invoke</param>
        /// <returns>The event callback registration</returns>
        public static EventCallback<ChangeEvent<bool>> RegisterDebouncedValueChangedCallback(
            this Toggle toggle, 
            int delayMs, 
            Action<bool> callback)
        {
            var fieldId = $"toggle_{toggle.GetHashCode()}_{Guid.NewGuid()}";
            
            EventCallback<ChangeEvent<bool>> handler = evt =>
            {
                RSV_DebouncedField.Debounce(fieldId, delayMs, () => callback?.Invoke(evt.newValue));
            };
            
            toggle.RegisterValueChangedCallback(handler);
            return handler;
        }
    }
}
