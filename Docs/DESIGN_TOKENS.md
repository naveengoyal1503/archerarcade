# Archer Arcade — Design Tokens

Status: **filled from the Claude Design prototype** in `Design/Archer Arcade.dc.html` (exported 2026-09-25).
Every size is dp at the 844 × 390 landscape reference. Values marked *(derived)* are not in the prototype; they
were derived from it and approved by Naveen on 2026-09-25. Code must not invent other values.

## 1. Colors — light theme (`Component.LIGHT` in the prototype)
| Token | Value | Use |
|---|---|---|
| bg (top) / bg2 (bottom) | `#BFE6FF` → `#FFF0DA` | sky gradient behind every screen |
| card | `rgba(255,255,255,.72)` | glass cards, pills |
| solid | `#FFFFFF` | solid cards, icon buttons |
| line (edge) | `rgba(255,255,255,.95)` | card border 1.5 dp |
| ink / ink-muted | `#2A2350` / `#5F5985` | text |
| track (sunken) | `rgba(42,35,80,.12)` | wells, bar tracks, chips |
| sh (shadow) | `rgba(42,35,80,.18)` | card shadow |
| hill | `#8BD346` | Home stage hill |

## 2. Colors — dark theme (`Component.DARK`)
| Token | Value |
|---|---|
| bg / bg2 | `#1E1A45` → `#3A2766` |
| card | `rgba(255,255,255,.08)` |
| solid | `#2E2860` |
| line | `rgba(255,255,255,.14)` |
| ink / ink-muted | `#FFFFFF` / `#C4BEEA` |
| track | `rgba(255,255,255,.14)` |
| sh | `rgba(0,0,0,.35)` |
| hill | `#2F7A4C` |

Page/backdrop behind the phone frame: `#15122E`. Dark theme = deep purple + gold accent (`#FFD23F`), light =
candy sky (light = candy confirmed by Naveen 2026-09-25).

## 2.1 3D button styles (face / edge; theme-independent)
| Style | Face | Edge (shadow) | Text | Used for |
|---|---|---|---|---|
| gold (CTA) | `#FFD23F` | `#D4A514` | `#2A2350` | START, Next level, Play today's level |
| play (hero) | `#F0641E` | `#B8460F` | `#FFFFFF` | Home PLAY card, current map node |
| primary | `#6D4AFF` | `#4A2BD1` | `#FFFFFF` | back button, Archers ▸, secondary actions |
| success | `#12A67A` | `#0B7555` | `#FFFFFF` | claim, owned, World 1 color |
| info | `#1C9AD6` | `#13709E` | `#FFFFFF` | Daily Challenge |
| danger | `#E5484D` | `#B02D32` | `#FFFFFF` | quit, destructive |
| pvp | `#E83E8C` | `#B02467` | `#FFFFFF` | 2-Player |
| training | `#8E7A5B` | `#65563F` | `#FFFFFF` | Training Range |
| locked | `#A9A4C9` | `#7D78A3` | `#FFFFFF` | locked map nodes (boss locked `#8B84B8`) |

## 2.2 Game colors
| Token | Value | Note |
|---|---|---|
| hp-player / hp-enemy | `#12A67A` / `#E5484D` | color-blind mode: `#1C9AD6` / `#F59E0B` |
| hp-back | = track | |
| timer / timer-warn | ink / `#E5484D` | warn at ≤ 3 s |
| coin / gold / star | `#FFD23F` (edge `#D4A514`) | |
| tier gold / silver / bronze | `#FFC928` / `#C9D3E3` / `#E09A5F` | badges |
| headshot text | `#FFD23F`, 26 dp | |
| confetti | `#FFD23F #FF5FA2 #38BDF8 #2ED3A0 #FFFFFF #FF8A3D` | |
| splash | radial `#8E6BFF` → `#5536D6`, title shadow `#3A1FB0` | |

## 3. Element colors (VFX + chips, always paired with an icon)
Archer colors come from the prototype; tip-only elements (Ice, Poison, Split/Heavy) reuse the prototype's archer
swatches for the same element. Glow = *(derived)* lighter tint.
| Element | Main | Glow *(derived)* | Icon |
|---|---|---|---|
| Normal (Ranger) | `#2ED3A0` (arrow chip: white) | `#FFFFFF` | ➵ / 🏹 |
| Fire | `#FF8A3D` | `#FFD2A6` | 🔥 |
| Electric | `#FFD23F` | `#FFF3C4` | ⚡ |
| Bomb | `#FF5A6E` | `#FFB800` | 💣 |
| Ice | `#7FD6F7` | `#E6F6FF` | ❄️ |
| Poison | `#9BE15D` | `#C6FF4A` | 🧪 |
| Split / Heavy | `#9D86FF` / `#8E7A5B` | `#FFFFFF` | 🔱 / 🪨 |

