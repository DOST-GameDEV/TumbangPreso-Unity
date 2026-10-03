# Radial purple Haunted sight

Human Feedback asks for radial rather than flat nearsight, dark purple matching
Nemu rather than black. Current ColourGrade uses only eye-space Z: a planar
visibility band. Interpret radial as real camera-distance falloff, preserving
2.5m clear / 7m obscured thresholds rather than introducing a new gameplay range.
Use view projection to convert eye depth to radial distance. Nemu's existing
ink palette supplies the deep-purple terminal colour. Do not change status
duration, targeting, audio, markers, networking or finalized hero kits.

First capture the shipping shader against controlled depth at normal aspect
ratios. Verify an equal-depth plane fades radially and becomes purple at far
samples; verify the un-Haunted pass remains unchanged. Then inspect actual
victim-camera output and expiry/isolation before closing the human note.
