# Economy Redesign Proposal

## Why redesign now

The first playable economy proved that Asterforge's technical foundation works, but it also exposed a structural problem in the economy itself.

The current prototype is built around a long automatic conversion chain:

`Ore -> Metal -> Components -> Research`

That chain creates a recurring ownership conflict whenever an intermediate resource is both a construction currency and a continuously consumed production input.

The Research Station made the problem obvious: Components are needed to build infrastructure, but active Research Stations continuously consume every new Component they can reach.

Adding pause buttons, reserves, priorities, or routing could make that model controllable, but those systems would mostly compensate for a problem created by the economy's basic shape.

This proposal changes the shape instead.

## Product direction

Asterforge should feel like a civilization becoming able to perceive, extract, and exploit more of its environment as technology improves.

Progression should broaden the economy rather than merely extend one production chain.

The player should increasingly decide what to build and what capability to unlock, while the game handles repetitive operation automatically.

The intended fantasy is not a logistics puzzle about keeping every intermediate buffer alive.

It is a growing industrial civilization whose reachable resource space expands with technology.

## Core economic grammar

### 1. Most resources are independent production streams

The default resource model is direct generation by infrastructure.

Examples:

- Extractors produce Ore.
- Coal Mines produce Coal after Coal extraction is unlocked.
- Later mining or harvesting facilities produce newly discovered resources directly.
- Research infrastructure produces Research as a capability stream rather than by continuously consuming construction materials.

Independent streams reduce accidental starvation cascades and make shortages easier to understand.

### 2. Technology expands what can be gathered

Resources may conceptually exist before the player can use them.

A technology can make a new resource detectable, extractable, or economically useful.

For example:

- Outpost technology permits Ore extraction.
- Industrialization permits Coal extraction.
- Later materials technology may reveal Silica, Rare Metals, or another precision-industry resource.
- Energy-era technology may unlock Uranium or another high-energy resource.
- Orbital and later eras may unlock off-world resources.

Unlocking technology should therefore change the visible economy, not only apply percentage bonuses.

### 3. Technology also improves existing extraction

A technology may increase the productivity or efficiency of an already available resource stream.

For example, a mining progression could move from basic extraction to mechanized mining, deep mining, and autonomous mining.

This allows old resources to remain relevant while new resources broaden the economy.

### 4. Deliberate spending is the primary sink for construction materials

Resources should most often leave the stockpile because the player intentionally spends them on something.

Primary sinks include:

- facilities
- upgrades
- research projects
- era transitions
- large infrastructure projects
- later cooperative or orbital projects

A resource that is important for construction should not normally be consumed automatically by an unrelated background process unless that continuous consumption is itself an intentional part of the resource's identity.

### 5. Continuous transformations are exceptional rather than universal

Asterforge can still contain production chains.

They should exist where the transformation is intuitive, strategically useful, and worth the management cost.

Examples that may remain appropriate:

- Ore into refined Metal
- Metal plus industrial inputs into Steel
- fuel into usable power capacity

The game should not assume that every new resource must be produced by consuming the previous resource forever.

A retained continuous transformation must also pass a recoverability check when its input is useful outside that transformation.

If downstream consumption can match or exceed the entire input stream, the player still needs a way to fund additional upstream capacity or other required construction without stopping existing production.

Prefer solving that through the resource and cost graph itself rather than universal reserves, priorities, or pause controls.

Moving a problematic transformation to deliberate project spending is valid when automatic consumption would otherwise recreate the same stockpile trap.

### 6. Consumable operating resources should feel naturally consumable

Continuous consumption fits resources whose purpose is to be burned, expended, or maintained.

Likely examples include:

- Coal or later fuels for power
- coolant
- maintenance supplies
- transport fuel
- specialized reactor inputs

Construction materials should not become implicit operating fuel merely to create a dependency graph.

## Resource web instead of resource ladder

The economy should form a web of mostly independent streams connected by deliberate costs and a smaller number of meaningful transformations.

