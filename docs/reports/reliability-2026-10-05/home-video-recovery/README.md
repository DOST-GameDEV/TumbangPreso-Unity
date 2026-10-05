# Recover Home animation after failed boot preload

A failed hidden boot preload was adopted by visible Home with its permanent
failed flag. The first Home then kept a static poster even when a new decoder
could work. This is a demonstrated failure path; the latest ordinary operator
log had no matching decoder warning, so it is not a confirmed explanation of
every first-launch animation report.

Home now discards a failed hidden player and creates one fresh player for the
same hero. Its poster stays visible while preparing. A healthy preloaded player
still transfers unchanged and reduced motion still opens no decoder. This does
not add repeated retries after a failure in visible Home.

Graphics-enabled Windows Unity6000.5.8f1 PlayMode used the real VideoPlayer and
shipping clip. The new reproduction warms it, delivers a controlled decoder
error, follows the ordinary Install handoff and asserts decoded frames advance
on the resulting Home. Original16720: causal failure plus two controls passing.
Candidate19548: same3/3 pass, including actual playback and disposal of the
failed player's resources. Raw results and concise receipts are adjacent.

Both parents exited and input/settings/Quality were restored. Per-run generated
202 owned UI metadata changes were retained then restored to exact frozen bytes.
Pre-existing Auditor dirt and unfinished Yasmin source remain protected. This
is native decoder/lifecycle evidence, not a new packaged cold-start film or all
Windows decoder/hardware acceptance.
