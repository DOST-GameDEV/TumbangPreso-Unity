using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// § THE RECALL BEAM. A column of light standing on your own tsinelas while it lies on the
    /// ground, in the colour you picked for the slipper highlight.
    ///
    /// 🧑 2026-09-19, with a frame of a Fortnite loot beam: *"create a prompt that creates this
    /// beam when your tsinelas is in the ground. the beam color will be matched with your
    /// slipper's highlight color. there shouldnt be any conflict with the slipperRecall."*
    ///
    /// ⚠️⚠️ IT IS THE WORLD HALF OF A QUESTION `SlipperRecall` ANSWERS ON THE SCREEN, AND THE
    /// TWO ARE BUILT NOT TO ARGUE. Both exist because `docs/VISION.md` § 0 says the tension of
    /// this game is the retrieval, and *"where is mine"* is the question a retrieval opens with.
    /// They differ in what they can do: a screen mark can point at something you cannot see and
    /// a beam cannot, and a beam is a landmark you can read out of the corner of your eye while
    /// a mark is not. **Everything they could have disagreed about is shared rather than
    /// duplicated**, which is this file's whole design and is listed once here:
    ///
    ///  * **Whose it is** is `Slipper`'s per-peer owner flag, the one `MatchInstaller` and
    ///    `MatchRpc` maintain. Neither feature derives it a second time.
    ///  * **The colour** is `Settings.SlipperHighlights.ColourOf`, the same call the landed rim
    ///    makes, so the ring, the rim and the beam cannot end up three different colours.
    ///  * **OFF means off** for both, through the same setting.
    ///  * **They stand down together.** Both fade out as the grab comes into reach, keyed off
    ///    `Balance.PickupRadius`, and `TumpMatchReadout` takes over with `[X] Pick up`. The
    ///    hand-off `SlipperRecall.Track` argues for at length is this component's too.
    ///  * **They cannot fight over a renderer.** The beam is painted by `VfxMaterial.Ghost`,
    ///    which attaches `VfxRenderTag`, which is what keeps `Slipper.RefreshHighlight`'s rim
    ///    pass and `InputEdgeTests`' toon-outline sweep off an effect parented to a prop.
    ///
    /// ⚠️⚠️ IT SHOWS FOR ANY LOOSE TSINELAS OF YOURS, NOT ONLY ONE THAT ENDED A FLIGHT, AND THE
    /// DIFFERENCE FROM THE LANDED RIM IS DELIBERATE. `Slipper`'s rim answers a narrow question,
    /// *"where did the one you just threw end up"*, so it is cleared by a recovery that teleports
    /// the shoe home rather than lighting a place the throw never reached. The beam answers the
    /// broad one, *"your tsinelas is lying over there"*, which is true however it got there. 🧑
    /// asked for it *"when your tsinelas is in the ground"*.
    ///
    /// ⚠️ BLUE IS LEGAL HERE AND IS THE SHIPPED DEFAULT. `CLAUDE.md` § 6.4 bans blue from UI
    /// chrome; this is a world effect wearing a colour the player chose, and
    /// `Settings.SlipperHighlights.Default`'s own note records blue being picked precisely
    /// because *"the arena is warm dust and wood"* and it is the one entry that cannot be
    /// mistaken for the floor or for the gold owner glow.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SlipperBeam : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ 2.2 m, WHICH IS ABOUT A DOORWAY AND IS CHOSEN AGAINST THE MAPS RATHER THAN AGAINST
        /// THE REFERENCE. The frame this was drawn from is a third-person game with open sky over
        /// most of it. Two of this game's three arenas are built UNDER things (a bridge, a roof),
        /// so a column tall enough to look impressive outdoors is a column that punches through a
        /// ceiling indoors. A little over head height clears the props a tsinelas actually hides
        /// behind, which is what it is for.
        /// </summary>
        public const float Height = 2.2f;

        /// <summary>
        /// ⚠️⚠️ THE COLUMN IS A STACK OF SEGMENTS AND THE FIRST BUILD WAS ONE CYLINDER, WHICH
        /// RENDERED AS A LENGTH OF PLASTIC PIPE. `Logs/shots-recall/beam-witness.png` on the
        /// first green run is the receipt: a hard silhouette at one alpha from base to tip, which
        /// is what a tube looks like and not what light looks like. Light in the reference is
        /// brightest where it leaves the object and gone by the top.
        ///
        /// ⚠️ SEGMENTS RATHER THAN A GRADIENT TEXTURE OR A CUSTOM SHADER, because this front end
        /// is blocky on purpose (`docs/Art_Direction.md` § 0) and because `VfxMaterial` is
        /// `Standard` in Fade mode by a measurement that file records. A stack needs no new
        /// asset, no new shader in `GameBuilder.EnsureRuntimeShaders`, and reads as a taper from
        /// every angle including from above.
        /// </summary>
        public const int Segments = 6;

        public const float BaseDiameter = 0.17f;
        public const float TipDiameter = 0.05f;
        public const float PoolDiameter = 0.85f;

        /// <summary>
        /// ⚠️⚠️ THE ALPHAS ARE LOW ON PURPOSE AND `AbilityShowcaseProbe` IS WHY THERE IS A NUMBER
        /// TO POINT AT. That probe FAILS a run in which one effect blows more than 12 per cent of
        /// the frame to white, and it caught Zack's ultimate at 62.8 per cent. A beam is thin, but
        /// it is also the one effect in the game a player deliberately walks up to and stands
        /// under, so it is the effect most able to fill a first-person frame.
        /// </summary>
        public const float BaseAlpha = 0.30f;
        public const float PoolAlpha = 0.20f;

        /// <summary>
        /// How far above <see cref="Balance.PickupRadius"/> the beam is at full strength.
        ///
        /// ⚠️⚠️ THE FADE IS PRESENTATION AND IS NOT A SECOND COPY OF THE PICKUP RULE. It reads
        /// the same published number so the beam cannot stand down at a distance the grab
        /// disagrees with, but the rule itself still lives in exactly one place,
        /// `Slipper.IsGrabbableIgnoringReach`, and this asks no question about eligibility at all.
        /// `CombatVerbs.SlideMayStartFrom` makes the same distinction for the slide.
        ///
        /// ⚠️ AND IT IS A FADE RATHER THAN A CUT BECAUSE THIS GAME IS FIRST PERSON. A 2.2 m
        /// column vanishing on a frame boundary as you step over your own shoe reads as a glitch;
        /// one metre of falloff is about three walking steps.
        /// </summary>
        public const float FadeMetres = 1.0f;

        private Slipper _slipper;
        private Transform _column;
        private Light _lamp;

        /// <summary>
        /// ⚠️⚠️ THE MATERIALS ARE BUILT ONCE AND TINTED EVERY FRAME, AND THE FIRST VERSION HAD
        /// THAT BACKWARDS. `Paint` ran from `Update` and called `VfxMaterial.Ghost`, which does
        /// `new Material(template)` per renderer and hands it to `VfxRenderTag.Own`. At sixty
        /// frames a second that is sixty materials per renderer per second added to a list that
        /// is only emptied when the slipper dies: the exact leak `VfxRenderTag.Own`'s own note
        /// was written to close, reintroduced one call site later and an order of magnitude
        /// worse. Ghost is for BUILDING an effect. Per-frame work writes into what it built.
        /// </summary>
        private readonly System.Collections.Generic.List<Material> _tinted =
            new System.Collections.Generic.List<Material>();

        private readonly System.Collections.Generic.List<float> _alphas =
            new System.Collections.Generic.List<float>();

        private readonly Transform[] _motes = new Transform[3];

        private bool _on;
        private Color _colour = Color.white;

        /// <summary>
        /// Whether the column is actually standing right now.
        ///
        /// ⚠️ IT IS NOT THE SAME QUESTION AS "WAS IT TURNED ON". `Set(true, ...)` arms the beam
        /// and <see cref="Update"/> is what takes it down again once the grab is in reach, so a
        /// probe asking the flag alone would be told a beam is drawing while the player is
        /// standing on the shoe with nothing on screen. This asks the objects.
        /// </summary>
        public bool Drawing => _on && _column != null && _column.gameObject.activeInHierarchy;

        /// <summary>
        /// The colour it is painted in, for anything that has to check it against the setting.
        /// 🧑's requirement was *"the beam color will be matched with your slipper's highlight
        /// color"*, and a claim nothing can fail is not a match.
        /// </summary>
        public Color Colour => _colour;

        /// <summary>
        /// ⚠️ BUILT ON FIRST USE RATHER THAN WITH THE SLIPPER. Most tsinelas in most rounds never
        /// need one: the taya's is parked, the other seats' are somebody else's, and a shoe in a
        /// hand has nothing to stand on. Building four renderers and a light per slipper at match
        /// install would pay for all four every round to use at most one.
        /// </summary>
        public static SlipperBeam Attach(Slipper slipper)
        {
            if (slipper == null) return null;

            var beam = slipper.GetComponent<SlipperBeam>();
            if (beam == null)
            {
                beam = slipper.gameObject.AddComponent<SlipperBeam>();
                beam._slipper = slipper;
            }

            return beam;
        }

        /// <summary>
        /// The one entry point. <paramref name="on"/> false takes the whole column down and
        /// builds nothing.
        /// </summary>
        public void Set(bool on, Color colour)
        {
            _colour = colour;

            if (!on)
            {
                _on = false;
                if (_column != null && _column.gameObject.activeSelf)
                    _column.gameObject.SetActive(false);
                return;
            }

            if (_column == null) Build();
            _on = true;

            if (!_column.gameObject.activeSelf) _column.gameObject.SetActive(true);
            Paint(NearFade());
        }

        private void Build()
        {
            var root = new GameObject("RecallBeam");
            _column = root.transform;
            _column.SetParent(transform, false);

            // ⚠️ THE SLIPPER'S OWN REST HEIGHT IS TAKEN OFF, so the column starts at the ROAD
            // rather than at the top of the shoe. `Slipper.RestHeight` is measured from the mesh
            // and differs per skin, which is exactly why it is asked rather than assumed.
            float floor = -_slipper.RestHeight;
            float segment = Height / Segments;

            // The pool of light the column stands in, first so the column draws over it.
            Add(Tube("GroundPool", PoolDiameter, 0.012f, floor + 0.006f), PoolAlpha, 0.70f);

            for (int i = 0; i < Segments; i++)
            {
                // ⚠️ THE TAPER IS ON BOTH WIDTH AND ALPHA, AND THE ALPHA CURVE IS THE ONE THAT
                // MATTERS. A linear fade still ends with a visible top edge; raising it to a
                // power puts most of the falloff in the last third, so the column simply stops
                // being there rather than stopping.
                float t = i / (float)(Segments - 1);

                float width = Mathf.Lerp(BaseDiameter, TipDiameter, t);
                float alpha = BaseAlpha * Mathf.Pow(1.0f - t, 1.7f);

                Add(Tube("Column" + i, width, segment * 1.04f, floor + (i + 0.5f) * segment),
                    alpha, Mathf.Lerp(1.0f, 0.4f, t));
            }

            // ⚠️ ONE LIGHT, NO SHADOWS, AND THERE IS NEVER MORE THAN ONE IN THE SCENE. The beam
            // only ever draws on the local player's own tsinelas, so this cannot multiply the way
            // a per-seat effect would. `CreateZapFlightVisual` sets the same three fields for the
            // same reason.
            var lampGo = new GameObject("Glow");
            lampGo.transform.SetParent(_column, false);
            lampGo.transform.localPosition = new Vector3(0.0f, floor + 0.35f, 0.0f);
            _lamp = lampGo.AddComponent<Light>();
            _lamp.type = LightType.Point;
            _lamp.range = 2.4f;
            _lamp.shadows = LightShadows.None;

            for (int i = 0; i < _motes.Length; i++)
            {
                var mote = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mote.name = "Mote" + i;
                mote.transform.SetParent(_column, false);
                mote.transform.localScale = Vector3.one * 0.05f;
                _motes[i] = mote.transform;

                var art = mote.GetComponent<Renderer>();
                StripCollider(mote);
                Add(art, 0.5f, 1.0f);
            }
        }

        /// <summary>
        /// Paints one piece ONCE, then remembers its material and the share of the beam's
        /// strength it carries. See the note on <see cref="_tinted"/> for why this is not done
        /// again every frame.
        /// </summary>
        private void Add(Renderer art, float alpha, float emission)
        {
            if (art == null) return;

            VfxMaterial.Ghost(art, Fade(alpha), emission);

            _tinted.Add(art.sharedMaterial);
            _alphas.Add(alpha);
        }

        /// <summary>
        /// ⚠️⚠️ `CreatePrimitive` HANDS BACK A COLLIDER AND A TSINELAS MAY NOT HAVE ONE.
        /// `MatchInstaller.StripColliders`' note is the receipt: *"a thrown slipper starts shoving
        /// players around the arena"*. `VfxMaterial.Ghost` strips it for everything it paints,
        /// which covers every piece here; the cubes are stripped a second time before that call
        /// because a primitive is dangerous from the frame it exists, not from the frame it is
        /// painted.
        /// </summary>
        private Renderer Tube(string name, float diameter, float height, float centreY)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(_column, false);

            // A Unity cylinder is two units tall, so half the wanted height is the Y scale.
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            go.transform.localPosition = new Vector3(0.0f, centreY, 0.0f);

            StripCollider(go);
            return go.GetComponent<Renderer>();
        }

        private static void StripCollider(GameObject go)
        {
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }

        /// <summary>
        /// Writes the live colour and strength into materials that already exist.
        ///
        /// ⚠️ THE SETTINGS PANEL IS REACHABLE FROM THE IN-MATCH PAUSE MENU, so a colour change
        /// has to reach a beam that is already standing. That is the same live-repaint
        /// requirement `Slipper.OnEnable` records for the rim, and it is why this is a tint over
        /// stored materials rather than a rebuild.
        /// </summary>
        private void Paint(float strength)
        {
            for (int i = 0; i < _tinted.Count; i++)
            {
                var m = _tinted[i];
                if (m == null) continue;

                Color c = Fade(_alphas[i] * strength);

                m.color = c;
                if (m.HasProperty(BaseColourId)) m.SetColor(BaseColourId, c);
                if (m.HasProperty(EmissionId))
                    m.SetColor(EmissionId, new Color(c.r, c.g, c.b, 1.0f) * (0.9f * strength));
            }

            if (_lamp != null)
            {
                _lamp.color = _colour;
                _lamp.intensity = 0.55f * strength;
            }
        }

        private static readonly int BaseColourId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private Color Fade(float alpha) =>
            new Color(_colour.r, _colour.g, _colour.b, Mathf.Clamp01(alpha));

        /// <summary>
        /// ⚠️⚠️ THE COLUMN IS FORCED UPRIGHT EVERY FRAME RATHER THAN TRUSTED TO BE. It is a child
        /// of the tsinelas, and a tsinelas is a THROWN object: `SpinInFlight` tumbles it and
        /// `Land` is the only thing that puts the rotation back. A beam that inherits a shoe's
        /// pose is a beam lying on its side the moment anything skips that path, and the routes
        /// that put a slipper down without landing it (the owner-mark recovery, a snapshot
        /// applied on a client) are exactly the routes nobody watches.
        ///
        /// ⚠️ THE PULSE IS SMALL ON PURPOSE. 🧑 on the menu work: **"make sure all main menu
        /// effects are subtle"**, and a beacon that throbs is a beacon that pulls the eye away
        /// from the fight it is standing beside.
        /// </summary>
        private void Update()
        {
            if (!_on || _column == null) return;

            _column.rotation = Quaternion.identity;

            float strength = NearFade();

            if (strength <= 0.001f)
            {
                if (_column.gameObject.activeSelf) _column.gameObject.SetActive(false);
                return;
            }

            if (!_column.gameObject.activeSelf) _column.gameObject.SetActive(true);

            float pulse = 0.88f + 0.12f * Mathf.Sin(Time.time * 2.4f);
            Paint(strength * pulse);


            float floor = -_slipper.RestHeight;

            for (int i = 0; i < _motes.Length; i++)
            {
                if (_motes[i] == null) continue;

                // Each mote climbs the column on its own offset and starts again at the bottom.
                float t = Mathf.Repeat(Time.time * 0.55f + i / (float)_motes.Length, 1.0f);
                float swing = Mathf.Sin((Time.time + i) * 1.7f) * 0.05f;

                _motes[i].localPosition = new Vector3(swing, floor + t * Height, swing * 0.6f);
                _motes[i].localRotation = Quaternion.Euler(0.0f, Time.time * 90.0f + i * 40.0f, 0.0f);
            }
        }

        /// <summary>
        /// 1 while the tsinelas is out of reach, 0 once the grab would take it.
        ///
        /// ⚠️ IT ASKS THE ROUND FOR THE OWNER RATHER THAN THE HUD FOR THE LOCAL PLAYER. This
        /// component only ever runs on a beam that is already established as YOURS (the caller
        /// gates on the same per-peer flag the owner glow uses), so the owning seat IS the local
        /// player and `GameServices.Round.PlayerAt` is the cheapest honest way to reach them.
        /// A `Visual` component reaching into the HUD would be the dependency pointing the wrong
        /// way round.
        /// </summary>
        private float NearFade()
        {
            var round = GameServices.Round;
            var owner = round != null && _slipper != null && _slipper.OwnerSlot >= 0
                ? round.PlayerAt(_slipper.OwnerSlot)
                : null;

            if (owner == null) return 1.0f;

            float d = Vector3.Distance(owner.transform.position, transform.position);
            return Mathf.InverseLerp(Balance.PickupRadius, Balance.PickupRadius + FadeMetres, d);
        }
    }
}
