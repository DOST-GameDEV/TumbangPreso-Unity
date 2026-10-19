"""PAYO night fires (2026-10-19): the shared atlas layout for the fire pedestal and the standing torch.

Owner, 2026-10-19, of the Payo map at night: "could also use some torch lights or those stone pedestals with fire in
them". Both pieces wear ONE material (`payo_fire_kit`) on ONE 512 px atlas, painted by fire_paint.py (system Python)
and mapped by fire_kit.py (Blender). This module is plain Python so both can import it.

A region is (x0, y0, x1, y1) in atlas pixels, y DOWN the image (row 0 at the top). A UV for Blender is u = x / SIZE,
v = 1 - y / SIZE. Each lathe ring or box face is mapped across a region: u along the way round, v up the height.
"""
SIZE = 512
PPM = 160.0          # about 160 pixels a metre round the pedestal (its column is 2 m round): soft, not noisy

R = {
    # the pedestal's stone: one warm sand colour, lighter at the top, darker and cooler at the foot (the calm rule)
    "stone":      (0, 0, 256, 160),
    # the carved band round the column: bold fret and diamond, ochre and terracotta, carved as relief
    "band":       (0, 160, 256, 224),
    # the bowl's rim: the woven red-black-white diamond band of the court's copings
    "weave":      (0, 224, 256, 256),
    # the plinth tops and the bowl's inner wall: plain paving stone
    "flat":       (0, 256, 256, 384),
    # the bed of coals in the bowl (and in the torch's head): charcoal with a few dark red seams
    "coal":       (256, 0, 384, 128),
    # the bamboo pole: yellow-green to ochre, a node line every segment
    "bamboo":     (384, 0, 512, 256),
    # the torch's head: a basket of split cane, a few big diamonds of weave
    "basket":     (256, 128, 384, 256),
    # the bindings (rattan, red-dyed) and the torch foot's river stones
    "binding":    (256, 256, 384, 288),
    "river":      (256, 288, 384, 384),
}


def uv(region, fu, fv):
    """A point inside a region: fu 0..1 across, fv 0..1 from its BOTTOM to its top."""
    x0, y0, x1, y1 = R[region]
    # keep 3 px off the edges so the mip levels never reach the neighbour
    x = x0 + 3 + (x1 - x0 - 6) * fu
    y = y1 - 3 - (y1 - y0 - 6) * fv
    return (x / SIZE, 1.0 - y / SIZE)
