"""
Procedural SFX generator for the VR bar game quick-wins pass.
Writes 16-bit PCM mono WAV @ 44.1 kHz into Assets/6. Audio/.
Placeholder-quality but tuned per event. No external deps beyond numpy.
"""
import numpy as np
import wave
import os

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "6. Audio")
OUT = os.path.abspath(OUT)


def _write(name, sig):
    sig = np.asarray(sig, dtype=np.float32)
    # Normalize with a little headroom, then guard against clipping.
    peak = np.max(np.abs(sig)) or 1.0
    sig = (sig / peak) * 0.92
    pcm = np.clip(sig, -1.0, 1.0)
    pcm16 = (pcm * 32767.0).astype(np.int16)
    path = os.path.join(OUT, name)
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm16.tobytes())
    print("wrote", path, len(pcm16), "samples")


def env(n, attack, decay, hold=0.0):
    """ADSR-ish: linear attack, hold, exponential-ish decay. Times in seconds."""
    a = int(attack * SR)
    h = int(hold * SR)
    d = max(1, n - a - h)
    e = np.ones(n, dtype=np.float32)
    if a > 0:
        e[:a] = np.linspace(0, 1, a)
    if d > 0:
        e[a + h:a + h + d] = np.exp(-np.linspace(0, 5, d))
    return e[:n]


def t(dur):
    return np.linspace(0, dur, int(dur * SR), endpoint=False)


def bandpass_noise(n, lo, hi):
    """Cheap band-pass: FFT mask on white noise."""
    rng = np.random.default_rng(1234)
    x = rng.standard_normal(n)
    X = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(n, 1.0 / SR)
    mask = (freqs >= lo) & (freqs <= hi)
    X *= mask
    y = np.fft.irfft(X, n)
    return y / (np.max(np.abs(y)) or 1.0)


# ---------------------------------------------------------------- ButtonPress
def button_press():
    d = 0.07
    x = t(d)
    tone = np.sin(2 * np.pi * 1200 * x) * env(len(x), 0.001, d)
    click = (np.random.default_rng(7).standard_normal(len(x))) * env(len(x), 0.0005, 0.01) * 0.5
    return tone * 0.7 + click


# ---------------------------------------------------------------- CashSale (cha-ching)
def cash_sale():
    def bell(freq, dur):
        x = t(dur)
        partials = (np.sin(2 * np.pi * freq * x)
                    + 0.5 * np.sin(2 * np.pi * freq * 2.01 * x)
                    + 0.25 * np.sin(2 * np.pi * freq * 3.0 * x))
        return partials * env(len(x), 0.001, dur)
    d1 = bell(988, 0.18)   # B5
    d2 = bell(1319, 0.45)  # E6
    gap = int(0.09 * SR)
    out = np.zeros(gap + len(d2))
    out[:len(d1)] += d1
    out[gap:gap + len(d2)] += d2
    return out


# ---------------------------------------------------------------- CashExpense (thunk)
def cash_expense():
    d = 0.3
    x = t(d)
    sweep = np.linspace(220, 90, len(x))
    tone = np.sin(2 * np.pi * np.cumsum(sweep) / SR)
    body = tone * env(len(x), 0.002, d)
    thud = bandpass_noise(len(x), 40, 200) * env(len(x), 0.001, 0.06) * 0.4
    return body * 0.8 + thud


# ---------------------------------------------------------------- PourLoop (seamless)
def pour_loop():
    d = 1.6
    n = int(d * SR)
    base = bandpass_noise(n, 500, 4500)
    # Bubbling: slow random amplitude flutter.
    rng = np.random.default_rng(99)
    flutter = rng.standard_normal(n)
    Ff = np.fft.rfft(flutter)
    freqs = np.fft.rfftfreq(n, 1.0 / SR)
    Ff *= (freqs < 25)  # keep only slow modulation
    flut = np.fft.irfft(Ff, n)
    flut = 0.6 + 0.4 * (flut / (np.max(np.abs(flut)) or 1.0))
    sig = base * flut
    # Make seamless: crossfade the tail into the head.
    xf = int(0.08 * SR)
    head = sig[:xf].copy()
    tail = sig[-xf:].copy()
    fade = np.linspace(0, 1, xf)
    sig[:xf] = tail * (1 - fade) + head * fade
    sig = sig[:-xf]
    return sig * 0.6


