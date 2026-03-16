# Demo: Three-Act Story

This sample demonstrates a complete three-act narrative structure using the Narrative Layer Manager.

## Scene Contents

### Objects (8 total)
1. **Hero** - Character with different appearances per act
2. **Village** - Settlement that changes over time
3. **Forest** - Environment that evolves
4. **NPC_QuestGiver** - Quest giver with availability conditions
5. **TreasureChest** - Reward that appears after boss defeat
6. **BossGate** - Barrier that opens in Act 3
7. **Skybox** - Atmospheric changes per act
8. **MusicZone** - Audio changes based on story progress

### Narrative States (5 total)
- **Act1_Introduction** - Chapter=1, Phase="Tutorial"
- **Act2_RisingAction** - Chapter=2, Phase="Exploration", QuestActive=true
- **Act2_Crisis** - Chapter=2, Phase="Crisis", BossDefeated=false
- **Act3_Climax** - Chapter=3, Phase="FinalBattle", BossDefeated=false
- **Act3_Resolution** - Chapter=3, Phase="Resolution", BossDefeated=true

### Layer: MainStory
Timeline with 5 beats showing the complete narrative arc.

## Pre-Built Conflicts

This demo includes intentional conflicts to demonstrate the Conflict Detector:

1. **UnreachableFallback** - The Village has a rule that blocks later rules
2. **MissingVariable** - One binding references a variable not in any state
3. **OverlappingRules** - Two rules on the Hero both try to set the material

Run the Conflict Detector in the NLM window to find and fix these issues!

## How to Use

1. Open the scene: `Samples~/Demo_ThreeAct/Demo_ThreeAct.unity`
2. Open Window > Narrative Layer Manager
3. Select the "MainStory" layer
4. Scrub through the timeline to see the scene evolve
5. Click "Conflicts" to see the pre-built issues
6. Try fixing the conflicts and re-run the detector

## Learning Objectives

- Understanding state definitions
- Creating narrative layers with beats
- Setting up object bindings
- Using the timeline scrubber
- Detecting and resolving conflicts
- Previewing without entering Play Mode

## Key Features Demonstrated

- ✓ Multiple property override types
- ✓ AND/OR condition chains
- ✓ FallThrough rule chaining
- ✓ Beat-to-beat diff visualization
- ✓ Conflict detection
- ✓ Non-destructive preview
- ✓ Validation reporting
