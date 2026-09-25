# Archer Arcade — Start Here (handoff for a new chat)

Written 2026-09-25 at the end of the planning chat. Read this first, then the files it points to.

## What we are building
Archer Arcade: a **landscape** Android archery duel game in **Unity 6000.3 LTS** (URP, C#). Pull back to aim,
arrows fly in an arc with gravity and wind, headshots, elemental arrow tips with big impact effects (lightning
from the sky, fireballs, bomb blasts, ice, poison), opponents that look more hurt as HP drops (cartoon, no
blood), crate towers + TNT on floating islands. Play vs the computer (campaign + quick duel) or **2 players on
one phone** (pass-and-play). Premium UI like MindTap. **Free forever** — no ads, no purchases, coins only earned.
"In loving memory of Maa ❤️" in Settings and Credits.

## Source of truth (already written — do not contradict)
| File | What |
|---|---|
| `CLAUDE.md` | rules, tech, folder structure, conventions, commands |
| `Docs/GAME_DESIGN.md` | every rule and number (shot, impact effects, injury looks, archers, enemies, 8 arrow tips, boosters, AI, modes, economy, 22 badges, audio, UI, save data) |
| `Docs/LEVELS.md` | World 1 levels 1–20 in full (goal, enemy, distance, wind, props, par, rewards, tip unlocks), daily challenge, worlds 2–5 outline |
| `Docs/PROGRESS.md` | the checklist, Phases 0–15 + open questions + decisions log — work through it in order |
| `Docs/ROADMAP.md` | v1.0 scope; 1.1 / 1.2 / 1.3 / 2.0 (online) later |
| `Docs/SCREEN_INVENTORY.md` | 34 screens/states |
| `Docs/DESIGN_TOKENS.md` | structure + sound/haptic ids; colors/fonts/sizes come from the Claude Design prototype |

## Decisions already made
- Landscape only (confirmed by Naveen), reference 844 × 390 dp. Package `com.naveencodes.archerarcade`.
- v1.0 scope is deliberately small: 1 world (Whispering Forest, 20 levels, mini-boss L10, boss Forest Warden L20),
  4 archers (Ranger, Fire, Electric, Bomb), 8 arrow tips, 7 enemy types, AI Easy/Medium/Hard/Boss, same-phone
  2-player, daily challenge, training, boosters (coins only, never required), chests, 22 badges.
- Multiplayer v1 = same phone. Online only in 2.0. No INTERNET permission until then. Match logic is pure C#,
  deterministic and input-driven (shot = angle, power, tip, ability) so online can be added later.
- A reference stickman archery game was shown ONLY to explain features. Never copy its UI, layout or art.
- Characters come from the Claude Design roster (SVG parts + pivots), exported to shaded 2.5D rig sprites by
  `Tools/art` and animated by `Archers/RigSolver` (the design's own rig maths).

## Current status
- See `Docs/PROGRESS.md` for the live checklist. All 15 phases are code complete: Logic (tested with
  `dotnet test Tools/LogicTests/EditMode`, 248 tests), Runtime (menus, match, 2.5D archers, arena, VFX, audio),
  Editor builders and PlayMode tests — all compiled against Unity 6000.3.24f1's DLLs with
  `dotnet build Tools/UnityCheck/Game.Runtime` / `Game.RuntimeEditor` / `Game.Editor` / `Game.PlayModeTests`.
- The Unity project has not been opened yet: running in Unity, the device checks and the APK wait for Naveen's PC.
- Generated content (re-run instead of hand-editing): art `node Tools/art/export_art.mjs` +
  `python Tools/art/build_art.py`, fonts `python Tools/build_fonts.py`, audio `python Tools/gen_sounds.py`,
  store art `python Tools/art/make_store_art.py`.

## First open on Naveen's PC (once)
1. Unity Hub ▸ Add ▸ this folder, editor 6000.3.24f1. Unity reads `Packages/manifest.json` (URP, uGUI/TMP,
   Input System, Test Framework) and creates ProjectSettings.
2. If Unity asks to enable the new Input System backend and restart: Yes.
3. Menu **ArcherArcade ▸ Build ▸ All** (project settings, GameConfig asset, import rules, scenes). Run it again once
   after the restart so the URP asset and TMP essentials are in place.
4. Open `Assets/ArcherArcade/Scenes/Boot.unity` ▸ Play. Game view 1688 × 780 (844 × 390 × 2) to match the design.
5. Tests: Window ▸ General ▸ Test Runner ▸ EditMode + PlayMode ▸ Run All.
6. Design check: **ArcherArcade ▸ Capture ▸ Screens + Match** → `Builds/Capture/`.
7. APK: **ArcherArcade ▸ Build ▸ Android APK (dev)** → `Builds/Android/ArcherArcade.apk` (AAB only when asked).

## Open questions — answered 2026-09-25
1. Style: light theme = candy colors (dark = prototype purple + gold).
2. Art: **2.5D** (3D toon characters, 2D gameplay plane).
3. Placeholder art/music now, replaced later by id.
4. Name "Archer Arcade" + package `com.naveencodes.archerarcade`: final.

## Reuse from MindTap (C:\Users\navee\Desktop\NaveenCodes\apps\Arrow)
Same Unity install `E:\Unity\Editors\6000.3.24f1` (C: is nearly full — keep caches on E:), adb in
`...\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe` (Git Bash needs `MSYS_NO_PATHCONV=1`),
batch build/test pattern (`Batch.FailOnCompileErrors`, `-runTests`), tween utility, SafeArea, UIManager router +
back stack + pause, SaveSystem (.tmp/.bak), AudioManager + `Tools/gen_sounds.py`, HapticsManager, DisplayRate
(90/120 Hz + battery saver + thermal fallback), Hinglish strings table, `Features.cs` release switches.
Lessons learned there:
- Never leave `Time.timeScale = 0` without a pausing screen (MindTap's blank-board bug); heal it in UIManager.
- PlayMode tests must wait in real time (`WaitForSecondsRealtime`) or they hang when a popup pauses.
- Modals/sheets must re-apply the current light/dark theme when opened.
- Big boards: cap intro stagger so the board never looks empty.
- Build AABs only when Naveen asks; day to day build the APK and install on his phone (Nothing A059, 120 Hz) when told.

## How Naveen works
- Speaks Hinglish; reply in simple Hinglish. Keep him updated in short lines; suggest the next step at the end.
- "install kar" = install the APK on his phone; "aab bna" = build release AABs.
- Never suggest ads/monetization. Always keep the Maa tribute.
