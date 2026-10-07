# Prepare shared assets on the current login and main-menu route

The source140 ordinary Windows release records a 4531.743ms frame on the first
HERO button opening. That complete run retained 175 timing windows and 74 actions,
then failed to reach results because its fixture selected training mode. Training
resets the round clock and unregisters the other actors. Required consequences
that were absent are preserved as failures, not qualified gameplay timing.

The retired splash preload no longer runs on the current startup route. The
current login/main-menu preparation now awaits the shared asynchronous roster
request and the existing yielded gameplay preparation before map and Home media.
It prepares the existing icon, prop and effect-data caches without constructing
extra character previews or casting abilities. Progress follows completed work.
Login remains first, the main loading view stays for at least five seconds and
until preparation completes, and background priority has its existing lifetime.

Opt-in performance logging splits preview instantiation, toon setup, pet setup,
idle binding and first-step CPU work. Coroutine resource waits are labelled
separately. The diagnostic also records immediate reopening of the same HERO
button. Its explicit menu-only mode does not exercise gameplay or results.
Normal tournament launches do not enable this logging.

The first native candidate passed startup order, first Home playback and
cancellation, but the existing login asset check found zero of 51 expected skill
icons. This exposed the missing active-route gameplay preparation. That original
failure is retained. The corrected candidate41768 passes the same three native controls with all
21387 frozen inputs and preferences restored. [Exact evidence](evidence.json)
checks four native source files in each run and retains the original failure.
The actual ordinary release c completes an explicit 14-window menu-only route.
First HERO opening falls from4531.743ms to118.279ms; immediate same-button
reopening peaks at16.641ms. First preview preparation is18.198ms CPU, including
15.917ms toon setup, followed by5.164ms first-step CPU. That leaves an unqualified
first-use hitch; these stages do not explain the entire presented frame.

The main preparation window has a859.876ms maximum and twelve frames over50ms.
Mode selection still peaks at179.768ms and settings at90.872ms. Roster wait is
3366.666ms and gameplay preparation2067.019ms including yields, not CPU totals.
The [release comparison](release-menu-comparison.json) retains exact raw timing
and log evidence plus the source6e/runtime301db008 build identity. The player,
runner and all21387 build inputs/preferences are restored. This is a single
hidden release comparison and demonstrates improvement without closing all
hitch or gameplay gates.

Measurements use hidden graphics without OS input or foreground control. Release
allocation counters are unavailable after their known-allocation calibration.
Wall-clock frames include probe overhead and do not isolate GPU duration. Focused
human/device acceptance and full tournament readiness remain separate.
