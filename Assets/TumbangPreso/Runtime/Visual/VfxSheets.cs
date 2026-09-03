using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// The sourced ability art, and the one table that says how each sheet is cut.
    ///
    /// ⚠️⚠️ EVERY SHEET HERE IS CC0 AND RECOLOURED ON THE WAY IN. `docs/Asset_Sourcing.md` is
    /// the licensed source list and `tools/build_vfx_sheets.py` is the recolour. The sources
    /// arrive in cobalt blue, cornflower, orange and black line art; the six heroes in this game
    /// are `UiTheme.HeroMagmaCore`, `HeroIce`, `HeroFire`, `HeroElectric`, `HeroSpirit` and
    /// `HeroWitch`, and a hero whose floor effect is a different colour from their own deck
    /// icon, popup text and character-select tile is two heroes.
    ///
    /// ⚠️⚠️ THE NUMBERS BELOW ARE A CONTRACT WITH FILES ON DISK, AND
    /// `VfxSheetTests.EverySheetIsTheSizeTheTableWasWrittenAgainst` IS WHY THAT IS SAFE.
    /// `InputGlyphs` learned this the expensive way: a sheet is one size in Explorer and another
    /// in the engine the moment an importer rescales it, and nothing anywhere says so. The test
    /// loads every sheet named here and asserts its pixel size against `Columns`, `Rows` and
    /// `Cell`, so a sheet rebuilt at a different grid fails on the next EditMode run rather than
    /// on somebody's screen.
    ///
    /// ⚠️ ONE ENTRY PER SHEET, NOT PER ABILITY. Most of them are shared: Dante's stomp
    /// and his fissure are the same rupture at two scales, and that is the point. `VISION.md`
    /// § 2 rule 3 asks the budget to go on DETAIL, and one well-drawn rupture reused twice is
    /// more detail per byte than two adequate ones.
    ///
    /// ⚠️ THE SHEETS ARE NOT A REPLACEMENT FOR THE GAMEPLAY GEOMETRY AND MUST NEVER BECOME ONE.
    /// `HeroHazards` still owns every footprint, every collider and every radius. These are the
    /// transient layer over the top: what the cast LOOKS like in the half second it happens.
    /// Nothing in this file may change collision, range, authority, cooldowns or balance.
    /// </summary>
    public static class VfxSheets
    {
        /// <summary>Where `Resources.Load` finds them. One folder, one importer, one licence.</summary>
        public const string Folder = "Vfx/";

        /// <summary>
        /// One recoloured source sheet.
        ///
        /// ⚠️ `Fps` OF ZERO MEANS "NOT A FLIPBOOK". The hex ring is a single written decal and
        /// the bolt sheet is eight alternative strokes, one CHOSEN per strike rather than played
        /// in order. `VfxFlipbook.Play` refuses a zero-fps sheet and `VfxFlipbook.Still` is the
        /// call for those two.
        /// </summary>
        public readonly struct Sheet
        {
            public readonly string Resource;

            /// <summary>
            /// ⚠️⚠️ WIDTH AND HEIGHT SEPARATELY, BECAUSE ONE SHEET IS NOT SQUARE AND A SINGLE
            /// `Cell` FIELD SILENTLY MIS-SLICED IT. The eleven flipbooks are 96 x 96 and Zack's
            /// bolt sheet is eight strokes at **64 x 512**: a
            /// bolt is a tall thin thing and cutting it square would have taken the top eighth
            /// of one stroke and called it the effect.
            /// </summary>
            public readonly int CellWidth;
            public readonly int CellHeight;

            public readonly int Columns;
            public readonly int Frames;
            public readonly int Fps;
            public readonly bool Loops;

            /// <summary>
            /// Where the ground line sits inside a cell, as a fraction of the cell height
            /// measured UP from the bottom edge.
            ///
            /// ⚠️⚠️ THESE ARE SIDE-ON EFFECTS AND WITHOUT THIS THEY FLOAT OR SINK. Every source
            /// sheet is drawn standing on a floor that is somewhere inside the cell rather than
            /// at its bottom edge: the rupture's contact line is a quarter of the way up, the
            /// implosion's is halfway. A quad centred on the cast point buries half of one and
            /// hangs the other in the air, and it looks like the ability is placed wrong rather
            /// than like the art is anchored wrong.
            ///
            /// ⚠️ THE NUMBERS ARE THE PACK'S OWN PIVOTS, converted from pixels-down-from-the-top
            /// to a fraction up from the bottom. `tools/build_vfx_sheets.py` prints them.
            /// </summary>
            public readonly float Pivot;

            public Sheet(string resource, int cellWidth, int cellHeight, int columns,
                         int frames, int fps, bool loops, float pivot)
            {
                Resource = resource;
                CellWidth = cellWidth;
                CellHeight = cellHeight;
                Columns = columns;
                Frames = frames;
                Fps = fps;
                Loops = loops;
                Pivot = pivot;
            }

            /// <summary>Rows the sheet actually occupies, which is what its height must be.</summary>
            public int Rows => (Frames + Columns - 1) / Columns;

            /// <summary>The pixel size the file on disk has to be. The test asserts exactly this.</summary>
            public Vector2Int PixelSize => new Vector2Int(CellWidth * Columns, CellHeight * Rows);

            /// <summary>The quad's height as a multiple of its width. Never assume square.</summary>
            public float Aspect => CellHeight / (float)CellWidth;

            public float LifeSeconds => Fps > 0 ? Frames / (float)Fps : 0.0f;
        }

        // -------------------------------------------------------------------
        // ⚠️ KEEP THIS LIST IN STEP WITH `tools/build_vfx_sheets.py`. That script prints
        // exactly this table at the end of every run for the purpose.
        // -------------------------------------------------------------------

        /// <summary>Dante. Ground breaking upward. Seismic Stomp and Titan Fissure.</summary>
        public static readonly Sheet Rupture = new Sheet("vfx_rupture_v1", 96, 96, 5, 20, 20, false, 0.260f);

        /// <summary>Cheska. The formation transient for the sheet, the wall and the nova.</summary>
        public static readonly Sheet FrostNova = new Sheet("vfx_frostnova_v1", 96, 96, 5, 13, 20, false, 0.365f);

        /// <summary>Sean. The leading edge of Flame Rush.</summary>
        public static readonly Sheet EmberJet = new Sheet("vfx_emberjet_v1", 96, 96, 5, 14, 20, false, 0.417f);

        /// <summary>Zack. The ground impact under a strike, and his sprint start and stop.</summary>
        public static readonly Sheet Spark = new Sheet("vfx_spark_v1", 96, 96, 5, 14, 20, false, 0.438f);

        /// <summary>Nemu. The intake at the centre of Devouring Seance.</summary>
        public static readonly Sheet Implosion = new Sheet("vfx_implosion_v1", 96, 96, 5, 14, 20, false, 0.479f);

        /// <summary>Sean. Supernova's main burst, and the can knockdown's debris.</summary>
        public static readonly Sheet Burst = new Sheet("vfx_burst_v1", 96, 96, 5, 15, 20, false, 0.396f);

        /// <summary>Nemu. Phantom Veil on and off, and the Astral Hijack transfer.</summary>
        public static readonly Sheet Bloom = new Sheet("vfx_bloom_v1", 96, 96, 5, 16, 20, false, 0.333f);

        /// <summary>Sean. Ignition Cannon's projectile head and its impact.</summary>
        public static readonly Sheet BoltHead = new Sheet("vfx_bolthead_v1", 96, 96, 5, 12, 20, false, 0.479f);

        /// <summary>The street's own. Shadow Blink, knockdowns, and anything that puffs.</summary>
        public static readonly Sheet Smoke = new Sheet("vfx_smoke_v1", 96, 96, 5, 14, 20, false, 0.500f);

        /// <summary>The street's own. Landing, sprint dust and a slipper hitting asphalt.</summary>
        public static readonly Sheet Dust = new Sheet("vfx_dust_v1", 96, 96, 5, 14, 20, false, 0.271f);

        /// <summary>Sean. The pieces Supernova throws.</summary>
        public static readonly Sheet Shrapnel = new Sheet("vfx_shrapnel_v1", 96, 96, 5, 14, 20, false, 0.417f);

        // ⚠️⚠️ THERE IS NO PHAISTER SHEET, AND THAT IS RECORDED RATHER THAN FORGOTTEN.
        // `docs/Asset_Sourcing.md` § 3 maps her Hex and her Grand Coven to the CC0 summoning
        // circles, and `docs/TODO.md` § 131.2 allows a mapping to be dropped when the reason is
        // written down after an in-engine comparison. The reason is `DrapeToGround`:
        // `HeroHazards.SpawnHexSigil` conforms both ward layers to the road because 🧑 reported
        // *"her magic circle doesnt draw over the sidewalk and thats weird af"*, and a four
        // vertex quad has nothing to conform with. Her kit is also the ONLY one in
        // `HeroHazards.cs` with no `CreatePrimitive` in it, so there is no primitive layer here
        // for § 131 to replace. `tools/build_vfx_sheets.py` carries the full argument and the
        // route back in, which is a subdivided UV plate rather than a decal.

        /// <summary>
        /// Zack. Eight alternative bolt strokes, 64 px wide and 512 px tall.
        ///
        /// ⚠️ THE CELL IS CHOSEN, NOT STEPPED. Playing these in order would be a bolt writhing
        /// like a rope; a strike is one stroke that is there and then gone. `VfxFlipbook.Still`
        /// takes the cell index, and the callers pick it from the strike position so two bolts in
        /// one cast are never the same stroke.
        /// </summary>
        public static readonly Sheet Bolt = new Sheet("vfx_bolt_v1", 64, 512, 8, 8, 0, false, 0.000f);

        /// <summary>Every sheet, for the tests and for the warmup. Order is not meaningful.</summary>
        public static readonly Sheet[] All =
        {
            Rupture, FrostNova, EmberJet, Spark, Implosion, Burst, Bloom,
            BoltHead, Smoke, Dust, Shrapnel, Bolt,
        };
    }
}
