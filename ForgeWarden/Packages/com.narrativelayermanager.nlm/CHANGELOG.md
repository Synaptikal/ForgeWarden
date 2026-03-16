# Changelog

## [1.0.0] - 2026-03-12
### Added
- NarrativeVariable, NarrativeCondition, NarrativeConditionGroup
- NarrativeStateDefinition ScriptableObject
- NarrativeBeat + NarrativeLayerDefinition ScriptableObject
- NarrativePropertyOverride (8 override types)
- NarrativeObjectBinding MonoBehaviour (first-match rule system, FallThrough support)
- NLM_Evaluator: typed condition evaluation, AND/OR chain, short-circuit
- NLM_BeatDiff: cross-beat object diff
- NLM_ScenePreviewApplicator: non-destructive preview with guaranteed restore
- NLM_ConflictDetector: 4 conflict types
- NLM_MainWindow: timeline scrubber, 3-column layout
- NLM_TimelineTrack: MeshGenerationContext custom draw
- NarrativeObjectBindingEditor: custom Inspector with live condition preview
- NLM_ValidationReport: self-contained (zero Core dependency)
- Demo_ThreeAct sample
- 12 NUnit tests