A conceptual early shape could look like this:

```text
Ore ---------> Metal ---------> machinery
   \                           \
    \                           -> selected heavy-industry products
     \
Coal --------> power

later discovered resource ----> precision infrastructure

research infrastructure ------> Research
```

The important property is not the exact resource names.

The important property is that one weak intermediate stream does not automatically freeze every later system.

## Research

Research should represent installed scientific capability.

Research Stations should therefore be capital investments first.

A likely baseline is:

- commissioning a Research Station costs construction resources
- an active Research Station generates Research over time
- Research Stations may later require power
- Research Stations do not continuously burn Components merely to remain productive

Research becomes the mechanism that expands the economic possibility space.

Useful research rewards include:

- reveal a new resource
- unlock extraction infrastructure for a new resource
- improve extraction of an existing resource
- unlock a new facility or project
- unlock a new industrial process
- improve automation or consolidation

Flat percentage bonuses can exist, but they should not carry the progression fantasy by themselves.

## First playable redesign

The redesign should again implement only enough content to test Outpost and Industry before extending later eras.

Exact prices and rates should be treated as tuning values, not design commitments.

### Outpost

The player starts with access to Ore and basic extraction.

The first minutes should establish three ideas:

1. infrastructure creates persistent production capacity
2. construction is an intentional investment
3. Research unlocks qualitatively new capability

A possible Outpost roster is:

- Extractor: produces Ore
- Refinery or Smelter: creates refined Metal if keeping the Ore-to-Metal transformation remains fun in play
- Research Station: produces Research without consuming construction materials every second

The first major goal is Industrialization research.

### Industry

Industrialization makes Coal available for extraction and introduces power.

A possible Industry roster is:

- Coal Mine: produces Coal
- Coal Generator: converts Coal into power capacity while demand exists
- heavy-industry facility: unlocks Steel or an equivalent strategic material

Industry should test whether a small number of continuous dependencies remain interesting when the rest of the economy is not one long automatic chain.

The first vertical slice should still end with a stable industrial capability rather than merely owning one machine.

The exact success condition should be reconsidered after the redesigned economy is playable.

It may remain sustained Steel production, but that should not be preserved solely because the first prototype used it.

## Era progression

The existing eight-era zoom-out structure remains useful:

1. Outpost
2. Industry
3. Automation
4. Precision
5. Energy
6. Orbital
7. Planetary
8. Stellar

The economic role of eras changes slightly.

Each era should ideally introduce one or more of the following:

- a newly exploitable resource
- a new scale of extraction
- a new capability that combines existing resources
- a new automation abstraction
- a larger spatial or industrial scale

A tentative progression could be:

### Outpost

- Ore
- basic refinement
- Research

### Industry

- Coal
- power
- heavy industry

### Automation

- automation technology
- improved extraction and production control
- possibly the first newly detected precision resource

### Precision

- Silica, Rare Metals, or another resource appropriate to electronics and advanced manufacturing
- precision machinery
- richer research infrastructure

### Energy

- Uranium or another high-energy resource
- major power-generation expansion

### Orbital

- off-world extraction
- orbital construction materials
- multiple production regions

### Planetary

- regional or planetary aggregation
- large infrastructure projects

### Stellar

- stellar-scale energy and construction resources
- megastructures

These names and resources are placeholders until the earlier eras prove what the game actually needs.

## What shortages should feel like

Shortages should remain part of the game.

The redesign is not trying to eliminate scarcity.

A good shortage tells the player:

- what is limiting progress
- why it is limiting progress
- what meaningful choices can improve it

A bad shortage traps the player because automation is consuming the same resource they are trying to save and the only available response is waiting.

The redesigned economy should prefer shortages caused by insufficient capacity or deliberate competing investments over shortages caused by an uncontrollable downstream consumer.

## What not to add yet

Do not introduce these systems merely to rescue the old chain model:

- per-machine pause controls
- resource reserve sliders
- routing priorities
- conveyor topology
- complex input buffers
- manual power priorities

