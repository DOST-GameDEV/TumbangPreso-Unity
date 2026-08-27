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
        /// <summary>
        /// Kuro's projected body, for the seat that has no live <see cref="Visual.GhostPetCompanion"/>
        /// to possess.
        ///
        /// ⚠️⚠️ `lifetime` IS PASSED IN BECAUSE THIS OBJECT IS A HERO'S WAY HOME, NOT A HAZARD.
        /// It was hard-coded at 4.0 s beside an ability whose duration is 6.0 s, so on this path
        /// the ghost and its purple point light deleted themselves TWO SECONDS BEFORE the
        /// ability ended. 🧑, 2026-08-27: *"dont make nemu pet aura disappear (purple light)
        /// until she comes back"*.
        ///
        /// ⚠️⚠️ AND THE LIGHT GOING OUT WAS THE VISIBLE HALF OF A WORSE BUG.
        /// `GhostlyPoltergeistAbility.OnEnd` teleports Nemu onto this object and does nothing at
        /// all when it is already gone, so every one of those runs left her standing where she
        /// cast from with no trip home and no explanation. The aura vanishing was the player
        /// watching the return anchor be destroyed.
        ///
        /// ⚠️ THE CALLER PASSES ITS OWN `Duration` PLUS A MARGIN, so the ability always wins the
        /// race to clean this up, and the number cannot drift the way a literal 4.0 did.
        /// </summary>
        public static GameObject SpawnGhostPoltergeist(Vector3 position, Vector3 direction, int ownerSlot,
                                                       float lifetime)
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
            comp.Lifetime = lifetime;

            return go;
        }

        public sealed class GhostPoltergeistComponent : MonoBehaviour
        {
            public Vector3 Direction;
            public int OwnerSlot;

            /// <summary>Seconds this body stays in the world, set by the caster. See
            /// <see cref="SpawnGhostPoltergeist"/> for why it is not a literal here.</summary>
            public float Lifetime = 4.0f;

            private CharacterMotor _target;

            /// <summary>Set once the haunt has landed, so it lands once and the body stays.
            /// See the branch that reads it.</summary>
            private bool _haunted;

            private void Update()
            {
                Lifetime -= Time.deltaTime;
                if (Lifetime <= 0.0f)
                {
                    Object.Destroy(gameObject);
                    return;
                }

                // ⚠️ A HAUNTED GHOST HOLDS STATION AND STOPS LOOKING FOR WORK. Everything below
                // is flight and hunting, and neither is wanted once the hit has landed: Nemu is
                // still out, and this object is still the place she returns to.
                if (_haunted) return;

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

                        // ⚠️⚠️ THE HAUNT NO LONGER DESTROYS THE BODY. This was the second of the
                        // two ways the purple light could go out with Nemu still projected, and
                        // by far the faster one: a ghost that found somebody half a second after
                        // being cast deleted itself, and Nemu's only route home with it. It marks
                        // itself spent and hangs where it landed for the rest of the ability,
                        // which is also where a player who just watched it connect is looking.
                        _haunted = true;
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
                    // ⚠️ IT CUTS RATHER THAN FADES, which is the whole shape of the cue. A tail
                    // that trails off says the danger is lessening; a hole in the world is either
                    // there or it is not, and four players need to know which on one frame.
                    GameServices.Audio?.PlayAt("sfx_void_close", transform.position);
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
        // KULAM HEX SIGIL & WITCH REGALIA (Phaister Abilities)
        // -------------------------------------------------------------------
        public static GameObject SpawnKulamHexSigil(Vector3 position, float radius = 2.4f, float duration = 6.0f, int ownerSlot = -1)
        {
            var go = new GameObject("KulamHexSigilZone");
            go.transform.position = position;

            // ⚠️⚠️ THE CIRCLE IS DRAWN AS STROKES, AND IT WAS TWO FILLED DISCS. What stood here
            // was `PrimitiveType.Cylinder` at `radius * 2.0` and another at `radius * 1.25`, and
            // a Unity cylinder is SOLID: those are not rings, they are two stacked translucent
            // PLATES covering the whole footprint and then some. At the shipped 2.4 m that is
            // about **18 m² of a 196 m² court painted magenta for one skill**, before the spokes
            // and the nodes, which is the puddle `docs/VISION.md` § 2 exists to stop.
            //
            // ⚠️⚠️ AND TWO COPLANAR TRANSLUCENT PLATES SORT ARBITRARILY, so which of the two
            // colours won was decided per frame by a distance comparison between two centres 3 mm
            // apart. `docs/TODO.md` § 19.2a has the account: the same fault shipped on Sean's
            // trail and drew a different colour per drop.
            //
            // `VfxShapes.Sigil` draws the same circle as LINE ART: an outer ring, an inner ring,
            // a five-pointed star and rune ticks, with the road showing between them. It paints
            // roughly **8 per cent of its own circle** against the two discs' 200 per cent, it
            // cannot lose a sort because there is nothing stacked on it, and it actually looks
            // like a witch's circle rather than a coloured coin. § 21.5.
            SpawnWitchSigil(position, radius, duration, 5, 2)
                .transform.SetParent(go.transform, worldPositionStays: true);

            // ⚠️ THE PERIMETER NODES STAY, BECAUSE THEY ARE THE ONLY VERTICAL THING IN THE
            // EFFECT and a mark on the road is edge-on at eye height. They are `Prism` shards
            // rather than `PrimitiveType.Cube` for the reason § 19 gives: the cube was the one
            // primitive Dante's debris, Sean's embers, the void's shards and the frost spikes
            // were ALL made of, so four fictions shared one lump of geometry.
            for (int n = 0; n < 6; n++)
            {
                float ang = n * 60.0f * Mathf.Deg2Rad;
                float dist = radius * 0.88f;

                var node = VfxShapes.Stand(go.transform, $"HexNode_{n}",
                                           VfxShapes.Prism(5, 1.0f, 0.18f, 0.2f, 0.4f, 40 + n),
                                           0.11f, heightScale: 0.34f);
                node.transform.localPosition =
                    new Vector3(Mathf.Sin(ang) * dist, 0.16f, Mathf.Cos(ang) * dist);
                node.transform.localRotation = Quaternion.Euler(0.0f, n * 60.0f, 0.0f);

                VfxMaterial.Ghost(node.GetComponent<Renderer>(),
                                  new Color(0.98f, 0.25f, 0.65f, 0.85f), 0.45f);
            }

            // ⚠️ THE "CRESCENT MOON" IS DELETED. It was a `Cylinder` scaled flat and tilted 30
            // degrees, which is an ELLIPSE seen at an angle and not a crescent by any
            // construction. The sigil's own inner wheel now occupies that space and actually
            // carries a symbol.

            // 6. Violet / Magenta Occult Light
            var lightGo = new GameObject("HexLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0, 1.4f, 0);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroWitchBright;
            light.range = radius * 2.2f;
            light.intensity = 0.95f;

            // 7. Rising Witch Cast Particles (Occult motes & gold sparkles)
            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.WitchSigil, duration);

            // ⚠️⚠️ HER OWN CUE, AND THIS PLAYED `ability_shatter_trap` PLUS `sfx_ghost_teleport`:
            // a trap breaking, and Nemu's teleport. Both are borrowed, and the first is from the
            // deleted ability set that `AudioCues.DeletedAbilityCues` exists to track.
            // `docs/TODO.md` § 20 had just finished taking that same cue off Cheska's two ground
            // powers; a third kit reaching for it would have made it three.
            GameServices.Audio?.PlayAt("sfx_hex_cast", position);
            ComicPopup.Spawn(position, "KULAM!", UiTheme.HeroWitchBright, 1.25f);

            var comp = go.AddComponent<KulamHexSigilComponent>();
            comp.Radius = radius;
            comp.Duration = duration;
            comp.OwnerSlot = ownerSlot;

            HazardVolume.Attach(go, radius, ownerSlot);
            return go;
        }

        public static void SpawnShadowBlinkBurst(Vector3 departurePos, Vector3 arrivalPos)
        {
            // ⚠️⚠️ BOTH ENDS ARE GLYPHS NOW, NOT FILLED DISCS. A 3.2-scale `Cylinder` is a
            // 1.6 m RADIUS solid plate, so a blink stamped 8 m² of magenta on the road at the
            // departure point and another 4.5 m² at the arrival, for two marks that live under
            // half a second each. A cast glyph says the same thing (a spell happened HERE) in
            // strokes, and it is the same symbol her other two powers draw, which is what makes
            // the three read as one hero's craft rather than three unrelated flashes.
            //
            // ⚠️ THE DEPARTURE MARK IS THE BIGGER OF THE TWO AND THAT IS DELIBERATE. It is where
            // the knockback `OverlapSphere` is centred, so it is the one the other three players
            // need to see; the arrival is where she already is and her body marks that.
            SpawnCastGlyph(departurePos, 1.55f, 0.55f, seed: 5);

            for (int i = 0; i < 8; i++)
            {
                var shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shard.name = "BlinkShard";
                shard.transform.position = departurePos + Vector3.up * 0.5f;
                shard.transform.localScale = Vector3.one * Random.Range(0.12f, 0.24f);
                VfxMaterial.Ghost(shard.GetComponent<Renderer>(),
                                  new Color(0.70f, 0.15f, 0.95f, 0.90f), 0.5f);
                var rb = shard.AddComponent<Rigidbody>();
                rb.linearVelocity = Vector3.up * 2.5f + Random.insideUnitSphere * 3.5f;
                Object.Destroy(shard, 0.60f);
            }

            // Arrival Glow Rune Stamp, smaller and shorter: she is standing on it.
            SpawnCastGlyph(arrivalPos, 1.15f, 0.42f, seed: 6);
        }

        /// ⚠ IT RETURNS THE OBJECT, AND IT USED TO RETURN void. Every other spawner in this
        /// file hands back what it made, and `AbilityShowcaseProbe.Solo` needs that to sweep the
        /// effect up before the next capture: an effect it cannot collect survives into the NEXT
        /// frame and quietly appears in a shot that is supposed to show one ability. The class
        /// note on `Transient` records that exact trap.
        public static GameObject SpawnGrandCovenEclipse(Vector3 position, float radius = 5.0f, float duration = 5.0f)
        {
            var go = new GameObject("GrandCovenEclipseEffect");
            go.transform.position = position;

            // ⚠️⚠️ THIS WAS THE SINGLE LARGEST PAINTED OBJECT EVER PUT IN THIS GAME AND THE
            // ARITHMETIC IS NOT CLOSE. The corona was a `Cylinder` at `radius * 2.0`, and a Unity
            // cylinder is one unit across, so at the default 5.0 m it is a solid disc of radius
            // 5 m: **78.5 m² of a 196 m² court, 40 per cent of the box in one plate**, with a
            // second 23.8 m² disc stacked on top of it. `docs/VISION.md` § 2 rule 5 asks that a
            // mid-fight frame still show the lata, the chalk and every player, and rule 1 puts a
            // skill's floor at 3 to 8 per cent. The old measured worst offender in the whole game
            // was Zack's corridor at 27.2 per cent.
            //
            // ⚠️ AN ULTIMATE MAY BE BIG. RULE 2 SAYS SO, AND THAT IS NOT WHAT THIS WAS. Big and
            // FILLED are different claims: the heptagram below keeps the full 5 m reach, so the
            // power reads as arena-wide exactly as intended, and paints about 8 per cent of the
            // circle it covers because it is strokes. Same footprint, a twelfth of the pixels.
            //
            // ⚠️ SEVEN POINTS, NOT FIVE, AND IT IS HOW HER ULTIMATE IS TOLD FROM HER SKILL.
            // § 21.5: everything Phaister does is a drawn symbol, so the silhouette rule the
            // other five heroes follow cannot separate her own kit. The skills draw a pentagram
            // and the ultimate draws a heptagram at double the radius, which is how occult
            // diagrams actually escalate.
            SpawnWitchSigil(position, radius, duration, 7, 3, 0.02f, 11)
                .transform.SetParent(go.transform, worldPositionStays: true);

            // ⚠️ THE DARK MOON STAYS, BECAUSE IT IS THE ONLY THING IN THE EFFECT THAT SAYS
            // "ECLIPSE" RATHER THAN "SPELL", and it comes down from `radius * 1.1` to `0.34`.
            // At the old size it was a 23.8 m² black plate over the middle of the court; at this
            // one it is a disc under her feet that the sigil's inner wheel rings, which is what
            // an eclipse actually looks like from below.
            var moonCore = VfxShapes.Lay(go.transform, "EclipseMoonCore",
                                         VfxShapes.Crystal(18, 0.0f),
                                         radius * 0.34f, 0.025f);
            VfxMaterial.Ghost(moonCore.GetComponent<Renderer>(),
                              new Color(0.05f, 0.01f, 0.08f, 0.95f), 0.0f);
            VfxMaterial.StripCollider(moonCore);

            // ⚠️ THE EIGHT CUBE "SOLAR FLARE BEAMS" ARE DELETED. They were 9 m long stretched
            // cubes crossing the whole arena at 0.80 alpha, and the sigil's own rune ticks and
            // star strokes already give the mark its radial structure. Eight more bars on top of
            // it is `VISION.md` § 2 rule 4 broken inside one effect.

            AbilityVfx.AttachAura(go.transform, AbilityVfx.Aura.WitchSigil, duration);
            Object.Destroy(go, duration);
            return go;
        }

        public sealed class KulamHexSigilComponent : MonoBehaviour
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
                            GameServices.Audio?.PlayAt("sfx_hex_afflict", p.transform.position);
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------
        // THUNDERSTRIKE OVERDRIVE (Zack Ultimate)
        // -------------------------------------------------------------------
        // -------------------------------------------------------------------
        // WITCH SIGILS (Phaister, all three powers)
        //
        // ⚠️⚠️ THE HEX HAZARD SHIPPED WITH NO GEOMETRY AT ALL. `PhaisterHexHazard.Initialize`
        // added a `Light` and an aura and nothing else, so the sixth hero's signature power was
        // an invisible purple glow on the road with a damage circle nobody could see. That is
        // the fault `HeroAbility.TelegraphRadius` exists to prevent, in its most complete form:
        // not a telegraph that lies, a telegraph that is not drawn.
        //
        // ⚠️⚠️ AND EVERY ONE OF HER POWERS DRAWS THE SAME KIND OF MARK ON PURPOSE, WHICH IS THE
        // OPPOSITE OF THE RULE THE OTHER FIVE FOLLOW. 🧑: *"she does hexes curses and spells and
        // has glyphs effects during spells or abilities casting"*. For Sean, Zack, Cheska, Dante
        // and Nemu the silhouette says WHICH ability it is, because their kits are five unrelated
        // physical events. Phaister's kit is one CRAFT: everything she does is a symbol drawn in
        // the air or on the ground, so the sigil is her signature and the three are told apart by
        // SIZE, by how many rings they carry and by where they sit, exactly the way real occult
        // diagrams are. A pentagram at her feet, a heptagram over the court.
        // -------------------------------------------------------------------

        /// <summary>
        /// A witch's circle: two counter-rotating sigils on the ground, and a light that reaches
        /// the street rather than the mark.
        /// </summary>
        /// <param name="points">Star points. 5 is a pentagram, 7 a heptagram.</param>
        /// <param name="skip">How far along each stroke jumps. Must be coprime with points.</param>
        public static GameObject SpawnWitchSigil(Vector3 position, float radius, float duration,
                                                 int points = 5, int skip = 2,
                                                 float lift = 0.02f, int seed = 0)
        {
            var go = new GameObject("WitchSigil");
            go.transform.position = position;

            // ⚠️ TWO WHEELS TURNING OPPOSITE WAYS IS THE ENTIRE READ, and it is one extra mesh.
            // A single ring rotating is a loading spinner; two nested rings turning against each
            // other is a mechanism, which is what an occult diagram is supposed to look like.
            // The outer carries the star, the inner is a plain rune band so the two do not fight
            // each other for the eye.
            // ⚠️⚠️ `HeroWitch`, NOT `HeroSpirit`, AND THE FIRST VERSION OF THIS DREW HER IN
            // NEMU'S PURPLE. `UiTheme` already carries `HeroWitch` (e82882) and
            // `HeroWitchBright` (f44498) for the sixth hero, and nothing in her kit used either
            // of them: every popup, light and mark reached for `HeroSpiritBright`, which is
            // Nemu's. `Hero_Strike_Balance.md` § 8.1 is explicit that hue is the one channel this
            // game cannot spare, and § 8's own history has the worked example of what sharing it
            // costs: *"Sean's Supernova was spawning Dante's magma. Two heroes reading as one is
            // the most expensive form of repetitive, because it costs a character."* Two spirit
            // heroes on one violet is that fault with the colour instead of the geometry.
            //
            // ⚠️ IT MATTERS MOST FOR THESE TWO SPECIFICALLY. Nemu and Phaister are the only pair
            // in the game who share an ELEMENT, so hue is doing more work here than anywhere
            // else: Nemu's void is a dark funnel and Phaister's hex is bright line art, but a
            // player catching either in the corner of their eye reads the colour first.
            var outer = VfxShapes.Lay(go.transform, "SigilOuter",
                                      VfxShapes.Sigil(points, skip, 0.045f, 0.74f, 14, 40, seed),
                                      radius, lift);
            VfxMaterial.Ghost(outer.GetComponent<Renderer>(),
                              Alpha(UiTheme.HeroWitch, 0.88f), 0.30f);

            // ⚠️ THE INNER WHEEL IS ALWAYS A PENTAGRAM, WHATEVER THE OUTER ONE IS. It was
            // `points + 2` with `skip + 1`, so the ultimate's outer heptagram sat over a
            // nine-pointed inner star and the two together read as a compass rose or a sunburst
            // rather than as an occult diagram: `ability_coven_eclipse_v16.png`. Holding the
            // inner ring constant leaves the OUTER star as the only thing that changes between
            // her skill and her ultimate, which is the escalation § 21.5 is actually claiming.
            var inner = VfxShapes.Lay(go.transform, "SigilInner",
                                      VfxShapes.Sigil(5, 2, 0.055f, 0.52f, 8, 32, seed + 17),
                                      radius * 0.55f, lift + 0.008f);
            VfxMaterial.Ghost(inner.GetComponent<Renderer>(),
                              Alpha(UiTheme.HeroWitchBright, 0.78f), 0.26f);

            // ⚠️⚠️ 1.1, NOT 2.5, AND THE 2.5 CAME IN WITH THE HERO. Every hazard light in this
            // file came down by roughly two thirds on 2026-08-25 for one reason, written up on
            // the ice sheet: a hot source sitting on top of its own effect paints the EFFECT and
            // not the street, so the dark parts of the mark render as the light's own colour at
            // full brightness. `PhaisterHexHazard` set 2.5 at `radius * 2.0` and would have
            // washed a violet sigil to flat white the first time anybody rendered it.
            //
            // ⚠️ RAISED AS WELL AS DIMMED. Higher up the falloff across the mark is much flatter,
            // so what is left spills onto the road, which is the job: the glow says something is
            // there before a player can see what it is.
            var lightGo = new GameObject("SigilLight");
            lightGo.transform.SetParent(go.transform, false);
            lightGo.transform.localPosition = new Vector3(0.0f, 1.7f, 0.0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = UiTheme.HeroWitchBright;
            light.range = radius * 2.4f;
            light.intensity = 1.1f;

            var spin = go.AddComponent<WitchSigilSpin>();
            spin.Outer = outer.transform;
            spin.Inner = inner.transform;
            spin.Duration = duration;

            return go;
        }

        /// <summary>A theme colour at a given alpha, so a call site states the two separately.</summary>
        private static Color Alpha(Color c, float a) => new Color(c.r, c.g, c.b, a);

        /// <summary>
        /// Turns the two rings against each other and fades the whole mark out at the end.
        ///
        /// ⚠️ IT IS AN `IVfxTimeline`, so `AbilityShowcaseProbe` can wind it to any moment of its
        /// own life and photograph it. `ArcFlicker`'s note has the argument: an effect that
        /// animates in `Update` and nothing else is an effect that freezes on frame one in every
        /// capture, which is how the whole § 8 silhouette pass came to be reviewed against
        /// pictures that could not contain it.
        ///
        /// ⚠️ THE FADE IS THE LAST FIFTH ONLY. `Hero_Strike_Balance.md` § 8.5 item 2: a player has
        /// to be able to tell a spent zone from a live one, and a mark that dims from the first
        /// frame reads as a failing effect rather than as a timer.
        /// </summary>
        public sealed class WitchSigilSpin : MonoBehaviour, Visual.IVfxTimeline
        {
            public Transform Outer;
            public Transform Inner;
            public float Duration = 6.0f;

            /// <summary>Degrees per second, opposite ways. Slow: this is a diagram, not a fan.</summary>
            private const float OuterSpin = 26.0f;
            private const float InnerSpin = -41.0f;

            private const float FadeFrom = 0.8f;

            private float _elapsed;
            private Renderer _outerRenderer;
            private Renderer _innerRenderer;
            private float _outerAlpha = 1.0f;
            private float _innerAlpha = 1.0f;

            public float LifeSeconds => Mathf.Max(0.2f, Duration);

            private void Awake()
            {
                if (Outer != null) _outerRenderer = Outer.GetComponent<Renderer>();
                if (Inner != null) _innerRenderer = Inner.GetComponent<Renderer>();

                if (_outerRenderer != null) _outerAlpha = _outerRenderer.sharedMaterial.color.a;
                if (_innerRenderer != null) _innerAlpha = _innerRenderer.sharedMaterial.color.a;
            }

            private void Update() => StepTo(_elapsed + Time.deltaTime);

            public void StepTo(float seconds)
            {
                _elapsed = seconds;

                if (Outer != null)
                    Outer.localRotation = Quaternion.Euler(0.0f, OuterSpin * _elapsed, 0.0f);

                if (Inner != null)
                    Inner.localRotation = Quaternion.Euler(0.0f, InnerSpin * _elapsed, 0.0f);

                float t = Mathf.Clamp01(_elapsed / LifeSeconds);
                if (t < FadeFrom) return;

                float k = 1.0f - Mathf.InverseLerp(FadeFrom, 1.0f, t);
                Fade(_outerRenderer, _outerAlpha * k);
                Fade(_innerRenderer, _innerAlpha * k);
            }

            private static void Fade(Renderer r, float alpha)
            {
                if (r == null || r.sharedMaterial == null) return;

                var c = r.sharedMaterial.color;
                c.a = alpha;
                r.sharedMaterial.color = c;
            }
        }

        /// <summary>
        /// The glyph that flashes while a spell is being CAST, at the caster's feet.
        ///
        /// ⚠️ IT IS SHORT AND IT IS SMALL, because it is punctuation rather than a hazard. 🧑
        /// asked for *"glyphs effects during spells or abilities casting"*, and the trap in that
        /// is drawing a second full-size circle every time she presses a button: three of those
        /// live at once is `docs/VISION.md` § 2 rule 4 broken by one hero on her own. A 1.1 m
        /// mark for under a second says "a spell is being cast here" and is gone before the thing
        /// it summoned has finished arriving.
        /// </summary>
        public static GameObject SpawnCastGlyph(Vector3 position, float radius = 1.1f,
                                                float duration = 0.75f, int seed = 3)
        {
            var go = SpawnWitchSigil(position, radius, duration, 5, 2, 0.03f, seed);
            go.name = "WitchCastGlyph";

            Object.Destroy(go, duration);
            return go;
        }

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
