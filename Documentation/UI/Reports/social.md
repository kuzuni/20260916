# Social module implementation report

## Scope and references

Implemented `SocialScreenModule.Register` for references 06, 07, 11, 13, 18, 21,
22, 23 and 24. Every source PNG was inspected at its original 1080 × 1920
resolution before implementation. The module owns only its task folder and this
report; it does not alter the bootstrap, shared routing contract, scenes, asset
catalog, other feature modules, or `Assets/_Recovery`.

## Registered routes

| Route | Presentation | Runtime behavior |
|---|---|---|
| `profile` | Modal | Editable local name/gender/avatar preview, profile/settings tabs and ranking link |
| `settings` | Modal | Same profile view initially selecting settings, with six real `Toggle` controls |
| `player-details` | Modal | Shared dictionary payload, equipment/skill/stat presentation |
| `chat` | Fullscreen | Three selectable tabs, real scroll view and `InputField`; send is explicitly local-only |
| `power-ranking` | Modal | Scrollable portrait list; rows open shared player details |
| `shop` | Page | Scrollable deal cards and exactly five gem offers (60, 220, 800, 1500, 3300) |
| `pvp-opponents` | Modal | Scrollable opponent cards, portrait detail links and local challenge feedback |
| `pvp` | Page | Scrollable league table, sticky local-player row, rewards and challenge routes |
| `pvp-rewards` | Modal | Current rewards plus a scrollable reward-tier list |

The two additional gem offers intentionally show `가격 미설정`; no price or
payment behavior was invented. Ranking, PvP, profile edits, settings and chat are
deterministic local demonstration state and make no backend claim.

## Layout and assets

All hierarchy is constructed at runtime below `ScreenContext.Root`. Modal panels
are centered and bounded from `ScreenContext.Width`/`Height`, allowing the shared
safe-area host to adapt them at both portrait aspect ratios. Long content uses
real `ScrollRect`, `RectMask2D`, viewport and content objects. Artwork is kept in
separate image layers and reuses sprites exposed by `MainScreenAssets`; screenshots
are not imported or used as runtime backgrounds. Dark stone surfaces, double
gold/bronze rules, blue action controls, crimson close controls, Korean labels,
portrait slots and orange item slots reproduce the supplied visual language.

No Social-specific generated bitmap was available in the baseline, and image
generation was not used. Consequently the implementation reuses the shared icon
catalog and code-native ornamentation rather than claiming bespoke portrait,
crest, gem-pile, banner or shop-product paintings.

## Integration hook

After the foundation contract is integrated, the coordinator must invoke:

```csharp
SocialScreenModule.Register(registry);
```

No direct dependency on another feature module and no shared-type substitute was
added. Cross-screen navigation exclusively uses `ScreenContext.Open` route keys.

## Verification performed

- Confirmed the worktree baseline is `7b244bc968d0569479fe95997ec329deda773885`.
- Inspected all nine original PNGs at full resolution with the container image viewer.
- Ran `git diff --check` (pass).
- Checked route declarations, presentations, five required gem quantities, real
  `ScrollRect`, `InputField`, `Toggle`, and payload-key usage by static search.
- Checked newly added metadata GUIDs for uniqueness against tracked project metas.

## Unrun acceptance checks and limitations

Unity 6000.3.8f1 cloud compilation, play-mode modal-stack input tests and captures
at 1080 × 1920 / 1080 × 2280 (including simulated safe insets) remain unrun. The
foundation routing types described in `ARCHITECTURE.md` are deliberately absent
from this baseline and are being supplied by the separate common-foundation task;
this feature module therefore cannot compile in isolation without violating the
frozen contract. Static checks are not reported as a Unity pass. No local Unity,
Unity MCP, user-PC runner, credential, or `Library/Moonlit.command` was used.

Final visual acceptance still requires the coordinator's cloud integration run,
including modal click-blocking, top-only back, ranking/PvP parent scroll retention,
tab restoration, safe-area captures, and device-font review.
