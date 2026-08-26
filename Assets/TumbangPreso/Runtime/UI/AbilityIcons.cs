using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// What KIND of thing an ability is, drawn as a shape rather than spelled out.
    ///
    /// ⚠️⚠️ THE GLYPH DESCRIBES THE SHAPE OF THE EFFECT, NOT THE ELEMENT. Fire, ice and
    /// lightning are already carried by the hero's colour, and a flame icon on a fire hero's
    /// three powers tells the player nothing about which of the three to press. What they need
    /// at a glance is *what it does to the world*: does it put something on the ground, put a
    /// wall in front of me, move me, protect me, or hit everything at once. Two heroes with
    /// completely different fiction share a glyph when they share a job, on purpose.
    ///
    /// ⚠️ ADD A GLYPH RATHER THAN REUSING A NEAR MISS. A wrong icon is worse than a generic
    /// one, because the player trusts it once and then stops trusting all of them.
    /// </summary>
    public enum AbilityGlyph
    {
        /// <summary>A patch of ground that does something to whoever stands on it.</summary>
        Zone,

        /// <summary>A solid thing placed in the world that bodies and slippers cannot pass.</summary>
        Wall,

        /// <summary>Moves the caster. Rushes, dashes, grinds.</summary>
        Dash,

        /// <summary>Protects the caster. Armour, immunity, carapace.</summary>
        Shield,

        /// <summary>Goes off around the caster and hits everything in reach.</summary>
        Burst,

        /// <summary>Sends something away from the caster at a target.</summary>
        Projectile,

        /// <summary>The caster stops being fully present. Phase, ghost, untargetable.</summary>
        Phase,

        /// <summary>Comes down from above onto a place. Slams, strikes, fissures.</summary>
        Slam,

        /// <summary>Changes the tsinelas rather than the world. Overcharge, curve, empower.</summary>
        Empower,

        // -------------------------------------------------------------------
        // BESPOKE HERO GLYPHS (Unique across all 15 abilities)
        // -------------------------------------------------------------------

        /// <summary>Dante Skill 1: Seismic Stomp (Heavy downward ground slam with shockwaves).</summary>
        DanteStomp,

        /// <summary>Dante Skill 2: Demonic Carapace (Solid heavy armored crest).</summary>
        DanteShield,

        /// <summary>Dante Ultimate: Titan Fissure (Jagged tectonic ground split).</summary>
        DanteFissure,

        /// <summary>Sean Skill 1: Flame Rush (Forward flame rocket chevrons).</summary>
        SeanRush,

        /// <summary>Sean Skill 2: Ignition Cannon (Empowered flaming fireball / throw).</summary>
        SeanIgnite,

        /// <summary>Sean Ultimate: Supernova (High-impact 8-point explosive supernova star).</summary>
        SeanSupernova,

        /// <summary>Cheska Skill 1: Permafrost Sheet (Frosted ice ground zone with crystal shards).</summary>
        CheskaFrostSheet,

        /// <summary>Cheska Skill 2: Ice Barricade (Three crystalline ice pillars).</summary>
        CheskaBarricade,

        /// <summary>Cheska Ultimate: Glacial Nova (6-point radial blizzard snowflake).</summary>
        CheskaNova,

        /// <summary>Zack Skill 1: Bolt Sprint (Dynamic high-speed lightning streak bolt).</summary>
        ZackSprint,

        /// <summary>Zack Skill 2: Static Charge (Electrified spark core with orbital charge arcs).</summary>
        ZackOvercharge,

        /// <summary>Zack Ultimate: Thunderstrike (Overhead thundercloud with downward lightning strike).</summary>
        ZackThunderstrike,

        /// <summary>Nemu Skill 1: Ghost Step (Crescent moon spirit phase).</summary>
        NemuPhase,

        /// <summary>Nemu Skill 2: Astral Projection (Kuro the ghost companion pet silhouette).</summary>
        NemuAstralPet,

        /// <summary>Nemu Ultimate: Seance Void (Gravitational swirling spiral vortex).</summary>
        NemuSeanceVoid,

        /// <summary>Phaister Skill 1: Kulam Hex Sigil (Occult hexagonal magic circle rune).</summary>
        PhaisterHexSigil,

        /// <summary>Phaister Skill 2: Shadow Blink (Occult dimensional warp diamond with shadow streaks).</summary>
        PhaisterShadowBlink,

        /// <summary>Phaister Ultimate: Grand Eclipse (Crowned solar eclipse corona with flare rays).</summary>
        PhaisterEclipse,

        /// <summary>Phaister Witchfire Empower (Witchfire empowered mystical slipper wisp).</summary>
        PhaisterWitchfire,
    }

    /// <summary>
    /// Procedural icons for the ability deck, the hold-to-inspect panel and character select.
    ///
    /// ⚠️⚠️ THEY ARE BAKED IN CODE, LIKE EVERY OTHER SURFACE IN THIS UI. `GodotTheme` already
    /// paints every box, border and shadow in the game into a cached texture rather than
    /// shipping PNGs, for the reason its own note gives: a baked file that drifts from the
    /// code that wanted it is indistinguishable from a broken conversion. Icons follow the same
    /// rule, and it also means the ability art is not blocked on the team's art queue
    /// (`CLAUDE.md` § 4a). When real icons land, replace `Bake` and nothing else changes.
    ///
    /// ⚠️ WHITE ON TRANSPARENT, TINTED AT THE USE SITE. One texture per glyph, ever, and the
    /// hero colour arrives through `Image.color`. Baking a coloured icon per hero would be
    /// five times the textures for no visual difference.
    /// </summary>
    public static class AbilityIcons
    {
        private const int Size = 128;

        private static readonly Dictionary<AbilityGlyph, Sprite> Cache =
            new Dictionary<AbilityGlyph, Sprite>();
        private static Sprite _cooldownDisc;

        public static Sprite For(AbilityGlyph glyph)
        {
            if (Cache.TryGetValue(glyph, out var cached) && cached != null) return cached;

            var sprite = Bake(glyph);
            Cache[glyph] = sprite;
            return sprite;
        }

        /// <summary>A soft-edged disc used by the HUD's radial cooldown veil.</summary>
        public static Sprite CooldownDisc()
        {
            if (_cooldownDisc != null) return _cooldownDisc;

            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float u = (x + 0.5f) / Size * 2.0f - 1.0f;
                    float v = (y + 0.5f) / Size * 2.0f - 1.0f;
                    pixels[y * Size + x] = new Color(1, 1, 1, Disc(u, v, 0.92f));
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "ability_cooldown_disc",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            tex.SetPixels(pixels);
            tex.Apply(false, false);

            _cooldownDisc = Sprite.Create(tex, new Rect(0, 0, Size, Size),
                new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect);
            _cooldownDisc.name = tex.name;
            _cooldownDisc.hideFlags = HideFlags.HideAndDontSave;
            return _cooldownDisc;
        }

        /// <summary>
        /// A one-word name for the glyph, for the inspect panel's "what is this" line.
        ///
        /// ⚠️ IT SAYS THE JOB, NOT THE SHAPE. "GROUND ZONE" is useful; "circle" is noise.
        /// </summary>
        public static string LabelFor(AbilityGlyph glyph)
        {
            switch (glyph)
            {
                case AbilityGlyph.Zone: return "GROUND ZONE";
                case AbilityGlyph.Wall: return "BLOCKER";
                case AbilityGlyph.Dash: return "MOBILITY";
                case AbilityGlyph.Shield: return "PROTECTION";
                case AbilityGlyph.Burst: return "AREA BURST";
                case AbilityGlyph.Projectile:
                    return "PROJECTILE";
                case AbilityGlyph.Phase: return "EVASION";
                case AbilityGlyph.Slam: return "FROM ABOVE";
                case AbilityGlyph.Empower: return "TSINELAS BUFF";
                case AbilityGlyph.DanteStomp: return "SEISMIC STOMP";
                case AbilityGlyph.DanteShield: return "CARAPACE";
                case AbilityGlyph.DanteFissure: return "TITAN FISSURE";
                case AbilityGlyph.SeanRush: return "FLAME DASH";
                case AbilityGlyph.SeanIgnite: return "IGNITION THROW";
                case AbilityGlyph.SeanSupernova: return "SUPERNOVA";
                case AbilityGlyph.CheskaFrostSheet: return "FROZEN FLOOR";
                case AbilityGlyph.CheskaBarricade: return "ICE BARRICADE";
                case AbilityGlyph.CheskaNova: return "GLACIAL BURST";
                case AbilityGlyph.ZackSprint: return "BOLT SPRINT";
                case AbilityGlyph.ZackOvercharge: return "STATIC THROW";
                case AbilityGlyph.ZackThunderstrike: return "THUNDERSTRIKE";
                case AbilityGlyph.NemuPhase: return "GHOST STEP";
                case AbilityGlyph.NemuAstralPet: return "KURO PROJECTION";
                case AbilityGlyph.NemuSeanceVoid: return "SEANCE VOID";
                default:
                    return "POWER";
            }
        }

        // ------------------------------------------------------------------ baking

        private static Sprite Bake(AbilityGlyph glyph)
        {
            var pixels = new Color[Size * Size];

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    // Centred, normalised to roughly -1..1
                    float u = (x + 0.5f) / Size * 2.0f - 1.0f;
                    float v = (y + 0.5f) / Size * 2.0f - 1.0f;

                    float a = Coverage(glyph, u, v);
                    pixels[y * Size + x] = new Color(1.0f, 1.0f, 1.0f, Mathf.Clamp01(a));
                }
            }

            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                name = "abilityglyph_" + glyph,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            tex.SetPixels(pixels);
            tex.Apply(false, false);

            var sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f),
                                       100.0f, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        /// <summary>
        /// The glyph shapes, as signed-distance coverage in a -1..1 square.
        /// </summary>
        private static float Coverage(AbilityGlyph glyph, float u, float v)
        {
            switch (glyph)
            {
                case AbilityGlyph.Zone:
                    return EllipseRing(u, v, 0.82f, 0.54f, 0.26f);

                case AbilityGlyph.Wall:
                    return Mathf.Max(Mathf.Max(
                        Box(u + 0.44f, v + 0.16f, 0.18f, 0.50f),
                        Box(u, v - 0.02f, 0.20f, 0.68f)),
                        Box(u - 0.44f, v + 0.16f, 0.18f, 0.50f));

                case AbilityGlyph.Dash:
                    return Mathf.Max(
                        Chevron(u - 0.30f, v, 0.62f, Stroke),
                        Chevron(u + 0.16f, v, 0.62f, Stroke));

                case AbilityGlyph.Shield:
                    return Crest(u, v, 1.36f, 1.72f);

                case AbilityGlyph.Burst:
                    return Mathf.Max(
                        Disc(u, v, 0.26f),
                        Spokes(u, v, 6, 0.40f, 0.94f, Stroke));

                case AbilityGlyph.Projectile:
                    return Mathf.Max(
                        RightTriangle(u - 0.30f, v, 0.44f, 0.60f),
                        Box(u + 0.60f, v, 0.26f, Stroke * 0.55f));

                case AbilityGlyph.Phase:
                    // A thick crescent with the caster half gone
                    return Mathf.Max(
                        Sub(Disc(u, v, 0.84f), Disc(u - 0.34f, v + 0.05f, 0.76f)),
                        Disc(u - 0.40f, v - 0.14f, 0.17f));

                case AbilityGlyph.Slam:
                    // Down arrow/stomp slamming onto a ground bar
                    return Mathf.Max(Mathf.Max(
                        Box(u, v - 0.27f, Stroke, 0.45f),
                        DownTriangle(u, v + 0.18f, 0.46f, 0.44f)),
                        Box(u, v + 0.80f, 0.66f, 0.11f));

                case AbilityGlyph.Empower:
                    return Bolt(u, v);

                case AbilityGlyph.DanteStomp:
                    return SeismicBoot(u, v);

                case AbilityGlyph.DanteShield:
                    return HornedCarapace(u, v);

                case AbilityGlyph.DanteFissure:
                    // Jagged vertical split crack with branching fissures
                    return FissureCrack(u, v);

                case AbilityGlyph.SeanRush:
                    // High-thrust flame chevrons
                    return Mathf.Max(Mathf.Max(
                        Chevron(u - 0.36f, v, 0.68f, Stroke * 1.15f),
                        Chevron(u + 0.08f, v, 0.68f, Stroke * 1.15f)),
                        RightTriangle(u + 0.62f, v, 0.30f, 0.36f));

                case AbilityGlyph.SeanIgnite:
                    // Flaming slipper / rising flame silhouette
                    return FlameBurst(u, v);

                case AbilityGlyph.SeanSupernova:
                    // 8-ray brilliant exploding star
                    return SupernovaStar(u, v);

                case AbilityGlyph.CheskaFrostSheet:
                    // Elliptical frost ring with diagonal crystal facets
                    return FrostSheet(u, v);

                case AbilityGlyph.CheskaBarricade:
                    // 3 sharp crystalline ice pillars
                    return IceBarricade(u, v);

                case AbilityGlyph.CheskaNova:
                    // 6-point snowflake
                    return Snowflake(u, v);

                case AbilityGlyph.ZackSprint:
                    // Lightning stroke with a pair of speed trails
                    return SprintBolt(u, v);

                case AbilityGlyph.ZackOvercharge:
                    // Static charge orb with orbiting electric sparks
                    return StaticOrb(u, v);

                case AbilityGlyph.ZackThunderstrike:
                    // Storm cloud striking lightning down
                    return ThunderstrikeCloud(u, v);

                case AbilityGlyph.NemuPhase:
                    // Crescent body with spirit trails, distinct from the generic phase glyph
                    return GhostStep(u, v);

                case AbilityGlyph.NemuAstralPet:
                    // Kuro the ghost companion pet silhouette
                    return KuroGhostPet(u, v);

                case AbilityGlyph.NemuSeanceVoid:
                    // Swirling spiral seance vortex
                    return SpiralVoid(u, v);

                case AbilityGlyph.PhaisterHexSigil:
                    // Occult hexagonal magic rune sigil
                    return HexWardSigil(u, v);

                case AbilityGlyph.PhaisterShadowBlink:
                    // Dimensional shadow warp rift
                    return ShadowBlinkRift(u, v);

                case AbilityGlyph.PhaisterEclipse:
                    // Solar eclipse with dark moon core and corona rays
                    return SolarEclipseNova(u, v);

                case AbilityGlyph.PhaisterWitchfire:
                    // Empowered witchfire flame wisp
                    return WitchfireOrb(u, v);

                default:
                    return Disc(u, v, 0.62f);
            }
        }

        // ------------------------------------------------------------------ primitives

        /// <summary>
        /// The one stroke weight in the set.
        ///
        /// ⚠️⚠️ 0.16 OF THE HALF-SQUARE, WHICH IS ABOUT 10 OF 128 TEXTURE PIXELS AND 3 OF THE
        /// 40 SCREEN PIXELS A DECK GLYPH ACTUALLY OCCUPIES. Anything thinner disappears, and
        /// that is not a guess: the previous set ran 0.06 to 0.09 and every glyph in it was a
        /// smudge. If a new glyph "needs" a finer line, it needs fewer parts instead.
        /// </summary>
        private const float Stroke = 0.16f;

        private static float Edge(float distance)
        {
            const float feather = 2.5f / Size;
            return Mathf.Clamp01(0.5f - distance / feather);
        }

        private static float Sub(float shape, float hole) => Mathf.Clamp01(shape - hole);

        private static float Disc(float u, float v, float r)
            => Edge(Mathf.Sqrt(u * u + v * v) - r);

        private static float EllipseDisc(float u, float v, float rx, float ry)
        {
            float d = Mathf.Sqrt((u / rx) * (u / rx) + (v / ry) * (v / ry)) - 1.0f;
            return Edge(d * ry);
        }

        private static float EllipseRing(float u, float v, float rx, float ry, float thickness)
        {
            float d = Mathf.Sqrt((u / rx) * (u / rx) + (v / ry) * (v / ry)) - 1.0f;
            return Edge(Mathf.Abs(d) * ry - thickness * 0.5f);
        }

        private static float Box(float u, float v, float halfW, float halfH)
        {
            float dx = Mathf.Abs(u) - halfW;
            float dy = Mathf.Abs(v) - halfH;
            return Edge(Mathf.Max(dx, dy));
        }

        /// <summary>
        /// A capsule between two points.
        /// </summary>
        private static float Segment(float u, float v, float ax, float ay, float bx, float by,
                                     float halfThickness)
        {
            float pax = u - ax, pay = v - ay;
            float bax = bx - ax, bay = by - ay;

            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay));
            float dx = pax - bax * h, dy = pay - bay * h;

            return Edge(Mathf.Sqrt(dx * dx + dy * dy) - halfThickness);
        }

        private static float Bolt(float u, float v)
        {
            float upper = Segment(u, v, 0.20f, 0.86f, -0.26f, 0.08f, Stroke * 0.85f);
            float cross = Segment(u, v, -0.26f, 0.08f, 0.20f, 0.02f, Stroke * 0.85f);
            float lower = Segment(u, v, 0.20f, 0.02f, -0.16f, -0.86f, Stroke * 0.85f);
            return Mathf.Max(upper, Mathf.Max(cross, lower));
        }

        private static float SprintBolt(float u, float v)
        {
            float bolt = Bolt(u - 0.16f, v);
            float upperTrail = Segment(u, v, -0.82f, 0.34f, -0.34f, 0.34f, Stroke * 0.45f);
            float lowerTrail = Segment(u, v, -0.76f, -0.36f, -0.42f, -0.36f, Stroke * 0.45f);
            return Mathf.Max(bolt, Mathf.Max(upperTrail, lowerTrail));
        }

        private static float SeismicBoot(float u, float v)
        {
            float shin = Box(u + 0.24f, v - 0.34f, 0.18f, 0.43f);
            float foot = Box(u - 0.02f, v + 0.10f, 0.45f, 0.18f);
            float toe = Disc(u - 0.43f, v + 0.10f, 0.18f);
            float ground = Segment(u, v, -0.82f, -0.56f, 0.82f, -0.56f, Stroke * 0.52f);
            float shockL = Segment(u, v, -0.22f, -0.38f, -0.65f, -0.18f, Stroke * 0.42f);
            float shockR = Segment(u, v, 0.26f, -0.38f, 0.68f, -0.18f, Stroke * 0.42f);
            return Mathf.Max(Mathf.Max(shin, Mathf.Max(foot, toe)),
                Mathf.Max(ground, Mathf.Max(shockL, shockR)));
        }

        private static float HornedCarapace(float u, float v)
        {
            float shell = Crest(u, v + 0.10f, 1.34f, 1.42f);
            float hornL = Segment(u, v, -0.48f, 0.46f, -0.78f, 0.84f, Stroke * 0.62f);
            float hornR = Segment(u, v, 0.48f, 0.46f, 0.78f, 0.84f, Stroke * 0.62f);
            float groove = Mathf.Max(
                Segment(u, v, -0.34f, 0.30f, 0.0f, -0.12f, Stroke * 0.28f),
                Segment(u, v, 0.34f, 0.30f, 0.0f, -0.12f, Stroke * 0.28f));
            return Mathf.Max(Sub(shell, groove), Mathf.Max(hornL, hornR));
        }

        private static float GhostStep(float u, float v)
        {
            float crescent = Sub(Disc(u - 0.18f, v + 0.02f, 0.70f),
                                 Disc(u - 0.42f, v + 0.16f, 0.60f));
            float wispTop = Segment(u, v, 0.15f, 0.46f, 0.74f, 0.64f, Stroke * 0.42f);
            float wispBottom = Segment(u, v, 0.18f, -0.36f, 0.76f, -0.52f, Stroke * 0.42f);
            float spark = Disc(u - 0.50f, v + 0.36f, 0.12f);
            return Mathf.Max(crescent, Mathf.Max(wispTop, Mathf.Max(wispBottom, spark)));
        }

        /// <summary>A fat chevron pointing right, of the given vertical span.</summary>
        private static float Chevron(float u, float v, float halfSpan, float thickness)
        {
            if (Mathf.Abs(v) > halfSpan) return 0.0f;

            float leg = u + Mathf.Abs(v) * 0.9f;
            return Edge(Mathf.Abs(leg) - thickness * 0.5f);
        }

        private static float DownTriangle(float u, float v, float halfW, float height)
        {
            if (v < -height || v > 0.0f) return 0.0f;

            float t = Mathf.InverseLerp(-height, 0.0f, v);
            return Edge(Mathf.Abs(u) - halfW * t);
        }

        private static float UpTriangle(float u, float v, float halfW, float height)
        {
            if (v < 0.0f || v > height) return 0.0f;

            float t = Mathf.InverseLerp(0.0f, height, v);
            return Edge(Mathf.Abs(u) - halfW * (1.0f - t));
        }

        private static float RightTriangle(float u, float v, float halfH, float length)
        {
            if (u < -length || u > 0.0f) return 0.0f;

            float t = Mathf.InverseLerp(-length, 0.0f, u);
            return Edge(Mathf.Abs(v) - halfH * (1.0f - t));
        }

        /// <summary>A solid shield crest: straight shoulders, tapering to a point.</summary>
        private static float Crest(float u, float v, float width, float height)
        {
            if (v > height * 0.5f || v < -height * 0.5f) return 0.0f;

            float t = Mathf.InverseLerp(height * 0.5f, -height * 0.5f, v);
            float curve = 1.0f - Mathf.Pow(t, 4.0f);
            return Edge(Mathf.Abs(u) - width * 0.5f * Mathf.Max(0.02f, curve));
        }

        private static float Spokes(float u, float v, int count, float inner, float outer,
                                    float thickness)
        {
            float best = 0.0f;

            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2.0f * i / count;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float su = u * cos + v * sin;
                float sv = -u * sin + v * cos;

                best = Mathf.Max(best, Segment(su, sv, inner, 0.0f, outer, 0.0f, thickness * 0.5f));
            }

            return best;
        }

        // ------------------------------------------------------------------ bespoke helpers

        private static float FissureCrack(float u, float v)
        {
            float s1 = Segment(u, v, 0.05f, 0.85f, -0.15f, 0.20f, Stroke * 0.9f);
            float s2 = Segment(u, v, -0.15f, 0.20f, 0.15f, -0.25f, Stroke * 0.9f);
            float s3 = Segment(u, v, 0.15f, -0.25f, -0.05f, -0.85f, Stroke * 0.9f);
            float b1 = Segment(u, v, -0.15f, 0.20f, -0.65f, 0.45f, Stroke * 0.75f);
            float b2 = Segment(u, v, 0.15f, -0.25f, 0.65f, -0.40f, Stroke * 0.75f);
            return Mathf.Max(Mathf.Max(s1, s2), Mathf.Max(s3, Mathf.Max(b1, b2)));
        }

        private static float FlameBurst(float u, float v)
        {
            float fireball = EllipseRing(u - 0.38f, v, 0.36f, 0.36f, Stroke * 0.76f);
            float hotCore = Disc(u - 0.38f, v, 0.13f);
            float trail = Segment(u, v, -0.70f, 0.0f, 0.08f, 0.0f, Stroke * 0.72f);
            float upperFlame = Segment(u, v, -0.72f, 0.36f, 0.12f, 0.14f, Stroke * 0.62f);
            float lowerFlame = Segment(u, v, -0.72f, -0.36f, 0.12f, -0.14f, Stroke * 0.62f);
            return Mathf.Max(Mathf.Max(fireball, hotCore),
                Mathf.Max(trail, Mathf.Max(upperFlame, lowerFlame)));
        }

        private static float SupernovaStar(float u, float v)
        {
            float core = Disc(u, v, 0.28f);
            float s4 = Spokes(u, v, 4, 0.24f, 0.92f, Stroke * 1.25f);
            float d4 = Spokes(u * 0.7071f - v * 0.7071f, u * 0.7071f + v * 0.7071f, 4, 0.24f, 0.72f, Stroke * 0.85f);
            return Mathf.Max(core, Mathf.Max(s4, d4));
        }

        private static float FrostSheet(float u, float v)
        {
            float slab = EllipseRing(u, v - 0.04f, 0.86f, 0.48f, Stroke * 0.95f);
            float crystal = Spokes(u, v + 0.03f, 4, 0.0f, 0.34f, Stroke * 0.62f);
            float center = Disc(u, v + 0.03f, 0.12f);
            float glint = Segment(u, v, 0.46f, 0.10f, 0.70f, 0.24f, Stroke * 0.42f);
            return Mathf.Max(slab, Mathf.Max(crystal, Mathf.Max(center, glint)));
        }

        private static float IceBarricade(float u, float v)
        {
            float center = Box(u, v + 0.12f, 0.20f, 0.48f);
            float centerTip = UpTriangle(u, v - 0.36f, 0.20f, 0.48f);
            float left = Box(u + 0.46f, v + 0.23f, 0.17f, 0.37f);
            float leftTip = UpTriangle(u + 0.46f, v - 0.14f, 0.17f, 0.34f);
            float right = Box(u - 0.46f, v + 0.23f, 0.17f, 0.37f);
            float rightTip = UpTriangle(u - 0.46f, v - 0.14f, 0.17f, 0.34f);
            float baseLine = Segment(u, v, -0.78f, -0.62f, 0.78f, -0.62f, Stroke * 0.45f);
            return Mathf.Max(baseLine, Mathf.Max(Mathf.Max(center, centerTip),
                Mathf.Max(Mathf.Max(left, leftTip), Mathf.Max(right, rightTip))));
        }

        private static float Snowflake(float u, float v)
        {
            float rays = Spokes(u, v, 6, 0.0f, 0.88f, Stroke * 0.66f);
            float branch1 = Segment(u, v, 0.0f, 0.54f, -0.20f, 0.72f, Stroke * 0.36f);
            float branch2 = Segment(u, v, 0.0f, 0.54f, 0.20f, 0.72f, Stroke * 0.36f);
            float branch3 = Segment(u, v, 0.47f, -0.27f, 0.64f, -0.08f, Stroke * 0.36f);
            float branch4 = Segment(u, v, -0.47f, -0.27f, -0.64f, -0.08f, Stroke * 0.36f);
            float center = Disc(u, v, 0.15f);
            return Mathf.Max(rays, Mathf.Max(center,
                Mathf.Max(Mathf.Max(branch1, branch2), Mathf.Max(branch3, branch4))));
        }

        private static float StaticOrb(float u, float v)
        {
            float brokenOrbit = Sub(EllipseRing(u, v, 0.74f, 0.74f, Stroke * 0.72f),
                                    Box(u, v, 0.12f, 0.92f));
            float core = EllipseRing(u, v, 0.27f, 0.27f, Stroke * 0.58f);
            float coreBolt = SprintBolt(u * 2.35f, v * 2.35f);
            float sparkL = Segment(u, v, -0.84f, 0.12f, -0.64f, 0.28f, Stroke * 0.42f);
            float sparkR = Segment(u, v, 0.84f, -0.12f, 0.64f, -0.28f, Stroke * 0.42f);
            return Mathf.Max(brokenOrbit, Mathf.Max(core,
                Mathf.Max(coreBolt, Mathf.Max(sparkL, sparkR))));
        }

        private static float ThunderstrikeCloud(float u, float v)
        {
            float c1 = Disc(u - 0.32f, v - 0.42f, 0.27f);
            float c2 = Disc(u + 0.32f, v - 0.42f, 0.27f);
            float c3 = Disc(u, v - 0.52f, 0.34f);
            float cloudBase = Box(u, v - 0.30f, 0.62f, 0.17f);
            float cloud = Mathf.Max(cloudBase, Mathf.Max(c1, Mathf.Max(c2, c3)));

            // Heavy lightning bolt striking down from cloud base
            float boltTop = Segment(u, v, 0.08f, 0.20f, -0.22f, -0.22f, Stroke * 0.85f);
            float boltCross = Segment(u, v, -0.22f, -0.22f, 0.14f, -0.26f, Stroke * 0.85f);
            float boltBot = Segment(u, v, 0.14f, -0.26f, -0.06f, -0.88f, Stroke * 0.85f);
            float strike = Mathf.Max(boltTop, Mathf.Max(boltCross, boltBot));

            return Mathf.Max(cloud, strike);
        }

        private static float KuroGhostPet(float u, float v)
        {
            // Head and body
            float head = Disc(u, v - 0.08f, 0.46f);
            float earL = DownTriangle(u - 0.26f, v - 0.42f, 0.15f, 0.24f);
            float earR = DownTriangle(u + 0.26f, v - 0.42f, 0.15f, 0.24f);
            float body = Box(u, v + 0.18f, 0.46f, 0.26f);
            float lobes = Mathf.Max(
                Disc(u - 0.30f, v + 0.44f, 0.16f),
                Mathf.Max(Disc(u, v + 0.44f, 0.16f), Disc(u + 0.30f, v + 0.44f, 0.16f)));

            float ghost = Mathf.Max(head, Mathf.Max(body, Mathf.Max(lobes, Mathf.Max(earL, earR))));

            // Cut out cute ghost eyes
            float eyeL = Disc(u - 0.16f, v - 0.10f, 0.09f);
            float eyeR = Disc(u + 0.16f, v - 0.10f, 0.09f);

            return Sub(ghost, Mathf.Max(eyeL, eyeR));
        }

        private static float SpiralVoid(float u, float v)
        {
            float eye = EllipseRing(u, v, 0.86f, 0.58f, Stroke * 0.72f);
            float core = Disc(u, v, 0.18f);
            float hook1 = Segment(u, v, -0.08f, 0.18f, 0.52f, 0.30f, Stroke * 0.56f);
            float hook2 = Segment(u, v, 0.08f, -0.18f, -0.52f, -0.30f, Stroke * 0.56f);
            float tail1 = Segment(u, v, 0.52f, 0.30f, 0.66f, 0.10f, Stroke * 0.44f);
            float tail2 = Segment(u, v, -0.52f, -0.30f, -0.66f, -0.10f, Stroke * 0.44f);
            return Mathf.Max(eye, Mathf.Max(core,
                Mathf.Max(Mathf.Max(hook1, hook2), Mathf.Max(tail1, tail2))));
        }

        private static float HexWardSigil(float u, float v)
        {
            // Hexagonal rune circle with outer ring, inner ring and 6 radiating rune spokes
            float outerRing = EllipseRing(u, v, 0.84f, 0.84f, Stroke * 0.72f);
            float innerRing = EllipseRing(u, v, 0.44f, 0.44f, Stroke * 0.60f);
            float spokes = Spokes(u, v, 6, 0.22f, 0.90f, Stroke * 0.55f);
            float core = Disc(u, v, 0.16f);
            return Mathf.Max(outerRing, Mathf.Max(innerRing, Mathf.Max(spokes, core)));
        }

        private static float ShadowBlinkRift(float u, float v)
        {
            // Diamond dimensional rift with shadow streaks
            float dx = Mathf.Abs(u) / 0.42f + Mathf.Abs(v) / 0.65f;
            float riftCore = Edge(dx - 1.0f);
            float dxIn = Mathf.Abs(u) / 0.24f + Mathf.Abs(v) / 0.42f;
            float riftHole = Edge(dxIn - 1.0f);
            float riftRing = Sub(riftCore, riftHole);
            float centerGlint = Disc(u, v, 0.10f);
            float streakL = Segment(u, v, -0.82f, 0.0f, -0.42f, 0.0f, Stroke * 0.75f);
            float streakR = Segment(u, v, 0.42f, 0.0f, 0.82f, 0.0f, Stroke * 0.75f);
            float spark1 = Segment(u, v, -0.55f, 0.35f, -0.32f, 0.22f, Stroke * 0.45f);
            float spark2 = Segment(u, v, 0.32f, -0.22f, 0.55f, -0.35f, Stroke * 0.45f);
            return Mathf.Max(riftRing, Mathf.Max(centerGlint,
                Mathf.Max(Mathf.Max(streakL, streakR), Mathf.Max(spark1, spark2))));
        }

        private static float SolarEclipseNova(float u, float v)
        {
            // Crowned solar eclipse: dark moon core, glowing corona ring, and 8 solar flare rays
            float corona = EllipseRing(u, v, 0.55f, 0.55f, Stroke * 0.85f);
            float rays8 = Spokes(u, v, 8, 0.55f, 0.92f, Stroke * 0.65f);
            float moonDisc = Disc(u + 0.08f, v - 0.04f, 0.38f);
            float innerCrescent = Sub(Disc(u, v, 0.40f), moonDisc);
            return Mathf.Max(corona, Mathf.Max(rays8, innerCrescent));
        }

        private static float WitchfireOrb(float u, float v)
        {
            float wisp = EllipseRing(u, v, 0.60f, 0.75f, Stroke * 0.70f);
            float core = Disc(u, v + 0.05f, 0.22f);
            float tip = UpTriangle(u, v - 0.45f, 0.22f, 0.35f);
            float flame = Mathf.Max(wisp, Mathf.Max(core, tip));
            float sparkL = Segment(u, v, -0.70f, 0.10f, -0.50f, 0.25f, Stroke * 0.45f);
            float sparkR = Segment(u, v, 0.50f, -0.15f, 0.70f, 0.00f, Stroke * 0.45f);
            return Mathf.Max(flame, Mathf.Max(sparkL, sparkR));
        }
    }
}

