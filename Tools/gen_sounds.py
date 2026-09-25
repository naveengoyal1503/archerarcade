#!/usr/bin/env python3
"""
Archer Arcade placeholder audio (CLAUDE.md: `python Tools/gen_sounds.py`).

Writes Assets/ArcherArcade/Resources/Audio/Generated/<id>.wav for every sound id listed in
Docs/DESIGN_TOKENS.md §8 (sfx, music loops, stingers). Impact sounds get 3 variations (<id>_v1.._v3) so repeated
hits don't sound robotic; AudioManager picks one at random and jitters the pitch. Real sounds dropped into
Resources/Audio/Final/<id>.wav (same id) override these.

Everything is synthesised here (original, no samples), deterministic (fixed seed), 22.05 kHz mono 16-bit.
Music loops are whole bars so they loop seamlessly; release tails are wrapped to the start.
"""
import math
import os
import re
import sys
import wave

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TOKENS = os.path.join(ROOT, "Docs", "DESIGN_TOKENS.md")
OUT = os.path.join(ROOT, "Assets", "ArcherArcade", "Resources", "Audio", "Generated")
SR = 22050
RNG = np.random.default_rng(20260925)

# ----------------------------------------------------------------------------- helpers


def t_axis(sec):
    return np.arange(int(sec * SR)) / SR


def env(n, attack=0.005, decay=0.2, sustain=0.0, release=0.05, hold=0.0):
    """ADSR-ish envelope over n samples (times in seconds)."""
    a = int(attack * SR)
    h = int(hold * SR)
    d = int(decay * SR)
    r = int(release * SR)
    e = np.zeros(n)
    i = 0
    seg = min(a, n - i)
    if seg > 0:
        e[i:i + seg] = np.linspace(0, 1, seg, endpoint=False)
        i += seg
    seg = min(h, n - i)
    if seg > 0:
        e[i:i + seg] = 1
        i += seg
    seg = min(d, n - i)
    if seg > 0:
        e[i:i + seg] = np.linspace(1, sustain, seg, endpoint=False)
        i += seg
    rest = n - i
    if rest > 0:
        tail = min(r, rest)
        body = rest - tail
        e[i:i + body] = sustain
        if tail > 0:
            e[i + body:] = np.linspace(sustain, 0, tail)
    return e


def exp_decay(n, seconds):
    return np.exp(-np.arange(n) / (seconds * SR))


def noise(n):
    return RNG.uniform(-1, 1, n)


def lowpass(x, cutoff):
    """One-pole low-pass; cutoff may be an array (sweeps)."""
    cutoff = np.broadcast_to(np.asarray(cutoff, dtype=float), x.shape)
    y = np.zeros_like(x)
    prev = 0.0
    k = 1 - np.exp(-2 * math.pi * cutoff / SR)
    for i in range(len(x)):
        prev += k[i] * (x[i] - prev)
        y[i] = prev
    return y


def highpass(x, cutoff):
    return x - lowpass(x, cutoff)


def bandpass(x, lo, hi):
    return lowpass(highpass(x, lo), hi)


def sine(freq, sec, phase=0.0):
    """Sine with a constant or swept frequency (array)."""
    n = int(sec * SR)
    f = np.broadcast_to(np.asarray(freq, dtype=float), (n,))
    return np.sin(2 * math.pi * np.cumsum(f) / SR + phase)


def square(freq, sec, duty=0.5):
    n = int(sec * SR)
    f = np.broadcast_to(np.asarray(freq, dtype=float), (n,))
    ph = np.cumsum(f) / SR
    return np.where((ph % 1.0) < duty, 1.0, -1.0)


def tri(freq, sec):
    n = int(sec * SR)
    f = np.broadcast_to(np.asarray(freq, dtype=float), (n,))
    ph = np.cumsum(f) / SR
    return 2 * np.abs(2 * (ph % 1.0) - 1) - 1


