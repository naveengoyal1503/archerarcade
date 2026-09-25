# Archer Arcade — Levels

All numbers are starting values (tuned in playtests) and live in level data files, not code.
Every level is validated by an EditMode test: the AI aim solver must be able to hit every required target /
zone from the start positions with the level's arrows and worst-case wind.

Columns: **Tier** E Easy · M Medium · H Hard · MB mini-boss · B boss. **Dist** = horizontal distance between
archers (m). **Wind** = range rolled each turn. **Par** = turns (duels) or arrows (target levels) for ★★★.
**Preview** = trajectory preview length (share of the arc).

---

## World 1 — Whispering Forest (v1.0)

Theme: sunny forest → dusk → night with fireflies (levels 16–20). Music: "Forest" loop; boss: "Warden" loop.
Enemies: the Bramble Bandits (Scout Pip, Hunter Moss, the Twig Twins, Ranger Bramble, Captain Thorn) and the
boss, the Forest Warden.

Difficulty rhythm: levels 1–4 teach (Easy), then a mixed Medium/Hard rhythm with a breather Easy after each
peak. Mini-boss at 10, boss at 20. Chests after levels 5, 10, 15, 20.

| # | Name | Tier | Goal | Opponent (AI) | Dist | Wind | Arena / props | New idea card | Par | Preview | Reward (first clear) |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | First Arrow | E | Targets: hit 3 static targets | — | 12–18 | 0 | Open meadow | **Tutorial**: drag back to aim, release to shoot | 4 arrows | 60 % | 20 |
| 2 | Hello, Pip | E | Duel | Scout Pip (Easy, 60 HP) | 16 | 0 | Open meadow | **Duels & HP**: take turns, knock them out | 6 turns | 45 % | 20 |
| 3 | Behind the Fence | E | Duel | Scout Pip (Easy, 80 HP) | 18 | 0–1 | Wooden wall in the middle | **Walls**: arc over cover · **Fire tip** unlocked | 6 turns | 45 % | 20 · unlocks **Fire tip** |
| 4 | Feel the Breeze | E | Targets: 4 targets | — | 15–25 | 1 | Open, leaves drifting | **Wind**: read the gauge, aim into it | 6 arrows | 45 % | 20 |
| 5 | Headhunter | M | Duel | Hunter Moss (Medium) | 20 | 0–2 | Low wall | **Headshots** ×2 damage | 5 turns | 30 % | 30 + **Chest 1** · unlocks **Fire Archer** |
| 6 | Crate Escape | E | Duel | Crossbow Scout (Easy, flat fast shots) | 18 | 0–1 | **Crate tower** between the islands | **Crate towers**: knock crates off | 6 turns | 45 % | 20 |
| 7 | Apple of My Eye | M | Apple Shot: hit 3 apples | friendly dummy | 14–22 | 0–2 | Dummy on stumps | **Apple Shot**: don't hit the dummy! | 4 arrows | 30 % | 30 · unlocks **Electric tip** |
| 8 | Swing Time | M | Targets: 4 swinging targets | — | 18–26 | 0–2 | Pendulum targets | **Swinging targets** · try the **Electric tip** (lightning!) | 6 arrows | 30 % | 30 |
| 9 | Moss Returns | H | Duel | Hunter Moss (Hard) | 24 | 1–3 | Moving platform (enemy) | **Moving platforms** | 5 turns | 18 % | 45 · unlocks **Split tip** |
| 10 | Captain Thorn | MB | Duel (mini-boss) | Captain Thorn (Hard, 160 HP, ability Thorn Volley) | 26 | 1–3 | Stump fort + crate tower with a **TNT crate** | **Mini-boss** · **TNT crates** | 7 turns | 18 % | 80 + **Chest 2** |
| 11 | Shield Up | E | Duel | Shield Bearer (Easy) | 20 | 0–1 | Shield blocks body shots; **Bomb tip** knocks it down | **Shields & Bomb tips** | 6 turns | 45 % | 20 · unlocks **Bomb tip** |
| 12 | Boing! | M | Trick Shot: hit 2 hidden targets | — | 18–24 | 0–2 | Bounce pads, tall wall | **Bounce pads** | 4 arrows | 30 % | 30 |
| 13 | Twig Twins | M | Gauntlet: 2 archers in a row | Twig Twins (Twin Shooters, Medium: 2 arrows/turn) | 20 / 24 | 1–3 | Two stumps | **Gauntlet**: HP carries over · **Heavy tip** | 9 turns | 30 % | 30 · unlocks **Heavy tip** |
| 14 | Cut the Rope | H | Rescue: cut 1 rope while an enemy shoots | Hunter Moss (Medium) | 22 | 1–3 | Cage on a rope, enemy on a tower | **Rescue** | 5 turns | 18 % | 45 |
| 15 | Kaboom Valley | M | Duel | Tower Sniper (Medium) on a crate tower | 24 | 1–3 | Explosive barrels + TNT tower under the sniper | **Explosive barrels** · topple the tower · **Ice tip** | 5 turns | 30 % | 30 + **Chest 3** · unlocks **Ice tip** |
| 16 | Dusk Duel | E | Duel | Healer Druid (Easy, heals 10 every 2 turns) | 20 | 0–2 | Fireflies, darker sky | **Healers**: finish them fast | 6 turns | 45 % | 20 |
| 17 | Split Decision | M | Targets: 3 targets behind cover | — | 20–28 | 1–3 | Cover rocks; targets clustered | **Split arrows** | 3 arrows | 30 % | 30 |
| 18 | Bramble's Revenge | H | Duel | Ranger Bramble (Hard, Triple Shot + **Bubble shield** every 3 turns) | 28 | 2–4 | Moving platforms both sides, crate tower | **Shield bubbles**: Electric pops them · **Poison tip** | 5 turns | 18 % | 45 · unlocks **Poison tip** |
| 19 | The Long Night | H | Gauntlet: 3 archers | Crossbow Scout (Medium), Healer Druid (Hard), Hunter Moss (Hard) | 22 / 26 / 30 | 2–4 | Night, bounce pad, barrels, TNT tower | (none) | 12 turns | 18 % | 45 |
| 20 | The Forest Warden | B | Boss (see GAME_DESIGN §6.4) | Forest Warden (Boss, 250 HP) | 30 | 2–5 | Stump fort, rotating shields, vine walls | **Boss** card | 9 turns | 18 % | 150 + **Chest 4** · unlocks **Bomb Archer** |

