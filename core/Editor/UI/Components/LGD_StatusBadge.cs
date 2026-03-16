using UnityEngine.UIElements;
using LiveGameDev.Core;

namespace LiveGameDev.Core.Editor.UI
{
    /// <summary>
    /// UI Toolkit VisualElement that renders a coloured status badge (✅ ℹ️ ⚠️ ❌ 🔴).
    /// Register via UxmlFactory or create in code: new LGD_StatusBadge(status).
    /// </summary>
    public class LGD_StatusBadge : VisualElement
    {
        public new class UxmlFactory : UxmlFactory<LGD_StatusBadge, UxmlTraits> { }

        private readonly Label _label;

        public LGD_StatusBadge() : this(ValidationStatus.Pass) { }

        public LGD_StatusBadge(ValidationStatus status)
        {
            _label = new Label();
            Add(_label);
            SetStatus(status);
        }

        /// <summary>Update the badge to reflect a new ValidationStatus.</summary>
        public void SetStatus(ValidationStatus status)
        {
            _label.text = status switch
            {
                ValidationStatus.Pass     => "✅ Pass",
                ValidationStatus.Info     => "ℹ️ Info",
                ValidationStatus.Warning  => "⚠️ Warning",
                ValidationStatus.Error    => "❌ Error",
                ValidationStatus.Critical => "🔴 Critical",
                _                         => status.ToString()
            };
            RemoveFromClassList("lgd-pass");
            RemoveFromClassList("lgd-info");
            RemoveFromClassList("lgd-warning");
            RemoveFromClassList("lgd-error");
            RemoveFromClassList("lgd-critical");
            AddToClassList($"lgd-{status.ToString().ToLower()}");
        }
    }
}
