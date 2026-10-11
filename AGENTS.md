# Asterforge Agent Notes

## Product constraints

- Every progression path must be completable solo.
- Multiplayer may accelerate, smooth, or diversify progression, but it must not gate resources, machines, eras, research, or required achievements.
- The first playable milestone is a solo Outpost vertical slice ending at sustainable steel production.
- Do not add multiplayer infrastructure until the solo vertical slice is enjoyable and stable.

## Architecture

- Target Unity 6000.3.26f1 LTS.
- Target Windows and macOS as first-class desktop platforms.
- macOS releases should target both Intel 64-bit and Apple silicon unless the platform plan is intentionally revised.
- Keep authoritative game simulation in `Asterforge.Game` without Unity engine references.
- Treat Unity-facing code as presentation, input, audio, persistence adapters, and later networking adapters around the simulation.
- Prefer analytical/rate-based advancement for idle and offline progress instead of replaying frame ticks.
- Prefer integer or fixed-point authoritative economy values over floating-point resource state.

## Planned networking

Networking is intentionally deferred. When introduced, use direct IP:port peer connectivity over Unity Transport/UDP with one host authoritative for the session. Each computer retains ownership of its own persistent world.
