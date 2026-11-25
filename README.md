# VOID Overlay Termo

VOID Overlay Termo is an independent Windows desktop overlay for CPU and GPU temperatures, with short history charts and a system-tray menu. Its sibling, [VOID Overlay Metron](../Metron/README.md), is a separate app for utilization, memory, and FPS availability; neither app depends on the other.

## Features

- CPU/GPU temperature readings and rolling history charts via LibreHardwareMonitor.
- Optional CPU/GPU load, clock, RAM, and VRAM readings when exposed by the hardware provider.
- Transparent, topmost overlay with independently configurable background, text readability, colors, font, spacing, and padding.
- Nine coordinate presets, center-relative X/Y offsets, display selection, full-screen/work-area bounds, click-through and lock controls.
- Independent CPU/GPU temperature and graph visibility; unused metric hardware is not sampled.
- Per-user startup integration, settings persistence, and optional sensor/position diagnostics.
- Missing or invalid readings display as `N/A`; values are never fabricated.

## Requirements and limitations

- Windows 10/11 x64. The project targets `net8.0-windows`; a framework-dependent publish requires the .NET 8 Windows Desktop Runtime.
- The application manifest requests administrator elevation for hardware sensor access. Some hardware or drivers may still not expose temperature sensors.
- This source tree has no verified screenshot, installer, or public release link. Hardware, multi-monitor, and DPI behavior must be validated on the target PC.

## Run and build

Install a compatible .NET SDK and run from the repository root:

```powershell
dotnet run --project .\src\VOIDOverlay.Termo\VOIDOverlay.Termo.csproj
.\scripts\build.ps1 -Configuration Release
.\scripts\test.ps1 -Configuration Release
```

The scripts work from any current directory and store build/test artifacts under `artifacts\build\`.

## Publish

```powershell
.\scripts\publish.ps1
```

This creates a framework-dependent Windows x64 application in `artifacts\publish\win-x64\`. It does not create an archive or installer. The executable requests administrator approval.

## Tray, settings, and configuration

The tray menu uses the shared order: app name, **Open Settings...**, **Show/Hide Overlay**, **Lock Position**, **Start with Windows**, **About VOID Overlay Termo**, **Exit**. Settings use the shared General, Position, Metrics, Appearance, and About cards. Settings appearance supports System, Light, and Dark palettes.

Color values use `#RRGGBB`; font size is 9–24 DIPs. X=0 and Y=0 center the overlay on the selected display; positive X/Y move right/down and negative X/Y move left/up. Numeric coordinates display up to two decimal places and synchronize with sliders. Position presets set convenient coordinates without constraining later edits. The display selector falls back to the primary display if a configured monitor is unavailable; the usable bounds can be the full monitor or taskbar-excluding work area.

The overlay background is off by default. Temperature text and its history graph can be enabled independently for CPU and GPU; disabling both for a metric stops its hardware sampling. Settings are stored in `%LOCALAPPDATA%\VOIDOverlay\Termo\settings.json`. Legacy edge-anchored coordinates are migrated to equivalent center-relative offsets, and legacy shared edge-margin settings migrate to independent offsets. Diagnostics are opt-in via `--diagnostics`; they may include hardware identifiers and readings. Review and redact diagnostic data before sharing.

## Troubleshooting

- **Temperature is `N/A`:** check device/driver support and the elevation prompt. Hardware and firmware determine sensor availability.
- **Overlay is hidden:** use its tray visibility action or double-click the tray icon.
- **Settings do not persist:** verify the current user can write to `%LOCALAPPDATA%\VOIDOverlay\Termo`.
- **Position changed after a display update:** verify the selected display and usable-area choice; Termo clamps coordinates to the selected display's usable bounds.

## Development, structure, and licensing

The app project is under `src/VOIDOverlay.Termo/`, tests under `tests/VOIDOverlay.Termo.Tests/`, and scripts under `scripts/`. The workspace-level [product standard](../docs/PRODUCT_STANDARD.md), [settings parity](../docs/SETTINGS_PARITY.md), and [project structure](../docs/PROJECT_STRUCTURE.md) explain shared conventions and app-specific exceptions.

Run `.\scripts\test.ps1 -Configuration Release` for automated tests covering sensor selection, telemetry presentation, positioning, settings migration, and window sizing. These tests do not require specialized sensor hardware.

See [CONTRIBUTING.md](./CONTRIBUTING.md), [DEPENDENCIES.md](./DEPENDENCIES.md), [THIRD-PARTY-NOTICES.md](./THIRD-PARTY-NOTICES.md), and [LICENSE](./LICENSE). Dependency/license review has unresolved follow-ups; see those notices before redistribution.
