# Asterforge Handoff and TODO

Updated 2026-10-12 after merging PR #3, the first playable Outpost prototype.

## Current state

Asterforge is now playable from a clean Unity project checkout without manual scene setup.

The project targets Unity 6000.6.5f1 with URP 17.6.0 and Unity Test Framework 1.8.0.

The authoritative economy remains in the pure C# `Asterforge.Game` assembly. Unity-facing prototype code owns presentation and persistence adapters rather than simulation rules.

The playable loop currently covers:

1. Mine Ore manually.
2. Commission Extractors.
3. Smelt Ore into Metal.
4. Assemble Metal into Components.
5. Convert Components into Research.
6. Spend 100 Research to unlock Industrialization.
7. Commission Coal Mines and Steelworks.
8. Work toward sustained Steel production.

Industrialization intentionally costs Research only. Do not restore the earlier simultaneous Metal + Components + Research stockpile requirement or the pause-and-bank workaround.

## Facility managers

The prototype includes six fixed anime-style facility managers:

- Rhea Vale — Extraction Foreman — Extractor
- Toma Sato — Furnace Chief — Smelter
- Mara Quinn — Assembly Lead — Assembler
- Dr. Ivo Mercer — Research Director — Research Station
- Nika Drazic — Mining Supervisor — Coal Mine
- Elias Voss — Steelworks Superintendent — Steelworks

Managers appear when their production stage unlocks and currently provide presentation only. They must not gate progression or add bonuses, morale, traits, or upgrade systems unless that direction is intentionally revisited later.

The final portrait assets are under `Assets/Resources/Managers/`.

Rhea, Mara, and Nika originally failed to load because their committed binary blobs were bad. The files were replaced and the user confirmed that the portraits render correctly afterward.

`FacilityManagerPortraitRecovery` was added while diagnosing the asset problem. It should be treated as temporary compatibility scaffolding, not permanent architecture.

## Presentation

`OutpostPrototype` is a self-bootstrapping IMGUI prototype.

It currently provides:

- resource telemetry
- manual Ore extraction
- production cards and machine purchase controls
- Industrialization progression
- Coal and Steel progression
- an animated retro-industrial outpost vista
- facility-manager presentation

This layer exists to prove gameplay and visual direction quickly. Do not invest heavily in extending the current IMGUI architecture.

The intended production presentation remains a living 2D industrial diorama with a dashboard-style interface. Free-placement factories and conveyor-layout puzzles are outside the product direction.

## Persistence and offline progress

The game has versioned Outpost snapshots containing resource balances, machine counts, Industrialization state, and the save timestamp.

The temporary Unity persistence adapter:

- loads at startup
- advances elapsed offline time analytically
- autosaves every 10 seconds
- saves on application pause
- saves on application quit

Offline progress has no arbitrary time cap.

Manual playtesting has visibly confirmed offline progress restoration, including both short and long elapsed intervals.

## Verification status

The EditMode suite currently contains 20 expected tests.

The original economy and production tests were confirmed passing before persistence was added. Four snapshot/offline-progress tests were added afterward.

Before treating the current `main` as a stable milestone, run the full 20-test EditMode suite once on the merged head and record any failure rather than assuming green status from earlier runs.

Manual Unity verification has confirmed:

- the project compiles and enters Play Mode
- the Outpost loop is playable
- Research-based Industrialization is materially easier to understand and use than the discarded stockpile design
- persistence restores offline production
- all six facility-manager portraits render after the final binary replacements

## Known prototype debt

### Presentation architecture

`OutpostPrototype` and the manager overlay are prototype code and should eventually be replaced by proper scene/prefab/UI Toolkit presentation.

The manager overlay currently reaches the prototype world through reflection. Do not preserve that coupling when moving to production UI architecture.

The manager portraits currently use `Resources.Load`. This is acceptable for the prototype but should be reconsidered with the production asset-loading approach.

### Portrait recovery

`FacilityManagerPortraitRecovery` retries missing manager textures because the original asset failure looked like an import-timing problem.

The real failure was corrupted image blobs. Now that the corrected portraits have been confirmed working, remove the recovery component after one clean-import smoke test proves it is unnecessary.

### Vertical-slice objective

The UI describes the goal as sustaining at least 1 Steel/s, but the current prototype largely infers progress from Steelworks ownership rather than measuring actual sustained throughput.

The objective must eventually be based on real production rate, including input starvation and future power throttling.

