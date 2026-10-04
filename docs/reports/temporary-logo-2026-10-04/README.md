# Temporary owner TUMP logo

Supplied PNG retained byte-for-byte in the existing brand paths. Visible ink bounds
are339,133–1598,961 in the1920x1080 export. Sprite UVs exclude only its transparent
canvas; existing layout rectangles, colour and aspect fitting remain.

OwnerUiTheme logo, login3-logo and fallback brand UI now use this same sprite.
This covers separate login, loading, credits, settings, picker and play-menu logos.
Old screen atlas pixels remain untouched. The title-screen wall painting baked
into its background is not changed by this separate-UI-logo swap.

First native check caught the old power-of-two importer resizing the new PNG
to2048x1024 and distorting its visible aspect to1.7106. Unity's TextureImporter
was used to disable NPOT rescaling; GUID and other importer settings preserved.
Same scoped case then passes1/1,0.8559084s09:31:08–09UTC. Credits pixels inspected,
all three sprite routes and supplied ink aspect asserted. Input hashes unchanged,
settings restored, Editor exit0, no OOM event increase.

Actual target-device rendering and the baked title illustration remain separate.
