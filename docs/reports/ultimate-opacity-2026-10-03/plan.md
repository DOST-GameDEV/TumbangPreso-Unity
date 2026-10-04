# Ultimate-screen opacity investigation

Human Feedback describes a translucent/low-opacity ultimate screen and asks for
clarity like the tag replay. First measure the actual shared ultimate render
texture and canvas. Distinguish intended 0.12-second return/reduced-motion entry
fades, texture alpha, HDR/tonemapping and camera world-look scoping. Do not assume
that missing WorldLookCamera is the cause: WorldLookPresentation already names
UltimateSceneCamera explicitly. Preserve finalized hero presentation and timings.
Use a small actual hero/camera stage before any production change.
