"""Amihan's footstep: `step_amihan`, the only file this writes.

Owner, 2026-10-02: *"make amihans footstteps feel light"*, and asked whether that meant
sound too, chose a NEW airy step sound. It is a FOOTSTEP (the base game's locomotion
family, like `step_rubber`), not a skill cue, so the September 29 skill sound switch
(`AudioCues.IsSkillSfx`) does not silence it. `MotionFoley` plays it for her body only,
once per stride cycle, where everyone else plays the shared rubber step.

The recipe uses her own wind instrument (`tools/build_amihan_audio.py`: filtered noise,
the filter is the instrument) and is deliberately small: a quiet sandal TICK far below
the shared rubber step, a soft cloth BRUSH as her sleeve and hem pass, and a short
breath of AIR rising in pitch as her foot leaves the court (she is lifting, not
landing). No low thump: weight is what this step must not have.

Seeded and numpy only, so a rebuild writes identical bytes. It does NOT run
`build_amihan_audio.py`'s recipes: those skill cues were deleted on the owner's
instruction and must not be written back into Resources/Sfx.

Provisional until the owner hears it in play. Peak and RMS are measurements, not
listening approval.

Run: python tools/build_amihan_steps.py
"""
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import build_amihan_audio as amihan  # noqa: E402  (shared DSP helpers only; its __main__ never runs on import)

ROOT = Path(__file__).resolve().parents[1]
REPORT = ROOT / "docs/reports/amihan-presentation-2026-10-02/step-audio.json"


def step():
    """A light step: tick, brush, and a breath of air lifting away."""
    s = 0.2
    t = amihan.times(s)
    n = len(t)
    # The sandal tick: high, tiny, gone in a few milliseconds (the shared rubber step is a fuller 0.1 s slap).
    tick = amihan.svf(amihan.white(n, 9701), 2600, 3.0) * amihan.np.exp(-t / 0.006) * 0.55
    # The cloth brush: her coat hem passing, a soft high band.
    brush = amihan.band(amihan.white(n, 9702), 3000, 1.6) * amihan.env_ar(t, 0.012, 0.035, 0.016) * 1.1
    # The lift: a short breath of air whose band rises as the foot leaves the court.
    lift = amihan.band(amihan.white(n, 9703), amihan.sweep(t, 520, 1350, 0.16, 0.9), 2.4) \
        * amihan.env_ar(t, 0.03, 0.055, 0.05) * 4.2
    return amihan.finish("step_amihan", tick + brush + lift, s, 0.5)


if __name__ == "__main__":
    row = step()
    report = {
        "provenance": "Original deterministic synthesis (numpy only, tools/build_amihan_steps.py); no external samples or voices.",
        "listening": "Not yet heard by the owner in the game mix. Peak and RMS are measurements, not approval.",
        "cues": [row],
    }
    REPORT.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(f"Authored step_amihan only: {row}")
