# Cold start of the shipped player

- **Commit** `94c8fb54c369625b198ff71bb764b3aab758f5d3`
- **Artifact** `C:\Users\matth\Desktop\TumbangPreso-Unity\TumbangPreso.exe`
- **Built** 2026-09-05 09:30:50, protocol 24, StandaloneWindows64
- **Profile cleared** NO (opt in with --clean-profile)
- **Preset** CLASSIC tournament (docs/VISION.md 1.1)
- **Gate** NATIONALS CANDIDATE
- **Artifact tree state** `clean`, UGS `dcf0831e-a5f4-43b4-832e-b687f13a3569` / `production`
- **Generated** 2026-09-05T09:39:39

## Verdict: PASS

| Step | Verdict | Seconds | Detail |
|---|---|---|---|
| the artifact is a nationals candidate | PASS | 0.0 | SHA 94c8fb54c369, tree clean, protocol 24, StandaloneWindows64, UGS dcf0831e-a5f4-43b4-832e-b687f13a3569/production |
| launches and identifies itself | PASS | 3.0 | clean |
| reaches the arena and exits cleanly | PASS | 47.2 | clean |
| a real CLASSIC tournament round became active | PASS | 0.0 | round 1, active, 3 seats driving, 45.0 s sampled |

### What the player believed at exit

```
TUMBANG PRESO NETWORK STATE REPORT

role            : HOST
networked       : True
local slot      : 0
protocol        : 24
mode            : Classic
map             : Eskinita
sampled         : 45.0 s
round           : 1
defender        : 0
round active    : True
lata upright    : False   flips: 1
tournament ruleset : OK
tournament modifiers : none
build identity  : TUMBANG PRESO 1.0.0 | 94c8fb54c369 | protocol 24 | StandaloneWindows64

seat   char   bot      origin  taya   score  travelled  skills  ults
--------------------------------------------------------------------------
0         0 False       Human  True      20        0.5       0     0
1         3  True         Bot False       0       33.3       0     0
2         6  True         Bot False     100       26.7       0     0
3         9  True         Bot False       0       27.3       0     0

```

⚠️⚠️ **THE LAST TWO ROWS ARE DIFFERENT CLAIMS AND USED TO BE ONE.** *"Reaches the arena and exits cleanly"* is about the PROCESS: it launched from a cleared profile, identified itself, loaded a map, installed four bots and came back. *"A real round became active"* is about the GAME. `docs/TODO.md` § 143.15 is a green run of the first printed under the second's name, with `round: 0` in its own capture.

⚠️ **A truly clean MACHINE is still a human test.** This clears the profile at most; it cannot clear a driver, a firewall rule, a codec or a Visual C++ runtime that this machine has and a borrowed one does not. `Attention.md`.