Emoji are prototype stand-ins; the game uses an icon-font subset (`Icons.cs`) with the same meanings.

## 4. Typography
Display **Fredoka** (500/600/700), body **Nunito** (600/700/800/900). Both OFL.
| Role | Font | Size (dp) |
|---|---|---|
| Logo (splash) | Fredoka 700 | 60 |
| PLAY hero | Fredoka 700 | 54 |
| Screen title | Fredoka 700 | 25 |
| Big CTA | Fredoka 700 | 24–26 |
| Heading / card title | Fredoka 700 | 16–22 |
| Pill numbers (coins, stars) | Fredoka 700 | 17 |
| Body | Nunito 800 | 13–14 |
| Caption / small label | Nunito 800 | 11–12 |
| Micro label | Nunito 800 | 9.5–10.5 |
| HUD timer / numbers | Fredoka 700 | 17–22 |
| Damage numbers | Fredoka 700 | 20 (crit 26) |
| HEADSHOT! | Fredoka 700 | 26 |

## 5. Shape and depth
| Token | Value |
|---|---|
| radius: small chip / bar | 5–7 |
| radius: button (icon 44×44, normal) | 14 |
| radius: big CTA | 18 |
| radius: card | 20–22 |
| radius: hero card (PLAY) | 28 |
| radius: pill (h 36) | 18 |
| radius: logo tile (96) | 30 |
| button edge depth (normal / CTA / hero) | 4 / 5 / 7 |
| pressed offset (normal / CTA / hero) | 3 / 4 / 5 (edge shrinks to 1–2) |
| card border | 1.5 |
| hero glow | `0 14 24 rgba(edge,.3)` |
| phone frame shadow | `0 30 80 rgba(0,0,0,.5)` (prototype only) |

## 6. Layout
| Token | Value |
|---|---|
| screen side padding | 18 |
| header height | 58 (back 44×44, title, coin pill right) |
| home top bar | top 14, height 48 |
| min touch target | 44 (bigger-targets setting: *(derived)* 56) |
| match ground line | y 316 of 390; archers at x 120 / 724 (right-handed) |
| left-handed | HUD buttons mirrored (see Design state 06) |

## 7. Motion
| Token | Value |
|---|---|
| ui-pop | 300 ms, overshoot 1.15 (`aaPop`) ≈ easeOutBack |
| screen-transition | 250 ms fade + scale .97 → 1 (`aaIn`) |
| coin-fly | 600 ms (GAME_DESIGN §12) |
| idle breathing | 2.4 s loop, −3 dp, scaleY 1.02 (`aaBreath`) |
| float (logo) | 2.4 s, −6 dp |
| pulse (play arrow) | 1.8 s, scale 1.07 |
| splash load bar | 1.6 s easeOut; splash → Home after 1.9 s |
| toast | 1.9 s |
| hit-stop head / body / legs | 70 / 45 / 30 ms (GAME_DESIGN §3.3) |
| shake head / body | 8 / 5 dp |
| knockout slow-mo | 0.25× for 0.8 s real time |
| camera follow / return | *(derived)* 0.35 s smooth / 0.5 s easeInOut |

## 8. Sound ids
`sfx_bow_draw`, `sfx_bow_creak_loop`, `sfx_release`, `sfx_whoosh`, `sfx_hit_wood`, `sfx_hit_stone`, `sfx_hit_metal`,
`sfx_hit_body`, `sfx_hit_shield`, `sfx_headshot`, `sfx_crate_break`, `sfx_barrel_boom`, `sfx_bounce`, `sfx_burn`,
`sfx_zap`, `sfx_lightning_strike`, `sfx_thunder`, `sfx_fire_burst`, `sfx_on_fire_loop`, `sfx_freeze`, `sfx_ice_shatter`,
`sfx_poison_cloud`, `sfx_bubble_pop`, `sfx_bubble_cast`, `sfx_tnt_boom`, `sfx_tower_topple`, `sfx_heal`, `sfx_bomb`, `sfx_triple_fan`, `sfx_meteor`, `sfx_storm`, `sfx_cluster`, `sfx_ai_aim`, `sfx_turn_start`,
`sfx_timer_tick`, `sfx_knockout`, `sfx_cheer`, `sfx_coin`, `sfx_star_1..3`, `sfx_chest_shake`, `sfx_chest_open`,
`sfx_badge`, `sfx_ui_tap`, `sfx_ui_confirm`, `sfx_ui_back`, `sfx_toggle_on`, `sfx_toggle_off`;
music `mus_home`, `mus_world1`, `mus_boss`, `mus_duel`, `sting_victory`, `sting_defeat`.

## 9. Haptic ids
`hap_power_tick`, `hap_release`, `hap_hit`, `hap_headshot`, `hap_win`, `hap_timer_warn`, `hap_ui_tap`.
