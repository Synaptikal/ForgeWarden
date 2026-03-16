# Live Game Dev Suite — Zone Density Heatmap Generator (ZDHG)

Generates visual density maps from static world data without requiring playtesting.

## Features
- Spawn point density (tag-filtered)
- NavMesh coverage (triangle centroid method, Burst-accelerated)
- Static object density per layer
- Desert cell detection with configurable threshold
- Unity 6 Scene View Overlay (single draw call — procedural texture)
- Export heatmap as PNG + CSV
- Zone boundary display with per-zone density validation

## Requirements
- Unity 6000.0+
- com.[publisher].livegamedev.core 1.0.0
- com.unity.ai.navigation 2.0.0
- com.unity.burst 1.8.19
- com.unity.collections 2.5.1
- com.unity.editorcoroutines 1.0.0

## Performance
- 10km² scene, 10m grid (~1,000,000 cells): < 3 seconds via Job System + Burst
- NavMesh managed arrays are copied to NativeArray once per generation (Allocator.TempJob)

## Quick Start
1. Tag spawn points: EnemySpawn / ResourceSpawn / PvPSpawn
2. Bake a NavMesh on your scene
3. Open: Window > Live Game Dev > Zone Density Heatmap
4. Configure layers and cell size
5. Click Generate
