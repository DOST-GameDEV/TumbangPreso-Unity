# Lagoon water presentation,2026-09-24

Native views show a broad nearly uniform cyan surface obscuring the bed, submerged
piles and hull contact. Current shader uses0.78near opacity, a shallow colour largely
independent of authored water tint and very small ripple variation. The real bed is
1.9m below the surface, with a180m-square collider/renderer. Preserve physical water,
seabed collision, swimming, new platform-fall recovery, stock float and boat motion.

The viewed Embassy stilt-village photo shows readable shallow seabed/piles and
working boats in calm water. Adapt that depth/material relationship to TUMP's simple
bright style. Keep geometric/colour clarity, gentle moving light and quiet highlights;
no photographic reflection, noisy checker caustics or dark ocean replacement.

Read [Unity's camera-depth documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/SL-CameraDepthTexture.html).
Depth-based intersections require camera depth output and opaque shadow-caster
coverage. The first local treatment will instead use the existing transparent water
and real bed, avoiding a new shared camera-depth ownership system: reduce near
opacity, retain stronger grazing/far coverage, add broad restrained surface/bed
variation and legible gentle ripples. Judge physical submerged geometry before
adding any contact-foam system.

Extend only the visual bed mesh under the water plane so clearer water does not
expose its old square edge. Keep its existing collider/transform/height unchanged.
Add quiet sand/vegetation colour fields and preserve moving caustics on the actual
bed. Shader refinement toggle supports honest same-camera before/after evidence;
no UI or gameplay setting is added. Both shaders are Lagoon-only.

Inspect actual preview, pile/water edge and houseboat at matched cameras/time, plus
Low and two held time samples. Check shader errors and unchanged physical surfaces.
One useful pass, then refine demonstrated faults only. Boats and island/coast/sky
remain separate following tasks; no whole-map or performance completion claim.
