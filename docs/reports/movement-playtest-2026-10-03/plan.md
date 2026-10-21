# Movement playtest revision

Owner's October 3 16:27 Manila playtest request supersedes earlier same-day values.
Attacker walk/run 2.5/5 m/s; defender 3.75/7.5 m/s. Jump target 1 m / 0.5 seconds
uses launch 8 m/s and character gravity 32 m/s². Projectile gravity stays 20.
Stamina 250, drain/regen 100/s, delay 1 second, start floor 50, fatigue 2.5 seconds
with no walking penalty. Shove and defender lunge must work with zero stamina
and during fatigue, on both predicted/local and host paths. AI cannot refuse a
free shove for low stamina. Separate conditional retrieval slide keeps its
previous explicit 25-point cost; decouple it from the now-zero shove constant.
Other distances, cooldowns and active-effect modifiers are unchanged. Protocol 138.

Qualification: managed rules, native eight-speed matrix, measured real jump,
fatigue walking, existing lunge timing, local shove and host shove/lunge with
empty fatigued bar. Preserve original stale-contract failure and output.

Initial process-scoped compiler experiment: DOTNET_GCHeapHardLimit=0x20000000 (512 MiB),
per Microsoft's documented hexadecimal environment setting. This constrains
.NET compiler children, not Unity gameplay parameters. Keep all RAM/disk/time
guards. Source: https://learn.microsoft.com/dotnet/core/runtime-config/garbage-collector
