# Archer Arcade — Game Design

Status: v1.0 design, 2026-09-25 (rev 2: elemental arrow tips, injury looks, enemy types, boosters). All numbers are **starting values** stored in configs and tuned in playtests.
Level-by-level data lives in `LEVELS.md`; screens in `SCREEN_INVENTORY.md`; versions in `ROADMAP.md`.

---

## 1. Vision and goals

**One line:** a bright, juicy archery duel game where every shot feels great, played in 1–2 minute matches
against the computer or a friend on the same phone. Free forever.

**Player goals**
- Land the perfect arc: read distance and wind, pull, release, headshot.
- Beat every level with 3 stars, beat the boss, unlock and upgrade every archer.
- Beat a friend in a best-of-3 duel on one phone.

**Product goals (v1.0)**
- Shot feel good enough that a new player shoots 10 arrows "just for fun" in the first minute.
- A full, polished v1 with one world (20 levels + boss), 4 archers, 4 arrow types, AI with 3 difficulties plus
  a boss, local 2-player, daily challenge, badges and chests. No dead ends.
- Stable 60 fps (120 on high-refresh phones), no hangs, save never lost, APK < 60 MB.
- A codebase ready for staged updates (1.1 → 1.3) and online play later (2.0) without rewriting match logic.

**Design pillars**
1. **Feel first:** pull tension, release snap, arrow whoosh, hit-stop, shake, slow-mo finish.
2. **Readable skill:** the player always sees why a shot missed (wind gauge, landing mark, AI "adjusts aim").
3. **Short and fair:** turn timer, no grinding, no pay walls; losing still gives something (coins, badge progress).
4. **Premium cartoon style:** chunky 3D-looking UI, colorful elements, smooth animation, great audio.

**Non-goals (v1.0):** online multiplayer, accounts, leaderboards, gore, realistic art, loot boxes, ads.

---

## 2. Core loop

```
Home → pick mode → (pick archer + arrows) → MATCH (turn by turn) → Victory / Defeat
     → stars + coins + badge progress → chest every 5 levels → upgrade / unlock archers → next level
Daily: Daily Challenge (streak) · Weekly: (1.2) · Always: Quick Duel, 2-Player, Training
```

---

## 3. The shot (core mechanic)

### 3.1 Aiming — drag to pull
- Touch **anywhere** on the play area (not on UI) and drag **away** from the target. The bow follows the drag
  angle; the pull distance sets power (0–100 %). Release to shoot. Drag back to the start point (< 12 dp) and
  release = cancel (no turn used).
- Power curve: `power = clamp01(dragDp / 180)` eased (easeOutQuad) so small adjustments are precise.
- Angle limits: −10° … +80° above horizontal (facing the opponent).
- Aim UI: power ring around the archer (0 → 100 %, color green → yellow → orange), angle number, and a dotted
  **trajectory preview** showing the first part of the arc:
  - Easy levels / Training: 45 % of the arc · Medium: 30 % · Hard + Boss: 18 % · 2-Player: setting (default 30 %).
- Haptic tick every 10 % of power; bow creak sound rises in pitch with power.
- Left-handed setting mirrors nothing in the world, only moves HUD buttons.

### 3.2 Flight
- 2D ballistic motion in world units (1 unit ≈ 1 m): `v0 = lerp(12, 34, power) m/s`, gravity `g = 18 m/s²`
  (arcade gravity, tuned for readable arcs), wind adds horizontal acceleration `a = wind × 0.9 m/s²`.
- Arrow type modifiers: Heavy ×1.35 gravity; Split splits at apex; Explosive normal flight.
- Fixed-step simulation (120 Hz) in Logic so results are identical on every device; Runtime interpolates visuals.
- Camera follows the arrow with a smooth zoom-out to fit shooter + arrow + target, then eases back.
- Arrows stick into walls/crates/shields/bodies (stay until the end of the turn), bounce off bounce pads, break
  on stone.

### 3.3 Hits and damage
| Zone | Multiplier | Feedback |
|---|---|---|
| Head | ×2.0 | "HEADSHOT!" text, gold burst, ding, hit-stop 70 ms, shake 8 dp |
| Body | ×1.0 | white burst, thud, hit-stop 45 ms, shake 5 dp |
| Legs | ×0.6 | small burst, hit-stop 30 ms |
| Shield / cover | 0 (absorbs) | wood/metal sound, splinters |

