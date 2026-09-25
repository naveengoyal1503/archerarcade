/*
 * Archer Arcade — character export extension. Runs in the browser page after Design/aa-characters.js (patched by
 * export_art.mjs to expose window.AAInternals). It adds, in the same drawing kit and outline style as the design:
 *   - the generic World 1 enemy types that have no design sheet yet (Bramble Bandit, Crossbow Scout, Shield Bearer,
 *     Healer Druid, Tower Sniper),
 *   - the earned hero skins from CosmeticCatalog (Forest Cloak, Ember Coat, Neon Bolt, Blast Goggles) as palette swaps
 *     (+ goggles gear),
 *   - one arrow per arrow tip, props and effect sprites,
 * and window.AAExport.looks() / parts(look) used by the exporter.
 * Everything here is original art made for Archer Arcade.
 */
(function () {
  const A = window.AAInternals;
  const { OL, P, Pi, ln, tube, G, el, rr, rect, leaf, flame, dims, partsOf, POSES, GEAR, headP } = A;
  const SW = () => A.getSW();

  /* ---------- extra gear ---------- */
  GEAR.bandit = () => ({
    svg: P('M-52 -84C-74 -92 -90 -80 -96 -62C-82 -70 -70 -72 -60 -70Z', '#6A3279', { w: .8 }) +
      P('M-54 -78C-70 -66 -76 -50 -72 -36C-64 -50 -58 -60 -50 -68Z', '#5A2A68', { w: .8 }) +
      P('M62 -84C60 -120 22 -132 -10 -128C-44 -124 -62 -100 -58 -70C-48 -86 -28 -96 -2 -98C24 -100 46 -96 62 -84Z', '#7B3F8C',
        { sh: rect(-80, -140, 44, 90), hi: el(12, -118, 16, 5) }) +
      P(el(18, -112, 4.5), '#EDE6F2', { w: .5 }) + P(el(-14, -114, 4.5), '#EDE6F2', { w: .5 }) + P(el(40, -100, 4), '#EDE6F2', { w: .5 }) +
      P(el(-38, -98, 4), '#EDE6F2', { w: .5 }),
    box: [-100, -136, 168, 108]
  });
  GEAR.crossbow_scout = () => ({
    svg: P('M-30 -112C-36 -136 -50 -150 -66 -154C-58 -136 -48 -122 -36 -108Z', '#FF8A3D', { inner: ln('M-34 -112C-42 -128 -52 -140 -62 -150', SW() * .4, '#C9602A') }) +
      P('M64 -80C66 -124 -54 -128 -56 -80Z', '#3E7C8C', { sh: el(-50, -70, 50, 60), hi: el(20, -112, 16, 5) }) +
      P(rr(-62, -88, 150, 12, 6), '#2E5E6C') +
      P(el(34, -96, 12), '#C9D2DC', { w: .8 }) + P(el(34, -96, 7), '#7FD6F7', { w: .6, hi: el(37, -99, 2.5) }),
    box: [-70, -158, 162, 96]
  });
  GEAR.shield_bearer = () => ({
    svg: P('M64 -84C66 -134 -58 -138 -58 -84Z', '#A7B0BF', { sh: el(-52, -74, 52, 64), hi: el(18, -120, 18, 6) }) +
      P(rr(-64, -92, 136, 14, 7), '#7C8596') +
      P(rr(54, -92, 10, 34, 5), '#7C8596') +
      P(el(-20, -118, 4), '#5A6272', { w: .5 }) + P(el(4, -126, 4), '#5A6272', { w: .5 }) + P(el(28, -120, 4), '#5A6272', { w: .5 }),
    box: [-68, -140, 144, 94]
  });
  GEAR.healer_druid = () => ({
    svg: P('M-4 -30C-10 6 14 34 38 30C54 26 66 10 64 -12C54 -2 40 -6 30 -2C20 -8 8 -18 -4 -30Z', '#F4F1EA', { hi: el(40, 18, 6, 3) }) +
      P('M58 -86C54 -120 -30 -128 -52 -94C-60 -80 -58 -60 -50 -46C-40 -66 -22 -80 0 -86C22 -92 42 -90 58 -86Z', '#E9E4DA', { sh: rect(-70, -130, 36, 90) }) +
      leaf(-40, -104, -150, 26, '#5E9A32') + leaf(-18, -118, -120, 26, '#8CC04A') + leaf(8, -122, -90, 26, '#5E9A32') +
      leaf(32, -116, -60, 24, '#8CC04A') + leaf(52, -102, -30, 22, '#5E9A32') +
      P(el(8, -122, 6), '#FF5FA2', { w: .6, hi: el(10, -124, 2) }),
    box: [-80, -152, 150, 190]
  });
  GEAR.tower_sniper = () => ({
    svg: P('M-40 -104C-60 -112 -70 -130 -64 -150C-50 -136 -30 -130 -10 -130Z', '#FFD23F', { w: .8 }) +
      P('M70 -92C60 -106 30 -112 4 -112C-24 -112 -52 -104 -66 -90C-50 -96 -26 -100 4 -100C30 -100 54 -98 70 -92Z', '#1F2A48') +
      P('M44 -100C44 -140 -38 -144 -40 -100Z', '#2E3A5E', { sh: el(-40, -94, 40, 50), hi: el(12, -130, 12, 4) }) +
      P(rr(-42, -110, 88, 10, 5), '#3FB6C9'),
    box: [-72, -154, 148, 70]
  });
  const bombGear = GEAR.bomb;
  GEAR.bomb_goggles = () => {
    const base = bombGear();
    return {
      svg: base.svg + P(rr(-8, -112, 72, 22, 11), '#3A3140') + P(el(16, -101, 12), '#C9D2DC', { w: .8 }) + P(el(16, -101, 7.5), '#7FD6F7', { w: .6, hi: el(19, -104, 2.5) }) +
        P(el(46, -101, 12), '#C9D2DC', { w: .8 }) + P(el(46, -101, 7.5), '#7FD6F7', { w: .6, hi: el(49, -104, 2.5) }),
      box: [-72, -150, 150, 70]
    };
  };

  /* ---------- extra characters (same schema as the design roster) ---------- */
  const BANDIT = {
    key: 'bandit', name: 'Bramble Bandit', role: 'World 1 · Enemy', s: 1, skin: '#E8B48C', hair: '#3B2440', gearName: 'bandana',
    arm: { ua: '#C9A77A', fa: '#C9A77A', faO: { band: '#6A3279', bandLen: 10 }, hand: '#E8B48C' },
    leg: { ul: '#4A3A5E', ll: '#4A3A5E', llO: { bandTop: '#6A3279', bandTopLen: 6 }, foot: '#3B2440', sole: '#1E1224' },
    torso: {
      c: '#C9A77A', detail: D => {
        const a = D.TW / 2, t = -D.TH;
        return Pi(`M${-a - 6} ${t - 6}H${a + 6}V-30H${-a - 6}Z`, '#6A3279') + Pi(rect(-a - 6, -30, D.TW + 12, 12), '#3B2440') +
          Pi(rr(a - 22, -33, 13, 18, 4), '#C9CED6') + ln(`M${-a * .55} ${t + 8}L${-a * .55} -34`, SW() * .4, '#3B2440', .6);
      }
    },
    cape: { c: '#5A2A68', shape: 'thorn' }, quiver: { c: '#3B2440', band: '#C9CED6', f: ['#7B3F8C', '#C9A77A', '#7B3F8C'] },
    bow: { wood: '#5A3A22', grip: '#3B2440', string: '#EDE6F2', style: 'long' },
    face: { mouth: 'grin', under: P('M14 -72Q40 -80 72 -70L70 -50Q40 -58 16 -52Z', '#3B2440', { w: .6 }) + P(el(37, -61, 11, 12.5), '#FFFFFF', { w: .45 }) },
    sw: ['#7B3F8C', '#C9A77A', '#3B2440', '#4A3A5E', '#E8B48C']
  };
  const SCOUT = {
    key: 'crossbow_scout', name: 'Crossbow Scout', role: 'World 1 · Enemy', s: .95, skin: '#F1C39A', hair: '#6B4226', gearName: 'cap',
    b: { bw: .92, lw: .92, lh: .95 },
    arm: { ua: '#3E7C8C', fa: '#E8D8B0', faO: { band: '#6B4A2B', bandLen: 12 }, hand: '#F1C39A' },
    leg: { ul: '#6B4A2B', ll: '#3E7C8C', foot: '#4A3222', sole: '#2A1C14' },
    torso: {
      c: '#E8D8B0', detail: D => {
        const a = D.TW / 2, t = -D.TH;
        return Pi(`M${-a - 6} ${t - 6}H${a + 6}V${t + 22}L0 ${t + 40}L${-a - 6} ${t + 22}Z`, '#3E7C8C') + Pi(rect(-a - 6, -28, D.TW + 12, 12), '#6B4A2B') +
          Pi(rr(a - 22, -31, 13, 18, 4), '#FF8A3D') + ln(`M${a * .5} ${t + 6}L${-a * .7} -30`, SW() * .9, '#6B4A2B');
      }
    },
    cape: null, quiver: { c: '#6B4A2B', band: '#3E7C8C', f: ['#FF8A3D', '#E8D8B0', '#FF8A3D'] },
    bow: { wood: '#6B4A2B', grip: '#2E5E6C', string: '#F7F1E1', style: 'crossbow', metal: '#9AA8BC' },
    face: { mouth: 'smile', eye: 'dot' },
    sw: ['#3E7C8C', '#E8D8B0', '#6B4A2B', '#FF8A3D', '#F1C39A']
  };
  const BEARER = {
    key: 'shield_bearer', name: 'Shield Bearer', role: 'World 1 · Enemy', s: 1, skin: '#C98E64', hair: '#3A2014', gearName: 'helmet',
    b: { bw: 1.3, lw: 1.15, lh: .95 },
    arm: { ua: '#8A94A6', uaO: { bandTop: '#6B4A2B', bandTopLen: 12 }, fa: '#6B4A2B', faO: { band: '#8A94A6', bandLen: 10 }, hand: '#C98E64' },
    leg: { ul: '#6B4A2B', ll: '#4A3222', llO: { bandTop: '#8A94A6', bandTopLen: 8 }, foot: '#3A2618', sole: '#1E140C' },
    torso: {
      c: '#8A94A6', detail: D => {
        const a = D.TW / 2, t = -D.TH;
        let rings = '';
        for (let y = t + 12; y < -34; y += 14) rings += ln(`M${-a + 2} ${y}Q0 ${y + 6} ${a - 2} ${y}`, SW() * .35, '#5A6272', .7);
        return rings + Pi(`M${-a * .3} ${t - 6}H${a * .3}V-34H${-a * .3}Z`, '#B3262E') + Pi(rect(-a - 6, -34, D.TW + 12, 14), '#4A3222') +
          Pi(rr(-10, -37, 20, 20, 5), '#F2C14E');
      }
    },
    cape: { c: '#6E2A2E', shape: 'coat' }, quiver: { c: '#4A3222', band: '#8A94A6', f: ['#B3262E', '#8A94A6', '#B3262E'] },
    bow: { wood: '#4A3222', grip: '#8A94A6', string: '#F7F1E1', style: 'heavy', band: '#8A94A6' },
    face: { mouth: 'grin', browW: 1.4, over: P('M36 -40C28 -44 18 -40 16 -32C24 -34 32 -32 38 -30C44 -32 52 -34 58 -32C54 -40 44 -44 36 -40Z', '#3A2014') },
    sw: ['#8A94A6', '#6B4A2B', '#B3262E', '#4A3222', '#C98E64']
  };
  const DRUID = {
    key: 'healer_druid', name: 'Healer Druid', role: 'World 1 · Enemy', s: 1, skin: '#D9A77E', hair: '#E9E4DA', gearName: 'leaf_crown',
    b: { bw: 1.05, lw: 1, lh: 1.05 },
    arm: { ua: '#4E8A2A', fa: '#4E8A2A', faO: { band: '#E9E4DA', bandLen: 8 }, hand: '#D9A77E' },
    leg: { ul: '#3E6A26', ll: '#3E6A26', foot: '#6B4226', sole: '#3A2618' },
    torso: {
      c: '#5E9A32', detail: D => {
        const a = D.TW / 2, t = -D.TH;
        return Pi(`M${-a * .25} ${t - 6}H${a * .25}V20H${-a * .25}Z`, '#E9E4DA') + Pi(rect(-a - 6, -30, D.TW + 12, 11), '#6B4226') +
          Pi(el(0, -24, 8), '#C6FF4A') + leaf(a * .2, t + 30, -30, 18, '#8CC04A');
      }
    },
    cape: { c: '#3E6A26', shape: 'moss' }, quiver: { c: '#6B4226', band: '#E9E4DA', f: ['#C6FF4A', '#5E9A32', '#FF5FA2'] },
    bow: { wood: '#7A5230', grip: '#E9E4DA', string: '#C6FF4A', glow: 1, style: 'branch' },
    face: { eye: 'lidded', mouth: 'calm', brow: '#E9E4DA', browW: 1.3 },
    sw: ['#5E9A32', '#E9E4DA', '#3E6A26', '#C6FF4A', '#D9A77E']
  };
  const SNIPER = {
    key: 'tower_sniper', name: 'Tower Sniper', role: 'World 1 · Enemy', s: 1, skin: '#EDB38A', hair: '#1F2A48', gearName: 'hat',
    b: { bw: .85, lw: .9, lh: 1.2 },
    arm: { ua: '#2E3A5E', fa: '#2E3A5E', faO: { band: '#3FB6C9', bandLen: 9 }, hand: '#EDB38A' },
    leg: { ul: '#1F2A48', ll: '#1F2A48', llO: { bandTop: '#3FB6C9', bandTopLen: 5 }, foot: '#3A2618', sole: '#1E140C' },
    torso: {
      c: '#2E3A5E', detail: D => {
        const a = D.TW / 2, t = -D.TH;
        return Pi(`M${a * .1} ${t - 6}H${a + 6}V-30H${a * .1}Z`, '#3FB6C9') + Pi(rect(-a - 6, -30, D.TW + 12, 11), '#1F2A48') +
          Pi(rr(a - 20, -33, 12, 18, 4), '#FFD23F');
      }
    },
    cape: { c: '#1F2A48', shape: 'default' }, quiver: { c: '#1F2A48', band: '#FFD23F', f: ['#3FB6C9', '#FFD23F', '#3FB6C9'] },
    bow: { wood: '#1F2A48', grip: '#FFD23F', tipCap: '#FFD23F', string: '#F7F1E1', style: 'recurve', scale: 1.15 },
    face: { eye: 'lidded', mouth: 'calm', over: P(el(36, -60, 13), 'none', { w: .6 }) + ln('M49 -60L62 -58', SW() * .5) },
    sw: ['#2E3A5E', '#3FB6C9', '#1F2A48', '#FFD23F', '#EDB38A']
  };

  /* ---------- crossbow (bow style) ---------- */
  const bowP0 = A.bowP;
  function crossbowP(ch) {
    const B = ch.bow, w = 10, lim = 44;
    const prod = `M-4 ${-lim}Q20 0 -4 ${lim}`;
    const stringD = `M-4 ${-lim}L-46 0L-4 ${lim}`;
    return {
      svg: ln(stringD, 6.5 * SW() / 6, OL) + ln(stringD, 2.8 * SW() / 6, B.string) +
        P(rr(-74, -8, 96, 16, 7), B.wood, { hi: rr(-60, -5, 60, 3, 1.5) }) +
        P(rr(-80, -4, 26, 22, 6), B.wood) + tube(prod, w, B.metal) + ln(prod, w * .28, '#FFFFFF', .35) +
        P(rr(-8, -12, 16, 24, 5), B.grip) + P(rr(-50, -6, 10, 12, 3), B.metal, { w: .6 }),
      box: [-90, -lim - 20, 130, 2 * lim + 40]
    };
  }

  /* ---------- skins: palette swaps (+ extra gear) on the heroes ---------- */
  const SKINS = {
    skin_ranger_forest: { base: 'ranger', map: { '#4FB548': '#2F7A4C', '#3F9A3A': '#1F5A36', '#F3E6C4': '#E6D3A3', '#8B5A2E': '#6B4226', '#E0B04A': '#FFD23F', '#B7793F': '#8A5A36' } },
    skin_fire_ember: { base: 'fire', map: { '#A12A2E': '#3A3140', '#D8321F': '#FF7A1F', '#F2782B': '#FFB23F', '#38313A': '#1E1A20', '#FFD34A': '#FF5A2A', '#FFB23F': '#FF8A1F' } },
    skin_electric_neon: { base: 'electric', map: { '#23386E': '#3A1F6E', '#FFD83A': '#FF5FD2', '#3FE0F0': '#5CFFB0', '#15224A': '#1E0F3A', '#DDF1FF': '#F3DDFF', '#9AA8BC': '#B9A8D8' } },
    skin_bomb_goggles: { base: 'bomb', gear: 'bomb_goggles', map: { '#6E7F36': '#4E5A6E', '#7A8A3A': '#5E6B80', '#5E6B2C': '#3E4858', '#F08A24': '#FFB800', '#8A6A3A': '#5E6B80' } }
  };

  const HEROES = { ranger: A.RANGER, fire: A.FIRE, electric: A.ELEC, bomb: A.BOMB };
  const ENEMIES = {
    scout_pip: A.PIP, hunter_moss: A.MOSS, twig_red: A.twig('#E0413A', 'twig_red'), twig_blue: A.twig('#3A7BE0', 'twig_blue'),
    ranger_bramble: A.BRAMBLE, captain_thorn: A.THORN, forest_warden: A.WARDEN,
    bandit: BANDIT, crossbow_scout: SCOUT, shield_bearer: BEARER, healer_druid: DRUID, tower_sniper: SNIPER
  };
  // Logic BodyScale per look (EnemyTable): visuals must match the hit zones exactly.
  const SCALE = { captain_thorn: 1.1, forest_warden: 1.35 };

  function recolor(svg, map) {
    if (!map) return svg;
    return svg.replace(/#[0-9A-Fa-f]{6}\b/g, m => {
      const k = Object.keys(map).find(x => x.toLowerCase() === m.toLowerCase());
      return k ? map[k] : m;
    });
  }

  function looks() {
    const out = [];
    for (const [id, ch] of Object.entries(HEROES)) out.push({ id, ch, hero: 1 });
    for (const [skin, s] of Object.entries(SKINS)) out.push({ id: skin, ch: HEROES[s.base], hero: 1, map: s.map, gear: s.gear });
    for (const [id, ch] of Object.entries(ENEMIES)) out.push({ id, ch, hero: 0 });
    return out.map(l => ({ ...l, scale: SCALE[l.id] || 1 }));
  }

  /** SVG strings for every part of one look, drawn with the outline width for the given render scale. */
  function parts(lookId, swUnits) {
    const L = looks().find(l => l.id === lookId);
    const ch = L.ch;
    const prev = A.getSW();
    A.setSW(swUnits);
    const D = dims(ch);
    const res = {};
    const add = (name, p) => { if (p) res[name] = { svg: recolor(p.svg, L.map), box: p.box }; };
    for (const ex of ['normal', 'aim', 'hurt', 'happy']) add('head_' + ex, partsOf(ch, D, ex).head());
    const pt = partsOf(ch, D, 'normal');
    const gearFn = L.gear ? GEAR[L.gear] : pt.gear;
    add('gear', gearFn());
    add('torso', pt.torso());
    if (pt.cape) add('cape', pt.cape());
    add('quiver', pt.quiver());
    add('upper_arm', pt.ua());
    add('forearm', pt.fa());
    add('hand', pt.hand());
    add('upper_leg', pt.ul());
    add('lower_leg', pt.ll());
    add('foot', pt.foot());
    add('bow', ch.bow.style === 'crossbow' ? crossbowP(ch) : bowP0(ch, D));
    A.setSW(prev);
    return {
      parts: res,
      rig: {
        id: lookId, name: ch.name, hero: L.hero, scale: L.scale, gear: ch.gearName, hasCape: !!ch.cape, boss: !!ch.boss,
        dims: D, bowStyle: ch.bow.style, stringColor: recolor(ch.bow.string, L.map), stringGlow: !!ch.bow.glow,
        swatches: ch.sw.map(c => recolor(c, L.map)), weakSpot: ch.key === 'warden'
      }
    };
  }

  /** Whole-body idle picture for UI lists (roster cards, map, loadout), facing right. */
  function idleSvg(lookId, pose) {
    const L = looks().find(l => l.id === lookId);
    const ch = { ...L.ch, hero: 1 };
    const s = (ch.s || 1) * 1.15;
    const saved = GEAR[ch.key];
    if (L.gear) GEAR[ch.key] = GEAR[L.gear];
    const svg = A.place(ch, pose || POSES[0][1], ch.boss ? { cx: 540, gy: 960 } : { cx: 256, gy: 470 });
    GEAR[ch.key] = saved;
    return { svg: recolor(svg, L.map), s };
  }

  /** Head + gear portrait (like the design's expression thumbnails), facing right. */
  function portraitSvg(lookId, ex) {
    const L = looks().find(l => l.id === lookId);
    const ch = L.ch;
    const prev = A.getSW();
    A.setSW(6);
    const D = dims(ch), pt = partsOf(ch, D, ex);
    const h = pt.head(), g = (L.gear ? GEAR[L.gear] : pt.gear)();
    A.setSW(prev);
    const x0 = Math.min(h.box[0], g.box[0]), y0 = Math.min(h.box[1], g.box[1]);
    const x1 = Math.max(h.box[0] + h.box[2], g.box[0] + g.box[2]), y1 = Math.max(h.box[1] + h.box[3], g.box[1] + g.box[3]);
    return { svg: recolor(h.svg + g.svg, L.map), box: [x0, y0, x1 - x0, y1 - y0] };
  }

  window.AACrossbow = (ch) => crossbowP(ch);
  window.AAExport = { looks: () => looks().map(l => ({ id: l.id, hero: l.hero, scale: l.scale, name: l.ch.name })), parts, idleSvg, portraitSvg, POSES };
})();
