# Eskinita background staging, 2026-09-24

Observed in the bright garden frames: supplied Mountain.png becomes an oversized
pale triangle behind low neighborhood buildings. The painting itself has brush
texture and must stay intact. Three existing unlit transparent quads retain its
aspect, colors, alpha and material; their current elevated centers put visible
peaks roughly50m above the ground at150-180m distance.

Lower those three existing centers independently to -3m, -6m and -4m, preserving
horizontal position, aspect and width. Because the top22percent is transparent,
this leaves visible ridges roughly14-23m above the distant district, with their
bases below the horizon. These are backdrop cards, not reachable terrain.
The intended effect is a low distant landscape behind the inhabited neighborhood,
not mountains competing with house silhouettes. No painting edits, new shaders,
new hills, runtime systems or changes to the lighting branch are needed.

Prior PEAK/A Short Hike composition research supports layered foreground and
background with quieter distant forms; this is a TUMP composition inference.
The existing moving sky now has a successful blue/warm cloud balance under the
adopted lighting. Preserve its animated panorama and judge again at overview/eye
height before adding more clouds. Use the existing PrimaryHomesMaterialReview
against the garden after frames and the actual preview route; no new tests.

Actual author found two active cards. MountainBackdrop sits under inactive Malayo;
retain that inactive state. Only active Quad and Quad(1) positions changed.
