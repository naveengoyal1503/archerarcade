# Archer Arcade — Screen Inventory

Every screen and state in v1.0. "Design frame" is filled when the Claude Design prototype is exported;
"Implemented by" is filled as screens are built. Orientation: landscape, 844 × 390 dp reference.
Back = Android Back behaviour.

| # | Screen / state | Purpose and contents | Back | Design frame | Implemented by |
|---|---|---|---|---|---|
| 1 | Splash | Logo + "NaveenCodes by Maa Labs™", loads save | — | | |
| 2 | First-launch tutorial (Level 1) | Hand-guided drag-to-aim over 3 targets, skip button | Pause | | |
| 3 | Home | Profile card (name, 3 pinned badges), coin pill, archer idling on a stage, big **Play** (continue campaign), mode cards (Quick Duel, 2-Player, Daily, Training), buttons: Archers, Badges, Shop, Stats, Settings | Exit confirm | | |
| 4 | Exit confirm (modal) | "Leave Archer Arcade?" Stay / Quit | Stay | | |
| 5 | World map (World 1) | Scrollable path of 20 level nodes, stars, chest nodes, locked nodes shake, world progress bar, back | Home | | |
| 6 | Level intro card | Level name, goal, opponent portrait, par, stars so far, Play | Map | | |
| 7 | "New!" idea card (modal) | Icon + title + one sentence + small looping demo (also "New tip!" cards with the impact effect: fireball, lightning, …) | Close | | |
| 8 | Archer select | Carousel of archers, turntable preview, stats bars, passive + ability demo, lock reason, upgrade button | Previous | | |
| 9 | Archer upgrade (sheet) | Level n → n+1, stat changes, coin cost, confirm | Close | | |
| 10 | Loadout | Archer + skin + trail, up to 3 special arrow-tip slots with ammo, boosters (Shield Bubble, Multi Arrow, Iron Helmet, Extra Heart — coins), Start | Previous | | |
| 11 | Match — aiming | HUD: HP bars + portraits, ability/power bar, wind gauge, turn timer ring, arrow-tip bar (ammo, locked tips with unlock level), active boosters, hit/headshot counters, pause; power ring + preview | Pause | | |
| 12 | Match — flight / impact | Camera follow, trail, hit-stop, damage numbers, "HEADSHOT!" | Pause | | |
| 13 | Match — AI turn | "Moss is aiming…" banner, AI aim animation | Pause | | |
| 14 | Match — knockout | Slow-mo, poof, victory pose | — | | |
| 15 | Pause (modal) | Resume, Restart, Settings, Quit to map/home | Resume | | |
| 16 | Victory | Stars pop, coins fly, badge progress, Next / Replay / Map | Map | | |
| 17 | Defeat | Tip, Retry, Assist offer (after 3), Map | Map | | |
| 18 | Chest (modal) | Shake, open, rewards list, coin fly | Close after open | | |
| 19 | Unlock (modal) | "Fire Archer unlocked!" + Try now | Close | | |
| 20 | Quick Duel setup | Archer, AI difficulty, arena, Start | Home | | |
| 21 | 2-Player setup | Names, archers + skins, arena, best of 1/3/5, wind, preview, handicap, Start | Home | | |
| 22 | Pass the phone | Full-screen cover "Pass to P2", I'm ready | Pause | | |
| 23 | 2-Player round result / final | Round winner, score, Rematch / Swap sides / Home | Home | | |
| 24 | Daily Challenge | Today's twist, streak calendar (7 days), Play, reward | Home | | |
| 25 | Training Range | Distance chips, wind slider, stats, exit | Home | | |
| 26 | Badges | Wall of 20 badges with tiers and progress, pin/unpin | Home | | |
| 27 | Badge unlocked (toast) | In-match / on-result toast | — | | |
| 28 | Shop (coins only) | Skins and trails, preview, buy with coins, "earned by playing" note | Home | | |
| 29 | Stats | Shots, accuracy, headshots, longest shot, wins/losses per mode, favourite archer, time played | Home | | |
| 30 | Settings | Audio, haptics, theme, language, left-handed, bigger targets, sensitivity, assist, reduce motion, battery saver, How to play, Credits, Privacy; footer "In loving memory of Maa ❤️" | Home | | |
| 31 | How to play | Re-read every "New!" card | Settings | | |
| 32 | Credits | Team, fonts/tools licences, Maa tribute | Settings | | |
| 33 | Coins sheet | Balance + where coins come from (no buying) | Close | | |
| 34 | Not enough coins (modal) | Shows how many are missing + ways to earn | Close | | |

States to cover in design: light + dark theme for every screen; locked / unlocked / maxed archer; empty and full
badge wall; 0 and many coins; first-time vs returning Home; long names in 2-Player.
