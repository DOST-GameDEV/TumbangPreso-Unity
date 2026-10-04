# Basilio boulder launch velocity

Baseline3af811d31, Unity6000.5.8f1, Windows D3D11 isolatedqa-a nativePlayMode. DanteBoulder.Spawn assigned the unit direction returned by Slipper.SolveArc directly as velocity. The authored GeoRules.BoulderSpeed14m/s was used only to solve direction, never to scale movement.

The new real-flight regression samples five fixed steps after spawning the shipping boulder. Original test failed0.088814497m against expected1.24339771m. The focused candidate multiplies the solved direction by the existing authored speed. Art, cast, hit radius, roll range, ownership and can-contact rules remain unchanged. Candidate2/2PASS: actual flight1.2434m matches expected1.2434m, and the existing authored cast/held-shoe control passes. Runner12238/Unity34640 terminal0. All19284inputs verified;210generated metadata/auditor deltas preserved and restored,9preferences and QualitySettings restored.

Original runner38818/Unity35480 terminal2;19284frozen inputs,210generated metadata/auditor deltas preserved before/after and restored,9existing preferences and QualitySettings restored. Candidate adds the existing authored cast/held-shoe control to the same flight test. Packaged peer and human feel acceptance remain separate.
