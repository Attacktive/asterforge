# Asterforge

Asterforge is an automation-first incremental game where a tiny industrial outpost grows into a stellar-scale production civilization.

The game is designed to be complete when played alone. Optional multiplayer will later connect independent worlds for resource exchange and cooperative projects without gating progression.

## Platform targets

The first-class desktop targets are:

- Windows
- macOS, built as a Universal application for Intel 64-bit and Apple silicon

The project is pinned to Unity 6000.6.5f1 and uses URP.

## Play the prototype

The current Outpost prototype auto-bootstraps when entering Play Mode, so no scene setup is required yet.

1. Open the project in Unity 6000.6.5f1.
2. Press the Play button in the editor toolbar.
3. Switch to the Game view if Unity does not focus it automatically.
4. Mine Ore, build production machines, research Industrialization, then work toward Steel.

Industrialization costs 100 Research. The production chain can keep running normally while Research accumulates, so no intermediate stockpile management is required.

The current presentation uses a temporary IMGUI shell so the real economy loop can be played before production UI and 2D art are built.

## First milestone

The first vertical slice is deliberately solo. It ends when the player can sustain steel production and proves the production, power, research, persistence, and offline-progress loops before networking is introduced.
