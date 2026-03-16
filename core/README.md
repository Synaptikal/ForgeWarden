# Live Game Dev Suite — Core

Foundational shared package for the Live Game Dev Suite.
Installed automatically as a dependency of RSV, ZDHG, and ESS.

## Contents
- `LGD_BaseDefinition` — abstract base ScriptableObject for all suite definitions
- `LGD_ValidationReport` / `LGD_ValidationEntry` — unified validation result model
- `ValidationStatus` — 5-level severity enum
- `ILgdValidatable` — validation interface
- `LGD_EventBus` — decoupled cross-tool event system
- `LGD_SuiteSettings` — project-level suite configuration
- Shared UI Toolkit components (StatusBadge, ReportPanel, ExportButton, TagFilterBar)

## Requirements
- Unity 6000.0+