def pluck(freq, sec, bright=0.5, decay=0.996):
    """Karplus-Strong plucked string."""
    n = int(sec * SR)
    period = max(2, int(SR / freq))
    buf = RNG.uniform(-1, 1, period)
    buf = lowpass(buf, 2000 + bright * 8000)
    out = np.zeros(n)
    for i in range(n):
        j = i % period
        out[i] = buf[j]
        buf[j] = decay * 0.5 * (buf[j] + buf[(j + 1) % period])
    return out


def mix(*parts):
    n = max(len(p) for p in parts)
    out = np.zeros(n)
    for p in parts:
        out[:len(p)] += p
    return out


def pad(x, sec):
    n = int(sec * SR)
    if len(x) >= n:
        return x[:n]
    return np.concatenate([x, np.zeros(n - len(x))])


def at(x, sec, total):
    """Places x starting at sec inside a buffer of total seconds."""
    out = np.zeros(int(total * SR))
    s = int(sec * SR)
    end = min(len(out), s + len(x))
    out[s:end] += x[:end - s]
    return out


def normalize(x, peak=0.89):
    m = np.max(np.abs(x)) or 1.0
    return x / m * peak


def fade(x, fin=0.002, fout=0.01):
    n = len(x)
    a = min(n, int(fin * SR))
    b = min(n, int(fout * SR))
    y = x.copy()
    if a > 0:
        y[:a] *= np.linspace(0, 1, a)
    if b > 0:
        y[n - b:] *= np.linspace(1, 0, b)
    return y


def note(name):
    """'A4' → Hz."""
    names = {"C": -9, "C#": -8, "D": -7, "D#": -6, "E": -5, "F": -4, "F#": -3, "G": -2, "G#": -1, "A": 0, "A#": 1, "B": 2}
    m = re.match(r"([A-G]#?)(-?\d)", name)
    semis = names[m.group(1)] + (int(m.group(2)) - 4) * 12
    return 440.0 * 2 ** (semis / 12)


def bell(freq, sec, decay=0.5):
    n = int(sec * SR)
    partials = [(1, 1.0), (2.0, 0.45), (2.76, 0.3), (5.4, 0.12)]
    out = np.zeros(n)
    for ratio, amp in partials:
        out += amp * sine(freq * ratio, sec) * exp_decay(n, decay / ratio ** 0.5)
    return out


def boom(sec, low=90, amount=1.0):
    n = int(sec * SR)
    body = lowpass(noise(n), np.linspace(1800, 120, n)) * exp_decay(n, sec * 0.35)
    thump = sine(np.linspace(low * 1.8, low * 0.5, n), sec) * exp_decay(n, 0.18)
    crack = highpass(noise(n), 1500) * exp_decay(n, 0.03)
    return body * 2.2 * amount + thump * 1.2 + crack * 0.5


def knock(freq, sec=0.18, noise_amt=0.6):
    n = int(sec * SR)
    tone = sine(freq * (1 + 0.15 * np.exp(-np.arange(n) / (0.01 * SR))), sec) * exp_decay(n, 0.05)
    tone2 = sine(freq * 2.3, sec) * exp_decay(n, 0.025) * 0.4
    hit = bandpass(noise(n), 300, 3000) * exp_decay(n, 0.012) * noise_amt
    return tone + tone2 + hit


# ----------------------------------------------------------------------------- sound effects

def sfx_bow_draw(v):
    sec = 0.42
    n = int(sec * SR)
    f = np.linspace(180, 420, n) * (1 + 0.02 * v)
    creak = square(f * 0.5, sec, 0.3) * 0.15 + bandpass(noise(n), 400, 2500) * 0.5
    grain = (np.sin(2 * math.pi * np.cumsum(np.linspace(28, 60, n)) / SR) > 0.6) * 0.6 + 0.4
    return creak * grain * env(n, 0.03, 0.3, 0.6, 0.05)


