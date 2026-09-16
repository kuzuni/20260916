# Shared UI foundation integration

The runtime bootstrap creates one scaled root, independently sorted main/navigation canvases,
an unbounded sibling popup stack, and a non-interactive toast canvas. Feature modules must only
register builders; they must not create another scaler, event system, router, or safe-area policy.

## Coordinator hook

After feature branches are merged, add registrations after `main.screens` is assigned in
`RuntimeMainScreenFactory.Create`:

```csharp
ForgeScreenModule.Register(main.screens);
ProgressionScreenModule.Register(main.screens);
SocialScreenModule.Register(main.screens);
```

The foundation intentionally has no compile-time reference to those not-yet-merged classes.
Builders receive an empty safe logical root through `ScreenContext.Root`. Pages replace the
current page and close all modals. Modals/fullscreen presentations push a sibling layer; call
`context.Open` for details and `context.Close` from that layer's independent close button.

## Frozen route keys

| Module | Keys |
|---|---|
| Forge | `forge-probability`, `forge-probability-details`, `forge-item-details`, `equipment-details`, `forge-comparison`, `auto-forge` |
| Progression | `progress-pass`, `offline-rewards`, `skill-details`, `summon-probability`, `summon-probability-details`, `summon-result`, `skills-pets-heroes`, `dungeons`, `dungeon-details` |
| Social | `profile`, `settings`, `player-details`, `power-ranking`, `chat`, `shop`, `pvp`, `pvp-opponents`, `pvp-rewards` |

Use `ScreenPresentation.Page` for `skills-pets-heroes`, `dungeons`, `shop`, and `pvp`; their
detail views are modals. Registration is idempotent (a key is replaced), while repeated opening
of the current page or top modal is ignored. Route navigation must use these strings rather than
dependencies on another module class.

CI has separate PlayMode test and graphics jobs. Both fail when required secrets are missing.
The graphics job deliberately runs Unity under Xvfb without `-nographics` and uploads six aspect
ratio/safe-area captures, the verification report, and the Unity log.
