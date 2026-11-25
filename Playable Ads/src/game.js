/* Avoid The Cold — Meta playable ad (portrait).
 * HTML5 port of Assets/Scenes/GamePlay_AvoidTheCold.unity. Rules, numbers and art mirror the C#:
 *   LevelFlow, LevelLoader, LevelData (Level_01..05), DraggableShape, ShapeDropSlot, PuzzleProgressTracker,
 *   CountdownTimer, FreezeMeterValue, FreezeMeterUI, DangerWarningEffect, FrostOverlayEffect, CanvasGroupFader,
 *   MissionResolver, OutsideEnvironmentSeasonSwitcher, AnimatorStateOnPuzzleComplete (Curtain/Daya/Tappu),
 *   HideOnPuzzleComplete (PieceTray, WindlinesStormy), ShowOnPuzzleComplete (ConfettiFullscreen),
 *   ResultScreenUI, ResultBannerIntroAnimation, LevelTutorialSequencer, IdleHandTutorial, HandTutorialUI,
 *   AudioManager.
 * Ad-only changes are marked AD: (portrait layout, no intro story — opens on the core gameplay, shorter result
 * delays, 2 levels, fail -> retry, install end card after the win, DOWNLOAD button, auto-advance). Voice-over is
 * streamed from a server in the Unity build (RuntimeAudioLoader) so it is not available offline.
 */