- Base damage (Normal arrow, archer level 1): **25**. Archer HP (level 1): **100**. So: 4 body hits or 2 headshots.
- Damage numbers float up (pooled), crits in gold and bigger.
- Knockout: HP ≤ 0 → slow-mo (timeScale 0.25 for 0.8 s real time) on the final hit, cartoon "poof" + stars
  spin, winner victory pose.

### 3.3.1 Impact effects (every hit must look and sound spectacular)
The arrow's **tip** decides what happens on impact (see §5). The tip glows in flight (fire flicker, electric
sparks, frost mist, bomb fuse spark) so the player sees what is coming. On impact:
- **Fire tip**: fireball burst (0.4 s), target catches fire (flames on the body for its burn turns), scorch mark.
- **Electric tip**: a **lightning bolt strikes down from the sky** onto the target 0.15 s after impact, screen
  flash, the target shakes (stunned: its next turn timer −4 s), sparks jump to one nearby target/prop.
- **Bomb tip**: big cartoon explosion (shockwave ring + smoke + debris), knockback pushes the target back,
  nearby crates fly apart.
- **Ice tip**: frost burst, the target is encased in ice for a moment (its next draw is 30 % slower), icicles.
- **Poison tip**: green cloud stays on the target for 2 turns (damage per turn), bubbles.
- Every element has its own sound, haptic and camera move (lightning = quick flash + shake; bomb = big shake +
  slow-mo 0.1 s; fire = warm glow). Effects are pooled and scaled down with "Reduce motion".

### 3.3.2 Injury looks (the opponent looks hurt as HP drops — cartoon, never gore)
| HP left | Look |
|---|---|
| 100–76 % | clean, confident idle |
| 75–51 % | scratches, a torn sleeve/cape, a band-aid, arrows that hit stay stuck in armor/clothes |
| 50–26 % | limps, heavier breathing idle, cracked helmet/armor piece falls off, sweat drops, angry/worried face |
| 25–1 % | dizzy stars circling the head, wobbling stance, bandaged head, "hurt" face, slower draw animation |
| 0 | knockout: poof cloud + stars, falls over, then a little "ghost" or sleeping Z's (never blood) |

Element marks stay until the end of the match: **soot and smoke** (fire), **frizzy hair + sparks** (electric),
**frost patches** (ice), **green tint + bubbles** (poison), **bruised armor** (bomb).
These are extra sprite layers / material swaps on the rig, driven by HP thresholds and status flags.

### 3.4 Wind
- Shown as a gauge at the top center: arrow direction + 0–5 bars + number (e.g. "3 ←").
- Changes at the start of each turn within the level's range (LEVELS.md): Easy 0–1, Medium 0–3, Hard 2–5.
- Leaves / snow particles drift with the wind so it can be felt, not only read.

### 3.5 Turns
- Players alternate turns. **Turn timer 12 s** (config); last 3 s ticks + pulse. Timeout = turn passes, no shot.
- The first turn goes to the player in campaign; coin-flip animation in Quick Duel and 2-Player.
- Status effects (burn, etc.) tick at the start of the affected archer's turn.

---

## 4. Archers (v1.0 roster: 4)

Each archer has stats, a **passive** and one **ability**. The ability charges after **3 of your turns**
(or 2 turns if you landed a headshot) and is used by tapping the ability button **before** releasing.

| Archer | Unlock | HP / dmg (lvl 1) | Passive | Ability |
|---|---|---|---|---|
| **Ranger** (starter) | owned | 100 / 25 | Steady hands: trajectory preview +10 % longer | **Triple Shot**: 3 arrows in a ±4° spread, 60 % damage each |
| **Fire Archer** | clear level 5 | 95 / 25 | Every hit adds **Burn**: 6 damage at the start of the target's next 2 turns | **Meteor Arrow**: flaming arrow, 40 damage + burn, 1.5 m splash (12) |
| **Electric Archer** | earn 20 stars | 100 / 24 | Hits **chain** to one other target/prop within 3 m for 50 % | **Storm Bolt**: a fast bolt that ignores wind, 35 damage, chains twice |
| **Bomb Archer** | beat the World 1 boss (level 20) | 105 / 22 | Arrows **explode** on impact: 1.2 m radius, +10 splash, break crates | **Cluster Bomb**: splits into 3 bomblets at the apex, 18 each + splash |

