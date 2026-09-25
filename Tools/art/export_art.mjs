// Archer Arcade art exporter, step 1 of 2: renders the design's vector art to raw PNGs.
//
//   node Tools/art/export_art.mjs        (then: python3 Tools/art/build_art.py)
//
// Loads Design/aa-characters.js (the Claude Design character roster, unmodified on disk; patched in memory to expose
// its drawing kit) plus Tools/art/characters_ext.js and Tools/art/props.js, and renders every rig part, portrait,
// roster picture, arrow, prop and effect sprite with headless Chromium into Tools/.cache/art/raw/.
// build_art.py then adds the 2.5D shading, trims, packs atlases and writes Assets/ArcherArcade/Resources/Art/.
import fs from 'node:fs';
import path from 'node:path';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
let playwright;
try { playwright = require('playwright'); } catch { playwright = require('/opt/node22/lib/node_modules/playwright'); }

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '..', '..');
const RAW = path.join(ROOT, 'Tools', '.cache', 'art', 'raw');

// World scale shared with the runtime (ArcherRig): 140 design units = 1 m at body scale 1; sprites are 200 px per m.
export const UNITS_PER_METER = 140;
export const PX_PER_METER = 200;
// Outline width in sprite pixels (the design draws 6 px at its 1.15 display scale → 7.45 px at ours).
const OUTLINE_PX = 7.45;

function patchedDesign() {
  const src = fs.readFileSync(path.join(ROOT, 'Design', 'aa-characters.js'), 'utf8');
  const hook = 'window.AAChars={board(){';
  if (src.split(hook).length !== 2) throw new Error('aa-characters.js changed: export hook not found');
  const bowHook = 'bow:()=>bowP(ch,D),string:()=>stringP(ch,D)';
  if (src.split(bowHook).length !== 2) throw new Error('aa-characters.js changed: bow hook not found');
  const expose = 'window.AAInternals={OL,P,Pi,ln,tube,G,el,rr,rect,leaf,flame,rot,add,sub,ik,dims,limb,hand,foot,torso,capeP,' +
    'quiverP,bowP,stringSeg,stringP,arrowP,headP,GEAR,partsOf,build,place,wrap,POSES,RANGER,FIRE,ELEC,BOMB,PIP,MOSS,twig,' +
    'BRAMBLE,THORN,WARDEN,setSW:v=>{SW=v},getSW:()=>SW};';
  return src
    .replace(bowHook, "bow:()=>(ch.bow.style==='crossbow'?window.AACrossbow(ch,D):bowP(ch,D)),string:()=>(ch.bow.style==='crossbow'?{svg:'',box:[0,0,1,1]}:stringP(ch,D))")
    .replace(hook, expose + hook);
}

function writePng(file, dataUrl) {
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, Buffer.from(dataUrl.split(',')[1], 'base64'));
}

