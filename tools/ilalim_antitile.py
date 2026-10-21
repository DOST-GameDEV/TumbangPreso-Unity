"""Anti-tiling for the Ilalim kits' big flat surfaces (ILALIM-1.3).

Owner, 2026-09-29, over an aerial of the east side: "dont we have something that we used in
lagoon and kanto to have random tile rotation and feather offsets so it doesnt tile so much",
then "not only ground but the flat roofs".

This is Kanto's roof fix and the Lagoon's rock fix (KANTO_DESIGN_GUIDE.md section 3,
LAGOON_REWORK_GUIDE.md "ROCK ANTI-TILING"), as one shared node chain: the texture is sampled
again under a ROTATED, SCALED and OFFSET mapping, and that sample is laid over the first through
a big FEATHERED noise mask. Two extra samples here (37 degrees at 0.61, and -71 degrees at 1.43),
each on its own mask, so no repeat lines up at any distance. The masks are soft-edged fields
tens of metres across, never speckle, so the drawing stays flat.

Only for textures without a direction (roof membranes, concrete, lots). Jointed or striped
textures (pavement slabs, siding, pour lines) must not use it: rotation breaks their lines.

Unity (ILALIM-1.4) needs the same thing in the map shader: two extra rotated samples and a
world-space noise mask.
"""
import math

SAMPLES = (
    # rotation (degrees), scale, offset, mask noise scale, mask seed offset
    (37.0, 0.61, (0.37, 0.71), 0.35, (0.0, 0.0, 0.0)),
    (-71.0, 1.43, (0.13, 0.52), 0.23, (11.3, 5.7, 0.0)),
)


def anti_tile(nodes, links, vec, image, colour):
    """Blend rotated samples of `image` (read at UV socket `vec`) over `colour`; returns the new
    colour socket."""
    for rot_deg, scale, offset, mask_scale, seed in SAMPLES:
        mp = nodes.new("ShaderNodeMapping")
        mp.inputs["Rotation"].default_value = (0, 0, math.radians(rot_deg))
        mp.inputs["Scale"].default_value = (scale, scale, 1)
        mp.inputs["Location"].default_value = (*offset, 0)
        links.new(vec, mp.inputs["Vector"])
        tex = nodes.new("ShaderNodeTexImage")
        tex.image = image
        links.new(mp.outputs["Vector"], tex.inputs["Vector"])
        shift = nodes.new("ShaderNodeVectorMath")
        shift.operation = "ADD"
        shift.inputs[1].default_value = seed
        links.new(vec, shift.inputs[0])
        noise = nodes.new("ShaderNodeTexNoise")
        noise.inputs["Scale"].default_value = mask_scale
        noise.inputs["Detail"].default_value = 0.0
        links.new(shift.outputs["Vector"], noise.inputs["Vector"])
        feather = nodes.new("ShaderNodeMapRange")
        feather.interpolation_type = "SMOOTHSTEP"
        feather.inputs["From Min"].default_value, feather.inputs["From Max"].default_value = 0.42, 0.58
        links.new(noise.outputs["Fac"], feather.inputs["Value"])
        blend = nodes.new("ShaderNodeMix")
        blend.data_type = "RGBA"
        links.new(feather.outputs["Result"], blend.inputs["Factor"])
        links.new(colour, blend.inputs[6])
        links.new(tex.outputs["Color"], blend.inputs[7])
        colour = blend.outputs[2]
    return colour
