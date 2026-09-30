using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// § THE RECALL BEAM. A shaft of light standing on your own tsinelas while it lies in the
    /// road, in the colour you picked for the slipper highlight.
    ///
    /// 🧑 2026-09-20, with a frame of a Fortnite loot beam: *"create a beam coming off of a
    /// slipper that is thrown off the ground. this will be similar to the items on ground in
    /// fortnite. make this a shader not a model ... the highlighted beam will be in sync with the
    /// player's settings for the slipper highlights."*
    ///
    /// ⚠️⚠️ THE SHADER IS THE FEATURE AND THIS FILE IS ONLY WHERE IT STANDS. An earlier build of
    /// this effect stacked six primitive cylinders at six alphas to fake a taper and animated
    /// three cubes up it for motes; it was rejected and reverted. Everything it was doing in
    /// geometry and in `Update`, `Shaders/SlipperBeam.shader` now does per pixel. What is left
    /// here is two renderers, one light, and the questions only the game can answer: whose
    /// tsinelas it is, what colour the player chose, and how close they are to picking it up.
    ///
    /// ⚠️⚠️ AND ITS SHAPE CHANGED ON 2026-09-30, FROM A COLUMN TO A LINE. Request: *"overhaul the
    /// highlight beam when a tsinelas is thrown. it should look similar to the highlight beam of
    /// the dropped items in apex legends ... noticeable but not too distracting."* The history is
    /// the argument for the new shape. The 2.2 m hollow cone was findable and hid the can, the feet
    /// and the chase; 3694d67c cut it to a 0.48 m locator, which hid nothing and could not be found
    /// from across the street. Both were COLUMNS, whose visibility and whose obstruction are the
    /// same number (width times height). The reference is a thin camera-facing line in a soft
    /// haze: its visibility comes from height and brightness and its obstruction from a core a few
    /// centimetres wide, so the two stop being one trade. The shader header has the picture.
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
    ///    makes, so the ring, the rim and the beam cannot end up three different colours. That is
    ///    🧑's *"in sync with the player's settings for the slipper highlights"*, met by asking
    ///    rather than by copying.
    ///  * **OFF means off** for both, through the same setting.
    ///  * **They stand down together.** Both fade out as the grab comes into reach, keyed off
    ///    `Balance.PickupRadius`, and `TumpMatchReadout` takes over with `[X] Pick up`.
    ///  * **They cannot fight over a renderer.** The beam is painted by `VfxMaterial.Beam`, which
    ///    attaches `VfxRenderTag`, which is what keeps `Slipper.RefreshHighlight`'s rim pass and
    ///    `InputEdgeTests`' toon-outline sweep off an effect parented to a prop.
    ///
    /// ⚠️⚠️ IT SHOWS FOR ANY LOOSE TSINELAS OF YOURS, NOT ONLY ONE THAT ENDED A FLIGHT, AND THE
    /// DIFFERENCE FROM THE LANDED RIM IS DELIBERATE. `Slipper`'s rim answers a narrow question,
    /// *"where did the one you just threw end up"*, so it is cleared by a recovery that teleports
    /// the shoe home rather than lighting a place the throw never reached. The beam answers the
    /// broad one, *"your tsinelas is lying over there"*, which is true however it got there.
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
        /// ⚠️ 1.9 m: A LITTLE OVER EYE HEIGHT, AND UNDER THE 2.2 m THAT MET THE CEILINGS. Two of the
        /// arenas are built under a bridge or a roof, which is why the first column stopped at a
        /// doorway. This one clears a standing body and the props a tsinelas hides behind, so it
        /// can be seen over them, and the core has thinned to nothing well before any ceiling.
        /// </summary>
        public const float Height = 1.9f;

        /// <summary>
        /// Half the width of the haze, in metres, up close. The core the eye actually finds is
        /// about a ninth of this. At range the shader widens it to hold about a pixel, so this is
        /// the NEAR size only.
        /// </summary>
        public const float HalfWidth = 0.14f;

        public const float PoolDiameter = 0.62f;

        /// <summary>
        /// ⚠️ THE HAZE AND THE POOL, NOT THE CORE. The core's brightness is the shader's
        /// `_CoreAlpha`; these are the two soft terms, and they are kept low on purpose: they are
        /// what gives the line a colour and a footing, and they are the parts that would wash a
        /// first-person frame if pushed. `SlipperRecallShots.MeasureBeamFrameFraction` holds the
        /// whole effect under the 12 per cent frame budget.
        /// </summary>
        public const float BaseAlpha = 0.42f;
        public const float PoolAlpha = 0.22f;

        /// <summary>
        /// How far above <see cref="Balance.PickupRadius"/> the beam is at full strength.
        ///
        /// ⚠️⚠️ THE FADE IS PRESENTATION AND IS NOT A SECOND COPY OF THE PICKUP RULE. It reads
        /// the same published number so the beam cannot stand down at a distance the grab
        /// disagrees with, but the rule itself still lives in exactly one place,
        /// `Slipper.IsGrabbableIgnoringReach`, and this asks no question about eligibility at all.
        /// `CombatVerbs.SlideMayStartFrom` makes the same distinction for the slide.
        ///
        /// ⚠️ AND IT IS A FADE RATHER THAN A CUT BECAUSE THIS GAME IS FIRST PERSON. A low
        /// column vanishing on a frame boundary as you step over your own shoe reads as a glitch;
        /// one metre of falloff is about three walking steps.
        /// </summary>
        public const float FadeMetres = 1.0f;

        private Slipper _slipper;
        private Transform _column;
        private Light _lamp;

        /// <summary>
        /// ⚠️⚠️ THE MATERIALS ARE BUILT ONCE AND DRIVEN EVERY FRAME, AND THE MESH VERSION HAD
        /// THAT BACKWARDS. Its per-frame paint called `VfxMaterial.Ghost`, which does
        /// `new Material(template)` per renderer and hands it to `VfxRenderTag.Own`: at sixty
        /// frames a second that is sixty materials per renderer per second added to a list only
        /// emptied when the slipper dies, which is the exact leak `VfxRenderTag.Own` exists to
        /// close, reintroduced one call site later. A painter BUILDS an effect. Per-frame work
        /// writes one float into what it built.
        /// </summary>
        private readonly System.Collections.Generic.List<Material> _painted =
            new System.Collections.Generic.List<Material>();

        private bool _on;
        private bool _shaded;
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
        /// 🧑's requirement was that the beam be *"in sync with the player's settings for the
        /// slipper highlights"*, and a claim nothing can fail is not a match.
        /// </summary>
        public Color Colour => _colour;

        /// <summary>
        /// Whether the real shader is what is drawing, rather than `VfxMaterial.Beam`'s flat
        /// fallback.
        ///
        /// ⚠️ IT IS PUBLIC SO A TEST CAN TELL THE TWO APART, because a render cannot always. The
        /// fallback is a coloured cylinder standing in the right place at the right height: from
        /// the player's own eyes looking down at the road it is very nearly the same picture, and
        /// it is the picture the owner rejected.
        /// </summary>
        public bool Shaded => _shaded;

        /// <summary>
        /// ⚠️ BUILT ON FIRST USE RATHER THAN WITH THE SLIPPER. Most tsinelas in most rounds never
        /// need one: the taya's is parked, the other seats' are somebody else's, and a shoe in a
        /// hand has nothing to stand on. Building the renderers and a light per slipper at match
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

            Tint();
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

            // ⚠️⚠️ THE POOL IS FIRST BUT THE DRAW ORDER IS THE SHADER'S JOB. The column's queue is
            // Transparent+1, so it draws over the pool whatever order these two renderers were
            // created in. Creating it first is belt and braces, not the mechanism.
            //
            // ⚠️ 6 mm OFF THE ROAD. Coplanar with it is z-fighting on every surface in the game,
            // and higher than that is a disc visibly hovering when you crouch beside your own shoe.
            _shaded = PaintOnce(Disc("GroundPool", PoolDiameter, 0.004f, floor + 0.006f),
                            PoolAlpha, pool: true);

            // ⚠️⚠️ THE RIBBON IS A QUAD WHOSE CORNERS THE SHADER PLACES. Its object origin is the
            // road under the shoe and nothing else about its transform is read: the vertex shader
            // swings it to face the camera and sizes it in world metres from `_Height` and
            // `_Width`. Without the shader it would be a fixed card lying in one plane, so the
            // flat fallback gets the old cylinder instead, which is at least round.
            _shaded &= PaintOnce(VfxMaterial.BeamAvailable
                                     ? Ribbon(floor)
                                     : Disc("Column", HalfWidth * 2.0f, Height, floor + Height * 0.5f),
                                 BaseAlpha, pool: false);

            var column = _painted.Count > 1 ? _painted[_painted.Count - 1] : null;
            if (column != null && column.HasProperty(HeightId))
            {
                column.SetFloat(HeightId, Height);
                column.SetFloat(WidthId, HalfWidth);
            }

            // ⚠️ ONE LIGHT, NO SHADOWS, AND THERE IS NEVER MORE THAN ONE IN THE SCENE. The beam
            // only ever draws on the local player's own tsinelas, so this cannot multiply the way
            // a per-seat effect would. `CreateZapFlightVisual` sets the same three fields for the
            // same reason.
            //
            // ⚠️⚠️ AND IT IS THE ONE PART OF THE EFFECT A SHADER CANNOT DO. The shader lights its
            // own pixels and nothing else: without this the road under the shoe stays exactly as
            // dark as the road beside it, and a beam that does not spill on what it stands on
            // reads as drawn over the scene rather than as standing in it.
            var lampGo = new GameObject("Glow");
            lampGo.transform.SetParent(_column, false);
            lampGo.transform.localPosition = new Vector3(0.0f, floor + 0.15f, 0.0f);
            _lamp = lampGo.AddComponent<Light>();
            _lamp.type = LightType.Point;
            _lamp.range = 0.9f;
            _lamp.shadows = LightShadows.None;
        }

        /// <summary>
        /// One quad, x -1..1 across and y 0..1 up, with its origin on the road.
        ///
        /// ⚠️ THE BOUNDS ARE SET BY HAND, AND GENEROUSLY. Unity culls a renderer against its mesh
        /// bounds in object space, and this mesh's real corners are only decided in the vertex
        /// shader, so bounds computed from the two-unit quad would cull a 1.9 m line the moment
        /// its foot left the frame while its top was still in it.
        /// </summary>
        private Renderer Ribbon(float floor)
        {
            var go = new GameObject("Ribbon");
            go.transform.SetParent(_column, false);
            go.transform.localPosition = new Vector3(0.0f, floor, 0.0f);

            var mesh = new Mesh { name = "SlipperBeamRibbon" };
            mesh.vertices = new[]
            {
                new Vector3(-1.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(-1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f),
            };
            mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
            // Oversized again because these bounds are in the TSINELAS'S scale, which is per skin
            // and can be well under one, while the shader sizes the ribbon in world metres.
            mesh.bounds = new Bounds(new Vector3(0.0f, Height, 0.0f),
                                     new Vector3(12.0f, Height * 6.0f, 12.0f));
            _ribbonMesh = mesh;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            return go.AddComponent<MeshRenderer>();
        }

        private Mesh _ribbonMesh;

        private void OnDestroy()
        {
            if (_ribbonMesh != null) Destroy(_ribbonMesh);
        }

        /// <summary>
        /// ⚠️⚠️ `CreatePrimitive` HANDS BACK A COLLIDER AND A TSINELAS MAY NOT HAVE ONE.
        /// `MatchInstaller.StripColliders`' note is the receipt: *"a thrown slipper starts shoving
        /// players around the arena"*. `VfxMaterial.Beam` strips it for everything it paints, and
        /// this strips it again before that call because a primitive is dangerous from the frame
        /// it exists, not from the frame it is painted.
        /// </summary>
        private Renderer Disc(string name, float diameter, float height, float centreY)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(_column, false);
            go.transform.localScale = new Vector3(diameter, height * 0.5f, diameter);
            go.transform.localPosition = new Vector3(0.0f, centreY, 0.0f);

            VfxMaterial.StripCollider(go);
            return go.GetComponent<Renderer>();
        }

        private bool PaintOnce(Renderer art, float alpha, bool pool)
        {
            if (art == null) return false;

            bool shaded = VfxMaterial.Beam(art, _colour, alpha, pool);
            _painted.Add(art.sharedMaterial);
            return shaded;
        }

        /// <summary>
        /// Writes the live colour into materials that already exist.
        ///
        /// ⚠️ THE SETTINGS PANEL IS REACHABLE FROM THE IN-MATCH PAUSE MENU, so a colour change
        /// has to reach a beam that is already standing. That is the same live-repaint
        /// requirement `Slipper.OnEnable` records for the rim, and it is why this writes into
        /// stored materials rather than rebuilding them.
        /// </summary>
        private void Tint()
        {
            var opaque = new Color(_colour.r, _colour.g, _colour.b, 1.0f);

            for (int i = 0; i < _painted.Count; i++)
            {
                var m = _painted[i];
                if (m == null) continue;

                if (m.HasProperty(ColourId)) m.SetColor(ColourId, opaque);

                // The fallback material is `Ghost`'s, which carries none of the shader's
                // properties and is tinted through these instead. Both are written because the
                // fallback is reached in a player, where nobody is watching for a warning.
                if (!_shaded)
                {
                    m.color = new Color(opaque.r, opaque.g, opaque.b, m.color.a);
                    if (m.HasProperty(BaseColourId)) m.SetColor(BaseColourId, m.color);
                }
            }
        }

        /// <summary>
        /// Drives the one float the whole effect hangs off.
        ///
        /// ⚠️ ONE `SetFloat` PER RENDERER PER FRAME IS THE ENTIRE PER-FRAME COST OF THIS FEATURE.
        /// The billboard, the core, the haze and the climbing sparkles are all in the shader, so
        /// nothing here has to know about any of them.
        /// </summary>
        private void Paint(float strength)
        {
            for (int i = 0; i < _painted.Count; i++)
            {
                var m = _painted[i];
                if (m == null) continue;

                if (m.HasProperty(StrengthId))
                {
                    m.SetFloat(StrengthId, strength);
                }
                else
                {
                    // The fallback again: no `_Strength`, so the strength has to go into the
                    // alpha the flat material does have.
                    var c = m.color;
                    m.color = new Color(c.r, c.g, c.b, BaseAlpha * strength);
                    if (m.HasProperty(BaseColourId)) m.SetColor(BaseColourId, m.color);
                }
            }

            if (_lamp != null)
            {
                _lamp.color = _colour;
                _lamp.intensity = 0.16f * strength;
            }
        }

        private static readonly int HeightId = Shader.PropertyToID("_Height");
        private static readonly int WidthId = Shader.PropertyToID("_Width");
        private static readonly int ColourId = Shader.PropertyToID("_Color");
        private static readonly int StrengthId = Shader.PropertyToID("_Strength");
        private static readonly int BaseColourId = Shader.PropertyToID("_BaseColor");

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
        /// from the fight it is standing beside. The movement a player actually reads is the
        /// shader's climbing sparkles, which cost nothing here. The ribbon's FACING ignores this
        /// rotation (the shader turns it to the camera), but its origin, the pool and the lamp
        /// all hang below the shoe and need it.
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

            // A slow breath rather than a throb; the climbing sparkles in the shader are the
            // motion a player reads.
            float pulse = 0.92f + 0.08f * Mathf.Sin(Time.time * 1.8f);
            Paint(strength * pulse);
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
