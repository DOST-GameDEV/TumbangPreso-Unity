using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE WEIGHT OF THE LONGHORN IN LIVE PLAY (2026-10-10). The owner, of CONTINENTAL DRIFT as play showed it after
    /// the cutscene: "the actual in-game ulitmate is just effects on the ground, it doesnt sell that weight of the
    /// longhorn's slampunch". The cutscene ends on its fist landing; play then released five bands of fracture marks.
    /// Now each band is that fist coming down on it: the same painted fist and forearm (`Models/UltimateProps/longhorn-fist`)
    /// drops out of a knot of dust over the band, lands on the band's release, flattens against the court and stops
    /// dead, the court heaves up round it, grit is thrown, molten light breaks out underneath, and it is drawn back up
    /// as the next one falls further on, left fist then right. A giant punching its way up the court.
    ///
    /// ⚠️ LOOK ONLY, AND A PURE FUNCTION OF THE WAVE'S AGE. `DanteDriftVisual` steps this with the same accepted age it
    /// steps the fracture marks with, so every peer, a rejoin and the replay draw the same fist at the same height: no
    /// clock of its own, no `Update`, no random. It decides nothing: who is hit, when, and how wide are `DanteDriftWave`'s.
    /// It has no collider. It lives beside the marks under the wave's own object and dies with it.
    ///
    /// ⚠️ A BLOW LANDS EXACTLY ON ITS BAND'S RELEASE (band times `GeoRules.DriftInterval`), so the fall is BEFORE it. The
    /// first band is released at age 0, the frame the cutscene's fist has just landed. That blow is the longhorn's own
    /// fist, and the longhorn is standing there in play (`Abilities.DanteLonghornPerch`), so the first blow draws no
    /// arm of its own: only what the fist does to the court, where that fist is.
    ///
    /// ⚠️ PEOPLE ARE AIMING THROUGH THIS. Each arm is on screen for under a second and stands on a band that is being
    /// struck anyway; the dust it comes out of is five metres up; nothing else here is taller than a player's knee.
    /// </summary>
    public sealed class DanteDriftFist : MonoBehaviour, IVfxTimeline
    {
        // How long before its band's release a fist is first seen, how long it stays down, and how long it takes to leave.
        const float Fall = .24f, Flatten = .09f, Down = .24f, Leave = .38f, From = 7f, Size = 1.5f;
        static readonly int ColourId = Shader.PropertyToID("_Color"), BaseColourId = Shader.PropertyToID("_BaseColor");

        sealed class Blow
        {
            public Transform Holder, Arm, Rubble, Cracks, Ring;
            public Renderer RingSkin, Flash, Pool;
            public readonly Transform[] Grit = new Transform[7], Dust = new Transform[3], Sparks = new Transform[5];
            public float Side;
            // The effects kit's pieces (`UltimateFx`): the scorch a blow leaves, the molten light that races out over it,
            // and the smoke it throws. Null on a machine that cannot draw the kit's shader; the blow plays without them.
            public UltimateFx.Piece Scorch, Molten;
            public readonly UltimateFx.Piece[] Smoke = new UltimateFx.Piece[2];
            public readonly MaterialPropertyBlock Block = new MaterialPropertyBlock();
        }
        readonly Blow[] _blows = new Blow[GeoRules.DriftBlasts];
        bool _calm;
        public float LifeSeconds => GeoRules.DriftSeconds;

        public static DanteDriftFist Build(Transform parent, Vector3 origin, Vector3 forward, float halfWidth, float reach)
        {
            var root = new GameObject("ContinentalDriftFists");
            root.transform.SetParent(parent, false);
            var ground = origin; ground.y = VfxShapes.GroundPoint(origin).y;
            forward.y = 0;
            root.transform.SetPositionAndRotation(ground, Quaternion.LookRotation(forward.sqrMagnitude > .001f ? forward.normalized : Vector3.forward, Vector3.up));
            var view = root.AddComponent<DanteDriftFist>();
            view._calm = Settings.SettingsStore.Current.ReducedEffects;
            float depth = reach / GeoRules.DriftBlasts;
            for (int i = 0; i < view._blows.Length; i++)
            {
                // The first blow is the longhorn's own right fist, where the cutscene (and `DanteLonghornPerch`, which stands
                // the beast there in play) has it: 2.75 m to his right and 3 m ahead. The rest go left, right, left up the
                // court, each in the middle of its band and a fifth of the way off the centre line.
                float side = i % 2 == 0 ? 1 : -1;
                var at = i == 0 ? new Vector3(Mathf.Min(2.75f, halfWidth * .9f), 0, Mathf.Min(3f, depth * .9f))
                                : new Vector3(side * Mathf.Min(halfWidth * .2f, 2.2f), 0, (i + .5f) * depth);
                view._blows[i] = view.BuildBlow(i, at, side);
            }
            view.StepTo(0);
            return view;
        }

        Blow BuildBlow(int index, Vector3 at, float side)
        {
            var blow = new Blow { Side = side };
            blow.Holder = new GameObject("Blow" + index).transform;
            blow.Holder.SetParent(transform, false); blow.Holder.localPosition = at;

            // The fist. Dressed at its own size, then scaled, so its ink line grows with it.
            // Not for the first blow: that fist is on the end of the longhorn's arm.
            var arm = index == 0 ? null : UltimateProp.Spawn("longhorn-fist", blow.Holder, UltimateProp.EarthPalette, ToonSkin.PersonOutlineWidth * 3f);
            if (arm != null) blow.Arm = arm.transform;

            var stone = new Color(.25f, .22f, .2f, 1); var dust = new Color(.58f, .49f, .4f, 1); var lava = new Color(1f, .5f, .1f, 1);
            blow.Rubble = Solid(blow.Holder, "HeavedCourt", VfxShapes.Upheaval(9, .06f, .6f, 301 + index, .1f), stone, .45f);
            blow.Cracks = Solid(blow.Holder, "MoltenCracks", VfxShapes.Fracture(6, 3, .05f, 311 + index), lava * .3f, 0);
            Heat(blow.Cracks.GetComponent<Renderer>(), lava);
            var ring = VfxShapes.Stand(blow.Holder, "DustRing", VfxShapes.Collar(22, .3f, .8f, .22f, 0, 321 + index), 1);
            blow.Ring = ring.transform; blow.RingSkin = ring.GetComponent<Renderer>();
            blow.RingSkin.shadowCastingMode = ShadowCastingMode.Off; blow.RingSkin.receiveShadows = false;
            VfxMaterial.Ghost(blow.RingSkin, new Color(dust.r, dust.g, dust.b, .6f), .05f);
            for (int i = 0; i < blow.Grit.Length; i++)
                blow.Grit[i] = Solid(blow.Holder, "Grit" + i, VfxShapes.Spire(4, .3f, .4f, 331 + index * 7 + i), stone * 1.1f, .45f);
            for (int i = 0; i < blow.Dust.Length; i++)
            {
                blow.Dust[i] = Solid(blow.Holder, "Dust" + i, VfxShapes.NovaShell(5, 8, .1f, 341 + index * 3 + i), dust * .4f, 0);
                Heat(blow.Dust[i].GetComponent<Renderer>(), dust * .74f);
            }
            for (int i = 0; i < blow.Sparks.Length; i++)
            {
                blow.Sparks[i] = Solid(blow.Holder, "Ember" + i, VfxShapes.Prism(4, 1f, .12f), lava * .3f, 0);
                Heat(blow.Sparks[i].GetComponent<Renderer>(), new Color(1f, .7f, .22f, 1));
            }
            if (UltimateFx.Ready)
            {
                // The same three the cutscene's stomp has (`HeroIntroductionScene.DanteFx.cs`), at a blast's size: a black
                // burn torn into islands with fissures alight in it, a disc of light eaten from its middle, and two curls
                // of smoke with a burning edge. All of it lies on the court or stands under a player's head for half a second.
                Color soot = new Color(.06f, .05f, .045f, 1f), magma = new Color(.8f, .22f, .03f, 1f), hot = new Color(1f, .8f, .3f, 1f);
                blow.Scorch = UltimateFx.Disc(blow.Holder, soot, magma, UltimateFx.Ink, 3).With("_Hot", lava).With("_Glow", 1.3f).With("_Radial", .35f)
                    .With("_Scale", new Vector4(2.4f, 5f, 0, 0)).With("_Scroll", new Vector4(0, 0, 0, 0)).With("_Band", .14f).With("_Rim", .07f);
                blow.Scorch.Seed = 3.1f + index * 1.7f;
                blow.Molten = UltimateFx.Disc(blow.Holder, lava, hot, UltimateFx.Streak, 5).With("_Hot", hot).With("_Glow", .7f).With("_Emit", .9f)
                    .With("_Scale", new Vector4(1.1f, 8f, 0, 0)).With("_Spread", 1.2f).With("_Band", .3f);
                for (int i = 0; i < blow.Smoke.Length; i++)
                {
                    blow.Smoke[i] = UltimateFx.Wisp(blow.Holder, i == 0 ? "hook" : "arc", soot, lava, UltimateFx.Smoke, 8).With("_Hot", hot).With("_Glow", .9f)
                        .With("_Scale", new Vector4(1.8f, 1.5f, 0, 0)).With("_Scroll", new Vector4(2.4f, .2f, 0, 0)).With("_Sweep", 1.1f).With("_Shape", .7f).With("_Band", .22f);
                    blow.Smoke[i].Seed = 2 + index * 3 + i * 1.9f;
                }
            }
            blow.Flash = Lamp(blow.Holder, "SlamFlash", true, 1.7f, .4f);
            blow.Pool = Lamp(blow.Holder, "MoltenLight", false, 1.4f, .3f);
            blow.Holder.gameObject.SetActive(false);
            return blow;
        }

        static Transform Solid(Transform parent, string name, Mesh mesh, Color colour, float fill)
        {
            var go = VfxShapes.Stand(parent, name, mesh, 1);
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            colour.a = 1;
            VfxMaterial.Solid(renderer, colour);
            if (fill > 0) Heat(renderer, colour * fill);
            return go.transform;
        }

        /// <summary>Its own light: a small fill on rock so it is not a black cutout at dusk, nearly all of it on lava and dust.</summary>
        static void Heat(Renderer renderer, Color light)
        {
            var material = renderer.sharedMaterial;
            if (material == null || !material.HasProperty("_EmissionColor")) return;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(light.r, light.g, light.b, 1));
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        /// <summary>A piece of additive light (`Shaders/SpiritGlow`): a quad turned to the lens, or lying as it is posed.</summary>
        static Renderer Lamp(Transform parent, string name, bool billboard, float falloff, float core)
        {
            var mesh = new Mesh { name = "DriftFistLight" };
            mesh.vertices = new[] { new Vector3(-.5f, -.5f, 0), new Vector3(.5f, -.5f, 0), new Vector3(.5f, .5f, 0), new Vector3(-.5f, .5f, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2f);
            var go = VfxShapes.Stand(parent, name, mesh, 1);
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            var shader = Resources.Load<Shader>("Shaders/SpiritGlow");
            if (shader == null) { renderer.enabled = false; VfxRenderTag.Attach(go); return renderer; }
            var material = new Material(shader) { name = name };
            material.SetFloat("_Billboard", billboard ? 1f : 0f); material.SetFloat("_Band", 0f);
            material.SetFloat("_Falloff", falloff); material.SetFloat("_Core", core); material.SetFloat("_Lift", billboard ? .5f : 0f);
            renderer.sharedMaterial = material;
            VfxRenderTag.Own(go, material);
            return renderer;
        }

        static float Fraction(float n) { float s = Mathf.Sin(n * 12.9898f) * 43758.5453f; return s - Mathf.Floor(s); }
        static float Out(float u) { u = Mathf.Clamp01(u); return 1 - (1 - u) * (1 - u); }
        static float Pop(float age) => age <= 0 || age >= 1 ? 0 : age < .25f ? age / .25f : 1 - (age - .25f) / .75f;
        static void Show(Transform piece, bool on) { if (piece.gameObject.activeSelf != on) piece.gameObject.SetActive(on); }

        void Glow(Blow blow, Renderer light, Color colour, float strength)
        {
            if (light == null || light.sharedMaterial == null) return;
            colour.a = Mathf.Max(0, strength * (_calm ? .4f : 1f));
            blow.Block.SetColor(ColourId, colour);
            light.SetPropertyBlock(blow.Block);
            light.enabled = colour.a > .002f;
        }

        public void StepTo(float seconds)
        {
            for (int i = 0; i < _blows.Length; i++)
            {
                var blow = _blows[i];
                float age = seconds - i * GeoRules.DriftInterval;
                bool live = age >= -Fall - .08f && age < GeoRules.DriftVisualTail;
                Show(blow.Holder, live);
                if (live) Step(blow, i, age);
            }
        }

        void Step(Blow blow, int index, float age)
        {
            var ember = new Color(1f, .5f, .1f, 1);

            // ------------------------------------------------------------- the fist
            if (blow.Arm != null)
            {
                bool seen = age >= -Fall && age < Down + Leave;
                Show(blow.Arm, seen);
                if (seen)
                {
                    float high, lean = 0; var scale = Vector3.one;
                    if (age < 0)
                    {
                        // Falling: slow off the top and faster all the way, swinging in from the caster's side. Nothing eases
                        // it at the bottom; the court is what stops it.
                        float u = (age + Fall) / Fall;
                        high = From * (1 - u * u); lean = -16f * (1 - u);
                        scale = new Vector3(1 - .06f * u, 1 + .1f * u, 1 - .06f * u);
                    }
                    else if (age < Down)
                    {
                        // Landed: it flattens against the court, springs back a little past its shape, and sits shuddering.
                        float flat = Mathf.Sin(Mathf.Clamp01(age / Flatten) * Mathf.PI);
                        float back = Mathf.Sin(Mathf.Clamp01((age - Flatten) / .1f) * Mathf.PI) * .05f;
                        scale = new Vector3(1 + .16f * flat - back, 1 - .22f * flat + back, 1 + .16f * flat - back);
                        high = 0; lean = _calm ? 0 : Mathf.Sin(age * 90f) * 1.2f * (1 - age / Down);
                    }
                    else
                    {
                        // Drawn back up: it bears down first, then goes, slowly and then quickly, thinning into the dust.
                        float u = (age - Down) / Leave;
                        float press = Mathf.Sin(Mathf.Clamp01(u / .22f) * Mathf.PI) * .12f;
                        float up = Mathf.Clamp01((u - .12f) / .88f); up *= up;
                        high = From * 1.05f * up - press; lean = 10f * up;
                        scale = new Vector3(1 - .35f * up, 1 + .12f * up, 1 - .35f * up);
                    }
                    blow.Arm.localPosition = new Vector3(0, high, 0);
                    blow.Arm.localRotation = Quaternion.Euler(lean, 0, blow.Side * -6f * Mathf.Clamp01(high / From));
                    blow.Arm.localScale = scale * Size;
                }
            }

            // The knot of dust it comes out of and goes back into, over the band and well above anyone's head.
            for (int i = 0; i < blow.Dust.Length; i++)
            {
                float life = (age + Fall + .06f - i * .05f) / (Fall + Down + Leave + .2f);
                float size = Pop(life) * (1.5f + .5f * i);
                Show(blow.Dust[i], size > .05f);
                blow.Dust[i].localPosition = new Vector3((i - 1) * 1.1f, From - .9f + i * .35f + life * .5f, (i % 2 == 0 ? .5f : -.4f));
                blow.Dust[i].localScale = Vector3.one * size;
                blow.Dust[i].localRotation = Quaternion.Euler(life * 70, i * 80, life * 40);
            }

            // ------------------------------------------------------------- the blow
            bool landed = age >= 0;
            // The court heaves up round it at once, stands while the fist is down, and sinks back as the band cools.
            {
                float up = Mathf.Clamp01(age / .07f), sink = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.6f, 1.05f, age));
                Show(blow.Rubble, landed && sink < .98f);
                float wide = Mathf.Lerp(1.4f, 3.0f, Out(up));
                blow.Rubble.localPosition = new Vector3(0, .04f, 0);
                blow.Rubble.localRotation = Quaternion.Euler(0, index * 67, 0);
                blow.Rubble.localScale = new Vector3(wide, 1.5f * Out(up) * (1 + .25f * Mathf.Sin(up * Mathf.PI)) * (1 - sink), wide);
            }
            // Molten light breaks through under it: cracks run out bright, then cool and close.
            {
                float run = Out(age / .16f), cool = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.35f, .95f, age));
                Show(blow.Cracks, landed && cool < .98f);
                blow.Cracks.localPosition = new Vector3(0, .035f, 0);
                blow.Cracks.localRotation = Quaternion.Euler(0, index * 41 + 15, 0);
                blow.Cracks.localScale = new Vector3(Mathf.Lerp(.6f, 3.6f, run), 1, Mathf.Lerp(.6f, 3.6f, run)) * (1 - .55f * cool);
                blow.Pool.transform.localPosition = new Vector3(0, .12f, 0);
                blow.Pool.transform.localRotation = Quaternion.Euler(90, 0, 0);
                blow.Pool.transform.localScale = new Vector3(8.5f, 8.5f, 1);
                Glow(blow, blow.Pool, ember, landed ? (1 - cool) * (.45f + .35f * Mathf.Clamp01(1 - age / .2f)) : 0);
                blow.Flash.transform.localPosition = new Vector3(0, .7f, 0);
                blow.Flash.transform.localScale = Vector3.one * Mathf.Lerp(3f, 5.5f, Mathf.Clamp01(age / .1f));
                Glow(blow, blow.Flash, ember, landed ? Mathf.Clamp01(1 - age / .2f) * .9f : 0);
            }
            // The kit's part of it: light racing out and eaten, the scorch it leaves cooling to nothing as the band clears,
            // and smoke curling up off it, eaten from its foot.
            if (blow.Scorch != null)
            {
                float u = Mathf.Clamp01(age / .3f), reach = Mathf.Lerp(1.2f, 6.5f, Out(u));
                UltimateFx.Place(blow.Molten, new Vector3(0, .09f, 0), Quaternion.identity, new Vector3(reach, 1, reach));
                UltimateFx.Set(blow.Molten, landed ? age : -1f, Mathf.Lerp(.12f, 1f, Mathf.SmoothStep(0, 1, u)), _calm ? .5f : 1f);
                float burnt = Mathf.Lerp(1f, 3.6f, Out(u)), clear = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.6f, GeoRules.DriftVisualTail, age));
                UltimateFx.Place(blow.Scorch, new Vector3(0, .07f, 0), Quaternion.Euler(0, index * 53f, 0), new Vector3(burnt, 1, burnt));
                UltimateFx.Set(blow.Scorch, landed ? age : -1f, Mathf.Lerp(.42f, 1f, clear));
                for (int i = 0; i < blow.Smoke.Length; i++)
                {
                    float life = Mathf.Clamp01((age - i * .04f) / .8f), grown = (2.6f - i * .5f) * (.3f + .7f * Out(life / .5f));
                    UltimateFx.Place(blow.Smoke[i], new Vector3(0, .15f, 0), Quaternion.Euler(0, index * 71f + i * 150f, 0) * Quaternion.Euler(8f, 0, 0),
                                     new Vector3(grown * .8f, grown, grown * (.7f + .3f * life)));
                    UltimateFx.Set(blow.Smoke[i], landed ? age - i * .04f : -1f, Mathf.Lerp(.2f, 1f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.18f, 1f, life))));
                }
            }
            // A ring of dust runs out low across the court.
            {
                float life = Mathf.Clamp01(age / .55f), wide = Mathf.Lerp(1.6f, 7f, Out(life));
                Show(blow.Ring, landed && life < 1);
                blow.Ring.localPosition = new Vector3(0, .1f, 0);
                blow.Ring.localScale = new Vector3(wide, Mathf.Lerp(1.3f, .4f, life), wide);
                var tint = new Color(.58f, .49f, .4f, .6f * (1 - life));
                blow.Block.SetColor(ColourId, tint); blow.Block.SetColor(BaseColourId, tint);
                blow.RingSkin.SetPropertyBlock(blow.Block);
            }
            // Grit thrown out and up, turning over, gone where each comes down.
            for (int i = 0; i < blow.Grit.Length; i++)
            {
                float h = Fraction(index * 7.3f + i * 5.1f), g = Fraction(index * 3.9f + i * 2.7f), angle = (i * 51 + index * 23) * Mathf.Deg2Rad;
                float far = 1.2f + (2.2f + 2.6f * h) * age, high = .2f + (5.5f + 3.5f * g) * age - 11f * age * age;
                Show(blow.Grit[i], landed && high > .05f);
                float size = (.09f + .09f * g) * Mathf.Clamp01(age / .05f);
                blow.Grit[i].localPosition = new Vector3(Mathf.Cos(angle) * far, high, Mathf.Sin(angle) * far);
                blow.Grit[i].localScale = new Vector3(size, size * 2.4f, size);
                blow.Grit[i].localRotation = Quaternion.Euler(i * 70 + age * 520 * (h - .5f) * 2, i * 50, i * 110 + age * 430);
            }
            // And embers go up off the cracks after it.
            for (int i = 0; i < blow.Sparks.Length; i++)
            {
                float h = Fraction(index * 9.1f + i * 4.3f), life = (age - .04f - i * .05f) / (.6f + .3f * h), angle = (i * 72 + index * 31) * Mathf.Deg2Rad;
                float size = Pop(life);
                Show(blow.Sparks[i], size > .05f);
                blow.Sparks[i].localPosition = new Vector3(Mathf.Cos(angle) * (1f + h), .2f + life * (1.4f + h), Mathf.Sin(angle) * (1f + h));
                blow.Sparks[i].localScale = new Vector3(.08f, .3f, .08f) * size;
                blow.Sparks[i].localRotation = Quaternion.Euler(0, i * 37 + life * 200, 0);
            }
        }
    }
}