- Upgrades: levels 1–10 per archer, bought with coins (§8). Each level: +4 % damage, +4 HP.
- Cosmetics per archer (earned): 2 skins in v1 (default + one from badges/chests), color swatch variants.
- Each archer has its own animation set: idle breathing, draw, hold, release, recoil, hit, knockout poof,
  victory pose, and an ability cast.
- Element colors (for VFX + UI chips): Normal white, Fire orange-red, Electric cyan-yellow, Bomb dark + orange.
  Never color-only: every element also has an icon.

### 4.1 Enemy types (campaign + Quick Duel; each with its own look and behaviour)
| Enemy | Behaviour | First seen |
|---|---|---|
| Bandit Archer | basic duel opponent | W1 L2 |
| Shield Bearer | carries a big shield that blocks body shots; Heavy/Bomb tips knock it down | W1 L11 |
| Crossbow Scout | flatter, faster shots (less arc), low HP | W1 L6 |
| Twin Shooter | shoots 2 arrows per turn at lower damage | W1 L13 |
| Healer Druid | heals itself 10 HP every 2 turns with a leaf glow | W1 L16 |
| Bubble caster (Ranger Bramble in W1; Bubble Mage from 1.1) | casts a **shield bubble** every 3 turns that absorbs one arrow (Electric pops it instantly) | W1 L18 |
| Tower Sniper | stands high on a crate tower; knock the tower down to drop it | W1 L15 |
| Giant Brute | 2× HP, slow, big head (easier headshots) | 1.1 |
| Balloon Rider | floats and drifts between turns | 1.1 |
| Teleporter | blinks to a new island every 2 turns | 1.2 |
Bosses and mini-bosses: §6.4 and LEVELS.md.

---

## 5. Arrow tips (elements) — v1.0: 8 tips, more in updates

Every arrow has a **tip**. Tips are **unlocked by playing** (campaign levels), picked in the **Loadout** (Normal
+ up to 3 special tips per match) and switched during your turn from the arrow bar. Special tips have ammo per
match and refill every match (never consumed permanently, nothing to buy).

| Tip | Unlock | Ammo | Damage | Impact effect (§3.3.1) |
|---|---|---|---|---|
| Normal | owned | ∞ | 25 | clean hit, sticks in |
| Fire | W1 L3 | 3 | 22 + burn 6 × 2 turns | fireball burst, target catches fire |
| Electric | W1 L7 | 3 | 20 + chain 50 % | lightning strikes from the sky, stun (−4 s next timer), pops shield bubbles |
| Split | W1 L9 | 2 | 12 × 3 | splits into 3 at the apex (±6°) |
| Bomb | W1 L11 | 2 | 20 + splash 12 (1.2 m) | big explosion, knockback, breaks crates/barrels/shields |
| Heavy | W1 L13 | 3 | 35 | ×1.35 gravity, knocks shields down, cracks armor |
| Ice | W1 L15 | 2 | 18 | freeze: target's next draw 30 % slower, frost look |
| Poison | W1 L18 | 2 | 10 + 8 × 2 turns | green cloud stays for 2 turns |
| Laser | 1.1 | 1 | 30 | straight beam, ignores gravity and wind |
| Saw Blade | 1.1 | 2 | 15 × 2 | cuts through crates and keeps going |
| Drill | 1.2 | 2 | 28 | pierces shields and one crate |
| Homing | 1.2 | 1 | 20 | gently curves toward the nearest enemy |
| Black Hole | 1.3 | 1 | 15 | pulls nearby enemies and props together for 1 turn |
| Meteor | 1.3 | 1 | 45 | calls a meteor down where it lands |

Archer bonus: an archer deals **+20 % with its own element** (Fire Archer + Fire tip, Electric Archer + Electric
tip, Bomb Archer + Bomb tip), so choosing archer + tip together matters.
Arrow **trails** stay cosmetic (Classic, Sparkle, Rainbow, Comet), earned from chests and badges.

