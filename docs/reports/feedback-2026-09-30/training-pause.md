# Training controls disabled by their pause menu

QA_TUMP_0049 / ENG-0930-TRAINING-PAUSE. Found with actual manual input in the
integrated Linux player described in integrated-player.md. Practice > Training >
Escape made Character, Defender and all training rules disabled. Expected: change
training configuration while offline simulation remains paused.

PracticeRange.CanEdit reused PresentationClock.BlocksInput, whose requested scale0
branch correctly refuses gameplay during pause. Configuration is not gameplay
input. It now retains the existing active/local/network and shared-presentation
conditions without treating ordinary paused simulation as a configuration lock.
No ability mechanics, network semantics, pause behavior or UI layout changed.

Unity6000.5.8f1 native graphics check: baseline1/1fails on CanEdit while the actual
prepared training menu is open. Final1/1passes3.44seconds. It checks scale0,
interactable Defender/cheat controls, existing character/bot/defender/configuration
changes, retained parked input, refusal during a presentation hold and network
mode, popup reuse, resume and preserved saved preferences. Before/after menu
captures were inspected. No memory stop or new OOM kill.

The first fixture attempt had internal-method access compile errors; one bounded
fixture repair invoked those existing presentation transitions through reflection.
That failed compile is not runtime evidence. The result does not claim a refreshed
player or actual-peer check; the actual-player barrier review follows this fix.
