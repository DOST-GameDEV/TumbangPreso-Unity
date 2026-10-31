using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE PAINTED LOOK OF NEMU'S THREE SKILL GHOSTS (owner 2026-10-28, of the reworked kits: *"its pretty poorly made so
    /// eventually we'll need to do a vfx revamp again"*). PEEKER is the mint mouse, FETCHER the cocoa bear, KEEPER the butter
    /// owl (`PetGhostModels`, the set he chose on 2026-10-19). This file is what they do AROUND their bodies.
    ///
    /// ⚠️ WHY IT REPLACES WHAT IT REPLACES. Each ghost wore ASTRA's `NemuSmokeBatch` round it: dozens of additive glow dots
    /// (eyes, aura, trail, drips, sparks, a ring of dots for every beat) and dark violet smoke balls. On the sunlit court with
    /// no bloom those dots go pale and vanish (`docs/VFX_BENCH_METHOD.md` section 10.1, *"Additive glow and low alpha wash
    /// out"*), and a dot cloud is the opposite of designed (section 1a). So every moment that matters is now the approved
    /// cloud puffs (`FxCloudPuffs`, *"heavily optimized"*): opaque, three painted tones, readable in daylight, each one
    /// typed below. And NOTHING POPS: every ghost unfolds out of a puff of its own colour as it arrives (<see cref="Unfold"/>)
    /// and is gone into one as it leaves (<see cref="Depart"/>).
    ///
    ///  * PEEKER (mouse): it rises out of a mint and lilac puff on its spot; a small sigh of mint as it fades to a shimmer, so
    ///    a glance sees WHERE it hid (the ring says the same, for everyone); a kick of mint where it launches; a little burst on
    ///    the victim's crown as it latches; gone up into a puff as it lets go, or down into one if nobody came.
    ///  * FETCHER (bear): out of a cocoa puff beside her; it barrel-rolls along its dash to the slipper (<see cref="BearWeave"/>,
    ///    the drawing only, back on its line before it reaches); small cream puffs fall behind it as it runs (its trail,
    ///    history, never re-pointed); a burst in its paws at the grab; a slow small puff while it holds the slipper out; gone
    ///    into a puff with its happy spin.
    ///  * KEEPER (owl): out of a butter puff beside the can; chalk dust off the can's foot at every heave (each pulse of the
    ///    1.5 s lift is a pulse you can see); three butter puffs round the rim as it cheers; gone into a puff.
    ///
    /// ⚠️ COSMETIC ONLY. No rule, number, timing, target or message changes: every puff is spawned by each peer's OWN copy of
    /// the ghost from state that peer already has (its age, its phase, its carry flag), so every screen sees the same thing
    /// with nothing sent. `NetSession.ProtocolVersion` does not move.
    ///
    /// ⚠️ GPU-DRIVEN. Her puffs live in ONE field of their own (<see cref="NemuGhostPuffs"/>: one mesh, one material, two
    /// passes, the same `CloudPuffField` the landing dust uses), so a burst of hers never takes a row of somebody's landing
    /// dust. A puff is five numbers written into a row; the shader runs its life. Nothing is made per puff and nothing is
    /// rebuilt per frame.
    ///
    /// Behind <see cref="Painted"/> (true). False draws exactly what the game drew before this file (ASTRA's dots and smoke
    /// round the pet ghosts). Only in force while `PetGhostModels.PetGhosts` is too (<see cref="On"/>): the tones are the pets'.
    /// </summary>
    public static class NemuGhostLook
    {
        /// <summary>The painted look round her skill ghosts. False is the look before it, unchanged.</summary>
        public static bool Painted = true;

        /// <summary>In force: the painted look, on the pet ghosts it was made for.</summary>
        public static bool On => Painted && PetGhostModels.PetGhosts;

        public enum Pet { Mouse, Bear, Owl }

        // ------------------------------------------------------------------ the tones

        // Three painted steps per tone, read off the models' own atlases (`tools/build_nemu_ghosts_simple.py`: the mint mouse
        // #BDE8D3 / #8FCDB3, the game's cocoa #9A6646 / #764A30 with its cream patch #EDD3B6, the butter owl #FFE6A0 / #E8C46A)
        // and pulled apart the way `DanteRefusalPuff`'s visibility pass had to be: the lit tone keeps its colour, the shade is
        // the colour richer, the line is dark. Each pet's second tone is Nemu's own lilac (the mouse's cloth, #7A5CC8): no puff
        // of hers is one flat hue (section 1a, *"Vary the hue between two named extremes"*).
        private static readonly Color[] Lit =
        {
            new Color(.80f, .95f, .87f), new Color(.80f, .72f, .97f),   // mint, lilac
            new Color(.93f, .83f, .71f), new Color(.80f, .72f, .97f),   // cream, lilac
            new Color(1f, .93f, .70f), new Color(.80f, .72f, .97f),     // butter, lilac
        };
        private static readonly Color[] Shade =
        {
            new Color(.47f, .80f, .65f), new Color(.56f, .43f, .86f),
            new Color(.62f, .42f, .29f), new Color(.56f, .43f, .86f),
            new Color(.91f, .72f, .30f), new Color(.56f, .43f, .86f),
        };
        private static readonly Color[] Line =
        {
            new Color(.18f, .42f, .33f), new Color(.30f, .18f, .52f),
            new Color(.33f, .19f, .11f), new Color(.30f, .18f, .52f),
            new Color(.52f, .36f, .10f), new Color(.30f, .18f, .52f),
        };
        // The can's own dust, from `CloudPuffField.Puff.Landing`: the KEEPER's heave kicks up the court, not ghost stuff.
        private static readonly Color DustLit = new Color(.93f, .87f, .73f), DustShade = new Color(.77f, .70f, .57f), DustLine = new Color(.56f, .49f, .39f);

        // ------------------------------------------------------------------ the arrival

        /// <summary>How long a ghost takes to unfold out of its puff.</summary>
        public const float UnfoldSeconds = .4f;

        /// <summary>
        /// Its size as it arrives: from 0.55, never from nothing (a body that grows from a point is a pop), out to a little
        /// past full and settled, the way a cushion is plumped. Hidden at first inside its puff, which is why it may start
        /// that large.
        /// </summary>
        public static float Unfold(float age)
        {
            float u = Mathf.Clamp01(age / UnfoldSeconds);
            if (u >= 1f) return 1f;
            float e = 1f - (1f - u) * (1f - u);
            return Mathf.Lerp(.55f, 1f, e) + .10f * Mathf.Sin(u * Mathf.PI) * u;
        }

        /// <summary>How much of it shows as it arrives: in over the first two thirds of the unfold, while its puff covers it.</summary>
        public static float UnfoldFade(float age) => Mathf.SmoothStep(0f, 1f, age / (UnfoldSeconds * .66f));

        // ------------------------------------------------------------------ the bear's dash

        /// <summary>
        /// ⚠️ THE BEAR'S BARREL ROLL. The court film v2 showed the bear "barely circles in real play", and the reason was the rule:
        /// FETCHER runs at `NecroRules.FetchSpeed` (9 m/s) the moment it pops out, so `PetGhostFlight`'s idle circles (which
        /// only run while it is NOT running) never get a frame. So the life goes INTO the run: it rolls round its own line of
        /// travel, a corkscrew about a quarter of a metre wide, banking into each half turn. Seen from above it weaves, from
        /// the side it bobs over. The weight <paramref name="run"/> is how fast it goes (0 to 1), and it is eased to nothing
        /// over the last 1.7 m to the slipper, so its hands arrive exactly where the rule's do. Drawing only: the host's
        /// `Position`, the arrival test and the hold point never move. Returns the offset in world space.
        /// </summary>
        public static Vector3 BearWeave(float clock, float run, float goalDistance, Quaternion heading, out float roll)
        {
            float near = Mathf.SmoothStep(0f, 1f, (goalDistance - .5f) / 1.2f);
            float k = Mathf.Clamp01(run) * near;
            const float Period = .6f;
            float th = clock * (2f * Mathf.PI / Period);
            roll = -30f * Mathf.Cos(th) * k;
            return heading * new Vector3(.26f * Mathf.Sin(th), .16f * (1f - Mathf.Cos(th)), 0f) * k;
        }

        // ------------------------------------------------------------------ the puffs, each typed

        /// <summary>
        /// Out of a puff of its own colour: a wide one on the ground, a lilac one a moment later at its middle, a small one at
        /// its head. <paramref name="feet"/> is under where it will be.
        /// </summary>
        public static void Arrive(Pet pet, Vector3 feet)
        {
            int p = (int)pet * 2;
            Emit(feet + Vector3.up * .04f, 1.30f, .55f, .45f, .25f, .3f, 1f, p, 0f);
            Emit(feet + Vector3.up * .22f, .95f, .50f, .32f, .45f, 2.4f, .85f, p + 1, .06f);
            Emit(feet + Vector3.up * .42f, .62f, .45f, .18f, .35f, 4.1f, .75f, p, .12f);
        }

        /// <summary>Gone into a puff: a ring at its middle that rises, and a small sigh higher up that rises further.</summary>
        public static void Depart(Pet pet, Vector3 middle)
        {
            int p = (int)pet * 2;
            Emit(middle, 1.05f, .60f, .35f, .60f, 1.1f, 1f, p, 0f);
            Emit(middle + Vector3.up * .18f, .70f, .55f, .20f, .90f, 3.7f, .8f, p + 1, .07f);
        }

        /// <summary>PEEKER fades to its shimmer: one soft sigh of mint where it hid.</summary>
        public static void Hide(Vector3 middle)
        {
            Emit(middle, .75f, .50f, .26f, .22f, .7f, .75f, 0, 0f);
            Emit(middle + Vector3.up * .10f, .50f, .45f, .16f, .30f, 2.9f, .6f, 1, .08f);
        }

        /// <summary>PEEKER launches at the eyes: a low kick of mint off its spot.</summary>
        public static void Lunge(Vector3 spot)
            => Emit(spot + Vector3.up * .04f, .85f, .35f, .42f, .08f, 1.9f, 1f, 0, 0f);

        /// <summary>PEEKER latches on: a little burst on the victim's crown (above the face, so their own lens is never in it).</summary>
        public static void Latch(Vector3 crown)
        {
            Emit(crown, .62f, .32f, .26f, .16f, .4f, .7f, 0, 0f);
            Emit(crown, .45f, .30f, .20f, .22f, 3.3f, .6f, 1, .04f);
        }

        /// <summary>FETCHER's trail: a small cream puff dropped where its body just was. It stays where it fell (a trail is history).</summary>
        public static void Wake(Vector3 at, int beat)
            => Emit(at, .55f, .45f, .12f, .15f, beat * 1.7f, .5f, 2 + (beat % 3 == 2 ? 1 : 0), 0f);

        /// <summary>FETCHER snatches the slipper: a burst in its paws, cocoa then lilac.</summary>
        public static void Grab(Vector3 paws)
        {
            Emit(paws, .90f, .40f, .35f, .20f, .9f, 1f, 2, 0f);
            Emit(paws + Vector3.up * .05f, .55f, .36f, .24f, .28f, 3.1f, .7f, 3, .05f);
        }

        /// <summary>FETCHER holds it out: a slow small puff, "here, take it".</summary>
        public static void Offer(Vector3 paws, int beat)
            => Emit(paws + Vector3.up * .06f, .45f, .50f, .15f, .30f, beat * 2.3f, .5f, 2, 0f);

        /// <summary>KEEPER heaves: chalk dust off the can's foot, once per pulse of the lift.</summary>
        public static void Heave(Vector3 foot, int beat)
        {
            var p = Puff(foot + Vector3.up * .02f, .80f, .40f, .40f, .10f, beat * 1.3f, .9f, 0);
            p.Lit = DustLit; p.Shade = DustShade; p.Line = DustLine;
            Emit(p, 0f);
        }

        /// <summary>KEEPER cheers the can up: three butter puffs round the rim, out of step, rising.</summary>
        public static void Cheer(Vector3 rim, Vector3 side)
        {
            side.y = 0f; side = side.sqrMagnitude > 1e-4f ? side.normalized : Vector3.right;
            Vector3 across = Vector3.Cross(Vector3.up, side);
            Emit(rim + side * .16f, .60f, .55f, .20f, .50f, .2f, .8f, 4, 0f);
            Emit(rim - side * .08f + across * .14f, .52f, .50f, .18f, .55f, 2.2f, .75f, 5, .05f);
            Emit(rim - side * .08f - across * .14f, .52f, .50f, .18f, .45f, 4.4f, .75f, 4, .10f);
        }

        // ------------------------------------------------------------------ where they go

        private static CloudPuffField _stage;
        private static float _stageNow;

        /// <summary>A film writes the puffs into its own field at its own moment instead of the match's (null to stop).</summary>
        public static void Stage(CloudPuffField field, float now) { _stage = field; _stageNow = now; }

        private static CloudPuffField.Puff Puff(Vector3 at, float scale, float life, float reach, float lift, float turn, float share, int tone)
            => new CloudPuffField.Puff
            {
                At = at, Scale = scale, Life = life, Reach = reach, Lift = lift, Turn = turn, Share = share,
                Lit = Lit[tone], Shade = Shade[tone], Line = Line[tone],
            };

        private static void Emit(Vector3 at, float scale, float life, float reach, float lift, float turn, float share, int tone, float late)
            => Emit(Puff(at, scale, life, reach, lift, turn, share, tone), late);

        private static void Emit(in CloudPuffField.Puff puff, float late)
        {
            if (_stage != null) { _stage.Spawn(puff, _stageNow + late); return; }
            NemuGhostPuffs.Spawn(puff, late);
        }
    }

    /// <summary>
    /// The match's field of Nemu's ghost puffs, made the first time one is asked for (or at load, <see cref="Prepare"/>). Its
    /// own field, so her bursts never take a row of the landing dust's (`CloudPuffs`). Scaled time, so a pause holds them.
    ///
    /// ⚠️ THE CLOCK IS HANDED TO THE MATERIAL IN `LateUpdate`, AFTER EVERY GHOST HAS SPAWNED ITS PUFFS FOR THE FRAME. A puff may
    /// be asked for a moment late (<c>late</c> &gt; 0: the second and third puff of an arrival), and `CloudPuffField.Spawn`
    /// hands the material the moment it was given; `LateUpdate` puts the real clock back before anything is drawn.
    /// </summary>
    public sealed class NemuGhostPuffs : MonoBehaviour
    {
        private static NemuGhostPuffs _current;
        private CloudPuffField _field;
        private float _now;

        /// <summary>The one draw's triangles right now, for a cost report.</summary>
        public static int DrawnTriangles => _current != null && _current._field != null && _current._field.Live ? _current._field.DrawnTriangles : 0;

        public static void Prepare()
        {
            if (_current != null) return;
            var go = new GameObject("Nemu ghost puffs") { hideFlags = HideFlags.DontSave };
            var marks = WorldContactPresentation.Current;
            if (marks != null) go.transform.SetParent(marks.transform, false);
            _current = go.AddComponent<NemuGhostPuffs>();
            _current._field = new CloudPuffField(go.transform, "Nemu ghost puffs");
        }

        public static void Spawn(in CloudPuffField.Puff puff, float late)
        {
            Prepare();
            _current._field.Spawn(puff, _current._now + Mathf.Max(0f, late));
        }

        private void LateUpdate()
        {
            if (_field == null || !_field.Live) return;
            if (GameServices.Round != null && !GameServices.Round.RoundActive) { _field.Clear(); return; }
            _now += Time.deltaTime;
            _field.Tick(_now);
        }

        private void OnDestroy()
        {
            _field?.Dispose();
            _field = null;
            if (_current == this) _current = null;
        }
    }
}
