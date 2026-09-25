# Archer Arcade — Roadmap

Every version is free forever: no ads, no purchases. Each update adds content on top of the last; existing saves
always keep working. Staged features sit behind switches in `Logic/Meta/Features.cs` (like MindTap), so one
source tree can build any version.

---

## v1.0 — "Whispering Forest" (first release)

**Goal:** a complete, polished game with excellent shot feel. Small scope, no dead ends.

- Core shot: drag-to-aim, power ring, trajectory preview, gravity + wind, head/body/legs hits, hit-stop, shake,
  slow-mo knockout, camera follow.
- AI: Easy / Medium / Hard + Boss profiles, learning aim, human think time, fairness tests.
- 4 archers: Ranger, Fire, Electric, Bomb (passives + abilities), upgrades lvl 1–10, 2 skins each.
- 8 arrow tips with big impact effects: Normal, Fire (burn), Electric (lightning from the sky + stun),
  Split, Bomb (explosion + knockback), Heavy, Ice (freeze), Poison (cloud) — unlocked by playing.
- Injury looks: opponents look more and more hurt as HP drops (cartoon, no gore) + element marks.
- 7 enemy types (Crossbow Scout, Shield Bearer, Twin Shooter, Healer Druid, Tower Sniper, bubble shields…).
- Crate towers + TNT crates on floating-island arenas; match boosters (Shield Bubble, Multi Arrow, Iron Helmet,
  Extra Heart) for coins only.
- Campaign World 1: 20 levels (mini-boss L10, boss L20), 7 goal types, stars, par, chests every 5 levels.
- Modes: Campaign, Quick Duel vs computer, 2-Player on one phone (pass-and-play, best of 1/3/5, handicap),
  Daily Challenge (streak chest), Training Range.
- Meta: coins (earned only), 22 badges × 3 tiers, badge wall + pins, stats, trails.
- Polish: full UI (light + dark), animations, music (4 tracks + stingers), full SFX list, haptics, accessibility
  (left-handed, bigger targets, assist, reduce motion), English + Hinglish, 60/120 fps + battery saver.
- Release: AAB signed with the upload key, store listing, website page naveencodes.com/archer-arcade/.

## v1.1 — "Sunscorch Desert"
- World 2: levels 21–40 + boss The Sand Serpent (gusting wind, quicksand, mirages).
- New archers: **Ice Archer** (slow/freeze turn timer; ability Frost Wall shield) and **Poison Archer** (poison
  stacks; ability Toxic Cloud).
- New tips: **Laser** (straight beam), **Saw Blade** (cuts through crates); enemies Giant Brute, Balloon Rider,
  Bubble Mage.
- **Survival mode**: endless waves, best wave saved, badge "Last Archer Standing".
- **Daily missions** (3 a day) + mission board on Home.
- +10 badges.

## v1.2 — "Frostpeak Mountains"
- World 3: levels 41–60 + boss The Frost Giant (icy slides, icicles, avalanche turns).
- New archers: **Blast Archer** (knockback; ability Shockwave pushes off platforms) and **Wind Archer** (controls
  wind; ability Gale).
- New tips: **Drill** (pierces shields), **Homing** (weak), **Grappling** (move to a new perch); enemy Teleporter.
- **Weekly Challenge**: 7 hard levels a week, big chest.
- **Tournament** (vs computer): 8-archer bracket, 3 rounds.
- Replays: save and watch your best knockout (deterministic replay from shot inputs).

## v1.3 — "Ember Volcano" + "Sky Castle"
- World 4 (61–80) boss The Magma Drake; World 5 (81–100) final boss The Storm King.
- New archers: **Shadow Archer** (invisible trail; ability Blink) and **Light Archer** (heals on hit; ability Sun Beam).
- New tips: **Black Hole** (pulls enemies together), **Meteor** (calls a meteor), **Healing**.
- **Boss Rush**, archer mastery (per-archer challenges and a gold skin), photo mode for victory poses.

## v2.0 — Online (needs a decision on hosting and the INTERNET permission)
- Online 1-vs-1 with friends via room code (turn-based, sends `ShotInput`s; server only relays turns).
- Nearby play over local Wi-Fi as a no-server option.
- Must stay free: pick hosting that costs ~nothing at small scale, no accounts beyond a nickname, no data selling,
  a clear privacy policy update, and an "offline only" setting.

---

## Ideas backlog (not scheduled)
- Clans of archers, co-op boss fights (2 players vs boss on one phone).
- Seasonal events by phone date (Diwali lanterns world skin, winter snowfall), like MindTap.
- Level editor + share a level as a code (offline).
- Weather: rain (arrows drop faster), fog (no preview).
- Pets that cheer / give tiny passives (cosmetic-first).

## Explicitly NOT doing
- Ads, IAP, paid currency, loot boxes, energy systems, "watch to double", data collection, gore.
