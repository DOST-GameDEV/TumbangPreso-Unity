# Black in-game UI outlines

Owner rule2026-09-22: replace red/burgundy in-game UI outlines with black.
UiTheme.InGameOutline is black and gameplay text/icon constructors now use it:
main HUD, scores, clocks, prompts, spectator hints, notifications, ability hints,
replay/training captions, off-screen icons and in-match chat. Role/ability/text
fills and supplied artwork remain unchanged. No character/environment outline
or model was recoloured. The rule is stored in AGENTS.md.

Native Windows v55 built1211MB/83s,guard32ee8acef6a6. Its actual spectator startup
and manual-flight view show black borders around the HUD and the exact control
hint identified in the owner's photograph. The native image was inspected.

The wider existing spectator review did NOT pass: after those successful views it
reported world-held slipper visibility in POV. That independent finding is retained
in result.json and runner-result.json; it is not waived or described as a full
spectator pass. Input/profile preservation passed. No test was added for a simple
colour change. Continue the saved accessibility work and wider backlog afterward.
