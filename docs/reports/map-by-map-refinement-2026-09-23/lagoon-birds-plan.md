# Lagoon flying birds, owner2026-09-24

Lagoon has no AmbientLife component (its GUID is absent from the scene), and the
existing author covers only the other four maps. Reuse the existing native kalapati
and maya models and fly clips. They are village visitors, not a claim about a
specific local species assemblage. Source fly phase0has spread wings for a glide.

Extend the existing AmbientLife actor with opt-in aerial wandering, used only by
Lagoon. Three small birds get separate starts, heights, destinations and timing.
Each leg chooses a new goal within a clear air volume above the village and water
near the islands, follows a curved path, banks smoothly and alternates wingbeats
with short glides. Do not traverse a fixed waypoint loop or synchronize all actors.
Use the existing private cosmetic random stream. No colliders, score, navigation,
network state or gameplay random change. Pause with scaled time. Other maps retain
their existing ground-animal and perched-bird branches.

Use broad clear air above the houses, not perfect bird obstacle/path tooling. Keep
native body size and sparse count for readable action. Initial scope is varied
flight; this does not close the broader REFINE-2.7natural-animal review.

One native focused run should show world scale and close flap/glide poses, actual
movement and wing changes, bounded altitude and pause behavior. Inspect the real
result, publish and return to the preserved house/material/water/island queue.
No new capture framework, native build or unrelated animal rewrite.