### 5.1 Match boosters (optional, before a match, coins only)
Picked on the Loadout screen, one of each per match at most; prices from EconomyConfig, earned coins only.
| Booster | Effect | Cost |
|---|---|---|
| Shield Bubble | a bubble absorbs the first arrow that hits you | 120 |
| Multi Arrow | your first shot fires 3 arrows | 100 |
| Iron Helmet | the first headshot on you counts as a body hit | 80 |
| Extra Heart | +25 max HP for this match | 150 |
Boosters are never required: every level is tested to be winnable without them.

---

## 6. Computer opponent (AI)

### 6.1 How it aims
1. `Logic.AimSolver` computes the exact (angle, power) that hits a chosen zone of the target, including gravity,
   wind and the arrow type (numeric search on the simulated flight, same code as real shots).
2. Adds **error** from the difficulty profile (angle σ, power σ).
3. **Learns** between turns: each miss shrinks its error by the learn rate ("it's adjusting its aim").
4. Picks a target zone: body (Easy/Medium) or head chance (Hard/Boss).
5. Waits a human-like **think time** with a visible aim animation, then shoots.

### 6.2 Difficulty profiles (config)
| Profile | Angle σ | Power σ | Learn / miss | Head aim | Ability use | Think time |
|---|---|---|---|---|---|---|
| Easy | 12.0° | 20 % | −15 % | 0 % | never | 1.0–1.4 s |
| Medium | 5.5° | 9.5 % | −35 % | 15 % | when charged, 50 % | 0.8–1.2 s |
| Hard | 2.8° | 5 % | −50 % | 60 % | when charged | 0.6–1.0 s |
| Boss | 1.8° | 3.5 % | −50 % | 70 % | always + boss moves | 0.6–0.9 s |

Angle/power σ were tuned (2026-09-25) so the §6.3 fairness tests land mid-range; the first values (6.0/3.5/1.6/1.2°,
10/6/3/2.5 %) made every profile too accurate (Easy 38 %, Medium 60 %, Hard 82 %, Boss 86 %).

### 6.3 Fairness targets (verified by EditMode tests, 1,000 simulated duels per profile)
- First-shot hit rate on a 20 m target in wind 2: Easy 15–30 %, Medium 30–50 %, Hard 55–75 %, Boss 65–85 %.
- A careful player should beat Easy in ≤ 8 turns and lose to Hard sometimes. Tests fail if rates drift.

### 6.4 Boss: "The Forest Warden" (World 1, level 20)
- 250 HP, stands on a raised stump fort with 2 wooden shields that rotate every turn.
- Moves: **Vine Wall** (grows a wall in front of the player every 3 turns), **Rain of Leaves** (3 arrows falling
  from above), **Enrage** at 30 % HP (shoots twice per turn).
- Weak spot: a glowing knot on its chest does ×2.5 when uncovered.

---

## 7. Modes (v1.0)

