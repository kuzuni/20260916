# Cloud task 03 — profile/PvP/chat/shop (9 references)

Implement SocialScreenModule.Register per ARCHITECTURE.md. Own only Assets/DarkFantasyUI/Scripts/Screens/Social/**, Art/Screens/Social/** with metas, and Documentation/UI/Reports/social.md. Do not edit shared files.

Routes/reference IDs: 06 profile (Modal, settings is its tab); 07 settings (register as Modal opening same profile view with settings tab); 11 player-details (Modal, shared by main/ranking/PvP/opponents); 13 chat (Fullscreen); 18 power-ranking (Modal); 21 shop (Page); 22 pvp-opponents (Modal); 23 pvp (Page); 24 pvp-rewards (Modal).

Inspect original PNGs. Build profile with name/gender/avatar editing and ranking link; settings with real toggles; shared player equipment/skill/stats view; scrollable portrait ranking and PvP lists, player's sticky row and challenge CTA; opponent selection and reward tiers; tabbed scrollable local chat with InputField/send feedback. No actual messages sent to others.
Shop must have scrollable deal cards and EXACTLY five gem offers: 60, 220, 800, 1500, 3300. Reference only displays the first three; do not invent real prices for last two or connect payments. Reuse shared premium currency artwork.

All lists have real ScrollRect behavior; nested profile links preserve parent scroll and selection. Local demo state and explicit unconnected actions, no promises of a live backend.

Use existing/generated illustrated art; preserve generation prompts and report any unavailable assets. Never paste reference screenshots into production. Cross-feature calls only via context.Open.

Deliver code/report with registrations, hooks, checks and visual evidence/limitations. Foundation types come from a parallel task; no conflicting stub types in production. Do not merge main.
