using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Spawns and manages hazard entities, barriers, zones, and visual effects for hero abilities.
    /// </summary>
    public static class HeroHazards
    {
        // -------------------------------------------------------------------
        // ICE WALL BARRICADE (Cheska Skill 2)
        // -------------------------------------------------------------------
        public static GameObject SpawnIceBarricade(Vector3 position, Vector3 forward, float duration = 6.0f)
        {
            var go = new GameObject("IceBarricade");
            go.transform.position = position;
            go.transform.rotation = Quaternion.LookRotation(forward);

            // Create wall geometry: 3.6m wide, 2.2m tall, 0.4m thick
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "WallVisual";
            wall.transform.SetParent(go.transform, false);
            wall.transform.localScale = new Vector3(3.6f, 2.2f, 0.4f);
            wall.transform.localPosition = new Vector3(0, 1.1f, 0);

            var renderer = wall.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.4f, 0.85f, 1.0f, 0.85f);
            }

            var col = wall.GetComponent<Collider>();
            if (col != null) col.isTrigger = false;

            var comp = go.AddComponent<IceBarricadeComponent>();
            comp.Duration = duration;

            return go;
        }

        public sealed class IceBarricadeComponent : MonoBehaviour
        {
            public float Duration = 6.0f;
            private float _left;

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left <= 0.0f) Destroy(gameObject);
            }
        }

        // -------------------------------------------------------------------
        // ICE SHEET ZONE (Cheska Skill 1)
        // -------------------------------------------------------------------
        public static GameObject SpawnIceSheet(Vector3 position, float radius = 4.5f, float duration = 5.0f, int ownerSlot = -1)
        {
            var go = new GameObject("IceSheetZone");
            go.transform.position = position;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(radius * 2.0f, 0.02f, radius * 2.0f);
            visual.transform.localPosition = new Vector3(0, 0.01f, 0);

            var r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.5f, 0.9f, 1.0f, 0.5f);
            }

            Object.Destroy(visual.GetComponent<Collider>());

            var comp = go.AddComponent<IceSheetComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;

            return go;
        }

        public sealed class IceSheetComponent : MonoBehaviour
        {
            public float Radius = 4.5f;
            public float Duration = 5.0f;
            public int OwnerSlot = -1;
            private float _left;

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left <= 0.0f)
                {
                    Destroy(gameObject);
                    return;
                }

                var round = GameServices.Round;
                if (round == null) return;

                foreach (var p in round.Players)
                {
                    if (p == null) continue;
                    if (p.PlayerSlot == OwnerSlot) continue;

                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude <= Radius)
                    {
                        // Apply slip impulse in player's current moving direction
                        if (p.Velocity.sqrMagnitude > 0.1f)
                        {
                            Vector3 slip = p.Velocity.normalized * 3.5f * Time.deltaTime;
                            p.ApplyImpulse(slip);
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // SHOCK TRAIL ZONE (Zack Skill 1)
        // -------------------------------------------------------------------
        public static GameObject SpawnShockTrail(Vector3 position, float radius = 2.0f, float duration = 3.0f, int ownerSlot = -1)
        {
            var go = new GameObject("ShockTrailZone");
            go.transform.position = position;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(radius * 2.0f, 0.03f, radius * 2.0f);
            visual.transform.localPosition = new Vector3(0, 0.015f, 0);

            var r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(1.0f, 0.95f, 0.2f, 0.6f);
            }

            Object.Destroy(visual.GetComponent<Collider>());

            var comp = go.AddComponent<ShockTrailComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;

            return go;
        }

        public sealed class ShockTrailComponent : MonoBehaviour
        {
            public float Radius = 2.0f;
            public float Duration = 3.0f;
            public int OwnerSlot = -1;
            private float _left;

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left <= 0.0f)
                {
                    Destroy(gameObject);
                    return;
                }

                var round = GameServices.Round;
                if (round == null) return;

                foreach (var p in round.Players)
                {
                    if (p == null) continue;
                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude <= Radius)
                    {
                        if (p.PlayerSlot == OwnerSlot)
                        {
                            // Speed boost to owner
                            p.ApplyImpulse(p.transform.forward * 4.0f * Time.deltaTime);
                        }
                        else
                        {
                            // Stagger & slow opponents
                            p.ApplyStagger(0.2f);
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // GHOST POLTERGEIST PROJECTILE (Nemu Skill 2)
        // -------------------------------------------------------------------
        public static GameObject SpawnGhostPoltergeist(Vector3 position, Vector3 direction, int ownerSlot)
        {
            var go = new GameObject("GhostPoltergeist");
            go.transform.position = position + Vector3.up * 1.0f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "GhostOrb";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.6f;

            var r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.7f, 0.3f, 0.9f, 0.75f);
            }

            Object.Destroy(visual.GetComponent<Collider>());

            var comp = go.AddComponent<GhostPoltergeistComponent>();
            comp.Direction = direction.normalized;
            comp.OwnerSlot = ownerSlot;

            return go;
        }

        public sealed class GhostPoltergeistComponent : MonoBehaviour
        {
            public Vector3 Direction;
            public int OwnerSlot;
            private float _lifetime = 4.0f;
            private CharacterMotor _target;

            private void Update()
            {
                _lifetime -= Time.deltaTime;
                if (_lifetime <= 0.0f)
                {
                    Destroy(gameObject);
                    return;
                }

                if (_target == null)
                {
                    // Find nearest enemy to home towards
                    var round = GameServices.Round;
                    if (round != null)
                    {
                        float bestDist = 12.0f;
                        foreach (var p in round.Players)
                        {
                            if (p == null || p.PlayerSlot == OwnerSlot) continue;
                            float d = Vector3.Distance(transform.position, p.transform.position);
                            if (d < bestDist)
                            {
                                bestDist = d;
                                _target = p;
                            }
                        }
                    }

                    transform.position += Direction * 9.0f * Time.deltaTime;
                }
                else
                {
                    Vector3 targetPos = _target.transform.position + Vector3.up * 1.2f;
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, 11.0f * Time.deltaTime);

                    if (Vector3.Distance(transform.position, targetPos) < 0.8f)
                    {
                        // Haunt the target
                        _target.ApplyStagger(1.8f);
                        _target.ApplyImpulse(Random.onUnitSphere * 3.0f);
                        GameServices.Audio?.PlayAt("downed", transform.position);
                        Destroy(gameObject);
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // EARTH PILLAR (Dante Ultimate)
        // -------------------------------------------------------------------
        public static GameObject SpawnEarthPillar(Vector3 position, float duration = 6.0f)
        {
            var go = new GameObject("EarthPillar");
            go.transform.position = position;

            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "PillarVisual";
            pillar.transform.SetParent(go.transform, false);
            pillar.transform.localScale = new Vector3(1.2f, 2.0f, 1.2f);
            pillar.transform.localPosition = new Vector3(0, 2.0f, 0);

            var r = pillar.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.45f, 0.32f, 0.22f);
            }

            var comp = go.AddComponent<EarthPillarComponent>();
            comp.Duration = duration;

            return go;
        }

        public sealed class EarthPillarComponent : MonoBehaviour
        {
            public float Duration = 6.0f;
            private float _left;

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left <= 0.0f) Destroy(gameObject);
            }
        }

        // -------------------------------------------------------------------
        // SEANCE VOID ZONE (Nemu Ultimate)
        // -------------------------------------------------------------------
        public static GameObject SpawnSeanceVoid(Vector3 position, float radius = 7.5f, float duration = 5.0f, int ownerSlot = -1)
        {
            var go = new GameObject("SeanceVoidZone");
            go.transform.position = position;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(radius * 2.0f, 0.05f, radius * 2.0f);
            visual.transform.localPosition = new Vector3(0, 0.02f, 0);

            var r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.35f, 0.1f, 0.55f, 0.6f);
            }

            Object.Destroy(visual.GetComponent<Collider>());

            var comp = go.AddComponent<SeanceVoidComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;

            return go;
        }

        public sealed class SeanceVoidComponent : MonoBehaviour
        {
            public float Radius = 7.5f;
            public float Duration = 5.0f;
            public int OwnerSlot = -1;
            private float _left;

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left <= 0.0f)
                {
                    Destroy(gameObject);
                    return;
                }

                var round = GameServices.Round;
                if (round == null) return;

                // Slow enemy players and pull dropped slippers inward
                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == OwnerSlot) continue;

                    Vector3 diff = transform.position - p.transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude <= Radius)
                    {
                        p.ApplyStagger(0.2f);
                        p.ApplyImpulse(diff.normalized * 2.5f * Time.deltaTime);
                    }
                }

                // Pull dropped slippers towards void center
                foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (s != null && s.State != SlipperState.Held)
                    {
                        Vector3 sDiff = transform.position - s.transform.position;
                        sDiff.y = 0.0f;
                        if (sDiff.magnitude <= Radius && sDiff.magnitude > 0.5f)
                        {
                            s.transform.position += sDiff.normalized * 4.0f * Time.deltaTime;
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // EXPLOSION EFFECT (Sean Skill 2 & Ultimate)
        // -------------------------------------------------------------------
        public static void CreateExplosion(Vector3 center, float radius, float knockback, float stunTime, int sourceSlot)
        {
            var round = GameServices.Round;
            if (round == null) return;

            // Visual explosion sphere that expands and fades
            var vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vfx.name = "ExplosionVfx";
            vfx.transform.position = center + Vector3.up * 0.5f;
            vfx.transform.localScale = Vector3.one * (radius * 0.8f);

            var r = vfx.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1.0f, 0.45f, 0.1f, 0.8f);
            Object.Destroy(vfx.GetComponent<Collider>());
            Object.Destroy(vfx, 0.4f);

            GameServices.Audio?.PlayAt("lata_impact", center);

            // Damage/Knockback players
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                Vector3 to = p.transform.position - center;
                to.y = 0.0f;
                float d = to.magnitude;

                if (d <= radius)
                {
                    float force = Mathf.Lerp(knockback, knockback * 0.3f, d / radius);
                    Vector3 push = (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward) * force;
                    push.y = 4.5f;

                    p.ApplyImpulse(push);
                    if (p.PlayerSlot != sourceSlot && stunTime > 0.0f)
                    {
                        p.ApplyStagger(stunTime);
                    }
                }
            }

            // Also launch can if within explosion
            if (round.Lata != null)
            {
                Vector3 canDiff = round.Lata.transform.position - center;
                canDiff.y = 0.0f;
                if (canDiff.magnitude <= radius)
                {
                    round.Lata.HostKnockDown(sourceSlot);
                }
            }
        }
    }
}
