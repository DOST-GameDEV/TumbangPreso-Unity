# Correct units for observed lunge charge

The prior4556 AI age synchronization treated CombatVerbs.ObservedLungeCharge as elapsed seconds. Its actual contract is a clamped ratio with-1 for no active wind-up. That was my implementation mistake. The existing24 controls did not include a nearby half-charge minimum-hold boundary and therefore missed it.

Original native48184 passes the prior three cases but fails the new0.25second/1.7metre case: the bot releases early and spends0.961538seconds of dash cooldown. The corrected producer multiplies the ratio by Balance.LungeChargeTime before comparing elapsed ages and advancing its timer. Reach prediction continues using the actual consumer LungeChargeRatio; no extra physical power is invented.

Candidate45240 passes all25 continuity, minimum-hold, motor-turn, aim, power, obstruction and lifetime controls. Both jobs terminate before restoration; all21369 frozen inputs and preferences restore exactly. Original failure and earlier24-green evidence remain intact. This is the corrected source boundary, not complete natural-match or hit-rate acceptance.

The diagnostic also previously estimated charge duration as frame count multiplied by the latest frame duration. Variable frame timing makes that estimate unreliable. It now retains the normal consumer's actual private charge accumulator before the release edge and reports seconds explicitly, alongside the public ratio. Old short-release/power labels cannot be retroactively repaired from missing samples. The new observer compiles with this native candidate; its natural runtime exercise follows separately.

The fourteen map/mode baseline cases all passed their current bounds/retrieval/pursuit checks on the preceding source. They include many missed lunges and the pre-correction unit defect. Held-slipper boundary warnings in Eskinita concern an attached hand beyond the body boundary, not demonstrated loose or floating slippers. These baseline traces are preserved separately and do not establish final AI quality or tournament readiness.

Desktop725e remains the independently verified release; no package replacement is claimed from these Editor checks. Laptop effects/preview/Archive/floor work remains reserved. No LAN, new worker/service or foreground control was used.
