using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace LiveGameDev.Core.Editor.UI
{
    /// <summary>
    /// Tag-based filter toolbar. Renders toggleable tag chips.
    /// Subscribe to OnTagsChanged to respond to user selection.
    /// </summary>
    public class LGD_TagFilterBar : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<LGD_TagFilterBar, UxmlTraits> { }

        /// <summary>Fires when the set of selected tags changes.</summary>
        public event Action<string[]> OnTagsChanged;

        private readonly HashSet<string> _selectedTags = new();
        private readonly VisualElement _chipContainer;

        public LGD_TagFilterBar()
        {
            _chipContainer = new VisualElement();
            _chipContainer.AddToClassList("lgd-tag-chip-container");
            Add(_chipContainer);
            AddToClassList("lgd-tag-filter-bar");
        }

        /// <summary>Rebuild the tag bar with a new set of available tags.</summary>
        public void SetAvailableTags(string[] tags)
        {
            _chipContainer.Clear();
            _selectedTags.Clear();
            foreach (var tag in tags)
            {
                var chip = new Toggle(tag) { value = false };
                chip.RegisterValueChangedCallback(evt =>
                {
                    if (evt.newValue) _selectedTags.Add(tag);
                    else              _selectedTags.Remove(tag);
                    OnTagsChanged?.Invoke(GetSelectedTags());
                });
                chip.AddToClassList("lgd-tag-chip");
                _chipContainer.Add(chip);
            }
        }

        /// <summary>Returns the currently selected tags.</summary>
        public string[] GetSelectedTags() => _selectedTags.ToArray();
    }
}
