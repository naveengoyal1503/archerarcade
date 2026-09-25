# Archer Arcade — Screen Inventory

Every screen and state in v1.0. "Design frame" = screen id in `Design/Archer Arcade.dc.html` (state numbers from
`Design/Archer Arcade States.dc.html`); "—" = not in the prototype yet, build from the kit and GAME_DESIGN;
"Implemented by" is filled as screens are built. Orientation: landscape, 844 × 390 dp reference.
Back = Android Back behaviour.

| # | Screen / state | Purpose and contents | Back | Design frame | Implemented by |
|---|---|---|---|---|---|
| 1 | Splash | Logo + "NaveenCodes by Maa Labs™", loads save | — | `splash` (state 01) | `SplashScreen` (Home scene, first run) |
| 2 | First-launch tutorial (Level 1) | Hand-guided drag-to-aim over 3 targets, skip button | Pause | — (uses `match` HUD; overlay not in prototype) | `Tutorial/TutorialOverlay` over the Level 1 match |
| 3 | Home | Profile card (name, 3 pinned badges), coin pill, archer idling on a stage, big **Play** (continue campaign), mode cards (Quick Duel, 2-Player, Daily, Training), buttons: Archers, Badges, Shop, Stats, Settings | Exit confirm | `home` (state 02 dark) | `HomeScreen`, `ArcherStage`, `ModeCard` |
| 4 | Exit confirm (modal) | "Leave Archer Arcade?" Stay / Quit | Stay | — (prototype uses a "Back again to exit" toast) | `ConfirmModal` from `HomeScreen.OnBack` |
| 5 | World map (World 1) | Scrollable path of 20 level nodes, stars, chest nodes, locked nodes shake, world progress bar, back | Home | `map` (state 10 world locked) | `MapScreen` |
| 6 | Level intro card | Level name, goal, opponent portrait, par, stars so far, Play | Map | `loadout` header card (kicker, goal) | `LoadoutScreen.Campaign` header card + in-match `MatchBanner` |
| 7 | "New!" idea card (modal) | Icon + title + one sentence + small looping demo (also "New tip!" cards with the impact effect: fireball, lightning, …) | Close | `loadout` "New!" line | `IdeaCardModal` (Loadout, match start, result, How to play) |
| 8 | Archer select | Carousel of archers, turntable preview, stats bars, passive + ability demo, lock reason, upgrade button | Previous | `archers` (state 09 locked) | `ArchersScreen` |
| 9 | Archer upgrade (sheet) | Level n → n+1, stat changes, coin cost, confirm | Close | `archers` upgrade button | `UpgradeModal` |
| 10 | Loadout | Archer + skin + trail, up to 3 special arrow-tip slots with ammo, boosters (Shield Bubble, Multi Arrow, Iron Helmet, Extra Heart — coins), Start | Previous | `loadout` | `LoadoutScreen` |
| 11 | Match — aiming | HUD: HP bars + portraits, ability/power bar, wind gauge, turn timer ring, arrow-tip bar (ammo, locked tips with unlock level), active boosters, hit/headshot counters, pause; power ring + preview | Pause | `match` (states 05, 06) | `HudScreen`, `DragAim`, `TrajectoryPreview`, `MatchSceneRoot` |
| 12 | Match — flight / impact | Camera follow, trail, hit-stop, damage numbers, "HEADSHOT!" | Pause | `match` | `ArrowFlight`/`ArrowView`, `CameraRig`, `MatchJuice`, `DamageNumbers` |
| 13 | Match — AI turn | "Moss is aiming…" banner, AI aim animation | Pause | `match` (AI turn) | `AI/AiTurn` + `HudScreen` turn label |
| 14 | Match — knockout | Slow-mo, poof, victory pose | — | `match` (KO) | `MatchJuice.Knockout`, `TimeScaleDriver`, `FighterRoster` |
| 15 | Pause (modal) | Resume, Restart, Settings, Quit to map/home | Resume | `match` + `pause` (state 03) | `PauseModal` |
| 16 | Victory | Stars pop, coins fly, badge progress, Next / Replay / Map | Map | `result` victory (state 07) | `ResultScreen` (victory), `Confetti`, `CoinFly` |
| 17 | Defeat | Tip, Retry, Assist offer (after 3), Map | Map | `result` defeat (state 08) | `ResultScreen` (defeat: tip, Retry with Assist) |
| 18 | Chest (modal) | Shake, open, rewards list, coin fly | Close after open | `chests` + `chestOpen` (state 11) | `ChestsScreen` + `ChestOpenModal` |
| 19 | Unlock (modal) | "Fire Archer unlocked!" + Try now | Close | — | `UnlockModal` |
| 20 | Quick Duel setup | Archer, AI difficulty, arena, Start | Home | `modes` | `LoadoutScreen.QuickDuel` |
| 21 | 2-Player setup | Names, archers + skins, arena, best of 1/3/5, wind, preview, handicap, Start | Home | `modes` (pvp) | `PvpSetupScreen` |
| 22 | Pass the phone | Full-screen cover "Pass to P2", I'm ready | Pause | `match` + `pass` (state 04) | `PassModal` |
| 23 | 2-Player round result / final | Round winner, score, Rematch / Swap sides / Home | Home | `result` (pvp) | `ResultScreen` (2-Player round / final) |
| 24 | Daily Challenge | Today's twist, streak calendar (7 days), Play, reward | Home | `daily` | `DailyScreen` → `LoadoutScreen.Daily` |
| 25 | Training Range | Distance chips, wind slider, stats, exit | Home | `modes` (training) + `match` | `LoadoutScreen.Training` + match HUD (hits / bullseyes) |
| 26 | Badges | Wall of 20 badges with tiers and progress, pin/unpin | Home | `badges` | `BadgesScreen`, `BadgeModal` |
| 27 | Badge unlocked (toast) | In-match / on-result toast | — | — | `UIManager.Toast` (result screen, badge modal) |
| 28 | Shop (coins only) | Skins and trails, preview, buy with coins, "earned by playing" note | Home | `shop` | `ShopScreen`, `ShopPreviewModal` |
| 29 | Stats | Shots, accuracy, headshots, longest shot, wins/losses per mode, favourite archer, time played | Home | `stats` | `StatsScreen` |
| 30 | Settings | Audio, haptics, theme, language, left-handed, bigger targets, sensitivity, assist, reduce motion, battery saver, How to play, Credits, Privacy; footer "In loving memory of Maa ❤️" | Home | `settings` (state 13 dark) | `SettingsScreen` |
| 31 | How to play | Re-read every "New!" card | Settings | — | `HowToPlayScreen` |
| 32 | Credits | Team, fonts/tools licences, Maa tribute | Settings | `credits` (state 14) | `CreditsScreen` |
| 33 | Coins sheet | Balance + where coins come from (no buying) | Close | — | `CoinsModal` (coin pill) |
| 34 | Not enough coins (modal) | Shows how many are missing + ways to earn | Close | `archers` locked CTA (state 09) | `CoinsModal` (missing amount) |

States to cover in design: light + dark theme for every screen; locked / unlocked / maxed archer; empty and full
badge wall; 0 and many coins; first-time vs returning Home; long names in 2-Player.

Prototype-only screen not in v1.0: `missions` (daily missions are ROADMAP 1.1) — not built for 1.0.

Added beyond the list (in the design's mode list): **Survival** — `LoadoutScreen.Survival` → match (wave banner,
wave chip, HP carries over) → `ResultScreen` (waves, best, coins).
