# Bot retrieval landing prediction

The bot's analytic shortcut stopped a flying slipper at a height relative to its
launch, then forced the target to world-zero. A native raised launch falling onto
lower terrain predictsz1.85while the actual host-simulated slipper lands atz4.58:
a2.73metre horizontal error in the bot's retrieval target.

Extract the existing ground-circle flight calculation into shared
SlipperLandingPrediction. Preserve its native fixed-step gravity, signed curve,
world banks/bounds and swept supporting-floor selection. The ground circle still
uses that same calculation and geometry; no authored effect or map is edited.
Bot retrieval uses the same predictor and a per-brain source/result cache, refreshed
at the existing tier's Think cadence0.34/0.24/0.16seconds. Replacement sources
refresh immediately; an observed landed/null source retires the old result.

Windows Unity6000.5.8f1 native baseline1/1fails at2.73m. Final8/8passes: raised
launch now predictsz4.58, three bot flat/curve/bank actual-flight comparisons,
cache refresh/source retirement with calibrated100cached reads and0allocation
events, plus the three existing ground-circle flight comparisons. Full forecast
cost is not claimed allocation-free; retained query refresh bounds its frequency.
All701frozen input hashes remain unchanged; both new script GUIDs import natively.
No native fixture repair.60473/70644terminal; profiles/input preferences preserved.
[Raw receipts](bot-landing-checks).

Actual actors/can contacts and later ability/world changes may redirect flight;
cached predictions respond at the bot's ordinary decision cadence. Skim ground
continuation was separate follow-through at this revision and is now qualified
in [the Skim prediction repair](skim-prediction.md). No whole-match FPS/difficulty or
new actual-player/peer claim. The current120peer result predates this change and
remains its exact c26c5c34b scope. No wire/hero-kit/loading/assets change.
