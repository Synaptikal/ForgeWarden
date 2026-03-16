# Changelog

## [1.0.1] - 2026-03-15

### Fixed
- **NLM_Applicator.ApplyComponentToggle()** — `GetComponent(name) as Behaviour` returned `null` for `BoxCollider` and all other `Collider`-derived components because `Collider` does not inherit from `Behaviour`. Enable/disable had no effect on any collider component. Now correctly handles both `Behaviour` and `Collider` types.
- **NLM_Evaluator.EvaluateGroup()** — condition chains now evaluate with standard **AND-before-OR precedence** (e.g. `A OR B AND C` correctly evaluates as `A OR (B AND C)`). Previously, conditions were evaluated strictly left-to-right with no precedence, causing `A OR B AND C` to evaluate as `(A OR B) AND C`. This could silently block rules from firing in content where OR conditions preceded AND conditions.

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
