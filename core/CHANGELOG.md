# Changelog

## [1.0.0] - 2026-03-12
### Added
- Initial release
- LGD_BaseDefinition, ValidationStatus, LGD_ValidationReport, LGD_ValidationEntry
- ILgdValidatable interface
- LGD_EventBus with generic Subscribe/Publish/Unsubscribe
- LGD_SuiteSettings ScriptableObject with InitializeOnLoad auto-creation
- LGD_SuiteSettingsProvider for Project Settings integration
- Shared UI components: StatusBadge, ReportPanel, ExportButton, TagFilterBar
- LGD_AssetUtility and LGD_PathUtility helpers
- Event system for cross-tool communication (SchemaValidatedEvent, BindingValidatedEvent, HeatmapGeneratedEvent, SimulationCompleteEvent)
