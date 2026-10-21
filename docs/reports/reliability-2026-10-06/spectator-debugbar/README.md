# Retire gameplay debug chrome while watching

The laptop's natural Arena frames showed an Editor debug strip over the spectator HUD, advertising gameplay seat shortcuts. Those shortcuts now yield to the watcher. The debug canvas now hides for networked play and launch/HUD spectator state, while preserving its existing solo gameplay appearance and release-build removal.

Unity9024 exits0 with one temporary native role/render case passing: ordinary gameplay strip visible, launch spectator hidden, HUD-only spectator hidden and gameplay return visible. The two isolated960x540 renders were inspected; the black strip remains in gameplay and is absent while watching. These UI-only renders do not qualify live map background or physical controls. The earlier natural Arena image supplies the original presentation evidence.

All21154 frozen inputs and216 native metadata/settings deltas restore, along with input/editor preferences, quality settings and seed. The temporary capture fixture is retained here and removed from Assets after its terminal restoration; no new permanent test is introduced for this small visibility adjustment. No hero, camera solve or network contract changes.
