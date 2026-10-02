# Touch layout collection recovery: no defect reproduced

Source inspection raised a possible null-collection hazard: TouchLayoutStore accepts a parsed TouchLayoutFile and TweakFor/SetTweak directly enumerate or index its Tweaks list. This required actual engine evidence because JsonUtility determines how null or missing collection bytes are loaded. Production code was not patched based on that inference.

One original-source native Unity 6000.5.8f1 EditMode run passed all four focused JSON reload cases. Explicit null Tweaks, missing Tweaks, a normal saved tweak and malformed text all produced usable public TweakFor results and accepted a normal SetTweak edit that survived reloading. Null/missing forms preserved the saved opacity 0.7 and global scale 1.2; normal data preserved the saved offsets/scale; malformed input used existing defaults. Reading did not rewrite the saved preference or bump Revision. The suspected crash did not reproduce for these forms.

No product change, candidate run, repair or repeated validation was made. This is an investigation result, not an additional shipped fix. The NEW proof fixture/meta were retired byte-for-byte from both MAIN and qualification into task-owned Logs after terminal output and preservation checks. Original test sources remain unchanged; retired hashes/paths are recorded in retired-fixtures.json.

Session 44506 ran four selected cases through the serialized CPU pool with 1536MB budget/2048MB reserve, 450-second limit and named `touch-layout-recovery1002` profile. Preparation finished exit0 before launch, with exactly three frozen original-source/fixture/meta inputs. Fresh XML reports 4 passed/0 failed; the guard was terminal, restored and held no lease. All 1289 protected source/private hashes matched and the three native inputs matched their recorded bytes. No task-owned Unity/player process remained at release to the next validation owner.

The fixture preserved/restored the shared touch-layout preference, cached layout object and Revision. The guard independently restored one shared Editor input preference; there were zero existing files in the named task profile. No SDK, input-event simulation, InputSettings clone, scene load or physical touch/phone rendering acceptance was involved.

Original TouchLayoutStore SHA256 remains `14C8E77E112DA939D4BC00277D9675C0B8CBE0A64AC21A9FC2FBE4BBC4E318CE` in MAIN and qualification. Retired fixture SHA256 is `35AC0B694CDA0670B4B6A43589B817DF2AE65AFBABFFA0E2CD680BEE99002354`; meta `C50980F36004EC704E6967DD970AF39B4F41FF5F15AAEE3910D31E970553CD99`.

Raw XML/logs/guard receipts and input/protected manifests are preserved beside this finding. These cases rule out the proposed collection failure under the installed engine and tested bytes; they are not a broad touch-layout audit or a full competition-readiness claim.
