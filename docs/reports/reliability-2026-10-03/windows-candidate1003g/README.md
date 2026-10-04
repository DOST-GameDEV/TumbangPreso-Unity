# Accepted 1003g packaged artifact and startup

Frozen source e3ddc2f2d9eca1a8fcc4b3bfefeef31497badda9, protocol134, Windows64.
This artifact excludes the later seat-producer fix, movement/economy revisions
and newer Main changes. Do not pair it with current protocol137 clients.

The Windows build completed. The strict frozen-input receipt remains **failed**:
213 import/generated deltas were retained, not hidden. Separate classification
accepted 204 trailing-whitespace-only metadata/settings deltas, the committed
build helper's two shader inclusions in GraphicsSettings and eight generated
identity/warmup assets and metadata. No C# or package inputs changed.

The full packaged manifest contains 258 files and 2,551,899,275 bytes, SHA256
222703b59537b8e41394d8aba4e4962015b5d3508db36d7adbefd18765a3e0eb.
Runtime.dll SHA256 c8159c1db1589930ac9e6f4e50f5fc6b3242d2709f755863863648c97bebc105.
Every packaged file was hashed with unchanged size/mtime checked during hashing.
The files are retained at the absolute artifactRoot in artifact-manifest.json;
no temporary share server has been started.

The existing normal-player startup/menu route passed on a fresh owned profile:
cold loading/login music gate, studio intro, 2,756 silent frames, terms checkbox,
supplied login layouts, revealed-home music, title motion/reduced motion and
hub/settings/credits/mode/back navigation. Normal exit, shared input preservation
and unchanged runtime hash were checked. Receipts are retained here; raw images,
motion samples and logs remain under candidate1003g/Logs/competition-candidate1003g/startup-menu.

No SDK login/signup, physical input, simultaneous peers, online/Relay or hitch
acceptance is claimed by this check. BuildOptions.None means the development-only
performance-only route must not be run against this artifact.
