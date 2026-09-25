/*
 * Archer Arcade — arrows, arena props, World 1 scenery, effect sprites and UI pictures, drawn with the design's kit
 * (window.AAInternals: same ink colour, outline, shade and highlight rules as the character roster).
 * Colours follow Docs/DESIGN_TOKENS.md and the prototype's match canvas (ground #7ED957 / #5FBF3F, wall #C98A4B /
 * #A66A32 / #E8B07A, sun #FFE27A, hills #B9E6A6, arrow shaft #6B4A2B, fletch #FF5FA2, chest colours).
 * Units: arrows use character units (140 per m); everything else centimetres (100 per m). All art is original.
 */
(function () {
  const A = window.AAInternals;
  const { OL, P, Pi, ln, tube, el, rr, rect, leaf, flame } = A;
  const f = n => Math.round(n * 10) / 10;
  const out = [];
  // Sprite densities (px per metre). Props are big on screen, effects are small and soft.
  const PPM = { arrows: 200, props: 160, scenery: 110, backdrop: 64, fx: 128, ui: 256 };

  /** Registers one sprite: svg drawn in `units` per metre, pivot at local (0,0). */
  function add(group, id, sw, draw, opt = {}) {
    const units = opt.units || (group === 'arrows' ? 140 : 100);
    const ppm = opt.ppm || PPM[group];
    const pxPerUnit = ppm / units;
    const prev = A.getSW();
    A.setSW(sw / pxPerUnit);
    const r = draw();
    A.setSW(prev);
    out.push({ id, group, svg: r.svg, box: r.box, pxPerUnit, pxPerMeter: ppm, shade: opt.shade ?? (group === 'scenery' ? .6 : 1), trim: opt.trim ?? 1, margin: opt.margin, border: opt.border });
  }
  const SW = () => A.getSW();
  const star = (cx, cy, r1, r2, n, rot = -90) => {
    let d = '';
    for (let i = 0; i < n * 2; i++) {
      const r = i % 2 ? r2 : r1, a = (rot + i * 180 / n) * Math.PI / 180;
      d += (i ? 'L' : 'M') + f(cx + Math.cos(a) * r) + ' ' + f(cy + Math.sin(a) * r);
    }
    return d + 'Z';
  };
  const lin = (id, stops, x1 = 0, y1 = 0, x2 = 0, y2 = 1) =>
    `<defs><linearGradient id="${id}" x1="${x1}" y1="${y1}" x2="${x2}" y2="${y2}">${stops.map(([o, c, op]) => `<stop offset="${o}" stop-color="${c}"${op != null ? ` stop-opacity="${op}"` : ''}/>`).join('')}</linearGradient></defs>`;
  const rad = (id, stops) =>
    `<defs><radialGradient id="${id}">${stops.map(([o, c, op]) => `<stop offset="${o}" stop-color="${c}"${op != null ? ` stop-opacity="${op}"` : ''}/>`).join('')}</radialGradient></defs>`;
  const fillOnly = (d, c, op) => `<path d="${d}" fill="${c}"${op != null ? ` fill-opacity="${op}"` : ''}/>`;

  /* ================= arrows (tip at 0,0, pointing +x; character units) ================= */
  const L = 104, TIP = L + 18;
  function arrowBody(fl1, fl2, shaft = '#D9B27A', w = 5) {
    return tube(`M${-TIP} 0H${L - TIP}`, w, shaft) +
      P(`M${-TIP - 4} 0L${-TIP + 8} -12H${-TIP + 26}L${-TIP + 16} 0Z`, fl1, { w: .7 }) +
      P(`M${-TIP - 4} 0L${-TIP + 8} 12H${-TIP + 26}L${-TIP + 16} 0Z`, fl2, { w: .7 });
  }
  const head = (c, hi = true) => P(`M-22 -9L0 0L-22 9Z`, c, { w: .8, hi: hi ? 'M-18 -5L-8 -1L-18 -1Z' : null });
  const glow = (c, r = 16, op = .35) => `<path d="${el(-14, 0, r)}" fill="${c}" fill-opacity="${op}"/>`;
  const ABOX = [-TIP - 10, -22, TIP + 14, 44];
  add('arrows', 'arrow_normal', 5.5, () => ({ svg: arrowBody('#FF5FA2', '#FFFFFF') + head('#C9D2DC'), box: ABOX }));
  add('arrows', 'arrow_fire', 5.5, () => ({
    svg: arrowBody('#FF8A3D', '#FFD23F') + glow('#FF8A1F', 18) + flame(-20, 0, -100, .55) + head('#FF8A1F'), box: [-TIP - 10, -30, TIP + 14, 52]
  }));
  add('arrows', 'arrow_electric', 5.5, () => ({
    svg: arrowBody('#FFD83A', '#3FE0F0') + glow('#3FE0F0', 18) + head('#3FE0F0') +
      ln('M-30 -14L-24 -6L-32 -2L-26 6', SW() * .45, '#FFF3C4'), box: ABOX
  }));
  add('arrows', 'arrow_bomb', 5.5, () => ({
    svg: arrowBody('#FF5A6E', '#FFB800') + P(el(-8, 0, 12), '#2B2730', { hi: el(-5, -5, 4, 2.5) }) + P(rr(-24, -6, 8, 12, 2), '#D9A441', { w: .6 }) +
      ln('M-8 -12q3 -7 9 -7', SW() * .4, '#D9A441') + P(el(2, -20, 4), '#FFD83A', { w: .5 }), box: [-TIP - 10, -30, TIP + 14, 52]
  }));
  add('arrows', 'arrow_ice', 5.5, () => ({
    svg: arrowBody('#E6F6FF', '#7FD6F7') + glow('#E6F6FF', 17, .5) +
      P('M-26 -11L-10 -8L2 0L-10 8L-26 11L-20 0Z', '#7FD6F7', { w: .8, hi: 'M-22 -7L-10 -5L-2 -1L-18 -1Z' }), box: ABOX
  }));
  add('arrows', 'arrow_poison', 5.5, () => ({
    svg: arrowBody('#9BE15D', '#6A3279') + glow('#C6FF4A', 16, .4) + head('#9BE15D') + P(el(-30, -10, 4), '#C6FF4A', { w: .5 }) + P(el(-38, 8, 3), '#C6FF4A', { w: .5 }), box: ABOX
  }));
  add('arrows', 'arrow_split', 5.5, () => ({
    svg: arrowBody('#9D86FF', '#FFFFFF') + P('M-24 -3L-2 -12L-16 -1Z', '#9D86FF', { w: .7 }) + P('M-24 3L-2 12L-16 1Z', '#9D86FF', { w: .7 }) + head('#C9D2DC'), box: ABOX
  }));
  add('arrows', 'arrow_heavy', 6, () => ({
    svg: tube(`M${-TIP} 0H-18`, 8, '#8E7A5B') + P(`M${-TIP - 6} 0L${-TIP + 8} -14H${-TIP + 28}L${-TIP + 18} 0Z`, '#65563F', { w: .7 }) +
      P(`M${-TIP - 6} 0L${-TIP + 8} 14H${-TIP + 28}L${-TIP + 18} 0Z`, '#8E7A5B', { w: .7 }) +
      P('M-30 -13L2 0L-30 13L-24 0Z', '#6E7584', { w: .9, hi: 'M-26 -8L-8 -1L-24 -1Z' }), box: [-TIP - 14, -24, TIP + 18, 48]
  }));
  add('arrows', 'arrow_thorn', 5.5, () => ({
    svg: arrowBody('#8E1C24', '#F2C14E', '#4A2A1A') + P('M-26 -8C-18 -10 -8 -8 2 0C-8 8 -18 10 -26 8C-20 4 -20 -4 -26 -8Z', '#5A3420', { w: .8 }), box: ABOX
  }));
  add('arrows', 'arrow_leaf', 5.5, () => ({
    svg: P('M-60 0Q-30 -20 0 0Q-30 20 -60 0Z', '#8CC04A', { w: .8, hi: 'M-48 -4Q-30 -12 -10 -2Z' }) + ln('M-66 0H-8', SW() * .5, '#4E8A2A') +
      P('M-60 0Q-44 -10 -30 0Q-44 10 -60 0Z', '#5E9A32', { w: .5 }), box: [-74, -24, 80, 48]
  }));
  add('arrows', 'bomblet', 5.5, () => ({
    svg: P(el(0, 0, 13), '#2B2730', { hi: el(4, -5, 4, 2.5) }) + P(rr(-5, -18, 10, 7, 2), '#D9A441', { w: .6 }) + ln('M0 -18q3 -7 9 -7', SW() * .4, '#D9A441') + P(el(10, -26, 4), '#FFD83A', { w: .5 }),
    box: [-18, -34, 36, 52]
  }));
  add('arrows', 'arrow_half_front', 5.5, () => ({ svg: tube('M-60 0H-18', 5, '#D9B27A') + head('#C9D2DC'), box: [-66, -14, 70, 28] }));
  add('arrows', 'arrow_half_back', 5.5, () => ({ svg: arrowBody('#FF5FA2', '#FFFFFF').replace(`M${-TIP} 0H${L - TIP}`, `M${-TIP} 0H-66`), box: [-TIP - 10, -16, 70, 32] }));

  /* ================= props (centimetres, 160 px/m, pivot = logical centre) ================= */
  // Crate: 100 x 100 at scale 1 (runtime scales to the level's crate size).
  function crate(face, plank, edge, cracked) {
    let s = P(rr(-50, -50, 100, 100, 8), face, {
      inner: Pi(rect(-50, -50, 100, 16), edge) + Pi(rect(-50, 34, 100, 16), edge) + Pi(rect(-50, -50, 16, 100), edge) + Pi(rect(34, -50, 16, 100), edge) +
        ln('M-34 -34L34 34', SW() * 2.2, OL) + ln('M-34 -34L34 34', SW() * 1.2, plank) +
        ln('M-24 -14H24M-24 10H24', SW() * .35, plank, .8) +
        [[-42, -42], [42, -42], [-42, 42], [42, 42]].map(([x, y]) => `<path d="${el(x, y, 3.5)}" fill="${OL}" fill-opacity=".55"/>`).join(''),
      sh: rect(-60, -60, 22, 130), hi: rr(-40, -46, 70, 6, 3)
    });
    if (cracked) s += ln('M-6 -50L4 -22L-8 -4L6 18L-2 50', SW() * .9, OL) + ln('M4 -22L22 -30M-8 -4L-26 6', SW() * .6, OL);
    return { svg: s, box: [-54, -54, 108, 108] };
  }
  add('props', 'crate', 6, () => crate('#E0A267', '#B97B42', '#C98A4B', false));
  add('props', 'crate_cracked', 6, () => crate('#E0A267', '#B97B42', '#C98A4B', true));
  add('props', 'crate_plank', 6, () => ({ svg: P(rr(-40, -8, 80, 16, 4), '#C98A4B', { hi: rr(-34, -5, 50, 3, 1.5) }), box: [-44, -12, 88, 24] }));
  add('props', 'tnt', 6, () => {
    let stripes = '';
    for (let x = -70; x < 60; x += 22) stripes += `<path d="M${x} 50L${x + 11} 50L${x + 41} 20L${x + 30} 20Z" fill="#2B2730"/>`;
    return {
      svg: P(rr(-50, -50, 100, 100, 8), '#E5484D', {
        inner: `<path d="${rect(-50, 20, 100, 30)}" fill="#FFD23F"/>` + stripes + Pi(rect(-50, -50, 100, 14), '#B02D32') +
          P(el(0, -8, 20), '#2B2730', { w: .8, hi: el(6, -14, 6, 4) }) + P(rr(-6, -34, 12, 8, 2), '#D9A441', { w: .6 }) +
          ln('M0 -34q4 -8 12 -8', SW() * .5, '#D9A441') + P(star(14, -44, 9, 4, 5), '#FFD23F', { w: .5 }),
        sh: rect(-60, -60, 22, 130), hi: rr(-40, -46, 70, 6, 3)
      }),
      box: [-54, -56, 108, 110]
    };
  });
  // Explosive barrel: circle r 45 in Logic; drawn 84 wide x 96 tall around the centre.
  add('props', 'barrel', 6, () => ({
    svg: P('M-34 -48Q-46 0 -34 48H34Q46 0 34 -48Z', '#E5484D', {
      inner: Pi(rect(-50, -34, 100, 10), '#6E7584') + Pi(rect(-50, 24, 100, 10), '#6E7584') +
        P(star(0, -2, 18, 8, 8, -90), '#FFD23F', { w: .7 }) + P(el(0, -2, 6), '#FF8A3D', { w: .5 }),
      sh: rect(-56, -60, 24, 130), hi: rr(14, -40, 7, 60, 3.5)
    }) + P(el(0, -48, 34, 8), '#B02D32', { w: .8 }),
    box: [-40, -58, 80, 110]
  }));
  // Wall: 9-slice (tiled middle), drawn 60 x 180 for a 50 cm wide wall; border keeps the cap and base.
  add('props', 'wall', 6, () => {
    let planks = '';
    for (let y = -64; y < 88; y += 24) planks += `<path d="${rect(-30, y, 60, 3)}" fill="#A66A32"/>`;
    return {
      svg: P(rr(-30, -86, 60, 176, 6), '#C98A4B', { inner: planks + `<path d="${rect(-30, -90, 8, 180)}" fill="${OL}" fill-opacity=".14"/>`, hi: rr(16, -76, 5, 150, 2.5) }) +
        P(rr(-35, -94, 70, 16, 6), '#E8B07A', { hi: rr(-28, -91, 44, 3.5, 1.7) }),
      box: [-40, -100, 80, 196]
    };
  }, { border: [18, 20, 18, 34] });
  add('props', 'stone_wall', 6, () => {
    let bricks = '';
    for (let r = 0; r < 7; r++) {
      const y = -84 + r * 25, off = r % 2 ? -14 : 0;
      for (let x = -30 + off; x < 30; x += 28) bricks += Pi(rr(Math.max(-30, x), y, Math.min(26, 30 - Math.max(-30, x)) - (x < -30 ? -(x + 30) : 0), 22, 5), r % 3 ? '#B9B2C9' : '#A69FBA');
    }
    return { svg: P(rr(-30, -86, 60, 176, 8), '#9A93B0', { inner: bricks, sh: rect(-40, -96, 20, 200), hi: rr(16, -76, 5, 150, 2.5) }), box: [-40, -96, 80, 192] };
  }, { border: [16, 18, 16, 18] });
  // Bounce pad: springy jelly block, 9-slice, drawn 120 x 40.
  add('props', 'pad', 6, () => ({
    svg: P(rr(-60, -20, 120, 40, 20), '#2ED3A0', {
      inner: `<path d="${rect(-60, 6, 120, 14)}" fill="#12A67A"/>` + [[-38, -6], [-12, -8], [14, -6], [40, -8]].map(([x, y]) => `<path d="${el(x, y, 5, 3.5)}" fill="#FFFFFF" fill-opacity=".7"/>`).join(''),
      hi: rr(-48, -15, 90, 5, 2.5)
    }),
    box: [-66, -26, 132, 52]
  }), { border: [34, 30, 34, 30] });
  // Target board: circle r 45 (Logic default) — rings red / white, gold bull.
  function board(broken) {
    const rings = P(el(0, 0, 45), '#FFFFFF', {
      inner: `<path d="${el(0, 0, 36)}" fill="#E5484D"/><path d="${el(0, 0, 27)}" fill="#FFFFFF"/><path d="${el(0, 0, 18)}" fill="#E5484D"/>` +
        P(el(0, 0, 9), '#FFD23F', { w: .6 }), sh: el(-8, 8, 50) + ' ' + el(4, -4, 50), hi: el(16, -24, 10, 5)
    });
    return rings;
  }
  add('props', 'target', 6, () => ({ svg: board(), box: [-50, -50, 100, 100] }));
  add('props', 'target_half', 6, () => ({ svg: `<g clip-path="url(#tclip)"><clipPath id="tclip"><path d="M-60 -60H2L-6 -20L6 10L-4 60H-60Z"/></clipPath>${board()}</g>` + ln('M2 -50L-6 -20L6 10L-4 50', SW(), OL), box: [-50, -50, 60, 100] }));
  add('props', 'target_post', 6, () => ({
    svg: P(rr(-7, 0, 14, 100, 5), '#9A6234', { hi: rr(2, 4, 3, 90, 1.5) }), box: [-11, -4, 22, 108]
  }), { border: [0, 12, 0, 12] });
  add('props', 'target_stand', 6, () => ({
    svg: P('M-40 0L-6 -14L6 -14L40 0Z', '#9A6234') + P(rr(-44, -4, 88, 10, 5), '#7A4E2A'), box: [-48, -20, 96, 30]
  }));
  add('props', 'balloon', 6, () => ({
    svg: ln('M0 20Q-6 40 0 60', SW() * .6, OL) + P('M0 22C-28 18 -32 -10 -30 -20C-26 -46 26 -46 30 -20C32 -10 28 18 0 22Z', '#FF5FA2', { hi: el(12, -24, 6, 10) }) + P('M-5 20L5 20L3 26H-3Z', '#E83E8C', { w: .6 }),
    box: [-36, -50, 72, 114]
  }));
  add('props', 'apple', 6, () => ({
    svg: P('M0 -10C-10 -18 -22 -12 -22 2C-22 16 -10 22 0 18C10 22 22 16 22 2C22 -12 10 -18 0 -10Z', '#E5484D', { hi: el(8, -6, 5, 3), sh: el(-12, 10, 20) }) +
      ln('M0 -10Q2 -18 6 -22', SW() * .6, '#6B4226') + leaf(4, -18, -30, 12, '#5FBF3F'),
    box: [-26, -28, 52, 52]
  }));
  add('props', 'apple_half', 6, () => ({
    svg: P('M0 -10C-10 -18 -22 -12 -22 2C-22 16 -10 22 0 18Z', '#E5484D') + P('M-2 -8C-8 -12 -16 -8 -16 2C-16 12 -8 16 -2 14Z', '#FFF3C4', { w: .5 }) + P(el(-8, 2, 2, 3), '#6B4226', { w: 0 }),
    box: [-26, -22, 30, 44]
  }));
  // Dummy: box 50 x 160 standing on its feet (pivot at the feet).
  add('props', 'dummy', 6, () => ({
    svg: P(rr(-6, -60, 12, 60, 4), '#9A6234') + P(rr(-34, -12, 68, 12, 5), '#7A4E2A') +
      P(rr(-24, -130, 48, 78, 18), '#E3C98E', { inner: ln('M-24 -104H24M-24 -80H24', SW() * .5, '#B99A5A') + ln('M-10 -130L-14 -52M10 -130L14 -52', SW() * .35, '#B99A5A', .8), hi: rr(12, -122, 5, 50, 2.5) }) +
      P(rr(-40, -112, 80, 10, 5), '#B99A5A') +
      P(el(0, -146, 22), '#E3C98E', { hi: el(8, -154, 6, 4) }) + `<path d="${el(-7, -148, 2.5, 3.5)}" fill="${OL}"/><path d="${el(7, -148, 2.5, 3.5)}" fill="${OL}"/>` + ln('M-8 -138Q0 -132 8 -138', SW() * .6),
    box: [-44, -172, 88, 176]
  }));
  add('props', 'stump', 6, () => ({
    svg: P('M-44 0V-50Q-44 -60 0 -60Q44 -60 44 -50V0Z', '#9A6234', { inner: ln('M-20 -52V0M14 -54V0', SW() * .4, '#7A4E2A', .7), sh: rect(-60, -70, 24, 80), hi: rr(26, -48, 5, 40, 2.5) }) +
      P(el(0, -56, 44, 11), '#E0A267', { inner: `<path d="${el(0, -56, 28, 7)}" fill="none" stroke="#B97B42" stroke-width="${SW() * .5}"/><path d="${el(0, -56, 14, 3.5)}" fill="none" stroke="#B97B42" stroke-width="${SW() * .5}"/>` }) +
      P('M-44 -6Q-60 0 -64 4H-40Z', '#9A6234', { w: .8 }) + P('M44 -6Q60 0 64 4H40Z', '#9A6234', { w: .8 }),
    box: [-68, -72, 136, 80]
  }));
  add('props', 'rope', 6, () => ({
    svg: `<path d="${rect(-6, -50, 12, 100)}" fill="#C9A77A"/>` + ln('M-6 -50L6 -38M-6 -30L6 -18M-6 -10L6 2M-6 10L6 22M-6 30L6 42', SW() * .45, '#8B6A3E') +
      ln('M-6 -52V52M6 -52V52', SW() * .8, OL),
    box: [-9, -50, 18, 100]
  }), { shade: 0, trim: 0, margin: 0, border: [0, 0, 0, 0] });
  add('props', 'cage', 6, () => {
    let bars = '';
    for (let x = -32; x <= 32; x += 16) bars += ln(`M${x} -40V44`, SW() * 1.9, OL) + ln(`M${x} -40V44`, SW() * .9, '#C9D2DC');
    return {
      svg: P(rr(-42, 40, 84, 14, 6), '#6E7584') + P('M-42 -38Q0 -74 42 -38Z', '#6E7584', { hi: el(10, -56, 12, 4) }) + P(el(0, -64, 8), '#FFD23F', { w: .7 }) + bars,
      box: [-48, -76, 96, 136]
    };
  });
  // The friend in the cage (Rescue): a fox cub, original character.
  function fox(happy) {
    return P('M-18 30C-26 10 -20 -8 0 -8C20 -8 26 10 18 30Z', '#FF8A3D', { hi: el(8, 4, 5, 8) }) + P('M-10 30C-12 18 -8 10 0 10C8 10 12 18 10 30Z', '#FFF3E0', { w: .6 }) +
      P('M18 26C34 26 40 12 36 0C30 10 24 14 16 16Z', '#FF8A3D', { w: .8 }) + P('M36 0C38 -4 34 -8 30 -6C32 -2 32 2 36 0Z', '#FFF3E0', { w: .6 }) +
      P(el(0, -22, 18, 16), '#FF8A3D', { hi: el(8, -30, 5, 3) }) + P('M-16 -30L-18 -50L-4 -36Z', '#FF8A3D', { w: .8 }) + P('M16 -30L18 -50L4 -36Z', '#FF8A3D', { w: .8 }) +
      P('M-12 -14Q0 -2 12 -14Q0 -20 -12 -14Z', '#FFF3E0', { w: .5 }) + `<path d="${el(0, -15, 3, 2.2)}" fill="${OL}"/>` +
      (happy ? ln('M-11 -24Q-7 -30 -3 -24M3 -24Q7 -30 11 -24', SW() * .6) : `<path d="${el(-7, -24, 2.6, 3.4)}" fill="${OL}"/><path d="${el(7, -24, 2.6, 3.4)}" fill="${OL}"/>`);
  }
  add('props', 'fox', 6, () => ({ svg: fox(false), box: [-30, -56, 74, 92] }));
  add('props', 'fox_happy', 6, () => ({ svg: fox(true), box: [-30, -56, 74, 92] }));
  // Shield Bearer's tall shield: 24 x 144 (Logic half 12 x 72), drawn a bit wider for readability.
  add('props', 'shield_tall', 6, () => ({
    svg: P(rr(-18, -74, 36, 148, 12), '#B97B42', {
      inner: ln('M-6 -74V74M6 -74V74', SW() * .4, '#8B5A2E', .8) + Pi(rect(-20, -54, 40, 8), '#8A94A6') + Pi(rect(-20, 44, 40, 8), '#8A94A6') +
        P(el(0, -4, 10), '#8A94A6', { w: .7, hi: el(3, -7, 3, 2) }), sh: rect(-26, -80, 12, 170), hi: rr(8, -64, 4, 110, 2)
    }),
    box: [-24, -80, 48, 160]
  }));
  add('props', 'shield_boss', 6, () => ({
    svg: P(rr(-18, -36, 36, 72, 14), '#8B5B37', { inner: ln('M-6 -36V36M6 -36V36', SW() * .4, '#5E3A20', .8), sh: rect(-26, -40, 12, 80), hi: rr(8, -28, 4, 44, 2) }) +
      leaf(-4, -30, -70, 22, '#5E9A32') + leaf(4, -30, -110, 18, '#8CC04A') + P(el(0, 4, 8), '#C6FF4A', { w: .6 }),
    box: [-24, -58, 48, 100]
  }));
  // Moving platform: 9-slice, drawn 280 x 40 (box 2.8 m x 0.4 m), leafy log with glowing propeller seeds below.
  add('props', 'platform', 6, () => ({
    svg: P(rr(-140, -20, 280, 40, 20), '#9A6234', {
      inner: `<path d="${rect(-140, -20, 280, 12)}" fill="#7ED957"/>` + `<path d="${rect(-140, -8, 280, 4)}" fill="#5FBF3F"/>` + ln('M-100 6H-40M-10 10H60M90 4H120', SW() * .45, '#7A4E2A', .8),
      hi: rr(-120, -16, 220, 4, 2)
    }) + P(el(-140, 0, 10, 18), '#B97B42', { inner: `<path d="${el(-140, 0, 5, 10)}" fill="none" stroke="#7A4E2A" stroke-width="${SW() * .4}"/>` }) +
      P(el(140, 0, 10, 18), '#B97B42', { inner: `<path d="${el(140, 0, 5, 10)}" fill="none" stroke="#7A4E2A" stroke-width="${SW() * .4}"/>` }),
    box: [-154, -26, 308, 52]
  }), { border: [40, 26, 40, 26] });
  add('props', 'propeller', 6, () => ({
    svg: P(el(0, 0, 8), '#FFD23F', { w: .6 }) + P('M0 0Q-30 -12 -44 -2Q-30 6 0 0Z', '#FFFFFF', { w: .7 }) + P('M0 0Q30 -12 44 -2Q30 6 0 0Z', '#FFFFFF', { w: .7 }),
    box: [-48, -14, 96, 26]
  }));
  // Vine wall: 9-slice vertical, drawn 60 x 260.
  add('props', 'vine_wall', 6, () => {
    let s = '';
    for (let y = -120; y < 120; y += 40) s += leaf(y % 80 ? -24 : 24, y, y % 80 ? 200 : -20, 26, y % 80 ? '#5E9A32' : '#8CC04A');
    return {
      svg: tube('M-10 130C-30 60 20 20 -6 -40C-20 -80 10 -110 0 -130', 16, '#4E8A2A') + tube('M12 130C30 70 -16 30 10 -30C24 -70 -6 -100 6 -130', 12, '#5E9A32') + s +
        [[-16, -60], [18, 0], [-14, 60], [14, -110], [-4, 100]].map(([x, y]) => P(`M${x - 4} ${y}L${x + (x < 0 ? -12 : 12)} ${y - 6}L${x + 4} ${y + 4}Z`, '#2F5A3A', { w: .6 })).join(''),
      box: [-44, -134, 88, 268]
    };
  }, { border: [0, 40, 0, 40] });
  add('props', 'bubble', 4, () => ({
    svg: rad('bub', [[0, '#FFFFFF', 0], [.72, '#9DE8FF', .12], [.93, '#7FD6F7', .45], [1, '#FFFFFF', .9]]) + `<circle r="100" fill="url(#bub)"/>` +
      `<path d="${el(38, -46, 22, 12)}" fill="#FFFFFF" fill-opacity=".75" transform="rotate(35 38 -46)"/>` + `<circle r="100" fill="none" stroke="#FFFFFF" stroke-width="${SW() * .8}" stroke-opacity=".8"/>`,
    box: [-104, -104, 208, 208]
  }), { shade: 0 });

  /* ---- floating islands: caps + tileable middle (no vertical ink lines at the tile seams) ---- */
  const GRASS = '#7ED957', GRASS2 = '#5FBF3F', DIRT = '#B97B42', DIRT2 = '#9A6234', ROCK = '#8E7A5B';
  function islandBand(x0, x1) {
    const w = x1 - x0;
    let lip = `M${x0} -6`;
    for (let x = x0; x < x1; x += 25) lip += `Q${x + 6.25} 26 ${x + 12.5} 16Q${x + 18.75} 26 ${x + 25} 14`;
    lip += `V-6Z`;
    let top = `M${x0} -2`;
    for (let x = x0; x < x1; x += 50) top += `Q${x + 12.5} -12 ${x + 25} -3Q${x + 37.5} 6 ${x + 50} -2`;
    return lin('dirtG', [[0, DIRT], [.55, DIRT2], [1, ROCK]]) +
      `<path d="${rect(x0, 0, w, 120)}" fill="url(#dirtG)"/>` +
      `<path d="${rect(x0, 94, w, 26)}" fill="${ROCK}"/>` +
      fillOnly(lip, GRASS2) + fillOnly(`${top}V8H${x0}Z`, GRASS) +
      ln(top.replace(/Z$/, ''), SW(), OL);
  }
  add('props', 'island_mid', 6, () => {
    const x0 = -50, x1 = 50;
    const peb = [[-26, 46, 7], [14, 70, 5], [30, 38, 4], [-8, 104, 6]].map(([x, y, r]) => `<path d="${el(x, y, r, r * .75)}" fill="${OL}" fill-opacity=".18"/>`).join('');
    return { svg: islandBand(x0, x1) + peb + ln(`M${x0} 120H${x1}`, SW(), OL), box: [x0, -16, 100, 140] };
  }, { shade: 0, trim: 0, margin: 0 });
  function islandCap(side) {
    const s = side; // -1 left, +1 right
    const d = `M0 -3Q${24 * s} -10 ${42 * s} 2Q${58 * s} 18 ${50 * s} 50Q${44 * s} 86 ${22 * s} 108Q${10 * s} 120 0 120Z`;
    const id = 'cap' + (s < 0 ? 'L' : 'R');
    return {
      svg: `<clipPath id="${id}"><path d="${d}"/></clipPath><g clip-path="url(#${id})">${islandBand(s < 0 ? -80 : 0, s < 0 ? 0 : 80)}</g>` +
        `<path d="${d}" fill="none" stroke="${OL}" stroke-width="${SW()}" stroke-linejoin="round"/>` + `<path d="M0 -3V120" stroke="${GRASS}" stroke-width="0"/>`,
      box: s < 0 ? [-62, -14, 62, 138] : [0, -14, 62, 138]
    };
  }
  add('props', 'island_left', 6, () => islandCap(-1), { shade: .5 });
  add('props', 'island_right', 6, () => islandCap(1), { shade: .5 });
  const under = (id, w, h, seed) => add('props', id, 6, () => {
    const d = `M${-w / 2} 0Q${-w * .42} ${h * .45} ${-w * .12} ${h * .8}L0 ${h}L${w * .14} ${h * .74}Q${w * .44} ${h * .4} ${w / 2} 0Z`;
    const roots = seed % 2 ? ln(`M${-w * .2} ${h * .5}Q${-w * .34} ${h * .9} ${-w * .26} ${h * 1.2}`, SW() * .9, '#6B4226') : '';
    return {
      svg: roots + P(d, ROCK, { inner: ln(`M${-w * .3} ${h * .3}L${-w * .05} ${h * .55}M${w * .2} ${h * .2}L${w * .08} ${h * .6}`, SW() * .45, '#65563F', .8), sh: rect(-w, -10, w * .45, h * 1.3), hi: rr(w * .18, h * .12, 6, h * .3, 3) }),
      box: [-w / 2 - 6, -6, w + 12, h * 1.25 + 8]
    };
  });
  under('island_under_1', 160, 110, 1);
  under('island_under_2', 110, 80, 2);
  under('island_under_3', 70, 60, 3);
  add('props', 'root', 6, () => ({ svg: ln('M0 0Q-10 30 4 60Q14 84 2 110', SW() * 1.9, OL) + ln('M0 0Q-10 30 4 60Q14 84 2 110', SW() * .9, '#6B4226') + leaf(4, 60, 30, 18, '#5FBF3F'), box: [-16, -4, 36, 118] }));

  /* ================= World 1 scenery (110 px/m) ================= */
  const treeCanopy = (cx, cy, r, c1, c2) => {
    const cs = [[0, 0, r], [-r * .7, r * .25, r * .7], [r * .7, r * .2, r * .72], [-r * .3, -r * .45, r * .66], [r * .35, -r * .4, r * .6]];
    const d = cs.map(([x, y, rr_]) => el(cx + x, cy + y, rr_)).join(' ');
    return `<path d="${d}" fill="none" stroke="${OL}" stroke-width="${f(SW() * 2)}" stroke-linejoin="round"/><path d="${d}" fill="${c1}"/>` +
      cs.slice(3).map(([x, y, rr_]) => `<path d="${el(cx + x + rr_ * .2, cy + y - rr_ * .15, rr_ * .55)}" fill="${c2}"/>`).join('') +
      `<path d="${el(cx - r * .5, cy + r * .45, r * .6, r * .3)}" fill="${OL}" fill-opacity=".16"/>`;
  };
  add('scenery', 'tree_round', 6, () => ({
    svg: tube('M0 0V-150', 34, '#8B5A2E') + tube('M0 -110L-36 -150', 14, '#8B5A2E') + treeCanopy(0, -200, 88, '#5FBF3F', '#8CD65E'),
    box: [-160, -330, 320, 336]
  }));
  add('scenery', 'tree_pine', 6, () => ({
    svg: tube('M0 0V-60', 26, '#7A4E2A') + P('M0 -300L-70 -170H-40L-100 -60H100L40 -170H70Z', '#2F8F4E', { inner: `<path d="M0 -300L20 -250L-10 -230Z" fill="#FFFFFF" fill-opacity=".25"/>`, sh: 'M0 -300L-100 -60H-20Z' }),
    box: [-108, -308, 216, 314]
  }));
  add('scenery', 'tree_night', 6, () => ({
    svg: tube('M0 0V-150', 34, '#5A3A22') + treeCanopy(0, -200, 88, '#2F7A4C', '#3F9A5C'), box: [-160, -330, 320, 336]
  }));
  add('scenery', 'bush', 6, () => ({ svg: treeCanopy(0, -30, 34, '#4FB548', '#8CD65E'), box: [-66, -90, 132, 96] }));
  add('scenery', 'bush_berry', 6, () => ({
    svg: treeCanopy(0, -30, 34, '#3F9A3A', '#6CC04A') + [[-20, -34], [8, -52], [22, -22], [-6, -14]].map(([x, y]) => P(el(x, y, 5), '#E83E8C', { w: .5 })).join(''),
    box: [-66, -90, 132, 96]
  }));
  add('scenery', 'grass', 5, () => ({ svg: P('M-26 0Q-22 -26 -14 -34Q-12 -14 -6 -6Q-2 -34 6 -40Q8 -14 12 -6Q18 -28 28 -30Q20 -12 22 0Z', GRASS2, { w: .8 }), box: [-32, -44, 64, 48] }));
  const flower = (id, petal, mid) => add('scenery', id, 5, () => ({
    svg: ln('M0 0Q-4 -16 0 -30', SW() * 1.8, OL) + ln('M0 0Q-4 -16 0 -30', SW() * .8, GRASS2) + leaf(-2, -12, -150, 12, GRASS) +
      [0, 72, 144, 216, 288].map(a => P(el(Math.cos(a * Math.PI / 180) * 8, -34 + Math.sin(a * Math.PI / 180) * 8, 6), petal, { w: .6 })).join('') + P(el(0, -34, 5), mid, { w: .6 }),
    box: [-20, -52, 40, 56]
  }));
  flower('flower_pink', '#FF5FA2', '#FFD23F');
  flower('flower_yellow', '#FFD23F', '#FF8A3D');
  flower('flower_white', '#FFFFFF', '#FFB800');
  add('scenery', 'mushroom', 5, () => ({
    svg: P(rr(-8, -24, 16, 24, 6), '#FFF3E0') + P('M-24 -20Q-24 -46 0 -46Q24 -46 24 -20Z', '#E5484D', { hi: el(8, -38, 5, 3) }) +
      P(el(-10, -32, 4), '#FFFFFF', { w: 0 }) + P(el(10, -28, 3), '#FFFFFF', { w: 0 }),
    box: [-28, -52, 56, 56]
  }));
  add('scenery', 'rock', 6, () => ({ svg: P('M-40 0Q-44 -26 -18 -38Q10 -48 32 -30Q46 -16 40 0Z', '#A69FBA', { sh: rect(-60, -60, 30, 70), hi: rr(8, -36, 16, 5, 2.5) }), box: [-48, -50, 94, 54] }));
  add('scenery', 'cloud', 5, () => ({
    svg: `<path d="${el(-40, 0, 36, 28)} ${el(0, -18, 46, 38)} ${el(44, 0, 34, 26)} ${rr(-76, -4, 150, 30, 15)}" fill="#FFFFFF"/>` +
      `<path d="${rr(-70, 12, 138, 12, 6)}" fill="#D6ECFF"/>`,
    box: [-80, -60, 160, 88]
  }), { shade: 0 });
  add('scenery', 'sun', 5, () => ({
    svg: rad('sunG', [[0, '#FFE27A', .9], [.55, '#FFE27A', .35], [1, '#FFE27A', 0]]) + `<circle r="120" fill="url(#sunG)"/>` + `<circle r="48" fill="#FFE27A"/>` + `<path d="${el(14, -16, 14, 9)}" fill="#FFFFFF" fill-opacity=".6"/>`,
    box: [-122, -122, 244, 244]
  }), { shade: 0 });
  add('scenery', 'moon', 5, () => ({
    svg: rad('moonG', [[0, '#FFF3C4', .7], [.5, '#FFF3C4', .2], [1, '#FFF3C4', 0]]) + `<circle r="110" fill="url(#moonG)"/>` +
      `<path d="M10 -44A46 46 0 1 0 44 16A36 36 0 1 1 10 -44Z" fill="#FFF3C4"/>` + `<path d="${el(-14, 6, 6)}" fill="#E8D9A0"/><path d="${el(-2, 26, 4)}" fill="#E8D9A0"/>`,
    box: [-112, -112, 224, 224]
  }), { shade: 0 });
  add('scenery', 'firefly', 3, () => ({
    svg: rad('ffG', [[0, '#FFF7B0', 1], [.3, '#E6FF6A', .7], [1, '#C6FF4A', 0]]) + `<circle r="20" fill="url(#ffG)"/>`, box: [-20, -20, 40, 40]
  }), { shade: 0 });

  /* backdrop layers (soft, no ink; tinted by the runtime for time of day): tileable 1000 cm wide */
  function hills(id, c, h, bumps, seed) {
    add('backdrop', id, 0, () => {
      let d = `M-500 0`;
      const n = bumps;
      for (let i = 0; i < n; i++) {
        const x0 = -500 + i * 1000 / n, x1 = x0 + 1000 / n;
        const hh = h * (0.7 + 0.3 * Math.abs(Math.sin((i + 1) * seed)));
        d += `C${x0 + 1000 / n * .25} ${-hh} ${x1 - 1000 / n * .25} ${-hh} ${x1} 0`;
      }
      d += `V${h * .8}H-500Z`;
      return { svg: fillOnly(d, c), box: [-500, -h * 1.05, 1000, h * 1.85] };
    }, { shade: 0, trim: 0, margin: 0 });
  }
  hills('hills_far', '#B9E6A6', 260, 3, 1.7);
  hills('hills_near', '#94D878', 200, 4, 2.3);
  add('backdrop', 'forest_far', 0, () => {
    let d = 'M-500 0';
    for (let x = -500; x < 500; x += 50) {
      const t = (x + 500) / 50, hgt = 150 + 60 * Math.abs(Math.sin(t * 1.9));
      d += `L${x + 10} ${-hgt * .55}L${x + 25} ${-hgt}L${x + 40} ${-hgt * .55}L${x + 50} 0`;
    }
    d += 'V120H-500Z';
    return { svg: fillOnly(d, '#6CBF6A'), box: [-500, -220, 1000, 340] };
  }, { shade: 0, trim: 0, margin: 0 });
  add('backdrop', 'island_far', 0, () => ({
    svg: fillOnly('M-160 0Q-80 -30 0 -24Q90 -30 160 0Q120 40 60 70Q20 120 0 160Q-20 110 -70 70Q-130 40 -160 0Z', '#A8D8F0') +
      fillOnly('M-150 -4Q-80 -40 0 -34Q90 -40 150 -4Q80 6 0 4Q-80 6 -150 -4Z', '#9ED49A') +
      `<path d="${el(-40, -60, 40, 34)} ${el(10, -80, 46, 40)} ${el(60, -54, 34, 28)}" fill="#8CC98A"/>`,
    box: [-162, -122, 324, 284]
  }), { shade: 0 });

  /* ================= effects (128 px/m, mostly unshaded) ================= */
  add('fx', 'glow', 0, () => ({ svg: rad('glowG', [[0, '#FFFFFF', 1], [.4, '#FFFFFF', .45], [1, '#FFFFFF', 0]]) + `<circle r="50" fill="url(#glowG)"/>`, box: [-50, -50, 100, 100] }), { shade: 0, margin: 0 });
  add('fx', 'dot', 0, () => ({ svg: `<circle r="20" fill="#FFFFFF"/>`, box: [-20, -20, 40, 40] }), { shade: 0 });
  add('fx', 'square', 0, () => ({ svg: `<path d="${rect(-20, -12, 40, 24)}" fill="#FFFFFF"/>`, box: [-20, -12, 40, 24] }), { shade: 0, margin: 2 });
  add('fx', 'spark', 0, () => ({ svg: fillOnly(star(0, 0, 30, 6, 4, -90), '#FFFFFF'), box: [-30, -30, 60, 60] }), { shade: 0 });
  add('fx', 'star', 4, () => ({ svg: P(star(0, 0, 28, 13, 5), '#FFD23F', { hi: el(-6, -8, 5, 3) }), box: [-32, -32, 64, 64] }));
  add('fx', 'puff', 5, () => ({ svg: `<path d="${el(-22, 6, 24)} ${el(0, -12, 28)} ${el(24, 4, 22)} ${el(2, 14, 26)}" fill="none" stroke="${OL}" stroke-width="${SW() * 2}"/>` + `<path d="${el(-22, 6, 24)} ${el(0, -12, 28)} ${el(24, 4, 22)} ${el(2, 14, 26)}" fill="#FFFFFF"/>` + `<path d="${el(-10, 18, 30, 12)}" fill="#D6D2EA"/>`, box: [-52, -44, 104, 90] }));
  add('fx', 'smoke', 0, () => ({ svg: rad('smG', [[0, '#FFFFFF', .9], [.7, '#FFFFFF', .5], [1, '#FFFFFF', 0]]) + `<path d="${el(-18, 4, 26)} ${el(4, -12, 30)} ${el(22, 6, 24)}" fill="url(#smG)"/>`, box: [-46, -44, 94, 80] }), { shade: 0 });
  add('fx', 'splinter', 4, () => ({ svg: P('M-14 -3L12 -5L16 0L10 5L-14 3Z', '#C98A4B'), box: [-18, -8, 36, 16] }));
  add('fx', 'leaf', 4, () => ({ svg: leaf(-14, 0, 0, 28, '#8CC04A'), box: [-18, -12, 34, 24] }));
  add('fx', 'flame', 4, () => ({ svg: flame(0, 0, 0, 1.4), box: [-18, -46, 36, 50] }));
  add('fx', 'ice_shard', 4, () => ({ svg: P('M0 -26L8 -4L0 20L-8 -4Z', '#7FD6F7', { hi: 'M0 -20L4 -4L0 -2Z' }), box: [-12, -30, 24, 54] }));
  add('fx', 'ice_block', 5, () => ({
    svg: P(rr(-40, -90, 80, 100, 12), '#BDEBFF', { hi: 'M-30 -80L-10 -80L-34 -20Z', inner: `<path d="M10 -84L30 -84L-20 6L-36 6Z" fill="#FFFFFF" fill-opacity=".35"/>` }).replace('fill="#BDEBFF"', 'fill="#BDEBFF" fill-opacity=".65"'),
    box: [-46, -96, 92, 112]
  }), { shade: 0 });
  add('fx', 'bubble_small', 3, () => ({ svg: `<circle r="10" fill="#C6FF4A" fill-opacity=".5" stroke="${OL}" stroke-width="${SW()}"/>` + `<path d="${el(3, -4, 3, 2)}" fill="#FFFFFF"/>`, box: [-14, -14, 28, 28] }), { shade: 0 });
  add('fx', 'poison_cloud', 0, () => ({ svg: rad('pcG', [[0, '#9BE15D', .75], [.7, '#9BE15D', .35], [1, '#9BE15D', 0]]) + `<path d="${el(-22, 6, 30)} ${el(4, -12, 34)} ${el(26, 6, 28)}" fill="url(#pcG)"/>`, box: [-54, -48, 110, 86] }), { shade: 0 });
  add('fx', 'ring', 0, () => ({ svg: `<circle r="46" fill="none" stroke="#FFFFFF" stroke-width="8"/>`, box: [-52, -52, 104, 104] }), { shade: 0 });
  add('fx', 'scorch', 0, () => ({ svg: rad('scG', [[0, '#2A1B30', .6], [1, '#2A1B30', 0]]) + `<path d="${el(0, 0, 40, 14)}" fill="url(#scG)"/>`, box: [-40, -14, 80, 28] }), { shade: 0 });
  add('fx', 'bolt', 0, () => ({
    svg: `<path d="M8 -200L-14 -110L6 -104L-18 -10L2 -6L-10 60" fill="none" stroke="#FFF3C4" stroke-width="16" stroke-linejoin="round" stroke-opacity=".45"/>` +
      `<path d="M8 -200L-14 -110L6 -104L-18 -10L2 -6L-10 60" fill="none" stroke="#FFFFFF" stroke-width="6" stroke-linejoin="round"/>`,
    box: [-30, -204, 52, 268]
  }), { shade: 0 });
  add('fx', 'zzz', 4, () => ({ svg: P('M-12 -12H12L-6 8H12V14H-14L4 -6H-12Z', '#FFFFFF'), box: [-16, -16, 32, 32] }));
  add('fx', 'ghost', 5, () => ({
    svg: P('M-22 20V-10Q-22 -34 0 -34Q22 -34 22 -10V20L14 12L7 20L0 12L-7 20L-14 12Z', '#FFFFFF', { hi: el(8, -20, 5, 3) }).replace('fill="#FFFFFF"', 'fill="#FFFFFF" fill-opacity=".92"') +
      `<path d="${el(-7, -12, 3, 4)}" fill="${OL}"/><path d="${el(7, -12, 3, 4)}" fill="${OL}"/>` + ln('M-4 -2Q0 2 4 -2', SW() * .5),
    box: [-26, -38, 52, 62]
  }));
  add('fx', 'bandaid', 3, () => ({ svg: P(rr(-18, -6, 36, 12, 6), '#F5C9A0', { inner: `<path d="${rect(-6, -6, 12, 12)}" fill="#E0A880"/>` + `<circle cx="-2" cy="-2" r="1" fill="#B98A60"/><circle cx="2" cy="2" r="1" fill="#B98A60"/>` }), box: [-20, -8, 40, 16] }));
  add('fx', 'bandage', 4, () => ({ svg: P('M-50 -10Q0 -26 50 -10L50 8Q0 -8 -50 8Z', '#FFFFFF', { inner: ln('M-40 -8L-34 6M-10 -14L-4 0M20 -14L26 0', SW() * .4, '#D6D2EA') }) + P('M-50 -2L-68 -14L-64 4Z', '#FFFFFF', { w: .7 }), box: [-72, -24, 126, 36] }));
  add('fx', 'sweat', 3, () => ({ svg: P('M0 -12Q-8 0 -6 6Q-4 12 0 12Q4 12 6 6Q8 0 0 -12Z', '#8FD8FF', { hi: el(2, 4, 2, 3) }), box: [-10, -16, 20, 30] }));
  add('fx', 'scratch', 3, () => ({ svg: ln('M-12 -8L10 6M-10 -2L6 10M-4 -10L12 0', 4, '#C24A4A', .8), box: [-14, -12, 28, 24] }), { shade: 0 });
  add('fx', 'soot', 0, () => ({ svg: rad('sootG', [[0, '#2A1B30', .7], [1, '#2A1B30', 0]]) + `<path d="${el(-8, 0, 18, 12)} ${el(10, 4, 14, 10)}" fill="url(#sootG)"/>`, box: [-28, -14, 54, 30] }), { shade: 0 });
  add('fx', 'frost', 0, () => ({ svg: `<path d="${star(0, 0, 18, 5, 6)}" fill="#E6F6FF" fill-opacity=".85"/>` + `<path d="${el(10, 8, 10, 7)}" fill="#BDEBFF" fill-opacity=".7"/>`, box: [-20, -20, 42, 38] }), { shade: 0 });
  add('fx', 'bruise', 0, () => ({ svg: rad('brG', [[0, '#7B3F8C', .55], [1, '#7B3F8C', 0]]) + `<path d="${el(0, 0, 16, 12)}" fill="url(#brG)"/>`, box: [-16, -12, 32, 24] }), { shade: 0 });
  add('fx', 'crack', 3, () => ({ svg: ln('M-10 -14L0 -2L-6 6L6 14M0 -2L10 -6', 3.5, OL, .85), box: [-14, -18, 28, 36] }), { shade: 0 });
  add('fx', 'heal_plus', 3, () => ({ svg: P('M-5 -16H5V-5H16V5H5V16H-5V5H-16V-5H-5Z', '#2ED3A0'), box: [-20, -20, 40, 40] }));
  add('fx', 'heart', 4, () => ({ svg: P('M0 14C-24 -2 -22 -22 -10 -22C-4 -22 0 -16 0 -12C0 -16 4 -22 10 -22C22 -22 24 -2 0 14Z', '#E5484D', { hi: el(-9, -14, 4, 3) }), box: [-26, -26, 52, 44] }));
  add('fx', 'helmet', 4, () => ({ svg: P('M-26 8C-26 -26 26 -26 26 8Z', '#A7B0BF', { hi: el(8, -12, 7, 3) }) + P(rr(-30, 4, 60, 8, 4), '#7C8596'), box: [-34, -26, 68, 42] }));
  add('fx', 'feather', 4, () => ({ svg: leaf(-16, 0, 0, 32, '#FFFFFF'), box: [-20, -12, 38, 24] }));
  add('fx', 'confetti', 0, () => ({ svg: `<path d="${rr(-8, -4, 16, 8, 2)}" fill="#FFFFFF"/>`, box: [-8, -4, 16, 8] }), { shade: 0, margin: 2 });
  add('fx', 'coin', 3, () => ({ svg: P(el(0, 0, 16), '#FFD23F', { inner: `<path d="${el(0, 0, 10)}" fill="none" stroke="#D4A514" stroke-width="3"/>`, hi: el(5, -6, 4, 2.5) }), box: [-20, -20, 40, 40] }));
  add('fx', 'shadow', 0, () => ({ svg: rad('shG', [[0, '#1E1A45', .45], [.6, '#1E1A45', .25], [1, '#1E1A45', 0]]) + `<path d="${el(0, 0, 50, 12)}" fill="url(#shG)"/>`, box: [-50, -12, 100, 24] }), { shade: 0 });
  add('fx', 'dash', 0, () => ({ svg: `<path d="${rr(-24, -3, 48, 6, 3)}" fill="#FFFFFF"/>`, box: [-24, -3, 48, 6] }), { shade: 0, margin: 2 });
  add('fx', 'white', 0, () => ({ svg: `<path d="${rect(-8, -8, 16, 16)}" fill="#FFFFFF"/>`, box: [-8, -8, 16, 16] }), { shade: 0, trim: 0, margin: 0 });

  /* ================= UI pictures (256 px/m, i.e. drawn at 1 unit = 2.56 px) ================= */
  function chest(c1, c2, open, trim) {
    const body = P(rr(-56, -14, 112, 58, 12), c2, {
      inner: Pi(rect(-58, 10, 116, 10), trim) + ln('M-36 -14V44M36 -14V44', SW() * .45, OL, .35), sh: rect(-70, -20, 26, 80), hi: rr(-46, -8, 80, 5, 2.5)
    });
    const lid = open
      ? P('M-56 -18L-44 -70Q0 -84 44 -70L56 -18Z', c1, { inner: Pi(rect(-60, -46, 120, 9), trim), hi: rr(-30, -72, 50, 5, 2.5) })
      : P('M-56 -14V-30Q-56 -56 0 -56Q56 -56 56 -30V-14Z', c1, { inner: Pi(rect(-60, -30, 120, 9), trim), sh: rect(-70, -60, 26, 60), hi: rr(-30, -50, 50, 5, 2.5) });
    const glowIn = open ? `<path d="${el(0, -16, 50, 14)}" fill="#FFF3C4"/>` + rad('cg', [[0, '#FFF3C4', .9], [1, '#FFF3C4', 0]]) + `<path d="${el(0, -40, 70, 60)}" fill="url(#cg)"/>` : '';
    const lock = P(rr(-12, -26, 24, 28, 7), '#FFE58A', { w: .8, inner: `<path d="${el(0, -14, 3.5, 4.5)}" fill="#D4A514"/>` });
    return { svg: glowIn + body + lid + (open ? '' : lock), box: [-74, -104, 148, 156] };
  }
  const CHESTS = [['wood', '#E0A267', '#B97B42', '#8B5A2E'], ['silver', '#DCE3EE', '#A8B4C8', '#7C8596'], ['gold', '#FFD84D', '#E0A800', '#B8860B'], ['forest', '#8CC04A', '#5E9A32', '#FFD23F']];
  for (const [id, c1, c2, trim] of CHESTS) {
    add('ui', 'chest_' + id, 5, () => chest(c1, c2, false, trim), { units: 100 });
    add('ui', 'chest_' + id + '_open', 5, () => chest(c1, c2, true, trim), { units: 100 });
  }
  // App logo: bow + arrow on the purple splash tile (the tile itself is drawn by the UI).
  add('ui', 'logo_bow', 6, () => ({
    svg: tube('M-8 -70Q56 0 -8 70', 14, '#B7793F') + ln('M-8 -70Q56 0 -8 70', 4, '#FFFFFF', .35) + ln('M-8 -70L-8 70', 3.5, '#F7F1E1') +
      tube('M-40 0H56', 6, '#D9B27A') + P('M50 -12L78 0L50 12Z', '#C9D2DC', { w: .8 }) + P('M-50 0L-38 -14H-18L-28 0Z', '#FF5FA2', { w: .7 }) + P('M-50 0L-38 14H-18L-28 0Z', '#FFFFFF', { w: .7 }) +
      P(rr(-2, -14, 16, 28, 5), '#5A3A22'),
    box: [-60, -80, 144, 160]
  }), { units: 100 });
  add('ui', 'target_icon', 5, () => ({ svg: board(), box: [-50, -50, 100, 100] }), { units: 100 });
  add('ui', 'medal', 5, () => ({
    svg: P('M-22 -60L-6 -18H6L22 -60Z', '#6D4AFF', { w: .8 }) + P(el(0, 8, 34), '#FFFFFF', { inner: `<path d="${el(0, 8, 26)}" fill="none" stroke="${OL}" stroke-opacity=".12" stroke-width="4"/>`, hi: el(10, -6, 8, 5) }),
    box: [-40, -64, 80, 110]
  }), { units: 100 });

  window.AAProps = { sprites: () => out };
})();
