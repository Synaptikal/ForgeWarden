# Narrative Layer Manager

**Preview any story beat in your Unity scenes — without entering Play Mode.**

[![Unity Version](https://img.shields.io/badge/Unity-6000.0%2B-blue.svg)](https://unity.com)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.md)

## What is Narrative Layer Manager?

NLM is a professional Unity editor extension that revolutionizes how you build and preview narrative-driven games. Define story states, create timelines, and instantly visualize how your scenes evolve throughout your game's narrative — all without entering Play Mode.

## Key Features

### 🎬 Non-Destructive Scene Preview
- Scrub through narrative beats and see your scene update in real-time
- Scene is never dirtied — fully restored when you close the window
- Perfect for iterating on narrative-driven environments

### 📊 Visual Timeline
- Custom-built timeline scrubber with color-coded beats
- Click to select, Shift+click to compare beats
- Keyboard navigation (arrow keys)

### 🔍 Conflict Detection
Automatically detects:
- **UnreachableFallback**: Rules that block later rules
- **MissingVariable**: References to undefined variables
- **OverlappingRules**: Multiple rules modifying the same property

### 📋 Validation & Reporting
- Comprehensive validation with suggested fixes
- Export reports as Markdown, CSV, or JSON
- Perfect for team reviews and documentation

### 🎯 8 Property Override Types
- SetActive, SwapMaterial, PositionOffset, ScaleMultiplier
- EnableComponent, DisableComponent, SetColor, ReplaceGameObject

### 🔗 Flexible Conditions
- AND/OR logic chains with **standard operator precedence** (AND binds before OR)
  - `A OR B AND C` evaluates as `A OR (B AND C)`, not left-to-right
- 4 data types: Bool, Int, Float, String
- 8 comparison operators including IsTrue/IsFalse

### 📋 Professional Inspector
- Live condition preview against any state
- Copy/paste rules between bindings
- Variable dropdown with auto-complete
- Full undo/redo support

## Quick Start

### 1. Create States
```
Right-click > Create > Narrative Layer Manager > State Definition
```
Add variables like Chapter, QuestComplete, PlayerLevel

### 2. Create a Layer
```
Right-click > Create > Narrative Layer Manager > Layer Definition
```
Add beats and assign your states

### 3. Bind Objects
Add **Narrative Object Binding** component to GameObjects and define rules:
```
Rule 0: Chapter >= 5 → ShowRuins
Rule 1: Chapter >= 2 → ShowDamaged
Rule 2: (no conditions) → ShowIntact
```

### 4. Preview
Open **Window > Narrative Layer Manager**, select your layer, and scrub the timeline!

## Installation

### Package Manager (Git URL)
```
Window > Package Manager > + > Add package from git URL
https://github.com/narrativelayermanager/nlm.git
```

### Asset Store
Download from the Unity Asset Store and import into your project.

## Documentation

- [Getting Started](Documentation~/GettingStarted.md)
- [Best Practices](Documentation~/BestPractices.md)
- [API Reference](Documentation~/APIReference.md)

## Demo Scene

Check out the included **Demo_ThreeAct** sample scene with:
- 8 bound objects
- 5 narrative beats
- Pre-built conflicts to find and fix
- Complete three-act structure

## System Requirements

- Unity 6000.0 or higher
- No external dependencies
- Works with all render pipelines

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Space | Toggle preview |
| ← → | Navigate beats |
| Ctrl+C | Copy selected rule |
| Ctrl+V | Paste rule |
| Delete | Remove selected |
| F | Focus search |

## Support

- 📧 Email: support@narrativelayermanager.com
- 🐛 Issues: [GitHub Issues](https://github.com/narrativelayermanager/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/narrativelayermanager/discussions)

## License

This project is licensed under the MIT License - see [LICENSE.md](LICENSE.md)

## Roadmap

- [ ] Unity Timeline integration
- [ ] Visual Scripting nodes
- [ ] Runtime debugging overlay
- [ ] Multi-scene state persistence
- [ ] AI-assisted rule suggestions

---

**Made with ❤️ for narrative game developers**
