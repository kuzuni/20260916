# Auto-forge checkbox frame — generated artwork

Built-in imagegen, 2026-09-16. Reference 12-auto-forge.png inspected at full resolution. Generated PNG copied unchanged to Assets/DarkFantasyUI/Resources/Moonlit/Forge/CheckboxFrame-v1.png. Output 1254x1254, corner alpha 0; source exec-d606b808-189e-48be-a719-1d83df6781ee.png.

Exact prompt:

Use case: stylized-concept. Create one reusable EMPTY checkbox frame for a dark-fantasy mobile RPG Unity UI. Square shape with chamfered octagonal corners, straight thin antique bronze/gold metallic bevel around a nearly black charcoal inset face. Strong readable silhouette, restrained gold edge highlights, tiny worn scratches, slight inner shadow. Empty dark center: the gold checkmark is drawn later as a separate live UI graphic. Centered straight-on orthographic, occupies 90 percent of square canvas, equal margins. True alpha transparency outside the frame. No checkmark, no tick, no text, numbers, symbols, jewels, curls, circular shapes, surrounding panel, mockup or scene. Premium hand-painted gothic inventory metal border, clean and legible at 48 pixels.

Runtime uses the whole imported texture on a 48x48 aspect-preserving Image. Tick is an independent live gold Text graphic controlled by a real Toggle. Frame remains present when unchecked. The existing SettingsSwitch-v1 atlas and switch construction now live in the shared PopupSkin.Switch helper; settings retains its wrapper and behavior, while auto-forge uses it for the stat-filter master. Four tier bands/icons reuse existing generated Forge atlases, separate from checkbox/name/rate/star. No new tier or switch artwork was generated.

Validation at commit: generated bitmap visually inspected, dimensions/transparency measured, static diff checked. Added auto-forge behavior test for imported art, master disable/re-enable, retained individual filters across close/reopen, quantity clamp and start/stop. Hosted Unity 6000.3.8f1 tests and captures pending.
