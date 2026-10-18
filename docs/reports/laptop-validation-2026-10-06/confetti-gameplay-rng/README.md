# Confetti preserves gameplay randomness

## Original defect

Claude UIca4778b94 adds26 confetti blocks for earned moments. Each Burst samples UnityEngine.Random six times per block, the same stream used by AIController for ordinary bot choices, aim spread, lapses and timing. On exact53d79142b2fa3ac85dc5c802ab0525b23967ffd9, a native seeded-stream check observes the next gameplay value change from0.566583812 to0.668526351 solely because Burst ran. This confirms shared-stream consumption, not a measured desync, tournament outcome change or general fairness guarantee.

Original corrected fixture Unity55016 has one actual RNG failure and one passing visual draw/lifetime control, exit2. The initial Unity6208 run also had a fixture AmbiguousMatchException from selecting the inherited OnPopulateMesh overload by name. Its actual RNG failure remains valid, but the second failure is not a product bug. The fixture is corrected to select the exact VertexHelper signature; the original failed receipts are retained. No assertion is weakened or log failure suppressed.

## Focused correction

Burst saves/restores Random.state in try/finally around the existing cosmetic rolls. The same26 blocks, six draws per block, colours, ranges, mesh code and unscaled animation/lifetime remain. No random service, gameplay RNG redesign, AI tuning, layout/font/hero or art change is included. Restoration also occurs if cosmetic setup throws; exceptions still propagate.

The same native cases compare four subsequent random samples exactly and require a real nonempty pool mesh while live, retirement after its lifetime and zero mesh after expiration. The control reads the protected mesh output without mutating the simulation. This qualifies that small API/lifetime boundary, not human visual approval or all existing VFX RNG uses.

## Evidence

Local Windows11 gamergmae, Unity6000.5.8f1, isolated named worker profile. Candidate Unity35876 has actual2PASS/0FAIL, exit0, using the exact same corrected two-test fixture. The only candidate input change is HudConfetti.cs; all other21205 inputs match. Original and candidate each retain279 import deltas before full restoration. Both qualified runs preserve source/import deltas and restore21206 frozen inputs,13 existing editor preferences and four original profile files. Current UIca477 source remains distinct from the earlier1856UI7 and80440 fixed-frame match evidence. Raw XML/source/fixture/launch/restoration receipts retain exact bytes under folder attributes; SHA256.json covers all retained evidence.