| Mode | What | Rewards |
|---|---|---|
| **Campaign** | World 1 "Whispering Forest": 20 levels (level 10 mini-boss, level 20 boss). Level goals vary (§7.1). | stars, coins, chests every 5 levels, unlocks |
| **Quick Duel** | You vs the computer: pick archer, AI difficulty, arena. | coins (Easy 10 / Medium 15 / Hard 25) |
| **2-Player (same phone)** | Pass-and-play: each player picks an archer; turns alternate; a "Pass the phone" screen hides the previous aim; best of 1 / 3 / 5; handicap slider (HP 70–130 %). | badge progress only (no coins, so it can't be farmed) |
| **Daily Challenge** | One seeded level per day (same for everyone, offline) with a special rule (e.g. strong wind, Split arrows only). | 50 coins; 7-day streak chest (300 coins + trail) |
| **Training Range** | Targets at 10/20/30/40 m, wind slider, long trajectory preview, no timer. | none (practice), badge "First Bullseye" |

### 7.1 Level goal types (campaign)
- **Duel**: knock out the opponent.
- **Targets**: hit N targets within M arrows (static, then swinging, then moving).
- **Apple Shot**: hit the apple on a friendly dummy's head without touching the dummy.
- **Gauntlet**: defeat 2–3 enemy archers one after another (HP carries over).
- **Rescue**: shoot the rope to free a captured friend, while an enemy shoots at you.
- **Trick Shot**: the target is behind cover; use a bounce pad / break a crate.
- **Boss**: special rules (§6.4).

### 7.2 Stars
- Duel / Gauntlet / Boss: ★ win · ★★ win with ≥ 50 % HP · ★★★ win within the level's **par** turns.
- Targets / Apple / Trick / Rescue: ★ complete · ★★ within par + 2 arrows · ★★★ within par arrows.

---

## 8. Economy (coins only, never bought)

| Source | Coins |
|---|---|
| Campaign first clear | Easy 20 · Medium 30 · Hard 45 · mini-boss 80 · boss 150 |
| Replay clear | 50 % of first clear |
| Each star (first time) | +10 |
| Headshot | +3 each (max 15 per match) |
| Quick Duel win | Easy 10 · Medium 15 · Hard 25 (loss: 3) |
| Daily Challenge | 50; 7-day streak chest 300 |
| Chest (every 5 campaign levels) | 80–140 coins (seeded) + a cosmetic chance (seeded, fixed per chest) |
| Badges | 30 bronze · 60 silver · 120 gold |

| Spend | Cost |
|---|---|
| Archer upgrade lvl 2 → 10 | 100 · 150 · 220 · 300 · 400 · 520 · 660 · 820 · 1000 |
| Skins | 300–600 (coins only; some only from badges) |
| Trails | 200–400 |

Rules: coin balance can never go negative; every reward and cost comes from `EconomyConfig`; UI shows the config
values. No energy, no timers on playing, no "watch to double".

---

## 9. Badges (v1.0: 22, each with bronze / silver / gold)

| Badge | Bronze / Silver / Gold |
|---|---|
| Sharpshooter — headshots | 10 / 50 / 200 |
| Bullseye — target-level hits | 25 / 100 / 400 |
| Untouchable — win without taking damage | 1 / 5 / 20 |
| Wind Whisperer — hits in wind ≥ 4 | 10 / 40 / 150 |
| Long Shot — hits from ≥ 35 m | 5 / 25 / 100 |
| Firestarter — burn damage dealt | 100 / 500 / 2000 |
| Chain Reaction — electric chains | 10 / 50 / 200 |
| Demolition — crates/barrels destroyed | 10 / 50 / 200 |
| Triple Threat — Triple Shot hits | 10 / 40 / 150 |
| Forest Hero — World 1 stars | 20 / 40 / 60 |
| Warden Slayer — beat the boss | 1 / 3 (on Hard replay) / 3★ |
| Quick Draw — shots released in < 3 s | 20 / 100 / 400 |
| Comeback — win with < 15 % HP | 1 / 5 / 20 |
| Duelist — 2-Player matches played | 3 / 15 / 50 |
| Friendly Rivals — 2-Player best-of-3 finished | 1 / 5 / 20 |
| Daily Devotee — daily streak | 3 / 7 / 30 |
| Collector — archers owned | 2 / 3 / 4 |
| Upgrader — total archer levels | 10 / 25 / 40 |
| Apple Picker — apples hit | 3 / 10 / 30 |
| Coin Keeper — coins earned total | 1,000 / 5,000 / 20,000 |
| Thunderstruck — lightning strikes landed | 10 / 50 / 200 |
| Tower Toppler — crate towers knocked down | 3 / 15 / 60 |

Badge wall (Badges screen) + pin up to 3 on the Home profile card. Unlock toast + sound + haptic in the match.

---

## 10. Arena props (v1.0)

| Prop | Behaviour | First seen |
|---|---|---|
| Wooden wall | blocks arrows (arrows stick) | L3 |
| Crate | breaks after 2 hits or 1 explosive | L6 |
| Moving platform | archer stands on it; moves between turns | L9 |
| Bounce pad | arrows bounce off at the mirror angle, keep 80 % speed | L12 |
| Swinging target | pendulum target | L8 |
| Explosive barrel | 30 damage in 2 m when hit | L15 |
| Shield (wooden) | carried by enemy; absorbs; Heavy knocks it down | L11 |
| Rope | cut by any hit (Rescue levels) | L14 |
| Crate tower | tall stack of crates between the islands; hits knock crates off, bombs topple it | L6 |
| TNT crate | inside a tower; any hit makes it explode (2.5 m, 35 damage) and blows the tower apart | L10 |
| Floating island | arenas are islands over a void; knockback near an edge makes the target stumble (no fall-death in v1) | L1 |

---

## 11. Audio and haptics

- **Music**: Home theme, World 1 theme, boss theme, 2-Player theme; victory and defeat stingers; crossfade 0.6 s.
- **SFX** (2–3 variations each, ±4 % pitch): bow draw (rises with power), creak loop, release twang, whoosh
  (by speed), impact wood / stone / metal / body / shield, headshot ding, crate break, barrel boom, bounce pad
  boing, burn crackle, electric zap, bomb blast, triple-shot fan, meteor roar, storm crack, cluster pops, AI
  "hmm" aim tick, turn start chime, timer ticks, knockout poof, crowd cheer (victory), coin, star pop (3 pitches),
  chest shake + open, badge unlock, UI tap / confirm / back / toggle.
- **Haptics**: light tick per 10 % power, medium on release, heavy on hit, double on headshot, success on win,
  warning on timer's last 3 s. Setting to turn off.
- Placeholder sounds generated by `Tools/gen_sounds.py` for every id; real sounds override by id.

---

## 12. UI and art direction

- **Look**: bright cartoon, chunky 3D-looking buttons with a pressed pose, rounded glass cards, big readable
  numbers. Display font rounded (e.g. Fredoka), body Nunito. **Light theme** candy colors (confirmed by Naveen);
  **dark theme** deep purple + gold accent as in the Claude Design prototype (DESIGN_TOKENS §2).
- World 1 art: parallax forest (sky + 3 layers + floating far islands), sun rays, drifting leaves, wind streaks,
  fireflies at dusk and night levels, stars and moon at night.
- Characters: stylized, big heads (readable headshots), thick outlines, idle breathing, squash/stretch.
  **2.5D** (confirmed by Naveen): the Design roster's parts rendered with baked 3D volume shading ("vinyl toy":
  highlight, rim light, ambient occlusion), depth-tinted back limbs and a soft ground shadow, assembled every
  frame by the roster's own rig (same bone names and pivots) — see PROGRESS Decisions log. The game is played on a
  2D side plane (Logic is 2D). Final art replaces parts by id.
