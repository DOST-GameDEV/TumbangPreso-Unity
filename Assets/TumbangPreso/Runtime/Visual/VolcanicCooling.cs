using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// Takes the heat out of a volcanic surface over the life of the zone that owns it.
    ///
    /// ⚠️⚠️ THIS IS THE MOTION CHANNEL, AND `Hero_Strike_Balance.md` § 8.5 ITEM 2 IS THE ENTRY
    /// IT CLOSES FOR DANTE: *"a spent effect and a live one look identical, so a player cannot
    /// tell whether a patch of ice is about to expire. That is a real gameplay read and it is
    /// free."* Seismic Stomp leaves a 4.0 s decal on the court and, until this, second 0.2 and
    /// second 3.9 were the same picture. A player deciding whether to cross it had nothing to
    /// read but their own count.
    ///
    /// ⚠️ ROCK GOING OUT IS THE MOST LEGIBLE POSSIBLE VERSION OF THAT READ, AND IT IS THE ONLY
    /// ONE THAT COSTS NOTHING. The alternative everybody reaches for is fading the whole effect,
    /// which says "this is being deleted" rather than "this is expiring", and on ground it is
    /// worse than that: a half-transparent fracture is a fracture you can see the road through.
    /// § 8.5 item 2 already names the rule for the auras — *"fade the rim, not the whole
    /// thing"* — and this is that rule where the rim is the magma.
    ///
    /// ⚠️ THE CRUST IS DELIBERATELY LEFT ALONE. Cooled basalt does not go anywhere, and the
    /// street keeping its scar for the four seconds the ability is on it is `docs/TODO.md` § 27.4's
    /// motif: displacement is the one element whose signature is that you can see where the
    /// fight was afterwards. What ends is the glow, not the damage.
    ///
    /// ⚠️ IT IS NOT AN `IVfxTimeline`, AND THAT IS A DECISION RATHER THAN AN OMISSION.
    /// `VfxTimeline.StepAll` winds EVERY implementer in the scene to the same fraction of its
    /// own life, and `AbilityShowcaseProbe.Solo` calls it at 0.35. The probe spawns the lava
    /// decal with a 60 s life so it can be photographed at all, so implementing the interface
    /// would wind this to 21 seconds of cooling and photograph Dante's stomp as a cold rock in
    /// every capture from here on. A persistent zone's age is not the thing those frames are
    /// for; the transients are, and they have their own implementers.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class VolcanicCooling : MonoBehaviour
    {
        /// <summary>How long the zone lives. Cooling completes with it.</summary>
        public float Seconds = 4.0f;

        /// <summary>
        /// How much of the life is spent at full heat before it starts going out.
        ///
        /// ⚠️ A HOLD, BECAUSE A HAZARD MUST NOT LOOK SPENT WHILE IT IS STILL DANGEROUS. Dante's
        /// decal is decoration and this is only a look, but the same component is the obvious
        /// thing to hang on a zone that does resolve, and a glow that starts dropping on frame
        /// one would teach a player to walk into live ground. The same shape as the aura curve
        /// in `docs/TODO.md` § 8 item 4: hold, then fall.
        /// </summary>
        public float HoldFraction = 0.45f;

        /// <summary>
        /// The light the lava casts, dimmed on the same curve as the veins.
        ///
        /// ⚠️⚠️ WITHOUT THIS THE MAGMA DOES NOT GLOW, IT IS ONLY BRIGHT, AND 🧑 CALLED THE
        /// DISTINCTION EXACTLY ON 2026-08-28: *"is there glow light from the lava? its emmissive
        /// but we dont have any bloom shader so it wont be glowwy unless theres a bloom shader
        /// OR light coming from the lava"*. He is right on both halves. There is no bloom pass
        /// anywhere in this project, and `ColourGrade` does keep an HDR target so a vein above
        /// 1.0 rolls off through the tonemap rather than clipping, but a roll-off only decides
        /// the colour of the pixels the vein already covers. Nothing SPREADS it. A 4 cm crack at
        /// 1.9 is a thin bright line and the eye reads exactly that.
        ///
        /// ⚠️ SO THE GLOW IS A REAL LIGHT, WHICH IS THE CHEAPER OF HIS TWO OPTIONS AND THE MORE
        /// HONEST ONE. A bloom pass is a full-screen post that would touch the stack
        /// `PostAntiAlias` and `ColourGrade` own, and it would bloom every bright thing in the
        /// game to fix one ability. A point light over the decal spills onto the road, the
        /// upheaval slabs, the launched rock and the legs of anybody standing in it, which is
        /// what a hole full of lava would actually do and what a screen-space blur cannot fake.
        ///
        /// ⚠️ AND IT MUST DIE WITH THE HEAT OR IT CONTRADICTS THE THING IT LIGHTS. The veins go
        /// out on `_Cool` and a light still burning over cold rock would read as a bug. It is
        /// the same curve, so the two cannot disagree.
        /// </summary>
        public Light Glow;

        /// <summary>Intensity at full heat. Faded to zero by the end of the life.</summary>
        public float GlowIntensity = 2.6f;

        /// <summary>
        /// How far the whole zone sinks before it is deleted, in metres.
        ///
        /// ⚠️⚠️ IT LEAVES BY GOING BACK INTO THE ROAD, BECAUSE `Object.Destroy` IS NOT AN EXIT.
        /// 🧑 2026-08-28: *"make it clip/fall into the ground when the effect is over instead of
        /// disappearing immediately"*. The zone used to vanish between two frames at full
        /// opacity, which reads as the game removing an object rather than as the effect ending,
        /// and it is the loudest possible way to break the illusion that this is real broken
        /// road: real ground does not blink out.
        ///
        /// ⚠️ SINKING IS THE RIGHT VERB HERE AND FADING IS NOT. The crust is OPAQUE on purpose
        /// (`docs/TODO.md` § 19.2a: ground that has been burnt or broken writes depth so it
        /// cannot be out-sorted), so fading it would both undo that and read as the rock turning
        /// to glass. Sinking needs no transparency at all: the street is opaque geometry that
        /// already writes depth, so once the slabs are under it they are occluded by the world
        /// itself. The clip is free and it is the same clip a player would get from any solid.
        ///
        /// ⚠️ AND IT MUST CLEAR THE TALLEST PIECE, NOT THE DECAL. The crust lies at 22 mm and
        /// would be gone almost at once; the upheaval slabs stand `rise` 0.72 through `Stand`'s
        /// `radius * 0.62` height scale, which is about 0.98 m on a 2.2 m stomp. A sink tuned to
        /// the flat part leaves the slabs standing in a hole, so the caller passes a depth taken
        /// from the radius the slabs were built against.
        /// </summary>
        public float SinkDepth = 1.4f;

        /// <summary>
        /// The fraction of the life at which it starts going down.
        ///
        /// ⚠️ LATE, AND AFTER THE COOLING RATHER THAN WITH IT. Two things reading at once is one
        /// thing nobody can read: if it dimmed and sank together, the player would see one blurry
        /// event instead of "the heat went out, and then the ground took it back". It also keeps
        /// the sink out of the window where the decal still has to be legible.
        /// </summary>
        public float SinkStart = 0.78f;

        /// <summary>
        /// The fraction of the life at which it is fully under the road.
        ///
        /// ⚠️⚠️ IT IS BEFORE 1.0 ON PURPOSE, AND `VolcanicZoneTests` IS WHAT FOUND THAT IT HAD TO
        /// BE. The first version sank from `SinkStart` to exactly 1.0, which is the same instant
        /// `ExpiryCue` deletes the object, so the zone reached its full depth on the frame it
        /// stopped existing and every frame before that was part way down. Measured: it had
        /// dropped 0.95 m against a tallest slab of 0.99 m, so the last thing the player actually
        /// saw was a slab still standing proud of the road, disappearing. That is the exact
        /// defect the sink was added to remove, arrived at from the other side.
        ///
        /// ⚠️ SO THE SINK FINISHES EARLY AND THE ZONE SPENDS ITS LAST MOMENTS BURIED. That costs
        /// nothing: this decal resolves no damage, it is decoration, and an invisible decoration
        /// is exactly as expensive as a deleted one for the fraction of a second involved.
        /// </summary>
        public float SinkEnd = 0.94f;

        private Renderer[] _renderers;
        private Vector3 _restPosition;
        private float _elapsed;
        private static readonly int CoolId = Shader.PropertyToID("_Cool");

        /// <summary>
        /// Attach to a spawned zone and take its children with it.
        ///
        /// ⚠️ THE RENDERERS ARE COLLECTED ONCE, AT ATTACH. Every piece of one of these zones is
        /// created by its spawner before this is added, and nothing is added later; walking the
        /// hierarchy every frame would be `GetComponentsInChildren` in an `Update` on up to
        /// twenty renderers, which is the shape of allocation `docs/TODO.md` § 15 measured
        /// costing the 6x probe an eighth of its frames.
        /// </summary>
        public static VolcanicCooling Attach(GameObject go, float seconds, float hold = 0.45f)
        {
            if (go == null) return null;

            var cooling = go.AddComponent<VolcanicCooling>();
            cooling.Seconds = seconds;
            cooling.HoldFraction = hold;
            cooling._renderers = go.GetComponentsInChildren<Renderer>(true);

            // ⚠️ THE REST POSITION IS CAPTURED, NOT READ EVERY FRAME. The sink writes the
            // transform, so deriving the target from the CURRENT position would compound its own
            // output and the zone would accelerate through the floor and keep going.
            cooling._restPosition = go.transform.position;
            return cooling;
        }

        private void Update()
        {
            if (_renderers == null || Seconds <= 0.0f) return;

            _elapsed += Time.deltaTime;

            float life = Mathf.Clamp01(_elapsed / Seconds);
            float hold = Mathf.Clamp01(HoldFraction);

            // Zero through the hold, then a smooth ramp to fully out at the end of the life.
            float cool = life <= hold
                ? 0.0f
                : Mathf.SmoothStep(0.0f, 1.0f, (life - hold) / Mathf.Max(0.001f, 1.0f - hold));

            foreach (var r in _renderers)
            {
                if (r == null) continue;

                // ⚠️ `sharedMaterial`, BECAUSE `VfxMaterial.Volcanic` ALREADY BUILT A MATERIAL
                // PER RENDERER AND `VfxRenderTag` OWNS IT. Touching `.material` here would make
                // Unity clone it a second time, which both leaks the clone past the tag that is
                // supposed to free it and writes the cool value into a copy nothing draws.
                var m = r.sharedMaterial;
                if (m == null || !m.HasProperty(CoolId)) continue;

                m.SetFloat(CoolId, cool);
            }

            // ⚠️ THE SINK RUNS BEFORE THE LIGHT AND OUTSIDE ITS NULL CHECK. It was written after
            // it, behind an `if (Glow == null) return`, which meant a zone spawned without a
            // light would cool correctly and then vanish on the last frame exactly as it used to.
            // Nothing spawns one today; `SpawnCrackedLavaDecal` always hands its light over. It
            // would have been silent until the day something did.
            Sink(life);

            if (Glow == null) return;

            // ⚠️ THE FLICKER IS TWO SINES AT UNRELATED RATES, NOT ONE. A single sine is a pulse
            // and a pulse reads as a mechanism blinking; two that never line up read as
            // convection, which is what is actually moving under a crust. The shader's own vein
            // pulse runs at its own rate for the same reason, so the light and the surface are
            // never caught agreeing and going flat together.
            float flicker = 1.0f
                          + Mathf.Sin(Time.time * 7.3f) * 0.06f
                          + Mathf.Sin(Time.time * 11.9f) * 0.04f;

            Glow.intensity = GlowIntensity * (1.0f - cool) * flicker;
        }

        /// <summary>
        /// Take the zone back into the road over the last of its life.
        ///
        /// ⚠️ IT ACCELERATES, BECAUSE THAT IS WHAT COLLAPSING GROUND DOES. `t * t` rather than a
        /// smoothstep: a smoothstep eases out at the end, which would have the slabs slowing to
        /// a gentle stop just as they reach the road and read as a lift lowering them. Squaring
        /// starts imperceptibly and finishes fastest, so the last thing the player sees is the
        /// ground taking it rather than the effect being switched off.
        ///
        /// ⚠️ IT FINISHES AT `SinkEnd`, WHICH IS BEFORE THE DELETION RATHER THAN ON IT. See that
        /// field: sinking all the way to 1.0 means the deepest the zone ever gets is the frame it
        /// is destroyed, so a slab is still standing proud of the road when it blinks out.
        /// </summary>
        private void Sink(float life)
        {
            float start = Mathf.Clamp01(SinkStart);
            float end = Mathf.Clamp01(SinkEnd);

            if (life <= start) return;

            float t = end > start ? Mathf.Clamp01((life - start) / (end - start)) : 1.0f;
            transform.position = _restPosition - Vector3.up * (SinkDepth * t * t);
        }
    }
}
