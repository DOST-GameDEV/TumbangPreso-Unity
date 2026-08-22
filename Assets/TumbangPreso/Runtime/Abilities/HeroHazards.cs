using System.Collections.Generic;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Spawns and manages grand hazard entities, barriers, zones, and visual effects for hero abilities.
    /// Features dynamic lighting, animated procedural geometry, ground shockwaves, and particle chunks.
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

            // Create grand multi-pillar glacial wall (5 jagged ice crystals in an arc)
            for (int i = -2; i <= 2; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"IcePillar_{i}";
                pillar.transform.SetParent(go.transform, false);

                float height = (3.0f - Mathf.Abs(i) * 0.4f) * Random.Range(0.9f, 1.1f);
                float width = 0.85f;
                float rotY = i * 8.0f + Random.Range(-5.0f, 5.0f);
                float rotZ = i * -4.0f;

                pillar.transform.localScale = new Vector3(width, height, 0.55f);
                pillar.transform.localPosition = new Vector3(i * 0.75f, height * 0.5f, -Mathf.Abs(i) * 0.15f);
                pillar.transform.localRotation = Quaternion.Euler(Random.Range(-5.0f, 5.0f), rotY, rotZ);

                var r = pillar.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(0.45f, 0.88f, 1.0f, 0.9f);
                }

                var col = pillar.GetComponent<Collider>();
                if (col != null) col.isTrigger = false;
            }

            // Cyan frost glow light
            var lightGo = new GameObject("IceLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.5f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.3f, 0.85f, 1.0f);
            light.range = 6.0f;
            light.intensity = 2.5f;

            GameServices.Audio?.PlayAt("ability_shatter_trap", position);

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
                if (_left <= 0.0f)
                {
                    Shatter();
                }
            }

            private void Shatter()
            {
                // Spawn ice explosion shards on break
                for (int i = 0; i < 8; i++)
                {
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = "IceShard";
                    shard.transform.position = transform.position + Vector3.up * Random.Range(0.5f, 1.8f) + Random.insideUnitSphere * 0.8f;
                    shard.transform.localScale = Vector3.one * Random.Range(0.2f, 0.45f);
                    shard.transform.rotation = Random.rotation;

                    var r = shard.GetComponent<Renderer>();
                    if (r != null) r.material.color = new Color(0.6f, 0.9f, 1.0f, 0.8f);

                    var rb = shard.AddComponent<Rigidbody>();
                    rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.2f) * Random.Range(3.0f, 7.0f);
                    rb.angularVelocity = Random.insideUnitSphere * 15.0f;

                    Object.Destroy(shard, 1.2f);
                }

                GameServices.Audio?.PlayAt("slipper_land", transform.position);
                Object.Destroy(gameObject);
            }
        }

        // -------------------------------------------------------------------
        // ICE SHEET ZONE (Cheska Skill 1)
        // -------------------------------------------------------------------
        public static GameObject SpawnIceSheet(Vector3 position, float radius = 4.5f, float duration = 5.0f, int ownerSlot = -1)
        {
            var go = new GameObject("IceSheetZone");
            go.transform.position = position;

            // Grand multi-disc frosted surface
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "VisualOuter";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(radius * 2.0f, 0.02f, radius * 2.0f);
            visual.transform.localPosition = new Vector3(0, 0.01f, 0);

            var r = visual.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.4f, 0.85f, 1.0f, 0.6f);
            Object.Destroy(visual.GetComponent<Collider>());

            var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inner.name = "VisualInner";
            inner.transform.SetParent(go.transform, false);
            inner.transform.localScale = new Vector3(radius * 1.2f, 0.025f, radius * 1.2f);
            inner.transform.localPosition = new Vector3(0, 0.015f, 0);

            var ir = inner.GetComponent<Renderer>();
            if (ir != null) ir.material.color = new Color(0.85f, 0.96f, 1.0f, 0.75f);
            Object.Destroy(inner.GetComponent<Collider>());

            // Glowing ice aura light
            var lightGo = new GameObject("FrostLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.6f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.4f, 0.85f, 1.0f);
            light.range = radius * 1.5f;
            light.intensity = 2.0f;

            GameServices.Audio?.PlayAt("ability_shatter_trap", position);

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
                    Object.Destroy(gameObject);
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
                        // Apply friction loss & uncontrollable slip in velocity direction
                        if (p.Velocity.sqrMagnitude > 0.1f)
                        {
                            Vector3 slip = p.Velocity.normalized * 4.5f * Time.deltaTime;
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
            visual.name = "ShockVisual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(radius * 2.0f, 0.03f, radius * 2.0f);
            visual.transform.localPosition = new Vector3(0, 0.015f, 0);

            var r = visual.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1.0f, 0.92f, 0.2f, 0.7f);
            Object.Destroy(visual.GetComponent<Collider>());

            // Flashing electric sparks light
            var lightGo = new GameObject("ShockLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.5f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.95f, 0.3f);
            light.range = radius * 2.0f;
            light.intensity = 3.0f;

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
                    Object.Destroy(gameObject);
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
                            // Electric speed rail boost to owner
                            p.ApplyImpulse(p.transform.forward * 5.0f * Time.deltaTime);
                        }
                        else
                        {
                            // Stagger & electrify opponents
                            p.ApplyStagger(0.25f);
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // FIRE NAPALM TRAIL (Sean Skill 1)
        // -------------------------------------------------------------------
        public static GameObject SpawnFireTrail(Vector3 position, float radius = 1.8f, float duration = 3.0f, int ownerSlot = -1)
        {
            var go = new GameObject("FireTrailZone");
            go.transform.position = position;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "FireVisual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(radius * 2.0f, 0.03f, radius * 2.0f);
            visual.transform.localPosition = new Vector3(0, 0.015f, 0);

            var r = visual.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1.0f, 0.4f, 0.05f, 0.8f);
            Object.Destroy(visual.GetComponent<Collider>());

            // Flickering fire light
            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.6f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.5f, 0.1f);
            light.range = radius * 2.2f;
            light.intensity = 3.2f;

            var comp = go.AddComponent<FireTrailComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;

            return go;
        }

        public sealed class FireTrailComponent : MonoBehaviour
        {
            public float Radius = 1.8f;
            public float Duration = 3.0f;
            public int OwnerSlot = -1;
            private float _left;

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left <= 0.0f)
                {
                    Object.Destroy(gameObject);
                    return;
                }

                var round = GameServices.Round;
                if (round == null) return;

                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == OwnerSlot) continue;
                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude <= Radius)
                    {
                        p.ApplyStagger(0.2f);
                        p.ApplyImpulse(diff.normalized * 3.0f * Time.deltaTime);
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // GHOST POLTERGEIST PROJECTILE (Nemu Skill 2 Autonomous Option)
        // -------------------------------------------------------------------
        public static GameObject SpawnGhostPoltergeist(Vector3 position, Vector3 direction, int ownerSlot)
        {
            var go = new GameObject("GhostPoltergeist");
            go.transform.position = position + Vector3.up * 1.0f;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "GhostOrb";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = Vector3.one * 0.75f;

            var r = visual.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.75f, 0.35f, 0.95f, 0.85f);
            Object.Destroy(visual.GetComponent<Collider>());

            var lightGo = new GameObject("GhostLight");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.8f, 0.4f, 1.0f);
            light.range = 5.0f;
            light.intensity = 2.5f;

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
                    Object.Destroy(gameObject);
                    return;
                }

                if (_target == null)
                {
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

                    transform.position += Direction * 9.5f * Time.deltaTime;
                }
                else
                {
                    Vector3 targetPos = _target.transform.position + Vector3.up * 1.2f;
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, 11.5f * Time.deltaTime);

                    if (Vector3.Distance(transform.position, targetPos) < 0.9f)
                    {
                        _target.ApplyStagger(1.8f);
                        _target.ApplyImpulse(Random.onUnitSphere * 3.5f);
                        GameServices.Audio?.PlayAt("downed", transform.position);
                        Object.Destroy(gameObject);
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

            // Grand volcanic basalt pillar with magma crest
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "PillarVisual";
            pillar.transform.SetParent(go.transform, false);
            pillar.transform.localScale = new Vector3(1.35f, 2.4f, 1.35f);
            pillar.transform.localPosition = new Vector3(0, 2.4f, 0);

            var r = pillar.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.3f, 0.22f, 0.18f);

            var magmaTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            magmaTop.name = "MagmaTop";
            magmaTop.transform.SetParent(go.transform, false);
            magmaTop.transform.localScale = new Vector3(1.4f, 0.8f, 1.4f);
            magmaTop.transform.localPosition = new Vector3(0, 4.6f, 0);
            var mr = magmaTop.GetComponent<Renderer>();
            if (mr != null) mr.material.color = new Color(1.0f, 0.35f, 0.05f);
            Object.Destroy(magmaTop.GetComponent<Collider>());

            var lightGo = new GameObject("MagmaLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 4.0f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.4f, 0.1f);
            light.range = 6.5f;
            light.intensity = 3.0f;

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
                if (_left <= 0.0f) Object.Destroy(gameObject);
            }
        }

        // -------------------------------------------------------------------
        // SEANCE VOID ZONE (Nemu Ultimate)
        // -------------------------------------------------------------------
        public static GameObject SpawnSeanceVoid(Vector3 position, float radius = 7.5f, float duration = 5.0f, int ownerSlot = -1)
        {
            var go = new GameObject("SeanceVoidZone");
            go.transform.position = position;

            // Double cosmic accretion vortex discs
            var outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outer.name = "VortexOuter";
            outer.transform.SetParent(go.transform, false);
            outer.transform.localScale = new Vector3(radius * 2.0f, 0.04f, radius * 2.0f);
            outer.transform.localPosition = new Vector3(0, 0.02f, 0);
            var r = outer.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.25f, 0.05f, 0.45f, 0.75f);
            Object.Destroy(outer.GetComponent<Collider>());

            var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inner.name = "VortexInner";
            inner.transform.SetParent(go.transform, false);
            inner.transform.localScale = new Vector3(radius * 1.1f, 0.05f, radius * 1.1f);
            inner.transform.localPosition = new Vector3(0, 0.03f, 0);
            var ir = inner.GetComponent<Renderer>();
            if (ir != null) ir.material.color = new Color(0.65f, 0.2f, 0.9f, 0.85f);
            Object.Destroy(inner.GetComponent<Collider>());

            // Pulsing violet gravity light
            var lightGo = new GameObject("VoidLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.2f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.7f, 0.2f, 1.0f);
            light.range = radius * 1.6f;
            light.intensity = 4.0f;

            GameServices.Audio?.PlayAt("ability_bagsak_bomb", position);

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
                    Object.Destroy(gameObject);
                    return;
                }

                // Rotate cosmic vortex discs
                transform.Rotate(Vector3.up, 60.0f * Time.deltaTime);

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
                        p.ApplyImpulse(diff.normalized * 3.5f * Time.deltaTime);
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
                            s.transform.position += sDiff.normalized * 5.0f * Time.deltaTime;
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // GRAND EXPLOSION EFFECT (Sean Skill 2 & Ultimate, Dante Stomp)
        // -------------------------------------------------------------------
        public static void CreateExplosion(Vector3 center, float radius, float knockback, float stunTime, int sourceSlot)
        {
            var round = GameServices.Round;
            if (round == null) return;

            // 1. Expanding fireball core sphere
            var vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vfx.name = "ExplosionCore";
            vfx.transform.position = center + Vector3.up * 0.6f;
            vfx.transform.localScale = Vector3.one * (radius * 0.4f);

            var r = vfx.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(1.0f, 0.55f, 0.1f, 0.9f);
            Object.Destroy(vfx.GetComponent<Collider>());

            var anim = vfx.AddComponent<ExplosionVfxAnim>();
            anim.TargetRadius = radius * 1.1f;
            Object.Destroy(vfx, 0.5f);

            // 2. Expanding ground shockwave ring
            var shockRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shockRing.name = "ShockwaveRing";
            shockRing.transform.position = center + Vector3.up * 0.05f;
            shockRing.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);

            var sr = shockRing.GetComponent<Renderer>();
            if (sr != null) sr.material.color = new Color(1.0f, 0.85f, 0.3f, 0.8f);
            Object.Destroy(shockRing.GetComponent<Collider>());

            var ringAnim = shockRing.AddComponent<ShockwaveRingAnim>();
            ringAnim.TargetRadius = radius * 1.4f;
            Object.Destroy(shockRing, 0.4f);

            // 3. Flying fiery debris sparks
            for (int i = 0; i < 8; i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = "ExplosionSpark";
                spark.transform.position = center + Vector3.up * 0.5f;
                spark.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);

                var spr = spark.GetComponent<Renderer>();
                if (spr != null) spr.material.color = new Color(1.0f, Random.Range(0.4f, 0.8f), 0.1f);
                Object.Destroy(spark.GetComponent<Collider>());

                var rb = spark.AddComponent<Rigidbody>();
                rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.5f) * Random.Range(6.0f, 14.0f);
                Object.Destroy(spark, 0.6f);
            }

            // 4. Bright flash light
            var lightGo = new GameObject("ExplosionLight");
            lightGo.transform.position = center + Vector3.up * 1.0f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.65f, 0.2f);
            light.range = radius * 2.5f;
            light.intensity = 5.0f;
            Object.Destroy(lightGo, 0.35f);

            GameServices.Audio?.PlayAt("ability_bagsak_bomb", center);

            // Damage / Knockback players
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                Vector3 to = p.transform.position - center;
                to.y = 0.0f;
                float d = to.magnitude;

                if (d <= radius)
                {
                    float force = Mathf.Lerp(knockback, knockback * 0.35f, d / radius);
                    Vector3 push = (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward) * force;
                    push.y = 5.0f;

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

        private sealed class ExplosionVfxAnim : MonoBehaviour
        {
            public float TargetRadius = 5.0f;
            private float _elapsed;

            private void Update()
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / 0.5f);
                transform.localScale = Vector3.one * Mathf.Lerp(1.0f, TargetRadius * 2.0f, Mathf.Sqrt(t));

                var r = GetComponent<Renderer>();
                if (r != null)
                {
                    var col = r.material.color;
                    col.a = Mathf.Lerp(0.9f, 0.0f, t);
                    r.material.color = col;
                }
            }
        }

        private sealed class ShockwaveRingAnim : MonoBehaviour
        {
            public float TargetRadius = 6.0f;
            private float _elapsed;

            private void Update()
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / 0.4f);
                float r = Mathf.Lerp(0.5f, TargetRadius * 2.0f, t);
                transform.localScale = new Vector3(r, 0.02f, r);

                var rend = GetComponent<Renderer>();
                if (rend != null)
                {
                    var col = rend.material.color;
                    col.a = Mathf.Lerp(0.8f, 0.0f, t);
                    rend.material.color = col;
                }
            }
        }
    }
}
