# Eskinita distant landscape staging, 2026-09-24

The two active supplied mountain paintings now sit lower behind the neighborhood.
Their texture, material, width, aspect, horizontal placement and shadows are unchanged.
The third historical MountainBackdrop is inside inactive Malayo and stays inactive.
The author changed only two scene transform positions; it did not rebuild the map.

Compared the identical 5_W and 1_E bright game-camera views with garden-v1. The
oversized pale peaks no longer compete with roofs, trees and utility lines. The
neighborhood carries the middle distance and the retained moving clouds carry the
sky. The low ridges can be occluded by houses at street height, which is appropriate
for this urban composition. Greyscale25 comparison and actual preview/small image
were also inspected. Existing house/preview cases passed2/2in6.244s.

The preview itself still shows the old dark rig and grade, unlike the game views.
This is LIGHT-1.8 integration still missing from the lighting branch50f1fc255,
not a reason to add brighter materials or more geometry. Complete that integration
before choosing final map cards. Static cards and whole-map acceptance remain open.
