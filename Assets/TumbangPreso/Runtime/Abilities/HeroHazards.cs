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

            // Create compact glacial wall (3 jagged ice crystals in a focused barrier)
            for (int i = -1; i <= 1; i++)
            {
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = $"IcePillar_{i}";
                pillar.transform.SetParent(go.transform, false);

                float height = (2.6f - Mathf.Abs(i) * 0.4f) * Random.Range(0.95f, 1.15f);
                float width = 0.85f;
                float rotY = i * 8.0f + Random.Range(-4.0f, 4.0f);
                float rotZ = i * -4.0f;

                pillar.transform.localScale = new Vector3(width, height, 0.55f);
                pillar.transform.localPosition = new Vector3(i * 0.75f, height * 0.5f, -Mathf.Abs(i) * 0.12f);
                pillar.transform.localRotation = Quaternion.Euler(Random.Range(-4.0f, 4.0f), rotY, rotZ);

                // ⚠️ THE ONE EFFECT THAT IS BOTH SEE-THROUGH AND SOLID. Everything else that
                // goes through `VfxMaterial` loses its collider; a barricade is the wall the
                // ability is named after, so it keeps it.
                VfxMaterial.Ghost(pillar.GetComponent<Renderer>(),
                                  new Color(0.35f, 0.90f, 1.0f, 0.92f), 0.35f,
                                  stripCollider: false);

                var col = pillar.GetComponent<Collider>();
                if (col != null) col.isTrigger = false;

                // Crystal diamond topper for each pillar
                var topper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                topper.name = $"IceTopper_{i}";
                topper.transform.SetParent(pillar.transform, false);
                topper.transform.localPosition = new Vector3(0, 0.5f, 0);
                topper.transform.localRotation = Quaternion.Euler(45.0f, 45.0f, 0);
                topper.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
                VfxMaterial.Ghost(topper.GetComponent<Renderer>(), new Color(0.85f, 0.98f, 1.0f, 0.95f), 0.6f);
            }

            // Initial ground eruption frost chips
            for (int c = 0; c < 6; c++)
            {
                var chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chip.name = "IceEruptChip";
                chip.transform.position = position + forward * Random.Range(-0.3f, 0.3f) + Vector3.up * 0.2f;
                chip.transform.localScale = Vector3.one * Random.Range(0.14f, 0.26f);
                VfxMaterial.Ghost(chip.GetComponent<Renderer>(), new Color(0.70f, 0.95f, 1.0f, 0.85f));
                var rb = chip.AddComponent<Rigidbody>();
                rb.linearVelocity = Vector3.up * 3.0f + Random.insideUnitSphere * 1.6f;
                Object.Destroy(chip, 0.75f);
            }

            // Cyan frost glow light
            var lightGo = new GameObject("IceLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.2f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroIceBright;
            light.range = 5.0f;
            light.intensity = 2.5f;

            GameServices.Audio?.PlayAt("ability_shatter_trap", position);
            ComicPopup.Spawn(position, "ICE WALL!", UiTheme.HeroIceBright, 1.2f);

            var comp = go.AddComponent<IceBarricadeComponent>();
            comp.Duration = duration;

            HazardVolume.Attach(go, 1.6f, -1);

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

                    // ⚠️⚠️ THE COLLIDER GOES, AND THAT IS A GAMEPLAY FIX RATHER THAN A VISUAL
                    // ONE. Twelve cubes with rigidbodies AND colliders spawned inside the
                    // barricade every time one expired, so anybody standing near a wall that
                    // timed out got shoved around by decoration.
                    VfxMaterial.Ghost(shard.GetComponent<Renderer>(),
                                      new Color(0.6f, 0.95f, 1.0f, 0.85f));

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
        public static GameObject SpawnIceSheet(Vector3 position, float radius = 2.3f, float duration = 5.0f, int ownerSlot = -1)
        {
            var go = new GameObject("IceSheetZone");
            go.transform.position = position;

            // Grand multi-disc frosted surface with rotating snowflake ornaments
            // ⚠️⚠️ A SIX-SIDED CRYSTAL, NOT A DISC, AND THE LOW SIDE COUNT IS THE READ. See
            // `SpawnFireTrail` for why every effect needed its own silhouette. Ice is the one
            // thing in this set that GREW rather than spread or was broken, so it is the one
            // shape that reads as ordered: hard straight edges and six corners, against Sean's
            // directional smear and Zack's discharge star.
            var visual = VfxShapes.Lay(go.transform, "VisualOuter",
                                       VfxShapes.Crystal(6, 0.26f),
                                       radius, 0.01f);

            // ⚠️⚠️ ALPHA 0.30, DOWN FROM 0.65, AND THIS DISC USED TO RENDER AS A HOLE OF PURE
            // WHITE. Measured off `Logs/shots-abilities/ability_ice_sheet_v1.png`: the whole
            // 2.3 m circle came back at 255,255,255 with the ice spikes standing on it invisible
            // against their own floor.
            //
            // ⚠️ THE CAUSE IS THE PAIR, NOT EITHER HALF. A ghost material at 0.65 alpha is
            // survivable and a 2.5-intensity point light 0.6 m above it is survivable; together
            // they push the surface far past 1.0, and `ColourGrade`'s ACES curve cannot recover
            // a value that the surface shader already clipped. This is the same fault, in the
            // same shape, as the sign emission that washed the PC Express fascia pink
            // (`docs/TODO.md` Closed, v16), and it was found the same way: by rendering it.
            VfxMaterial.Ghost(visual.GetComponent<Renderer>(), new Color(0.3f, 0.85f, 1.0f, 0.30f), 0.12f);

            // ⚠️⚠️ THE INNER DISC AND THE FOUR SNOWFLAKE CROSSBARS ARE DELETED, AND THIS IS THE
            // ONE ABILITY THAT WAS BREAKING `docs/VISION.md` § 2 RULE 4 AGAINST ITSELF. Rule 4
            // caps what may overlap; this drew FIVE translucent primitives on one cast, an
            // outer disc at r 2.3, an inner disc at r 1.495, four 3.68 m crossbars, a point
            // light and a mote emitter, before any other player cast anything at all.
            //
            // ⚠️ ITS RADIUS WAS NEVER THE PROBLEM. 2.3 m sits inside the 1.8 to 2.5 m budget and
            // it stays exactly where it was. What replaces the deleted layers is rule 3, the
            // whole point of the budget: **spend it on DETAIL, not on AREA.** Flat translucent
            // planes stacked on flat translucent planes is more puddle, not more ice.
            //
            // What the sheet is made of now: one disc, a HARD CRYSTALLINE RIM that gives the
            // patch an edge instead of a fade, and a cluster of spikes at the centre with real
            // height. A silhouette against the street is readable at a glance and from across
            // the arena; a second wash of the same blue is readable from neither.

            // The rim. A thin ring standing just proud of the floor, so the edge of the danger
            // is a LINE rather than the place a gradient gives up.
            var rim = VfxShapes.Lay(go.transform, "FrostRim",
                                    VfxShapes.Crystal(6, 0.26f),
                                    radius * 1.07f, 0.05f);
            VfxMaterial.Ghost(rim.GetComponent<Renderer>(), new Color(0.70f, 0.93f, 1.0f, 0.42f), 0.28f);
            VfxMaterial.StripCollider(rim);

            // ⚠️ THE SLOWEST OF THE THREE, because ice is the one that GREW rather than spread,
            // and it is the one where the expiry read matters most: this is a slip zone a player
            // stands at the edge of deciding whether to cross. § 8.5 item 2 names it by name.
            var iceLife = rim.AddComponent<HazardRimLife>();
            iceLife.Duration = duration;
            iceLife.BaseAlpha = 0.42f;
            iceLife.PulseAmount = 0.10f;
            iceLife.PulseHz = 0.9f;

            // ⚠️ THE SPIKES ARE THE READ FROM ACROSS THE COURT, AND THEY ARE DELIBERATELY SOLID
            // RATHER THAN GHOSTED. Every other part of this effect is translucent, so the eye
            // has nothing to fix on; five opaque shards catching the key light is what says
            // "ice" at 10 m where a blue disc says "wet".
            //
            // ⚠️ AND THEY ARE UNDER KNEE HEIGHT ON PURPOSE. This is a slip zone a player is
            // meant to be able to see ACROSS while deciding whether to cross it. Anything tall
            // enough to hide a body turns a readability fix into a sightline problem, which is
            // the fault `docs/TODO.md` § 4 is about on Bayan Plaza.
            for (int s = 0; s < 5; s++)
            {
                float ang = s * 72.0f * Mathf.Deg2Rad + 0.4f;
                float rr = radius * (s == 0 ? 0.0f : 0.34f);

                var spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spike.name = $"FrostSpike_{s}";
                spike.transform.SetParent(go.transform, false);
                spike.transform.localPosition = new Vector3(Mathf.Cos(ang) * rr,
                                                            0.16f,
                                                            Mathf.Sin(ang) * rr);
                spike.transform.localRotation = Quaternion.Euler(Random.Range(-14.0f, 14.0f),
                                                                 s * 72.0f,
                                                                 Random.Range(-14.0f, 14.0f));
                // ⚠️ THE SPIKES WERE 0.16 m ACROSS AND VANISHED. In the v1 render they read as
                // five grey specks on a white plate, which is worse than no detail at all
                // because it costs five renderers to draw nothing. Two and a half times the
                // footprint and roughly double the height is what makes a silhouette at the
                // 10 m the arena is actually read across, and they stay under knee height.
                float h = s == 0 ? 0.62f : Random.Range(0.34f, 0.48f);
                spike.transform.localScale = new Vector3(0.30f, h, 0.30f);
                VfxMaterial.Solid(spike.GetComponent<Renderer>(),
                                  new Color(0.62f, 0.86f, 0.98f), 0.10f);
                VfxMaterial.StripCollider(spike);
            }

            // Glowing ice aura light
            var lightGo = new GameObject("FrostLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.5f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroIceBright;
            // ⚠️⚠️ EVERY HAZARD LIGHT IN THIS FILE CAME DOWN BY ROUGHLY TWO THIRDS ON
            // 2026-08-25, AND THE REASON IS THE SAME EVERY TIME: the light was painting its own
            // effect rather than the street around it. A 2.5-intensity source 0.6 m above a
            // 2.3 m disc puts almost all of its energy on the disc, so the "dark" and "tinted"
            // parts of every effect rendered as the light's own colour at full brightness.
            //
            // ⚠️ RAISED AS WELL AS DIMMED. Higher up the falloff across the mark is much
            // flatter, so what is left spills onto the road, which is the job: the glow tells a
            // player something is there before they can see what it is.
            light.range = radius * 2.2f;
            light.intensity = 0.9f;

            // ⚠️⚠️ CHESKA'S AMBIENCE IS ON HER ZONE, NOT ON HER BODY. She is the one hero with
            // no aura of her own and that is deliberate: all three of her powers are placed on
            // the GROUND, so motes on her model would point at her while the thing that matters
            // is three metres in front of her. The frost breathes where the danger is.
            //
            // ⚠️ ONE EMITTER, ON A ZONE, NEVER ON A TRAIL. A zone is singular and lives 5 s;
            // the dash trails drop thirty discs a dash and get nothing.
            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.FrostMote, duration);

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

            // ⚠️⚠️ A LIVE ANCHOR WITH AN ARC, NOT A YELLOW DISC. Same fault and same fix as the
            // fire trail above, and the same measurement behind it: this is dropped once every
            // 0.30 s of a 2.5 s sprint and every drop used to be one flat translucent cylinder,
            // so a single dash laid a yellow carpet across a quarter of the arena.
            //
            // What it is made of now: a small dark scorch, a bright ring, and a CRACKLING
            // VERTICAL ARC that snaps at knee height. The arc is the whole read. A wire on the
            // ground is something you can see is live from the SIDE, which is exactly the
            // information a player sprinting toward it needs and which a disc seen edge-on at
            // eye height cannot give them.
            //
            // `SeanHeroKit`'s fire trail carries the full reasoning; this is its counterpart in
            // Zack's palette, and the two are deliberately different SHAPES rather than the same
            // shape in two colours. `docs/Hero_Strike_Balance.md` § 4.4.

            // ⚠️⚠️ A JAGGED STAR, NOT A DISC. See the note on `SpawnFireTrail`: every floor
            // effect in the game used to be the same cylinder and hue was doing all the work.
            // Lightning arrives at a point and runs OUT along the ground, so its mark is a
            // discharge pattern; that is a different silhouette from Sean's directional smear
            // even before either of them is coloured, which is the point.
            int seed = Mathf.RoundToInt((position.x - position.z) * 613.0f);

            var visual = VfxShapes.Lay(go.transform, "ShockScorch",
                                       VfxShapes.Star(7, 0.40f, seed),
                                       radius * 0.72f, 0.015f);
            VfxMaterial.Ghost(visual.GetComponent<Renderer>(), new Color(0.16f, 0.14f, 0.03f, 0.90f), 0.0f);
            VfxMaterial.StripCollider(visual);

            var ring = VfxShapes.Lay(go.transform, "ShockRing",
                                     VfxShapes.Star(7, 0.40f, seed),
                                     radius, 0.010f);
            // Same clipping fault and same fix as the fire trail's rim: 1.15 emission wrote
            // past white and `ability_shock_trail_v1.png` came back as one flat yellow coin with
            // the arc invisible on top of it.
            VfxMaterial.Ghost(ring.GetComponent<Renderer>(), new Color(1.0f, 0.92f, 0.18f, 0.55f), 0.32f);

            // Electricity does not breathe, it flickers. Fastest pulse of the three and a
            // shallower swing, so it reads as current rather than as heat.
            var shockLife = ring.AddComponent<HazardRimLife>();
            shockLife.Duration = duration;
            shockLife.BaseAlpha = 0.55f;
            shockLife.PulseAmount = 0.13f;
            shockLife.PulseHz = 4.1f;
            VfxMaterial.StripCollider(ring);

            // ⚠️ THE ARC IS A JAGGED LINE THAT REBUILDS ITSELF ON A TIMER. A straight bolt reads
            // as a post; the jitter is what makes it read as current.
            var arcGo = new GameObject("ShockArc");
            arcGo.transform.SetParent(go.transform, false);
            arcGo.transform.localPosition = Vector3.zero;
            arcGo.AddComponent<ArcFlicker>().Build(radius);

            // Flashing electric sparks light
            var lightGo = new GameObject("ShockLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.2f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroElectricBright;
            // Fixed rather than radius-scaled, and dimmed for the reason on the fire trail's
            // light: at 3.5 it rendered its own scorch as flat yellow.
            light.range = 3.0f;
            light.intensity = 1.0f;

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
                                ComicPopup.Zap(p.transform.position);   // Flavour: culled past 15 m
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
        public static GameObject SpawnFireTrail(Vector3 position, float radius = 1.8f,
                                               float duration = 3.0f, int ownerSlot = -1,
                                               Vector3 forward = default)
        {
            var go = new GameObject("FireTrailZone");
            go.transform.position = position;

            // ⚠️⚠️ A DASH LEAVES A STREAK, NOT A CIRCLE, AND UNTIL 2026-08-25 EVERY FLOOR EFFECT
            // IN THIS GAME WAS THE SAME SCALED CYLINDER. 🧑, on the first ability capture: *"look
            // at this shit all of them look like circles lang"*. Fire, lightning, ice, magma and
            // a tear in the world were five fictions drawn as one primitive in five colours, so
            // the only channel telling them apart was HUE, which is the one channel the game
            // already spends twice: `Art_Direction.md` § 1 reserves orange and blue for the two
            // ROLES, and `UiTheme` spends five more on hero identity.
            //
            // ⚠️ THE STREAK IS A GAMEPLAY READ AS WELL AS AN ART ONE. It POINTS. A player who
            // sees one knows which way the caster went, which a chain of circles cannot tell
            // them, and knowing where Sean went is most of what surviving Sean is.
            //
            // ⚠️ SEEDED OFF THE POSITION so consecutive marks in one trail differ from each
            // other while any given mark is identical between captures. `VfxShapes` has the
            // reasoning; an unseeded probe is one that measured 110 and then 467 penalties on
            // consecutive runs (`CLAUDE.md` § 7.1).
            int seed = Mathf.RoundToInt((position.x + position.z) * 977.0f);
            float yaw = 0.0f;
            if (forward.sqrMagnitude > 0.0001f)
            {
                forward.y = 0.0f;
                yaw = Quaternion.LookRotation(forward.normalized, Vector3.up).eulerAngles.y;
            }

            // ⚠️⚠️ A SCORCH MARK WITH A BURNING RIM, NOT AN ORANGE DISC. This is the single
            // most-seen effect in Hero Strike (Sean drops one every 0.15 s of every dash) and
            // until 2026-08-25 it was one flat translucent cylinder, which is the literal
            // definition of the fault described as *"it just looks like puddles everywhere"*.
            //
            // Three parts, and each is doing a job the flat disc could not:
            //  * a DARK charred core, so the mark reads as burnt ground rather than as coloured
            //    light lying on the road;
            //  * a bright licking RIM at the edge, which is where a real fire actually is;
            //  * a short ember column, because heat is the only thing in this game that goes UP
            //    and vertical is the one direction a floor effect has spare.
            //
            // `docs/VISION.md` § 2 rule 3: the budget is spent on DETAIL, not on AREA. The
            // radius came down from 1.6 to 1.0 at the same time (`SeanHeroKit.TrailRadius`), so
            // this is strictly more to look at over strictly less floor.

            // The char. Dark, nearly opaque, and the thing that persists. Stretched 1.55x
            // along the run and pinched across it, so the mark has a direction.
            var visual = VfxShapes.Lay(go.transform, "FireChar",
                                       VfxShapes.Streak(0.60f, 12, seed),
                                       radius * 0.90f, 0.015f, yaw);
            visual.transform.localScale = new Vector3(radius * 0.62f, 1.0f, radius * 1.28f);
            // ⚠️⚠️ 0.93 ALPHA, AND "MAKE IT TRANSLUCENT SO THE ROAD SHOWS THROUGH" IS THE
            // MISTAKE THAT COST THREE RENDER PASSES. Write it down so nobody retries it.
            //
            // The char is drawn ON TOP of `edge`, which is a full bright orange DISC and not a
            // ring: `edge` covers the whole footprint at radius * 2.0 and the char covers
            // radius * 1.80 of it. So any alpha under about 0.9 here does not blend the char
            // with the ROAD, it blends the char with the bright orange plate directly beneath
            // it, and the result is mid-terracotta however dark the colour is.
            //
            // ⚠️ THAT IS WHY DARKENING THE COLOUR TWICE AND CUTTING THE LIGHT TWICE DID NOTHING.
            // v1 blamed the colour (0.16, 0.05, 0.02), v2 blamed the light (3.5 down to 1.1),
            // v3 blamed both (0.05, 0.02, 0.01 and 0.55) and every one of the three still
            // rendered an orange middle, because none of them was the cause. Alpha was.
            // `Logs/shots-abilities/ability_fire_trail_v1.png` through `_v3.png` are the three
            // wrong answers, kept on disk.
            //
            // ⚠️ A SCORCH IS OPAQUE ANYWAY. Burnt asphalt does not show the road through it.
            //
            // ⚠️ THE PLATE ALSO GREW TO radius * 1.80 SO THE RIM BAND IS THINNER. At 1.55 the
            // gap to the 2.0 edge was a fat mustard donut and the mark read as a ring rather
            // than as burnt ground with a hot edge.
            VfxMaterial.Ghost(visual.GetComponent<Renderer>(), new Color(0.17f, 0.07f, 0.03f, 0.92f), 0.0f);
            VfxMaterial.StripCollider(visual);

            // The burning edge. Bright, thin, and OUTSIDE the char, so the mark has a hot
            // perimeter and a cold middle exactly like a scorch does.
            var edge = VfxShapes.Lay(go.transform, "FireEdge",
                                     VfxShapes.Streak(0.60f, 12, seed + 1),
                                     1.0f, 0.010f, yaw);
            edge.transform.localScale = new Vector3(radius * 0.86f, 1.0f, radius * 1.66f);
            // ⚠️ EMISSION 1.05 CLIPPED THE RIM TO A FLAT YELLOW DONUT. Measured off
            // `ability_fire_trail_v1.png`: the ring came back as one solid band with no shading
            // and no hue left in it, because anything over about 0.5 here writes past white
            // before `ColourGrade` ever sees the frame. 0.30 keeps it hot and keeps it ORANGE.
            VfxMaterial.Ghost(edge.GetComponent<Renderer>(), new Color(1.0f, 0.36f, 0.05f, 0.42f), 0.22f);
            VfxMaterial.StripCollider(edge);

            // The rim licks while the fire burns and dies back as it goes out. See `HazardRimLife`.
            var fireLife = edge.AddComponent<HazardRimLife>();
            fireLife.Duration = duration;
            fireLife.BaseAlpha = 0.42f;
            fireLife.PulseAmount = 0.20f;   // flame is the most restless of the three
            fireLife.PulseHz = 2.3f;

            // ⚠️⚠️ THE EMBERS ARE CUBES, AND THE BILLBOARD QUADS THEY REPLACE RENDERED AS
            // LITERAL YELLOW SQUARES. `ability_fire_trail_v1.png` is unambiguous about it: three
            // flat rectangles standing on the mark, because a `Quad` with an untextured material
            // IS a rectangle and nothing about facing the camera changes that. A soft plume
            // needs an alpha texture, which is a whole asset path this effect does not warrant.
            //
            // ⚠️ CUBES ARE ALSO THE RIGHT ANSWER RATHER THAN A CHEAP ONE. This game is voxel
            // art (`docs/Voxel_Person_Guide.md`, and the whole cast is built from boxes), so a
            // scatter of small bright cubes rising off a scorch is IN the visual language. A
            // soft photographic flame would be the thing that looked broken, which is
            // `docs/VISION.md` § 6: *"his UI art is the design system. Anything drawn in a
            // different visual language is the thing that looks broken, not the thing that
            // looks new."*
            for (int f = 0; f < 5; f++)
            {
                var ember = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ember.name = $"Ember_{f}";
                ember.transform.SetParent(go.transform, false);

                float fa = f * 72.0f * Mathf.Deg2Rad + 0.5f;
                float fr = radius * Random.Range(0.15f, 0.62f);
                ember.transform.localPosition = new Vector3(Mathf.Cos(fa) * fr,
                                                            Random.Range(0.14f, 0.52f),
                                                            Mathf.Sin(fa) * fr);
                ember.transform.localRotation = Random.rotation;

                float s = Random.Range(0.07f, 0.14f);
                ember.transform.localScale = new Vector3(s, s, s);

                // Solid rather than ghosted: an ember is a hot chip of something, and the one
                // thing in this effect that should be opaque.
                VfxMaterial.Solid(ember.GetComponent<Renderer>(),
                                  new Color(1.0f, Random.Range(0.45f, 0.72f), 0.12f), 0.30f);
                VfxMaterial.StripCollider(ember);
                ember.AddComponent<EmberDrift>();
            }

            // Flickering fire light
            var lightGo = new GameObject("FireLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.3f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroFireBright;
            // ⚠️ THE LIGHT REACH IS DECOUPLED FROM THE HAZARD RADIUS NOW. At radius 1.0 a
            // `radius * 2.4` range is 2.4 m, which lights nothing; the mark still has to throw
            // a glow onto the street or a narrower trail becomes an invisible one. 3.2 m fixed.
            //
            // ⚠️⚠️ AND 3.5 INTENSITY IS WHAT MADE THE DARK CHAR RENDER BRIGHT SALMON. See the
            // note on the ice sheet's light: a hot source sitting on top of its own effect
            // paints the effect, not the street. At 1.1 the char stays burnt and the glow still
            // reaches the road, which is the only thing the light was ever for.
            light.range = 3.2f;
            light.intensity = 0.55f;

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

            // ⚠️ A PROPERTY BLOCK CANNOT MAKE AN OPAQUE MATERIAL SEE-THROUGH. The block wrote
            // an alpha of 0.9 into a shader that never reads one, so the decoy was a solid
            // purple ball rather than a spirit.
            VfxMaterial.Ghost(visual.GetComponent<Renderer>(), new Color(0.85f, 0.4f, 1.0f, 0.9f), 0.7f);

            // Spectral Horns / Ears
            for (int h = -1; h <= 1; h += 2)
            {
                var horn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                horn.name = $"GhostHorn_{h}";
                horn.transform.SetParent(visual.transform, false);
                horn.transform.localPosition = new Vector3(h * 0.28f, 0.40f, 0.05f);
                horn.transform.localRotation = Quaternion.Euler(15.0f, 0, h * -25.0f);
                horn.transform.localScale = new Vector3(0.22f, 0.32f, 0.20f);
                VfxMaterial.Ghost(horn.GetComponent<Renderer>(), new Color(0.95f, 0.65f, 1.0f, 0.85f), 0.8f);
            }

            // Swirling spirit halo ring
            var halo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            halo.name = "SpiritHalo";
            halo.transform.SetParent(visual.transform, false);
            halo.transform.localPosition = new Vector3(0, -0.1f, 0);
            halo.transform.localScale = new Vector3(1.25f, 0.03f, 1.25f);
            VfxMaterial.Ghost(halo.GetComponent<Renderer>(), new Color(0.75f, 0.30f, 1.0f, 0.65f), 0.6f);

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

            VfxMaterial.Solid(pillar.GetComponent<Renderer>(), new Color(0.28f, 0.20f, 0.16f));

            var magmaTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            magmaTop.name = "MagmaTop";
            magmaTop.transform.SetParent(go.transform, false);
            magmaTop.transform.localScale = new Vector3(1.45f, 0.85f, 1.45f);
            magmaTop.transform.localPosition = new Vector3(0, 4.8f, 0);
            // ⚠️ MAGMA STAYS ORANGE WHILE DANTE KEEPS A JADE ACCENT. See the note on
            // `UiTheme.HeroMagmaCore`: his colour is the crust, this is the melt.
            VfxMaterial.Ghost(magmaTop.GetComponent<Renderer>(), UiTheme.HeroMagmaCore, 0.9f);

            // 4 Basalt angled base buttresses
            for (int b = 0; b < 4; b++)
            {
                var buttress = GameObject.CreatePrimitive(PrimitiveType.Cube);
                buttress.name = $"BasaltButtress_{b}";
                buttress.transform.SetParent(go.transform, false);
                float angle = b * 90.0f;
                buttress.transform.localRotation = Quaternion.Euler(20.0f, angle, 0);
                buttress.transform.localPosition = Quaternion.Euler(0, angle, 0) * new Vector3(0, 0.6f, 0.85f);
                buttress.transform.localScale = new Vector3(0.65f, 1.4f, 0.55f);
                VfxMaterial.Solid(buttress.GetComponent<Renderer>(), new Color(0.24f, 0.17f, 0.14f));
                VfxMaterial.StripCollider(buttress);
            }

            // Initial eruption volcanic debris
            for (int d = 0; d < 4; d++)
            {
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "PillarEruptRock";
                rock.transform.position = position + Vector3.up * 0.3f + Random.insideUnitSphere * 0.4f;
                rock.transform.localScale = Vector3.one * Random.Range(0.18f, 0.35f);
                VfxMaterial.Solid(rock.GetComponent<Renderer>(), UiTheme.HeroMagmaCore);
                VfxMaterial.StripCollider(rock);
                var rb = rock.AddComponent<Rigidbody>();
                rb.linearVelocity = Vector3.up * 4.5f + Random.insideUnitSphere * 2.0f;
                Object.Destroy(rock, 0.85f);
            }

            var lightGo = new GameObject("MagmaLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 4.2f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroMagmaCore;
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
        public static GameObject SpawnCrackedLavaDecal(Vector3 position, float radius = 2.4f, float duration = 4.0f)
        {
            var go = new GameObject("CrackedLavaDecal");
            go.transform.position = position;

            // ⚠️⚠️ AN IRREGULAR FRACTURE, NOT A DISC. See `SpawnFireTrail`: hue was carrying
            // the whole load because every effect was the same cylinder. Ground that has been
            // STOMPED breaks along uneven lines, so a ragged eleven-sided outline is both the
            // honest shape and the one that cannot be mistaken for Cheska's hard hexagon or
            // Zack's star at a glance.
            int seed = Mathf.RoundToInt((position.x * 3.0f + position.z) * 401.0f);

            var outer = VfxShapes.Lay(go.transform, "CrackedAsphalt",
                                      VfxShapes.Splat(11, 0.30f, seed),
                                      radius, 0.015f);
            VfxMaterial.Ghost(outer.GetComponent<Renderer>(), new Color(0.19f, 0.14f, 0.11f, 0.88f), 0.0f);
            VfxMaterial.StripCollider(outer);

            // ⚠️⚠️ THE CRACKS ARE CRACKS NOW, AND THIS EFFECT WAS CALLED `SpawnCrackedLavaDecal`
            // WHILE DRAWING A SOLID DISC. `Logs/shots-abilities/ability_lava_decal_v2.png` shows
            // what shipped: a flat plate of blown-out yellow, `UiTheme.HeroMagmaCore` at 0.9
            // emission, which writes past white and takes every trace of hue with it. The name
            // promised fissured ground and the geometry delivered a coin.
            //
            // ⚠️ IT IS A DARK PLATE WITH GLOWING SEAMS, WHICH IS THE OPPOSITE ARRANGEMENT AND
            // THE CORRECT ONE. Cooled crust is nearly black and the heat shows in the gaps, so
            // the bright pixels are a small fraction of the footprint rather than all of it.
            // That is `docs/VISION.md` § 2 rule 3 exactly: the same 2.2 m circle, spent on
            // detail instead of on area, and it costs the frame far less because most of it is
            // now dark.
            //
            // Seven seams radiating from the centre at uneven angles and lengths. Uneven because
            // a regular star reads as a manhole cover, which is a thing that is already on this
            // map.
            for (int c = 0; c < 7; c++)
            {
                float a = c * (360.0f / 7.0f) + Random.Range(-14.0f, 14.0f);
                float len = radius * Random.Range(0.55f, 0.95f);

                var seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
                seam.name = $"MagmaSeam_{c}";
                seam.transform.SetParent(go.transform, false);
                seam.transform.localRotation = Quaternion.Euler(0.0f, a, 0.0f);
                seam.transform.localPosition = Quaternion.Euler(0.0f, a, 0.0f)
                                               * new Vector3(0.0f, 0.0f, len * 0.5f)
                                               + new Vector3(0.0f, 0.03f, 0.0f);
                seam.transform.localScale = new Vector3(Random.Range(0.09f, 0.17f), 0.02f, len);

                VfxMaterial.Ghost(seam.GetComponent<Renderer>(),
                                  new Color(1.0f, 0.34f, 0.04f, 0.90f), 0.38f);
                VfxMaterial.StripCollider(seam);
            }

            // A small hot centre where the foot landed, rather than a disc covering the lot.
            var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            core.name = "MagmaCore";
            core.transform.SetParent(go.transform, false);
            core.transform.localScale = new Vector3(radius * 0.42f, 0.025f, radius * 0.42f);
            core.transform.localPosition = new Vector3(0, 0.02f, 0);
            VfxMaterial.Ghost(core.GetComponent<Renderer>(),
                              new Color(1.0f, 0.42f, 0.06f, 0.85f), 0.34f);
            VfxMaterial.StripCollider(core);

            // Lava pulse light. Raised and cut to a quarter, for the reason written up on the
            // ice sheet's light: at 4.0 sitting 0.8 m over its own decal it lit the decal.
            var lightGo = new GameObject("LavaLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.6f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroMagmaCore;
            light.range = radius * 2.4f;
            light.intensity = 1.0f;

            Object.Destroy(go, duration);
            return go;
        }

        // -------------------------------------------------------------------
        // SEANCE VOID ZONE (Nemu Ultimate)
        // -------------------------------------------------------------------
        public static GameObject SpawnSeanceVoid(Vector3 position, float radius = 3.2f, float duration = 5.0f, int ownerSlot = -1)
        {
            var go = new GameObject("SeanceVoidZone");
            go.transform.position = position;

            // ⚠️⚠️ THE VOID IS DARK IN THE MIDDLE AND BRIGHT AT THE RIM, AND IT USED TO BE THE
            // EXACT OPPOSITE. `Logs/shots-abilities/ability_seance_void_v1.png` is the evidence
            // and there is no kind way to describe what it showed: a violet ring around a disc
            // of pure 255,255,255 white with a solid purple ball sitting in it, which reads as a
            // cartoon EYEBALL and not as a hole in the world. It was the worst-looking effect in
            // the game and it had never been rendered on its own.
            //
            // Three separate faults, all of them the same mistake made three times:
            //  * `VortexInner` was `HeroSpiritBright` at 0.70 alpha and 0.8 emission, which
            //    clips past white before `ColourGrade` ever sees it, so the brightest part of
            //    the effect was its CENTRE;
            //  * `SingularityCore` was a 1.54 m sphere at 0.95 alpha, which is opaque, so the
            //    "singularity" was a beach ball;
            //  * a 4.5-intensity light 1.2 m up then lit all of it at once.
            //
            // ⚠️ A THING THAT PULLS YOU IN HAS TO LOOK LIKE IT GOES DOWN. That is the whole read
            // and it is what `docs/VISION.md` § 2 rule 3 means by spending the budget on detail:
            // the darkest point is the middle, the rim is where the light is, and the shards
            // lean inward so the eye is carried toward the centre rather than around the edge.
            // It also lets the void be SMALLER (2.8 m, down from 3.2) without reading as weaker.

            // ⚠️⚠️ IT HOVERS. A BLACK HOLE LYING FLAT ON THE ROAD IS A MANHOLE. 🧑 2026-08-25:
            // *"make make blackhole float, its on the floor"*. Every other effect in this file
            // is ground-level because every other effect IS ground: a scorch, a fracture, a
            // frozen patch. This one is a tear in the world, and the one thing that separates it
            // from a painted circle is that the street carries on underneath it.
            //
            // ⚠️ THE HOVER IS ALSO WHY IT IS THE ONLY ROUND SHAPE LEFT. Every other effect got
            // its own silhouette on 2026-08-25 because they were all discs (see
            // `SpawnFireTrail`). A vortex is genuinely radial and a circle is the honest shape
            // for it, so instead of changing the outline it changes AXIS: horizontal versus
            // vertical is a bigger difference than any two outlines on the same plane.
            const float Hover = 1.35f;

            var core = new GameObject("VoidCore");
            core.transform.SetParent(go.transform, false);
            core.transform.localPosition = new Vector3(0.0f, Hover, 0.0f);

            // The mouth. Nearly black, and the one part that must never be bright.
            var outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outer.name = "VoidMouth";
            outer.transform.SetParent(core.transform, false);
            outer.transform.localScale = new Vector3(radius * 2.0f, 0.04f, radius * 2.0f);
            outer.transform.localPosition = Vector3.zero;
            VfxMaterial.Ghost(outer.GetComponent<Renderer>(), new Color(0.07f, 0.02f, 0.12f, 0.90f), 0.0f);

            // The throat. Smaller and DARKER still, so the disc has depth rather than being one
            // flat plate: two steps down reads as a funnel where one step reads as a lid.
            var inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            inner.name = "VoidThroat";
            inner.transform.SetParent(core.transform, false);
            inner.transform.localScale = new Vector3(radius * 1.05f, 0.05f, radius * 1.05f);
            inner.transform.localPosition = new Vector3(0, 0.01f, 0);
            VfxMaterial.Ghost(inner.GetComponent<Renderer>(), new Color(0.02f, 0.00f, 0.05f, 0.96f), 0.0f);

            // The event horizon: a thin bright ring at the LIP, which is the only lit part.
            var lip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lip.name = "VoidLip";
            lip.transform.SetParent(core.transform, false);
            lip.transform.localScale = new Vector3(radius * 2.14f, 0.05f, radius * 2.14f);
            lip.transform.localPosition = new Vector3(0, -0.01f, 0);
            VfxMaterial.Ghost(lip.GetComponent<Renderer>(),
                              new Color(0.62f, 0.20f, 0.98f, 0.60f), 0.34f);
            VfxMaterial.StripCollider(lip);

            // ⚠️⚠️ AND THE GROUND STILL HAS TO SAY WHERE THE DANGER IS, WHICH IS THE HALF A
            // HOVERING EFFECT BREAKS. The hazard resolves by distance on the FLOOR
            // (`SeanceVoidComponent` compares flat positions), so lifting the art off the floor
            // without leaving a mark would put the gameplay circle somewhere the player cannot
            // see it. That is the exact fault `HeroAbility.TelegraphRadius` exists to stop: a
            // telegraph that lies is worse than no telegraph.
            //
            // A faint ring at the real radius, on the road, under the hovering vortex.
            var pull = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pull.name = "VoidGroundPull";
            pull.transform.SetParent(go.transform, false);
            pull.transform.localScale = new Vector3(radius * 2.0f, 0.02f, radius * 2.0f);
            pull.transform.localPosition = new Vector3(0, 0.015f, 0);
            VfxMaterial.Ghost(pull.GetComponent<Renderer>(),
                              new Color(0.34f, 0.10f, 0.62f, 0.42f), 0.16f);
            VfxMaterial.StripCollider(pull);

            // ⚠️ THE SHARDS ARE THE PULL, AND THEY ARE WHAT THE OLD CORE ORB SHOULD HAVE BEEN.
            // Eight chips of debris caught mid-fall, leaning in and tipped down toward the
            // centre. They say "this drags things" in a still frame, which a rotating disc
            // cannot, and they are cubes for the same reason the fire trail's embers are: the
            // cast and the props are voxel art and a smooth effect is the thing that looks
            // wrong. `SeanceVoidComponent` spins the parent, so they orbit for free.
            for (int s = 0; s < 8; s++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = $"VoidShard_{s}";
                shard.transform.SetParent(go.transform, false);

                // ⚠️ THE SHARDS SPAN THE GAP BETWEEN THE ROAD AND THE HOVERING CORE, which is
                // what sells the hover: debris caught partway UP is the only thing in the frame
                // that says the vortex is lifting rather than that the art is floating by
                // mistake. Heights are spread across the whole 0.2 m to 1.5 m column on purpose.
                float a = s * 45.0f * Mathf.Deg2Rad;
                float rr = radius * Random.Range(0.55f, 0.92f);
                shard.transform.localPosition = new Vector3(Mathf.Cos(a) * rr,
                                                            Random.Range(0.20f, 1.50f),
                                                            Mathf.Sin(a) * rr);

                // Tipped toward the middle, which is the half of this that carries the meaning.
                shard.transform.localRotation = Quaternion.Euler(Random.Range(28.0f, 52.0f),
                                                                 -a * Mathf.Rad2Deg,
                                                                 Random.Range(-20.0f, 20.0f));
                float sc = Random.Range(0.10f, 0.20f);
                shard.transform.localScale = new Vector3(sc, sc * Random.Range(1.2f, 2.4f), sc);

                VfxMaterial.Solid(shard.GetComponent<Renderer>(),
                                  new Color(0.55f, 0.28f, 0.85f), 0.22f);
                VfxMaterial.StripCollider(shard);
            }

            // Pulsing violet gravity light
            var lightGo = new GameObject("VoidLight");
            lightGo.transform.SetParent(go.transform, false);
            // ⚠️ RAISED AND CUT TO A QUARTER. See the ice sheet's light: at 4.5 sitting 1.2 m
            // over its own disc, this lit the void rather than the street, which is how a hole
            // in the world ended up being the brightest thing in the frame.
            lightGo.transform.localPosition = new Vector3(0, 2.1f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroSpiritBright;
            light.range = radius * 2.6f;
            light.intensity = 1.1f;

            // ⚠️ THE VORTEX EMITS FOR ITS WHOLE LIFE, not just at the moment it opens. It is
            // a 5 s zone that DRAGS people in, so it has to keep looking dangerous the whole
            // time; a one-shot burst at cast leaves four seconds of a flat purple disc.
            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.VoidWisp, duration);

            // ⚠️ A TEAR IN THE WORLD IS NOT A BOMB. This played `ability_bagsak_bomb`, so Nemu's
            // ultimate opened with the same detonation as Sean's and Dante's. `sfx_possess_enter`
            // is the rising, sucking shape written for her possession and it is the same gesture
            // a vortex makes: something being pulled through.
            GameServices.Audio?.PlayAt("sfx_possess_enter", position);
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

            // 1b. Secondary Branching Fork Lightning Bolts
            SpawnLightningBolt(position + new Vector3(-1.5f, 18.0f, 1.2f), position + new Vector3(-0.8f, 0, 0.6f), UiTheme.HeroElectricBright, 0.35f);
            SpawnLightningBolt(position + new Vector3(1.4f, 20.0f, -1.0f), position + new Vector3(0.9f, 0, -0.5f), UiTheme.HeroElectricBright, 0.35f);

            // 2. Flying electric spark shards
            for (int i = 0; i < 12; i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = "ThunderSpark";
                spark.transform.position = position + Vector3.up * 0.5f;
                spark.transform.localScale = Vector3.one * Random.Range(0.15f, 0.35f);

                VfxMaterial.Ghost(spark.GetComponent<Renderer>(), UiTheme.HeroElectricBright, 1.0f);

                var rb = spark.AddComponent<Rigidbody>();
                rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.5f) * Random.Range(5.0f, 12.0f);
                Object.Destroy(spark, 0.5f);
            }

            // 3. Expanding Electric Ground Shockwave
            var shockRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shockRing.name = "ThunderShockRing";
            shockRing.transform.position = position + Vector3.up * 0.04f;
            shockRing.transform.localScale = new Vector3(0.5f, 0.03f, 0.5f);
            VfxMaterial.Ghost(shockRing.GetComponent<Renderer>(), UiTheme.HeroElectric, 0.8f);

            // 3b. Inner Ionization Core Flash Disc
            var ionCore = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ionCore.name = "ThunderIonCore";
            ionCore.transform.position = position + Vector3.up * 0.045f;
            ionCore.transform.localScale = new Vector3(radius * 0.9f, 0.02f, radius * 0.9f);
            VfxMaterial.Ghost(ionCore.GetComponent<Renderer>(), new Color(1.0f, 1.0f, 0.60f, 0.85f), 1.0f);
            Object.Destroy(ionCore, 0.20f);

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

            // ⚠️⚠️ THIS WAS `ability_flick_dash`. Zack's ultimate, the loudest thing in his kit,
            // landed on the sound of a dash, from the deleted ability set. `sfx_lightning_strike`
            // stays on the CAST, where the arc is; this is the bolt reaching the street, and it
            // has the crack, the ground sub and the roll that a dash cannot supply.
            GameServices.Audio?.PlayAt("sfx_thunder_impact", position);
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
        /// <summary>
        /// Which fiction a blast belongs to.
        ///
        /// ⚠️⚠️ IT EXISTS BECAUSE ONE FUNCTION WAS DRAWING FOUR DIFFERENT EVENTS.
        /// `Hero_Strike_Balance.md` § 8.5 item 3: *"`CreateExplosion` draws the same sphere and
        /// shockwave for a 2.2 m stomp and a 4.8 m Supernova, so the two biggest moments in two
        /// different kits are the same picture at two sizes."* They were also the same SOUND at
        /// two sizes, and the same colour, and the same debris.
        ///
        /// ⚠️ THE DEFAULT IS `Fire` SO THE ENUM IS ADDITIVE. Every existing call keeps exactly
        /// the look it had unless it opts into another one.
        /// </summary>
        public enum ExplosionStyle
        {
            /// <summary>Sean. A ball of flame, embers, a burnt splat on the road.</summary>
            Fire,

            /// <summary>Dante. No fireball at all: the ground breaks and throws rock.</summary>
            Quake,

            /// <summary>Cheska. Ice going outward, shards rather than embers.</summary>
            Frost,

            /// <summary>A thrown tsinelas. Small, light, and the joke rather than an ultimate.</summary>
            Slipper,
        }

        /// <summary>The per-style numbers. See `ExplosionStyle` for why this is not one look.</summary>
        private readonly struct ExplosionLook
        {
            public readonly Color Core;
            public readonly Color Edge;
            public readonly string Cue;
            public readonly bool HasCore;
            public readonly int DebrisCount;
            public readonly Vector2 DebrisSize;
            public readonly Vector2 DebrisSpeed;
            public readonly float DebrisLift;
            public readonly float DebrisLife;
            public readonly float FlashIntensity;
            public readonly float FlashSeconds;
            public readonly float ShakeAmount;
            public readonly float ShakeSeconds;

            private readonly ExplosionStyle _style;

            public ExplosionLook(ExplosionStyle style, Color core, Color edge, string cue, bool hasCore,
                                 int debrisCount, Vector2 debrisSize, Vector2 debrisSpeed, float debrisLift,
                                 float debrisLife, float flashIntensity, float flashSeconds,
                                 float shakeAmount, float shakeSeconds)
            {
                _style = style;
                Core = core; Edge = edge; Cue = cue; HasCore = hasCore;
                DebrisCount = debrisCount; DebrisSize = debrisSize; DebrisSpeed = debrisSpeed;
                DebrisLift = debrisLift; DebrisLife = debrisLife;
                FlashIntensity = flashIntensity; FlashSeconds = flashSeconds;
                ShakeAmount = shakeAmount; ShakeSeconds = shakeSeconds;
            }

            /// <summary>
            /// The outline the blast leaves. ⚠️ NONE OF THEM IS A CIRCLE, which is the whole
            /// point of `VfxShapes`: `Hero_Strike_Balance.md` § 8.3 gives one silhouette per
            /// fiction and the shared explosion was the last thing still drawing a cylinder.
            /// </summary>
            public Mesh Ring(int seed)
            {
                switch (_style)
                {
                    // Broken ground fractures along uneven lines, and more raggedly than a burn.
                    case ExplosionStyle.Quake: return VfxShapes.Splat(13, 0.34f, seed);

                    // Ice is the one thing here that is ORDERED, so it is the one hard shape.
                    case ExplosionStyle.Frost: return VfxShapes.Crystal(6, 0.18f);

                    // A sandal makes a comic pop, not a crater. Few points, short ones.
                    case ExplosionStyle.Slipper: return VfxShapes.Star(6, 0.62f, seed);

                    // Burnt ground: ragged, but rounder than a fracture.
                    default: return VfxShapes.Splat(11, 0.22f, seed);
                }
            }

            /// <summary>Debris tint, varied per shard so the throw is not one flat colour.</summary>
            public Color DebrisColour()
            {
                switch (_style)
                {
                    case ExplosionStyle.Quake:
                        // Rock and dust, not sparks. Desaturated and dark so it reads as mass.
                        float g = Random.Range(0.28f, 0.46f);
                        return new Color(g * 1.15f, g, g * 0.82f);

                    case ExplosionStyle.Frost:
                        return Color.Lerp(UiTheme.HeroIce, UiTheme.HeroIceBright, Random.value);

                    case ExplosionStyle.Slipper:
                        // The tsinelas itself: rubber blue and foam cream.
                        return Random.value < 0.5f
                            ? new Color(0.24f, 0.45f, 0.72f)
                            : new Color(0.92f, 0.88f, 0.74f);

                    default:
                        return new Color(1.0f, Random.Range(0.4f, 0.9f), 0.1f);
                }
            }

            /// <summary>
            /// ⚠️ THE ELEMENTAL BURSTS ALREADY EXISTED AND THIS FUNCTION NEVER CALLED ONE.
            /// `AbilityVfx` has `SpawnMagmaEruption`, `SpawnIceBurst` and the rest, and the kits
            /// fire them at CAST. The payload drew its own ten cubes and nothing else, which is
            /// most of why the moment felt empty: the particles stopped before the blast landed.
            /// </summary>
            public void Burst(Vector3 at, float radius)
            {
                switch (_style)
                {
                    case ExplosionStyle.Quake: Visual.AbilityVfx.SpawnMagmaEruption(at, radius); break;
                    case ExplosionStyle.Frost: Visual.AbilityVfx.SpawnIceBurst(at, radius); break;
                    case ExplosionStyle.Slipper: break;   // deliberately bare. It is a slipper.
                    default: Visual.AbilityVfx.SpawnCastFlash(at, UiTheme.HeroFire, radius * 0.6f); break;
                }
            }
        }

        private static ExplosionLook LookFor(ExplosionStyle style)
        {
            switch (style)
            {
                case ExplosionStyle.Quake:
                    return new ExplosionLook(style, UiTheme.HeroMagmaCore, new Color(0.55f, 0.40f, 0.28f),
                        "sfx_quake_slam", hasCore: false, debrisCount: 14,
                        debrisSize: new Vector2(0.22f, 0.52f), debrisSpeed: new Vector2(4.0f, 9.0f),
                        debrisLift: 1.1f, debrisLife: 1.1f,
                        flashIntensity: 2.4f, flashSeconds: 0.22f,
                        shakeAmount: 0.85f, shakeSeconds: 0.42f);

                case ExplosionStyle.Frost:
                    return new ExplosionLook(style, UiTheme.HeroIceBright, UiTheme.HeroIce,
                        "sfx_frost_nova", hasCore: true, debrisCount: 16,
                        debrisSize: new Vector2(0.10f, 0.26f), debrisSpeed: new Vector2(8.0f, 15.0f),
                        debrisLift: 1.0f, debrisLife: 0.8f,
                        flashIntensity: 4.2f, flashSeconds: 0.28f,
                        shakeAmount: 0.45f, shakeSeconds: 0.24f);

                case ExplosionStyle.Slipper:
                    return new ExplosionLook(style, new Color(1.0f, 0.93f, 0.72f), new Color(0.95f, 0.80f, 0.45f),
                        "sfx_slipper_burst", hasCore: false, debrisCount: 8,
                        debrisSize: new Vector2(0.10f, 0.22f), debrisSpeed: new Vector2(5.0f, 10.0f),
                        debrisLift: 1.8f, debrisLife: 0.7f,
                        flashIntensity: 1.6f, flashSeconds: 0.14f,
                        shakeAmount: 0.22f, shakeSeconds: 0.16f);

                default:
                    return new ExplosionLook(style, UiTheme.HeroFireBright, UiTheme.HeroFire,
                        "sfx_explosion_heavy", hasCore: true, debrisCount: 12,
                        debrisSize: new Vector2(0.18f, 0.40f), debrisSpeed: new Vector2(7.0f, 15.0f),
                        debrisLift: 1.6f, debrisLife: 0.65f,
                        flashIntensity: 5.5f, flashSeconds: 0.35f,
                        shakeAmount: 0.55f, shakeSeconds: 0.28f);
            }
        }

        public static void CreateExplosion(Vector3 center, float radius, float knockback, float stunTime,
            int sourceSlot, string comicText = "KABOOM!", ISet<int> excludedSlots = null,
            ExplosionStyle style = ExplosionStyle.Fire)
        {
            var round = GameServices.Round;
            if (round == null) return;

            ExplosionLook look = LookFor(style);

            // ⚠️ SEEDED OFF POSITION, for the reason `VfxShapes` gives: two blasts in different
            // places differ from each other, but a given blast is identical between captures and
            // `AbilityShowcaseProbe`'s renders stay comparable version to version.
            int seed = Mathf.RoundToInt((center.x - center.z) * 613.0f);

            // 1. The core. A fireball is a fire thing: a quake has no ball of flame in it and
            //    a slipper has none either, so the sphere is the style's to refuse.
            if (look.HasCore)
            {
                var vfx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                vfx.name = "ExplosionCore";
                vfx.transform.position = center + Vector3.up * 0.6f;
                vfx.transform.localScale = Vector3.one * (radius * 0.4f);

                VfxMaterial.Ghost(vfx.GetComponent<Renderer>(), look.Core, 0.9f);
                VfxMaterial.StripCollider(vfx);

                var anim = vfx.AddComponent<ExplosionVfxAnim>();
                anim.TargetRadius = radius * 1.1f;
                Object.Destroy(vfx, 0.5f);
            }

            // 2. The ground shockwave, and ⚠️⚠️ THIS IS THE LINE THAT MADE THEM ALL ONE PICTURE.
            //    It was a `Cylinder`, so a 2.2 m stomp, a 4.5 m fissure, a 4.8 m supernova and a
            //    thrown tsinelas all drew the same expanding circle in the same fire colour.
            //    `Hero_Strike_Balance.md` § 8.2: silhouette carries WHICH ability this is and it
            //    was the one channel nothing was spending. A `VfxShapes` mesh scales exactly the
            //    way the cylinder did, so `ShockwaveRingAnim` is untouched and no footprint moves.
            //
            //    ⚠️ It also drops a collider nobody wanted. `CreatePrimitive` hands out a
            //    `SphereCollider` and a `CapsuleCollider`, and neither the core nor the ring ever
            //    stripped one, so every blast in the game briefly put two solid bodies in the
            //    street. `VfxShapes.Lay` builds a `MeshFilter` and a `MeshRenderer` and nothing else.
            var shockRing = VfxShapes.Lay(null, "ShockwaveRing", look.Ring(seed), 0.5f, 0.0f);
            shockRing.transform.position = center + Vector3.up * 0.05f;

            VfxMaterial.Ghost(shockRing.GetComponent<Renderer>(), look.Edge, 0.8f);

            var ringAnim = shockRing.AddComponent<ShockwaveRingAnim>();
            ringAnim.TargetRadius = radius * 1.4f;
            Object.Destroy(shockRing, 0.4f);

            // 3. Debris. ⚠️ THE COUNT AND THE SIZE SCALE WITH THE BLAST. Ten cubes at a fixed
            //    size told the player a slipper and a supernova threw the same amount of the
            //    same thing. Rock from a quake is bigger, slower and duller than an ember.
            int shards = Mathf.Clamp(Mathf.RoundToInt(look.DebrisCount * (radius / 3.0f)), 4, 22);
            for (int i = 0; i < shards; i++)
            {
                var spark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spark.name = "ExplosionSpark";
                spark.transform.position = center + Vector3.up * 0.5f;
                spark.transform.localScale = Vector3.one
                    * Random.Range(look.DebrisSize.x, look.DebrisSize.y)
                    * Mathf.Clamp(radius / 3.0f, 0.7f, 1.6f);
                spark.transform.rotation = Random.rotation;

                VfxMaterial.Ghost(spark.GetComponent<Renderer>(), look.DebrisColour(), 0.9f);

                var rb = spark.AddComponent<Rigidbody>();
                rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * look.DebrisLift)
                                    * Random.Range(look.DebrisSpeed.x, look.DebrisSpeed.y);
                rb.angularVelocity = Random.insideUnitSphere * 12.0f;
                Object.Destroy(spark, look.DebrisLife);
            }

            // 4. The flash.
            var lightGo = new GameObject("ExplosionLight");
            lightGo.transform.position = center + Vector3.up * 1.0f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = look.Core;
            light.range = radius * 2.6f;
            light.intensity = look.FlashIntensity;
            Object.Destroy(lightGo, look.FlashSeconds);

            // 5. The elemental particle burst, which already existed per element and was simply
            //    never reached from here. `AbilityVfx` has one for each of these.
            look.Burst(center, radius);

            GameServices.Audio?.PlayAt(look.Cue, center);

            // Comic Popup
            if (!string.IsNullOrEmpty(comicText))
            {
                ComicPopup.Spawn(center, comicText, UiTheme.HeroFireBright, 1.4f);
            }

            // Camera Shake on local rig
            if (UnityEngine.Camera.main != null)
            {
                var rig = UnityEngine.Camera.main.GetComponent<CameraSystem.CameraRig>();
                // ⚠️ THE SHAKE IS THE STYLE'S AND IT SCALES WITH THE BLAST. A flat 0.55 for
                // 0.28 s meant a thrown slipper hit the player's camera exactly as hard as a
                // Supernova, which is the same "one picture at two sizes" fault in the channel
                // the player feels rather than sees. Dante shakes hardest and longest because a
                // quake is the one of the four that is genuinely the ground moving.
                if (rig != null)
                {
                    float scale = Mathf.Clamp(radius / 3.0f, 0.6f, 1.5f);
                    rig.Shake(look.ShakeAmount * scale, look.ShakeSeconds);
                }
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

            // ⚠️ THIS ONE HAD TO BE SEE-THROUGH OR THE ABILITY IS UNPLAYABLE. It encases a
            // PLAYER, so at full opacity the victim spent 2.5 s looking at the inside of a
            // solid box and everyone else lost track of where they were.
            VfxMaterial.Ghost(go.GetComponent<Renderer>(), new Color(0.45f, 0.92f, 1.0f, 0.72f), 0.3f);

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

                    VfxMaterial.Ghost(shard.GetComponent<Renderer>(),
                                      new Color(0.7f, 0.96f, 1.0f, 0.85f));

                    var rb = shard.AddComponent<Rigidbody>();
                    rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.5f) * Random.Range(3.5f, 8.0f);
                    rb.angularVelocity = Random.insideUnitSphere * 25.0f;

                    Object.Destroy(shard, 1.2f);
                }

                // ⚠️ THE SHATTER IS TWELVE FLYING SHARDS AND A SOUND. It does not also need
                // a word, and it fires once per frozen player, so a three-target nova used to
                // print three of them 2.5 s after the cast.
                GameServices.Audio?.PlayAt("sfx_ice_freeze", transform.position);
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

                VfxMaterial.Solid(confetti.GetComponent<Renderer>(), colors[Random.Range(0, colors.Length)]);
                VfxMaterial.StripCollider(confetti);

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

                VfxMaterial.Ghost(seg.GetComponent<Renderer>(), color, 1.0f);

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

                // ⚠️⚠️ FOURTEEN OF THESE COME OUT OF ONE ULTIMATE, EACH WITH A COLLIDER AND A
                // RIGIDBODY, ALL SPAWNED ON TOP OF WHOEVER WAS JUST HIT. Decoration was doing
                // physics to players. Rock is opaque and lit flat; only the collider goes.
                VfxMaterial.Solid(rock.GetComponent<Renderer>(),
                                  Random.value < 0.5f ? new Color(0.22f, 0.18f, 0.15f) : UiTheme.HeroMagmaCore);
                VfxMaterial.StripCollider(rock);

                var rb = rock.AddComponent<Rigidbody>();
                rb.linearVelocity = (Random.insideUnitSphere + Vector3.up * 1.8f) * Random.Range(6.0f, 13.0f);
                rb.angularVelocity = Random.insideUnitSphere * 20.0f;

                Object.Destroy(rock, 1.4f);
            }
        }

        /// <summary>
        /// The fireball: grows fast, fades out.
        ///
        /// ⚠️⚠️ IT NEVER FADED, AND ON THE BIGGEST EFFECT IN THE GAME. Both animators here have
        /// always written a falling alpha into `material.color`, and both materials were the
        /// built-in opaque `Default-Material`, which does not read one. So Sean's Supernova grew
        /// a SOLID sphere to `4.8 * 2.2 = 10.6 m` across, centred one metre in front of the
        /// player who cast it, and then vanished at full brightness half a second later. From
        /// inside, the ultimate was an orange screen. `VfxMaterial.Ghost` at the spawn site is
        /// what makes these two lines do the thing they say.
        ///
        /// ⚠️ THE SCALE CURVE IS `Sqrt(t)`, WHICH IS THE HALF OF THIS THAT WAS ALWAYS RIGHT. A
        /// blast that expands linearly reads as a balloon; one that expands fastest at the start
        /// reads as a detonation. Do not "simplify" it to `t`.
        /// </summary>
        private sealed class ExplosionVfxAnim : MonoBehaviour
        {
            public float TargetRadius = 5.0f;
            private readonly Fader _fade = new Fader();
            private float _elapsed;

            private void Update()
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / 0.5f);
                transform.localScale = Vector3.one * Mathf.Lerp(1.0f, TargetRadius * 2.0f, Mathf.Sqrt(t));

                _fade.Apply(GetComponent<Renderer>(), Mathf.Lerp(0.85f, 0.0f, t));
            }
        }

        private sealed class ShockwaveRingAnim : MonoBehaviour
        {
            public float TargetRadius = 6.0f;
            private readonly Fader _fade = new Fader();
            private float _elapsed;

            private void Update()
            {
                _elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsed / 0.4f);
                float r = Mathf.Lerp(0.5f, TargetRadius * 2.0f, t);
                transform.localScale = new Vector3(r, 0.02f, r);

                _fade.Apply(GetComponent<Renderer>(), Mathf.Lerp(0.8f, 0.0f, t));
            }
        }

        /// <summary>
        /// Drops a renderer's alpha, and takes its glow down with it.
        ///
        /// ⚠️ THE EMISSION HAS TO FALL TOO, AND IT HAS TO FALL FROM A REMEMBERED BASE.
        /// `VfxMaterial` lights these flatly through `_EmissionColor` so a blast is not shaded
        /// by the arena's key light, and emission is ADDED after the blend: an effect faded to
        /// alpha 0 with its glow left at full still deposits its own colour on the frame, so the
        /// ring reaches zero opacity and stays visible.
        ///
        /// ⚠️⚠️ AND THE BASE IS CAPTURED ONCE RATHER THAN READ BACK EACH FRAME. Reading the
        /// current emission and scaling it by the current alpha COMPOUNDS: at 60 fps a half
        /// second fade multiplies by 0.9-ish thirty times over and the glow is gone in four
        /// frames, which is a pop rather than a fade and looks like the effect was destroyed
        /// early.
        /// </summary>
        /// <summary>
        /// A zone's rim breathes while it is live, and thins out as it runs out.
        ///
        /// ⚠️⚠️ THE ZONES WERE STATIC PLATES AND THAT IS MOST OF WHY THEY LOOK CHEAP.
        /// `docs/Hero_Strike_Balance.md` § 8.2 lists four channels an effect has, and MOTION is
        /// the one that carries *whether it is live or spent*. Every hazard in the game spent
        /// exactly none of it: `SpawnFireTrail`, `SpawnShockTrail` and `SpawnIceSheet` build
        /// their meshes once and never touch them again, so a 3 s zone at 0.1 s remaining is
        /// pixel-identical to one that just landed. 🧑: the skills *"feel repetitive or too
        /// simple, or too empty"*.
        ///
        /// ⚠️ THE PULSE IS SMALL ON PURPOSE, AND IT IS ON THE RIM ONLY. `VISION.md` § 2 rule 3
        /// spends the readability budget on detail rather than area, and rule 5 says a mid-fight
        /// frame must still show the lata, the chalk and every player. A zone that throbs hard
        /// enough to notice on its own is a zone competing with the ball. 16 per cent at 1.6 Hz
        /// on the EDGE reads as heat or charge without the core moving at all.
        ///
        /// ⚠️ AND THE DYING RAMP IS THE GAMEPLAY HALF. It is the read § 8.5 item 2 asks for:
        /// *"a player cannot tell whether the ice they are about to run across is spent."* The
        /// last 30 per cent of the life fades the rim toward nothing, so committing to a
        /// crossing becomes a decision made on what is on screen instead of on a count kept in
        /// the player's head. ⚠️ It fades the RIM rather than the whole effect, exactly as that
        /// item specifies: the char and the danger area stay put, because the hazard is still
        /// live right up to the moment it is not.
        /// </summary>
        private sealed class HazardRimLife : MonoBehaviour
        {
            public float Duration = 3.0f;
            public float BaseAlpha = 0.45f;
            public float PulseAmount = 0.16f;
            public float PulseHz = 1.6f;

            /// <summary>Fraction of the life after which the rim starts going.</summary>
            private const float FadeFrom = 0.70f;

            private readonly Fader _fade = new Fader();
            private Renderer _renderer;
            private float _elapsed;

            private void Awake() => _renderer = GetComponent<Renderer>();

            private void Update()
            {
                if (_renderer == null) return;

                _elapsed += Time.deltaTime;

                float pulse = 1.0f + PulseAmount * Mathf.Sin(_elapsed * PulseHz * Mathf.PI * 2.0f);

                float life = Duration > 0.001f ? Mathf.Clamp01(_elapsed / Duration) : 1.0f;
                float dying = life < FadeFrom ? 1.0f : Mathf.InverseLerp(1.0f, FadeFrom, life);

                _fade.Apply(_renderer, Mathf.Clamp01(BaseAlpha * pulse * dying));
            }
        }

        private sealed class Fader
        {
            private Color _baseEmission;
            private bool _captured;

            public void Apply(Renderer target, float alpha)
            {
                if (target == null) return;

                var material = target.material;
                bool glows = material.HasProperty("_EmissionColor");

                if (!_captured)
                {
                    if (glows) _baseEmission = material.GetColor("_EmissionColor");
                    _captured = true;
                }

                float a = Mathf.Clamp01(alpha);

                var colour = material.color;
                colour.a = a;
                material.color = colour;

                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);

                if (glows)
                    material.SetColor("_EmissionColor",
                        new Color(_baseEmission.r, _baseEmission.g, _baseEmission.b, 1.0f) * a);
            }
        }
    }
}
