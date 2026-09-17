# 30-screen reference catalog

> 2026-09-18 gameplay update: [current game rules](../Gameplay-20260918.md) supersede screenshot/demo economy, equipment, collection, combat and reward values. The 30 reference routes and safe-area contracts remain in effect. Three primitive skill effects are implemented; higher skill and pet/mount illustrations remain deferred by user request.

The user supplied 24 initial images and six additional profile/settings dialogs on 2026-09-17, explicitly making their filenames the screen and navigation requirements. Full original names are preserved in reference-manifest.json. Short ASCII filenames avoid Windows path limits. Reference PNGs are byte-for-byte copies, outside Assets so Unity does not import them into the player.

Read the corresponding original image at full resolution before implementing. These are visual references, not runtime backgrounds containing baked UI. Create real controls, text, scroll lists, independent item icons, reusable empty slot frames and separate decorative art.

| ID | Screen key | User's original instruction | Reference |
|---|---|---|---|
| 01 | `forge-probability` | 대장간 레벨33버튼 클릭시 뜨는 확률정보 팝업 | [PNG](References/01-forge-probability.png) |
| 02 | `forge-probability-details` | 대장간 레벨33버튼 클릭시 뜨는 확률정보 팝업에서 오른쪽 상단에 ! 버튼 클릭시 뜨는 세부정보 팝업 | [PNG](References/02-forge-probability-details.png) |
| 03 | `forge-item-details` | 대장간 레벨33버튼 클릭시 뜨는 확률정보 팝업에서 오른쪽 상단에 ! 버튼 클릭시 뜨는 세부정보 팝업에서 해당 아이템 클릭시 뜨는 세부정보 팝업 | [PNG](References/03-forge-item-details.png) |
| 04 | `dungeon-details` | 던전팝업에서 열기 버튼 클릭시 뜨는 세부 팝업 | [PNG](References/04-dungeon-details.png) |
| 05 | `progress-pass` | 메인 오른쪽상단에 요정버튼 있는데 그거 패스 버튼으로 만들고 그거 클릭시 뜨는 진행패스 팝업 | [PNG](References/05-progress-pass.png) |
| 06 | `profile` | 메인 왼쪽상단에 프로필(moonzsanf)클릭시 뜨는 프로필 설정 팝업 | [PNG](References/06-profile.png) |
| 07 | `settings` | 메인 왼쪽상단에 프로필(moonzsanf)클릭시 뜨는 프로필 설정 팝업에 설정 탭 부분 | [PNG](References/07-settings.png) |
| 08 | `equipment-details` | 메인 화면에 장비 슬롯 클릭시 뜨는 장비 세부정보 팝업 | [PNG](References/08-equipment-details.png) |
| 09 | `forge-comparison` | 메인에 모루 버튼 클릭시 뜨는 아이템 제작한거랑 현재 아이템이랑 비교해서 장착할지 판매할지 보는 팝업 | [PNG](References/09-forge-comparison.png) |
| 10 | `offline-rewards` | 메인에 왼쪽 상단에 5일 3시 이렇게 써있는거 클릭시 뜨는 오프라인보상팝업 | [PNG](References/10-offline-rewards.png) |
| 11 | `player-details` | 메인에서 !버튼 클릭 또는 랭킹팝업에서 프로필버튼(얼굴) 클릭 또는 pvp팝업에서 플레이어들 프로필 버튼 클릭 또는 pvp세부팝업에서 프로필 버튼 클릭시 뜨는 플레이어 세부정보 팝업  | [PNG](References/11-player-details.png) |
| 12 | `auto-forge` | 메인화면에서 자동 버튼 클릭시 뜨는 자동제련 팝업 | [PNG](References/12-auto-forge.png) |
| 13 | `chat` | 메인화면에서 채팅버튼 클릭시 뜨는 채팅팝업 | [PNG](References/13-chat.png) |
| 14 | `skill-details` | 스킬 팝업에서 스킬 버튼 클릭시 뜨는 스킬 세부정보 팝업 | [PNG](References/14-skill-details.png) |
| 15 | `summon-probability` | 스킬팝업에서 ! 버튼 클릭시 뜨는 소환 확률 팝업 | [PNG](References/15-summon-probability.png) |
| 16 | `summon-probability-details` | 스킬팝업에서 ! 버튼 클릭시 뜨는 소환 확률 팝업에서 ! 버튼 클릭시 뜨는 스킬 소환 확률 세부꺼 팝업 | [PNG](References/16-summon-probability-details.png) |
| 17 | `summon-result` | 스킬팝업에서 소환 버튼 클릭시 뜨는 소환 결과팝업 | [PNG](References/17-summon-result.png) |
| 18 | `power-ranking` | 프로필팝업에서 파워 랭킹부분 클릭시 뜨는 팝업 | [PNG](References/18-power-ranking.png) |
| 19 | `skills-pets-heroes` | 하단네비 가운데 버튼 클릭시 뜨는 스킬,펫,기술트리 팝업 근데 기술트리는 이름 영웅으로 바꾸기 | [PNG](References/19-skills-pets-heroes.png) |
| 20 | `dungeons` | 하단네비 두번째인 문버튼 클릭시 뜨는 던전팝업 | [PNG](References/20-dungeons.png) |
| 21 | `shop` | 하단네비 맨 오른쪽거 클릭시 뜨는 상점 팝업, 스크롤 가능해야함 아래에 다이아 상품 5개여야함  각각 60, 220 , 800, 1500, 3300 | [PNG](References/21-shop.png) |
| 22 | `pvp-opponents` | PVP 도전버튼 클릭시-상대선택팝업뜨는거 | [PNG](References/22-pvp-opponents.png) |
| 23 | `pvp` | PVP 팝업-하단네비중 맨왼쪽 해골 버튼 클릭시 뜸. | [PNG](References/23-pvp.png) |
| 24 | `pvp-rewards` | pvp팝업에서 상단에 선물상자 버튼 클릭시 뜨는 pvp 보상 세부 팝업 | [PNG](References/24-pvp-rewards.png) |

