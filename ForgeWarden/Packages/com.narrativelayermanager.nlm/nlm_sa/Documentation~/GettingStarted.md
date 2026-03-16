# Getting Started with Narrative Layer Manager

## Overview

The Narrative Layer Manager (NLM) is a Unity editor tool that lets you preview your scenes at any story beat, quest flag, or game phase — without entering Play Mode.

## Installation

### Via Package Manager (Git URL)
1. Open Window > Package Manager
2. Click the + button > Add package from git URL
3. Enter: `https://github.com/narrativelayermanager/nlm.git`

### Via Asset Store
1. Download from Unity Asset Store
2. Import into your project

## Quick Start

### 1. Create Your First State

1. In the Project window, right-click
2. Select **Create > Narrative Layer Manager > State Definition**
3. Name it "Tutorial_Start"
4. Add variables:
   - Chapter (Int) = 1
   - Phase (String) = "Tutorial"
   - QuestActive (Bool) = true

### 2. Create More States

Create additional states for different story moments:
- Tutorial_Complete (Chapter=1, Phase="Exploration")
- Chapter2_Start (Chapter=2, Phase="NewQuest")
- Boss_Defeated (Chapter=2, BossDefeated=true)

### 3. Create a Layer

1. Right-click in Project window
2. Select **Create > Narrative Layer Manager > Layer Definition**
3. Name it "MainStory"
4. Add beats and assign your states

### 4. Bind Objects

1. Select a GameObject in your scene
2. Add Component > Narrative Layer Manager > Narrative Object Binding
3. Define rules:
   - Rule 1: Chapter >= 2 → Show upgraded model
   - Rule 2: (no condition) → Show default model

### 5. Preview

1. Open Window > Narrative Layer Manager
2. Select your "MainStory" layer
3. Click through beats on the timeline
4. Watch your scene update in real-time!

## Core Concepts

### States
A **State** is a snapshot of narrative variables at a specific moment. Think of it as a save file that captures the story progress.

### Layers
A **Layer** is a timeline of states (beats) that form a narrative arc. You can have multiple layers for main story, side quests, character arcs, etc.

### Bindings
A **Binding** connects GameObjects to narrative conditions. Rules define when objects should change appearance or behavior.

### Rules
Rules are evaluated top-to-bottom:
- First matching rule wins (unless FallThrough is enabled)
- Empty conditions always match (useful for fallbacks)
- Multiple overrides can apply from one rule

## Next Steps

- Read [Best Practices](BestPractices.md)
- Check out the [Demo Scene](../Samples~/Demo_ThreeAct)
- Review the [API Reference](APIReference.md)