- Motion: UI pops 150–300 ms easeOutBack; screen transitions 250 ms; coin fly 600 ms; everything with unscaled time.
- Match HUD (our own design, not copied): health bar + portrait for both sides, **ability/power bar**
  (charges over turns), wind gauge, turn timer, **arrow-tip bar** at the bottom (owned tips with ammo, locked tips
  show the unlock level), boosters active icons, pause. Top-right: coins; session counters (hits, headshots).
- Juice list: hit-stop, shake, slow-mo KO, damage numbers, combo text ("DOUBLE HIT!"), confetti, coin fly,
  star pops, trail particles, landing marker for your last shot (ghost arc), wind particles.

---

## 13. Accessibility and comfort

- Left-handed layout; bigger touch targets; aim sensitivity slider; trajectory-assist toggle (Easy only in campaign,
  any in Training / 2-Player); color-blind safe element icons; reduce-motion (less shake, no slow-mo);
  text in English + Hinglish (MindTap's approach); pause anywhere; Android Back everywhere.

---

## 14. Save data (v1)

`version`, coins, owned archers + levels + skins, equipped archer/skin/trail, loadout, per-level stars + best
turns, badges (progress + tier + pinned), daily streak + last day, chests opened, settings (audio, haptics, theme,
language, left-handed, assist, sensitivity, battery saver, reduce motion), stats (shots, hits, headshots, longest
shot, wins/losses per mode, time played), tutorial flags, "New!" intro flags.

---

## 15. Tech notes that protect the future

- Match = pure state machine in Logic: `MatchState` + `ApplyShot(ShotInput)` + `Tick(dt)`. Runtime only renders.
- Online play (2.0) will send `ShotInput`s between phones; nothing in Logic may depend on frame rate or
  `UnityEngine`.
- Every level is data (JSON/SO) and validated by tests (the AI solver can reach every required target).