### Production simulation

The simulator is analytical and intentionally simple. Recipes are processed in sequence, so output produced earlier in one advancement interval can be consumed by a downstream recipe in the same interval.

Keep this behavior in mind when adding power, rate telemetry, or more complex dependencies. Do not accidentally make recipe declaration order into a hidden priority system.

### Save format

Snapshots are versioned, but a real migration/corruption policy is still future work. The prototype needs graceful handling for unsupported or damaged saves before a public release.

## TODO

### P0 — harden the merged playable milestone

1. Run the full 20-test EditMode suite on current `main`.
2. Perform one clean-import Play Mode smoke test.
3. Confirm all six manager portraits appear after a clean import.
4. Confirm save/load and offline progress after restarting Play Mode.
5. Remove `FacilityManagerPortraitRecovery` if the clean-import test remains healthy.
6. Keep PR #3 as the historical boundary for the first playable Outpost prototype.

### P1 — implement power

Power is the next major game system.

Existing design constraints are already decided:

- Power is capacity, not a stored resource.
- Insufficient power must degrade production gracefully rather than hard-switching the entire factory off.
- Coal eventually competes between Steel production and electricity generation.

Recommended first implementation:

1. Add a pure-C# power model under `Asterforge.Game`.
2. Give consuming machine types explicit power demand per unit.
3. Introduce the smallest useful generator roster rather than several overlapping generator types.
4. Start with proportional brownout scaling across powered production instead of manual power priorities.
5. Ensure Coal generation and Steelworks compete for Coal without depending on recipe iteration order.
6. Add deterministic tests for full power, partial brownout, zero power, and Coal competition.
7. Add power capacity, demand, and throughput telemetry to the prototype UI only after the simulation model is solid.

Do not add stored batteries, priority controls, or complex grid topology in the first power pass.

### P2 — make production truth visible

1. Calculate actual per-resource production and consumption rates.
2. Show bottlenecks instead of only nominal recipe rates.
3. Make the 1 Steel/s vertical-slice objective measure real sustained throughput.
4. Drive manager status lines from actual facility state such as starved inputs, brownout, standby, and healthy production.
5. Use this telemetry for balancing rather than relying on machine counts alone.

### P3 — replace the disposable presentation shell

Do this after the solo economy and power loop feel good.

1. Move from self-bootstrapping IMGUI to a real Unity scene and UI Toolkit interface.
2. Keep `Asterforge.Game` free of Unity references.
3. Put manager portraits directly into facility presentation rather than treating the crew manifest as the final layout.
4. Build the outpost as a living 2D industrial diorama whose machinery reflects actual machine counts and activity.
5. Add deliberate animation and audiovisual feedback for purchases, unlocks, shortages, and milestones.
6. Preserve Windows and Universal macOS as first-class targets.

### P4 — persistence hardening

1. Define save migration behavior by snapshot version.
2. Handle corrupted and unsupported saves without crashing.
3. Add an explicit reset/new-world action.
4. Decide whether save slots are useful or unnecessary for the intended personal-scale game.
5. Keep offline advancement analytical and uncapped.

### P5 — balancing and content

1. Tune machine prices and milestone timing from actual play sessions.
2. Keep the first manual phase short; automation should arrive quickly.
3. Add upgrades and a small research tree only after the core ratios are enjoyable.
4. Avoid progression requirements that force the player to fight automatic downstream consumption.
5. Preserve the no-prestige-reset direction; progression should zoom outward instead.

### Later — multiplayer

Do not begin multiplayer until the solo vertical slice is enjoyable and stable.

When it starts, preserve these constraints:

- multiplayer is completely optional
- each computer owns an independent persistent world
- first target is two-player LAN/direct IP
- use Unity Transport directly over UDP
- host is authoritative for the session protocol
- explicit resource transfer comes before automated trade routes
- resource transfers must be idempotent so duplicate UDP packets cannot duplicate resources
- disconnecting must pause remote exchange without damaging either local economy
- no resource, machine, research node, era, or required achievement may depend on multiplayer

## Recommended next session

Start by running the full 20-test suite on current `main` and removing the portrait-recovery scaffold if a clean import still renders all six managers.

Then implement the pure-C# power model before changing the UI again.

The next substantial design checkpoint is the generator roster and exact brownout behavior. Until evidence suggests otherwise, prefer the smallest generator set and proportional throughput scaling because they introduce the intended constraint without turning power into a second factory-management game.
