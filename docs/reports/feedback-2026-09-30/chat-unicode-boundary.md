# Chat Unicode boundary

The120-unit chat clamp could cut a valid emoji's UTF-16 pair in half. A message
with119ordinary characters followed by a supplementary character produced an
unmatched high surrogate; strict UTF-8 encoding of the clamped result failed.
The native baseline reproduces that failure while four other boundary/length /
newline/empty cases pass.

The boundary now backs up one unit when its last retained character is a high
surrogate. This also handles uGUI's InputField applying characterLimit first:
its own Substring can pass an already-clipped half pair to the network clamp.
The120-unit maximum, whitespace/newline handling and ordinary text are unchanged.
This is boundary repair, not a full Unicode grapheme segmenter or font-support claim.
No gameplay protocol, identity or wire layout changed.

Six native EditMode cases pass on Unity6000.5.8f1 Linux64: both pair boundaries,
an actual uGUI InputField pre-clipping case, and the existing length/newline/empty
contracts. Runtime and test input hashes stayed frozen. The first correction
passed five cases; inspecting the real InputField source identified the additional
pre-clipped route, which is included in the final correction and six-case result.

[Original failure](checks/chat-unicode-before.xml).
[Final six passes](checks/chat-unicode-final.xml).

No end-to-end peer chat, physical keyboard/phone, font-rendering or grapheme-cluster
qualification is claimed. Feedback-row addition is pending current Doc access
confirmation; no blocked write was bypassed.
