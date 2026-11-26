# Asterforge

Asterforge is an automation-first incremental game where a tiny industrial outpost grows into a stellar-scale production civilization.

The game is designed to be complete when played alone. Optional multiplayer will later connect independent worlds for resource exchange and cooperative projects without gating progression.

## Platform targets

The first-class targets are:

- Windows
- macOS, built as a Universal application for Intel 64-bit and Apple silicon
- Android, built for ARM64 phones and tablets in landscape orientation

The project is pinned to Unity 6000.6.5f1 and uses URP.

## Play the Outpost

1. Open the project in Unity 6000.6.5f1 and press Play. No scene setup is required.
2. Click, tap, or hold Mine Ore, then build an Extractor to automate extraction.
3. Build Smelters and Research Stations. They produce Metal and Research directly without consuming intermediate resources.
4. Research Industrialization for 100 Research, then build Coal Mines, Coal Generators, and Steelworks.
5. Sustain 1 Steel/s to complete the solo vertical slice.

The production dashboard uses runtime UI Toolkit with a responsive card layout, safe-area insets, keyboard-focus styling, and touch-sized controls. It replaces the temporary IMGUI control deck and procedural vista; production scene art remains a separate milestone.

## Production dashboard

Choose Buy 1, Buy 10, Buy 100, or Buy Max. Fixed batches are all-or-nothing; Max buys the largest affordable batch. The quoted total includes the existing price increase and per-unit rounding for every machine in the batch. Nothing is queued or purchased automatically.

Each resource shows its balance and last measured net rate. Machine cards explain missing materials and unlock requirements; purchase time estimates appear only when every missing resource has a positive measured net rate. Estimates are not guarantees, especially when stockpiles are running down.

Steel diagnostics distinguish last-interval output from sustainable output. Metal, Coal, and power limits are derived from the simulation, including Coal burned by generators. A stockpile can temporarily raise observed output without removing a sustainable-production bottleneck. The milestone panel shows the remaining limits and which machines address them.

UI and persistence use `OutpostSession` queries and commands instead of accessing the live world's mutable ledgers or private fields. Existing save filenames, snapshot versions, offline advancement, and production rules remain unchanged.

## Validation

The `Asterforge.Dashboard.Tests` EditMode assembly covers exact batch pricing, odd-price rounding, all-or-nothing spending, Max boundaries, machine-count and resource overflow, locked machines, stale quotes, purchase estimates, steel bottlenecks, stockpile-assisted production, snapshot restoration, and stylesheet imports.

Before releasing, run the Unity EditMode suite and check the dashboard in Play Mode at desktop, narrow-window, and phone-like landscape sizes. Verify that all controls remain reachable by scrolling, holding Mine Ore stops on release, buying a batch updates costs immediately, and restarting preserves an existing save. Runtime rendering and device behavior require Unity validation; source-level checks alone do not establish them.

## Build the Android prototype

Install Android Build Support, the Android SDK and NDK, and OpenJDK for Unity 6000.6.5f1, then choose **Asterforge > Build > Android Development APK** in the Unity editor. The development APK is written to `build/Android/asterforge.apk`.

The Android build targets ARM64 with IL2CPP, Android 8.0/API 26 or newer, and Android 16/API 36. Vulkan is preferred with OpenGL ES 3 as a fallback. The prototype is locked to landscape autorotation and stays inside Android's safe area.

Android currently retains the prototype rendering cap of 1080 pixels on the logical long side and targets 60 FPS. The dashboard now uses responsive UI Toolkit rather than the former pixel-authored IMGUI shell; validate the retained cap and touch behavior on Android devices before release. Warm pause/resume progress is restored through the same `OutpostSession` used by the dashboard.

## First milestone

The first vertical slice is deliberately solo. It ends when the player can sustain steel production and proves the production, power, research, persistence, and offline-progress loops before networking is introduced.