Some of these may become interesting later for their own reasons.

None should be required just to make the basic economy playable.

## Power

The deterministic power simulation already built on `main` is still useful.

Power remains capacity rather than a stored resource.

Insufficient power should degrade powered production gracefully.

Coal or another fuel may still compete between power and selected industrial uses if that competition proves fun.

The open playable-power integration should not be merged unchanged before this redesign is settled because its UI and balance assumptions are attached to the old economy.

The simulation foundation can be reused when the redesigned Industry layer reaches power again.

## Presentation

The current IMGUI prototype remains disposable but useful for testing.

Do not rewrite presentation architecture as part of the economy redesign unless the existing shell actively blocks testing.

The redesigned prototype should make the following visible early:

- current resource balances
- actual production rates
- newly available resources after research
- major bottlenecks
- power capacity and demand once power returns
- the current technological objective

The UI should celebrate new resource discovery or exploitation as a major progression event.

## Persistence

The existing snapshot system and offline-progress architecture remain valuable.

The redesigned economy should introduce a new snapshot version rather than attempting to preserve the semantic meaning of every prototype-era machine count.

Because the game is still in prototype development, it is acceptable to invalidate or explicitly reset old prototype saves if migration would distort the redesigned economy.

Do not spend substantial engineering effort translating old machine counts into a fundamentally different resource model.

## Multiplayer

The multiplayer contract does not change.

Every progression path must remain fully completable solo.

Multiplayer remains deferred until the redesigned solo vertical slice is enjoyable and stable.

Independent resource streams may later make trade more interesting because different mature worlds can accumulate different surpluses without requiring multiplayer-exclusive resources.

## Implementation plan

### Phase 1: freeze the old economy

- Treat current `main` as the historical first-playable baseline.
- Do not extend the existing automatic conversion ladder further.
- Do not merge the current playable-power integration unchanged.

### Phase 2: specify the redesigned Outpost and Industry

- Choose the exact resource roster for only the first two eras.
- Decide which transformations remain continuous and which become construction or project costs.
- For every retained continuous transformation, identify how the player can fund additional upstream capacity when its input stockpile is empty and downstream consumption matches or exceeds supply without stopping existing production.
- Re-run that recoverability check after offline advancement so a long absence cannot create a trap that online play avoided.
- Resolve failures through the resource and cost graph where possible; moving a transformation to deliberate project spending is preferable to adding universal reserves or priorities solely to rescue it.
- Define Research generation and Industrialization unlock behavior.
- Define the first meaningful Industry objective.
- Set rough rates and costs only after the interaction graph is clear.

### Phase 3: replace authoritative economy rules

- Keep the simulation in `Asterforge.Game`.
- Replace obsolete recipes and progression rules rather than layering compatibility behavior on top of them.
- Preserve analytical offline advancement.
- Reuse the power model where it still matches the redesigned rules.
- Add tests around resource unlocks, production streams, research progression, and any remaining transformations.

### Phase 4: expose production truth

- Show actual production rates rather than only nominal machine recipes.
- Make resource unlocks and technology effects obvious.
- Show why a resource is constrained when a shortage occurs.
- Keep the first playable UI intentionally simple while the economy is still moving.

### Phase 5: playtest before adding another era

Stop after the redesigned Industry milestone is playable.

The next era should not be implemented until repeated play answers these questions positively:

- Does unlocking a resource feel meaningful?
- Does research visibly expand what the player can do?
- Are shortages understandable without emergency pause controls?
- Does the player make real investment choices rather than merely wait for a chain to refill?
- Are the remaining continuous transformations interesting enough to justify their complexity?
- Does offline progress preserve the same decisions rather than creating unexpected starvation behavior?

## Success criterion for the redesign

The redesign succeeds if the player can encounter a shortage, understand it, and have an interesting response without needing to fight the automation system.

The economy should invite the player to ask "what capability should I build next?" more often than "which machine do I need to stop so it quits eating my stuff?"
