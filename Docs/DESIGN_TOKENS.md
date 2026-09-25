# Archer Arcade — Design Tokens

Status: **structure only.** Values are filled from the approved Claude Design prototype (Phase 0). Until then,
code must not invent values — ask. Every size is dp at the 844 × 390 landscape reference.

## 1. Colors — light theme
| Token | Value | Use |
|---|---|---|
| ink, ink-muted, surface, sunken, edge | TBD | text, cards, wells, borders |
| primary / primary-top / primary-edge | TBD | main 3D buttons (Play) |
| secondary / success / danger / neutral | TBD | other button styles |
| hp-player / hp-enemy / hp-back | TBD | HP bars |
| coin, gold, star | TBD | rewards |

## 2. Colors — dark theme
Same tokens as §1 (if "mix" is chosen: NaveenCodes dark surfaces + gold accent).

## 3. Element colors (VFX + chips, always paired with an icon)
| Element | Main | Glow | Icon |
|---|---|---|---|
| Normal | TBD | TBD | TBD |
| Fire | TBD | TBD | TBD |
| Electric | TBD | TBD | TBD |
| Bomb | TBD | TBD | TBD |
| Ice | TBD | TBD | TBD |
| Poison | TBD | TBD | TBD |
| Split / Heavy | TBD | TBD | TBD |

## 4. Typography
Display (rounded, e.g. Fredoka) and body (e.g. Nunito): sizes for title, heading, body, caption, HUD numbers,
damage numbers, "HEADSHOT!" text. TBD.

## 5. Shape and depth
Corner radii (card, button, pill, modal), 3D button edge depth + pressed offset, shadows (card, modal, HUD). TBD.

## 6. Layout
Safe-area margins, HUD positions (HP bars, wind gauge, timer, ability button, arrow picker, pause) for right- and
left-handed layouts, minimum touch target. TBD.

## 7. Motion
| Token | Value |
|---|---|
| ui-pop | TBD (≈ 150–300 ms, easeOutBack) |
| screen-transition | TBD (≈ 250 ms) |
| coin-fly | TBD (≈ 600 ms) |
| hit-stop head / body / legs | 70 / 45 / 30 ms (GAME_DESIGN §3.3) |
| shake head / body | 8 / 5 dp |
| knockout slow-mo | 0.25× for 0.8 s real time |
| camera follow / return | TBD |

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
