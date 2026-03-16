# Narrative Layer Manager - API Reference

## Runtime API

### NarrativeObjectBinding

```csharp
// Apply state at runtime
NarrativeObjectBinding binding = GetComponent<NarrativeObjectBinding>();
binding.ApplyState(currentState);
```

### NLM_Evaluator

Pure evaluation engine with no Unity dependencies.

```csharp
// Evaluate a condition group
bool result = NLM_Evaluator.EvaluateGroup(conditionGroup, state);

// Get triggered rules for a binding
var rules = NLM_Evaluator.ResolveBinding(binding, state);
```

### NLM_Applicator

```csharp
// Apply a single override
NLM_Applicator.ApplyOverride(gameObject, propertyOverride);
```

## Editor API

### NLM_ScenePreviewApplicator

```csharp
// Begin previewing a beat
NLM_ScenePreviewApplicator.BeginPreview(beat);

// Transition to a different beat
NLM_ScenePreviewApplicator.TransitionTo(newBeat);

// End preview and restore scene
NLM_ScenePreviewApplicator.EndPreview();

// Check if preview is active
bool isPreviewing = NLM_ScenePreviewApplicator.IsPreviewActive;
```

### NLM_BeatDiff

```csharp
// Compute differences between two beats
var diffs = NLM_BeatDiff.Compute(beatA, beatB);

// Collect all bindings in loaded scenes
var bindings = NLM_BeatDiff.CollectAll();
```

### NLM_ConflictDetector

```csharp
// Analyze layer for conflicts
var conflicts = NLM_ConflictDetector.Analyze(layer, bindings);
```

### NLM_ValidationReport

```csharp
// Create and populate a report
var report = new NLM_ValidationReport("Context");
report.Add(NLM_Status.Error, "Tag", "Message", "AssetPath", "SuggestedFix");

// Export report
string markdown = report.ToMarkdown();
string csv = report.ToCsv();
string json = report.ToJson();
```

## Data Types

### NarrativeVariable

```csharp
var variable = new NarrativeVariable
{
    Name = "Chapter",
    Type = NarrativeVariableType.Int,
    IntValue = 3
};

// Get value as object
object value = variable.GetValue();

// Parse from string
variable.SetFromString("5");

// Clone
var copy = variable.Clone();
```

### NarrativeCondition

```csharp
var condition = new NarrativeCondition
{
    VariableName = "Chapter",
    Operator = ConditionOperator.GreaterOrEqual,
    Value = "3",
    LogicToNext = ConditionLogic.And
};
```

### NarrativePropertyOverride

```csharp
// SetActive override
var override = new NarrativePropertyOverride
{
    Type = OverrideType.SetActive,
    ActiveState = true
};

// SwapMaterial override
var materialOverride = new NarrativePropertyOverride
{
    Type = OverrideType.SwapMaterial,
    MaterialIndex = 0,
    TargetMaterial = myMaterial
};
```

## Events

### State Change Events (Future)

```csharp
// Subscribe to beat changes
NLM_Events.OnBeatChanged += (oldBeat, newBeat) => {
    Debug.Log($"Transitioned from {oldBeat?.BeatName} to {newBeat?.BeatName}");
};
```

## Extension Points

### Custom Override Types

To add custom override types:

1. Extend `OverrideType` enum
2. Add fields to `NarrativePropertyOverride`
3. Handle in `NLM_Applicator.ApplyOverride()`
4. Add UI in `NarrativeObjectBindingEditor`

### Custom Conditions

To add custom condition operators:

1. Extend `ConditionOperator` enum
2. Handle in `NLM_Evaluator.EvaluateSingle()`
3. Add UI in `NarrativeObjectBindingEditor`

## Best Practices

### Null Checking
Always check for null before using:
```csharp
if (state?.GetVariable("Chapter") is NarrativeVariable var)
{
    // Use var
}
```

### Undo Support
When modifying assets in editor:
```csharp
Undo.RecordObject(target, "Description");
// Make changes
EditorUtility.SetDirty(target);
```

### Performance
Cache evaluations when possible:
```csharp
// Good: Cache the result
var triggeredRules = NLM_Evaluator.ResolveBinding(binding, state);
foreach (var rule in triggeredRules) { /* ... */ }

// Avoid: Multiple evaluations
if (NLM_Evaluator.ResolveBinding(binding, state).Count > 0) { /* ... */ }
```
