# KEEP EDITING returns navigation to live Settings

The current unsaved-changes dialog has its own nested ScreenFocus. Hiding its
selected KEEP EDITING button leaves EventSystem on an inactive object. The base
Settings control count excludes that modal and does not rebuild on closure.

A shared CloseDecision helper captures whether selection belongs to the dialog,
hides it, then rebuilds the base focus only for that owned selection. The current
KEEP EDITING callback and existing Escape dialog branch share it. Dirty values,
clean BACK and another live screen's selection remain intact. No layout, input
asset, SDK, account or gameplay change is included.

Three real PlayMode cases open the current TumpSettingsView and dirty decision,
invoke the actual KEEP EDITING button, preserve dirty session values and check
active current selection immediately and next frame. Two controls preserve a
foreign live canvas selection and the clean BACK callback.

Initial original session44421 ran ZERO tests: a33-character fixture metadata GUID
made Unity ignore the new fixture. Its XML/log/metadata and all first input hashes
are retained. One metadata-only correction supplied a valid32-character GUID; no
assertion, fixture code or production hunk was changed. Corrected original12978
passed2 controls and reproduced1 causal inactive-selection failure. Candidate34733
passed3/3 against the identical corrected fixture. No further repair or unrelated
suite was run.

Unity6000.5.8f1/D3D11/960x540, serialized GPU2048MB plus2048MB reserve, profile
settings-decision-focus1002,450s ceiling. Each prep completed exit0 before launch.
All12518 protected qualification hashes unchanged after final; first and repaired
protected manifests are identical. Four source/fixture/meta bytes frozen and
verified. All three guards terminal/restored/no lease. Protected manifests include
source, metadata and selected asset/settings/package inputs; unrelated private
MAIN changes remain excluded from publication.

This accepts current callback/EventSystem behavior and unchanged controls. Escape
shares the helper in source; physical controller/keyboard dispatch, pixels, live
services, standalone inclusion and whole competition readiness are not claimed.
