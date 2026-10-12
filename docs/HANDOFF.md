# Asterforge Handoff and TODO

Updated 2026-10-12 for PR #10, which activates the power model introduced by PR #4 in the playable Outpost.

## Current state

Asterforge is a playable solo Outpost vertical slice targeting Unity 6000.6.5f1 with URP 17.6.0.

The authoritative economy remains in the pure C# `Asterforge.Game` assembly. Unity-facing code owns presentation, input, and persistence adapters rather than simulation rules.

The playable loop now covers:

1. Mine Ore manually.
2. Commission Extractors.
3. Smelt Ore into Metal.
4. Assemble Metal into Components.
5. Convert Components into Research.
6. Spend 100 Research to unlock Industrialization.
7. Commission Coal Mines, Coal Generators, and Steelworks.
8. Keep Coal available for both electricity generation and Steel production.
9. Work toward sustained Steel output while respecting grid capacity and brownouts.

Industrialization intentionally costs Research only. Do not restore the earlier simultaneous Metal + Components + Research stockpile requirement or the pause-and-bank workaround.

## Power

Power is capacity, not a stored resource.

The initial grid balance is:

- Coal Generator: 150 Metal + 30 Components
- Coal Generator output: 4 Power
- Coal Generator fuel: 1 Coal per generator-second while supplying load
- Steelworks demand: 4 Power

One fueled Coal Generator therefore supports one Steelworks at full electrical throughput.

Insufficient fuel-backed capacity throttles powered production proportionally rather than hard-switching the factory off. Coal is shared between generation and Steel production, and the powered simulation budgets generator fuel and powered recipe inputs together before committing a step.

Fractional powered production is carried between simulation advances so repeated short steps do not permanently erase brownout progress. Large offline intervals use overflow-safe intermediate arithmetic.

The prototype exposes installed capacity, fuel-backed capacity, demand, and grid state. Keep electrical availability distinct from production starvation: a powered recipe that lacks Metal or another process input is not automatically an electrical brownout.

## Facility managers

The prototype includes six fixed anime-style facility managers:

- Rhea Vale — Extraction Foreman — Extractor
- Toma Sato — Furnace Chief — Smelter
- Mara Quinn — Assembly Lead — Assembler
- Dr. Ivo Mercer — Research Director — Research Station
- Nika Drazic — Mining Supervisor — Coal Mine
- Elias Voss — Steelworks Superintendent — Steelworks

Managers are presentation only. They must not gate progression or add bonuses, morale, traits, or upgrade systems unless that direction is intentionally revisited later.

Coal Generators intentionally do not have a manager in the current roster.

The final portrait assets are under `Assets/Resources/Managers/`.

Rhea, Mara, and Nika originally failed to load because their committed binary blobs were bad. The files were replaced and the portraits were confirmed working afterward.

`FacilityManagerPortraitRecovery` was added while diagnosing that asset problem. Treat it as temporary compatibility scaffolding rather than permanent architecture.

## Presentation

`OutpostPrototype` is a self-bootstrapping IMGUI prototype.

It currently provides:

- resource telemetry
- manual Ore extraction
- production cards and accelerating hold-to-repeat purchase controls
- Industrialization progression
- Coal, power, and Steel progression
- power-grid telemetry
- an animated retro-industrial outpost vista
- facility-manager presentation

The machine vista derives its slot count from the machine rosters rather than a hard-coded six-machine assumption.

The prototype recreates a missing `OutpostWorld` after Unity script reloads instead of throwing from `OnGUI`. This is crash hardening, not a promise to preserve unsaved transient Play Mode state across script reloads.

This layer exists to prove gameplay and visual direction quickly. Do not invest heavily in extending the current IMGUI architecture.

The intended production presentation remains a living 2D industrial diorama with a dashboard-style interface. Free-placement factories and conveyor-layout puzzles are outside the product direction.

## Persistence and offline progress

The game has versioned Outpost snapshots containing resource balances, machine counts, Industrialization state, and the save timestamp.

Snapshot v2 adds Coal Generator counts. Snapshot v1 restores with zero Coal Generators.

The temporary Unity persistence adapter:

- loads at startup
- advances elapsed offline time analytically
- autosaves every 10 seconds
- saves on application pause
- saves on application quit

Offline progress has no arbitrary time cap.

Manual playtesting has visibly confirmed offline progress restoration, including both short and long elapsed intervals.

## Verification

