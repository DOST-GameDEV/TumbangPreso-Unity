# Held tsinelas palm contact

The default tsinelas had an extra 7.87 mm gap above the hand anchor for all nine current heroes. The anchor already sits 3 mm above the authored palm surface. Carry support reused the 45 mm ground/flight clearance floor even though this shoe is thinner.

For valid meshes, carry support now uses measured thickness with a 1 mm degeneracy minimum. The missing-mesh fallback and loose/flight placement are preserved. An independent regression reads actual mesh vertices after the carry LateUpdate rather than repeating its support formula. The strict pre-fix test fails on nine tsinelas pairs. After the change all 90 combinations of nine heroes and ten selectable shoes have zero measured extra gap. Six native controls pass including movement/missing-anchor, first-person possession, model swaps and replay/ultimate copies. All 21,449 protected inputs and preferences restore.

The first wrong-selector run was stopped after exact process ownership checks, restored and excluded from slipper evidence. The initial wider tolerance run revealed the gap; the strict failure and fixed controls are retained here.

The current Windows package still predates this carry fix and Desktop remains the qualified 001d release. Actual Windows carry/replay/startup/previews and broader ground/air/hero-transition/full-match quality remain pending.
