# All-map render availability: first probe failure retained

First graphics probe92803 on1003d/source83f54e961/protocol134 failed at Ilalim
with NullReferenceException after Eskinita/BayanPlaza each rendered all three
profiles. Raw six rows/eight images remain; not an all-map pass. Normal native
exit2, shared input restored, Runtime unchanged and lease released. Six registered
maps and three profiles require18 rows; actual6 is correctly refused.

Static inspection of the actual Runtime DLL locates failing IL0x407 at
UnityEngine.Component.get_transform immediately after FindFirstObjectByType of
TumbangPreso.Visual.EnvColourPass. The current Ilalim scene contains its authored
Dressing root but no old EnvColourPass script GUID. This is a diagnostic lookup
assumption, not evidence that the world failed to render. No assets regenerated.

ONE diagnostic repair now measures active-scene authored Dressing when the old
colour pass is absent, still requiring a nonnull root and exact map/profile
coverage/positive counters. Existing-colour-pass path is unchanged. Corrected candidate28332 passes exact18rows across Eskinita,BayanPlaza,
IlalimNgTulay,SaBubong,LagoonCove,Kanto and Low/Balanced/High, positive SetPass
and triangle counters,24captures and normalexit0. All six Balanced world images
were inspected: authored worlds/parked bodies/viewmodel render, no fallback-pink
or empty-world observation in those views. Shared input restored/Runtime immutable/
leasefree. ONE lookup repair, no further repeat/repair, originalfalse preserved.

Frozen1003e source14dfe9e416f5a98698844d9da34ab3c34b849a17/protocol134/recording13,
Runtime501f0a02db575003c48910f08bdd0221810031c4d30aaf499f4727bbeca0c806.
First build15625 succeeds2433MB/81s; prepare70360/finalizer57666 terminal.
Strictfalse two generatedIDs; artifactclassifierPASS/all18,963 otherinputs unchanged.
Private MAIN overlays excluded; retained importer/settings dirt recorded.

This is staged Classic rendering with four parked actors and process-level frame
windows on this GPU. It does not qualify worst-case combat, every camera/effect,
Hero map interactions, recovery, WAN, Android or physical-input quality. First actual cold completed-arrival21607 also passes on this currentartifact:
normal LAN lobby/supportedHero1/30, completeclient Unicodechat inboth logs,
HOME/coldrejoin/newscene/samerecord/four frozenactors. Bothnormalexits/input+seed
preservation/Runtimeimmutability/free. Bothcareersonehistory/queue/witness and
ownhumanline, matchingonlinerecord/scores130/0/0/150/clearmarkers. This is normal
valid-traffic acceptance after rebindguard; no craftedlivefault or full-body/input
restoration matrix claim.
