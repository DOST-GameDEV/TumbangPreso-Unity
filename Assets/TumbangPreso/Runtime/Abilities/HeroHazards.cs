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

            // ⚠️⚠️ A WALL GOING UP PLAYED THE SOUND OF SOMETHING BREAKING, AND SO DID THE
            // SHEET. `ability_shatter_trap` was on BOTH of Cheska's ground powers, so two
            // different abilities shared one cue and that cue is a shatter fired at the moment
            // something is BUILT. This is the fault `tools/generate_ability_audio.py` was written
            // for (`ability_bagsak_bomb` on four callers, `ability_flick_dash` on a lightning
            // strike) surviving in the one kit that pass did not reach.
            //
            // ⚠️ `sfx_barricade_raise` ARRIVES AND STOPS, which is the gameplay half: three
            // pillars are a solid object that is now in the way, and the hard lock at the end of
            // the cue is the frame it becomes true. The sheet gets a rising shimmer instead,
            // because a sheet spreads and a wall lands.
            GameServices.Audio?.PlayAt("sfx_barricade_raise", position);

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

                // ⚠️⚠️ THIS PLAYED `slipper_land`: A RUBBER SANDAL HITTING THE ROAD, for a wall
                // of ice failing and coming down in twelve pieces. It is the single worst cue
                // mismatch left in the game and it is in the one place in Cheska's kit where ice
                // genuinely does break.
                ComicPopup.Freeze(transform.position);
                GameServices.Audio?.PlayAt("sfx_ice_shatter", transform.position);
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
            // ⚠️⚠️ IT IS A SLAB WITH WALLS NOW, NOT A HEXAGON PAINTED ON THE ROAD.
            // `ability_ice_sheet_v11.png` is what forced this: the outline work of § 8 landed and
            // the sheet still renders as a pale blue plate with five cubes standing on it,
            // because a `Crystal` handed to `Fan` and dropped by `Lay` at y = 0.01 IS a plate.
            // 🧑 named the cause from play: *"the same logic and code was used to generate all of
            // them"*. `VfxShapes.Prism` is the answer for this one: the same six-sided footprint,
            // extruded, so the ice has a top face and six sloping walls that take the key light
            // at three different angles.
            //
            // ⚠️ 0.26 m OF THICKNESS AND NOT MORE. It has to read from eye height and stay
            // something a player can see ACROSS while deciding whether to cross it, which is the
            // constraint the spikes below are already held to. A quarter of a metre is a kerb.
            //
            // ⚠️ `Stand`, NEVER `Lay`. `Lay` leaves the Y scale at 1.0, which is right for a flat
            // fan and silently wrong for anything with height; `docs/TODO.md` § 15.5 records the
            // 2 m ball that shipped from exactly that mistake.
            int seed = Mathf.RoundToInt((position.x * 5.0f + position.z) * 733.0f);

            var visual = VfxShapes.Stand(go.transform, "IceSlab",
                                         VfxShapes.Prism(6, 1.0f, 0.80f, 0.0f, 0.26f, seed),
                                         radius, heightScale: 0.26f, lift: 0.01f);

            // ⚠️ HER MOTIF: THE ICE KEEPS GOING. A slab says a patch froze; cracks running out of
            // it say the freeze PROPAGATED, which is what ice actually does and what separates
            // her from a hero who places a shape. Hairlines outside the hazard, so nothing about
            // the danger radius changes. See `SpawnFrostCracks` for the bound.
            SpawnFrostCracks(go.transform, radius, seed + 17);

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
            // ⚠️ 0.42 AND LIGHTER, AFTER `ability_ice_sheet_v13.png` CAME BACK TEAL. The
            // warning above is about a 0.65 alpha surface under a 2.5-intensity light clipping to
            // pure white, and both halves of that pair are gone: the light is 0.9 now and sits
            // 1.5 m up. What was left was the opposite fault, a thin blue wash over dark asphalt
            // reading as a puddle of dishwater. The gate has the headroom to say so: this frame
            // measures 1.9 per cent blown against a 12 per cent bound.
            VfxMaterial.Ghost(visual.GetComponent<Renderer>(), new Color(0.55f, 0.92f, 1.0f, 0.42f), 0.10f);

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
            // ⚠️⚠️ THE RIM IS A COLLAR AROUND THE SLAB, NOT A SECOND PLATE UNDER IT. It used
            // to be another flat `Crystal` at 1.07 radius, so the sheet was two translucent
            // discs stacked, which is the overlap `docs/VISION.md` § 2 rule 4 caps and which the
            // note below this one already argued the effect down from five layers to three.
            // A low prism ringing the slab does the same job with geometry the eye can resolve:
            // the edge of the danger is a raised LINE at ankle height.
            var rim = VfxShapes.Stand(go.transform, "FrostRim",
                                      VfxShapes.Collar(6, 1.0f, 0.90f, 0.0f, 0.26f, seed + 1),
                                      radius * 1.06f, heightScale: 0.11f, lift: 0.005f);
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

                // ⚠⚠ A TAPERED FIVE-SIDED SHARD, NOT A CUBE. The cubes are visible in
                // `ability_ice_sheet_v11.png` and they read as five blue BOXES sitting on a
                // plate, because that is what they are: `PrimitiveType.Cube` is the same
                // primitive Dante's debris, Sean's embers and Nemu's void shards are made of, so
                // four different fictions were sharing one lump of geometry. A `Prism` pulled in
                // hard at the top is a spike, it is built from the same builder as the slab
                // underneath it, and it costs one mesh.
                float shardH = s == 0 ? 0.62f : Random.Range(0.34f, 0.48f);

                var spike = VfxShapes.Stand(go.transform, $"FrostSpike_{s}",
                                            VfxShapes.Prism(5, 1.0f, 0.16f,
                                                            0.22f, 0.4f, seed + 7 + s),
                                            0.17f, heightScale: shardH);
                // ⚠️ THEY STAND ON THE SLAB, NOT IN IT. At 0.14 m the feet were buried inside a
                // 0.26 m slab, and a solid shard seen THROUGH a translucent blue surface is
                // darkened by it: `ability_ice_sheet_v12.png` renders five near-navy specks on
                // bright cyan, which is the opposite of the contrast they exist for.
                spike.transform.localPosition = new Vector3(Mathf.Cos(ang) * rr,
                                                            0.25f,
                                                            Mathf.Sin(ang) * rr);
                spike.transform.localRotation = Quaternion.Euler(Random.Range(-14.0f, 14.0f),
                                                                 s * 72.0f,
                                                                 Random.Range(-14.0f, 14.0f));
                // ⚠️ THE SPIKES WERE 0.16 m ACROSS AND VANISHED. In the v1 render they read as
                // five grey specks on a white plate, which is worse than no detail at all
                // because it costs five renderers to draw nothing. Two and a half times the
                // footprint and roughly double the height is what makes a silhouette at the
                // 10 m the arena is actually read across, and they stay under knee height.
                // ⚠️ 0.35 EMISSION, BECAUSE `Solid` IS SCENE-LIT AND HALF THE COURT IS IN
                // SHADOW. In `ability_ice_sheet_v13.png` the shards read as dark slate cones on
                // bright ice, which is the contrast backwards: they are the part that is supposed
                // to catch the light. `VfxMaterial`'s own note makes this argument for the ghost
                // material (*"a frost sheet shaded by the scene's key light goes dark on the
                // shadowed half of the court, which is exactly where a player most needs to see
                // it"*) and the solid path needs it just as much.
                VfxMaterial.Solid(spike.GetComponent<Renderer>(),
                                  new Color(0.72f, 0.92f, 1.0f), 0.35f);
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

            // Ice SPREADING, not ice breaking. See the barricade's note above for what these
            // two were sharing and why sharing it was backwards.
            GameServices.Audio?.PlayAt("sfx_ice_form", position);

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
                    // ⚠️⚠️ EVERY ZONE IN THIS FILE USED TO DIE IN SILENCE.
                    // `Hero_Strike_Balance.md` § 8.5 item 2 argues that a player who cannot tell
                    // a spent effect from a live one has lost a real gameplay read and that
                    // fixing it is free. That argument was applied to the VISUALS (the rims
                    // pulse, the auras thin) and never to the audio, which is the channel a
                    // player still has while they are looking somewhere else. This is a slip
                    // zone somebody is standing at the edge of deciding whether to cross; the
                    // moment it stops being dangerous is worth a sound.
                    //
                    // ⚠️ AND IT IS THE TRAILS THAT GET NOTHING, DELIBERATELY. One dash drops up
                    // to thirty marks and each lives 3 s, so trail expiry cues would be thirty
                    // overlapping tails inside three seconds. Same measurement `AbilityVfx` uses
                    // to keep emitters off trails. Singular zones only.
                    GameServices.Audio?.PlayAt("sfx_ice_thaw", transform.position);
                    Object.Destroy(gameObject);
                    return;
                }

                // Slow rotation on the ice zone
                transform.Rotate(Vector3.up, 20.0f * Time.deltaTime);

                if (!NetAuthority.ShouldResolve()) return;
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

                                // ⚠️⚠️ `NetCue`, BECAUSE THIS LINE IS NOW BEHIND A HOST GATE.
                                // Making the zone host-authoritative (`docs/TODO.md` § 38) put
                                // every hazard's on-hit decision on one machine, which is right,
                                // and silently made its SOUND host-only, which is not.
                                // `tools/audit_audio_reach.py` reported all three the moment the
                                // gates landed. Same split `Carrier.HostThrowAt` records: only
                                // the host may DECIDE, and announcing is a separate job.
                                NetCue.Play("ability_shatter_trap", p.transform.position);
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
            // Opaque, for the reason written up on the fire trail's char: a burnt mark does not
            // show the road through it, and an opaque surface cannot lose a sort to the bright
            // rim standing on it.
            VfxMaterial.Solid(visual.GetComponent<Renderer>(), new Color(0.14f, 0.12f, 0.03f), 0.0f);
            VfxMaterial.StripCollider(visual);

            // ⚠️⚠️ A BROKEN RIM, NOT A FULL BRIGHT PLATE, AND THE PLATE IS THE OTHER HALF OF
            // THE CORRIDOR PROBLEM. This was a second `Star` fan at the FULL radius in near-white
            // yellow at 0.55 alpha, drawn over the dark scorch. One of them is a hot discharge
            // mark; six of them, which is what a 2.5 s sprint lays, is the yellow carpet
            // `VISION.md` § 2 measures as 27.2 per cent of the box off a 6 s cooldown, and that
            // is more floor than any ultimate in the game.
            //
            // ⚠️ THE DARK SCORCH ABOVE KEEPS THE STAR SILHOUETTE, so nothing about which ability
            // this is changes: the outline work of `Hero_Strike_Balance.md` § 8.3 is carried by
            // the mark, not by the glow. What goes is the bright FILL, replaced by twelve short
            // plates at the rim. A player still sees a yellow discharge edge and the road
            // underneath it survives.
            //
            // ⚠️ AND THE BRIGHTNESS MOVES TO THE ARC, WHICH IS WHERE IT BELONGS. `ArcFlicker`
            // stands a live bolt at the anchor; that is a thing you can see is dangerous from the
            // side, which a plate on the ground cannot be at eye height however bright it is.
            var ring = VfxShapes.Lay(go.transform, "ShockRing",
                                     VfxShapes.Wedges(12, 0.78f, 9.0f, 0.0f, 0.10f, seed + 2),
                                     radius, 0.010f);
            // Same clipping fault and same fix as the fire trail's rim: 1.15 emission wrote
            // past white and `ability_shock_trail_v1.png` came back as one flat yellow coin with
            // the arc invisible on top of it.
            // ⚠️ DIMMED AGAIN AFTER THE CORRIDOR SHOT. `ability_corridors_v14.png` shows six of
            // these along one dash and the rims read as a scatter of yellow confetti: at 0.55
            // alpha and 0.32 emission a single rim is right and six overlapping ones are the
            // carpet this pass exists to remove, one step less bright.
            VfxMaterial.Ghost(ring.GetComponent<Renderer>(), new Color(1.0f, 0.90f, 0.20f, 0.40f), 0.22f);

            // ⚠️ NO `HazardRimLife` HERE EITHER, and for the same reason as the fire trail: this
            // is the other per-disc trail, and Zack's corridor is the widest in the game.
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

        /// <summary>
        /// Shrink a trail mark toward its own end, so a corridor of them reads back in time.
        ///
        /// ⚠️⚠️ IT IS WHAT MAKES SIX DROPS STOP LOOKING LIKE ONE DROP SIX TIMES. Every mark in a
        /// dash is built from the same call with the same constants, seeded off its position, so
        /// the only difference between them is a few degrees of outline. `ability_corridors_v13`
        /// and `_v14` both show that: a row of identical marks, which is the repetition 🧑 has
        /// reported three separate times. Age is the one axis that differs between them for free,
        /// and using it means the corridor POINTS: the small end is where the caster started.
        ///
        /// ⚠️⚠️ A SHRINK IS NOT THE PULSE THIS FILE ALREADY REFUSED, AND THE DIFFERENCE MATTERS.
        /// `SpawnFireTrail` records why `HazardRimLife` was taken off the trails: thirty rims
        /// throbbing out of phase along a corridor is visual noise, and thirty per-frame MATERIAL
        /// writes on top of it. This is monotone and it is one transform write: no oscillation,
        /// nothing to fall out of phase with, no allocation, and it is information rather than
        /// decoration. The pulse stays on the zones.
        ///
        /// ⚠️ IT SHRINKS THE MARK, NOT THE HAZARD. `Radius` is untouched and every component here
        /// resolves contact against that field, so what a player is standing in is exactly what
        /// `Hero_Strike_Balance.md` § 1 measures. A telegraph that lies is worse than no
        /// telegraph, so this only ever draws the mark SMALLER than the danger, never larger.
        /// </summary>
        private static void Burn(Transform mark, float left, float duration)
        {
            if (mark == null || duration <= 0.0f) return;

            float s = Mathf.Lerp(0.66f, 1.0f, Mathf.Clamp01(left / duration));
            mark.localScale = new Vector3(s, s, s);
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

                Burn(transform, _left, Duration);

                if (!NetAuthority.ShouldResolve()) return;
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

            // ⚠️ HIS MOTIF: IT IS STILL EATING. `docs/TODO.md` § 27.1. A trail drop is otherwise
            // a mark that appears whole and shrinks; separate burning pieces at falling density
            // outside its edge say the fire got there and has not finished, which is the only
            // thing that gives one drop an AGE distinct from the drop beside it.
            //
            // ⚠️ AND IT PUTS NO AREA BACK. His corridor was the worst offender ever measured in
            // this game at 27.2 per cent of the box (`docs/VISION.md` § 2), and § 19.3 took a
            // full-radius bright plate out of every drop to fix it. These are pieces with gaps:
            // about 9 per cent of the ring they are scattered in. See `SpawnCinderFringe`.
            SpawnCinderFringe(go.transform, radius, seed + 53);
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
            // ⚠️⚠️ OPAQUE, AND THE ALPHA WAS NOT A LOOK, IT WAS A SORTING BUG. Read the note
            // above: it argues at length for 0.93 alpha over any lower value, and it ends with
            // *"A SCORCH IS OPAQUE ANYWAY. Burnt asphalt does not show the road through it."*
            // That last line was right and the code stopped one step short of it.
            //
            // ⚠️⚠️ WHAT 0.92 ALPHA ACTUALLY COST: TWO COPLANAR TRANSLUCENT PLATES SORT
            // ARBITRARILY, SO THE MARK RENDERED A DIFFERENT COLOUR PER DROP. Unity orders
            // transparent renderers by the distance from the camera to each one's bounds centre.
            // The char and the bright rim under it are concentric and 5 mm apart, so their
            // centres are the same point to within rounding and the comparison is effectively a
            // coin toss. `ability_worstframe_v11.png` is the proof and it had been on disk
            // unexplained: six drops of ONE trail, alternating dark brown and bright salmon, from
            // one call with one set of constants. Nothing was wrong with either colour.
            //
            // ⚠️ AN OPAQUE MATERIAL CANNOT LOSE THAT ARGUMENT, WHICH IS THE REAL FIX. It renders
            // in the geometry queue and writes depth, so it occludes whatever is beneath it by
            // construction rather than by winning a sort. The rule this file now follows: ground
            // that has been BURNT or BROKEN is opaque, and only things you can genuinely see
            // through are ghosted.
            // ⚠️ BURNT ASPHALT, NOT A HOLE. At (0.15, 0.06, 0.03) and opaque this read as pure
            // black in `ability_fire_trail_v13.png`, so the mark looked like a gap in the road
            // with an orange ring round it. Scorched tarmac is a dark warm GREY; the road it sits
            // on is already dark, so the mark only has to be darker than that, not absent.
            VfxMaterial.Solid(visual.GetComponent<Renderer>(), new Color(0.25f, 0.19f, 0.16f), 0.0f);
            VfxMaterial.StripCollider(visual);

            // ⚠️⚠️ THE BRIGHT LOZENGE PLATE IS DELETED AND FLAMES REPLACE IT. THIS IS THE
            // SINGLE UGLIEST OBJECT IN HERO STRIKE AND IT TOOK A CORRIDOR RENDER TO SEE IT.
            // What stood here was a second `Streak` fan at `radius * 0.86` by `radius * 1.66`,
            // full orange at 0.42 alpha, drawn UNDER the char. On its own it is a hot perimeter.
            // Six of them in a row, which is what a dash actually lays, is
            // `ability_worstframe_v11.png`: a chain of flat salmon LOZENGES down the middle of
            // the street that reads as a row of leaves, and it is the widest painted thing in the
            // game.
            //
            // ⚠️⚠️ THE FAULT IS THE AXIS, NOT THE COLOUR OR THE ALPHA, AND THAT IS WHY THREE
            // EARLIER PASSES ON THIS EFFECT DID NOT FIX IT. The note above records darkening the
            // colour twice and cutting the light twice against `_v1` through `_v3`. Every one of
            // those was still a plate. Fire is the one fiction in the game that does not lie on
            // the ground, and a ground decal cannot use the only direction it has.
            //
            // ⚠️ IT ALSO PAYS FOR ITSELF IN FOOTPRINT, WHICH IS THE OTHER HALF. `VISION.md` § 2
            // measures Sean's and Zack's corridors as the two worst offenders in the game at 27.2
            // per cent of the box, and the arithmetic there is per-disc area times the drop count.
            // The plate was the largest disc in each drop, at 1.66 by 0.86 of the radius against
            // the char's 1.28 by 0.62. Deleting it takes roughly 40 per cent of the painted area
            // out of every drop Sean makes, and buys the budget the flames spend on detail, which
            // is `VISION.md` § 2 rule 3 exactly.
            //
            // ⚠️ THREE, NOT FIVE, AND UNDER HALF A METRE. Six drops live at once during a dash,
            // so anything on this object is multiplied by six before a second hero casts
            // anything. Three tongues at 0.55 m are visible from eye height and still let a
            // player see a body standing behind the mark, which is `ArcFlicker`'s knee-height
            // ceiling applied to the other trail.
            for (int t = 0; t < 3; t++)
            {
                // ⚠️ TALLER AND SPREAD ALONG THE RUN. The flames are meant to be the READ and at
                // 0.38 m they were smaller than the rim around them, so the effect still resolved
                // as a ring. They also sat in a circle, which fights the streak: a dash mark
                // points, so the fire on it should be strung out along the direction of travel.
                float ta = t * 120.0f + Random.Range(-24.0f, 24.0f);
                float tr = radius * Random.Range(0.10f, 0.44f);
                float th = Random.Range(0.52f, 0.78f);

                var flame = VfxShapes.Stand(go.transform, $"FireTongue_{t}",
                                            VfxShapes.Tongue(5, 0.34f,
                                                             Random.Range(0.16f, 0.38f),
                                                             Random.Range(0.35f, 0.75f),
                                                             0.18f, seed + 11 + t),
                                            radius * 0.30f, heightScale: th,
                                            yaw: ta);
                flame.transform.localPosition = new Vector3(
                    Mathf.Cos(ta * Mathf.Deg2Rad) * tr * 0.55f,
                    0.0f,
                    Mathf.Sin(ta * Mathf.Deg2Rad) * tr * 1.45f);

                // ⚠️ GHOSTED AND LOW-EMISSION, BECAUSE THREE OF THESE STACK ON EVERY DROP AND
                // SIX DROPS OVERLAP. The rim notes in this file record 1.05 emission clipping a
                // ring to a flat yellow donut; a flame is thin enough that it does not need the
                // help, and `AbilityShowcaseProbe`'s 12 per cent gate is measured on frames where
                // a whole corridor is live at once.
                VfxMaterial.Ghost(flame.GetComponent<Renderer>(),
                                  new Color(1.0f, Random.Range(0.42f, 0.66f), 0.10f, 0.72f), 0.20f);
            }

            // A thin hot lip at the very edge of the char, which is where a real fire is. It is
            // what the deleted plate was FOR, at a fraction of the area: a RING rather than a
            // disc, standing a few centimetres proud so it cannot argue with the char about
            // sort order the way the plate did.
            // ⚠️ THIN. At an 0.80 inner ratio this was a fat orange band that became the whole
            // effect: `ability_fire_trail_v13.png` reads as an ORANGE RING, which is one more
            // repeated outline and exactly the complaint this pass is answering. 0.90 is a lip.
            var edge = VfxShapes.Stand(go.transform, "FireEdge",
                                       VfxShapes.Collar(12, 1.0f, 0.90f, 0.14f, 0.0f, seed + 1),
                                       1.0f, heightScale: 0.04f, lift: 0.012f, yaw: yaw);
            edge.transform.localScale = new Vector3(radius * 0.64f, 0.04f, radius * 1.30f);
            // ⚠️ EMISSION 1.05 CLIPPED THE RIM TO A FLAT YELLOW DONUT. Measured off
            // `ability_fire_trail_v1.png`: the ring came back as one solid band with no shading
            // and no hue left in it, because anything over about 0.5 here writes past white
            // before `ColourGrade` ever sees the frame. 0.30 keeps it hot and keeps it ORANGE.
            VfxMaterial.Ghost(edge.GetComponent<Renderer>(), new Color(1.0f, 0.42f, 0.07f, 0.62f), 0.22f);
            VfxMaterial.StripCollider(edge);

            // ⚠️⚠️ NO `HazardRimLife` ON A TRAIL DISC, AND I PUT ONE HERE BEFORE REMOVING IT.
            // `AbilityVfx` already states the rule for this exact object: *"a dashing hero drops
            // a trail disc every 0.10 s and each lives 3 s, so ONE dash leaves up to thirty live
            // objects. Thirty looping ParticleSystems is a different kind of bug from the one
            // this feature is for. Zone hazards are singular and get one each; trails get none."*
            //
            // A per-frame material write is not a ParticleSystem, but it is the same shape of
            // mistake: thirty `Update` calls each writing colour and emission, and thirty rims
            // throbbing out of phase along a corridor, which is visual noise in the one place
            // `VISION.md` § 2 already measures as the worst offender at 27.2 per cent of the box.
            //
            // ⚠️ The expiry read is also worth least here. A trail disc lives 3 s and a player
            // runs THROUGH it; the ice sheet is a zone somebody stands at the edge of deciding
            // whether to cross, which is the case § 8.5 item 2 actually names. The pulse stays
            // on the sheet and off the trails.

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
            // ⚠️ FIVE EMBERS DOWN TO TWO, BECAUSE THE TONGUES ABOVE NOW CARRY THE VERTICAL
            // READ AND THE EMBERS WERE THE ONLY THING DOING IT BEFORE. Two is enough to say the
            // mark is throwing sparks; five plus three flames plus a char plus a rim is
            // `VISION.md` § 2 rule 4 broken on a single trail disc, six of which are live at once.
            for (int f = 0; f < 2; f++)
            {
                var ember = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ember.name = $"Ember_{f}";
                ember.transform.SetParent(go.transform, false);

                float fa = f * 180.0f * Mathf.Deg2Rad + 0.5f;
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

                Burn(transform, _left, Duration);

                if (!NetAuthority.ShouldResolve()) return;
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
                        if (!NetAuthority.ShouldResolve()) return;

                        // ⚠️ 6, THE DEFAULT, AND DELIBERATELY UNREMARKABLE. The
                        // poltergeist is the middle of the range this scale is measured
                        // against: heavier than Sean shrugging off a burn, lighter than
                        // standing inside a nova.
                        _target.ApplyStagger(1.8f, StunElement.Void, 6);
                        _target.ApplyImpulse(Random.onUnitSphere * 4.0f);
                        DizzyStars.Attach(_target.transform, 1.8f, UiTheme.HeroSpiritBright);
                        ComicPopup.Boo(_target.transform.position);

                        // ⚠️ `NetCue` FOR THE REASON THE ICE SHEET RECORDS: this sits behind the
                        // host gate three lines up, so three of the four players could not hear
                        // the poltergeist connect.
                        NetCue.Play("downed", transform.position);
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

            // ⚠️⚠️ DARK CRUST WITH REAL GAPS OVER A HOT BED, AND THE CRACKS ARE NOW HOLES RATHER
            // THAN DECORATION. This effect is named `SpawnCrackedLavaDecal` and it was a single
            // continuous `Splat` fan with seven cube "seams" laid on top of it: the geometry said
            // intact plate and the decoration argued with it, which is why the seams read as
            // orange STICKS in `ability_lava_decal_v11.png` rather than as light coming up
            // through a break.
            //
            // ⚠️⚠️ TWO LAYERS IN THE RIGHT ORDER IS THE WHOLE TECHNIQUE. A hot plate underneath,
            // then broken crust plates over it with gaps between them. The bright pixels are
            // exactly the gaps, so heat is a small fraction of the footprint by CONSTRUCTION
            // instead of by picking a dark colour and hoping, and every plate the crust does
            // cover is genuinely dark. `docs/VISION.md` § 2 rule 3 asks for the budget to go on
            // detail rather than area; this spends none of it on area at all.
            //
            // ⚠️ THE BED IS SMALLER THAN THE CRUST so no ring of raw glow escapes past the outer
            // edge. Nine plates, not eleven sides: `VfxShapes.Wedges` counts PIECES where
            // `Splat` counted corners, and nine pieces with a 9 degree gap is a fracture a player
            // can resolve at 10 m.
            // ⚠️⚠️ THE BED IS WELL INSIDE THE CRUST AND THE CRUST IS OPAQUE, AND THE FIRST
            // ATTEMPT AT THIS GOT BOTH WRONG. In `ability_lava_decal_v12.png` the effect is a
            // flat ORANGE octagon with a few dark chips around the outside, which is the exact
            // inverse of what the two layers are for. Two causes, and the second is the
            // interesting one:
            //  * the bed was at 0.92 of the radius, so it very nearly reached the outer edge and
            //    there was hardly any crust left to cover it;
            //  * both layers were ghosted, and two coplanar translucent plates sort arbitrarily,
            //    so the bright bed was drawn OVER the dark plates that are supposed to hide it.
            //    The fire trail's char note has the full account: it is the same fault and it had
            //    already produced a visible defect nobody had explained.
            //
            // ⚠️ SO THE CRUST IS OPAQUE. Burnt asphalt is opaque, an opaque surface writes depth,
            // and depth cannot be out-sorted. The bright pixels are now exactly the gaps between
            // the plates, which is what the whole two-layer idea was for.
            // ⚠️ HIS MOTIF: THE GROUND WENT SOMEWHERE. `docs/TODO.md` § 27.4. Everything else in
            // this game leaves the street exactly as it found it; displacement is the one element
            // whose real signature is that you can see where the fight was afterwards. The slabs
            // lean OUT from the rim, so they never stand in the middle a player walks through.
            SpawnUpheaval(go.transform, radius, seed + 29);

            var bed = VfxShapes.Lay(go.transform, "MagmaBed",
                                    VfxShapes.Splat(11, 0.22f, seed + 3),
                                    radius * 0.74f, 0.010f);
            VfxMaterial.Ghost(bed.GetComponent<Renderer>(), new Color(1.0f, 0.34f, 0.04f, 0.90f), 0.30f);
            VfxMaterial.StripCollider(bed);

            var outer = VfxShapes.Lay(go.transform, "CrackedAsphalt",
                                      VfxShapes.Wedges(13, 0.22f, 4.0f, 0.075f, 0.26f, seed),
                                      radius, 0.022f);
            VfxMaterial.Solid(outer.GetComponent<Renderer>(), new Color(0.19f, 0.15f, 0.13f), 0.0f);
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
            // ⚠️⚠️ SEVEN CUBE SEAMS AND A CYLINDER CORE DELETED. The seams were the previous
            // answer to the same complaint and they were drawing the cracks as OBJECTS: seven
            // stretched `PrimitiveType.Cube`s standing on an unbroken plate, plus a `Cylinder`
            // for a hot middle. Nine renderers, nine materials, nine colliders to strip, and it
            // still read as a coin with sticks on it because the plate underneath was never
            // actually broken. The gap between two `Wedges` plates is the crack, and it is free.
            //
            // ⚠️ THE CENTRE IS NOW THE BED SHOWING THROUGH `innerRatio`. `Wedges` leaves the
            // inner 14 per cent of the radius open by construction, so the hot plate is visible
            // in the middle exactly where the foot landed. That is what the deleted cylinder was
            // for, and it costs nothing.

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

            // ⚠️ THIS ZONE HAD NO COMPONENT AT ALL, so there was nowhere to hang an ending on:
            // `Object.Destroy(go, duration)` is a deletion, not an event. `ExpiryCue` is the
            // smallest thing that turns one into the other, and it keeps the deletion in the
            // same place rather than splitting the lifetime across two mechanisms.
            var cue = go.AddComponent<ExpiryCue>();
            cue.Cue = "sfx_magma_cool";
            cue.Seconds = duration;

            return go;
        }

        /// <summary>
        /// Counts a zone down, plays one cue as it goes, and takes the object with it.
        ///
        /// ⚠️ IT REPLACES AN `Object.Destroy(go, t)`, NOT A COMPONENT. Every other hazard here
        /// already owns a `MonoBehaviour` that ticks `_left` and can therefore say something on
        /// the way out; the cracked lava decal was pure decoration with a timed delete, which is
        /// why it was the only zone with no possible ending. Adding the tick here rather than
        /// giving it a full component keeps the difference honest: it still does nothing but
        /// expire.
        /// </summary>
        public sealed class ExpiryCue : MonoBehaviour
        {
            public string Cue;
            public float Seconds = 4.0f;

            private float _left;

            private void Start() => _left = Seconds;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left > 0.0f) return;

                if (!string.IsNullOrEmpty(Cue))
                    GameServices.Audio?.PlayAt(Cue, transform.position);

                Object.Destroy(gameObject);
            }
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

            // ⚠️⚠️ ONE FUNNEL, NOT THREE STACKED DISCS, AND THE THREE DISCS ARE WHY THIS READ AS
            // A PANCAKE. What stood here was a mouth, a throat and a lip, each a
            // `PrimitiveType.Cylinder` scaled flat and offset a centimetre from the last. The
            // comment they carried said *"two steps down reads as a funnel where one step reads
            // as a lid"*, and `ability_seance_void_v11.png` says otherwise in one frame: three
            // discs of decreasing size stack into ONE lilac plate with a darker ellipse painted
            // in the middle. Concentric flat rings are a target, not a hole. No arrangement of
            // discs is a funnel, because every one of them is parallel to the road.
            //
            // ⚠️⚠️ 🧑, 2026-08-26, naming the class this belongs to: *"the same logic and code was
            // used to generate all of them"*. This is the purest case of it in the file. Four
            // objects, four scaled cylinders, and the effect that is supposed to be a tear in the
            // world was built out of exactly the primitive the § 8 silhouette pass was written to
            // get rid of, in the one ability that never got the pass.
            //
            // `VfxShapes.Funnel` is a surface with a vertical PROFILE: unit radius at the lip,
            // deepest at the centre, falling on a power curve so the wall is near vertical at the
            // rim and most of the depth lives in the inner half. It is one mesh, one renderer and
            // one draw where three stood, and it is the only thing in the frame the eye reads as
            // going down.
            //
            // ⚠️ THE DEPTH IS TIED TO THE RADIUS BUT NOT EQUAL TO IT. At `radius * 0.40` a 2.8 m
            // void is 0.62 m deep, which is under half the 1.35 m hover, so the bottom of the
            // funnel stays clear of the road it is supposed to be floating above. Uniform scaling
            // would put it through the street.
            var outer = VfxShapes.Stand(core.transform, "VoidMouth",
                                        VfxShapes.Funnel(5, 12, 0.55f, 0.08f, 2.4f),
                                        radius, heightScale: radius * 0.40f);
            VfxMaterial.Ghost(outer.GetComponent<Renderer>(), new Color(0.06f, 0.01f, 0.11f, 0.93f), 0.0f);

            // The event horizon: a thin bright collar at the LIP, which is the only lit part.
            // A `Prism` ring rather than a fourth disc, so the rim has a real edge standing proud
            // of the mouth instead of another translucent plate blending into it.
            var lip = VfxShapes.Stand(core.transform, "VoidLip",
                                      VfxShapes.Collar(14, 1.0f, 0.945f, 0.0f, 0.0f, 3),
                                      radius * 1.05f, heightScale: 0.10f, lift: -0.02f);
            VfxMaterial.Ghost(lip.GetComponent<Renderer>(),
                              new Color(0.62f, 0.20f, 0.98f, 0.60f), 0.30f);
            VfxMaterial.StripCollider(lip);

            // ⚠️⚠️ AND THE GROUND STILL HAS TO SAY WHERE THE DANGER IS, WHICH IS THE HALF A
            // HOVERING EFFECT BREAKS. The hazard resolves by distance on the FLOOR
            // (`SeanceVoidComponent` compares flat positions), so lifting the art off the floor
            // without leaving a mark would put the gameplay circle somewhere the player cannot
            // see it. That is the exact fault `HeroAbility.TelegraphRadius` exists to stop: a
            // telegraph that lies is worse than no telegraph.
            //
            // ⚠️⚠️ A DASHED RING, NOT A FILLED DISC, AND THE FILLED DISC WAS THE SINGLE LARGEST
            // PAINTED AREA IN THE GAME. This telegraph was a `Cylinder` at the FULL radius at
            // 0.42 alpha, so a 2.8 m void laid a 24.6 m² violet plate on a 196 m² court: 12.5 per
            // cent of the box for a marker whose entire job is to say where the edge is. It is
            // most of the purple in `ability_worstframe_v11.png` and most of the reason the void
            // reads as a puddle rather than as a hole.
            //
            // ⚠️ THE INFORMATION IS THE RADIUS, AND A RING CARRIES IT. `HeroAbility.TelegraphRadius`
            // exists so a telegraph cannot lie about where the danger is, and the note above is
            // right that a hovering effect must still mark the floor. None of that requires
            // filling the circle in. Sixteen short plates at the rim say exactly the same thing
            // over about a twelfth of the area, and a broken ring reads as a boundary rather than
            // as a surface a player might think they can stand on.
            //
            // ⚠️⚠️ IT IS A CONTINUOUS RING, AND `Wedges` WAS THE WRONG BUILDER FOR IT. The
            // fracture builder was the first answer here because it is cheap and it made the
            // area small, which was the actual problem with the filled disc. But
            // `ability_seance_void_eye_v15.png` shows what it costs at eye height: eighteen
            // separate plates, each at its own angle and depth, read as PURPLE LITTER strewn
            // across the road rather than as the edge of anything. A boundary has to be
            // continuous to be read as a boundary, and Dante's crust wants the opposite because
            // broken ground genuinely is in pieces. Same shape budget, right builder.
            //
            // ⚠️ 0.93 INNER RATIO IS A 7 PER CENT BAND: about 1.1 m² at a 2.8 m void, against the
            // 24.6 m² the filled `Cylinder` painted. That is the whole point of the change and it
            // is unaffected by which builder draws it.
            var pull = VfxShapes.Lay(go.transform, "VoidGroundPull",
                                     VfxShapes.Collar(24, 0.02f, 0.93f, 0.0f, 0.0f, 5),
                                     radius, 0.015f);
            VfxMaterial.Ghost(pull.GetComponent<Renderer>(),
                              new Color(0.42f, 0.13f, 0.76f, 0.42f), 0.16f);
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

            /// <summary>
            /// How hard a body is dragged toward the centre, in impulse per second.
            ///
            /// ⚠️ THE DEFAULT IS THE OLD CONSTANT, so `SpawnSeanceVoid` keeps exactly the
            /// behaviour it was measured with. Only Kuro Unbound raises it: see § THE PULL.
            /// </summary>
            public float PullStrength = 4.0f;

            /// <summary>How fast a loose tsinelas slides in, in metres per second.</summary>
            public float SlipperPull = 5.5f;

            private float _left;
            private readonly Dictionary<int, float> _nextDrowseBySlot = new Dictionary<int, float>();

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left <= 0.0f)
                {
                    // ⚠️ IT CUTS RATHER THAN FADES, which is the whole shape of the cue. A tail
                    // that trails off says the danger is lessening; a hole in the world is either
                    // there or it is not, and four players need to know which on one frame.
                    GameServices.Audio?.PlayAt("sfx_void_close", transform.position);
                    Object.Destroy(gameObject);
                    return;
                }

                // Rotate cosmic vortex discs
                transform.Rotate(Vector3.up, 75.0f * Time.deltaTime);

                if (!NetAuthority.ShouldResolve()) return;
                var round = GameServices.Round;
                if (round == null) return;

                // -------------------------------------------------------------------
                // § THE PULL
                //
                // ⚠️⚠️ 🧑 2026-08-27: *"make kuro's pull stronger and longer ... make it pull
                // everyone and everything (except for can and nemu)"*. At 4.0 the impulse was
                // about **13 per cent of `Balance.Speed`**, so anybody walking out simply walked
                // out and the drag was decorative. `PullStrength` is a field rather than a
                // constant because the same component is Nemu's SKILL-tier Seance Void as well as
                // her ULTIMATE, and an ultimate that pulls exactly as hard as a skill is the
                // *"reads as a one time"* complaint in another costume.
                //
                // ⚠️⚠️ THE ONE THING IT MUST NEVER PULL IS THE LATA, AND THAT IS WHY THERE IS NO
                // CODE HERE FOR IT. Dragging the objective is scoring: `CLAUDE.md` § 4 is that
                // every point is awarded in ONE function, and a hazard that can walk the can into
                // its own centre would knock it over on somebody's behalf every cast. Kuro pulls
                // the PLAYERS and the TSINELAS, which is *"everyone and everything"* minus the
                // two things 🧑 named.
                //
                // ⚠️ AND THE OWNER IS EXEMPT, which is the "except nemu" half. It is her power;
                // being sucked into her own mouth would make casting it at her own feet a
                // self-stun.
                // -------------------------------------------------------------------
                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == OwnerSlot) continue;

                    Vector3 diff = transform.position - p.transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude <= Radius)
                    {
                        p.ApplyImpulse(diff.normalized * PullStrength * Time.deltaTime);
                        if (CanPulse(_nextDrowseBySlot, p.PlayerSlot, 1.25f))
                            p.ApplyStagger(0.35f);
                    }
                }

                // ⚠️ A HELD TSINELAS IS NOT PULLED AND DOES NOT NEED TO BE: it is in somebody's
                // hand, and that somebody is being pulled by the loop above. Yanking it out of
                // the carry would be a disarm, which is a verb this game does not have.
                foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (s != null && s.State != SlipperState.Held)
                    {
                        Vector3 sDiff = transform.position - s.transform.position;
                        sDiff.y = 0.0f;
                        if (sDiff.magnitude <= Radius && sDiff.magnitude > 0.5f)
                        {
                            s.transform.position += sDiff.normalized * SlipperPull * Time.deltaTime;
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // PHAISTER'S THREE POWERS, BUILT THREE DIFFERENT WAYS
        //
        // ⚠️⚠️ THEY USED TO BE ONE BUILDER AT THREE RADII AND THAT IS THE FAULT THIS SECTION
        // EXISTS TO ANSWER. 🧑 2026-08-26: *"the fucking abilities of phaister are repetitive
        // they use the same magic circle i want them to have different colors and different
        // animations and different symbols. DIFFERENT EVERYTHING FIGURE OUT HOW THEY WILL ALL BE
        // DIFF"*, and, exactly: *"her Q is just 2 stars on top of each other"*.
        //
        // ⚠️⚠️ HE WAS READING THE CODE OFF THE SCREEN. `SpawnWitchSigil` drew `VfxShapes.Sigil`
        // twice, an outer star polygon and an inner one, and `SpawnCastGlyph` called it with a
        // hard-coded `5, 2`. The hex, BOTH ends of the blink and the eclipse were therefore the
        // same pentagram stacked on itself; the only things that varied were radius and a seed
        // that moves the rim ticks and nothing else. `docs/TODO.md` § 21.5 argued for that on
        // purpose (*"her kit is one CRAFT"*) and the argument does not survive being looked at:
        // a shared visual LANGUAGE is a palette and a vocabulary, not a shared mesh function.
        //
        // ⚠️ SO ALL FIVE CHANNELS ARE NOW SPENT ON HER, INCLUDING THE FIFTH. `docs/TODO.md`
        // § 19.1 added CONSTRUCTION to silhouette, axis, motion and hue, and construction is the
        // one nobody had spent here:
        //
        //   Q  HEX    `VfxShapes.WardCircle`  rectilinear, dense, WRITTEN, and STATIC.
        //   E  SHADOW BLINK `VfxShapes.Rift`        vertical, torn, no circle in it at all.
        //   R  GRAND COVEN  `VfxShapes.Corona`      overhead, and the middle is empty.
        //
        //   Q  inscribes itself and then does not move.
        //   E  is two DIFFERENT events: a tear that opens, and a fall that closes.
        //   R  arrives from the sky and takes the sky with it (`Visual.SkyEvent`).
        //
        //   Q  magenta rules with gold writing.
        //   R  gold corona against a black hole; no magenta in the ultimate at all.
        //   E  near-white at the tear, because a rip in the world is not a colour.
        // -------------------------------------------------------------------

        // -------------------------------------------------------------------
        // THE SIGNATURE LAYER, ONE PER HERO
        //
        // ⚠️⚠️ THE FIVE KITS ALREADY HAD DIFFERENT BUILDERS AND STILL FELT ALIKE, WHICH IS THE
        // FINDING THIS SECTION EXISTS FOR. `docs/TODO.md` § 19 gave each of them its own
        // construction (a slab for Cheska, broken plates for Dante, a swept flame for Sean, a
        // branching tube for Zack, a funnel for Nemu) and that pass was right. What none of them
        // got was an answer to the question a MOTIF answers, which is not *"what does this
        // ability look like"* but *"what does this element LEAVE BEHIND"*. 🧑 2026-08-26: *"look
        // for a motif OR something else we can try to add to increase the quality or experience
        // of playing the characters, so that it doesnt feel like party confetti or some shit"*.
        //
        // ⚠️ SO EACH OF THESE IS ONE EXTRA LAYER ON THE EFFECT THAT HERO ALREADY HAD, on a
        // builder nothing else in the game uses, and every one of them is STROKES or PIECES
        // rather than area. `docs/VISION.md` § 2 rule 3: spend the budget on detail, not on
        // area. Between them these four add about 3 m² across the whole roster.
        //
        //   Cheska  `Fracture`  the ice keeps GOING past the slab, along cracks
        //   Dante   `Upheaval`  the road he broke is standing up around the hole
        //   Sean    `Cinder`    the fire has spread past its own edge and is still eating
        //   Zack    `Filament`  the charge has found something to arc to
        // -------------------------------------------------------------------

        /// <summary>
        /// Cracks running out of Cheska's ice, past the slab it froze.
        ///
        /// ⚠️⚠️ IT IS OUTSIDE THE HAZARD AND HURTS NOBODY, WHICH IS THE POINT AND ALSO THE RISK.
        /// `HeroAbility.TelegraphRadius` exists because *"a telegraph that lies is worse than no
        /// telegraph"*, so anything drawn beyond a zone's real radius has to be unmistakably
        /// DECORATION rather than danger. These are hairlines: no fill, no glow, no rim, at a
        /// third of the alpha of the slab itself. A player reads the slab as the ice and these as
        /// what the ice did to the road, which is exactly the distinction.
        ///
        /// ⚠️ THE REACH IS 1.35x AND NOT MORE. Far enough that the sheet visibly propagated;
        /// close enough that nobody standing on a crack thinks they are standing on ice.
        /// </summary>
        public static GameObject SpawnFrostCracks(Transform parent, float radius, int seed)
        {
            // ⚠️⚠️ THEY START AT THE SLAB'S EDGE AND THEY ARE HAIRLINES, AND THE FIRST VERSION
            // WAS NEITHER. `ability_ice_sheet_v21.png`: arms beginning at the centre at a 0.045
            // bar, ghosted at 0.34 with emission, drew as **white spokes laid over the ice** and
            // read as tape rather than as damage. Two separate mistakes and both are visible only
            // in a render. `0.72` puts the origin outside the hexagon so every stroke is on the
            // ROAD, and 0.014 is about 4 cm at this radius, which is a crack.
            //
            // ⚠️ AND NO EMISSION AT ALL. A crack is an absence of material, so it must not glow:
            // emission is what made these the brightest thing in the frame, on the effect whose
            // own note records the same slab clipping to pure white once already.
            var cracks = VfxShapes.Lay(parent, "FrostCracks",
                                       VfxShapes.Fracture(7, 3, 0.014f, seed, from: 0.72f),
                                       radius * 1.35f, 0.012f);

            VfxMaterial.Ghost(cracks.GetComponent<Renderer>(),
                              new Color(0.72f, 0.88f, 0.98f, 0.42f), 0.0f);
            VfxMaterial.StripCollider(cracks);
            return cracks;
        }

        /// <summary>
        /// The road Dante broke, standing up around the hole he made.
        ///
        /// ⚠️⚠️ IT IS THE ONLY THING IN THE GAME THAT SAYS WHERE A FIGHT HAPPENED. Every other
        /// effect is gone in seconds and leaves the street exactly as it found it. Displacement
        /// is his motif because it is the one element whose real-world signature is PERMANENT,
        /// and `docs/TODO.md` § 27.4's acceptance test is *"you can see where Dante has been
        /// fighting from across the arena, thirty seconds later"*.
        ///
        /// ⚠️ IT IS A DECAL WITH HEIGHT, NOT COLLISION, AND THAT BOUND IS LOAD-BEARING.
        /// `MapGeometryCheck` refuses geometry that floats or buries, the bots path around
        /// `HazardVolume` radii and nothing else, and a hole a player could stand in is a hole
        /// they will get stuck in. The slabs lean OUT from the rim so they never occupy the
        /// middle a player walks through.
        ///
        /// ⚠️ `Stand`, NEVER `Lay`. It has real height; § 19.1 records the 2 m ball that shipped
        /// from `Lay` leaving the Y scale at 1.0.
        /// </summary>
        public static GameObject SpawnUpheaval(Transform parent, float radius, int seed)
        {
            // ⚠️⚠️ SIX SLABS, OUTSIDE THE CRUST, AND THE FIRST VERSION WAS THE BLACK FLOWER
            // § 19.2d ALREADY WARNED ABOUT. Nine evenly spaced slabs of similar width at 0.92 of
            // the radius covered the hot bed completely and drew as a dark PINWHEEL:
            // `ability_lava_decal_v21.png`. That entry says it in as many words, about `Wedges`,
            // and it happened again to a different builder in the same place. **Even spacing is
            // the fault, not the count.**
            //
            // ⚠️ SO IT SITS AT 1.18 OF THE RADIUS, OUTSIDE the crust rather than on top of it,
            // exactly as Cheska's cracks sit outside her slab. What Dante's stomp did to the road
            // is a separate statement from what is burning in the middle of it, and stacking them
            // hid the effect the ability is actually about.
            //
            // ⚠️ THE RISE IS DOUBLED AND THE FOOTPRINT HALVED, because these have to read as
            // STANDING UP from eye height. A slab tipped 0.24 m is a plate on the road; the point
            // of the whole motif is that the material went somewhere.
            var raised = VfxShapes.Stand(parent, "Upheaval",
                                         VfxShapes.Upheaval(6, 0.06f, 0.72f, seed),
                                         radius * 1.18f,
                                         heightScale: radius * 0.62f,
                                         lift: 0.015f);

            // ⚠️ OPAQUE, BECAUSE BROKEN GROUND IS GROUND. `docs/TODO.md` § 19.2a's rule: ground
            // that has been burnt or broken is opaque and only things you can genuinely see
            // through are ghosted.
            //
            // ⚠️ AND IT IS ROAD-COLOURED, NOT NEAR-BLACK. 0.26/0.21/0.18 read as a hole in the
            // frame at this size; concrete that has been lifted is still concrete, and the whole
            // claim of the motif is that a player recognises it as the street.
            VfxMaterial.Solid(raised.GetComponent<Renderer>(), new Color(0.42f, 0.38f, 0.34f));
            VfxMaterial.StripCollider(raised);
            return raised;
        }

        /// <summary>
        /// Fire that has spread past its own edge and is still going.
        ///
        /// ⚠️⚠️ THE GAPS ARE THE EFFECT. A continuous shape says "this area is on fire"; separate
        /// pieces at falling density say "this is spreading and it has not got everywhere yet",
        /// which is the only thing that makes a trail drop feel like it has an age.
        /// `HeroHazards.Burn` already shrinks the mark toward its own end for the same reason;
        /// this is that idea in the geometry rather than in the animation.
        ///
        /// ⚠️ IT SITS OUTSIDE THE DISC AND IS DECORATION, exactly like Cheska's cracks, and it
        /// carries the same bound: no rim, low alpha, nothing that could be mistaken for the
        /// hazard's own edge. Sean's corridor was the worst offender in the game at 27.2 per cent
        /// of the box (`docs/VISION.md` § 2) and this must not put a single square metre back.
        /// Measured: the pieces cover about 9 per cent of the ring they are scattered in.
        /// </summary>
        public static GameObject SpawnCinderFringe(Transform parent, float radius, int seed)
        {
            var fringe = VfxShapes.Lay(parent, "CinderFringe",
                                       VfxShapes.Cinder(4, 8, 0.42f, seed),
                                       radius * 1.28f, 0.014f);

            VfxMaterial.Ghost(fringe.GetComponent<Renderer>(),
                              new Color(0.96f, 0.42f, 0.14f, 0.55f), 0.85f);
            VfxMaterial.StripCollider(fringe);
            return fringe;
        }

        /// <summary>
        /// The scorched ground a Supernova leaves, and the half of that ultimate that lasts.
        ///
        /// ⚠️⚠️ 🧑 2026-08-27: *"give supernova an explosion effect and more of an impact in a
        /// game it just reads as a one time down on laata and knockback"*. It already called
        /// `CreateExplosion`, so the missing thing was never the blast: it was that **Sean's was
        /// the only ultimate in the game that left nothing behind**. Dante's fissure stands earth
        /// pillars for 5.0 s, Nemu's maw runs 5.0 s, Phaister's eclipse is a 7.0 s zone, Cheska
        /// encases each victim for 2.5 s. Sean leapt, landed, and the court was exactly as it had
        /// been one second later, which is precisely the *"one time"* he is describing.
        ///
        /// ⚠️⚠️ AND IT IS WHAT MAKES THE ULTIMATE WORTH PRESSING AS A TAYA. With the lata gate in
        /// `CreateExplosion` a defending Sean no longer knocks his own can over, but that only
        /// removes a reason NOT to cast it. Burning ground around the objective is a reason to:
        /// the attackers' retrieval run has to cross it. Cast as an attacker, the same crater
        /// denies the taya their guard position. One power, opposite uses, chosen by where you
        /// land, which is the shape `docs/TODO.md` § 31.4 gave Phaister's eclipse.
        ///
        /// ⚠️ IT IS SEAN'S OWN CONSTRUCTION AND NOT HIS TRAIL'S. `SpawnFireTrail` drops discs on a
        /// timer; this is one torn ring with the cinder motif inside it, so the two do not read as
        /// the same power at two sizes. `docs/TODO.md` § 29's rule is about the SIGNATURE of an
        /// effect, and a crater and a trail are different signatures. `VfxShapes.Cinder` recurring
        /// is the point of a motif: it separates Sean from the other five, not from himself.
        ///
        /// ⚠️ THE RADIUS IS THE GAMEPLAY RADIUS. `HazardVolume` is attached at exactly this
        /// number so the bots path around what it actually is, which is the bound
        /// `AiTuning.HazardAvoidMaxRadius` exists to keep meaningful.
        /// </summary>
        public static GameObject SpawnSupernovaCrater(Vector3 position, float radius,
                                                      float duration, int ownerSlot)
        {
            var go = new GameObject("SupernovaCrater");
            go.transform.position = position;

            // The rim. `Collar` is a continuous annulus, which § 19.2's rule asks for on anything
            // that is a BOUNDARY: the edge of the burn is a boundary and has to be readable as
            // one from any angle.
            var rim = VfxShapes.Lay(go.transform, "CraterRim",
                                    VfxShapes.Collar(44, 0.10f, 0.94f),
                                    radius, 0.026f);
            VfxMaterial.Ghost(rim.GetComponent<Renderer>(),
                              new Color(1.00f, 0.46f, 0.12f, 0.72f), 1.05f);
            VfxMaterial.StripCollider(rim);

            // The burnt floor inside it, dark rather than bright: this is ash with heat under it.
            // ⚠️ IT IS THE DIMMEST PART OF THE EFFECT ON PURPOSE. A bright plate at 5 m radius is
            // the puddle `docs/VISION.md` § 2 rule 3 names; what says "burning" is the RIM and the
            // cinders, both of which are thin.
            var bed = VfxShapes.Lay(go.transform, "CraterBed",
                                    VfxShapes.Splat(26, 0.30f, ownerSlot * 13 + 7),
                                    radius * 0.92f, 0.012f);
            VfxMaterial.Ghost(bed.GetComponent<Renderer>(),
                              new Color(0.22f, 0.07f, 0.03f, 0.62f), 0.0f);
            VfxMaterial.StripCollider(bed);

            SpawnCinderFringe(go.transform, radius, ownerSlot * 31 + 5);

            var light = new GameObject("CraterGlow");
            light.transform.SetParent(go.transform, false);
            light.transform.localPosition = new Vector3(0.0f, 0.9f, 0.0f);
            var l = light.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.00f, 0.52f, 0.20f);
            l.range = radius * 2.4f;
            l.intensity = 1.5f;
            l.shadows = LightShadows.None;

            HazardVolume.Attach(go, radius, ownerSlot);

            var comp = go.AddComponent<SupernovaCraterComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;
            comp.Glow = l;

            Object.Destroy(go, duration);
            return go;
        }

        /// <summary>
        /// The crater burning down, and singeing whoever stands in it.
        ///
        /// ⚠️⚠️ HOST-SIDE, LIKE EVERYTHING THAT TOUCHES A BODY. `tools/audit_ability_authority.py`
        /// exists because 23 places in this tree did not do this (`docs/TODO.md` § 25.1 and
        /// § 31.11); a new one must not be the 24th.
        ///
        /// ⚠️ IT STAGGERS ON A CADENCE RATHER THAN EVERY FRAME, for the reason `sfx_stun_break`'s
        /// note gives about a cue at 10 Hz: a per-frame stagger is not a hazard, it is a hold, and
        /// `CharacterMotor.ApplyStagger` overlaps via `Max()` so re-applying faster than the
        /// stagger lasts is an inescapable lock.
        /// </summary>
        public sealed class SupernovaCraterComponent : MonoBehaviour
        {
            public float Radius = 4.8f;
            public float Duration = 5.0f;
            public int OwnerSlot = -1;
            public Light Glow;

            /// <summary>Seconds between singes. Longer than the stagger it applies.</summary>
            private const float SingeEvery = 0.85f;

            private const float SingeHold = 0.30f;

            private float _left;
            private float _next;
            private float _glowRest = 1.5f;

            private void Awake()
            {
                _left = Duration;
                if (Glow != null) _glowRest = Glow.intensity;
            }

            private void Update()
            {
                float dt = Time.deltaTime;
                _left -= dt;

                // The heat going out of it, so the last second is visibly cooling rather than
                // the whole thing vanishing on one frame.
                if (Glow != null && Duration > 0.0f)
                {
                    float k = Mathf.Clamp01(_left / Duration);
                    Glow.intensity = _glowRest * (0.25f + 0.75f * k)
                                     * (1.0f + Mathf.Sin(Time.time * 7.3f) * 0.08f);
                }

                if (!NetAuthority.ShouldResolve()) return;

                _next -= dt;
                if (_next > 0.0f) return;
                _next = SingeEvery;

                var round = GameServices.Round;
                if (round == null) return;

                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == OwnerSlot) continue;

                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude > Radius) continue;

                    p.ApplyStagger(SingeHold, StunElement.Fire, Balance.StunBreakPressesDefault);
                }
            }
        }

        /// <summary>
        /// Zack's charge finding something to arc to.
        ///
        /// ⚠️⚠️ THE ENDS COME OFF THE LIVE SCENE, WHICH IS THE WHOLE MOTIF AND IS NOT SOMETHING
        /// ANY OTHER EFFECT IN THIS GAME DOES. Every hazard here is built from constants and a
        /// position; this one asks what is actually nearby and draws to it, so standing next to
        /// the lata while Zack is charged looks different from standing in an empty corner.
        /// `docs/TODO.md` § 27.2: electricity is the one element whose fiction is that it
        /// connects things that already exist.
        ///
        /// ⚠️⚠️ IT MUST NOT BECOME A TARGETING AID, AND THAT BOUND IS WHY IT IS SHORT AND WHY IT
        /// IGNORES WHAT IT CANNOT SEE. An arc that reached a body through a barricade would tell
        /// a player where somebody is hiding, which is information the game does not otherwise
        /// give and would be an aimbot drawn in lightning. 3.2 m is inside the arc's own fiction
        /// and well under the distance at which hiding matters.
        ///
        /// ⚠️ AND IT IS PURELY VISUAL. No `HazardVolume`, no stagger, no collider: the arcs say
        /// where charge IS, and `ZackHeroKit` remains the only thing that decides what it does.
        /// </summary>
        public static GameObject SpawnCircuitArcs(Vector3 at, float reach, int ownerSlot,
                                                  float duration = 0.55f)
        {
            var go = new GameObject("CircuitArcs");
            go.transform.position = at + Vector3.up * 0.9f;

            // ⚠️ AT MOST THREE, AND THE NEAREST THREE. `docs/VISION.md` § 2 rule 4 caps what may
            // overlap, and a discharge that arced to everything on a busy court would be a web
            // over the whole arena. Three is enough to read as "it found something" and few
            // enough that the individual spans stay legible.
            var ends = new System.Collections.Generic.List<Vector3>(3);

            foreach (var hit in Physics.OverlapSphere(at, reach))
            {
                if (ends.Count >= 3) break;

                var motor = hit.GetComponentInParent<CharacterMotor>();
                if (motor != null && motor.PlayerSlot == ownerSlot) continue;

                var body = hit.GetComponentInParent<Transform>();
                if (body == null) continue;

                Vector3 to = body.position + Vector3.up * 0.6f - go.transform.position;
                if (to.sqrMagnitude < 0.35f) continue;

                // Local space is the unit circle, like every builder in `VfxShapes`, so the
                // reach divides out here and the mesh is scaled by it below.
                ends.Add(to / reach);
            }

            if (ends.Count == 0)
            {
                // ⚠️ NOTHING NEARBY IS A REAL ANSWER AND IT IS DRAWN AS ONE. Two stubs going
                // nowhere say the charge is live and found no route, which is more honest than
                // either drawing nothing (the ability looks broken) or inventing a target.
                ends.Add(new Vector3(0.42f, -0.35f, 0.30f));
                ends.Add(new Vector3(-0.38f, -0.30f, -0.26f));
            }

            var web = VfxShapes.Stand(go.transform, "Arcs",
                                      VfxShapes.TwoSided(
                                          VfxShapes.Filament(ends.ToArray(), 2, 0.035f,
                                                             ownerSlot * 31 + 7)),
                                      reach, heightScale: reach);

            VfxMaterial.Ghost(web.GetComponent<Renderer>(), UiTheme.HeroElectricBright, 1.20f);
            VfxMaterial.StripCollider(web);

            var flick = go.AddComponent<ArcFade>();
            flick.Web = web.transform;
            flick.Duration = duration;

            Object.Destroy(go, duration);
            return go;
        }

        /// <summary>
        /// The arcs guttering out.
        ///
        /// ⚠️ IT FLICKERS RATHER THAN FADES, and the two are not the same read. A discharge does
        /// not dim: it is either conducting or it is not, several times a second, which is what
        /// `Visual.ArcFlicker` already does for Zack's other geometry. A smooth fade here would
        /// make his one instantaneous element the only thing in his kit that eases out.
        /// </summary>
        public sealed class ArcFade : MonoBehaviour, Visual.IVfxTimeline
        {
            public Transform Web;
            public float Duration = 0.55f;

            private float _elapsed;
            private Renderer _renderer;
            private float _alpha = 1.0f;

            public float LifeSeconds => Mathf.Max(0.1f, Duration);

            private void Awake()
            {
                if (Web != null) _renderer = Web.GetComponent<Renderer>();
                if (_renderer != null) _alpha = _renderer.sharedMaterial.color.a;
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                if (_renderer == null || _renderer.sharedMaterial == null) return;

                float t = Mathf.Clamp01(_elapsed / LifeSeconds);

                // Two square waves at unrelated rates: on most of the time early, off most of
                // the time late, and never a smooth ramp between the two.
                float gate = (Mathf.Repeat(_elapsed * 23.0f, 1.0f) > t * 0.85f) ? 1.0f : 0.15f;

                var c = _renderer.sharedMaterial.color;
                c.a = _alpha * gate * (1.0f - t * t);
                _renderer.sharedMaterial.color = c;
            }
        }

        // -------------------------------------------------------------------
        // NEMU, WHOSE MOTIF IS ABSENCE
        //
        // ⚠️⚠️ EVERY OTHER EFFECT IN THIS GAME ADDS SOMETHING TO THE FRAME. Hers has to look
        // like it took something out of it: `docs/TODO.md` § 27.5, and `VfxShapes.Hollow` is the
        // construction, a rim around nothing with a torn inner edge. Nothing else in the file
        // uses it and nothing else should.
        //
        // ⚠️ AND HER KIT IS THE PET NOW, WHICH IS WHY BOTH OF THESE ARE ABOUT KURO. 🧑
        // 2026-08-26: *"for nemu i want her skills to involve her pet more as well as her ult"*,
        // and, on the old ultimate: *"her black hole dont make sense lowkey?"*. The vortex that
        // opened out of nothing three metres in front of her is gone; what is left either comes
        // out of the pet or marks where the pet went.
        // -------------------------------------------------------------------

        /// <summary>
        /// KURO UNBOUND. The pet, swollen into a maw, eating the street around it.
        ///
        /// ⚠️⚠️ IT IS THE OLD SEANCE VOID'S HAZARD WITH A NEW BODY, DELIBERATELY. The drag, the
        /// slow, the 2.8 m radius and the `HazardVolume` are what `Hero_Strike_Balance.md`
        /// measured and what the bots path around, so none of them moves: this is a fiction and
        /// presentation change and it must not quietly become a balance one. What changes is that
        /// it is no longer a hole that appeared, it is Kuro.
        ///
        /// ⚠️ THE MAW HAS REAL HEIGHT AND THE OLD ONE DID NOT. `Funnel` dished DOWN, which reads
        /// from above and disappears at eye level; a mouth standing 1.2 m off the road is
        /// readable from a standing player's own height, which is where this game is played from.
        /// The `Hollow` rim on the ground says how far it reaches and the maw says what it is.
        /// </summary>
        public static GameObject SpawnKuroUnbound(Vector3 position, float radius, float duration,
                                                  int ownerSlot, bool fromPet)
        {
            var go = new GameObject("KuroUnbound");
            go.transform.position = position;

            // The bite out of the road: her motif, and the thing that says how far it reaches.
            var rim = VfxShapes.Lay(go.transform, "Bite",
                                    VfxShapes.Hollow(48, 0.66f, 0.18f, ownerSlot * 13 + 5),
                                    radius, 0.02f);
            VfxMaterial.Ghost(rim.GetComponent<Renderer>(),
                              new Color(0.06f, 0.02f, 0.10f, 0.92f), 0.0f);
            VfxMaterial.StripCollider(rim);

            // ⚠️ THE MAW IS A `NovaShell` TURNED INSIDE OUT BY ITS COLOUR, NOT A NEW BUILDER.
            // Nemu already owns one construction here (`Hollow`) and § 27's rule is one motif per
            // hero, not one builder per object: a shell painted so dark it reads as an opening is
            // the same statement as the rim, in three dimensions. Giving her a second bespoke
            // solid would be the thing that rule exists to stop.
            // ⚠️⚠️ IT IS A MOUTH AROUND HIM, NOT A DOME OVER HIM, AND THE FIRST VERSION WAS THE
            // second. `ability_kuro_unbound_eye_v23.png` is a 1.3 m shell standing where the pet
            // is: at 5x scale Kuro is about 2.5 m, so the shell covered his legs and most of his
            // body and the effect read as a hole with a dog's head on top. **The whole point of
            // moving this ultimate onto the pet is that the pet is the thing you look at**, so
            // the shell comes down to a knee-high collar of teeth and he stands in it.
            //
            // ⚠️ AND IT IS GHOSTED HARDER FOR THE SAME REASON. At 0.88 it occluded him outright
            // from any angle that put it between him and the camera, which in a four-player
            // arena is most of them.
            var maw = VfxShapes.Stand(go.transform, "Maw",
                                      VfxShapes.NovaShell(6, 12, 0.22f, ownerSlot * 7 + 3),
                                      radius * 0.62f, heightScale: radius * 0.20f, lift: 0.10f);
            VfxMaterial.Ghost(maw.GetComponent<Renderer>(),
                              new Color(0.10f, 0.03f, 0.16f, 0.62f), 0.10f);
            VfxMaterial.StripCollider(maw);

            // ⚠️ A DARK SOURCE IS NOT A THING, SO THE LIGHT IS A THIN VIOLET RIM LIGHT RATHER
            // THAN A GLOW IN THE MIDDLE. Lighting the inside of a hole is the one thing that
            // would destroy the read, and every hazard light in this file already came down by
            // two thirds for the smaller version of the same mistake.
            var lightGo = new GameObject("MawRimLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0.0f, 0.25f, 0.0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroSpiritBright;
            light.range = radius * 2.0f;
            light.intensity = 0.85f;
            light.shadows = LightShadows.None;

            // ⚠️ `MawIntake`, NOT `VoidWisp`. Her phase already uses `VoidWisp` and that one is
            // her body coming apart: motes leaving a person and drifting. This has to say the
            // opposite, that the court is being taken IN, and the geometry cannot say it on its
            // own. The two auras differ by the sign and the size of one module.
            // ⚠️⚠️ WITH NO PET OUT THERE IS NOTHING IN THE MIDDLE, AND THAT IS A HOLE IN THE
            // DESIGN RATHER THAN IN THE RENDER. `ability_kuro_unbound_v23.png` is what an empty
            // one looks like and 🧑 asked the right question of it: *"where tf is kiro in this
            // ult? did u js forget to render him or what"*. In a match with Kuro out he is the
            // centrepiece: `GhostPetCompanion.Devour` grows him five times, horns him and turns
            // him black, and the geometry above is what happens AROUND him.
            //
            // ⚠️⚠️ BUT THE ABILITY HAS A FALLBACK PATH (`NemuHeroKit`, no pet out, it opens in
            // front of her) AND ON THAT PATH THERE IS NO KURO AT ALL. So the ultimate would be a
            // torn ring with a hole in it, which is the old Seance Void wearing the new name: the
            // exact thing this rework was for. **The fallback grows its own.** A spectral core,
            // dark, spiky and turning, so the power always has a body at its centre and the
            // sentence "Kuro is the black hole" is true however it was cast.
            //
            // ⚠️ IT IS DELIBERATELY NOT THE PET'S MESH. Loading `character-ghost.glb` here would
            // put an asset dependency into the hazard layer, and the fallback is not Kuro: it is
            // the shape of him that the spell reaches for when he is not there. `Spire` at a low
            // side count reads as a hunched, horned mass at this size and is what his own horns
            // are made of, which ties the two paths together without pretending they are one.
            if (!fromPet)
            {
                var core = VfxShapes.Stand(go.transform, "MawCore",
                                           VfxShapes.Spire(5, 0.34f, 0.42f, ownerSlot * 17 + 9),
                                           radius * 0.30f,
                                           heightScale: radius * 0.62f,
                                           lift: 0.05f);

                VfxMaterial.Ghost(core.GetComponent<Renderer>(),
                                  new Color(0.09f, 0.03f, 0.14f, 0.94f), 0.12f);
                VfxMaterial.StripCollider(core);

                for (int h = 0; h < 5; h++)
                {
                    float a = h / 5.0f * Mathf.PI * 2.0f;

                    var horn = VfxShapes.Stand(core.transform, $"MawCoreHorn_{h}",
                                               VfxShapes.Spire(4, 0.08f, 0.30f, 700 + h * 13),
                                               0.30f, heightScale: 0.85f);

                    horn.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.42f, 0.55f,
                                                               Mathf.Sin(a) * 0.42f);
                    horn.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 38.0f, 0.0f,
                                                                    -Mathf.Cos(a) * 38.0f);

                    VfxMaterial.Ghost(horn.GetComponent<Renderer>(),
                                      new Color(0.06f, 0.02f, 0.10f, 0.96f), 0.0f);
                    VfxMaterial.StripCollider(horn);
                }

                var turn = core.AddComponent<MawCoreTurn>();
                turn.Duration = duration;
            }

            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.MawIntake, duration);

            GameServices.Audio?.PlayAt("sfx_kuro_unbound", position);

            var anim = go.AddComponent<MawSwell>();
            anim.Rim = rim.transform;
            anim.Maw = maw.transform;
            anim.Duration = duration;

            var comp = go.AddComponent<SeanceVoidComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;

            // ⚠️⚠️ THE ULTIMATE PULLS 3.5x AS HARD AS THE SKILL THAT SHARES THIS COMPONENT.
            // 🧑 2026-08-27: *"make kuro's pull stronger and longer"*. At the shared default of
            // 4.0 the drag was about 13 per cent of `Balance.Speed`, which anybody could simply
            // walk out of: the most expensive thing Nemu can do was a visual with a slow on it.
            // 14.0 is about 3.0 m/s of inward drag against a 4.6 m/s walk, so leaving is still
            // possible and is now a decision rather than a formality. That bound is the whole
            // design: `docs/VISION.md` § 4 forbids anything with no counterplay, and a pull the
            // player cannot beat is a stun that lasts as long as the ultimate does.
            comp.PullStrength = 14.0f;
            comp.SlipperPull = 9.0f;

            HazardVolume.Attach(go, radius, ownerSlot);
            Object.Destroy(go, duration);
            return go;
        }

        /// <summary>
        /// The fallback core rising and turning.
        ///
        /// ⚠️ IT TURNS THE OPPOSITE WAY FROM THE SHELL AROUND IT, for the reason
        /// `GhostPetCompanion.StepDevour` gives about the real pet: two objects turning at the
        /// same rate are one rigid object, and the whole read here is a thing standing inside
        /// something else.
        /// </summary>
        public sealed class MawCoreTurn : MonoBehaviour, Visual.IVfxTimeline
        {
            public float Duration = 5.0f;

            private const float Rise = 0.6f;

            private float _elapsed;
            private Vector3 _full = Vector3.one;

            public float LifeSeconds => Mathf.Max(0.3f, Duration);

            private void Awake() => _full = transform.localScale;

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                // Sqrt so it arrives early, matching the pet's own swell curve rather than
                // growing across the whole five seconds.
                float k = Mathf.Sqrt(Mathf.Clamp01(_elapsed / Rise));

                transform.localScale = new Vector3(_full.x * k, _full.y * k, _full.z * k);
                transform.localRotation = Quaternion.Euler(0.0f, -30.0f * _elapsed, 0.0f);
            }
        }

        /// <summary>
        /// The maw opening, breathing, and closing.
        ///
        /// ⚠️ IT OPENS FROM NOTHING AND SHUTS TO NOTHING, which is the one motion a hole is
        /// allowed. Every other transient in this game grows and then fades in place; a mouth
        /// that faded would leave a translucent ghost of itself on the road for the last fifth of
        /// its life, which is exactly what "absence" must not look like.
        /// </summary>
        public sealed class MawSwell : MonoBehaviour, Visual.IVfxTimeline
        {
            public Transform Rim;
            public Transform Maw;
            public float Duration = 5.0f;

            /// <summary>How long it takes to open. Slower than a skill: this is an ultimate.</summary>
            private const float Open = 0.55f;

            /// <summary>How long it takes to shut. Faster than it opened: it is being let go.</summary>
            private const float Shut = 0.40f;

            private float _elapsed;
            private Vector3 _rimFull = Vector3.one;
            private Vector3 _mawFull = Vector3.one;

            public float LifeSeconds => Mathf.Max(0.3f, Duration);

            private void Awake()
            {
                if (Rim != null) _rimFull = Rim.localScale;
                if (Maw != null) _mawFull = Maw.localScale;
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                float open = Mathf.Clamp01(_elapsed / Open);
                float left = LifeSeconds - _elapsed;
                float shut = left >= Shut ? 1.0f : Mathf.Clamp01(left / Shut);

                // Sqrt on the way open so it arrives fast, squared on the way shut so the last of
                // it goes suddenly. `ExplosionVfxAnim` makes the same pairing for the same reason.
                float k = Mathf.Sqrt(open) * shut * shut;

                // ⚠️ THE MAW BREATHES AND THE RIM DOES NOT. A bite in the road is a fact and does
                // not pulse; the thing standing in it is alive. Two objects, two behaviours, one
                // effect: § 19's whole argument in miniature.
                float breath = 1.0f + Mathf.Sin(_elapsed * 3.4f) * 0.06f * shut;

                if (Rim != null)
                    Rim.localScale = new Vector3(_rimFull.x * k, _rimFull.y, _rimFull.z * k);

                if (Maw != null)
                {
                    Maw.localScale = new Vector3(_mawFull.x * k * breath,
                                                 _mawFull.y * k * breath,
                                                 _mawFull.z * k * breath);
                    Maw.localRotation = Quaternion.Euler(0.0f, _elapsed * 22.0f, 0.0f);
                }
            }
        }

        /// <summary>
        /// Where Kuro landed after carrying her home. A bite, and nothing else.
        ///
        /// ⚠️⚠️ IT REPLACES A CALL TO `SpawnShockTrail`, WHICH IS ZACK'S AND IS A LIVE HAZARD.
        /// `GhostPetCompanion.EndPossession` dropped a two-second electric zone with a
        /// `HazardVolume` on it every time Nemu teleported home, so her mobility power was
        /// placing another hero's damage on the court. This one damages nobody: it is 0.9 m of
        /// her own mark, for half a second, saying a body arrived here.
        /// </summary>
        public static GameObject SpawnSpiritReturn(Vector3 at)
        {
            var go = new GameObject("SpiritReturn");
            go.transform.position = at;

            var bite = VfxShapes.Lay(go.transform, "ReturnBite",
                                     VfxShapes.Hollow(28, 0.58f, 0.26f, 91),
                                     0.9f, 0.02f);
            VfxMaterial.Ghost(bite.GetComponent<Renderer>(),
                              new Color(0.30f, 0.12f, 0.46f, 0.85f), 0.30f);
            VfxMaterial.StripCollider(bite);

            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.VoidWisp, 0.5f);

            var fade = go.AddComponent<BiteFade>();
            fade.Bite = bite.transform;

            Object.Destroy(go, 0.75f);
            return go;
        }

        /// <summary>A bite spreading and going out. Half a second, and no hazard behind it.</summary>
        public sealed class BiteFade : MonoBehaviour, Visual.IVfxTimeline
        {
            public Transform Bite;

            private const float Life = 0.55f;

            private float _elapsed;
            private Vector3 _full = Vector3.one;
            private Renderer _renderer;
            private float _alpha = 1.0f;

            public float LifeSeconds => Life;

            private void Awake()
            {
                if (Bite != null)
                {
                    _full = Bite.localScale;
                    _renderer = Bite.GetComponent<Renderer>();
                    if (_renderer != null) _alpha = _renderer.sharedMaterial.color.a;
                }
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                float t = Mathf.Clamp01(_elapsed / Life);
                float k = Mathf.Sqrt(t);

                if (Bite != null)
                    Bite.localScale = new Vector3(_full.x * k, _full.y, _full.z * k);

                if (_renderer != null && _renderer.sharedMaterial != null)
                {
                    var c = _renderer.sharedMaterial.color;
                    c.a = _alpha * (1.0f - t * t);
                    _renderer.sharedMaterial.color = c;
                }
            }
        }

        /// <summary>A theme colour at a given alpha, so a call site states the two separately.</summary>
        private static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>
        /// HEX. A ward chalked on the road: rings, a written band, nested squares.
        /// </summary>
        public static GameObject SpawnHexSigil(Vector3 position, float radius = 2.4f,
                                                    float duration = 6.0f, int ownerSlot = -1)
        {
            var go = new GameObject("HexWardZone");
            go.transform.position = position;

            // ⚠️⚠️ ONE MESH, NOT TWO STACKED ONES. What stood here was two `Sigil`s at different
            // radii with the smaller one 8 mm above the larger, counter-rotating. That is two
            // coplanar translucent plates a centimetre apart, which `docs/TODO.md` § 19.2a
            // records sorting arbitrarily on Sean's trail and drawing a different colour per
            // drop, and it is also literally the thing 🧑 named: two stars on top of each other.
            // `WardCircle` puts the rings, the band, the squares, the triangle, the medallions
            // and every glyph into ONE triangle list, so there is no sort to lose and no second
            // wheel to turn.
            var ward = VfxShapes.Lay(go.transform, "Ward",
                                     VfxShapes.WardCircle(12, 4, 0.030f, ownerSlot * 7 + 3),
                                     radius, 0.02f);

            // ⚠️ MAGENTA RULES. The lines of the diagram are hers; the WRITING is gold, and it
            // is a second object for exactly that reason. Two hues in one hero palette is the
            // thing § 21.5 got right, and the way to spend it is on which PART of the mark is
            // which, not on a gradient nobody can point at.
            VfxMaterial.Ghost(ward.GetComponent<Renderer>(),
                              Alpha(UiTheme.HeroWitch, 0.90f), 0.34f);

            // The gold overlay: the same builder at a different seed, so the strokes land in
            // different cells and the two do not simply double each other's lines. Slightly
            // higher and much thinner, so it reads as ink ON the rules rather than beside them.
            var written = VfxShapes.Lay(go.transform, "WardWriting",
                                        VfxShapes.WardCircle(12, 4, 0.017f, ownerSlot * 7 + 41),
                                        radius * 0.995f, 0.032f);
            VfxMaterial.Ghost(written.GetComponent<Renderer>(),
                              new Color(1.00f, 0.86f, 0.32f, 0.80f), 0.42f);

            // ⚠️ THE WARD FOLLOWS THE ROAD TOO, and it is 4.8 m across, which is wider than the
            // pavement it can be thrown onto. The ultimate's circle is where this was reported
            // (🧑 2026-08-27: *"her magic circle doesnt draw over the sidewalk and thats weird
            // af"*) but the fault is the whole class of flat ground art, and a ward chalked half
            // onto a kerb is the same picture at a quarter of the size.
            VfxShapes.DrapeToGround(ward);
            VfxShapes.DrapeToGround(written);

            // ⚠️ THE PERIMETER MARKS ARE FOUR, NOT SIX, AND THEY STAND ON THE MEDALLIONS. Six at
            // 60 degrees lined up with nothing; the ward has four medallions at the compass
            // points and a standing character on each is the same glyph twice, once flat and once
            // upright, which is what makes the mark look like it was drawn by somebody who then
            // stood things on it.
            for (int n = 0; n < 4; n++)
            {
                float ang = (n * 90.0f + 45.0f) * Mathf.Deg2Rad;
                float dist = radius * 0.845f;

                // ⚠️ TWO-SIDED. Four marks at the compass points around a ward that four
                // players stand around: there is no yaw at which all four face the camera, so a
                // one-sided glyph is a mark that is missing from whichever side you approach.
                var node = VfxShapes.Stand(go.transform, $"WardMark_{n}",
                                           VfxShapes.TwoSided(VfxShapes.Rune(220 + n * 13)),
                                           0.22f, heightScale: 0.44f);
                node.transform.localPosition =
                    new Vector3(Mathf.Sin(ang) * dist, 0.14f, Mathf.Cos(ang) * dist);
                node.transform.localRotation =
                    Quaternion.Euler(0.0f, n * 90.0f + 45.0f, 0.0f);

                VfxMaterial.Ghost(node.GetComponent<Renderer>(),
                                  new Color(1.00f, 0.80f, 0.30f, 0.85f), 0.50f);
            }

            var lightGo = new GameObject("WardLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0.0f, 1.7f, 0.0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroWitchBright;
            light.range = radius * 2.4f;
            light.intensity = 1.1f;

            var inscribe = go.AddComponent<WardInscribe>();
            inscribe.Rules = ward.transform;
            inscribe.Writing = written.transform;
            inscribe.Duration = duration;

            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.WitchSigil, duration);

            GameServices.Audio?.PlayAt("sfx_hex_cast", position);

            var comp = go.AddComponent<HexSigilComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;

            HazardVolume.Attach(go, radius, ownerSlot);
            return go;
        }

        /// <summary>
        /// The ward being DRAWN, and then not moving.
        ///
        /// ⚠️⚠️ IT DOES NOT ROTATE, AND THAT IS THE ANIMATION. `WitchSigilSpin` turned two wheels
        /// against each other forever, which is a mechanism: right for a summoning circle in a
        /// cutscene and wrong for a trap on a road, because a moving mark is one a player's eye
        /// keeps returning to for the whole six seconds it is armed. This one is written on in
        /// under half a second and is then dead still until it expires. 🧑 asked for *"different
        /// animations"* between her three powers; the honest answer for the hex is that its
        /// motion happens once, at the start, and the ability is the thing that stays behind.
        ///
        /// ⚠️ THE TWO LAYERS ARE INSCRIBED IN ORDER: the magenta rules first, the gold writing
        /// after, with the writing lagging by a third of the reveal. Ruling a diagram and then
        /// filling in the characters is the order a person would do it in, and it is the only
        /// place in her kit where two objects are deliberately NOT in step.
        ///
        /// ⚠️ AN `IVfxTimeline`, so `AbilityShowcaseProbe` can wind it to any moment and
        /// photograph it. `ArcFlicker` carries the argument; a mark that only animates in
        /// `Update` is one that freezes on frame 1 in every capture.
        /// </summary>
        public sealed class WardInscribe : MonoBehaviour, Visual.IVfxTimeline
        {
            public Transform Rules;
            public Transform Writing;
            public float Duration = 6.0f;

            /// <summary>How long the hand takes. Short: a hex is snapped down, not laboured.</summary>
            private const float Inscribe = 0.42f;

            /// <summary>The writing starts this far into the reveal, as a fraction of it.</summary>
            private const float WritingLag = 0.34f;

            private const float FadeFrom = 0.82f;

            private float _elapsed;
            private Vector3 _rulesScale = Vector3.one;
            private Vector3 _writingScale = Vector3.one;
            private Renderer _rulesRenderer;
            private Renderer _writingRenderer;
            private float _rulesAlpha = 1.0f;
            private float _writingAlpha = 1.0f;

            public float LifeSeconds => Mathf.Max(0.2f, Duration);

            private void Awake()
            {
                if (Rules != null)
                {
                    _rulesScale = Rules.localScale;
                    _rulesRenderer = Rules.GetComponent<Renderer>();
                    if (_rulesRenderer != null) _rulesAlpha = _rulesRenderer.sharedMaterial.color.a;
                }

                if (Writing != null)
                {
                    _writingScale = Writing.localScale;
                    _writingRenderer = Writing.GetComponent<Renderer>();
                    if (_writingRenderer != null)
                        _writingAlpha = _writingRenderer.sharedMaterial.color.a;
                }
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                // ⚠️ IT OPENS OUTWARD FROM 0.62, NOT FROM ZERO. A mark that grows from a point is
                // a shockwave, and this game already has four of those. Starting most of the way
                // out and snapping to full size reads as a stamp landing, which is the gesture.
                float rules = Mathf.Clamp01(_elapsed / Inscribe);
                Scale(Rules, _rulesScale, Mathf.Lerp(0.62f, 1.0f, Ease(rules)));
                Fade(_rulesRenderer, _rulesAlpha * rules);

                float writeStart = Inscribe * WritingLag;
                float write = Mathf.Clamp01((_elapsed - writeStart) / (Inscribe * 0.9f));
                Scale(Writing, _writingScale, Mathf.Lerp(0.74f, 1.0f, Ease(write)));
                Fade(_writingRenderer, _writingAlpha * write);

                float t = Mathf.Clamp01(_elapsed / LifeSeconds);
                if (t < FadeFrom) return;

                // ⚠️ THE FADE IS THE LAST FIFTH ONLY, for the reason `WitchSigilSpin` recorded
                // and `Hero_Strike_Balance.md` § 8.5 argues: a player has to be able to tell a
                // spent zone from a live one, and a mark that dims from the first frame reads as
                // a broken effect rather than as a timer.
                float k = 1.0f - Mathf.InverseLerp(FadeFrom, 1.0f, t);
                Fade(_rulesRenderer, _rulesAlpha * k);
                Fade(_writingRenderer, _writingAlpha * k);
            }

            /// <summary>Fast in, hard stop. The same shape `ClipBuilder.PunchAt` gives a strike.</summary>
            private static float Ease(float t) => 1.0f - (1.0f - t) * (1.0f - t) * (1.0f - t);

            private static void Scale(Transform target, Vector3 full, float k)
            {
                if (target == null) return;
                target.localScale = new Vector3(full.x * k, full.y, full.z * k);
            }

            private static void Fade(Renderer r, float alpha)
            {
                if (r == null || r.sharedMaterial == null) return;

                var c = r.sharedMaterial.color;
                c.a = Mathf.Clamp01(alpha);
                r.sharedMaterial.color = c;
            }
        }

        /// <summary>
        /// SHADOW BLINK, the departure: the hole she left.
        ///
        /// ⚠️⚠️ THE TWO ENDS OF A BLINK ARE TWO DIFFERENT EVENTS AND THEY USED TO BE ONE BURST
        /// MIRRORED. `SpawnShadowBlinkBurst` stamped a cast glyph at each end, the same pentagram
        /// at two radii, plus eight `PrimitiveType.Cube` shards: so the moment she vanished and
        /// the moment she appeared were the same picture in two sizes. Leaving and arriving are
        /// opposite gestures and nothing about them should match.
        ///
        /// ⚠️ THE DEPARTURE IS THE ONE THAT CARRIES INFORMATION, which is why it is the bigger of
        /// the two and why the knockback is centred here. The three people chasing her already
        /// know where she went: they can see her. What they need is the mark that says she was
        /// standing HERE a moment ago, and it is where the shove came from.
        ///
        /// ⚠️ IT FACES THE BLINK. A tear is a flat thing and a flat thing seen edge-on is not
        /// there, so it is turned across the direction of travel: she went through it, so the
        /// people she left are looking at its face.
        /// </summary>
        public static GameObject SpawnShadowRift(Vector3 at, Vector3 direction)
        {
            var go = new GameObject("ShadowRift");
            go.transform.position = at + Vector3.up * 1.05f;

            direction.y = 0.0f;
            if (direction.sqrMagnitude < 0.0001f) direction = Vector3.forward;
            go.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

            // ⚠️ `Stand`, NOT `Lay`. `Lay` leaves the Y scale at 1.0, so a mesh authored at one
            // unit of height would come out 1 m tall whatever radius it was given.
            // `docs/TODO.md` § 19.1 records the 2 m ball that shipped from that exact mistake.
            // ⚠️ TWO-SIDED, AND THE ONE-SIDED VERSION SHIPPED INVISIBLE. `Rift` builds in the
            // XY plane and its front face is local +Z; turning the object to LOOK ALONG the
            // blink put that front where she went, so the three people she left behind, who are
            // the entire audience for this mark, were on the culled side.
            // `ability_blink_rift_eye_v19.png` has the light on the road and no tear in it.
            var tear = VfxShapes.Stand(go.transform, "Tear",
                                       VfxShapes.TwoSided(
                                           VfxShapes.Rift(11, 0.40f, 0.46f, 0.055f, 5)),
                                       0.95f, heightScale: 1.05f);

            // ⚠️ NEAR-WHITE, AND IT IS THE ONLY THING IN HER KIT THAT IS NOT MAGENTA OR GOLD. A
            // rip in the world is not one of her colours: the edge is the light of wherever the
            // tear goes, and giving it her palette would make it the third magenta mark in a kit
            // that already has two. It is also the cheapest possible way for the blink to be
            // told from the hex at a glance across the arena.
            VfxMaterial.Ghost(tear.GetComponent<Renderer>(),
                              new Color(0.94f, 0.88f, 1.00f, 0.95f), 1.05f);

            var lightGo = new GameObject("RiftLight");
            lightGo.transform.SetParent(go.transform, false);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.86f, 0.62f, 1.00f);
            light.range = 5.0f;
            light.intensity = 2.2f;
            light.shadows = LightShadows.None;

            var anim = go.AddComponent<RiftOpen>();
            anim.Tear = tear.transform;
            anim.Glow = light;

            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.WitchScatter, 0.5f);
            return go;
        }

        /// <summary>
        /// The tear opening and snapping shut.
        ///
        /// ⚠️⚠️ IT OPENS ACROSS AND CLOSES VERTICALLY, which is what makes it a tear rather than
        /// a scale. A shape that grows and shrinks on both axes is a bubble; a split widens
        /// sideways while it is being pulled apart and then the whole height collapses when it
        /// lets go. Two axes, two different curves, one object.
        /// </summary>
        public sealed class RiftOpen : MonoBehaviour, Visual.IVfxTimeline
        {
            public Transform Tear;
            public Light Glow;

            /// <summary>
            /// ⚠️⚠️ 0.52 s BECAME 1.30 s, AND THE OLD NUMBER IS WHY 🧑 SAW NOTHING. 2026-08-27,
            /// on the blink: *"its js a shadow too"*. The tear is the ONLY thing the three
            /// players she left behind ever see of this ability, and it existed for about
            /// thirty frames: she was gone before anybody had finished turning toward the noise.
            /// A mark that is not on screen long enough to be looked at cannot be told from a
            /// shadow, whatever it is made of, and this one is a near-white rip with its own
            /// light on it.
            ///
            /// ⚠️ IT IS STILL THE SHORTEST-LIVED THING IN HER KIT, WHICH IS CORRECT. The hex
            /// ward stands for 6.0 s and the eclipse for 7.0; a tear in the world that healed as
            /// slowly as a chalked circle would stop reading as a rip. Long enough to be seen,
            /// short enough to still be sudden.
            /// </summary>
            private const float Life = 1.30f;

            /// <summary>Fraction of the life spent widening. The rest is the snap.</summary>
            private const float OpenFraction = 0.34f;

            private float _elapsed;
            private Vector3 _full = Vector3.one;
            private float _glow = 2.2f;

            public float LifeSeconds => Life;

            private void Awake()
            {
                if (Tear != null) _full = Tear.localScale;
                if (Glow != null) _glow = Glow.intensity;
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                float t = _elapsed / Life;
                if (t >= 1.0f)
                {
                    Destroy(gameObject);
                    return;
                }

                float open = Mathf.Clamp01(t / OpenFraction);
                float shut = Mathf.Clamp01((t - OpenFraction) / (1.0f - OpenFraction));

                if (Tear != null)
                {
                    // Sqrt on the widen so it is quick off the mark, squared on the collapse so
                    // the last of it goes suddenly. Same reasoning `ExplosionVfxAnim` gives.
                    float wide = Mathf.Sqrt(open) * (1.0f - shut * shut);
                    float tall = 1.0f - shut * shut * shut;

                    Tear.localScale = new Vector3(_full.x * wide, _full.y * tall, _full.z * wide);
                }

                if (Glow != null) Glow.intensity = _glow * (1.0f - t) * open;
            }
        }

        /// <summary>
        /// SHADOW BLINK, the arrival: the script that carried her, falling in.
        ///
        /// ⚠️⚠️ IT IS BUILT FROM SEPARATE FALLING GLYPHS AND NOT FROM A MARK, so it shares no
        /// geometry with the departure at all. The tear is one torn sheet standing up; this is
        /// six small written characters dropping onto the spot and going out as they land. She
        /// does not arrive through a hole, she reassembles, and the two halves of the ability
        /// should not be able to be mistaken for each other in a still frame.
        ///
        /// ⚠️ IT IS SMALLER AND SHORTER THAN THE DEPARTURE ON PURPOSE. She is standing on it, so
        /// her body is the thing marking this place; the effect only has to say she got here.
        /// </summary>
        public static GameObject SpawnShadowArrival(Vector3 at)
        {
            var go = new GameObject("ShadowArrival");
            go.transform.position = at;

            for (int i = 0; i < 6; i++)
            {
                float ang = i / 6.0f * Mathf.PI * 2.0f + 0.4f;
                float reach = 0.42f + (i % 3) * 0.14f;

                // ⚠️ TWO-SIDED FOR THE SAME REASON THE TEAR IS. These are turned to face
                // outward around a circle, so at any camera angle roughly half of them present
                // their back and half of the effect is missing from every frame.
                var glyph = VfxShapes.Stand(go.transform, $"ArrivalGlyph_{i}",
                                            VfxShapes.TwoSided(VfxShapes.Rune(310 + i * 7)),
                                            0.26f, heightScale: 0.42f);

                glyph.transform.localPosition = new Vector3(Mathf.Cos(ang) * reach,
                                                            1.75f + (i % 2) * 0.30f,
                                                            Mathf.Sin(ang) * reach);
                glyph.transform.localRotation = Quaternion.Euler(0.0f, -ang * Mathf.Rad2Deg, 0.0f);

                VfxMaterial.Ghost(glyph.GetComponent<Renderer>(),
                                  new Color(1.00f, 0.84f, 0.34f, 0.92f), 0.60f);

                var fall = glyph.AddComponent<GlyphSettle>();
                fall.Delay = i * 0.035f;
            }

            // ⚠️ 0.75 s BECAME 1.55 s, FOR THE REASON `RiftOpen.Life` CARRIES. This is the half
            // of the blink the ARRIVING player is looking straight at, and six falling runes that
            // are gone in three quarters of a second read as a flicker rather than as writing.
            // It outlives the tear at the departure by 0.25 s deliberately: the two ends are one
            // gesture and it should finish where she finished, not where she started.
            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.WitchScatter, 0.34f);
            Object.Destroy(go, 1.55f);
            return go;
        }

        /// <summary>
        /// One arrival glyph dropping to the ground and going out.
        ///
        /// ⚠️ EACH ONE IS ON ITS OWN CLOCK, offset by a few hundredths, which is the difference
        /// between six things landing and one thing landing six times. The same argument
        /// `HeroHazards.Burn` makes about trail drops shrinking toward their own end.
        /// </summary>
        public sealed class GlyphSettle : MonoBehaviour, Visual.IVfxTimeline
        {
            public float Delay;

            /// <summary>
            /// ⚠️ 0.40 s BECAME 1.15 s WITH THE PARENT'S LIFETIME. The last glyph starts at
            /// `Delay` 0.175 s, so the whole set finished at 0.575 s inside a 0.75 s object: the
            /// runes had faded out well before their own parent was destroyed and the arrival was
            /// over almost before the teleport had registered. See `RiftOpen.Life` for the report
            /// this answers. The parent now lives 1.55 s, which leaves the same small margin at
            /// the end rather than a gap in the middle.
            /// </summary>
            private const float Life = 1.15f;

            private float _elapsed;
            private Vector3 _from;
            private Renderer _renderer;
            private float _alpha = 1.0f;

            public float LifeSeconds => Life + Delay;

            private void Awake()
            {
                _from = transform.localPosition;
                _renderer = GetComponent<Renderer>();
                if (_renderer != null) _alpha = _renderer.sharedMaterial.color.a;
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                float t = Mathf.Clamp01((_elapsed - Delay) / Life);
                if (t <= 0.0f) return;

                // Accelerating fall, so it arrives rather than drifts.
                var to = new Vector3(_from.x * 0.35f, 0.14f, _from.z * 0.35f);
                transform.localPosition = Vector3.Lerp(_from, to, t * t);

                if (_renderer != null && _renderer.sharedMaterial != null)
                {
                    var c = _renderer.sharedMaterial.color;
                    c.a = _alpha * (1.0f - t * t);
                    _renderer.sharedMaterial.color = c;
                }
            }
        }

        /// ⚠ IT RETURNS THE OBJECT, AND IT USED TO RETURN void. Every other spawner in this
        /// file hands back what it made, and `AbilityShowcaseProbe.Solo` needs that to sweep the
        /// effect up before the next capture: an effect it cannot collect survives into the NEXT
        /// frame and quietly appears in a shot that is supposed to show one ability.
        /// <summary>
        /// GRAND COVEN ECLIPSE. The sky goes out, and something opens in it.
        ///
        /// ⚠️⚠️ IT IS NOT A FLOOR CIRCLE ANY MORE AND THAT IS THE WHOLE REDESIGN. Every version
        /// of this ability so far has been a mark on the road: the merged one was a filled disc
        /// at **78.5 m², 40 per cent of the box** (`docs/TODO.md` § 21.2), and the version that
        /// replaced it was a heptagram, which was twelve times cheaper and still the same kind of
        /// object as her skill. 🧑 2026-08-26: *"can we make her ult cooler? on top of magic and
        /// shit / i want the sky to look ominous and shit and change for a brief moment into
        /// night and filled with magic"*.
        ///
        /// ⚠️⚠️ SO THE ULTIMATE'S FOOTPRINT IS ZERO SQUARE METRES OF FLOOR. `docs/VISION.md` § 2
        /// rule 2 allows an ultimate to be big and rule 5 requires the frame to still show the
        /// lata, the chalk and every player; an effect that lives in the SKY satisfies both
        /// without a trade, which is the argument `Visual.UltimateColumn` already made for going
        /// up. The only thing this puts on the road is a thin `Collar` at the reach, so the power
        /// still says how far it goes.
        ///
        /// ⚠️ THE THREE PARTS ARE THREE DIFFERENT OBJECTS BUILT THREE WAYS: the weather is
        /// `Visual.SkyEvent` (no geometry at all), the eclipse is a `Corona` with an empty middle
        /// hung face-down, and the reach is a `Collar`. Not one of them is the ward, and none of
        /// them is a star polygon.
        /// </summary>
        public static GameObject SpawnGrandCovenEclipse(Vector3 position, float radius = 5.0f,
                                                        float duration = 5.0f)
        {
            var go = new GameObject("GrandCovenEclipseEffect");
            go.transform.position = position;

            // ⚠️⚠️ THE WEATHER USED TO BE PLAYED FROM HERE AND THAT CALL IS DELETED, BECAUSE IT
            // WAS THE SECOND ONE AND IT WAS SHORTENING THE FIRST. `docs/TODO.md` § 26 moved the
            // sky to `HeroAbilitySystem.PlayUltimatePresentation`, the single point every
            // ultimate in the game passes through, precisely so there would be one place; this
            // line survived that move and became a duplicate nobody noticed, because both calls
            // asked for the same LOOK.
            //
            // ⚠️⚠️ THE WIND-UP IS WHAT MADE IT A BUG RATHER THAN A REDUNDANCY. `HeroAbility`
            // roots the caster for `UltimateWindup` 0.4 s and runs `OnActivate` at the END of it,
            // so the order is: the presentation starts the sky for its full length, and 0.4 s
            // later THIS line restarted the same weather at the ability's own raw `duration`.
            // `SkyEvent.Begin` zeroes `_elapsed` on every call ("a second cast should look like a
            // second event"), so the eclipse cut its own sky short every single time it was cast.
            // With `SkyEvent.SecondsFor` now adding a 3.20 s fall the gap is 3.2 s of weather,
            // which is most of what 🧑 was reporting as *"u dont even notice it"*.

            // ⚠️ 11 M UP, WHICH IS ABOVE EVERYTHING AND UNDER THE GUIDEWAY. Ilalim ng Tulay has a
            // deck over the street, so an eclipse at 40 m would be behind the map on the one arena
            // it was designed for. Low enough to be under the bridge and high enough that no
            // player, pillar or barricade can reach it.
            const float Height = 11.0f;

            var hung = new GameObject("Eclipse");
            hung.transform.SetParent(go.transform, false);
            hung.transform.localPosition = new Vector3(0.0f, Height, 0.0f);

            var corona = VfxShapes.Lay(hung.transform, "Corona",
                                       VfxShapes.Corona(24, 0.62f, 0.45f, 11),
                                       radius * 0.62f, 0.0f);

            // ⚠️ GOLD, WITH NO MAGENTA IN IT. Her skills are magenta rules with gold writing; if
            // the ultimate were magenta too, the only thing separating the three would be size
            // again, which is the fault this whole pass exists to remove. A corona is the colour
            // of a sun's edge and hers is the one that burns.
            VfxMaterial.Ghost(corona.GetComponent<Renderer>(),
                              new Color(1.00f, 0.78f, 0.26f, 0.95f), 1.30f);

            // ⚠️ THE MOON IS OPAQUE AND IT IS THE ONLY OPAQUE THING IN THE EFFECT. `docs/TODO.md`
            // § 19.2a's rule: two coplanar translucent plates sort arbitrarily, and a dark disc
            // ghosted over a bright corona is exactly that pair. It also has to actually OCCLUDE
            // to read as an eclipse, and an opaque renderer writes depth by construction rather
            // than by winning a sort. This costs no floor because it is eleven metres up.
            var moon = VfxShapes.Lay(hung.transform, "Moon",
                                     VfxShapes.Crystal(28, 0.0f),
                                     radius * 0.40f, -0.02f);

            // ⚠️⚠️ TURNED OVER, BECAUSE `Crystal` IS A FAN WOUND TO FACE UP AND THE AUDIENCE IS
            // UNDERNEATH IT. `Fan`'s note is the record of this exact fault costing a whole
            // capture pass: *"the object exists, the renderer is enabled, the material is correct
            // and the hierarchy looks right in every inspector; the shape is simply not in the
            // frame"*. Every other user of these builders lays them on the road and looks down at
            // them, so up is the right default and this is the first thing in the game hung over
            // the players' heads. `Corona` needs no equivalent: it goes through
            // `FacetedOriented` with a reference point ABOVE it, which turns every triangle for
            // the same reason without a transform.
            moon.transform.localRotation = Quaternion.Euler(180.0f, 0.0f, 0.0f);

            VfxMaterial.Solid(moon.GetComponent<Renderer>(), new Color(0.03f, 0.01f, 0.05f));
            VfxMaterial.StripCollider(moon);

            var glow = new GameObject("EclipseGlow");
            glow.transform.SetParent(hung.transform, false);
            var light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.00f, 0.74f, 0.30f);

            // ⚠️ 2.4 AT 26 M RATHER THAN SOMETHING BRIGHT AND CLOSE. `docs/TODO.md` § 8b is the
            // recorded cost of the other choice: Zack's ultimate ran a 6.0 point light over a
            // 17.5 m range in a 14 m box and blew **62.8 per cent** of the frame to white. This
            // one is eleven metres away from everything it lights, so its falloff across the
            // court is nearly flat and it reads as a sky rather than as a lamp.
            light.range = 26.0f;
            light.intensity = 2.4f;
            light.shadows = LightShadows.None;

            // The reach, on the ground. An annulus, because `docs/TODO.md` § 19.2's rule is that
            // a BOUNDARY has to be continuous to read as one: `Wedges` is for ground that is
            // genuinely in pieces.
            var reach = VfxShapes.Lay(go.transform, "EclipseReach",
                                      VfxShapes.Collar(48, 0.06f, 0.965f),
                                      radius, 0.03f);
            // ⚠️ PURPLE, NOT GOLD, WITH THE REST OF THE FLOOR. 🧑 2026-08-27: *"I DONT WANt GOLd
            // on dark i WANT PURPE OR PINK GLYPHS/MAGIC SHIT like in gravity falls"*. The corona
            // eleven metres up keeps its gold, so the sky and the ground are now different
            // colours, which is what stops the whole ultimate reading as one wash.
            VfxMaterial.Ghost(reach.GetComponent<Renderer>(),
                              new Color(UiTheme.HeroWitchBright.r, UiTheme.HeroWitchBright.g,
                                        UiTheme.HeroWitchBright.b, 0.70f), 0.75f);
            VfxMaterial.StripCollider(reach);

            // ⚠️ THE REACH IS A BOUNDARY, SO IT IS THE ONE PIECE THAT MUST NOT BREAK. It says how
            // far the ultimate goes; a boundary that vanishes where it crosses the kerb tells a
            // player the zone ends there, which is the opposite of true.
            VfxShapes.DrapeToGround(reach);

            // -------------------------------------------------------------------
            // § THE CIRCLE, WHICH IS WHAT HE ASKED FOR IN THE FIRST PLACE
            //
            // ⚠️⚠️ 🧑 2026-08-27: *"why dont i see a magic circle for phaister's ult? my idea for
            // it was that it would look like a giant magic circle with glyphs and patterns and
            // shit was cast on the whole battlefield for like 5 seconds"*. There was none: § 24
            // rebuilt her kit so that the WARD is the circle (`VfxShapes.WardCircle`) and the
            // ultimate is a CORONA hung eleven metres up, on the argument that three powers
            // needed three constructions. That argument is still right and this does not undo it:
            // the ultimate keeps its corona, its moon and its weather. What it was missing is the
            // thing on the FLOOR that says how far a battlefield-wide spell reaches.
            //
            // ⚠️⚠️ AND A NEAR-ARENA CIRCLE IS AFFORDABLE ONLY BECAUSE IT IS LINE ART.
            // `docs/VISION.md` § 2 rule 3 is the whole licence for this: *"spend the budget on
            // DETAIL, not on AREA. A flat coloured plane at 40 per cent of the arena reads as a
            // puddle"*. This paints almost no floor: rings, a written band and radial rules, with
            // the road showing through everywhere between them. That is why the same footprint
            // that would be forbidden as a disc is correct as an inscription.
            //
            // ⚠️ IT IS A DIFFERENT CIRCLE FROM THE WARD, NOT THE WARD MADE BIGGER, which is the
            // trap § 21.2 records against her whole kit (*"her Q is just 2 stars on top of each
            // other"*). The ward is a compact stamp with four rings and medallions; this is three
            // widely spaced rules with a glyph ring standing on them, and it turns. Scaling the
            // ward to 6.4 m would have been exactly the fault that pass existed to remove.
            // -------------------------------------------------------------------
            // ⚠️⚠️ AND IT IS BUILT IN STAGES, IN FRONT OF THE PLAYER, WHICH IS THE HALF THAT IS
            // NOT GEOMETRY. 🧑 2026-08-27: *"i just want it so that they see the stages of the
            // giant magic circle being cast for phaister ... like they see the circles being
            // constructed"*. An inscription that simply APPEARS is a decal; one that is drawn
            // ring by ring is a spell being cast, and the 0.4 s `HeroAbility.UltimateWindup` plus
            // the build below is the beat `Hero_Strike_Balance.md` § 4.3 asks an ultimate for:
            // *"the other three players get a beat to react"*. They now get something to react TO
            // rather than a flash and consequences.
            //
            // ⚠️ PURPLE, NOT GOLD, ON INSTRUCTION: *"or this but purple"*, against a reference
            // sheet of a gold circle. The corona overhead stays gold (see its note: a corona is
            // the colour of a sun's edge), so the two halves of the ultimate are no longer the
            // same colour, which separates the sky from the floor at a glance.
            var circle = new GameObject("CovenCircle");
            circle.transform.SetParent(go.transform, false);

            var build = circle.AddComponent<CovenCircleBuild>();
            build.Duration = duration;
            build.Radius = radius;
            build.Accent = UiTheme.HeroWitchBright;
            build.BuildRings();

            var anim = go.AddComponent<EclipseFall>();
            anim.Hung = hung.transform;
            anim.Corona = corona.transform;
            anim.Reach = reach.transform;
            anim.Glow = light;
            anim.Duration = duration;
            anim.RestHeight = Height;

            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.WitchEclipse, duration);
            Object.Destroy(go, duration);
            return go;
        }

        // -------------------------------------------------------------------
        // § THE COVEN CIRCLE, DRAWN IN STAGES
        //
        // ⚠️⚠️ 🧑 2026-08-27, having looked for it in a match and not found one: *"why dont i see
        // a magic circle for phaister's ult? my idea for it was that it would look like a giant
        // magic circle with glyphs and patterns and shit was cast on the whole battlefield"*,
        // then, decisively: *"i just want it so that they see the stages of the giant magic
        // circle being cast ... like they see the circles being constructed"*.
        //
        // ⚠️⚠️ THE STAGING IS THE FEATURE, NOT THE GEOMETRY. A finished inscription that appears
        // on one frame is a decal, and the eye files it as scenery. The same rings drawn one
        // after another over a second and a half are a SPELL BEING CAST, because a viewer reads
        // sequence as intent. It is also the only ultimate telegraph in the game that occupies
        // the whole build: `Hero_Strike_Balance.md` § 4.3 asks that *"the other three players get
        // a beat to react"*, and until now that beat was a 0.4 s root with a column over it.
        //
        // ⚠️⚠️ PINK AND VIOLET, NOT GOLD, AND HE SAID SO TWICE. *"or this but purple"* against a
        // gold reference sheet, and then *"I DONT WANt GOLd on dark i WANT PURPE OR PINK
        // GLYPHS/MAGIC SHIT like in gravity falls"*. `UiTheme.HeroWitch` is #e828c5 and
        // `HeroWitchBright` #f444d4, which are already her sigil colours, so this costs no new
        // palette. The corona hung overhead stays gold on purpose: a corona is the colour of a
        // sun's edge, and having the sky and the floor differ is what stops the ultimate reading
        // as one flat wash of a single hue.
        //
        // ⚠️ AND THE GLYPHS FLOAT. *"i want them to be floating and shit"*. They rise out of the
        // circle rather than lying in it, each one a different character now that
        // `VfxShapes.Rune` is a real alphabet rather than a stem with twigs on it. Twenty of them,
        // all distinct, which is the count he asked for: *"less glyphs ... BUt theyre all
        // different like 20 or so"*.
        //
        // ⚠️ THE WHOLE THING IS LINE ART AND THAT IS WHAT MAKES A NEAR-ARENA FOOTPRINT LEGAL.
        // `docs/VISION.md` § 2 rule 3: *"spend the budget on DETAIL, not on AREA"*. Almost all of
        // the road inside this circle is still road.
        // -------------------------------------------------------------------

        public sealed class CovenCircleBuild : MonoBehaviour, Visual.IVfxTimeline
        {
            public float Duration = 7.0f;
            public float Radius = 6.4f;
            public Color Accent = Color.magenta;

            /// <summary>How long the whole inscription takes to draw itself.</summary>
            private const float BuildSeconds = 1.55f;

            /// <summary>How long one layer takes to come in, once its turn arrives.</summary>
            private const float LayerFade = 0.30f;

            private readonly List<Transform> _layers = new List<Transform>();
            private readonly List<Renderer> _inks = new List<Renderer>();
            private readonly List<float> _alpha = new List<float>();
            private readonly List<Transform> _floaters = new List<Transform>();
            private readonly List<float> _floatPhase = new List<float>();
            private readonly List<Quaternion> _floatRest = new List<Quaternion>();

            private float _elapsed;

            /// <summary>
            /// Every ring, in the order they are drawn.
            ///
            /// ⚠️ THEY ARE SEPARATE OBJECTS RATHER THAN ONE MESH PRECISELY SO THEY CAN BE STAGED.
            /// `VfxShapes.WardCircle` builds a complete inscription in one mesh, which is correct
            /// for a stamp that appears at once (her Q) and useless here: a single renderer can
            /// only fade as a whole. One object per stage is what buys the sequence.
            /// </summary>
            /// <summary>
            /// The inscription, PLACED BY HAND.
            ///
            /// ⚠️⚠️ 🧑 2026-08-27, after two procedural attempts: *"the circle u gave her ult
            /// looks so boring, give her something this complex ... and yes manually draw it
            /// instead of using some for loop and shit to generate everything"*, and bluntly:
            /// *"bcz ur script generates suck as fuckk"*. He is right and the reason is worth
            /// stating precisely, because it is the same lesson as `docs/TODO.md` § 19 in a new
            /// place.
            ///
            /// ⚠️⚠️ A `for` LOOP OVER `i / count * 2π` CAN ONLY EVER PRODUCE ROTATIONAL
            /// SYMMETRY, AND ROTATIONAL SYMMETRY IS WHAT MAKES A THING LOOK MACHINE-MADE. Nine
            /// identical medallions at nine even angles is a hubcap. Every reference inscription
            /// he has sent is deliberately UNEVEN: medallions of four different sizes at four
            /// different radii, two of them overlapping into a cluster, script that runs in arcs
            /// rather than all the way round, and figures rotated off the axis. None of that is
            /// expressible as a loop with an index in it, which is exactly why the loop version
            /// read as boring however many rings were added to it.
            ///
            /// ⚠️ SO THE COMPOSITION IS A TABLE, AND THE TABLE IS THE ART. The loops that remain
            /// draw ONE element from one row; they never decide where anything goes. If this needs
            /// to be richer later, add rows.
            /// </summary>
            public void BuildRings()
            {
                var ink = Accent;
                var pale = new Color(1.00f, 0.72f, 0.99f);
                var deep = new Color(0.80f, 0.32f, 0.94f);

                // -------------------------------------------------------------------
                // THE CONCENTRIC RULES. Hand-picked radii, deliberately unevenly spaced: two
                // tight pairs (a "band") and three lone rules. Even spacing reads as a target.
                // -------------------------------------------------------------------
                AddRing("Rule_00", VfxShapes.Collar(72, 0.05f, 0.992f), 1.000f, ink, 0.72f);
                AddRing("Rule_01", VfxShapes.Collar(72, 0.05f, 0.990f), 0.958f, ink, 0.62f);
                AddRing("Rule_02", VfxShapes.Collar(68, 0.05f, 0.988f), 0.742f, pale, 0.55f);
                AddRing("Rule_03", VfxShapes.Collar(68, 0.05f, 0.986f), 0.706f, pale, 0.45f);
                AddRing("Rule_04", VfxShapes.Collar(60, 0.05f, 0.984f), 0.512f, deep, 0.52f);
                AddRing("Rule_05", VfxShapes.Collar(52, 0.05f, 0.972f), 0.238f, ink, 0.66f);
                AddRing("Rule_06", VfxShapes.Collar(44, 0.05f, 0.955f), 0.150f, pale, 0.74f);

                // Ticks, only in the outer band, and only across two thirds of it.
                AddTickArc(30.0f, 200.0f, 26, 0.958f, 1.000f);
                AddTickArc(240.0f, 350.0f, 16, 0.958f, 1.000f);

                // -------------------------------------------------------------------
                // THE SCRIPT. Arcs, not rings: three runs of writing at three radii, each
                // starting and stopping somewhere chosen rather than wrapping all the way round.
                // -------------------------------------------------------------------
                AddScriptArc(18.0f, 168.0f, 17, 0.976f, 0.050f, 1401);
                AddScriptArc(196.0f, 338.0f, 15, 0.976f, 0.050f, 1601);
                AddScriptArc(104.0f, 286.0f, 14, 0.724f, 0.042f, 2203);
                AddScriptArc(0.0f, 360.0f, 11, 0.194f, 0.028f, 3307);

                // -------------------------------------------------------------------
                // THE MEDALLIONS. Nine, and no two are the same: four sizes, four radii, four
                // different figures inside them, plus one deliberate overlapping PAIR at 176°
                // and 188° which is the single most "hand-drawn" thing in the whole composition.
                // Angles in degrees, radius as a fraction of the rim, size as a fraction of it.
                // -------------------------------------------------------------------
                AddMedallion( 92.0f, 0.872f, 0.150f, 7, 3, ink);
                AddMedallion(140.0f, 0.846f, 0.092f, 0, 0, pale);
                AddMedallion(176.0f, 0.884f, 0.128f, 5, 2, ink);
                AddMedallion(188.0f, 0.806f, 0.078f, 3, 1, deep);
                AddMedallion(232.0f, 0.858f, 0.116f, 6, 2, pale);
                AddMedallion(276.0f, 0.878f, 0.148f, 8, 3, ink);
                AddMedallion(312.0f, 0.822f, 0.084f, 0, 0, deep);
                AddMedallion(348.0f, 0.866f, 0.108f, 4, 1, pale);
                AddMedallion( 40.0f, 0.836f, 0.096f, 3, 1, ink);

                // -------------------------------------------------------------------
                // THE SPOKES. Five, not nine, and at hand-picked angles that DO NOT all line up
                // with medallions: three of them run to a medallion and two run into empty band,
                // which is what stops the figure reading as a wheel.
                // -------------------------------------------------------------------
                AddSpoke( 92.0f, 0.238f, 0.720f, ink);
                AddSpoke(176.0f, 0.238f, 0.756f, ink);
                AddSpoke(276.0f, 0.238f, 0.730f, ink);
                AddSpoke( 16.0f, 0.512f, 0.706f, deep);
                AddSpoke(212.0f, 0.512f, 0.706f, deep);

                // -------------------------------------------------------------------
                // THE FIGURES. Three, at three scales, three point counts and three ROTATIONS,
                // none of which is a multiple of another. A star repeated at three radii is the
                // "one builder at three sizes" fault § 24 rebuilt her whole kit to remove.
                // -------------------------------------------------------------------
                AddFigure("Fig_Outer", VfxShapes.Sigil(8, 3, 0.018f, 0.90f, 0, 52, 617),
                          0.672f, 14.0f, ink, 0.52f);
                AddFigure("Fig_Mid", VfxShapes.Sigil(6, 2, 0.022f, 0.88f, 0, 44, 811),
                          0.448f, -27.0f, deep, 0.58f);
                AddFigure("Fig_Core", VfxShapes.Sigil(3, 1, 0.030f, 0.84f, 0, 36, 907),
                          0.212f, 63.0f, pale, 0.64f);

                AddFloatingGlyphs(24);
            }

            /// <summary>One medallion: a small ring with its own figure in it, or empty.</summary>
            private void AddMedallion(float angleDeg, float at, float size,
                                      int points, int skip, Color colour)
            {
                var holder = new GameObject("Medallion");
                holder.transform.SetParent(transform, false);

                float a = angleDeg * Mathf.Deg2Rad;
                Vector3 where = OnGround(transform.position
                                + new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a)) * Radius * at
                                + Vector3.up * 0.020f);

                var ring = VfxShapes.Lay(holder.transform, "Ring",
                                         VfxShapes.Collar(28, 0.05f, 0.90f),
                                         Radius * size, 0.020f);
                ring.transform.position = where;
                Ink(ring, colour, 0.60f, 0.85f);

                // ⚠️ AN EMPTY MEDALLION IS A DELIBERATE ROW, NOT A MISSING ONE. Two of the nine
                // carry `points` 0. A composition in which every cell is filled is as mechanical
                // as one in which every cell is identical; the reference has plain circles in it
                // too, and they are what let the eye rest between the busy ones.
                if (points < 3) { Register(holder.transform, null, 0.0f); return; }

                var fig = VfxShapes.Lay(holder.transform, "Figure",
                                        VfxShapes.Sigil(points, skip, 0.055f, 0.80f, 0, 26,
                                                        700 + points * 31 + (int)angleDeg),
                                        Radius * size * 0.78f, 0.021f);
                fig.transform.position = where + Vector3.up * 0.001f;
                Ink(fig, colour, 0.66f, 1.05f);

                Register(holder.transform, null, 0.0f);
            }

            /// <summary>One radial rule, from one radius to another, at one angle.</summary>
            private void AddSpoke(float angleDeg, float from, float to, Color colour)
            {
                var holder = new GameObject("Spoke");
                holder.transform.SetParent(transform, false);

                float a = angleDeg * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a));

                var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bar.name = "Bar";
                bar.transform.SetParent(holder.transform, false);
                bar.transform.position = OnGround(transform.position
                                         + dir * Radius * (from + to) * 0.5f
                                         + Vector3.up * 0.0185f);
                bar.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                bar.transform.localScale = new Vector3(0.035f, 0.004f, Radius * (to - from));

                Ink(bar, colour, 0.40f, 0.80f);
                Register(holder.transform, null, 0.0f);
            }

            /// <summary>One figure, laid flat and turned to its own angle.</summary>
            private void AddFigure(string name, Mesh mesh, float scale, float turnDeg,
                                   Color colour, float alpha)
            {
                var go = VfxShapes.Lay(transform, name, mesh, Radius * scale, 0.019f);
                go.transform.localRotation = Quaternion.Euler(0.0f, turnDeg, 0.0f);
                Ink(go, colour, alpha, 0.85f);
                VfxShapes.DrapeToGround(go);
                Register(go.transform, null, 0.0f);
            }

            /// <summary>Ticks across the band, over one arc only.</summary>
            private void AddTickArc(float fromDeg, float toDeg, int count, float inner, float outer)
            {
                var holder = new GameObject("Ticks");
                holder.transform.SetParent(transform, false);

                for (int i = 0; i < count; i++)
                {
                    float f = count == 1 ? 0.0f : i / (float)(count - 1);
                    float a = Mathf.Lerp(fromDeg, toDeg, f) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a));

                    var tick = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tick.name = "Tick";
                    tick.transform.SetParent(holder.transform, false);
                    tick.transform.position = OnGround(transform.position
                                              + dir * Radius * (inner + outer) * 0.5f
                                              + Vector3.up * 0.019f);
                    tick.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
                    tick.transform.localScale = new Vector3(0.042f, 0.004f,
                                                            Radius * (outer - inner));
                    Ink(tick, Accent, 0.52f, 0.75f);
                }

                Register(holder.transform, null, 0.0f);
            }

            /// <summary>
            /// A run of writing along an arc, each character a different letter.
            ///
            /// ⚠️ IT IS AN ARC RATHER THAN A RING, WHICH IS THE WHOLE DIFFERENCE. Text that wraps
            /// all the way round with no beginning is a pattern; text that starts somewhere and
            /// stops somewhere is writing, and the reference has four separate runs of it at
            /// different radii.
            /// </summary>
            private void AddScriptArc(float fromDeg, float toDeg, int count, float at,
                                      float size, int seed)
            {
                var holder = new GameObject("Script");
                holder.transform.SetParent(transform, false);

                for (int i = 0; i < count; i++)
                {
                    float f = count == 1 ? 0.0f : i / (float)(count - 1);
                    float deg = Mathf.Lerp(fromDeg, toDeg, f);
                    float a = deg * Mathf.Deg2Rad;

                    var go = VfxShapes.Lay(holder.transform, "Char",
                                           VfxShapes.Rune(seed + i * 23),
                                           Radius * size, 0.019f);

                    go.transform.position = OnGround(transform.position
                                            + new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a))
                                              * Radius * at
                                            + Vector3.up * 0.019f);
                    go.transform.rotation = Quaternion.Euler(90.0f, -deg + 90.0f, 0.0f);

                    Ink(go, new Color(1.00f, 0.68f, 0.99f), 0.76f, 1.35f);
                }

                Register(holder.transform, null, 0.0f);
            }

            /// <summary>
            /// Lifts one piece of the inscription onto whatever is actually under it.
            ///
            /// ⚠️⚠️ THE CIRCLE IS 12.8 M ACROSS AND THE STREET IS NOT FLAT. Every piece used to
            /// be placed at the CASTER'S height, so on Ilalim ng Tulay everything past the kerb was
            /// buried under the pavement and the inscription ended in a hard straight line. 🧑
            /// 2026-08-27: *"her magic circle doesnt draw over the sidewalk and thats weird af"*.
            ///
            /// ⚠️ SNAPPED, NOT DRAPED, AND ONLY FOR THE SMALL PIECES. A medallion is 30 cm across
            /// and a tick is 4 cm wide, so the ground under one is flat at its own scale and moving
            /// it whole costs one ray. The RULES are metres across and cannot be snapped; they are
            /// bent vertex by vertex in `AddRing`.
            /// </summary>
            private Vector3 OnGround(Vector3 flat)
            {
                flat.y = VfxShapes.GroundAt(flat, flat.y);
                return flat;
            }

            /// <summary>Colour one piece and record it so the whole figure can dim together.</summary>
            private void Ink(GameObject go, Color colour, float alpha, float emission)
            {
                var r = go.GetComponent<Renderer>();
                if (r == null) return;

                VfxMaterial.Ghost(r, new Color(colour.r, colour.g, colour.b, alpha), emission);
                VfxMaterial.StripCollider(go);
                _inks.Add(r);
                _alpha.Add(alpha);
            }

            private void AddRing(string name, Mesh mesh, float scale, Color colour, float alpha)
            {
                var go = VfxShapes.Lay(transform, name, mesh, Radius * scale, 0.018f);

                var r = go.GetComponent<Renderer>();
                VfxMaterial.Ghost(r, new Color(colour.r, colour.g, colour.b, alpha), 0.75f);
                VfxMaterial.StripCollider(go);

                // ⚠️⚠️ THE RULES ARE THE THING THAT WAS VISIBLY CUT OFF, so they are the thing that
                // is DRAPED rather than snapped. A 12.8 m ring crosses the kerb, so one height for
                // the whole ring cannot be right anywhere: it has to bend. 🧑 2026-08-27:
                // *"her magic circle doesnt draw over the sidewalk and thats weird af"*.
                // `VfxShapes.DrapeToGround` has the diagnosis and why the two easy fixes are worse.
                VfxShapes.DrapeToGround(go);

                Register(go.transform, r, alpha);
            }

            /// <summary>
            /// ⚠️ EVERY ONE IS A DIFFERENT SEED AND THEREFORE A DIFFERENT LETTER, which only
            /// became true when `VfxShapes.Rune` was rebuilt around a closed set of body FORMS.
            /// Before that, twenty seeds gave twenty drafts of one character, which is precisely
            /// what 🧑 reported.
            /// </summary>
            private void AddFloatingGlyphs(int count)
            {
                var holder = new GameObject("Glyphs");
                holder.transform.SetParent(transform, false);

                for (int i = 0; i < count; i++)
                {
                    float a = i / (float)count * Mathf.PI * 2.0f;
                    float ring = Radius * (i % 2 == 0 ? 0.78f : 0.50f);

                    var go = VfxShapes.Stand(holder.transform, "Glyph",
                                             VfxShapes.TwoSided(VfxShapes.Rune(1300 + i * 37)),
                                             0.34f, heightScale: 0.50f);

                    go.transform.position = OnGround(transform.position
                                            + new Vector3(Mathf.Cos(a), 0.0f, Mathf.Sin(a)) * ring
                                            + Vector3.up * 0.35f);
                    go.transform.rotation = Quaternion.Euler(0.0f, -a * Mathf.Rad2Deg, 0.0f);

                    // ⚠️⚠️ THEY CAME BACK BLACK IN `ability_coven_eclipse_v35.png` AND THAT IS THE
                    // SAME FAULT AS KURO'S. `Ghost` at emission 1.15 still lets the LIT term
                    // dominate on a mesh this thin, and these are upright slabs a few centimetres
                    // wide seen edge-on under an eclipse that has just darkened the whole street.
                    // Alpha 1.0 and a much hotter emission make them self-lit, which is the only
                    // thing that survives their own weather. `docs/TODO.md` § 31.5 records the
                    // identical mistake on the pet, three hours earlier in the same session.
                    var r = go.GetComponent<Renderer>();
                    VfxMaterial.Ghost(r, new Color(1.00f, 0.62f, 0.98f, 1.00f), 2.20f);
                    VfxMaterial.StripCollider(go);

                    _inks.Add(r);
                    _alpha.Add(1.00f);
                    _floaters.Add(go.transform);
                    _floatPhase.Add(i * 0.7f);
                    _floatRest.Add(go.transform.localRotation);
                }

                Register(holder.transform, null, 0.0f);
            }

            /// <summary>
            /// ⚠️⚠️ THE BUILT SCALE IS REMEMBERED AND GROWN BACK TO, RATHER THAN GROWN TO ONE.
            /// `VfxShapes.Lay` sizes a ring by WRITING ITS `localScale`, so a ring laid at 6.4 m
            /// is an object whose local scale is 6.4. Animating it toward `Vector3.one` therefore
            /// did not reveal it, it SHRANK it to a metre across:
            /// `ability_coven_eclipse_v34.png` came back with a 12.8 m inscription drawn as a
            /// 2 m doodle under the caster, while the medallions and glyphs (which are children
            /// of unscaled holders and keep their own placement) sat correctly out at 6 m with
            /// nothing joining them up.
            /// </summary>
            private readonly List<Vector3> _layerFull = new List<Vector3>();

            private void Register(Transform layer, Renderer ink, float alpha)
            {
                _layers.Add(layer);
                _layerFull.Add(layer.localScale);
                layer.localScale = Vector3.zero;

                if (ink != null)
                {
                    _inks.Add(ink);
                    _alpha.Add(alpha);
                }
            }

            public float LifeSeconds => Mathf.Max(0.3f, Duration);

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            /// <summary>
            /// ⚠️⚠️ AN `IVfxTimeline`, AND THE FIRST RENDER OF THIS EFFECT IS WHY.
            /// `ability_coven_eclipse_eye_v32.png` came back with **no circle in it at all**:
            /// `BuildRings` creates every layer at `localScale` zero and only `Update` grows
            /// them, and `AbilityShowcaseProbe.Solo` does not run `Update` or call `StepAll`.
            /// The probe photographed a finished, correct inscription in its rest pose, which is
            /// invisible, and that reads as the feature being broken rather than as the capture
            /// being wrong.
            ///
            /// ⚠️ THIS IS THE SAME TRAP `GhostPetCompanion.StepTo` EXISTS FOR and the one
            /// `docs/TODO.md` records against Kuro twice. **Anything that is built by an
            /// animation must be windable, or it cannot be reviewed against a picture**, and
            /// `CLAUDE.md` § 6.1 requires every iteration to be reviewed against a picture.
            /// </summary>
            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                // ⚠️ THE LAYERS ARE SPACED ACROSS `BuildSeconds` RATHER THAN GIVEN A FIXED GAP,
                // so adding a seventh ring re-times the whole sequence instead of making the
                // build longer than the beat it is supposed to fit inside.
                float step = _layers.Count > 0 ? BuildSeconds / _layers.Count : BuildSeconds;

                for (int i = 0; i < _layers.Count; i++)
                {
                    if (_layers[i] == null) continue;

                    float since = _elapsed - i * step;
                    float k = Mathf.Clamp01(since / LayerFade);

                    // Overshoot slightly and settle, so each ring lands rather than grows.
                    float e = k < 1.0f ? 1.0f - Mathf.Pow(1.0f - k, 3.0f) : 1.0f;
                    float pop = k < 1.0f ? 1.0f + Mathf.Sin(k * Mathf.PI) * 0.05f : 1.0f;

                    _layers[i].localScale = _layerFull[i] * (e * pop);
                }

                // ⚠️ THE WHOLE INSCRIPTION TURNS, SLOWLY, AND ONLY AFTER IT IS FINISHED. Turning
                // it while it is still being drawn would make the stages look like one object
                // spinning up rather than like separate rings arriving.
                //
                // ⚠️⚠️ IT IS SET FROM `_elapsed` RATHER THAN ACCUMULATED WITH `Rotate`, so a
                // wound frame lands where a played one would. An incremental `Rotate` is
                // invisible to `StepTo`: the probe would jump straight to the end time and the
                // object would still be at its birth angle, which is the same class of fault as
                // the zero-scale one this method's note records.
                float spin = _elapsed > BuildSeconds ? (_elapsed - BuildSeconds) * 6.0f : 0.0f;
                transform.localRotation = Quaternion.Euler(0.0f, spin, 0.0f);

                for (int i = 0; i < _floaters.Count; i++)
                {
                    if (_floaters[i] == null) continue;

                    float t = _elapsed * 1.6f + _floatPhase[i];
                    var p = _floaters[i].localPosition;
                    p.y = 0.35f + Mathf.Sin(t) * 0.16f + Mathf.Min(_elapsed, 2.0f) * 0.18f;
                    _floaters[i].localPosition = p;

                    _floaters[i].localRotation = _floatRest[i]
                        * Quaternion.Euler(0.0f, _elapsed * 22.0f, 0.0f);
                }

                // The last half second dims the ink so the circle does not simply vanish.
                float left = Duration - _elapsed;
                if (left >= 0.6f) return;

                float fade = Mathf.Clamp01(left / 0.6f);
                for (int i = 0; i < _inks.Count; i++)
                {
                    if (_inks[i] == null || _inks[i].sharedMaterial == null) continue;

                    var c = _inks[i].sharedMaterial.color;
                    c.a = _alpha[i] * fade;
                    _inks[i].sharedMaterial.color = c;
                }
            }
        }

        /// <summary>
        /// The eclipse arriving, breathing, and going.
        ///
        /// ⚠️⚠️ IT COMES DOWN OUT OF THE SKY, WHICH IS THE ONLY MOTION IN HER KIT THAT STARTS
        /// OFF-SCREEN. The ward is stamped onto the road, the rift opens at head height, and this
        /// falls from above the map into place. Three powers, three directions of travel: 🧑 asked
        /// for *"different animations"* and this is the axis that actually separates them, not
        /// the speed of a spin.
        ///
        /// ⚠️ THE CORONA TURNS AND THE MOON DOES NOT. A ring of teeth rotating slowly around a
        /// dead black disc is the difference between an eclipse and a logo, and it is one line
        /// rather than the two counter-rotating wheels `WitchSigilSpin` used on all three powers.
        /// </summary>
        public sealed class EclipseFall : MonoBehaviour, Visual.IVfxTimeline
        {
            public Transform Hung;
            public Transform Corona;
            public Transform Reach;
            public Light Glow;
            public float Duration = 5.0f;
            public float RestHeight = 11.0f;

            /// <summary>How long it takes to arrive. Slower than a skill: it is an event.</summary>
            private const float Arrive = 0.75f;

            /// <summary>Degrees per second. Slow enough to read as astronomical.</summary>
            private const float CoronaSpin = 9.0f;

            private const float FadeFrom = 0.86f;

            private float _elapsed;
            private float _glow = 2.4f;
            private Vector3 _reachScale = Vector3.one;
            private Renderer _reachRenderer;
            private float _reachAlpha = 1.0f;

            public float LifeSeconds => Mathf.Max(0.3f, Duration);

            private void Awake()
            {
                if (Glow != null) _glow = Glow.intensity;

                if (Reach != null)
                {
                    _reachScale = Reach.localScale;
                    _reachRenderer = Reach.GetComponent<Renderer>();
                    if (_reachRenderer != null)
                        _reachAlpha = _reachRenderer.sharedMaterial.color.a;
                }
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                float arrive = Mathf.Clamp01(_elapsed / Arrive);
                float eased = 1.0f - (1.0f - arrive) * (1.0f - arrive);

                if (Hung != null)
                {
                    // Down from twice the rest height, and it grows into place: something
                    // enormous coming closer, rather than something small being turned on.
                    Hung.localPosition = new Vector3(
                        0.0f, Mathf.Lerp(RestHeight * 2.1f, RestHeight, eased), 0.0f);
                    Hung.localScale = Vector3.one * Mathf.Lerp(0.45f, 1.0f, eased);
                }

                if (Corona != null)
                    Corona.localRotation = Quaternion.Euler(0.0f, CoronaSpin * _elapsed, 0.0f);

                // The reach sweeps out under it as it lands, so the ground mark is caused by the
                // thing in the sky rather than being a second, separate announcement.
                if (Reach != null)
                {
                    float k = Mathf.Sqrt(arrive);
                    Reach.localScale = new Vector3(_reachScale.x * k, _reachScale.y,
                                                   _reachScale.z * k);
                }

                if (Glow != null) Glow.intensity = _glow * eased;

                float t = Mathf.Clamp01(_elapsed / LifeSeconds);
                if (t < FadeFrom) return;

                float fade = 1.0f - Mathf.InverseLerp(FadeFrom, 1.0f, t);
                if (Glow != null) Glow.intensity = _glow * fade;

                if (_reachRenderer != null && _reachRenderer.sharedMaterial != null)
                {
                    var c = _reachRenderer.sharedMaterial.color;
                    c.a = _reachAlpha * fade;
                    _reachRenderer.sharedMaterial.color = c;
                }
            }
        }

        public sealed class HexSigilComponent : MonoBehaviour
        {
            public float Radius = 2.4f;
            public float Duration = 6.0f;
            public int OwnerSlot = -1;
            private float _left;
            private readonly Dictionary<int, float> _nextHexBySlot = new Dictionary<int, float>();

            private void Start() => _left = Duration;

            private void Update()
            {
                _left -= Time.deltaTime;
                if (_left <= 0.0f)
                {
                    Object.Destroy(gameObject);
                    return;
                }

                // Slow rotation on occult hex circle
                transform.Rotate(Vector3.up, 18.0f * Time.deltaTime);

                if (!NetAuthority.ShouldResolve()) return;
                var round = GameServices.Round;
                if (round == null) return;

                foreach (var p in round.Players)
                {
                    if (p == null || p.PlayerSlot == OwnerSlot) continue;

                    Vector3 diff = p.transform.position - transform.position;
                    diff.y = 0.0f;
                    if (diff.magnitude <= Radius)
                    {
                        // Apply hex curse: reduce speed and stagger on intervals
                        p.ApplyImpulse(-p.Velocity.normalized * 3.5f * Time.deltaTime);

                        if (CanPulse(_nextHexBySlot, p.PlayerSlot, 1.10f))
                        {
                            p.ApplyStagger(0.35f);
                            ComicPopup.Spawn(p.transform.position + Vector3.up * 1.2f, "HEXED!", UiTheme.HeroWitchBright, 1.0f);

                            // ⚠ THE VICTIM GETS A DIFFERENT CUE FROM THE CAST, and it falls
                            // rather than rises. Every other on-hit sound in this game is an
                            // impact; a curse is not struck, it SETTLES. It also fires once per
                            // victim per 1.1 s, which is why it is mixed ten down: four people in
                            // one circle must not stack into a wall.
                            //
                            // ⚠️ `NetCue`, SAME AS THE OTHER TWO HAZARDS. It is behind the host
                            // gate at the top of this Update, so on the wire it was a curse that
                            // only the host could hear settling.
                            NetCue.Play("sfx_hex_afflict", p.transform.position);
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // THUNDERSTRIKE OVERDRIVE (Zack Ultimate)
        // -------------------------------------------------------------------
        // -------------------------------------------------------------------
        // ⚠️⚠️ `SpawnWitchSigil`, `WitchSigilSpin` AND `SpawnCastGlyph` WERE DELETED HERE ON
        // 2026-08-26, AND THE DELETION IS THE FIX RATHER THAN A TIDY-UP.
        //
        // The three of them were the ONE function all three of Phaister's powers were drawn by.
        // It laid `VfxShapes.Sigil` twice, an outer star polygon over an inner one, turned them
        // against each other, and every caller passed the same `5, 2`: so her hex, both ends of
        // her blink and her ultimate were the same pentagram stacked on itself at four radii.
        // 🧑, off the played build: *"her Q is just 2 stars on top of each other"*, and *"pls
        // dont use the same script to generate any abilitiy as it will feel cheap and it will
        // look all the same"*.
        //
        // ⚠️ LEAVING THEM IN PLACE UNUSED WOULD HAVE PUT THEM BACK. `docs/TODO.md` § 22 opens on
        // the pattern that cost this project two sessions: *"an entry marked closed is not
        // evidence the code changed ... grep for the call site, not the asset"*. A helper named
        // `SpawnWitchSigil` sitting in the file that every witch effect is written in is a helper
        // the next hero power will be built out of, whatever any comment says.
        //
        // What replaced them is above: `SpawnHexSigil` on `VfxShapes.WardCircle`,
        // `SpawnShadowRift` plus `SpawnShadowArrival` on `VfxShapes.Rift` and `Rune`, and
        // `SpawnGrandCovenEclipse` on `VfxShapes.Corona` and `Visual.SkyEvent`. `VfxShapes.Sigil`
        // itself survives in that file and is now used by nothing.
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

            // ⚠️ ONE SEED FOR BOTH HALVES OF THE STRIKE, declared before either uses it. The
            // ground star and the column above it are the same discharge, so they are generated
            // from the same number and a capture of one strike is reproducible.
            int boltSeed = Mathf.RoundToInt((position.x + position.z * 3.0f) * 411.0f);

            // 3. Expanding Electric Ground Shockwave
            // ⚠️ ZACK'S OWN SILHOUETTE, not a cylinder. § 8.3 gives the discharge STAR to
            // lightning because it arrives at a point and runs out along the ground, and his
            // Bolt Sprint trail has used it since the silhouette pass. His ultimate was still
            // drawing the circle the trail had already stopped drawing.
            var shockRing = VfxShapes.Lay(null, "ThunderShockRing",
                                          VfxShapes.Star(9, 0.46f, boltSeed), 0.5f, 0.0f);
            shockRing.transform.position = position + Vector3.up * 0.04f;
            VfxMaterial.Ghost(shockRing.GetComponent<Renderer>(), UiTheme.HeroElectric, 0.8f);

            // 3b. The ionisation column.
            //
            // ⚠️⚠️ THIS WAS A FLAT `Cylinder` DISC, ON THE ONE ABILITY WHOSE ENTIRE FICTION IS
            // THE VERTICAL AXIS. `Hero_Strike_Balance.md` § 8.4: *"horizontal versus vertical is
            // a bigger difference than any two outlines on the same plane"*, which is why the
            // void was lifted rather than reshaped. Thunderstrike drops a bolt from 24 m up and
            // then marked the ground with another circle, so the moment it landed it became
            // indistinguishable from every other blast in the game.
            //
            // ⚠️ IT IS SHORT AND WIDE RATHER THAN TALL. `VISION.md` § 2 rule 5 says a mid-fight
            // frame must still show the lata, the chalk and every player: a full-height pillar at
            // the strike point would hide a body in a 14 m box. 1.6 m is over head height for the
            // read and under the sightline that matters, and it lives 0.2 s.
            var ionCore = new GameObject("ThunderIonCore");
            ionCore.transform.position = position + Vector3.up * 0.045f;
            ionCore.transform.localScale = new Vector3(radius * 0.34f, 1.6f, radius * 0.34f);
            Mesh spire = VfxShapes.Spire(7, 0.20f, 0.34f, boltSeed);
            ionCore.AddComponent<MeshFilter>().sharedMesh = spire;
            ionCore.AddComponent<MeshRenderer>();
            VfxShapes.Own(ionCore, spire);

            // ⚠️ PULLED OFF WHITE, FOR THE REASON ON THE FLASH ABOVE. `(1, 1, 0.60)` at full
            // emission is the brightest surface in the game, and it sat in the middle of the
            // brightest light in the game. Keeping the hue and dropping the value leaves the
            // ionisation reading as the hottest thing on screen without taking the frame with it.
            VfxMaterial.Ghost(ionCore.GetComponent<Renderer>(), new Color(1.0f, 0.96f, 0.42f, 0.72f), 0.7f);
            Object.Destroy(ionCore, 0.20f);

            var ringAnim = shockRing.AddComponent<ShockwaveRingAnim>();
            ringAnim.TargetRadius = radius * 1.5f;
            Object.Destroy(shockRing, 0.45f);

            // 3. The flash.
            //
            // ⚠️⚠️ IT WAS BLOWING OUT THE ENTIRE STREET, AND IT TOOK THE FIRST TRANSIENT
            // CAPTURE TO SEE IT. Measured on `ability_blast_thunder_v9.png`: **62.8 per cent of
            // the overhead frame and 49.9 per cent of the eye-height frame were at or above
            // 245/255 luminance**, against 8.3 per cent for the worst of every other effect in
            // the set and 3.0 per cent for the empty street. `docs/VISION.md` § 2 rule 5 asks
            // that a mid-fight frame still show the lata, the chalk and every player; in that
            // frame the road markings themselves are gone.
            //
            // ⚠️ THE OLD NUMBERS WERE THE LOUDEST IN THE GAME BY A DISTANCE and nothing in the
            // repo said why. Intensity 6.0 over a 17.5 m range in a 14 m box, against the fire
            // blast's 5.5 over 12.5 m, so Zack's ultimate lit the whole arena about twice as
            // hard as Sean's did from half again the distance. These land it inside the family:
            // brightest of the five, and still a frame you can play through.
            //
            // ⚠️ THE RANGE IS TIED TO THE BLAST RADIUS, not to the map, so a future retune of
            // the ultimate's footprint carries its own light with it.
            var lightGo = new GameObject("ThunderLight");
            lightGo.transform.position = position + Vector3.up * 2.0f;
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroElectricBright;
            light.range = radius * 1.6f;
            light.intensity = 3.0f;
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

            if (!NetAuthority.ShouldResolve()) return;
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
                        // ⚠️ 7. Zack's Thunderstrike is an ultimate, but a shock locks
                        // rather than encases: it is the one element in `StunCoat` drawn almost
                        // entirely on the rim, and the escape is priced to match.
                        p.ApplyStagger(2.0f, StunElement.Shock, 7);
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
        /// <summary>
        /// What an explosion of a given style leaves ON the people it catches.
        ///
        /// ⚠️⚠️ IT IS A MAP AND NOT A FIELD ON THE ENUM BECAUSE THE TWO ANSWER DIFFERENT
        /// QUESTIONS. `ExplosionStyle` says how the blast is DRAWN; `StunElement` says how the
        /// victim is drawn and whether they can fight out. They agree today for every style,
        /// and the day a style wants a blast that leaves nothing behind, this is the one line
        /// that changes rather than the enum every caller names.
        /// </summary>
        private static StunElement ElementForStyle(ExplosionStyle style)
        {
            switch (style)
            {
                case ExplosionStyle.Quake: return StunElement.Stone;
                case ExplosionStyle.Frost: return StunElement.Ice;

                // ⚠️ THE SLIPPER IS `None` ON PURPOSE. It is "the joke rather than an
                // ultimate" in this enum's own words, and a thrown tsinelas that encased
                // somebody would be the single least readable thing in the game.
                case ExplosionStyle.Slipper: return StunElement.None;

                default: return StunElement.Fire;
            }
        }

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
            public readonly Color CoreColour;
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
                CoreColour = core; Edge = edge; Cue = cue; HasCore = hasCore;
                DebrisCount = debrisCount; DebrisSize = debrisSize; DebrisSpeed = debrisSpeed;
                DebrisLift = debrisLift; DebrisLife = debrisLife;
                FlashIntensity = flashIntensity; FlashSeconds = flashSeconds;
                ShakeAmount = shakeAmount; ShakeSeconds = shakeSeconds;
            }

            /// <summary>
            /// The BODY of the blast, generated rather than primitive. Null means this style has
            /// no core at all, which is a real answer: a thrown tsinelas has no ball of anything.
            /// </summary>
            public Mesh Core(int seed)
            {
                if (!HasCore) return null;

                switch (_style)
                {
                    // ⚠️ A FRONT, NOT A BALL. § 8.5 item 1 asks for "a shockwave with a FRONT",
                    // and for Dante it is a gameplay read too: Titan Fissure is cast 2.2 m AHEAD
                    // of him, so it is the one blast in the game that is AIMED, and a 360 degree
                    // ball said nothing about where. The arc points where it was thrown.
                    case ExplosionStyle.Quake:
                        return VfxShapes.Shockfront(155.0f, 14, 0.34f, 0.44f, 0.18f, seed);

                    // Ice keeps the hard, ordered faceting it has everywhere else in the kit:
                    // few rings, no roughness, so it reads as GROWN rather than blown apart.
                    case ExplosionStyle.Frost:
                        return VfxShapes.NovaShell(5, 9, 0.0f, seed);

                    // Fire is the same form roughened. A blast is radial but it is not TIDY.
                    default:
                        return VfxShapes.NovaShell(6, 10, 0.16f, seed);
                }
            }

            /// <summary>How high off the ground the core sits. A ground wave sits ON the road.</summary>
            public float CoreLift => _style == ExplosionStyle.Quake ? 0.04f : 0.6f;

            /// <summary>
            /// The vertical scale ceiling. ⚠️ ONLY THE GROUND WAVE HAS ONE: a shell is meant to
            /// grow in all three axes, and capping it would flatten a nova into a pancake.
            /// 0.9 holds Dante's rim near 0.30 m, which is a lip you can see over and step across
            /// rather than a wall standing in a 14 m box.
            /// </summary>
            public float CoreVerticalCap =>
                _style == ExplosionStyle.Quake ? 0.9f : float.PositiveInfinity;

            /// <summary>
            /// ⚠️ ONLY THE FRONT CARES WHICH WAY IT POINTS. A shell is radial, so yawing it
            /// would spend a transform on a rotation nobody can see.
            /// </summary>
            public float CoreYaw(Vector3 facing)
            {
                if (_style != ExplosionStyle.Quake) return 0.0f;

                facing.y = 0.0f;
                if (facing.sqrMagnitude < 0.0001f) return 0.0f;

                return Quaternion.LookRotation(facing.normalized, Vector3.up).eulerAngles.y;
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

                    // ⚠️ SEAN'S OWN BURST, not `SpawnCastFlash`. That flash is what every
                    // ability in the game plays at cast, so routing the ultimate's PAYLOAD to it
                    // would have made the biggest moment in his kit look like any cast.
                    default: Visual.AbilityVfx.SpawnFireBurst(at, radius); break;
                }
            }
        }

        private static ExplosionLook LookFor(ExplosionStyle style)
        {
            switch (style)
            {
                case ExplosionStyle.Quake:
                    // ⚠️ `hasCore` IS NOW TRUE AND THE CORE IS NOT A BALL. It was false because
                    // the only core available was a fireball and a quake has no fire in it.
                    // `VfxShapes.Shockfront` gave the style something a slam can actually have.
                    return new ExplosionLook(style, UiTheme.HeroMagmaCore, new Color(0.55f, 0.40f, 0.28f),
                        "sfx_quake_slam", hasCore: true, debrisCount: 14,
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

        /// <param name="facing">
        /// Which way the blast was aimed. ⚠️ Only the `Quake` front reads it; every other style
        /// is radial and ignores it. Leave it default for an unaimed blast.
        /// </param>
        public static void CreateExplosion(Vector3 center, float radius, float knockback, float stunTime,
            int sourceSlot, string comicText = "KABOOM!", ISet<int> excludedSlots = null,
            ExplosionStyle style = ExplosionStyle.Fire, Vector3 facing = default)
        {
            // ⚠️⚠️ THE PICTURE IS DRAWN BEFORE THE ROUND IS ASKED FOR, AND THAT ORDER IS THE
            // POINT. This function opened with `if (round == null) return;`, so an explosion
            // outside a live match drew NOTHING — which is every context the harness has.
            // `AbilityShowcaseProbe` runs in edit mode with no match, so the biggest effects in
            // the game were the ones it could never photograph, which is `docs/TODO.md` § 8
            // item 2. Splitting the visual out costs one call and makes the whole § 8 pass
            // reviewable against pictures instead of against prose. In play nothing changes:
            // a live match always has a round.
            CreateExplosionVisual(center, radius, comicText, style, facing);

            if (!NetAuthority.ShouldResolve()) return;
            var round = GameServices.Round;
            if (round == null) return;

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
                        // ⚠️ THE ELEMENT COMES FROM THE STYLE THE CALLER ALREADY PASSED,
                        // which is why this needed no new parameter. `ExplosionStyle` is
                        // already the caller saying what its blast is MADE OF, and that is
                        // exactly the question `StunElement` asks. Reading it here means every
                        // present and future explosion is wired the day it is written.
                        //
                        // ⚠️ AND A SUB-FLOOR `stunTime` IS FORCED BACK TO `None` INSIDE
                        // `ApplyStagger`, so the small slipper blast does not dress as a hold.
                        p.ApplyStagger(stunTime, ElementForStyle(style),
                                       Balance.StunBreakPressesDefault);
                        DizzyStars.Attach(p.transform, stunTime, UiTheme.HeroFireBright);
                    }
                }
            }

            // -------------------------------------------------------------------
            // ⚠️⚠️ A BLAST NEVER KNOCKS OVER ITS OWN CASTER'S OBJECTIVE, AND THAT ONE LINE IS
            // WHAT MAKES THREE ULTIMATES USABLE IN BOTH ROLES.
            //
            // 🧑 2026-08-27, reading Sean's card in a match: *"this too it reads as unusable on
            // defender"*, above *"Leap and crash down. Knocks the lata over on impact."* He is
            // right, and it was not only the wording: the taya's whole job is that the lata stays
            // up, so an ultimate whose headline effect is knocking it over is an ultimate a
            // defending Sean must never press. `AIController.StepHeroAbilities` already encodes
            // that as a special case (*"a defending Sean must never spend an ultimate knocking
            // over their own objective"*), which is the tell: when the AI needs a rule to stop it
            // using a power, the power is not finished.
            //
            // ⚠️ THE GATE IS THE CASTER'S ROLE, NOT THE HERO. Any explosion from any source now
            // obeys it, so a future kit cannot reintroduce the same trap, and an ATTACKER's blast
            // still knocks the can over exactly as before. What a defender gets instead is the
            // half that was always role-neutral: everyone near it thrown clear, and whatever the
            // caller leaves on the ground.
            //
            // ⚠️ A SEATLESS SOURCE (`sourceSlot` -1, which is a map hazard or a stray slipper)
            // COUNTS AS AN ATTACKER, because nobody is defending on its behalf. That keeps
            // `Slipper`'s own explosion and every environmental blast behaving as they always
            // have.
            // -------------------------------------------------------------------
            if (round.Lata != null)
            {
                Vector3 canDiff = round.Lata.transform.position - center;
                canDiff.y = 0.0f;

                var caster = sourceSlot >= 0 ? round.PlayerAt(sourceSlot) : null;
                bool castersOwnCan = caster != null && caster.IsDefender;

                if (canDiff.magnitude <= radius && !castersOwnCan)
                {
                    round.Lata.HostKnockDown(sourceSlot);
                }
            }
        }

        /// <summary>
        /// Everything a blast PUTS ON SCREEN, with no dependency on there being a match.
        ///
        /// ⚠️ IT IS NOT A PREVIEW OR A STUB. This is the only code that draws an explosion;
        /// <see cref="CreateExplosion"/> calls it and then resolves the damage. See that
        /// function's note for why the two were separated.
        /// </summary>
        public static void CreateExplosionVisual(Vector3 center, float radius,
            string comicText = "KABOOM!", ExplosionStyle style = ExplosionStyle.Fire,
            Vector3 facing = default)
        {
            ExplosionLook look = LookFor(style);

            // ⚠️ SEEDED OFF POSITION, for the reason `VfxShapes` gives: two blasts in different
            // places differ from each other, but a given blast is identical between captures and
            // `AbilityShowcaseProbe`'s renders stay comparable version to version.
            int seed = Mathf.RoundToInt((center.x - center.z) * 613.0f);

            // 1. The core. A fireball is a fire thing: a quake has no ball of flame in it and
            //    a slipper has none either, so the sphere is the style's to refuse.
            // ⚠️⚠️ THE CORE IS A GENERATED FACETED MESH, NOT `PrimitiveType.Sphere`. 🧑, after
            //    playing the styles: *"not all js spheres (its okay to have spheres but its ugly
            //    if they all are"*. Unity's sphere is smooth-shaded and near-perfectly round in a
            //    game whose cast and city are boxes, so it was both the same shape four times AND
            //    the wrong visual language. `VfxShapes.NovaShell` keeps the radius exactly (a
            //    nova IS radial, and squaring it off would lie about the danger, § 8.4) and
            //    spends the difference on facets that catch the key light one at a time.
            //    `VfxShapes.Shockfront` gives the quake a leading edge instead of a ball.
            Mesh coreMesh = look.Core(seed);
            if (coreMesh != null)
            {
                var vfx = new GameObject("ExplosionCore");
                vfx.transform.position = center + Vector3.up * look.CoreLift;
                vfx.transform.rotation = Quaternion.Euler(0.0f, look.CoreYaw(facing), 0.0f);
                vfx.transform.localScale = Vector3.one * (radius * 0.4f);

                vfx.AddComponent<MeshFilter>().sharedMesh = coreMesh;
                vfx.AddComponent<MeshRenderer>();
                VfxShapes.Own(vfx, coreMesh);

                VfxMaterial.Ghost(vfx.GetComponent<Renderer>(), look.CoreColour, 0.9f);

                var anim = vfx.AddComponent<ExplosionVfxAnim>();
                anim.TargetRadius = radius * 1.1f;

                // Every `VfxShapes` mesh is unit RADIUS. See `ExplosionVfxAnim.MeshRadius`.
                anim.MeshRadius = 1.0f;

                // Only the ground wave is capped, and only vertically. 0.9 keeps Dante's rim
                // around 0.30 m: a lip of broken road you can see over and step across.
                anim.MaxVerticalScale = look.CoreVerticalCap;

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
            light.color = look.CoreColour;
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
        private sealed class ExplosionVfxAnim : MonoBehaviour, Visual.IVfxTimeline
        {
            public float TargetRadius = 5.0f;

            /// <summary>
            /// ⚠️⚠️ UNIT RADIUS, NOT UNIT DIAMETER, AND GETTING THIS WRONG DOUBLES EVERY BLAST.
            /// This used to read `TargetRadius * 2.0f` and that was CORRECT for what it animated:
            /// `PrimitiveType.Sphere` is one unit ACROSS, so radius 0.5, so a scale of 2R gives a
            /// radius of R. Every shape `VfxShapes` generates is built at one unit of RADIUS
            /// instead, so the same line would have drawn a 4.8 m Supernova at 9.9 m and swallowed
            /// most of a 14 m arena. `VISION.md` § 2 exists because of exactly that failure.
            /// </summary>
            public float MeshRadius = 1.0f;

            /// <summary>
            /// ⚠️ A GROUND WAVE MUST NOT GROW INTO A WALL. The scale is uniform, so a
            /// `Shockfront` whose rim stands 0.34 units proud would reach 0.34 x 4.95 = 1.7 m on
            /// Dante's ultimate: over head height, in a box where `VISION.md` § 2 rule 5 requires
            /// that a mid-fight frame still show the lata, the chalk and every player. Capping
            /// only the Y lets the wave spread across the floor without ever hiding a body.
            /// </summary>
            public float MaxVerticalScale = float.PositiveInfinity;

            private readonly Fader _fade = new Fader();
            private float _elapsed;

            public float LifeSeconds => 0.5f;

            /// <summary>⚠️ THE PLAYER'S FRAME AND THE CAPTURE'S FRAME COME FROM THIS ONE BODY.
            /// See <see cref="Visual.IVfxTimeline"/>: a separate preview path would be a second
            /// answer to what an explosion looks like.</summary>
            public void StepTo(float seconds)
            {
                _elapsed = seconds;
                float t = Mathf.Clamp01(_elapsed / LifeSeconds);

                float wide = Mathf.Lerp(0.35f, TargetRadius / Mathf.Max(0.01f, MeshRadius),
                                        Mathf.Sqrt(t));
                float tall = Mathf.Min(wide, MaxVerticalScale);

                transform.localScale = new Vector3(wide, tall, wide);

                _fade.Apply(GetComponent<Renderer>(), Mathf.Lerp(0.85f, 0.0f, t));
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);
        }

        private sealed class ShockwaveRingAnim : MonoBehaviour, Visual.IVfxTimeline
        {
            public float TargetRadius = 6.0f;

            /// <summary>
            /// ⚠️⚠️ EVERY GROUND SHOCKWAVE IN THE GAME WAS DRAWN AT DOUBLE ITS INTENDED SIZE,
            /// FOR EXACTLY THE REASON `ExplosionVfxAnim.MeshRadius` IS WRITTEN UP TO PREVENT,
            /// AND IT WENT UNNOTICED BECAUSE NOTHING COULD PHOTOGRAPH IT.
            ///
            /// This line read `Mathf.Lerp(0.5f, TargetRadius * 2.0f, t)`. That was CORRECT while
            /// the ring was a `PrimitiveType.Cylinder`, which is one unit ACROSS: a scale of 2R
            /// on a mesh of radius 0.5 gives a radius of R. The § 8 silhouette pass replaced the
            /// cylinder with a `VfxShapes` mesh, and every one of those is built at one unit of
            /// RADIUS, so the same scale now gives a radius of 2R. The core's copy of this bug
            /// was caught and annotated at the time; the ring's was not.
            ///
            /// ⚠️ MEASURED OFF THE FIRST TRANSIENT CAPTURE, 2026-08-26. Sean's Supernova ring
            /// reached **26.9 m across** in a **14 m** box, and Zack's Thunderstrike star reached
            /// **42 m**, which whited out the entire street: `ability_blast_thunder_v8.png` is a
            /// frame in which the lata, the chalk and every player are gone, and that is
            /// precisely what `docs/VISION.md` § 2 rule 5 forbids.
            ///
            /// ⚠️ THIS IS A REGRESSION FIX AND NOT A BALANCE CHANGE. Restoring the divide puts
            /// the final radius back on `TargetRadius`, which is what the cylinder drew and what
            /// every footprint in `docs/Hero_Strike_Balance.md` § 1 was measured against.
            /// </summary>
            public float MeshRadius = 1.0f;

            private readonly Fader _fade = new Fader();
            private float _elapsed;

            public float LifeSeconds => 0.4f;

            public void StepTo(float seconds)
            {
                _elapsed = seconds;
                float t = Mathf.Clamp01(_elapsed / LifeSeconds);
                float r = Mathf.Lerp(0.25f, TargetRadius / Mathf.Max(0.01f, MeshRadius), t);
                transform.localScale = new Vector3(r, 0.02f, r);

                _fade.Apply(GetComponent<Renderer>(), Mathf.Lerp(0.8f, 0.0f, t));
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);
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
