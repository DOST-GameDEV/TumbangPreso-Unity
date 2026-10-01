# Offline match menu pause

## Report and correction

Owner relayed the tester request: "kapag nag press ka esc or menu when you're
offline dapat mag ppause yung game". The previous menu deliberately kept every
match running. Offline OnOpened now ends transient hitstop, saves the requested
speed and requests zero through the existing presentation clock. OnClosed restores
that speed only while the menu still owns the zero request. Nested Settings keeps
the outer menu active. Scene-exit speed resets are not overwritten. Networked
launches and listening sessions remain live. The existing notice tells the truth.
Hitstop cannot replace a stopped clock with a positive micro-slowdown.

No input bindings, ability behavior, network wire or loading paths changed.

## Validation

Unity6000.5.8f1 Linux64, native graphics, named isolated cloud-offline-menu-pause
profile, candidate71d763a6 plus explicit owned overlays.

Baseline reproduces four offline failures: clock stays1 or0.5, or remains in
hitstop0.05. The fifth network case had a fixture lookup error because the actual
canvas is root-level, not a child of PausePanel. One bounded fixture repair used
the existing named canvas without changing assertions. Final6/6 cases pass:

- Actual synthetic Escape opens and closes the menu at zero/normal speed
- Native scaled time and Rigidbody motion stop, then Resume resumes both
- Repeated open/close and nested Settings retain pause and restore prior0.5 speed
- Networked menu keeps time running and shows its live notice
- Pending hitstop ends; new hitstop cannot unpause the menu
- Destruction restores speed; an explicit exit speed reset is preserved

Fresh nonzero XML, no skipped cases; frozen source hashes unchanged. This is native
menu/clock/physics evidence, not a full match player build, actual online-peer test
or physical controller certification. Existing input routes were not replaced.

## Evidence

- baseline.xml: SHA-256 886d29574fd37acbe8ca7002d3573737b601b43b6ce0f61b37502a951b5913b7
- baseline-inputs.json: SHA-256 4a4d511e9e4f0396615b928c5648e5064ebde64e0d95e2e84eb04c9df517ea2b
- fixed.xml: SHA-256 a7d839b2665ed195b4dcd0f23c4ae732073adc8958d1eb00216f97dddb98120c
- fixed-inputs.json: SHA-256 9c0682bc66f1f1575f8a24e46edefe6ed137139e0e2e633b9279040560021f60
