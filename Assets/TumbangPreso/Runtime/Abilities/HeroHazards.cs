using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// Spawns and manages grand hazard entities, barriers, zones, and visual effects for hero abilities.
    /// Features dynamic lighting, animated procedural geometry, ground shockwaves, comic-style floaties,
    /// and kid-friendly cartoon particle bursts.
    /// </summary>
    public static class HeroHazards
    {
        private static bool CanPulse(Dictionary<int, float> nextPulseBySlot, int slot, float interval)
        {
            if (nextPulseBySlot.TryGetValue(slot, out float next) && Time.time < next)
                return false;

            nextPulseBySlot[slot] = Time.time + interval;
            return true;
        }

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

                float height = (3.2f - Mathf.Abs(i) * 0.45f) * Random.Range(0.95f, 1.15f);
                float width = 0.9f;
                float rotY = i * 8.0f + Random.Range(-5.0f, 5.0f);
                float rotZ = i * -4.0f;

                pillar.transform.localScale = new Vector3(width, height, 0.6f);
                pillar.transform.localPosition = new Vector3(i * 0.8f, height * 0.5f, -Mathf.Abs(i) * 0.15f);
                pillar.transform.localRotation = Quaternion.Euler(Random.Range(-5.0f, 5.0f), rotY, rotZ);

                var r = pillar.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = new Color(0.35f, 0.90f, 1.0f, 0.92f);
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
            light.color = UiTheme.HeroIceBright;
            light.range = 6.5f;
            light.intensity = 3.0f;

            GameServices.Audio?.PlayAt("ability_shatter_trap", position);
            ComicPopup.Spawn(position, "ICE WALL!", UiTheme.HeroIceBright, 1.2f);

            var comp = go.AddComponent<IceBarricadeComponent>();
            comp.Duration = duration;

            // ⚠️ REGISTERED WITH `HazardMap` SO THE BOTS PATH AROUND IT. Without this an
            // attacker walks straight through on its way to a tsinelas, gets caught, and the
            // round charges it the unretrieved-slipper penalty for a fetch it was making.
            // A wall, approximated as a disc covering its own half width. Good enough for
            // steering: the point is not to walk into it.
            HazardVolume.Attach(go, 3.0f, -1);

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

            public void Shatter()
            {
                // Spawn 12 cartoon bouncy ice explosion cubes on break
                for (int i = 0; i < 12; i++)
                {
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = "IceShard";
                    shard.transform.position = transform.position + Vector3.up * Random.Range(0.4f, 2.0f) + Random.insideUnitSphere * 0.9f;
                    shard.transform.localScale = Vector3.one * Random.Range(0.25f, 0.5f);
                    shard.transform.rotation = Random.rotation;

                    var r = shard.GetComponent<Renderer>();
                    if (r != null) r.material.color = new Color(0.6f, 0.95f, 1.0f, 0.85f);

                    var rb = shard.AddComponent<Rigidbody>();
                    rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.4f) * Random.Range(4.0f, 9.0f);
                    rb.angularVelocity = Random.insideUnitSphere * 20.0f;

                    Object.Destroy(shard, 1.2f);
                }

                ComicPopup.Freeze(transform.position);
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

            // Grand multi-disc frosted surface with rotating snowflake ornaments
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "VisualOuter";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = new Vector3(radius * 2.0f, 0.02f, radius * 2.0f);
            visual.transform.localPosition = new Vector3(0, 0.01f, 0);

            var r = visual.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.3f, 0.85f, 1.0f, 0.65f);
            var collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Object.Destroy(collider);
                else Object.DestroyImmediate(collider);
            }

            var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inner.name = "VisualInner";
            inner.transform.SetParent(go.transform, false);
            inner.transform.localScale = new Vector3(radius * 1.3f, 0.025f, radius * 1.3f);
            inner.transform.localPosition = new Vector3(0, 0.015f, 0);

            var ir = inner.GetComponent<Renderer>();
            if (ir != null) ir.material.color = new Color(0.85f, 0.96f, 1.0f, 0.80f);
            Object.Destroy(inner.GetComponent<Collider>());

            // Glowing ice aura light
            var lightGo = new GameObject("FrostLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.6f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroIceBright;
            light.range = radius * 1.6f;
            light.intensity = 2.5f;

            GameServices.Audio?.PlayAt("ability_shatter_trap", position);
            ComicPopup.Spawn(position, "SLIP & SLIDE!", UiTheme.HeroIceBright, 1.15f);

            var comp = go.AddComponent<IceSheetComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;

            // ⚠️ REGISTERED WITH `HazardMap` SO THE BOTS PATH AROUND IT. Without this an
            // attacker walks straight through on its way to a tsinelas, gets caught, and the
            // round charges it the unretrieved-slipper penalty for a fetch it was making.
            HazardVolume.Attach(go, radius, ownerSlot);

            return go;
        }

        public sealed class IceSheetComponent : MonoBehaviour
        {
            public float Radius = 4.5f;
            public float Duration = 5.0f;
            public int OwnerSlot = -1;
            private float _left;
            private float _whoaCooldown;

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                _whoaCooldown -= Time.deltaTime;

                if (_left <= 0.0f)
                {
                    Object.Destroy(gameObject);
                    return;
                }

                // Slow rotation on the ice zone
                transform.Rotate(Vector3.up, 20.0f * Time.deltaTime);

                var round = GameServices.Round;
                if (round == null) return;

                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == OwnerSlot) continue;

                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude <= Radius)
                    {
                        // Apply friction loss & cartoon uncontrollable slip in velocity direction
                        if (p.Velocity.sqrMagnitude > 0.1f)
                        {
                            Vector3 slip = p.Velocity.normalized * 5.5f * Time.deltaTime;
                            p.ApplyImpulse(slip);

                            if (_whoaCooldown <= 0.0f)
                            {
                                _whoaCooldown = 1.2f;
                                ComicPopup.Whoa(p.transform.position);
                                GameServices.Audio?.PlayAt("ability_shatter_trap", p.transform.position);
                            }
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
            if (r != null) r.material.color = new Color(1.0f, 0.95f, 0.1f, 0.75f);
            Object.Destroy(visual.GetComponent<Collider>());

            // Flashing electric sparks light
            var lightGo = new GameObject("ShockLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.5f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroElectricBright;
            light.range = radius * 2.2f;
            light.intensity = 3.5f;

            // ⚠⚠ A TRAIL IS DELIBERATELY *NOT* REGISTERED WITH `HazardMap`, AND THAT IS A
            // MEASUREMENT, NOT AN OVERSIGHT. `OnTick` drops one of these every 0.10 s for the
            // whole dash and each lives 3 s, so a single dashing hero leaves up to THIRTY
            // live discs and three of them fill a 14 by 14 box with a minefield. Registering
            // them took `BotBehaviourProbe`'s Hero Strike run from 59 throws, 122 skill uses
            // and 58 idle penalties down to **11 throws, 3 skill uses and 661 idle
            // penalties**: every bot was surrounded by obstacles it was trying to respect and
            // simply stopped playing. Trails are breadcrumbs to be run through, not terrain
            // to be walked around.

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
            private readonly Dictionary<int, float> _nextStaggerBySlot = new Dictionary<int, float>();

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
                            // Turbo speed boost to Zack
                            p.ApplyImpulse(p.transform.forward * 6.0f * Time.deltaTime);
                        }
                        else
                        {
                            // Discrete pulses keep the trail threatening without turning
                            // every rendered frame into a permanent action lock.
                            if (CanPulse(_nextStaggerBySlot, p.PlayerSlot, 1.1f))
                            {
                                p.ApplyStagger(0.25f);
                                ComicPopup.Zap(p.transform.position);
                                DizzyStars.Attach(p.transform, 1.2f, UiTheme.HeroElectricBright);
                            }
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
            if (r != null) r.material.color = new Color(1.0f, 0.35f, 0.05f, 0.85f);
            Object.Destroy(visual.GetComponent<Collider>());

            // Flickering fire light
            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.6f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroFireBright;
            light.range = radius * 2.4f;
            light.intensity = 3.5f;

            // ⚠⚠ A TRAIL IS DELIBERATELY *NOT* REGISTERED WITH `HazardMap`, AND THAT IS A
            // MEASUREMENT, NOT AN OVERSIGHT. `OnTick` drops one of these every 0.10 s for the
            // whole dash and each lives 3 s, so a single dashing hero leaves up to THIRTY
            // live discs and three of them fill a 14 by 14 box with a minefield. Registering
            // them took `BotBehaviourProbe`'s Hero Strike run from 59 throws, 122 skill uses
            // and 58 idle penalties down to **11 throws, 3 skill uses and 661 idle
            // penalties**: every bot was surrounded by obstacles it was trying to respect and
            // simply stopped playing. Trails are breadcrumbs to be run through, not terrain
            // to be walked around.

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
            private readonly Dictionary<int, float> _nextBurnBySlot = new Dictionary<int, float>();

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
                        p.ApplyImpulse(diff.normalized * 3.5f * Time.deltaTime);

                        if (CanPulse(_nextBurnBySlot, p.PlayerSlot, 0.85f))
                        {
                            p.ApplyStagger(0.2f);
                            ComicPopup.Bam(p.transform.position);
                        }
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
            visual.transform.localScale = Vector3.one * 0.8f;

            var r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                var block = new MaterialPropertyBlock();
                r.GetPropertyBlock(block);
                var ghostColor = new Color(0.85f, 0.4f, 1.0f, 0.9f);
                block.SetColor("_Color", ghostColor);
                block.SetColor("_BaseColor", ghostColor);
                r.SetPropertyBlock(block);
            }
            var ghostCollider = visual.GetComponent<Collider>();
            if (ghostCollider != null)
            {
                if (Application.isPlaying) Object.Destroy(ghostCollider);
                else Object.DestroyImmediate(ghostCollider);
            }

            var lightGo = new GameObject("GhostLight");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroSpiritBright;
            light.range = 5.5f;
            light.intensity = 3.0f;

            ComicPopup.Boo(position);

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

                    transform.position += Direction * 10.0f * Time.deltaTime;

                    // ⚠️ THE GHOST STAYS IN THE ARENA BECAUSE NEMU FOLLOWS IT. PHANTOM PHASE
                    // ends by teleporting the caster onto this object, so a ghost that flies
                    // out over the edge at 10 m/s is a hero blinking out of the world. The
                    // teleport itself is clamped as a last line of defence, but clamping the
                    // ghost is what keeps the destination somewhere worth blinking to.
                    Vector3 flown = transform.position;
                    flown.x = Mathf.Clamp(flown.x, -AIController.PlayableHalfX, AIController.PlayableHalfX);
                    flown.z = Mathf.Clamp(flown.z, -AIController.PlayableHalfZ, AIController.PlayableHalfZ);
                    transform.position = flown;
                }
                else
                {
                    Vector3 targetPos = _target.transform.position + Vector3.up * 1.2f;
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, 12.0f * Time.deltaTime);

                    if (Vector3.Distance(transform.position, targetPos) < 0.9f)
                    {
                        _target.ApplyStagger(1.8f);
                        _target.ApplyImpulse(Random.onUnitSphere * 4.0f);
                        DizzyStars.Attach(_target.transform, 1.8f, UiTheme.HeroSpiritBright);
                        ComicPopup.Boo(_target.transform.position);
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

            // Grand volcanic basalt pillar with molten magma crest
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "PillarVisual";
            pillar.transform.SetParent(go.transform, false);
            pillar.transform.localScale = new Vector3(1.4f, 2.5f, 1.4f);
            pillar.transform.localPosition = new Vector3(0, 2.5f, 0);

            var r = pillar.GetComponent<Renderer>();
            if (r != null) r.material.color = new Color(0.28f, 0.20f, 0.16f);

            var magmaTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            magmaTop.name = "MagmaTop";
            magmaTop.transform.SetParent(go.transform, false);
            magmaTop.transform.localScale = new Vector3(1.45f, 0.85f, 1.45f);
            magmaTop.transform.localPosition = new Vector3(0, 4.8f, 0);
            var mr = magmaTop.GetComponent<Renderer>();
            if (mr != null) mr.material.color = UiTheme.HeroEarthBright;
            Object.Destroy(magmaTop.GetComponent<Collider>());

            var lightGo = new GameObject("MagmaLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 4.2f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroEarthBright;
            light.range = 7.0f;
            light.intensity = 3.5f;

            var comp = go.AddComponent<EarthPillarComponent>();
            comp.Duration = duration;

            // A solid pillar. Nobody owns it, so everybody steers round it.
            HazardVolume.Attach(go, 1.4f, -1);

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
        // CRACKED LAVA DECAL (Dante Skill 1 Seismic Stomp)
        // -------------------------------------------------------------------
        public static GameObject SpawnCrackedLavaDecal(Vector3 position, float radius = 5.5f, float duration = 4.0f)
        {
            var go = new GameObject("CrackedLavaDecal");
            go.transform.position = position;

            // Outer asphalt crack ring
            var outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outer.name = "CrackedAsphaltRing";
            outer.transform.SetParent(go.transform, false);
            outer.transform.localScale = new Vector3(radius * 2.0f, 0.02f, radius * 2.0f);
            outer.transform.localPosition = new Vector3(0, 0.015f, 0);
            var or = outer.GetComponent<Renderer>();
            if (or != null) or.material.color = new Color(0.18f, 0.16f, 0.15f, 0.85f);
            Object.Destroy(outer.GetComponent<Collider>());

            // Glowing magma core disc
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "MagmaCore";
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = new Vector3(radius * 1.2f, 0.025f, radius * 1.2f);
            core.transform.localPosition = new Vector3(0, 0.02f, 0);
            var cr = core.GetComponent<Renderer>();
            if (cr != null) cr.material.color = UiTheme.HeroEarthBright;
            Object.Destroy(core.GetComponent<Collider>());

            // Lava pulse light
            var lightGo = new GameObject("LavaLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 0.8f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroEarthBright;
            light.range = radius * 1.8f;
            light.intensity = 4.0f;

            Object.Destroy(go, duration);
            return go;
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
            if (r != null) r.material.color = new Color(0.35f, 0.05f, 0.55f, 0.85f);
            Object.Destroy(outer.GetComponent<Collider>());

            var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inner.name = "VortexInner";
            inner.transform.SetParent(go.transform, false);
            inner.transform.localScale = new Vector3(radius * 1.2f, 0.05f, radius * 1.2f);
            inner.transform.localPosition = new Vector3(0, 0.03f, 0);
            var ir = inner.GetComponent<Renderer>();
            if (ir != null) ir.material.color = UiTheme.HeroSpiritBright;
            Object.Destroy(inner.GetComponent<Collider>());

            // Pulsing violet gravity light
            var lightGo = new GameObject("VoidLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.2f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroSpiritBright;
            light.range = radius * 1.8f;
            light.intensity = 4.5f;

            GameServices.Audio?.PlayAt("ability_bagsak_bomb", position);
            ComicPopup.Spawn(position, "VOID GALAXY!", UiTheme.HeroSpiritBright, 1.4f);

            HazardVolume.Attach(go, radius, ownerSlot);

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
            private readonly Dictionary<int, float> _nextDrowseBySlot = new Dictionary<int, float>();

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
                transform.Rotate(Vector3.up, 75.0f * Time.deltaTime);

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
                        p.ApplyImpulse(diff.normalized * 4.0f * Time.deltaTime);
                        if (CanPulse(_nextDrowseBySlot, p.PlayerSlot, 1.25f))
                            p.ApplyStagger(0.35f);
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
                            s.transform.position += sDiff.normalized * 5.5f * Time.deltaTime;
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // THUNDERSTRIKE OVERDRIVE (Zack Ultimate)
        // -------------------------------------------------------------------
        public static void CreateThunderstrike(Vector3 position, float radius = 7.0f, int sourceSlot = -1)
        {
            // 1. Sky Lightning Bolt Column & Multi-segment Arc
            SpawnLightningBolt(position + Vector3.up * 24.0f, position, UiTheme.HeroElectricBright, 0.40f);

            // 2. Flying electric spark shards
            for (int i = 0; i < 10; i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = "ThunderSpark";
                spark.transform.position = position + Vector3.up * 0.5f;
                spark.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);

                var spr = spark.GetComponent<Renderer>();
                if (spr != null) spr.material.color = UiTheme.HeroElectricBright;
                Object.Destroy(spark.GetComponent<Collider>());

                var rb = spark.AddComponent<Rigidbody>();
                rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.5f) * Random.Range(5.0f, 12.0f);
                Object.Destroy(spark, 0.5f);
            }

            // 3. Expanding Electric Ground Shockwave
            var shockRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shockRing.name = "ThunderShockRing";
            shockRing.transform.position = position + Vector3.up * 0.04f;
            shockRing.transform.localScale = new Vector3(0.5f, 0.03f, 0.5f);
            var sr = shockRing.GetComponent<Renderer>();
            if (sr != null) sr.material.color = UiTheme.HeroElectric;
            Object.Destroy(shockRing.GetComponent<Collider>());

            var ringAnim = shockRing.AddComponent<ShockwaveRingAnim>();
            ringAnim.TargetRadius = radius * 1.5f;
            Object.Destroy(shockRing, 0.45f);

            // 3. Bright flash light
            var lightGo = new GameObject("ThunderLight");
            lightGo.transform.position = position + Vector3.up * 2.0f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroElectricBright;
            light.range = radius * 2.5f;
            light.intensity = 6.0f;
            Object.Destroy(lightGo, 0.35f);

            GameServices.Audio?.PlayAt("ability_flick_dash", position);
            ComicPopup.Zap(position);

            // Camera shake on main camera
            if (UnityEngine.Camera.main != null)
            {
                var rig = UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>();
                if (rig != null) rig.Shake(0.6f, 0.3f);
            }

            // Stagger and knock back enemies
            var round = GameServices.Round;
            if (round != null)
            {
                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == sourceSlot) continue;
                    Vector3 diff = p.transform.position - position;
                    diff.y = 0.0f;
                    if (diff.magnitude <= radius)
                    {
                        p.ApplyStagger(2.0f);
                        p.ApplyImpulse((diff.sqrMagnitude > 0.01f ? diff.normalized : Vector3.forward) * 12.0f + Vector3.up * 3.5f);
                        DizzyStars.Attach(p.transform, 2.0f, UiTheme.HeroElectricBright);
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // GRAND EXPLOSION EFFECT (Sean Skill 2 & Ultimate, Dante Stomp)
        // -------------------------------------------------------------------
        public static void CreateExplosion(Vector3 center, float radius, float knockback, float stunTime,
            int sourceSlot, string comicText = "KABOOM!", ISet<int> excludedSlots = null)
        {
            var round = GameServices.Round;
            if (round == null) return;

            // 1. Expanding fireball core sphere
            var vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            vfx.name = "ExplosionCore";
            vfx.transform.position = center + Vector3.up * 0.6f;
            vfx.transform.localScale = Vector3.one * (radius * 0.4f);

            var r = vfx.GetComponent<Renderer>();
            if (r != null) r.material.color = UiTheme.HeroFireBright;
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
            if (sr != null) sr.material.color = UiTheme.HeroFire;
            Object.Destroy(shockRing.GetComponent<Collider>());

            var ringAnim = shockRing.AddComponent<ShockwaveRingAnim>();
            ringAnim.TargetRadius = radius * 1.4f;
            Object.Destroy(shockRing, 0.4f);

            // 3. Flying fiery debris sparks
            for (int i = 0; i < 10; i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = "ExplosionSpark";
                spark.transform.position = center + Vector3.up * 0.5f;
                spark.transform.localScale = Vector3.one * Random.Range(0.18f, 0.4f);

                var spr = spark.GetComponent<Renderer>();
                if (spr != null) spr.material.color = new Color(1.0f, Random.Range(0.4f, 0.9f), 0.1f);
                Object.Destroy(spark.GetComponent<Collider>());

                var rb = spark.AddComponent<Rigidbody>();
                rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.6f) * Random.Range(7.0f, 15.0f);
                Object.Destroy(spark, 0.65f);
            }

            // 4. Bright flash light
            var lightGo = new GameObject("ExplosionLight");
            lightGo.transform.position = center + Vector3.up * 1.0f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroFireBright;
            light.range = radius * 2.6f;
            light.intensity = 5.5f;
            Object.Destroy(lightGo, 0.35f);

            GameServices.Audio?.PlayAt("ability_bagsak_bomb", center);

            // Comic Popup
            if (!string.IsNullOrEmpty(comicText))
            {
                ComicPopup.Spawn(center, comicText, UiTheme.HeroFireBright, 1.4f);
            }

            // Camera Shake on local rig
            if (UnityEngine.Camera.main != null)
            {
                var rig = UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>();
                if (rig != null) rig.Shake(0.55f, 0.28f);
            }

            // Damage / Knockback players
            foreach (var p in round.Players)
            {
                if (p == null) continue;
                if (excludedSlots != null && excludedSlots.Contains(p.PlayerSlot)) continue;
                Vector3 to = p.transform.position - center;
                to.y = 0.0f;
                float d = to.magnitude;

                if (d <= radius)
                {
                    float force = Mathf.Lerp(knockback, knockback * 0.35f, d / radius);
                    Vector3 push = (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward) * force;
                    push.y = 5.5f;

                    p.ApplyImpulse(push);
                    if (p.PlayerSlot != sourceSlot && stunTime > 0.0f)
                    {
                        p.ApplyStagger(stunTime);
                        DizzyStars.Attach(p.transform, stunTime, UiTheme.HeroFireBright);
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

        // -------------------------------------------------------------------
        // ICE CUBE PRISON (Cheska Freeze Nova)
        // -------------------------------------------------------------------
        public static GameObject SpawnIceCubePrison(Transform victim, float duration = 2.5f)
        {
            if (victim == null) return null;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "IceCubePrison";
            go.transform.position = victim.position + Vector3.up * 0.95f;
            go.transform.rotation = victim.rotation;
            go.transform.localScale = new Vector3(1.35f, 1.95f, 1.35f);

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                r.material.color = new Color(0.45f, 0.92f, 1.0f, 0.72f);
            }
            Object.Destroy(go.GetComponent<Collider>());

            var comp = go.AddComponent<IceCubePrisonComponent>();
            comp.Duration = duration;
            comp.Victim = victim;

            return go;
        }

        public sealed class IceCubePrisonComponent : MonoBehaviour
        {
            public float Duration = 2.5f;
            public Transform Victim;
            private float _left;

            private void Start() => _left = Duration;

            private void Update()
            {
                if (Victim != null)
                {
                    transform.position = Victim.position + Vector3.up * 0.95f;
                }

                _left -= Time.deltaTime;
                if (_left <= 0.0f)
                {
                    Shatter();
                }
                else if (_left <= 0.5f)
                {
                    // Wobble before breaking
                    transform.position += Random.insideUnitSphere * 0.04f;
                }
            }

            public void Shatter()
            {
                for (int i = 0; i < 12; i++)
                {
                    var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    shard.name = "PrisonIceShard";
                    shard.transform.position = transform.position + Random.insideUnitSphere * 0.6f;
                    shard.transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f);
                    shard.transform.rotation = Random.rotation;

                    var r = shard.GetComponent<Renderer>();
                    if (r != null) r.material.color = new Color(0.7f, 0.96f, 1.0f, 0.85f);

                    var rb = shard.AddComponent<Rigidbody>();
                    rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.5f) * Random.Range(3.5f, 8.0f);
                    rb.angularVelocity = Random.insideUnitSphere * 25.0f;

                    Object.Destroy(shard, 1.2f);
                }

                GameServices.Audio?.PlayAt("sfx_ice_freeze", transform.position);
                ComicPopup.Spawn(transform.position + Vector3.up * 0.8f, "SHATTER!", UiTheme.HeroIceBright, 1.2f);
                Object.Destroy(gameObject);
            }
        }

        // -------------------------------------------------------------------
        // CONFETTI CELEBRATION SHOWER
        // -------------------------------------------------------------------
        public static void SpawnConfettiShower(Vector3 center, int count = 24)
        {
            Color[] colors =
            {
                new Color(1.0f, 0.25f, 0.35f), // Pink Red
                new Color(1.0f, 0.85f, 0.15f), // Gold
                new Color(0.25f, 0.85f, 1.0f), // Sky Blue
                new Color(0.35f, 1.0f, 0.45f), // Emerald Green
                new Color(0.95f, 0.45f, 1.0f), // Magenta
                new Color(1.0f, 0.55f, 0.15f), // Orange
            };

            for (int i = 0; i < count; i++)
            {
                var confetti = GameObject.CreatePrimitive(PrimitiveType.Cube);
                confetti.name = "ConfettiRibbon";
                confetti.transform.position = center + Vector3.up * 1.2f + Random.insideUnitSphere * 0.4f;
                confetti.transform.localScale = new Vector3(0.18f, 0.02f, 0.10f);
                confetti.transform.rotation = Random.rotation;

                var r = confetti.GetComponent<Renderer>();
                if (r != null) r.material.color = colors[Random.Range(0, colors.Length)];
                Object.Destroy(confetti.GetComponent<Collider>());

                var rb = confetti.AddComponent<Rigidbody>();
                rb.linearDamping = 1.8f;
                rb.angularDamping = 2.5f;
                rb.linearVelocity = (Random.insideUnitSphere * 4.0f + Vector3.up * Random.Range(6.0f, 11.0f));
                rb.angularVelocity = Random.insideUnitSphere * 35.0f;

                Object.Destroy(confetti, Random.Range(2.2f, 3.2f));
            }
        }

        // -------------------------------------------------------------------
        // PROCEDURAL ZIG-ZAG LIGHTNING BOLT
        // -------------------------------------------------------------------
        public static GameObject SpawnLightningBolt(Vector3 start, Vector3 end, Color color, float duration = 0.25f)
        {
            var go = new GameObject("LightningBoltHierarchy");
            int segments = 4;
            Vector3 prev = start;

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                Vector3 target = Vector3.Lerp(start, end, t);
                if (i < segments)
                {
                    target += Random.insideUnitSphere * 0.9f;
                }

                var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.name = $"BoltSeg_{i}";
                seg.transform.SetParent(go.transform, false);

                Vector3 dir = target - prev;
                float len = dir.magnitude;
                seg.transform.position = prev + dir * 0.5f;
                seg.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                seg.transform.localScale = new Vector3(0.28f, len * 0.5f, 0.28f);

                var r = seg.GetComponent<Renderer>();
                if (r != null) r.material.color = color;
                Object.Destroy(seg.GetComponent<Collider>());

                prev = target;
            }

            Object.Destroy(go, duration);
            return go;
        }

        // -------------------------------------------------------------------
        // VOLCANIC ROCK DEBRIS (Dante Skills)
        // -------------------------------------------------------------------
        public static void SpawnVolcanicRockDebris(Vector3 center, int count = 8)
        {
            for (int i = 0; i < count; i++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "VolcanicRock";
                rock.transform.position = center + Vector3.up * 0.4f;
                rock.transform.localScale = Vector3.one * Random.Range(0.25f, 0.55f);
                rock.transform.rotation = Random.rotation;

                var r = rock.GetComponent<Renderer>();
                if (r != null)
                {
                    r.material.color = Random.value < 0.5f ? new Color(0.22f, 0.18f, 0.15f) : UiTheme.HeroEarthBright;
                }

                var rb = rock.AddComponent<Rigidbody>();
                rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.8f) * Random.Range(6.0f, 13.0f);
                rb.angularVelocity = Random.insideUnitSphere * 20.0f;

                Object.Destroy(rock, 1.4f);
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