# ---------------------------------------------------------------- GlassBreak
def glass_break():
    d = 0.55
    n = int(d * SR)
    rng = np.random.default_rng(42)
    # Initial shatter burst (bright noise, fast decay).
    burst = bandpass_noise(n, 2000, 12000) * env(n, 0.0005, 0.12)
    out = burst * 0.8
    # Several high clinks scattered after the burst.
    for i, (freq, at) in enumerate([(5200, 0.02), (6400, 0.07), (4100, 0.13),
                                    (7300, 0.18), (3500, 0.26), (5900, 0.33)]):
        seg = t(0.18)
        clink = np.sin(2 * np.pi * freq * seg) * env(len(seg), 0.0005, 0.15)
        start = int(at * SR)
        end = min(n, start + len(clink))
        out[start:end] += clink[:end - start] * (0.5 - i * 0.05)
    return out


# ---------------------------------------------------------------- BottleBreak
def bottle_break():
    d = 0.6
    n = int(d * SR)
    # Duller, lower than glass + a thud body.
    burst = bandpass_noise(n, 800, 7000) * env(n, 0.0008, 0.16)
    thud = bandpass_noise(n, 60, 300) * env(n, 0.001, 0.1)
    out = burst * 0.7 + thud * 0.5
    for i, (freq, at) in enumerate([(2600, 0.03), (3300, 0.09), (1900, 0.16),
                                    (2200, 0.24), (3000, 0.32)]):
        seg = t(0.2)
        clink = np.sin(2 * np.pi * freq * seg) * env(len(seg), 0.0008, 0.16)
        start = int(at * SR)
        e = min(n, start + len(clink))
        out[start:e] += clink[:e - start] * (0.45 - i * 0.05)
    return out


# ---------------------------------------------------------------- BarAmbience (seamless loop)
def bar_ambience():
    d = 3.5
    n = int(d * SR)
    # Low room murmur: low-passed noise + a couple of faint tones, slow amplitude drift.
    base = bandpass_noise(n, 80, 900) * 0.7
    rng = np.random.default_rng(2024)
    drift = rng.standard_normal(n)
    Fd = np.fft.rfft(drift)
    freqs = np.fft.rfftfreq(n, 1.0 / SR)
    Fd *= (freqs < 8)  # very slow drift
    dr = np.fft.irfft(Fd, n)
    dr = 0.7 + 0.3 * (dr / (np.max(np.abs(dr)) or 1.0))
    sig = base * dr
    # Faint warm hum to suggest a busy room.
    x = t(d)
    sig += 0.06 * np.sin(2 * np.pi * 110 * x)
    # Seamless: crossfade tail into head.
    xf = int(0.2 * SR)
    head = sig[:xf].copy()
    tail = sig[-xf:].copy()
    fade = np.linspace(0, 1, xf)
    sig[:xf] = tail * (1 - fade) + head * fade
    sig = sig[:-xf]
    return sig * 0.5


# ---------------------------------------------------------------- Footstep
def footstep():
    d = 0.13
    x = t(d)
    # Soft body thud + a little surface scuff on top.
    body = np.sin(2 * np.pi * np.linspace(150, 70, len(x)) * x) * env(len(x), 0.001, d)
    scuff = bandpass_noise(len(x), 1500, 5000) * env(len(x), 0.0008, 0.04) * 0.3
    return body * 0.8 + scuff


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    _write("ButtonPress.wav", button_press())
    _write("CashSale.wav", cash_sale())
    _write("CashExpense.wav", cash_expense())
    _write("PourLoop.wav", pour_loop())
    _write("GlassBreak.wav", glass_break())
    _write("BottleBreak.wav", bottle_break())
    _write("BarAmbience.wav", bar_ambience())
    _write("Footstep.wav", footstep())
    print("done")