Electric Archer unlocks at **20 stars** (reachable around level 8 with good play).

### Per-level rules
- A level shows its "New!" card once (flag saved). Cards are skippable and re-readable in Settings → How to play.
- The player chooses archer + arrow tips (+ optional boosters) before every duel level; target levels lock the
  loadout when the level needs a specific tip (L11 gives Bomb ×2, L17 Split ×2).
- **Arrow tip unlocks (World 1):** Fire L3 · Electric L7 · Split L9 · Bomb L11 · Heavy L13 · Ice L15 · Poison L18.
  Every tip unlock shows a "New tip!" card with a looping demo of its impact effect (fireball, lightning, …).
- Replays keep the best stars and turns. Coins on replays are 50 %.
- Losing a duel: "Try again" (free, unlimited), tips after 2 losses (e.g. "Aim into the wind"), and after 3 losses
  the level offers **Assist** (preview +15 %) for that attempt; Assist still allows ★★ but not ★★★.

### Validation tests (EditMode)
- Every level loads, has a valid goal, spawn points inside the arena and a par ≥ the solver's minimum.
- AimSolver can hit every target / the opponent's head and body from the player's spawn in the worst wind.
- No target is reachable only by a shot that also hits the Apple-Shot dummy.
- Rewards and unlocks match the table (economy test).

---

## Daily Challenge (v1.0)

- Seed = date (UTC day number). Picks a base template (duel / targets / apple / trick) + a twist:
  Strong Wind (3–5), Split Only, Heavy Only, One Arrow Per Turn at 35 m, Tiny Targets, Night + fireflies.
- Opponent difficulty Medium; arena from World 1 props. Reward 50 coins; streak kept if solved each day.
- Same seed → same level on every phone (tested).

## Quick Duel arenas (v1.0)
Meadow (open), Fence (wall), Crates, Platforms, Barrels — random or chosen.

## 2-Player arenas (v1.0)
The same 5 arenas + "Mirror Forest" (symmetric, no props, pure skill). Wind on/off setting.

---

## Later worlds (ROADMAP)

| World | Version | Levels | Theme and new ideas | Boss | New archer(s) |
|---|---|---|---|---|---|
| 2 · Sunscorch Desert | 1.1 | 21–40 | sandstorms (gusting wind), cacti, quicksand platforms that sink each turn, mirages (fake targets) | **The Sand Serpent**: burrows and pops up in new spots | Ice, Poison |
| 3 · Frostpeak Mountains | 1.2 | 41–60 | icy platforms (slide), falling icicles, snow walls that rebuild, avalanche turns | **The Frost Giant**: throws ice boulders, freezes your timer | Blast, Wind |
| 4 · Ember Volcano | 1.3 | 61–80 | lava geysers between turns, fire walls, crumbling rock | **The Magma Drake**: flies between perches | Shadow, Light |
| 5 · Sky Castle | 1.3+ | 81–100 | floating islands, cannons, portals, wind fans | **The Storm King** (final boss) | — |
