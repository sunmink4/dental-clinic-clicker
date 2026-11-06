"""배경음악을 직접 합성해 assets/audio/bgm.wav 로 저장한다. 외부 음원을 쓰지 않아 저작권 걱정이 없다.

끝에서 울리는 소리는 곡 앞으로 되돌려 더하므로 반복할 때 이음매가 없다.
"""
import array
import math
import random
import wave
from pathlib import Path

RATE, BPM, BARS = 22050, 76, 16
BEAT = 60 / BPM
LENGTH = int(RATE * BEAT * 4 * BARS)
CHORDS = [[60, 64, 67, 71], [57, 60, 64, 67], [53, 57, 60, 64], [55, 59, 62, 65]]   # Cmaj7 Am7 Fmaj7 G7
SCALE = [72, 74, 76, 79, 81, 84]
buffer = [0.0] * LENGTH
random.seed(7)


def add_note(midi, start, seconds, volume):
    """배음 몇 개를 섞고 빠르게 줄어드는 피아노 비슷한 소리."""
    frequency = 440 * 2 ** ((midi - 69) / 12)
    first = int(start * RATE)
    for i in range(int(seconds * RATE)):
        t = i / RATE
        envelope = min(1, t / 0.01) * math.exp(-3.2 * t / seconds * 2)
        wave_value = (math.sin(math.tau * frequency * t) + 0.35 * math.sin(math.tau * frequency * 2 * t)
                      + 0.12 * math.sin(math.tau * frequency * 3 * t))
        buffer[(first + i) % LENGTH] += wave_value * envelope * volume


for bar in range(BARS):
    chord = CHORDS[bar % len(CHORDS)]
    start = bar * BEAT * 4
    add_note(chord[0] - 12, start, BEAT * 4, 0.22)
    for step in range(8):
        add_note(chord[[0, 2, 1, 3, 2, 1, 3, 2][step]], start + step * BEAT / 2, BEAT * 2, 0.10)
    for beat in (0, 1.5, 2, 3):
        if bar % 4 == 3 and beat > 1.5:
            continue
        if random.random() < 0.7:
            add_note(random.choice(SCALE), start + beat * BEAT, BEAT * 2.5, 0.13)

peak = max(abs(v) for v in buffer)
samples = array.array("h", (int(v / peak * 12000) for v in buffer))
out = Path(__file__).resolve().parent.parent / "assets" / "audio" / "bgm.wav"
out.parent.mkdir(parents=True, exist_ok=True)
with wave.open(str(out), "wb") as file:
    file.setnchannels(1)
    file.setsampwidth(2)
    file.setframerate(RATE)
    file.writeframes(samples.tobytes())
print(out, round(LENGTH / RATE, 1), "초")