GitHub Actions runs the Unity 6000.6.5f1 EditMode suite for pull requests and `main`. The workflow prepares sufficient runner disk space for the Unity editor image and uses repository Unity Personal activation secrets.

Dependabot tracks GitHub Actions dependencies.

Power coverage includes:

- full generation
- zero generation
- proportional brownouts
- generator fuel starvation
- shared Coal between generation and powered inputs
- batch invariance for shared fuel/input pressure
- fractional brownout carry between advances
- large representable offline intervals
- invalid/default generator fuel configuration
- v1 snapshot migration
- world-level power integration
- installed versus fuel-backed capacity

Manual Unity verification should still cover presentation behavior that EditMode tests cannot prove, especially hold controls, layout, persistence interactions, and the industrialized power UI.

## Known prototype debt

### Production truth

The UI still describes the vertical-slice objective as sustaining at least 1 Steel/s, but completion is largely inferred from Steelworks ownership.

The next major step is to calculate actual production and consumption rates so the objective can use real sustained Steel throughput, including input starvation and power limits.

### Presentation architecture

`OutpostPrototype` and the manager overlay are prototype code and should eventually be replaced by proper scene/prefab/UI Toolkit presentation.

The manager overlay currently reaches the prototype world through reflection. Do not preserve that coupling when moving to production UI architecture.

The manager portraits currently use `Resources.Load`. This is acceptable for the prototype but should be reconsidered with the production asset-loading approach.

### Portrait recovery

`FacilityManagerPortraitRecovery` retries missing manager textures because the original asset failure looked like an import-timing problem.

The real failure was corrupted image blobs. Remove the recovery component after one clean-import smoke test proves it is unnecessary.

### Production simulation

Unpowered recipes are still processed in sequence, so output produced earlier in one advancement interval can be consumed by a downstream recipe in the same interval.

The powered path deliberately removes ordering dependence around Coal generation, generator fuel, and powered recipe inputs. Do not accidentally reintroduce recipe declaration order as a hidden priority system when adding rate telemetry or more dependencies.

### Save format

Snapshots are versioned, but a complete migration/corruption policy is still future work. The prototype needs graceful handling for unsupported or damaged saves before a public release.

## TODO

### P0 — finish power integration verification

1. Keep the Unity EditMode workflow green on the power-integration PR.
2. Play through Industrialization and confirm Coal Generator purchase, grid telemetry, Steelworks power gating, and brownout messaging in Play Mode.
3. Confirm save/load and offline progress with at least one Coal Generator in the world.
4. Perform one clean-import Play Mode smoke test.
5. Confirm all six manager portraits appear after the clean import.
6. Remove `FacilityManagerPortraitRecovery` if the clean-import test remains healthy.

### P1 — make production truth visible

1. Calculate actual per-resource production and consumption rates from authoritative simulation behavior.
2. Distinguish process-input starvation, power limitation, and healthy throughput.
3. Show useful bottlenecks instead of only nominal recipe rates.
4. Make the 1 Steel/s vertical-slice objective measure real sustained throughput.
5. Drive manager status lines from actual facility state such as starved inputs, brownout, standby, and healthy production.
6. Use this telemetry for balancing rather than relying on machine counts alone.

### P2 — replace the disposable presentation shell

Do this after the solo economy, power loop, and production telemetry feel good.

1. Move from self-bootstrapping IMGUI to a real Unity scene and UI Toolkit interface.
2. Keep `Asterforge.Game` free of Unity references.
3. Put manager portraits directly into facility presentation rather than treating the crew manifest as the final layout.
4. Build the outpost as a living 2D industrial diorama whose machinery reflects actual machine counts and activity.
5. Add deliberate animation and audiovisual feedback for purchases, unlocks, shortages, and milestones.
6. Preserve Windows and Universal macOS as first-class targets.

### P3 — persistence hardening

1. Define save migration behavior by snapshot version.
2. Handle corrupted and unsupported saves without crashing.
3. Add an explicit reset/new-world action.
4. Decide whether save slots are useful or unnecessary for the intended personal-scale game.
5. Keep offline advancement analytical and uncapped.

### P4 — balancing and content

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

After power integration is merged and manually smoke-tested, implement authoritative production/consumption-rate telemetry.

The next substantial design checkpoint is the definition of “actual rate” under shared inputs and brownouts so the 1 Steel/s objective reflects what the factory is truly sustaining rather than what its installed machine count could theoretically produce.
