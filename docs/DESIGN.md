# Asterforge Design

## Product statement

Asterforge is an automation-first incremental game where a tiny industrial outpost grows into a stellar-scale production civilization.

The game must remain complete and satisfying when played entirely alone. Optional multiplayer connects independent worlds so they can exchange surplus resources and cooperate on large goals without creating multiplayer-only progression.

## Core loop

1. Acquire resources.
2. Process them into higher-value materials.
3. Manufacture machines and components.
4. Research improvements.
5. Automate repetitive management.
6. Consolidate mature infrastructure into larger abstractions.
7. Expand into a larger industrial scale.

There is no traditional prestige reset. Progression zooms outward instead of repeatedly deleting the player's economy.

## Eras

1. Outpost: learn production chains.
2. Industry: balance throughput and power.
3. Automation: make the economy manage itself.
4. Precision: handle complex manufacturing and soft specialization.
5. Energy: satisfy enormous power demand.
6. Orbital: coordinate geographically separate industry.
7. Planetary: manage aggregated industrial regions.
8. Stellar: construct megastructures and operate at stellar scale.

Only Outpost and Industry belong to the first implementation phase.

## First vertical slice

The first slice ends when the player sustains at least 1 Steel per second.

Resources:

- Ore
- Metal
- Components
- Research
- Coal
- Steel

Initial production chain:

`Ore -> Metal -> Components -> Research`

Industrialization is a Research breakthrough rather than a simultaneous warehouse requirement. The prototype cost is 100 Research. Reaching the milestone therefore rewards a functioning automated production chain instead of requiring the player to stop or overbuild downstream production merely to stockpile intermediates.

Industry adds Coal and Steel. Coal deliberately has competing uses in steel production and electricity generation, creating the first meaningful throughput constraint.

Power is capacity rather than a stored resource. When demand exceeds supply, production degrades gracefully instead of machines abruptly switching off.

## Facility managers

Every production facility has a persistent named manager who gives the facility a human identity. Managers are part of presentation and worldbuilding before they are a game system.

The initial cast is:

- Rhea Vale: Extraction Foreman
- Toma Sato: Furnace Chief
- Mara Quinn: Assembly Lead
- Dr. Ivo Mercer: Research Director
- Nika Drazic: Mining Supervisor
- Elias Voss: Steelworks Superintendent

Managers appear as the production stage they supervise becomes available. Their portraits and operational state should make the factory feel inhabited without requiring worker simulation.

Managers do not currently provide bonuses, traits, morale, levels, or progression gates. Those mechanics should only be introduced later if the characters remain valuable after the core factory loop is proven.

## Presentation

The game is a living 2D industrial diorama backed by a dashboard-style interface. It is not a free-placement factory builder. Production groups are logical systems while animated machinery, belts, furnaces, generators, lighting, effects, and facility managers visualize their state and throughput.

The visual direction is clean retro-industrial science fiction with anime-style human character art. Complexity and visual scale increase with technological eras.

## Idle behavior and persistence

Offline progress is first-class. The authoritative simulation should use rates, timestamps, and state-changing events so long absences can be advanced analytically where practical rather than replaying missed frame ticks.

There is no arbitrary offline-progress time cap.

Persistent world snapshots are versioned. At minimum they preserve resources, machine counts, progression milestones, and the timestamp needed to calculate offline advancement. Presentation state should not become authoritative save data unless it materially affects gameplay.

The current prototype autosaves periodically and saves on application pause or quit. The prototype persistence adapter is disposable infrastructure; production persistence should eventually sit behind a stable session/save boundary rather than reaching into presentation objects.

## Multiplayer contract

Every world is independent and complete.

The first multiplayer mechanic, after the solo slice is proven, is explicit resource transfer between two connected worlds. Later systems may add automated trade routes and cooperative projects.

No resource, era, machine, research node, or required achievement may depend on multiplayer.

Disconnecting a peer pauses remote cooperation without damaging local production or world integrity.
