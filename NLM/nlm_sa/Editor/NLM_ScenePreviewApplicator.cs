using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NarrativeLayerManager.Editor
{
    /// <summary>
    /// Non-destructive scene preview — applies a narrative beat state to all
    /// NarrativeObjectBindings without dirtying the scene.
    /// </summary>
    /// <remarks>
    /// <para>Guarantee: EndPreview() always restores the scene, even if called after
    /// a domain reload or window close.</para>
    /// 
    /// <para>[InitializeOnLoad] re-registers the EditorApplication.playModeStateChanged
    /// callback so EndPreview() fires if the user enters Play Mode mid-preview.</para>
    /// </remarks>
    [InitializeOnLoad]
    public static class NLM_ScenePreviewApplicator
    {
        /// <summary>Returns true if a preview is currently active</summary>
        public static bool IsPreviewActive { get; private set; }

        /// <summary>The currently previewed beat, or null if not previewing</summary>
        public static NarrativeBeat CurrentBeat { get; private set; }

        private static readonly List<NarrativeObjectBinding> _bindings = new();

        static NLM_ScenePreviewApplicator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChange;
        }

        #region Public API

        /// <summary>
        /// Begins previewing a narrative beat in the scene.
        /// </summary>
        /// <param name="beat">The beat to preview</param>
        /// <remarks>
        /// Caches current object states before applying.
        /// Safe to call multiple times — ends previous preview first.
        /// </remarks>
        public static void BeginPreview(NarrativeBeat beat)
        {
            if (IsPreviewActive) EndPreview();
            if (beat?.State == null) return;

            CurrentBeat = beat;
            IsPreviewActive = true;
            _bindings.Clear();
            CollectBindings(_bindings);

            foreach (var b in _bindings)
            {
                if (b == null) continue;
                b.CacheState();
                ApplyBinding(b, beat.State);
            }
            Repaint();
        }

        /// <summary>
        /// Transitions preview from current beat to a new beat.
        /// </summary>
        /// <param name="newBeat">The beat to transition to</param>
        /// <remarks>
        /// Restores original state before applying new beat.
        /// If not currently previewing, behaves like BeginPreview.
        /// </remarks>
        public static void TransitionTo(NarrativeBeat newBeat)
        {
            if (!IsPreviewActive) { BeginPreview(newBeat); return; }
            if (newBeat?.State == null) { EndPreview(); return; }

            foreach (var b in _bindings)
                if (b != null) b.RestoreState();

            CurrentBeat = newBeat;
            foreach (var b in _bindings)
                if (b != null)
                {
                    b.CacheState();
                    ApplyBinding(b, newBeat.State);
                }
            Repaint();
        }

        /// <summary>
        /// Ends the current preview and restores all objects.
        /// </summary>
        /// <remarks>
        /// Safe to call even if not currently previewing (no-op).
        /// Always restores cached states before clearing.
        /// </remarks>
        public static void EndPreview()
        {
            if (!IsPreviewActive) return;
            foreach (var b in _bindings)
                if (b != null) b.RestoreState();
            _bindings.Clear();
            CurrentBeat = null;
            IsPreviewActive = false;
            Repaint();
        }

        #endregion

        #region Private Methods

        private static void ApplyBinding(NarrativeObjectBinding binding, NarrativeStateDefinition state)
        {
            var triggered = NLM_Evaluator.ResolveBinding(binding, state);
            foreach (var rule in triggered)
                foreach (var ovr in rule.Overrides)
                    NLM_Applicator.ApplyOverride(binding.gameObject, ovr);
        }

        private static void CollectBindings(List<NarrativeObjectBinding> list)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                foreach (var root in scene.GetRootGameObjects())
                    list.AddRange(root.GetComponentsInChildren<NarrativeObjectBinding>(includeInactive: true));
            }
        }

        private static void OnPlayModeChange(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode && IsPreviewActive)
                EndPreview();
        }

        private static void Repaint()
        {
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }

        #endregion
    }
}