def sfx_bow_creak_loop(v):
    # 1 s loop: friction grains at a steady rate, seamless (whole cycles).
    sec = 1.0
    n = int(sec * SR)
    t = np.arange(n) / SR
    grains = 0.5 + 0.5 * np.sin(2 * math.pi * 24 * t)
    base = bandpass(noise(n), 300, 1800) * grains ** 3
    tone = square(140, sec, 0.25) * 0.08 * grains
    y = base + tone
    return y


def sfx_release(v):
    sec = 0.45
    s = pluck(note("A2") * (1 + 0.03 * v), sec, bright=0.8, decay=0.994) * 0.9
    n = len(s)
    thump = sine(np.linspace(160, 60, n), sec) * exp_decay(n, 0.04)
    snap = highpass(noise(n), 2500) * exp_decay(n, 0.006) * 0.6
    return s + thump * 0.8 + snap


def sfx_whoosh(v):
    sec = 0.42
    n = int(sec * SR)
    center = np.concatenate([np.linspace(600, 2800, n // 2), np.linspace(2800, 900, n - n // 2)])
    y = bandpass(noise(n), center * 0.6, center * 1.4)
    return y * env(n, 0.12, 0.25, 0.0, 0.05) * 1.6


def sfx_hit_wood(v):
    return knock(260 + 30 * v, 0.22) + at(knock(520 + 40 * v, 0.1, 0.3) * 0.3, 0.01, 0.22)


def sfx_hit_stone(v):
    sec = 0.2
    n = int(sec * SR)
    click = highpass(noise(n), 1800) * exp_decay(n, 0.01)
    ring = sine(1250 + 110 * v, sec) * exp_decay(n, 0.04) * 0.4 + sine(2100 + 90 * v, sec) * exp_decay(n, 0.03) * 0.25
    return click + ring


def sfx_hit_metal(v):
    sec = 0.7
    n = int(sec * SR)
    base = 520 + 40 * v
    out = np.zeros(n)
    for ratio, amp, d in [(1, 1, 0.3), (2.57, 0.6, 0.2), (4.33, 0.4, 0.12), (6.1, 0.25, 0.08)]:
        out += amp * sine(base * ratio, sec) * exp_decay(n, d)
    return out + highpass(noise(n), 3000) * exp_decay(n, 0.01) * 0.5


def sfx_hit_body(v):
    sec = 0.3
    n = int(sec * SR)
    thud = sine(np.linspace(200 + 20 * v, 70, n), sec) * exp_decay(n, 0.07)
    slap = bandpass(noise(n), 200, 2200) * exp_decay(n, 0.02)
    boing = sine(np.linspace(420, 300, n), sec) * exp_decay(n, 0.05) * 0.25
    return thud * 1.4 + slap * 0.7 + boing


def sfx_hit_shield(v):
    return mix(sfx_hit_wood(v) * 0.9, sfx_hit_metal(v) * 0.35)


def sfx_headshot(v):
    sec = 0.8
    ding = bell(note("E6") * (1 + 0.01 * v), sec, 0.35) * 0.8
    return mix(sfx_hit_body(v) * 0.6, ding)


def sfx_crate_break(v):
    total = 0.55
    out = np.zeros(int(total * SR))
    for i in range(7):
        k = knock(220 + RNG.uniform(-40, 120), 0.15, 1.0) * RNG.uniform(0.4, 1.0)
        out += at(k, i * 0.035 + RNG.uniform(0, 0.02), total)
    n = len(out)
    crunch = bandpass(noise(n), 500, 4000) * exp_decay(n, 0.09) * 0.8
    return out + crunch


def sfx_barrel_boom(v):
    return boom(1.1, 80 + 5 * v, 1.0)


def sfx_bounce(v):
    sec = 0.45
    n = int(sec * SR)
    t = np.arange(n) / SR
    f = 180 + 420 * (1 - np.exp(-t / 0.08)) + 60 * np.sin(2 * math.pi * 18 * t) * np.exp(-t / 0.2)
    return sine(f, sec) * exp_decay(n, 0.16)


def sfx_burn(v):
    sec = 0.7
    n = int(sec * SR)
    roar = lowpass(noise(n), np.linspace(3000, 700, n)) * env(n, 0.04, 0.6, 0.0, 0.05)
    return roar * 1.6 + crackle(sec, 18) * 0.5


def crackle(sec, rate):
    n = int(sec * SR)
    out = np.zeros(n)
    count = int(sec * rate)
    for _ in range(count):
        p = RNG.integers(0, max(1, n - 400))
        L = RNG.integers(60, 300)
        out[p:p + L] += highpass(noise(L), 1500) * exp_decay(L, 0.002) * RNG.uniform(0.3, 1.0)
    return out


def sfx_zap(v):
    sec = 0.32
    n = int(sec * SR)
    t = np.arange(n) / SR
    buzz = square(90 + 10 * v, sec, 0.3) * (0.5 + 0.5 * np.sign(np.sin(2 * math.pi * 30 * t)))
    fizz = highpass(noise(n), 2500) * (RNG.uniform(0, 1, n) > 0.85)
    return (buzz * 0.5 + fizz * 0.8) * env(n, 0.005, 0.25, 0.0, 0.05)


def sfx_lightning_strike(v):
    sec = 0.6
    n = int(sec * SR)
    crack = highpass(noise(n), 1200) * exp_decay(n, 0.05) * 1.4
    zapp = sine(np.linspace(2400, 300, n), sec) * exp_decay(n, 0.08) * 0.4
    return crack + zapp + boom(sec, 70, 0.4)


def sfx_thunder(v):
    sec = 1.8
    n = int(sec * SR)
    rumble = lowpass(noise(n), 260) * env(n, 0.08, 1.5, 0.0, 0.2)
    rolls = lowpass(noise(n), 500) * (0.5 + 0.5 * np.sin(2 * math.pi * 3.3 * np.arange(n) / SR)) * exp_decay(n, 0.6)
    return rumble * 5 + rolls * 2


def sfx_fire_burst(v):
    sec = 0.7
    n = int(sec * SR)
    whoomp = lowpass(noise(n), np.concatenate([np.linspace(200, 3500, n // 5), np.linspace(3500, 400, n - n // 5)]))
    return whoomp * env(n, 0.05, 0.6, 0.0, 0.05) * 2.2 + crackle(sec, 25) * 0.6


def sfx_on_fire_loop(v):
    sec = 1.5
    n = int(sec * SR)
    base = lowpass(noise(n), 900) * 0.6
    return base + crackle(sec, 20)


def sfx_freeze(v):
    sec = 0.8
    n = int(sec * SR)
    shimmer = np.zeros(n)
    for i, f in enumerate([2637, 3136, 3951, 4699]):
        shimmer += at(bell(f * (1 - 0.02 * v), 0.5, 0.2) * 0.3, i * 0.06, sec)
    hiss = highpass(noise(n), 4000) * env(n, 0.02, 0.6, 0.0, 0.1) * 0.4
    return shimmer + hiss


def sfx_ice_shatter(v):
    total = 0.6
    out = np.zeros(int(total * SR))
    for i in range(9):
        f = RNG.uniform(2500, 6000)
        out += at(sine(f, 0.12) * exp_decay(int(0.12 * SR), 0.03) * RNG.uniform(0.3, 1), RNG.uniform(0, 0.25), total)
    n = len(out)
    return out + highpass(noise(n), 3000) * exp_decay(n, 0.05) * 0.6


def sfx_poison_cloud(v):
    total = 0.9
    out = np.zeros(int(total * SR))
    for i in range(8):
        f0 = RNG.uniform(250, 600)
        blip = sine(np.linspace(f0, f0 * 2.2, int(0.07 * SR)), 0.07) * np.hanning(int(0.07 * SR))
        out += at(blip * 0.6, i * 0.09 + RNG.uniform(0, 0.03), total)
    n = len(out)
    hiss = bandpass(noise(n), 800, 4000) * env(n, 0.1, 0.7, 0.0, 0.1) * 0.4
    return out + hiss


def sfx_bubble_pop(v):
    sec = 0.12
    n = int(sec * SR)
    return sine(np.linspace(500 + 60 * v, 1500, n), sec) * exp_decay(n, 0.03) + highpass(noise(n), 3000) * exp_decay(n, 0.004) * 0.4


def sfx_bubble_cast(v):
    sec = 0.8
    out = np.zeros(int(sec * SR))
    for i, f in enumerate([note("C5"), note("E5"), note("G5"), note("C6")]):
        out += at(sine(f, 0.45) * env(int(0.45 * SR), 0.02, 0.4) * 0.4, i * 0.08, sec)
    return mix(out, sfx_bubble_pop(v) * 0.3)


def sfx_tnt_boom(v):
    return boom(1.4, 60 + 5 * v, 1.3)


def sfx_tower_topple(v):
    total = 1.0
    out = np.zeros(int(total * SR))
    for i in range(6):
        out += at(knock(300 - i * 25 + RNG.uniform(-10, 10), 0.2, 1.0) * (1 - i * 0.08), i * 0.12 + RNG.uniform(0, 0.03), total)
    return out


def sfx_heal(v):
    sec = 0.9
    out = np.zeros(int(sec * SR))
    for i, f in enumerate([note("C5"), note("E5"), note("G5"), note("C6"), note("E6")]):
        out += at(bell(f, 0.5, 0.25) * 0.35, i * 0.07, sec)
    return out


def sfx_bomb(v):
    return boom(0.9, 90 + 5 * v, 0.9)


def sfx_triple_fan(v):
    total = 0.5
    return sum(at(sfx_release(i) * 0.7, i * 0.07, total) for i in range(3))


def sfx_meteor(v):
    sec = 1.1
    n = int(sec * SR)
    fall = bandpass(noise(n), np.linspace(3000, 400, n) * 0.5, np.linspace(3000, 400, n)) * env(n, 0.3, 0.5, 0.3, 0.1) * 1.4
    return fall + at(boom(0.7, 80, 0.7), 0.55, sec) + crackle(sec, 15) * 0.4


def sfx_storm(v):
    sec = 0.9
    n = int(sec * SR)
    swell = bandpass(noise(n), 500, 5000) * env(n, 0.5, 0.35, 0.0, 0.05) * 0.8
    return swell + at(sfx_zap(v), 0.45, sec) + at(sfx_lightning_strike(v) * 0.5, 0.55, sec)


def sfx_cluster(v):
    total = 0.7
    return sum(at(boom(0.35, 120 + i * 20, 0.5) * 0.6, i * 0.12, total) for i in range(3))


def sfx_ai_aim(v):
    return sfx_bow_draw(v)[: int(0.3 * SR)] * 0.6


def sfx_turn_start(v):
    sec = 0.5
    return mix(bell(note("G5"), 0.4, 0.25) * 0.5, at(bell(note("C6"), 0.4, 0.3) * 0.5, 0.1, sec))


def sfx_timer_tick(v):
    return knock(1200, 0.06, 0.4) * 0.8


def sfx_knockout(v):
    sec = 1.0
    n = int(sec * SR)
    poof = lowpass(noise(n), np.linspace(4000, 300, n)) * exp_decay(n, 0.15) * 1.5
    slide = sine(np.linspace(1200, 300, n), sec) * env(n, 0.02, 0.8, 0.0, 0.1) * 0.35
    stars = sum(at(bell(f, 0.3, 0.15) * 0.2, 0.3 + i * 0.07, sec) for i, f in enumerate([note("E6"), note("C6"), note("G5")]))
    return poof + slide + stars


def sfx_cheer(v):
    sec = 1.1
    out = np.zeros(int(sec * SR))
    for i, f in enumerate([note("C5"), note("E5"), note("G5")]):
        out += at(brass(f, 0.18) * 0.5, i * 0.1, sec)
    out += at(brass(note("C6"), 0.6) * 0.6, 0.3, sec)
    out += at(brass(note("E5"), 0.6) * 0.3, 0.3, sec)
    return out


def brass(freq, sec):
    n = int(sec * SR)
    s = square(freq, sec, 0.35) * 0.5 + tri(freq * 2, sec) * 0.2
    return lowpass(s, 2500) * env(n, 0.02, 0.1, 0.7, 0.08)


def sfx_coin(v):
    sec = 0.3
    a = square(note("B5"), 0.07, 0.5)
    b = square(note("E6"), 0.23, 0.5) * exp_decay(int(0.23 * SR), 0.1)
    return lowpass(np.concatenate([a, b]), 6000) * 0.4


def sfx_star(level):
    f = [note("C6"), note("E6"), note("G6")][level - 1]
    return bell(f, 0.9, 0.4) * 0.8 + at(bell(f * 2, 0.5, 0.2) * 0.2, 0.02, 0.9)


def sfx_chest_shake(v):
    total = 0.6
    return sum(at(knock(340 + RNG.uniform(-30, 30), 0.1, 0.8) * 0.7, i * 0.08, total) for i in range(7))


def sfx_chest_open(v):
    sec = 1.2
    n = int(sec * SR)
    creak = bandpass(noise(n), 300, 1500) * (0.5 + 0.5 * np.sin(2 * math.pi * 30 * np.arange(n) / SR)) * env(n, 0.05, 0.3, 0.0, 0.05) * 0.8
    sparkle = sum(at(bell(f, 0.5, 0.2) * 0.3, 0.3 + i * 0.06, sec)
                  for i, f in enumerate([note("G5"), note("C6"), note("E6"), note("G6"), note("C7")]))
    return creak + sparkle


def sfx_badge(v):
    sec = 1.0
    chord = sum(bell(f, sec, 0.5) * 0.3 for f in [note("C6"), note("E6"), note("G6")])
    return mix(chord, sfx_freeze(0) * 0.3)


def sfx_ui_tap(v):
    sec = 0.05
    n = int(sec * SR)
    return sine(900, sec) * exp_decay(n, 0.012) * 0.6 + knock(1400, sec, 0.2) * 0.3


def sfx_ui_confirm(v):
    sec = 0.22
    return mix(sine(note("E5"), 0.08) * env(int(0.08 * SR), 0.003, 0.07) * 0.5,
               at(sine(note("A5"), 0.14) * env(int(0.14 * SR), 0.003, 0.13) * 0.5, 0.07, sec))


def sfx_ui_back(v):
    sec = 0.22
    return mix(sine(note("A5"), 0.08) * env(int(0.08 * SR), 0.003, 0.07) * 0.5,
               at(sine(note("E5"), 0.14) * env(int(0.14 * SR), 0.003, 0.13) * 0.5, 0.07, sec))


def sfx_toggle_on(v):
    sec = 0.08
    n = int(sec * SR)
    return sine(np.linspace(600, 1100, n), sec) * exp_decay(n, 0.03) * 0.6


def sfx_toggle_off(v):
    sec = 0.08
    n = int(sec * SR)
    return sine(np.linspace(1000, 550, n), sec) * exp_decay(n, 0.03) * 0.6


# ----------------------------------------------------------------------------- music

def chord_notes(root, kind):
    r = note(root)
    steps = {"maj": [0, 4, 7], "min": [0, 3, 7], "sus": [0, 5, 7], "dim": [0, 3, 6]}[kind]
    return [r * 2 ** (s / 12) for s in steps]


def render_loop(bpm, bars, progression, lead_scale, style):
    beat = 60.0 / bpm
    bar = beat * 4
    total = bar * bars
    n = int(total * SR)
    out = np.zeros(n + int(2 * SR))  # tail, wrapped later

    def place(x, sec, gain):
        s = int(sec * SR)
        e = min(len(out), s + len(x))
        out[s:e] += x[:e - s] * gain

    rng = np.random.default_rng(bpm * 7 + bars)
    melody_prev = 4
    for b in range(bars):
        root, kind = progression[b % len(progression)]
        tones = chord_notes(root, kind)
        t0 = b * bar
        # Bass: root on 1 and 3 (8ths in duel / boss).
        bass_f = tones[0] / 2
        steps = 8 if style in ("duel", "boss") else 4
        for i in range(steps):
            L = beat * (4 / steps) * 0.9
            f = bass_f * (1.5 if (style == "boss" and i % 4 == 3) else 1)
            tone = (tri(f, L) * 0.7 + sine(f, L) * 0.5) * env(int(L * SR), 0.005, L * 0.8, 0.3, 0.03)
            place(tone, t0 + i * beat * (4 / steps), 0.35 if style != "boss" else 0.45)
        # Pad / arpeggio.
        if style in ("home", "world"):
            for i in range(8):
                f = tones[i % 3] * (2 if i >= 4 else 1)
                pl = pluck(f, beat * 0.9, bright=0.4, decay=0.995)
                place(pl, t0 + i * beat / 2, 0.22)
        else:
            L = bar
            padv = sum(lowpass(square(f, L, 0.5), 1400) * 0.12 for f in tones)
            place(padv * env(int(L * SR), 0.1, 0.5, 0.6, 0.2), t0, 0.35)
        # Lead: a simple singable line from the scale, leaning on chord tones.
        if style != "boss" or b % 2 == 1:
            pattern = [1, 0, 1, 1, 0, 1, 1, 0] if style == "home" else [1, 1, 0, 1, 1, 0, 1, 1]
            for i, on in enumerate(pattern):
                if not on:
                    continue
                melody_prev = int(np.clip(melody_prev + rng.integers(-2, 3), 0, len(lead_scale) - 1))
                f = note(lead_scale[melody_prev])
                L = beat * 0.48
                n2 = int(L * SR)
                vib = f * (1 + 0.006 * np.sin(2 * math.pi * 5.5 * np.arange(n2) / SR))
                voice = (sine(vib, L) * 0.7 + tri(vib, L) * 0.3) if style in ("home", "world") else lowpass(square(vib, L, 0.25), 3000) * 0.5
                place(voice * env(n2, 0.01, L * 0.6, 0.4, 0.05), t0 + i * beat / 2, 0.22)
        # Drums.
        for i in range(8):
            ts = t0 + i * beat / 2
            if i % 4 == 0:
                L = 0.25
                kick = sine(np.linspace(150, 45, int(L * SR)), L) * exp_decay(int(L * SR), 0.08)
                place(kick, ts, 0.55 if style != "home" else 0.35)
            if style != "home" and i % 4 == 2:
                L = 0.2
                snare = bandpass(noise(int(L * SR)), 800, 6000) * exp_decay(int(L * SR), 0.05)
                place(snare, ts, 0.35)
            hat = highpass(noise(int(0.05 * SR)), 6000) * exp_decay(int(0.05 * SR), 0.012)
            place(hat, ts, 0.12 if i % 2 else 0.18)
        if style == "boss" and b % 4 == 3:
            for i in range(4):
                L = 0.3
                tom = sine(np.linspace(180 - i * 25, 80, int(L * SR)), L) * exp_decay(int(L * SR), 0.1)
                place(tom, t0 + bar - beat + i * beat / 4, 0.5)

    loop = out[:n].copy()
    tail = out[n:]
    loop[:len(tail)] += tail[:n]
    return loop


def music(id_):
    if id_ == "mus_home":
        return render_loop(100, 8, [("C4", "maj"), ("G3", "maj"), ("A3", "min"), ("F3", "maj")],
                           ["C5", "D5", "E5", "G5", "A5", "C6", "D6"], "home")
    if id_ == "mus_world1":
        return render_loop(112, 8, [("G3", "maj"), ("C4", "maj"), ("E3", "min"), ("D4", "maj")],
                           ["G4", "A4", "B4", "D5", "E5", "G5", "A5"], "world")
    if id_ == "mus_duel":
        return render_loop(128, 8, [("A3", "min"), ("F3", "maj"), ("C4", "maj"), ("G3", "maj")],
                           ["A4", "C5", "D5", "E5", "G5", "A5", "C6"], "duel")
    if id_ == "mus_boss":
        return render_loop(140, 8, [("D3", "min"), ("D3", "min"), ("A#2", "maj"), ("A2", "maj")],
                           ["D5", "F5", "G5", "A5", "C6", "D6", "F6"], "boss")
    raise KeyError(id_)


def sting_victory():
    sec = 2.2
    out = np.zeros(int(sec * SR))
    for i, f in enumerate(["C5", "E5", "G5"]):
        out += at(brass(note(f), 0.16) * 0.5, i * 0.12, sec)
    for f in ["C5", "E5", "G5", "C6"]:
        out += at(brass(note(f), 1.2) * 0.3, 0.4, sec)
    out += at(sum(bell(note(f), 1.2, 0.5) * 0.15 for f in ["C6", "E6", "G6"]), 0.4, sec)
    return out


def sting_defeat():
    sec = 1.8
    out = np.zeros(int(sec * SR))
    for i, f in enumerate(["G4", "F#4", "F4"]):
        out += at(brass(note(f), 0.3) * 0.5, i * 0.3, sec)
    n = int(0.9 * SR)
    wah = brass(note("E4"), 0.9) * (0.6 + 0.4 * np.sin(2 * math.pi * 6 * np.arange(n) / SR))
    return out + at(wah * 0.5, 0.9, sec)


# ----------------------------------------------------------------------------- ids → generators

VARIED = {
    "sfx_hit_wood", "sfx_hit_stone", "sfx_hit_metal", "sfx_hit_body", "sfx_hit_shield", "sfx_headshot", "sfx_release",
    "sfx_whoosh", "sfx_crate_break", "sfx_bounce", "sfx_zap", "sfx_bubble_pop",
}
LOOPS = {"sfx_bow_creak_loop", "sfx_on_fire_loop"}


def token_ids():
    text = open(TOKENS, encoding="utf-8").read()
    section = text.split("## 8.")[1].split("\n## ")[0]
    ids = []
    for m in re.finditer(r"`((?:sfx|mus|sting)_[a-z0-9_]+?)(?:_?(\d)\.\.(\d))?`", section):
        base, lo, hi = m.group(1), m.group(2), m.group(3)
        if lo:
            for k in range(int(lo), int(hi) + 1):
                ids.append(f"{base}_{k}" if not base.endswith("_") else f"{base}{k}")
        else:
            ids.append(base)
    return ids


def generate(id_, v):
    if id_.startswith("mus_"):
        return music(id_)
    if id_ == "sting_victory":
        return sting_victory()
    if id_ == "sting_defeat":
        return sting_defeat()
    m = re.match(r"sfx_star_(\d)", id_)
    if m:
        return sfx_star(int(m.group(1)))
    fn = globals().get(id_)
    if fn is None:
        raise SystemExit(f"No generator for {id_}: add one to Tools/gen_sounds.py")
    return fn(v)


def write_wav(path, samples):
    data = (np.clip(samples, -1, 1) * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def main():
    os.makedirs(OUT, exist_ok=True)
    ids = token_ids()
    if not ids:
        raise SystemExit("No sound ids found in DESIGN_TOKENS §8")
    written = 0
    for id_ in ids:
        loop = id_ in LOOPS or id_.startswith("mus_")
        variants = [1, 2, 3] if id_ in VARIED else [0]
        for v in variants:
            y = generate(id_, v)
            y = normalize(y, 0.8 if id_.startswith("mus_") else 0.89)
            if not loop:
                y = fade(y)
            name = f"{id_}_v{v}.wav" if v else f"{id_}.wav"
            write_wav(os.path.join(OUT, name), y)
            written += 1
    print(f"{len(ids)} ids, {written} files → {os.path.relpath(OUT, ROOT)}")


if __name__ == "__main__":
    sys.exit(main())
