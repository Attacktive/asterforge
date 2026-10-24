# Production dashboard acceptance checks

The dashboard must use the existing economy without changing prices, recipes, or save versions.

- Display resource balances and last measured net rates separately from sustainable production capacity.
- Explain steel limits using the simulation's Metal, Coal, and power capabilities, including generator fuel.
- Offer Buy 1, Buy 10, Buy 100, and Buy Max. Fixed batches are all-or-nothing, and all prices include per-unit escalation and rounding.
- Keep mutable world state behind a Unity-independent session interface; persistence must not reflect into private presentation fields.
- Preserve existing saves and offline progress.
- Keep controls reachable in desktop and narrow layouts, with keyboard focus, safe-area accommodation, and hold-to-mine input.

Run the Unity EditMode suite and manually inspect Play Mode at 1920x1080, 1280x720, and 800x600, plus a phone-like landscape aspect ratio. Confirm disabled purchase explanations, exact batch spending, immediate quote refresh, sustainable-versus-stockpile-assisted production, save reload, and input release behavior.

Android build integration remains in its existing separate pull request. Production scene art and multiplayer are outside this change.