## Explicit overrides

Additional child dialogs (each opens above the existing profile/settings modal, preserving its tab):

| ID | Screen key | User's original instruction | Reference |
|---|---|---|---|
| 25 | `profile-name` | 프로필-닉네임설정팝업 | [PNG](References/25-profile-name.png) |
| 26 | `profile-gender` | 프로필-성별설정팝업 | [PNG](References/26-profile-gender.png) |
| 27 | `profile-avatar` | 프로필-아바타설정팝업 | [PNG](References/27-profile-avatar.png) |
| 28 | `settings-language` | 설정-언어선택 팝업 | [PNG](References/28-settings-language.png) |
| 29 | `settings-blocked` | 설정-차단목록팝업 | [PNG](References/29-settings-blocked.png) |
| 30 | `settings-account` | 설정-계정 부분 팝업 | [PNG](References/30-settings-account.png) |

- Name confirmation costs 200 demo rubies; empty/unchanged names, insufficient funds and repeated confirmations cannot spend. Cancel preserves state.
- Gender has two choices with a separate selection check. Avatar has 20 choices in four columns, with independent empty rims and generated portrait art.
- Language has 11 mutually exclusive rows; selection survives dialog reopen within Play. Translation services are unconnected.
- Blocked list starts with the three reference demo names; select a row to reveal local unblock. Account link/logout/delete provide demo feedback only; no real account changes.

- Main fairy button opens progress-pass.
- Latest user icon override (2026-09-17): main progression-pass entry uses a blue sword/pass pennant; offline rewards uses a clock and reward chest. Replace fairy/brazier artwork while retaining frameless hit targets and separate timer labels.
- Main left timer opens offline-rewards.
- Main forge level button opens forge-probability → forge-probability-details → forge-item-details.
- Main anvil opens forge-comparison (generated versus equipped item, sell/equip).
- Main auto button opens auto-forge settings rather than immediately toggling.
- Main equipment slots open equipment-details.
- Main profile opens profile with settings as a tab, not a further modal level.
- Main info button, ranking portraits, PvP portraits and opponent portraits open the SAME player-details view with a player payload.
- Bottom nav index 0 = pvp; 1 = dungeons; 2 = skills-pets-heroes; 4 = shop. Index 3 has no new supplied reference; retain current behavior pending further design.
- Third tab of skills-pets-heroes must say 영웅, replacing 기술트리. Pet/hero tab appearance is not separately supplied: reuse this screen's established visual language and document inferred content.
- Skill catalog contains 18 entries with 15 initially owned; the first 15 display in reference 19 order. The final three unowned entries and their descriptions are local demo content because their art/names are not visible in the supplied reference. Count title derives from current ownership; equipped locks/ribbons are separate from ownership and update after equip/quick equip.
- Shop scrolls and contains exactly five diamond offers: 60, 220, 800, 1500, 3300. Prices not shown for the extra offers are unconfigured, not invented real prices.
- Distinguish a full content page (dungeons, skills/pets/heroes, shop, PvP) from modal overlays inside it. Keep nav visible/usable on base pages; modal dialogs above those pages block it.
- All visible controls must respond meaningfully with local demo state. No real payment, live chat delivery, server ranking or real multiplayer claim.

## Visual acceptance

Main battle background override (2026-09-18, supersedes the forest request): use the empty gothic stone crypt environment from ArtReferences/MainBattleCrypt.jpg for the battle viewport only. No displayed player, pet, monster or health bar; remove the former cyan particle overlay. HUD, forge, navigation, popup backgrounds and latest functional pass/offline icons remain unchanged. This is an environment revision, not a 31st screen. Generated asset and exact prompt: EmptyCryptBattle-v1-prompt.md.

Preserve dark stone, thin ornate gold/bronze frames, silver-trim blue buttons, crimson circular close buttons, illustrated icons, readable Korean type and original layout proportions. No generic plain-list replacement for these detailed references. Existing generated main-screen art is the shared baseline. Gold is the crown coin, ruby is the elongated red diamond. No added frame around timed event buttons. Anvil remains a separate foreground button.

Every screen must work at 1080x1920 and 1080x2280, including simulated top/bottom and side safe insets. Dim covers the full viewport; interactive content stays in the safe area. Scroll long content rather than squeezing type to illegibility.
