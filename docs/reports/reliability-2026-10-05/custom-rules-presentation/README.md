# Custom Rules presentation

The actual c171 Windows room's Rules screen still used a pale olive ground,
red text actions and a plain text Done control, unlike the current warm hub.
The change uses the existing maroon/night/honey/persimmon palette, a shared
scalable rules plate and a chartreuse primary action. Rule values, explanatory
copy, tabs, positions, hit targets, host authority and callbacks stay unchanged.
Original typefaces remain; no new artwork, textures or raster upscaling is used.

One focused native PlayMode review on Unity6000.5.8f1 renders the actual match
page at1920x1080,2560x1440 and3840x2160, then switches to the private-room page
at1080p and4K. The private password field remains present and the4-round/90s
rules remain unchanged. Corrected PID10856 exits0 and the case passes. Both
1080p pages were visually inspected; the five native captures accompany this report.
This is local UI acceptance, not a controller/touch or whole multiplayer pass.

The first fixture run (PID23428) selected the canvas from the wrong parent and
failed before capturing. The owner canvas is held separately by the screen;
the corrected fixture selects that actual stored canvas. Production changes
were identical across the runs. The first failure is retained as fixture evidence.
Both native parents are terminal and restore named profiles, shared input/editor
preferences and QualitySettings. Their202 generated UI metadata changes are
retained locally then restored to exact pre-run bytes; no frozen deltas remain.

The two-machine network test continues on its frozen c171 package/source151.
This presentation change is separate from that test's evidence.