async function main() {
  fs.rmSync(RAW, { recursive: true, force: true });
  fs.mkdirSync(RAW, { recursive: true });
  const browser = await playwright.chromium.launch();
  const page = await browser.newPage();
  await page.setContent('<!doctype html><html><body></body></html>');
  await page.addScriptTag({ content: patchedDesign() });
  await page.addScriptTag({ path: path.join(HERE, 'characters_ext.js') });
  await page.addScriptTag({ path: path.join(HERE, 'props.js') });
  await page.addScriptTag({
    content: `
      window.renderSvg = async (inner, vb, w, h) => {
        const svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="' + vb.join(' ') + '" width="' + w + '" height="' + h + '">' + inner + '</svg>';
        const img = new Image();
        img.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg);
        await img.decode();
        const c = document.createElement('canvas'); c.width = w; c.height = h;
        c.getContext('2d').drawImage(img, 0, 0, w, h);
        return c.toDataURL('image/png');
      };`
  });

  const manifest = { unitsPerMeter: UNITS_PER_METER, pxPerMeter: PX_PER_METER, looks: [], sprites: [] };
  const looks = await page.evaluate(() => window.AAExport.looks());

  // Rig parts: rendered in part-local units; (0,0) is the joint pivot.
  for (const look of looks) {
    const pxPerUnit = PX_PER_METER / UNITS_PER_METER * look.scale;
    const sw = OUTLINE_PX / pxPerUnit;
    const data = await page.evaluate(([id, s]) => window.AAExport.parts(id, s), [look.id, sw]);
    const entry = { ...data.rig, parts: {} };
    for (const [name, p] of Object.entries(data.parts)) {
      const [bx, by, bw, bh] = p.box;
      const m = Math.max(40, 0.35 * Math.max(bw, bh));
      const vb = [bx - m, by - m, bw + 2 * m, bh + 2 * m];
      const w = Math.ceil(vb[2] * pxPerUnit), h = Math.ceil(vb[3] * pxPerUnit);
      const url = await page.evaluate(([svg, v, W, H]) => window.renderSvg(svg, v, W, H), [p.svg, vb, w, h]);
      const file = path.join(RAW, 'characters', look.id, name + '.png');
      writePng(file, url);
      // Pixel position of the pivot (local 0,0) in the raw image, y down.
      entry.parts[name] = { file: path.relative(RAW, file), originX: -vb[0] * pxPerUnit, originY: -vb[1] * pxPerUnit, pxPerUnit };
    }
    // Portraits (head + gear) for the HUD, roster and profile.
    entry.portraits = {};
    for (const ex of ['normal', 'hurt', 'happy', 'aim']) {
      const pr = await page.evaluate(([id, e]) => window.AAExport.portraitSvg(id, e), [look.id, ex]);
      const [x0, y0, bw, bh] = pr.box;
      const S = Math.max(bw, bh) + 26, cx = x0 + bw / 2, cy = y0 + bh / 2;
      const url = await page.evaluate(([svg, v]) => window.renderSvg(svg, v, 256, 256), [pr.svg, [cx - S / 2, cy - S / 2, S, S]]);
      const file = path.join(RAW, 'portraits', look.id + '_' + ex + '.png');
      writePng(file, url);
      entry.portraits[ex] = path.relative(RAW, file);
    }
    // Whole-body pictures in the design's poses (roster cards, loadout, map, victory screens).
    entry.poses = {};
    const poseNames = ['idle', 'drawing', 'full_draw', 'release', 'hit', 'victory'];
    for (let i = 0; i < poseNames.length; i++) {
      const res = await page.evaluate(([id, k]) => { const P = window.AAExport.POSES[k][1]; return window.AAExport.idleSvg(id, P); }, [look.id, i]);
      const size = look.id === 'forest_warden' ? 1024 : 512;
      const vb = look.id === 'forest_warden' ? [0, 0, 1024, 1024] : [0, 0, 512, 512];
      const url = await page.evaluate(([svg, v, S]) => window.renderSvg(svg, v, S, S), [res.svg, vb, size]);
      const file = path.join(RAW, 'poses', look.id + '_' + poseNames[i] + '.png');
      writePng(file, url);
      entry.poses[poseNames[i]] = path.relative(RAW, file);
    }
    manifest.looks.push(entry);
    console.log('look', look.id);
  }

  // Arrows, props, scenery and effects (props.js): each sprite has its own pivot and pixel density.
  const sprites = await page.evaluate(() => window.AAProps.sprites());
  for (const s of sprites) {
    const [bx, by, bw, bh] = s.box;
    const m = s.margin ?? 24;
    const vb = [bx - m, by - m, bw + 2 * m, bh + 2 * m];
    const pxPerUnit = s.pxPerUnit;
    const w = Math.ceil(vb[2] * pxPerUnit), h = Math.ceil(vb[3] * pxPerUnit);
    const url = await page.evaluate(([svg, v, W, H]) => window.renderSvg(svg, v, W, H), [s.svg, vb, w, h]);
    const file = path.join(RAW, 'sprites', s.group, s.id + '.png');
    writePng(file, url);
    manifest.sprites.push({
      id: s.id, group: s.group, file: path.relative(RAW, file), originX: -vb[0] * pxPerUnit, originY: -vb[1] * pxPerUnit,
      pxPerUnit, pxPerMeter: s.pxPerMeter, shade: s.shade ?? 1, trim: s.trim ?? 1, border: s.border || null
    });
  }
  console.log('sprites', sprites.length);

  fs.writeFileSync(path.join(RAW, 'manifest.json'), JSON.stringify(manifest, null, 1));
  await browser.close();
}

main().catch(e => { console.error(e); process.exit(1); });
