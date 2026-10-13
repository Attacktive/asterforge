# Asterforge

Asterforge is an automation-first incremental game where a tiny industrial outpost grows into a stellar-scale production civilization.

The game is designed to be complete when played alone. Optional multiplayer will later connect independent worlds for resource exchange and cooperative projects without gating progression.

## Platform targets

The first-class targets are:

- Windows
- macOS, built as a Universal application for Intel 64-bit and Apple silicon
- Android, built for ARM64 phones and tablets in landscape orientation

The project is pinned to Unity 6000.6.5f1 and uses URP.

## Play the prototype

The current Outpost prototype auto-bootstraps when entering Play Mode, so no scene setup is required yet.

1. Open the project in Unity 6000.6.5f1.
2. Press the Play button in the editor toolbar.
3. Switch to the Game view if Unity does not focus it automatically.
4. Mine Ore, build production machines, research Industrialization, then work toward Steel.

Industrialization costs 100 Research. The production chain can keep running normally while Research accumulates, so no intermediate stockpile management is required.

The current presentation uses a temporary IMGUI shell so the real economy loop can be played before production UI and 2D art are built.

## Build the Android prototype

Install Android Build Support, the Android SDK and NDK, and OpenJDK for Unity 6000.6.5f1, then choose **Asterforge > Build > Android Development APK** in the Unity editor. The development APK is written to `build/Android/asterforge.apk`.

The Android build targets ARM64 with IL2CPP, Android 8.0/API 26 or newer, and Android 16/API 36. Vulkan is preferred with OpenGL ES 3 as a fallback. The prototype is locked to landscape autorotation and stays inside Android's safe area.

Because the current IMGUI shell is authored in pixels, Android temporarily caps its logical long side at 1080 pixels and targets 60 FPS. This compatibility shim is prototype-only and should disappear when the production UI replaces IMGUI.

## First milestone

The first vertical slice is deliberately solo. It ends when the player can sustain steel production and proves the production, power, research, persistence, and offline-progress loops before networking is introduced.
