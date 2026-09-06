# Bot behaviour across seeds

- **Commit** `2fde55d32246105a22237b31bdf29f2269f2fe5e`
- **Config digest** `447386b384c4` (Balance.cs, AiTuning.cs, MatchRules.cs, ThrowRules.cs)
- **Generated** 2026-09-06T15:53:03
- **Mode** Classic on Eskinita
- **Seeds** 20260823, 1, 7, 4242, 20260904, 99991

⚠️⚠️ **READ THE SPREAD BEFORE THE MEAN.** `CLAUDE.md` § 7.1: these numbers are liveness floors, never comparisons at n = 1. A change worth less than the spread below has not been measured by this probe, however different the two runs look.

| Metric | n | min | max | mean | median | stdev | spread |
|---|---|---|---|---|---|---|---|
| lata knocks | 6 | 69 | 84 | 77.2 | 77.5 | 4.7 | **19.4%** |
| tags | 6 | 137 | 156 | 144.5 | 143.5 | 5.9 | **13.1%** |
| sabotages | 6 | 0 | 7 | 4.8 | 6.0 | 2.6 | **145.8%** |
| throws | 6 | 175 | 190 | 181.8 | 183.0 | 5.4 | **8.3%** |
| retrievals | 6 | 170 | 184 | 176.2 | 177.0 | 4.9 | **7.9%** |
| camp penalties | 6 | 0 | 1 | 0.2 | 0.0 | 0.4 | **500.0%** |
| idle penalties | 6 | 0 | 0 | 0 | 0.0 | 0.0 | **0.0%** |
| skill uses | 6 | 0 | 0 | 0 | 0.0 | 0.0 | **0.0%** |
| ultimate uses | 6 | 0 | 0 | 0 | 0.0 | 0.0 | **0.0%** |
| slides | 6 | 123 | 136 | 129.7 | 129.0 | 4.7 | **10.0%** |

## Every arm the same launches produced

⚠️ **`slides` against `retrievals` is the ratio to read**, not the slide count on its own. `docs/TODO.md` § 146: nobody using it means the recovery is too long and normal retrieval stopping means it is too cheap, and only the fraction of retrievals that were a commitment can tell those apart.

| arm | n | lata knocks | tags | sabotages | throws | retrievals | camp penalties | idle penalties | skill uses | ultimate uses | slides |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Classic on Eskinita | 6 | 77.2 (69-84) | 144.5 (137-156) | 4.8 (0-7) | 181.8 (175-190) | 176.2 (170-184) | 0.2 (0-1) | 0 (0-0) | 0 (0-0) | 0 (0-0) | 129.7 (123-136) |
| HeroStrike on Eskinita | 6 | 71.8 (62-77) | 135.8 (132-140) | 3.8 (0-7) | 178.2 (176-181) | 172.3 (169-176) | 0.2 (0-1) | 0.3 (0-2) | 31.8 (30-37) | 14.3 (13-15) | 122.3 (114-132) |
| HeroStrike on IlalimNgTulay | 6 | 74.5 (67-80) | 130.5 (124-137) | 4 (1-6) | 176.7 (166-192) | 169.8 (160-183) | 0 (0-0) | 8 (0-48) | 31.3 (28-35) | 14.3 (14-15) | 122.8 (114-135) |

## Every run

| seed | lata knocks | tags | sabotages | throws | retrievals | camp penalties | idle penalties | skill uses | ultimate uses | slides |
|---|---|---|---|---|---|---|---|---|---|---|
| 20260823 | 80 | 137 | 5 | 185 | 179 | 0 | 0 | 0 | 0 | 131 |
| 1 | 76 | 141 | 7 | 190 | 184 | 0 | 0 | 0 | 0 | 135 |
| 7 | 75 | 156 | 3 | 183 | 177 | 1 | 0 | 0 | 0 | 136 |
| 4242 | 79 | 143 | 0 | 175 | 170 | 0 | 0 | 0 | 0 | 127 |
| 20260904 | 69 | 144 | 7 | 175 | 170 | 0 | 0 | 0 | 0 | 126 |
| 99991 | 84 | 146 | 7 | 183 | 177 | 0 | 0 | 0 | 0 | 123 |
