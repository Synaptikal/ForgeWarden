# Narrative Layer Manager - Best Practices

## Organizing Your Narrative

### State Naming Conventions
Use descriptive, consistent names:
- `Act1_Intro`
- `Act2_Midpoint_Twist`
- `Act3_Climax`
- `SideQuest_Forest_Complete`

### Variable Naming
- Use PascalCase: `Chapter`, `PlayerLevel`, `QuestComplete`
- Be specific: `MainQuest_Chapter` vs `Chapter`
- Use prefixes for organization: `MQ_Chapter`, `SQ_Forest_Status`

### Layer Organization
Create separate layers for:
- **Main Story**: Primary narrative arc
- **Side Quests**: Optional content
- **Character Arcs**: Companion storylines
- **World State**: Environmental changes

## Rule Design

### Rule Ordering
Order rules from most specific to most general:
```
Rule 0: Chapter >= 5 AND BossDefeated → ShowEpicEnding
Rule 1: Chapter >= 3 → ShowMidGame
Rule 2: Chapter >= 1 → ShowEarlyGame
Rule 3: (no conditions) → ShowDefault
```

### Using FallThrough
Enable FallThrough when you want multiple rules to apply:
```
Rule 0: Chapter >= 2 → EnableParticles [FallThrough=true]
Rule 1: Chapter >= 2 → SwapMaterial [FallThrough=false]
```

### Fallback Rules
Always include a no-condition rule at the bottom as a default state.

## Performance

### Optimization Tips
- Use `HideInNLMList` for decorative objects that don't need tracking
- Keep variable counts reasonable (< 50 per state)
- Use conflict detection to find redundant rules

### Runtime Considerations
- NLM is editor-only for preview
- At runtime, use `NarrativeObjectBinding.ApplyState()`
- Cache state definitions to avoid repeated lookups

## Common Patterns

### Progressive Damage
```csharp
// Building damage based on story progress
Rule 0: Chapter >= 5 → ReplaceGameObject(Ruins)
Rule 1: Chapter >= 3 → SwapMaterial(Damaged)
Rule 2: (no conditions) → ShowDefault
```

### Quest Gating
```csharp
// NPC availability
Rule 0: QuestComplete_Forest → ShowRewardDialogue
Rule 1: QuestActive_Forest → ShowInProgressDialogue
Rule 2: Chapter >= 2 → ShowQuestOffer
Rule 3: (no conditions) → HideNPC
```

### Environmental Storytelling
```csharp
// World changes based on player actions
Rule 0: TreesPlanted >= 10 → EnableComponent(ForestGrowth)
Rule 1: PollutionLevel > 50 → SetColor(SmogColor)
```

## Troubleshooting

### Rules Not Firing
1. Check variable names match exactly (case-sensitive)
2. Verify the state is assigned to the beat
3. Use Live Preview in the Inspector to test

### Conflicts
Run the Conflict Detector regularly:
- **UnreachableFallback**: Move unconditional rules to the bottom
- **MissingVariable**: Define variables in all beats
- **OverlappingRules**: Consolidate or sequence overrides

### Preview Not Working
1. Ensure a layer is selected
2. Check that beats have states assigned
3. Verify objects have NarrativeObjectBinding components

## Advanced Tips

### Multi-Scene Setup
For cross-scene narratives:
1. Create a persistent "NarrativeManager" object
2. Store current state in a ScriptableObject
3. Apply state on scene load

### Version Control
- States and Layers are assets — commit them
- Use descriptive commit messages for narrative changes
- Tag important narrative milestones

### Team Collaboration
- Document your variable naming conventions
- Use DesignerNotes fields to explain beat purposes
- Share layer files for collaborative editing
