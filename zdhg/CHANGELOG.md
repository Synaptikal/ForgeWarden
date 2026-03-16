# Changelog

## [1.0.1] - 2026-03-15

### Fixed
- **Test assembly configuration** — ZDHG job tests were located in `zdhg/Editor/Tests/` causing them to compile under the main Editor assembly without access to the NUnit test runner. Moved to `zdhg/Tests/Editor/` with a dedicated asmdef referencing `Unity.Burst`, `Unity.Mathematics`, and `nunit.framework`.
- **NativeArray disposal in tests** — job test arrays are now wrapped in `try/finally` blocks. Previously, a failing `Assert` could throw before `Dispose()` was reached, leaking native memory and causing cumulative allocation warnings across the test run.

## [1.0.0] - 2026-03-12
### Added
- Initial release
- ZDHG_Generator with async (Task<HeatmapResult>) and sync APIs
- NavMesh coverage via triangle centroid method with NavMeshCoverageJob (Burst)
- Spawn point collection via Object.FindObjectsByType<Transform>()
- Static object density via ZDHG_ObjectDensityReader
- ZDHG_GridBuilder with WorldToGrid / GridToWorld helpers
- Desert cell detection
- ZDHG_TextureRenderer: bakes heatmap to Texture2D for single-drawcall overlay
- ZDHG_SceneOverlay: Unity 6 [Overlay] integration in Scene View
- ZDHG_MainWindow with Layers, Zones, and Controls panels
- PNG and CSV export
- ZoneDefinition ScriptableObject with polygon and bounds modes
- LGD_EventBus: publishes HeatmapGeneratedEvent on completion
- Performance warning when grid cells exceed 50,000