(() => {
'use strict';

// ───────────────────────── config (scene / prefab / asset values) ─────────────────────────
const CFG = {
  adLevels: [0, 3],               // AD: Level_01 then Level_04 (LevelFlow has 5; all 5 are bundled — change/add indices to play others)
  levelTime: [14, 13],            // AD: freeze-meter time per ad level, 10-15 s (Unity: Level_01 30 s, Level_04 24 s)
  trayVisible: 3,                 // LevelLoader.trayVisibleCount
  trayMaxScale: 0.56,             // LevelLoader.maxPieceScale 0.8 (world) -> design px per source px in the tray
  pieceTint: [168, 217, 242, 0.3137], // LevelLoader.pieceTintColor (0.66,0.85,0.95,0.314)
  loosePieceAlpha: 0.62,          // AD: tray/drag pieces only
  strokeTint: [0, 138, 180],      // Piece.prefab/Outline colour (0,0.542,0.706), scale 1.05 x 1.07
  snapT: 0.35,                    // DraggableShape.snapTweenDuration (Ease.OutBack)
  idleHint: 7, handMove: 0.9, handPause: 0.3, hintPieces: 2, // IdleHandTutorial / HandTutorialUI / LevelTutorialSequencer
  danger: 0.3, barFade: 0.4,      // DangerWarningEffect
  frostStart: 0.5, fader: 1,      // FrostOverlayEffect.rampStartMeterValue, CanvasGroupFader.fadeSpeed
  seasonFade: 0.6,                // OutsideEnvironmentSeasonSwitcher.fadeDuration
  winterShade: 0.44,              // OutsideEnvironmentWinter/Layer colour (0,0,0,0.439)
  flakesAlpha: 0.39,              // ColdFlakes Image alpha
  winDelay: 2.4,                  // AD: ResultScreenUI.showDelaySeconds is 5 (lose = 5/2); shortened for the ad
  loseDelay: 1.4,
  panelT: 0.45, sideT: 0.4,       // ResultBannerIntroAnimation
  bannerHold: 2.6,                // AD: banner stays this long after its intro animation, then the game moves on by itself
                                  //     (win -> next level / install screen, lose -> same level again). No buttons.
};
const INSTALL_LINK = 'https://playschool.page.link/X2cE';
const FONT = "'Fredoka','Arial Rounded MT Bold','Trebuchet MS','Segoe UI',Verdana,sans-serif";
const LEVELS = window.__LEVELS;

// ───────────────────────── helpers ─────────────────────────
const clamp = (v, a, b) => v < a ? a : v > b ? b : v;
const lerp = (a, b, t) => a + (b - a) * t;
const rand = (a, b) => a + Math.random() * (b - a);
const pick = a => a[(Math.random() * a.length) | 0];
const E = {
  linear: t => t, outQuad: t => t * (2 - t), inOutSine: t => -(Math.cos(Math.PI * t) - 1) / 2,
  outCubic: t => 1 - Math.pow(1 - t, 3), smooth: t => t * t * (3 - 2 * t),
  outBack: t => { const c = 1.70158; return 1 + (c + 1) * Math.pow(t - 1, 3) + c * Math.pow(t - 1, 2); },
  outElastic: t => t === 0 || t === 1 ? t : Math.pow(2, -10 * t) * Math.sin((t * 10 - 0.75) * (2 * Math.PI) / 3) + 1,
};
let tweens = [], timers = [];
function tween(dur, fn, { ease = E.linear, delay = 0, done } = {}) { const t = { t: -delay, dur, fn, ease, done }; tweens.push(t); return t; }
function after(sec, fn) { const t = { t: sec, fn }; timers.push(t); return t; }
function kill(t) { if (!t) return; tweens = tweens.filter(x => x !== t); timers = timers.filter(x => x !== t); }

// ───────────────────────── assets / audio ─────────────────────────
const RAW = window.__ASSETS, IMG = {};
function loadImages() {
  return Promise.all(Object.keys(RAW).filter(k => RAW[k].startsWith('data:image')).map(k => new Promise(res => {
    const im = new Image(); im.onload = () => { IMG[k] = im; res(); }; im.onerror = res; im.src = RAW[k];
  })));
}
function loadFont() {
  if (!RAW.font || !window.FontFace || !document.fonts) return Promise.resolve();
  try {
    const f = new FontFace('Fredoka', 'url(' + RAW.font + ')');
    return Promise.race([f.load().then(ff => { document.fonts.add(ff); }), new Promise(r => setTimeout(r, 1500))]).catch(() => {});
  } catch (e) { return Promise.resolve(); }
}
const AC = window.AudioContext || window.webkitAudioContext;
const audio = { ctx: null, buf: {}, master: null, unlocked: false, last: {}, loops: {} };
function initAudio() {
  if (!AC) return;
  try { audio.ctx = new AC(); } catch (e) { return; }
  audio.master = audio.ctx.createGain(); audio.master.gain.value = 1; audio.master.connect(audio.ctx.destination);
  for (const k of Object.keys(RAW)) {
    if (!RAW[k].startsWith('data:audio')) continue;
    const bin = atob(RAW[k].split(',')[1]), arr = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);
    try { audio.ctx.decodeAudioData(arr.buffer, b => { audio.buf[k] = b; }, () => {}); } catch (e) {}
  }
}
function loop(name, vol) { // AudioManager.backgroundSource / BG_Wind (loop)
  if (!audio.ctx || !audio.unlocked) return;
  if (audio.loops[name]) { audio.loops[name].g.gain.setTargetAtTime(vol, audio.ctx.currentTime, 0.25); return; }
  if (!audio.buf[name]) { setTimeout(() => loop(name, vol), 250); return; }
  const s = audio.ctx.createBufferSource(), g = audio.ctx.createGain();
  s.buffer = audio.buf[name]; s.loop = true; g.gain.value = vol; s.connect(g); g.connect(audio.master); s.start();
  audio.loops[name] = { s, g };
}
function loopVol(name, vol, tc = 0.3) { const l = audio.loops[name]; if (l) l.g.gain.setTargetAtTime(vol, audio.ctx.currentTime, tc); }
function sfx(name, { vol = 1, rate = 1, minGap = 0.04 } = {}) {
  if (!audio.ctx || !audio.unlocked || !audio.buf[name]) return;
  const now = audio.ctx.currentTime; if (audio.last[name] && now - audio.last[name] < minGap) return; audio.last[name] = now;
  const s = audio.ctx.createBufferSource(), g = audio.ctx.createGain();
  s.buffer = audio.buf[name]; s.playbackRate.value = rate; g.gain.value = vol; s.connect(g); g.connect(audio.master); s.start();
}
function unlockAudio() {
  if (!audio.ctx || audio.unlocked) return;
  audio.unlocked = true; if (audio.ctx.resume) audio.ctx.resume();
  loop('wind', 0.29);                                   // BG_Wind AudioSource: playOnAwake, loop, volume 0.29
  loop('bgm', 0.45);                                    // AudioManager.PlayBG()
}
document.addEventListener('visibilitychange', () => {
  if (!audio.ctx) return;
  if (document.hidden) audio.ctx.suspend && audio.ctx.suspend(); else if (audio.unlocked) audio.ctx.resume && audio.ctx.resume();
});

// image (or 9-slice panel) + drop shadow rendered once into an offscreen canvas at its on-screen size
const SHADOW = {};
function shadowed(key, w, h, pad, col, blur, ox, oy, draw) {
  const id = key + '|' + Math.round(w) + 'x' + Math.round(h);
  if (SHADOW[id]) return SHADOW[id];
  const c = document.createElement('canvas'), k = Math.min(window.devicePixelRatio || 1, 2) * SCALE;
  c.width = Math.ceil((w + pad * 2) * k); c.height = Math.ceil((h + pad * 2) * k);
  const x = c.getContext('2d'); x.scale(k, k); x.shadowColor = col; x.shadowBlur = blur * k; x.shadowOffsetX = ox * k; x.shadowOffsetY = oy * k;
  if (draw) draw(x, pad, pad, w, h); else if (IMG[key]) x.drawImage(IMG[key], pad, pad, w, h);
  const out = { c, w: w + pad * 2, h: h + pad * 2 };
  return (SHADOW[id] = out);
}
// average colour of an image's top / bottom rows (used to extend the landscape banner art in portrait)
const EDGE = {};
function sampleEdges(key) {
  const im = IMG[key]; if (!im) return;
  try {
    const c = document.createElement('canvas'); c.width = im.width; c.height = im.height; const x = c.getContext('2d'); x.drawImage(im, 0, 0);
    const avg = y => { const d = x.getImageData(0, y, im.width, 2).data; let r = 0, g = 0, b = 0, n = 0; for (let i = 0; i < d.length; i += 4) { r += d[i]; g += d[i + 1]; b += d[i + 2]; n++; } return `${r / n | 0},${g / n | 0},${b / n | 0}`; };
    EDGE[key] = [avg(0), avg(im.height - 2)];
  } catch (e) {}
}
// tinted copies = SpriteRenderer/Image colour multiply
const TINT = {};
function tinted(key, r, g, b) {
  const id = key + '|' + r + ',' + g + ',' + b;
  if (TINT[id]) return TINT[id];
  const im = IMG[key]; if (!im) return null;
  const c = document.createElement('canvas'); c.width = im.width; c.height = im.height;
  const x = c.getContext('2d');
  x.drawImage(im, 0, 0);
  x.globalCompositeOperation = 'multiply'; x.fillStyle = `rgb(${r},${g},${b})`; x.fillRect(0, 0, c.width, c.height);
  x.globalCompositeOperation = 'destination-in'; x.drawImage(im, 0, 0);
  return (TINT[id] = c);
}

// ───────────────────────── layout (portrait design space 1080 x >=1920) ─────────────────────────
const cv = document.getElementById('c'), ctx = cv.getContext('2d');
let W = 1080, H = 1920, SCALE = 1, DPR = 1;
const L = {};
const FRAME_BS = 0.8; // 9-slice border draw scale
const FRAME_B = { l: 190 * 0.62 * FRAME_BS, r: 195 * 0.62 * FRAME_BS, t: 75 * 0.62 * FRAME_BS, b: 75 * 0.62 * FRAME_BS }; // Window_3 spriteBorder (shipped x0.62)
function layout() {
  const sw = window.innerWidth || 360, sh = window.innerHeight || 640;
  DPR = Math.min(window.devicePixelRatio || 1, 2);
  if (sh / sw >= 1920 / 1080) { W = 1080; H = 1080 * sh / sw; } else { H = 1920; W = 1920 * sw / sh; }
  SCALE = sw / W; cv.width = Math.round(sw * DPR); cv.height = Math.round(sh * DPR);
  for (const k in SHADOW) delete SHADOW[k];
  cv.style.width = sw + 'px'; cv.style.height = sh + 'px';
  const ox = (W - 1080) / 2, ex = H - 1920;
  L.ox = ox; L.cx = W / 2;
  L.hud = { y: 34 + Math.min(ex * 0.15, 40) };
  // WindowFrame (sliced) — AD: portrait: window on the left of the wall (opening ~1.42:1, close to the scene's 1.49:1),
  // curtain hanging off its right corner
  const fw = 910, fh = 580;
  L.frame = { x: ox + 16, y: 210 + ex * 0.22 + Math.min(ex * 0.15, 40), w: fw, h: fh };
  L.in = { x: L.frame.x + FRAME_B.l, y: L.frame.y + FRAME_B.t, w: fw - FRAME_B.l - FRAME_B.r, h: fh - FRAME_B.t - FRAME_B.b };
  // floor + furniture (Board2D/BG, Cupboard, Sofa, Mom, Kid)
  L.floorY = H - 600;
  L.sofa = { w: 980, h: 980 * 415 / 913 }; L.sofa.x = ox + 196; L.sofa.y = L.floorY + 176 - L.sofa.h;
  L.pot = { w: 250, h: 250 * 488 / 220 }; L.pot.x = ox - 14; L.pot.y = L.floorY + 150 - L.pot.h;
  L.mat = { w: 640, h: 640 * 262 / 466 }; L.mat.x = ox + 380; L.mat.y = L.floorY + 120;
  const sb = L.sofa.y + L.sofa.h;
  L.daya = { x: ox + 515, y: sb + 6, h: 620 };
  L.tappu = { x: ox + 808, y: sb + 2, h: 525 };
  // PieceTray (Rectangle 3384, sliced 60px) — AD: bottom-centre, wider than the scene's 50% so 3 cells fit portrait
  L.tray = { w: Math.min(1010, W - 40), h: 300 }; L.tray.x = L.cx - L.tray.w / 2; L.tray.y = H - 34 - L.tray.h;
  const pad = 42, sp = 18, n = CFG.trayVisible;
  L.cellW = (L.tray.w - pad * 2 - sp * (n - 1)) / n; L.cellH = L.tray.h - pad * 2;
  L.cells = []; for (let c = 0; c < n; c++) L.cells.push({ x: L.tray.x + pad + L.cellW / 2 + c * (L.cellW + sp), y: L.tray.y + L.tray.h / 2 });
  // window px per source px (852 composite -> interior width)
  L.pxs = L.in.w / 852;
  // curtain: attached on the rod at the window's top-right corner, natural Curtain.png proportions (425x346);
  // it may fly a little past the right edge of the screen
  L.curtain = { x: L.in.x + L.in.w - 24, y: L.frame.y + 12, w: 410, h: 410 * 346 / 425 };
}

// ───────────────────────── game state ─────────────────────────
const G = {
  phase: 'boot', T: 0, levelPos: 0, level: null, slots: [], pieces: [], queue: 0, filled: 0,
  remaining: 0, duration: 1, meter: 1, meterActive: false, resolved: false,
  barDanger: 0, frost: 0, frostTarget: 0, winterA: 1, curtainCalm: 0, calm: 0, trayA: 1, windA: 1,
  firstTouch: -1, tutorialSeen: false, tut: null, idle: 0, hint: null, drag: null,
};
const hand = { on: false, from: null, to: null, t: 0, a: 0 };

function slotRect(s) {
  const d = s.d;
  return { x: L.in.x + (d.cx - d.w / 2) * L.in.w, y: L.in.y + (d.cy - d.h / 2) * L.in.h, w: d.w * L.in.w, h: d.h * L.in.h };
}
function trayRect(p) { // per-cell fit, clamped (LevelLoader.SpawnPieceIntoCell)
  const c = L.cells[p.cell], s = Math.min(L.cellW / p.d.pw, L.cellH / p.d.ph, CFG.trayMaxScale);
  const w = p.d.pw * s, h = p.d.ph * s;
  return { x: c.x - w / 2, y: c.y - h / 2, w, h };
}
function pieceRect(p) {
  if (p.state === 'tray') { const r = trayRect(p); if (p.pop < 1) { const k = E.outBack(p.pop); return { x: r.x + r.w * (1 - k) / 2, y: r.y + r.h * (1 - k) / 2, w: r.w * k, h: r.h * k }; } return r; }
  if (p.state === 'placed') return slotRect(G.slots[p.si]);
  if (p.state === 'snap') { const a = p.from, b = slotRect(G.slots[p.si]), k = E.outBack(clamp(p.st / CFG.snapT, 0, 1)); return { x: lerp(a.x, b.x, k), y: lerp(a.y, b.y, k), w: lerp(a.w, b.w, k), h: lerp(a.h, b.h, k) }; }
  if (p.state === 'back') { const a = p.from, b = trayRect(p), k = E.outCubic(clamp(p.st / 0.18, 0, 1)); return { x: lerp(a.x, b.x, k), y: lerp(a.y, b.y, k), w: b.w, h: b.h }; }
  return p.r; // drag
}
const center = r => ({ x: r.x + r.w / 2, y: r.y + r.h / 2 });

// ───────────────────────── level flow (LevelFlow + LevelLoader.LoadLevel) ─────────────────────────
function loadLevel(pos) {
  G.levelPos = pos; const data = LEVELS[CFG.adLevels[pos]]; G.level = data;
  G.slots = data.pieces.map((d, i) => ({ i, id: d.id, d, filled: false }));
  G.pieces = data.pieces.map((d, i) => ({ i, id: d.id, d, key: d.key, state: 'hidden', cell: -1, si: i, pop: 1 }));
  const vis = Math.min(CFG.trayVisible, data.pieces.length);
  for (let c = 0; c < vis; c++) spawnIntoCell(c, c, false);
  G.queue = vis; G.filled = 0;
  G.duration = CFG.levelTime[pos] || data.time; G.remaining = G.duration; G.meter = 1; G.meterActive = true; G.resolved = false;
  G.barDanger = 0; G.frostTarget = 0;
  G.winterA = 1; G.curtainCalm = 0; G.calm = 0; G.trayA = 1; G.windA = 1;
  Banner.hide(); confetti.length = 0;
  G.phase = 'play'; G.idle = 0; hideHint();
  // LevelTutorialSequencer: first level, first time only, hints the first 2 pieces
  if (pos === 0 && !G.tutorialSeen) { G.tutorialSeen = true; G.tut = { idx: 0 }; showTutorialHint(); }
  else G.tut = null;
  if (pos === 0) Caption.show('Drag the glass pieces to fix the window!', 4.5); // AD: one-line instruction (VO 'Tutorial' is server-streamed)
}
function spawnIntoCell(pi, cell, animate) {
  const p = G.pieces[pi]; p.state = 'tray'; p.cell = cell; p.pop = animate ? 0 : 1;
  if (animate) tween(0.3, t => p.pop = t, { done: () => p.pop = 1 });
}
function onPlaced(p) { // ShapeDropSlot.TryAccept -> PuzzleProgressTracker / LevelLoader reveal
  const s = G.slots[p.si]; s.filled = true; G.filled++;
  if (G.queue < G.pieces.length) { const nx = G.queue++; spawnIntoCell(nx, p.cell, true); }
  G.idle = 0; hideHint(true);
  if (G.tut) { G.tut.idx++; if (G.tut.idx >= Math.min(CFG.hintPieces, G.pieces.length)) { G.tut = null; hideHint(); } else showTutorialHint(); }
  if (G.filled >= G.slots.length) allFilled();
}
function allFilled() { // OnAllSlotsFilled + MissionResolver success
  if (G.resolved) return; G.resolved = true; G.meterActive = false; G.phase = 'resolved';
  hideHint(); G.tut = null; Caption.hide();
  sfx('winsfx', { vol: 0.9 });                                         // AudioManager.Win()
  tween(CFG.seasonFade, t => G.winterA = 1 - t);                       // OutsideEnvironmentSeasonSwitcher crossfade
  tween(0.833, t => G.curtainCalm = t, { ease: E.smooth });            // Curtain: CurtainSwayToNormal
  G.calm = 1;                                                           // DayaSittingIdle / TappuSittingIdle
  tween(0.35, t => { G.trayA = 1 - t; G.windA = 1 - t; });             // HideOnPuzzleComplete (PieceTray, WindlinesStormy)
  G.frostTarget = 0;                                                    // FrostOverlayEffect -> 0
  confettiRain(150); confettiBurst(L.cx, L.in.y + L.in.h / 2, 70);     // ShowOnPuzzleComplete (ConfettiFullscreen)
  loopVol('wind', 0.12, 0.6);                                           // AD: wind eases off once the window is sealed
  after(CFG.winDelay, () => Banner.show('win'));
}
function failed() { // MissionResolver: meter depleted / timer expired
  if (G.resolved) return; G.resolved = true; G.meterActive = false; G.phase = 'resolved';
  hideHint(); G.tut = null; Caption.hide();
  if (G.drag) { const p = G.drag.p; p.from = pieceRect(p); p.state = 'back'; p.st = 0; G.drag = null; }
  after(CFG.loseDelay, () => Banner.show('lose'));
}
function nextLevel() {
  if (G.levelPos + 1 < CFG.adLevels.length) { loadLevel(G.levelPos + 1); loopVol('wind', 0.29, 0.5); }
  else EndCard.show('win');
}

// ───────────────────────── hand tutorial (LevelTutorialSequencer / IdleHandTutorial / HandTutorialUI) ─────────────────────────
function showHintFor(pi) {
  const p = G.pieces[pi]; if (!p || p.state !== 'tray') return false;
  hand.on = true; hand.pi = pi; hand.t = 0; tween(0.2, t => hand.a = Math.max(hand.a, t));
  return true;
}
function showTutorialHint() { if (!showHintFor(G.tut.idx)) { G.tut = null; hideHint(); } }
function hideHint(soft) { hand.on = false; if (!soft) hand.a = 0; }
function idleHint() { // FindNextUnplacedIndex — AD: only pieces currently in the tray (a queued piece has nothing to point at)
  const p = G.pieces.find(q => q.state === 'tray' && !G.slots[q.si].filled);
  if (p) showHintFor(p.i);
}

// ───────────────────────── result banners (ResultScreenUI + ResultBannerIntroAnimation) ─────────────────────────
// AD: no Retry / Next buttons — the banner pops up, holds, and the game continues by itself
const Banner = {
  kind: null, a: 0, panel: 0, side: 0, t: 0, auto: null,
  show(kind) {
    this.kind = kind; this.a = 0; this.panel = 0; this.side = 0; this.t = 0;
    G.phase = 'banner'; sfx('whoosh', { vol: 0.8 });
    tween(1 / CFG.fader, t => this.a = t);                                            // CanvasGroupFader 0 -> 1
    tween(CFG.panelT, t => this.panel = t, { ease: E.outCubic });                     // panel from below
    tween(CFG.sideT, t => this.side = t, { ease: E.outBack, delay: CFG.panelT });     // Tappu from left, Daya from right
    const hold = CFG.panelT + CFG.sideT + CFG.bannerHold;
    if (kind === 'win') {
      confettiRain(90);
      const last = G.levelPos + 1 >= CFG.adLevels.length;
      this.auto = after(hold, () => last ? EndCard.show('win') : (fadeCut(), nextLevel()));   // win -> next level / install screen
    } else {
      this.auto = after(hold, () => { fadeCut(); loadLevel(G.levelPos); loopVol('wind', 0.29, 0.5); }); // lose -> same level again
    }
  },
  hide() { this.kind = null; kill(this.auto); this.auto = null; },
};
// banner -> gameplay: fade out of the banner blue instead of a hard cut
function fadeCut() { G.cut = 1; tween(0.45, t => G.cut = 1 - t); }

// ───────────────────────── install end card + CTA ─────────────────────────
const EndCard = {
  on: false, t: 0, reason: null, a: 0,
  show(reason) {
    if (this.on) return; this.on = true; this.reason = reason; this.t = 0; G.phase = 'end';
    Banner.hide(); hideHint(); Caption.hide(); tween(0.4, t => this.a = t); sfx('whoosh', { vol: 0.6 });
    loopVol('wind', 0, 0.5);
  },
};
function cta() {
  /* Install link (Playschool dynamic link). INSIDE Meta the click goes through FbPlayableAd.onCTAClick() and the destination
     is the ad's own URL in Ads Manager — set it to INSTALL_LINK there. The anchor fallback only runs outside Meta
     (standalone preview / other networks), where FbPlayableAd does not exist. */
  sfx('pop');
  try {
    if (window.FbPlayableAd && window.FbPlayableAd.onCTAClick) window.FbPlayableAd.onCTAClick();
    else { const a = document.createElement('a'); a.href = INSTALL_LINK; a.target = '_blank'; a.rel = 'noopener'; document.body.appendChild(a); a.click(); a.remove(); }
  } catch (e) {}
}

// AD: short on-screen line (stands in for the server-streamed tutorial VO)
const Caption = {
  str: '', a: 0, timer: null,
  show(str, sec) { this.str = str; kill(this.timer); tween(0.3, t => this.a = Math.max(this.a, t)); this.timer = after(sec, () => this.hide()); },
  hide() { if (this.a <= 0) return; const a0 = this.a; tween(0.3, t => this.a = a0 * (1 - t)); },
};

// ───────────────────────── particles (Epic Toon FX StarBurst2D, WindlinesStormy, ConfettiFullscreen) ─────────────────────────
const parts = [], winds = [], confetti = [];
const CONF_COL = ['#ff4d6d', '#ffd23f', '#3bceac', '#4d96ff', '#b388ff', '#ff9f1c', '#ffffff'];
function starBurst(x, y) {
  for (let i = 0; i < 12; i++) { const a = i / 12 * Math.PI * 2 + rand(-0.2, 0.2), v = rand(380, 720); parts.push({ k: 'star', x, y, vx: Math.cos(a) * v, vy: Math.sin(a) * v, life: rand(0.5, 0.8), age: 0, s: rand(34, 60), rot: rand(0, 6), vr: rand(-6, 6) }); }
  for (let i = 0; i < 10; i++) { const a = rand(0, 6.28), v = rand(120, 380); parts.push({ k: 'spark', x, y, vx: Math.cos(a) * v, vy: Math.sin(a) * v, life: rand(0.4, 0.7), age: 0, s: rand(40, 80), rot: rand(0, 6), vr: rand(-3, 3) }); }
  parts.push({ k: 'ring', x, y, vx: 0, vy: 0, life: 0.45, age: 0, s: 0 });
}
function confettiRain(n) { for (let i = 0; i < n; i++) confetti.push({ x: rand(0, W), y: rand(-H * 0.6, -20), vx: rand(-60, 60), vy: rand(260, 520), rot: rand(0, 6), vr: rand(-8, 8), s: rand(16, 30), c: pick(CONF_COL), life: 5, age: 0 }); }
function confettiBurst(x, y, n) { for (let i = 0; i < n; i++) { const a = rand(-Math.PI, 0), v = rand(500, 1300); confetti.push({ x, y, vx: Math.cos(a) * v, vy: Math.sin(a) * v, rot: rand(0, 6), vr: rand(-10, 10), s: rand(16, 30), c: pick(CONF_COL), life: 4, age: 0 }); } }
function spawnWind() { // streaks blow in through the broken window across the room
  const r = L.in; winds.push({ x: r.x + rand(-60, r.w * 0.55), y: r.y + rand(r.h * 0.12, r.h * 1.15), len: rand(150, 300), v: rand(650, 1050), age: 0, life: rand(0.9, 1.4), amp: rand(8, 18), ph: rand(0, 6), w: rand(3, 5) });
}

// ───────────────────────── input (DraggableShape) ─────────────────────────
const toL = e => { const r = cv.getBoundingClientRect(); return { x: (e.clientX - r.left) / SCALE, y: (e.clientY - r.top) / SCALE }; };
const inR = (p, r, m = 0) => p.x >= r.x - m && p.x <= r.x + r.w + m && p.y >= r.y - m && p.y <= r.y + r.h + m;
cv.addEventListener('pointerdown', e => {
  unlockAudio(); if (G.firstTouch < 0) G.firstTouch = G.T;
  const pp = toL(e);
  if (EndCard.on) { if (EndCard.t > 0.4) cta(); return; }
  if (L.dl && inR(pp, L.dl, 6)) { cta(); return; }   // DOWNLOAD NOW
  if (G.phase === 'banner') return;                  // AD: banners take no input
  if (G.phase !== 'play') return;
  G.idle = 0; if (hand.on && !G.tut) hideHint();     // IdleHandTutorial: any touch hides the idle hint
  // topmost tray piece under the finger (BoxCollider2D.OverlapPoint) — AD: +18px touch slop for small fingers on phones
  for (let k = G.pieces.length - 1; k >= 0; k--) {
    const p = G.pieces[k]; if (p.state !== 'tray') continue;
    const r = pieceRect(p); if (!inR(pp, r, 18)) continue;
    p.state = 'drag'; p.r = { ...r }; G.drag = { p, ox: r.x - pp.x, oy: r.y - pp.y, id: e.pointerId };
    try { cv.setPointerCapture(e.pointerId); } catch (_) {}
    if (G.tut) hand.a = 0.35;                         // tutorial hand dims while the player drags
    return;
  }
});
cv.addEventListener('pointermove', e => {
  if (!G.drag || e.pointerId !== G.drag.id) return;
  const pp = toL(e), p = G.drag.p; p.r.x = pp.x + G.drag.ox; p.r.y = pp.y + G.drag.oy;
});
function endDrag(e) {
  if (!G.drag || (e && e.pointerId !== G.drag.id)) return;
  const p = G.drag.p; G.drag = null;
  if (G.tut) hand.a = 1;
  // FindOverlappingSlot: among slots the piece's bounds touch, the one whose centre is nearest the piece's centre
  const r = p.r, c = center(r); let best = null, bd = 1e9;
  for (const s of G.slots) {
    const sr = slotRect(s);
    if (r.x > sr.x + sr.w || r.x + r.w < sr.x || r.y > sr.y + sr.h || r.y + r.h < sr.y) continue;
    const sc = center(sr), d = (sc.x - c.x) ** 2 + (sc.y - c.y) ** 2; if (d < bd) { bd = d; best = s; }
  }
  if (best && !best.filled && best.id === p.id && G.phase === 'play') { // TryAccept -> SnapToSlot
    p.from = { ...r }; p.state = 'snap'; p.st = 0; sfx('connect', { vol: 0.8 });
    const sc = center(slotRect(best)); starBurst(sc.x, sc.y);
    onPlaced(p);
  } else { p.from = { ...r }; p.state = 'back'; p.st = 0; }  // ReturnToStart
}
cv.addEventListener('pointerup', endDrag);
cv.addEventListener('pointercancel', endDrag);

// ───────────────────────── update ─────────────────────────
function update(dt) {
  G.T += dt;
  for (const t of tweens.slice()) { t.t += dt; if (t.t < 0) continue; const k = clamp(t.t / t.dur, 0, 1); t.fn(t.ease(k)); if (k >= 1) { tweens.splice(tweens.indexOf(t), 1); t.done && t.done(); } }
  for (const t of timers.slice()) { t.t -= dt; if (t.t <= 0) { timers.splice(timers.indexOf(t), 1); t.fn(); } }
  if (EndCard.on) EndCard.t += dt; if (Banner.kind) Banner.t += dt;
  // CountdownTimer + FreezeMeterValue (meter -= dt / duration); MissionResolver fails on whichever hits 0 first
  if (G.phase === 'play' && G.meterActive && G.firstTouch >= 0) { // AD: the freeze timer starts on the viewer's first touch
    G.remaining = Math.max(0, G.remaining - dt); G.meter = clamp(G.meter - dt / G.duration, 0, 1);
    G.frostTarget = clamp((CFG.frostStart - G.meter) / CFG.frostStart, 0, 1);  // InverseLerp(0.5, 0, v)
    if (G.meter <= 0 || G.remaining <= 0) failed();
  }
  if (G.phase === 'resolved' && G.filled >= G.slots.length) G.frostTarget = 0;
  // CanvasGroupFader.MoveTowards(alpha, target, fadeSpeed * dt)
  G.frost += clamp(G.frostTarget - G.frost, -CFG.fader * dt, CFG.fader * dt);
  G.barDanger = clamp(G.barDanger + (G.meter <= CFG.danger && !G.resolved || (G.resolved && G.meter <= CFG.danger) ? 1 : -1) * dt / CFG.barFade, 0, 1);
  // pieces
  for (const p of G.pieces) {
    if (p.state === 'snap') { p.st += dt; if (p.st >= CFG.snapT) p.state = 'placed'; }
    else if (p.state === 'back') { p.st += dt; if (p.st >= 0.18) p.state = 'tray'; }
  }
  // tutorial / idle hint
  if (hand.on) {
    const p = G.pieces[hand.pi];
    if (!p || (p.state !== 'tray' && p.state !== 'drag')) { if (!G.tut) hideHint(); }
    hand.t += dt;
  } else if (G.phase === 'play' && !G.tut && !G.drag) {
    G.idle += dt; if (G.idle >= CFG.idleHint) { G.idle = 0; idleHint(); }
  }
  // fx
  for (let i = parts.length - 1; i >= 0; i--) { const p = parts[i]; p.age += dt; if (p.age >= p.life) { parts.splice(i, 1); continue; } p.x += p.vx * dt; p.y += p.vy * dt; p.vx *= 0.92; p.vy *= 0.92; p.rot += (p.vr || 0) * dt; }
  for (let i = confetti.length - 1; i >= 0; i--) { const c = confetti[i]; c.age += dt; if (c.age >= c.life || c.y > H + 60) { confetti.splice(i, 1); continue; } c.vy = Math.min(c.vy + 900 * dt, 560); c.vx *= 0.985; c.x += c.vx * dt + Math.sin(c.age * 4 + c.rot) * 40 * dt; c.y += c.vy * dt; c.rot += c.vr * dt; }
  if (G.phase === 'play' && G.windA > 0 && Math.random() < dt * 3.5) spawnWind(); // AD: lighter than the scene's stormy windlines
  for (let i = winds.length - 1; i >= 0; i--) { const w = winds[i]; w.age += dt; if (w.age >= w.life) { winds.splice(i, 1); continue; } w.x += w.v * dt; }
}

// ───────────────────────── drawing helpers ─────────────────────────
function img(k, x, y, w, h, a = 1) { const im = typeof k === 'string' ? IMG[k] : k; if (!im || a <= 0) return; if (a < 1) { ctx.globalAlpha *= a; ctx.drawImage(im, x, y, w, h); ctx.globalAlpha /= a; } else ctx.drawImage(im, x, y, w, h); }
function cover(k, x, y, w, h, ax = 0.5, ay = 0.5) { const im = IMG[k]; if (!im) return; const s = Math.max(w / im.width, h / im.height), dw = im.width * s, dh = im.height * s; ctx.save(); ctx.beginPath(); ctx.rect(x, y, w, h); ctx.clip(); ctx.drawImage(im, x + (w - dw) * ax, y + (h - dh) * ay, dw, dh); ctx.restore(); }
function slice9(k, x, y, w, h, b, bs = 1, cx = ctx) { // Image.Type.Sliced
  const im = typeof k === 'string' ? IMG[k] : k; if (!im) return;
  const [l, t, r, bo] = b, sw = im.width, sh = im.height, L2 = l * bs, T2 = t * bs, R2 = r * bs, B2 = bo * bs;
  const xs = [0, l, sw - r, sw], ys = [0, t, sh - bo, sh], dx = [x, x + L2, x + w - R2, x + w], dy = [y, y + T2, y + h - B2, y + h];
  for (let i = 0; i < 3; i++) for (let j = 0; j < 3; j++) {
    const sW = xs[i + 1] - xs[i], sH = ys[j + 1] - ys[j], dW = dx[i + 1] - dx[i], dH = dy[j + 1] - dy[j];
    if (sW > 0 && sH > 0 && dW > 0 && dH > 0) cx.drawImage(im, xs[i], ys[j], sW, sH, dx[i], dy[j], dW + (i < 2 ? 0.5 : 0), dH + (j < 2 ? 0.5 : 0));
  }
}
function rr(x, y, w, h, r) { ctx.beginPath(); ctx.moveTo(x + r, y); ctx.arcTo(x + w, y, x + w, y + h, r); ctx.arcTo(x + w, y + h, x, y + h, r); ctx.arcTo(x, y + h, x, y, r); ctx.arcTo(x, y, x + w, y, r); ctx.closePath(); }
function text(str, x, y, size, fill, stroke, align = 'center', sw = 0.18, weight = 600) {
  ctx.font = `${weight} ${size}px ${FONT}`; ctx.textAlign = align; ctx.textBaseline = 'middle'; ctx.lineJoin = 'round';
  if (stroke) { ctx.strokeStyle = stroke; ctx.lineWidth = size * sw; ctx.strokeText(str, x, y); }
  ctx.fillStyle = fill; ctx.fillText(str, x, y);
}
function wrap(str, maxW, size, weight = 500) {
  ctx.font = `${weight} ${size}px ${FONT}`; const words = str.split(' '), lines = []; let cur = '';
  for (const w of words) { const t = cur ? cur + ' ' + w : w; if (ctx.measureText(t).width > maxW && cur) { lines.push(cur); cur = w; } else cur = t; }
  if (cur) lines.push(cur); return lines;
}

// ───────────────────────── scene drawing ─────────────────────────
function drawRoom() {
  // Board2D/BG = Room_2: wallpaper + skirting + floor (sampled colours)
  cover('wall', 0, 0, W, L.floorY + 2, 0.5, 1);
  ctx.fillStyle = '#f96979'; ctx.fillRect(0, L.floorY, W, 62);
  ctx.fillStyle = '#fec479'; ctx.fillRect(0, L.floorY + 62, W, H - L.floorY - 62);
}
function drawWindow(T) {
  const f = L.frame, r = L.in;
  slice9('frame', f.x, f.y, f.w, f.h, [190 * 0.62, 75 * 0.62, 195 * 0.62, 75 * 0.62], FRAME_BS);
  // OutsideEnvironmentWinter (Donkey_BG + 44% black layer) crossfading to OutsideEnvironmentNormal (+ glass shades)
  ctx.save(); ctx.beginPath(); ctx.rect(r.x, r.y, r.w, r.h); ctx.clip();
  img('outside', r.x - 1, r.y - 1, r.w + 2, r.h + 2);
  if (G.winterA > 0) {
    ctx.fillStyle = `rgba(6,14,38,${CFG.winterShade * G.winterA})`; ctx.fillRect(r.x, r.y, r.w, r.h);
    ctx.fillStyle = `rgba(150,200,255,${0.16 * G.winterA})`; ctx.fillRect(r.x, r.y, r.w, r.h); // cold cast
  }
  if (G.winterA < 1) img(tinted('shades', 177, 254, 255), r.x, r.y, r.w, r.h, 0.5 * (1 - G.winterA));
  ctx.restore();
  // slots (dashed outline sprites)
  for (const s of G.slots) { const sr = slotRect(s); img(s.d.key ? s.d.key + '_slot' : null, sr.x, sr.y, sr.w, sr.h, 0.95); }
}
function drawPiece(p, r, stroke) {
  const [cr, cg, cb, ca] = CFG.pieceTint;
  // AD: loose pieces (tray / drag) use a stronger glass tint so they read on small portrait screens; placed panes keep 0.314
  img(tinted(p.key + '_piece', cr, cg, cb), r.x, r.y, r.w, r.h, stroke ? CFG.loosePieceAlpha : ca);
  if (stroke) { const [sr, sg, sb] = CFG.strokeTint, ew = r.w * 0.05, eh = r.h * 0.07; img(tinted(p.key + '_stroke', sr, sg, sb), r.x - ew / 2, r.y - eh / 2, r.w + ew, r.h + eh); }
}
function drawPlacedPieces() { for (const p of G.pieces) if (p.state === 'placed' || p.state === 'snap') drawPiece(p, pieceRect(p), false); }
function drawCurtain(T) { // Curtain prefab: 4-bone chain, CurtainSway (1.67s loop) -> CurtainSwayToNormal
  const im = IMG.curtain; if (!im) return;
  const c = L.curtain, calm = G.curtainCalm, amp = lerp(1, 0.18, calm);
  const sway = (Math.sin(T * 0.6 * 6.283) * 4 + Math.sin(T * 1.7 * 6.283) * 1.5) * amp; // CurtainWaveEffect values
  // the cloth hangs from its top-left (rod) and ripples more towards the hem
  const N = 26, sh = im.height / N, dh = c.h / N;
  ctx.save(); ctx.translate(c.x, c.y); ctx.rotate((sway * 0.6 - calm * 3) * Math.PI / 180);
  const billow = 1 + Math.sin(T * 0.8 * 6.283) * 0.03 * amp;
  for (let i = 0; i < N; i++) {
    const v = i / N, k = Math.pow(v, 1.3);
    const dx = (Math.sin(T * 6.283 / 1.667 - v * 3.4) * 16 + Math.sin(T * 9 - v * 7) * 3) * k * amp;
    ctx.drawImage(im, 0, i * sh, im.width, sh + 1, dx, i * dh, c.w * (1 + (billow - 1) * k), dh + 1.2);
  }
  ctx.restore();
}
function drawWinds() {
  if (G.windA <= 0) return;
  ctx.save(); ctx.lineCap = 'round';
  for (const w of winds) {
    const k = w.age / w.life, a = Math.sin(k * Math.PI) * 0.34 * G.windA;
    ctx.strokeStyle = `rgba(255,255,255,${a})`; ctx.lineWidth = w.w * (1 - k * 0.5);
    ctx.beginPath();
    for (let s = 0; s <= 10; s++) { const xx = w.x - w.len * (1 - s / 10), yy = w.y + Math.sin(xx * 0.012 + w.ph) * w.amp; if (s === 0) ctx.moveTo(xx, yy); else ctx.lineTo(xx, yy); }
    ctx.stroke();
  }
  ctx.restore();
}
function drawChar(base, sx, x, y, h, T, period, phase) {
  const s = IMG[base + (G.calm ? '_i' : '_s')]; if (!s) return;
  const w = h * s.width / s.height;
  // contact shadow on the floor under the feet
  ctx.save(); ctx.translate(x, y - 6); ctx.scale(1, 0.22);
  const g = ctx.createRadialGradient(0, 0, 0, 0, 0, w * 0.5);
  g.addColorStop(0, 'rgba(40,20,60,0.38)'); g.addColorStop(0.7, 'rgba(40,20,60,0.16)'); g.addColorStop(1, 'rgba(40,20,60,0)');
  ctx.fillStyle = g; ctx.beginPath(); ctx.arc(0, 0, w * 0.5, 0, 6.283); ctx.fill(); ctx.restore();
  ctx.save(); ctx.translate(x, y);
  if (!G.calm) { // DayaShivering (0.75s) / TappuShivering (1s) bone loops -> gentle shiver (chills are in the sprite)
    const t = T + phase;
    ctx.translate(Math.sin(t * 6.283 * 9) * 1.1 + Math.sin(t * 6.283 / period) * 0.8, 0);
    ctx.rotate((Math.sin(t * 6.283 * 7) * 0.25 + Math.sin(t * 6.283 / period) * 0.3) * Math.PI / 180);
    ctx.scale(1, 1 - Math.abs(Math.sin(t * 6.283 / period)) * 0.005);
  } else { // SittingIdle (1.25s)
    ctx.scale(1, 1 + Math.sin(T * 6.283 / 1.25 + phase) * 0.008);
  }
  // soft drop shadow against the sofa (pre-rendered once — a live canvas blur every frame is slow on phones)
  const pad = 48, c = shadowed(base + (G.calm ? '_i' : '_s'), w, h, pad, 'rgba(30,20,70,0.28)', 22, 10, 8);
  ctx.drawImage(c.c, -w / 2 - pad, -h - pad, c.w, c.h);
  ctx.restore();
}
function drawFurniture(T) {
  img('mat', L.mat.x, L.mat.y, L.mat.w, L.mat.h);
  img('pot', L.pot.x, L.pot.y, L.pot.w, L.pot.h);
  img('sofa', L.sofa.x, L.sofa.y, L.sofa.w, L.sofa.h);
  drawChar('daya', 0, L.daya.x, L.daya.y, L.daya.h, T, 0.75, 0);
  drawChar('tappu', 0, L.tappu.x, L.tappu.y, L.tappu.h, T, 1, 0.37);
}
function drawTray() {
  if (G.trayA <= 0) return;
  const t = L.tray; ctx.save(); ctx.globalAlpha = G.trayA;
  const tc = shadowed('tray', t.w, t.h, 40, 'rgba(40,20,80,0.25)', 24, 0, 8, (x, px, py, w, h) => slice9('tray', px, py, w, h, [60, 60, 60, 60], 1, x));
  ctx.drawImage(tc.c, t.x - 40, t.y - 40, tc.w, tc.h);
  ctx.restore();
  ctx.save(); ctx.globalAlpha = G.trayA;
  for (const p of G.pieces) if (p.state === 'tray' || p.state === 'back') drawPiece(p, pieceRect(p), true);
  ctx.restore();
}
function drawFrost() { // FrostOverlay + ColdFlakes (screen-space)
  if (G.frost <= 0.001) return;
  img(tinted('frost', 101, 215, 255), 0, 0, W, H, G.frost);
  img('flakes', 0, 0, W, H, CFG.flakesAlpha * G.frost * 2.2);
}
function drawHUD(T) {
  const y = L.hud.y;
  if (G.level) {
    // HUD/FreezeBG (border.png sliced) + FreezeMeterBar (Rectangle 3396, horizontal fill) + snowflake
    const bx = L.ox + 70, bw = 520, bh = 58, by = y + 34;
    const bc = shadowed('border', bw, bh, 20, 'rgba(0,0,0,0.25)', 10, 0, 4, (x, px, py, w, h) => slice9('border', px, py, w, h, [60, 60, 60, 60], 0.45, x));
    ctx.drawImage(bc.c, bx - 20, by - 20, bc.w, bc.h);
    const ix = bx + 10, iw = bw - 20, ih = bh - 18, iy = by + 9;
    const col = [lerp(145, 230, G.barDanger), lerp(210, 51, G.barDanger), lerp(232, 51, G.barDanger)].map(Math.round); // normal (0.57,0.82,0.91) -> danger (0.9,0.2,0.2)
    if (G.meter > 0) { ctx.save(); ctx.beginPath(); ctx.rect(ix, iy, iw * G.meter, ih); ctx.clip(); img(tinted('bar', col[0], col[1], col[2]), ix, iy, iw, ih); ctx.restore(); }
    const pulse = G.barDanger > 0.5 && !G.resolved ? 1 + 0.08 * Math.sin(T * 14) : 1, ss = 96 * pulse;
    const sc = shadowed('snow', 96, 96, 16, 'rgba(0,60,120,0.45)', 10, 0, 0), sk = ss / 96;
    ctx.drawImage(sc.c, bx - 52 - (ss - 96) / 2 - 16 * sk, by + bh / 2 - ss / 2 - 16 * sk, sc.w * sk, sc.h * sk);
  }
  // AD: DOWNLOAD NOW (Playschool kit) — top-right, every screen except the install end card
  if (!EndCard.on && IMG.dlbtn) {
    const dw = 300, dh = dw * IMG.dlbtn.height / IMG.dlbtn.width, dx = W - 26 - dw, dy = y + 4, pl = 1 + 0.035 * Math.sin(T * 5);
    L.dl = { x: dx, y: dy, w: dw, h: dh };
    ctx.save(); ctx.translate(dx + dw / 2, dy + dh / 2); ctx.scale(pl, pl); const dc = shadowed('dlbtn', dw, dh, 24, 'rgba(0,0,0,0.35)', 12, 0, 5); ctx.drawImage(dc.c, -dw / 2 - 24, -dh / 2 - 24, dc.w, dc.h); ctx.restore();
  } else L.dl = null;
}
function drawHand(T) {
  if (!hand.on || hand.a <= 0) return;
  const p = G.pieces[hand.pi]; if (!p) return;
  const from = center(p.state === 'drag' ? p.r : trayRect(p)), to = center(slotRect(G.slots[p.si]));
  const cyc = CFG.handMove + CFG.handPause * 2, t = hand.t % cyc;
  const k = t < CFG.handPause ? 0 : t > CFG.handPause + CFG.handMove ? 1 : (t - CFG.handPause) / CFG.handMove; // HandTutorialUI.LoopBetween (linear lerp)
  const x = lerp(from.x, to.x, k), y = lerp(from.y, to.y, k), im = IMG.hand; if (!im) return;
  const hs = 170, press = t < CFG.handPause ? 1 - 0.1 * Math.sin(t / CFG.handPause * Math.PI) : 1;
  ctx.save(); ctx.globalAlpha = 0.95 * hand.a; ctx.translate(x, y); ctx.scale(press, press); ctx.rotate(-0.15);
  const hc = shadowed('hand', hs, hs, 28, 'rgba(0,0,0,0.35)', 14, 0, 8);
  ctx.drawImage(hc.c, -hs * 0.38 - 28, -hs * 0.05 - 28, hc.w, hc.h); ctx.restore();
}
function drawCaption() {
  if (Caption.a <= 0.01 || !Caption.str) return;
  const y = L.frame.y + L.frame.h + 70, lines = wrap(Caption.str, 900, 50, 600);
  ctx.save(); ctx.globalAlpha = Caption.a;
  ctx.font = `600 50px ${FONT}`; const tw = Math.max(...lines.map(l => ctx.measureText(l).width)) + 70, th = lines.length * 60 + 34;
  ctx.fillStyle = 'rgba(30,20,70,0.72)'; rr(L.cx - tw / 2, y - th / 2, tw, th, 34); ctx.fill();
  lines.forEach((l, i) => text(l, L.cx, y - (lines.length - 1) * 30 + i * 60, 50, '#ffffff', null));
  ctx.restore();
}
function drawFx() {
  for (const p of parts) {
    const k = p.age / p.life;
    if (p.k === 'ring') { ctx.strokeStyle = `rgba(255,255,255,${1 - k})`; ctx.lineWidth = 14 * (1 - k) + 2; ctx.beginPath(); ctx.arc(p.x, p.y, lerp(30, 190, E.outCubic(k)), 0, 6.283); ctx.stroke(); continue; }
    const im = p.k === 'star' ? IMG.star : IMG.sparkle; if (!im) continue;
    const s = p.s * (p.k === 'star' ? (k < 0.2 ? k / 0.2 : 1 - (k - 0.2) * 0.6) : 1 - k * 0.5);
    ctx.save(); ctx.globalAlpha = 1 - k * k; ctx.translate(p.x, p.y); ctx.rotate(p.rot); ctx.drawImage(im, -s / 2, -s / 2, s, s); ctx.restore();
  }
  ctx.globalAlpha = 1;
  for (const c of confetti) { ctx.save(); ctx.translate(c.x, c.y); ctx.rotate(c.rot); ctx.scale(1, Math.cos(c.rot * 2)); ctx.fillStyle = c.c; ctx.fillRect(-c.s / 2, -c.s / 4, c.s, c.s / 2); ctx.restore(); }
}
function drawBanner(T) {
  const b = Banner; if (!b.kind) return;
  const win = b.kind === 'win', bg = win ? 'win_bg' : 'lose_bg', im = IMG[bg];
  // AD portrait: WinImage / tryAgain are landscape; the centre crop is fitted to the width and the rest of the
  // screen is extended with the art's own edge colours (sampled at load) so the banner is never cropped.
  const s = W / (im ? im.width : 608), dw = (im ? im.width : 608) * s, dh = (im ? im.height : 753) * s;
  const cyB = H * 0.33, top = cyB - dh / 2; // banner art sits high, characters stand below it
  const off = (1 - b.panel) * H; // panel slides up from below by its height (anchoredPosition + down * rect.height)
  ctx.save(); ctx.globalAlpha = b.a; ctx.translate(0, off);
  const ec = EDGE[bg] || ['27,95,224', '11,63,192'];
  ctx.fillStyle = `rgb(${ec[0]})`; ctx.fillRect(0, -off, W, top + off + 2);
  ctx.fillStyle = `rgb(${ec[1]})`; ctx.fillRect(0, top + dh - 2, W, H - top - dh + 2);
  if (im) ctx.drawImage(im, L.cx - dw / 2, top, dw, dh);
  // feather the art's top / bottom edges into the extension colour (no visible seam)
  const fb = ctx.createLinearGradient(0, top + dh - 160, 0, top + dh);
  fb.addColorStop(0, `rgba(${ec[1]},0)`); fb.addColorStop(1, `rgb(${ec[1]})`); ctx.fillStyle = fb; ctx.fillRect(0, top + dh - 160, W, 161);
  const ft = ctx.createLinearGradient(0, top, 0, top + 120);
  ft.addColorStop(0, `rgb(${ec[0]})`); ft.addColorStop(1, `rgba(${ec[0]},0)`); ctx.fillStyle = ft; ctx.fillRect(0, top - 1, W, 121);
  ctx.restore();
  // side characters: Tappu from the left, Daya from the right
  const tk = win ? 'win_tappu' : 'lose_tappu', dk = win ? 'win_daya' : 'lose_daya';
  const ti = IMG[tk], di = IMG[dk], base = H - 200;
  ctx.save(); ctx.globalAlpha = Math.min(1, b.a * 1.5);
  if (ti) { const h = win ? 600 : 560, w = h * ti.width / ti.height, x = L.ox + 40 - (1 - b.side) * (w + 80), bob = win ? Math.sin(T * 5) * 8 : Math.sin(T * 60) * 2.5;
    ctx.fillStyle = 'rgba(0,0,0,0.3)'; ctx.beginPath(); ctx.ellipse(x + w * 0.5, base - 6, w * 0.38, 22, 0, 0, 6.283); ctx.fill();
    ctx.drawImage(ti, x + (win ? 0 : bob), base - h + (win ? -Math.abs(bob) : 0), w, h); }
  if (di) { const h = win ? 680 : 680, w = h * di.width / di.height, x = L.ox + 1080 - 40 - w + (1 - b.side) * (w + 80), bob = win ? Math.sin(T * 5 + 1.3) * 8 : Math.sin(T * 55 + 1) * 2.5;
    ctx.fillStyle = 'rgba(0,0,0,0.3)'; ctx.beginPath(); ctx.ellipse(x + w * 0.52, base - 6, w * 0.36, 22, 0, 0, 6.283); ctx.fill();
    ctx.drawImage(di, x + (win ? 0 : bob), base - h + (win ? -Math.abs(bob) : 0), w, h); }
  ctx.restore();
}
/* ───────── install end screen (Playschool): layout in the 1080x1920 design space of the reference end slate ───────── */
const END_LAYOUT = { icon: { x: 288, y: 301, s: 507 }, text: { x: 120, y: 880 }, btn: { x: 194, y: 1124, w: 691, h: 233 } };
function drawEndCard(T) {
  const a = EndCard.a, t = EndCard.t;
  ctx.globalAlpha = a;
  const bgS = Math.max(W / 1080, H / 1920), es = Math.min(W / 1080, H / 1920);
  const bg = IMG.endbg; if (bg) ctx.drawImage(bg, L.cx - 540 * bgS, H / 2 - 960 * bgS, 1080 * bgS, 1920 * bgS); else { ctx.fillStyle = '#b48cec'; ctx.fillRect(0, 0, W, H); }
  ctx.save(); ctx.translate(L.cx, H / 2); ctx.scale(es, es); ctx.translate(-540, -960);
  const I = END_LAYOUT.icon, ip = E.outBack(clamp(t / 0.7, 0, 1)), bob = Math.sin(T * 2.2) * 8;
  if (IMG.shine) { ctx.save(); ctx.translate(I.x + I.s / 2, I.y + I.s / 2); ctx.rotate(T * 0.5); const ss = 900 * ip; ctx.globalAlpha = a * 0.55; ctx.drawImage(IMG.shine, -ss / 2, -ss / 2, ss, ss); ctx.restore(); ctx.globalAlpha = a; }
  if (IMG.endicon) { ctx.save(); ctx.translate(I.x + I.s / 2, I.y + I.s / 2 + bob); ctx.scale(ip, ip); ctx.rotate(Math.sin(T * 1.7) * 0.025); ctx.shadowColor = 'rgba(60,20,110,0.35)'; ctx.shadowBlur = 40; ctx.shadowOffsetY = 18; rr(-I.s / 2, -I.s / 2, I.s, I.s, I.s * 0.2); ctx.clip(); ctx.drawImage(IMG.endicon, -I.s / 2, -I.s / 2, I.s, I.s); ctx.restore(); }
  const tp = E.outCubic(clamp((t - 0.25) / 0.6, 0, 1));
  if (IMG.endtext) { ctx.globalAlpha = a * tp; ctx.drawImage(IMG.endtext, END_LAYOUT.text.x, END_LAYOUT.text.y + (1 - tp) * 40); ctx.globalAlpha = a; }
  const B = END_LAYOUT.btn, bp = E.outElastic(clamp((t - 0.55) / 0.8, 0, 1)), pulse = 1 + 0.06 * Math.sin(T * 6);
  if (IMG.endbtn) { ctx.save(); ctx.translate(B.x + B.w / 2, B.y + B.h / 2); ctx.scale(bp * pulse, bp * pulse); ctx.shadowColor = 'rgba(60,20,110,0.35)'; ctx.shadowBlur = 24; ctx.shadowOffsetY = 10; ctx.drawImage(IMG.endbtn, -B.w / 2, -B.h / 2, B.w, B.h); ctx.restore(); }
  if (IMG.hand && t > 0.9) { const hc = (T % 1.2) / 1.2, hs = 190; ctx.save(); ctx.translate(B.x + B.w * 0.72, B.y + B.h * 0.85 + Math.sin(hc * Math.PI * 2) * 14); ctx.rotate(-0.3); ctx.drawImage(IMG.hand, -hs * 0.38, -hs * 0.05, hs, hs); ctx.restore(); }
  ctx.restore();
  ctx.globalAlpha = 1;
}
function render() {
  const T = G.T;
  ctx.setTransform(DPR * SCALE, 0, 0, DPR * SCALE, 0, 0);
  ctx.clearRect(0, 0, W, H);
  if (EndCard.on && EndCard.a >= 1) { drawEndCard(T); return; }
  drawRoom();
  drawWindow(T);
  drawPlacedPieces();
  drawCurtain(T);
  drawFurniture(T);
  drawWinds();
  drawTray();
  drawFx();
  drawFrost();
  if (G.drag) drawPiece(G.drag.p, G.drag.p.r, true);
  drawCaption();
  drawHand(T);
  drawHUD(T);
  drawBanner(T);
  if (G.cut > 0) { ctx.fillStyle = `rgba(16,70,200,${G.cut})`; ctx.fillRect(0, 0, W, H); }
  if (EndCard.on) drawEndCard(T);
}

// ───────────────────────── boot ─────────────────────────
let lastT = 0;
function step(ts) {
  // real-time clock: a slow device runs several fixed sub-steps per frame instead of slowing the game (and the freeze
  // timer) down; a long gap (tab switch) is capped so nothing jumps.
  let acc = Math.min(0.25, Math.max(0, (ts - lastT) / 1000)); lastT = ts;
  while (acc > 1e-4) { const d = Math.min(1 / 30, acc); update(d); acc -= d; }
  render();
}
function frame(ts) { step(ts); requestAnimationFrame(frame); }
function boot() {
  layout(); window.addEventListener('resize', layout);
  sampleEdges('win_bg'); sampleEdges('lose_bg');
  // AD: no intro story — the ad opens directly on Level 1 (LevelFlow.LoadCurrentLevel)
  loadLevel(0);
  const ld = document.getElementById('load'); if (ld) ld.remove();
  requestAnimationFrame(t => { lastT = t; requestAnimationFrame(frame); });
}
window.__ATC = { G, CFG, EndCard, Banner, // QA hooks
  solveOne: () => { const p = G.pieces.find(q => q.state === 'tray'); if (!p) return false; const r = pieceRect(p); p.state = 'drag'; p.r = { ...r }; G.drag = { p, ox: 0, oy: 0, id: -1 }; const sr = slotRect(G.slots[p.si]); p.r.x = sr.x + sr.w / 2 - r.w / 2; p.r.y = sr.y + sr.h / 2 - r.h / 2; endDrag(); return true; },
  wrongOne: () => { const p = G.pieces.find(q => q.state === 'tray'); if (!p) return; const r = pieceRect(p); p.state = 'drag'; p.r = { ...r, x: 10, y: 10 }; G.drag = { p, ox: 0, oy: 0, id: -1 }; endDrag(); },
  freeze: () => { G.meter = 0.0001; G.remaining = 0.01; },
  step: ts => step(ts), // QA: drive one display frame at timestamp ts (ms)
  advance: sec => { for (let i = 0, n = Math.round(sec * 60); i < n; i++) update(1 / 60); render(); return G.phase; }, // deterministic clock for QA in throttled tabs
  layout: () => ({ W, H, SCALE, L }), dl: () => L.dl,
};
initAudio();
Promise.all([loadImages(), loadFont()]).then(boot);
})();
