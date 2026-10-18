using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️ HE IS REALLY UP THERE (owner, 2026-10-08, of CONTINENTAL DRIFT's cutscene, which ends with him on the longhorn's
    /// back as its fist lands: "by then dante should still be on the longhorn and only then will it disappeaer into the
    /// ground"; asked whether that is only a look, "he's really up there"; asked whether he is held on it, "he can" walk
    /// off; and 2026-10-10: "he should still be on top of the longhorn post-cutscene and he can jump off it").
    ///
    /// So when the ultimate is released after its cutscene, every peer stands the longhorn up where the cutscene left it
    /// (the same painted model, in the cutscene's last pose: right fist down, head dropped) with a real shelf where the
    /// crown of its back is (no plate of court: that broke up and fell off in the cutscene), and his body is put on it (`Earthquake.OnActivate`, `DanteHeroKit`). It waits a moment and then goes under
    /// over `SinkSeconds`. He rides it down unless he walks or jumps off, and then he falls as anyone falls.
    ///
    /// ⚠️ WHAT THIS DOES AND DOES NOT DECIDE. It is a floor and a picture. It moves nobody: his own motor stands on the
    /// shelf and falls under its own gravity, on the peer that simulates him, like any floor. The shelf goes down no
    /// faster than he falls (it gathers speed at `10.6 m/s²` against his `Balance.CharacterGravity` of 20), so he stays on
    /// it. It hits nobody, blocks nobody and scores nothing: the five blasts are `DanteDriftWave`'s and start from the
    /// court as they always did, and he can be hit up there because hits are resolved by distance, not by this.
    ///
    /// ⚠️ ONLY HE CAN STAND ON IT. Every other body is told to ignore the shelf. A beast seven metres across coming up
    /// under three other players would otherwise shove them about, and coming down on one it would trap them; neither is
    /// part of this ultimate.
    ///
    /// ⚠️ NOT IN THE WORLD SNAPSHOT. It lives about a second. A peer that joins inside that second does not see the
    /// longhorn; it still sees him where his owner has him. Putting it in the snapshot is a wire change, and this rides
    /// on protocol 156 without one.
    /// </summary>
    public sealed class DanteLonghornPerch : MonoBehaviour
    {
        /// <summary>How high its back is once it has fully risen: the cutscene's lift (`HeroIntroductionScene.Dante.cs`, `DnTitanTop`).</summary>
        public const float BackHeight = 3.4f;
        public const float HoldSeconds = .25f, SinkSeconds = .8f;
        // The shelf is the crown of rock on its back (the model's `stand` node sits on it): 2.6 m across, as that is.
        const float ShelfThick = .4f, ShelfWide = 2.6f;

        float _age, _rise;
        Vector3 _ground;
        Transform _shelf, _model, _head;
        Transform[] _dust;
        // Where it goes under: the scorch it leaves and the smoke off it (`UltimateFx`), as the cutscene's stomp has them.
        UltimateFx.Piece _scorch;
        readonly UltimateFx.Piece[] _smoke = new UltimateFx.Piece[3];

        /// <summary>
        /// How far up he can be put from <paramref name="feet"/>: the full back, or as much of it as the roof over him allows
        /// (the same measure SUPERNOVA takes, `SeanHeroKit.SupernovaApexRise`). Under a low roof the longhorn is simply less
        /// far out of the ground, as it is part of the way through its rise in the cutscene.
        /// </summary>
        public static float RiseAt(Vector3 feet)
        {
            const float head = 1.5f;
            return Physics.Raycast(feet + Vector3.up * 1.0f, Vector3.up, out var roof, BackHeight + head, ~0, QueryTriggerInteraction.Ignore)
                ? Mathf.Clamp(roof.distance + 1.0f - head, 0f, BackHeight) : BackHeight;
        }

        /// <summary>Stand the longhorn up under <paramref name="rider"/>, whose feet are at <paramref name="feet"/> on the court, its back <paramref name="rise"/> above them.</summary>
        public static DanteLonghornPerch Spawn(CharacterMotor rider, Vector3 feet, float yaw, float rise)
        {
            var go = new GameObject("DanteLonghornPerch");
            go.transform.SetPositionAndRotation(feet, Quaternion.Euler(0, yaw, 0));
            var perch = go.AddComponent<DanteLonghornPerch>();
            perch._ground = feet; perch._rise = rise;

            // The shelf: the only solid part, a slab whose top is the top of its back. Kinematic, because it moves.
            var shelf = new GameObject("LonghornBack"); shelf.transform.SetParent(go.transform, false);
            shelf.AddComponent<Rigidbody>().isKinematic = true;
            var box = shelf.AddComponent<BoxCollider>();
            box.size = new Vector3(ShelfWide, ShelfThick, ShelfWide - .2f);
            box.center = new Vector3(0, 0, .1f);
            perch._shelf = shelf.transform;
            var round = GameServices.Round;
            if (round != null)
                foreach (var actor in round.Players)
                {
                    if (actor == null || actor == rider) continue;
                    foreach (var other in actor.GetComponentsInChildren<Collider>(true)) Physics.IgnoreCollision(box, other, true);
                }

            // The picture: the cutscene's longhorn, in the pose its last frame holds.
            var model = UltimateProp.Spawn("longhorn", go.transform, UltimateProp.EarthPalette, ToonSkin.PersonOutlineWidth * 3.5f);
            if (model != null)
            {
                perch._model = model.transform;
                var arm = PaeteProp.Find(model, "arm-r");
                if (arm != null) arm.localRotation = Quaternion.Euler(-40f, 0, 0);
                perch._head = PaeteProp.Find(model, "head");
            }
            // Dust where it goes under: round pale puffs, opaque, as the cutscene's are.
            perch._dust = new Transform[6];
            for (int i = 0; i < perch._dust.Length; i++)
            {
                var puff = VfxShapes.Stand(go.transform, "SinkDust" + i, VfxShapes.NovaShell(5, 8, .12f, 411 + i), 1);
                Dress(puff, new Color(.22f, .19f, .16f, 1), 1.9f);
                perch._dust[i] = puff.transform;
            }
            if (UltimateFx.Ready)
            {
                Color soot = new Color(.06f, .05f, .045f, 1f), magma = new Color(.8f, .22f, .03f, 1f), lava = new Color(1f, .5f, .1f, 1f), hot = new Color(1f, .8f, .3f, 1f);
                perch._scorch = UltimateFx.Disc(go.transform, soot, magma, UltimateFx.Ink, 3).With("_Hot", lava).With("_Glow", 1.3f).With("_Radial", .35f)
                    .With("_Scale", new Vector4(2.4f, 5f, 0, 0)).With("_Scroll", new Vector4(0, 0, 0, 0)).With("_Band", .14f).With("_Rim", .07f);
                perch._scorch.Seed = 7.3f;
                for (int i = 0; i < perch._smoke.Length; i++)
                {
                    perch._smoke[i] = UltimateFx.Wisp(go.transform, i == 1 ? "arc" : "hook", soot, lava, UltimateFx.Smoke, 8).With("_Hot", hot).With("_Glow", .9f)
                        .With("_Scale", new Vector4(1.8f, 1.5f, 0, 0)).With("_Scroll", new Vector4(2.4f, .2f, 0, 0)).With("_Sweep", 1.1f).With("_Shape", .7f).With("_Band", .22f);
                    perch._smoke[i].Seed = 4 + i * 2.3f;
                }
            }
            perch.Pose();
            Physics.SyncTransforms();
            return perch;
        }

        static void Dress(GameObject piece, Color colour, float fill)
        {
            var renderer = piece.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            VfxMaterial.Solid(renderer, colour);
            var material = renderer.sharedMaterial;
            if (material == null || !material.HasProperty("_EmissionColor")) return;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(colour.r, colour.g, colour.b, 1) * fill);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }

        void Update()
        {
            _age += Time.deltaTime;
            // A round that ends or resets takes it away at once: it must never be standing in the next one.
            if (_age >= HoldSeconds + SinkSeconds || GameServices.Round?.RoundActive != true)
            {
                gameObject.SetActive(false); Destroy(gameObject); return;
            }
            Pose();
            Physics.SyncTransforms();
        }

        void Pose()
        {
            // It leans on its fist a moment, then goes: slowly, then faster all the way down. One steady gathering of speed,
            // slower than a falling body, so whoever stands on it is carried and not dropped.
            float u = Mathf.Clamp01((_age - HoldSeconds) / SinkSeconds), under = u * u;
            float top = _rise * (1 - under);
            _shelf.localPosition = new Vector3(0, top - ShelfThick * .5f, 0);
            float shudder = Settings.SettingsStore.Current.ReducedEffects ? 0 : Mathf.Sin(_age * 47f) * .03f * Mathf.Sin(u * Mathf.PI);
            if (_model != null)
            {
                // The whole of it is under by the end: its horns and claws stand higher than its back.
                _model.localPosition = new Vector3(shudder, top - BackHeight - under * 1.1f, 0);
                if (_head != null) _head.localRotation = Quaternion.Euler(10f + 14f * under, 0, Mathf.Sin(_age * 9f) * 2f * u);
            }
            if (_scorch != null)
            {
                // The ground burns black round it as it goes under, and smoke curls up off the place.
                float burnt = Mathf.Lerp(2.5f, 5.2f, u);
                UltimateFx.Place(_scorch, new Vector3(0, .07f, .4f), Quaternion.Euler(0, 35, 0), new Vector3(burnt, 1, burnt));
                UltimateFx.Set(_scorch, u > 0 ? _age - HoldSeconds : -1f, .45f);
                for (int i = 0; i < _smoke.Length; i++)
                {
                    float life = Mathf.Clamp01((_age - HoldSeconds - i * .12f) / (SinkSeconds * .8f)), grown = (3.4f - i * .5f) * (.3f + .7f * (1 - (1 - life) * (1 - life)));
                    float round = (i * 120 + 60) * Mathf.Deg2Rad;
                    UltimateFx.Place(_smoke[i], new Vector3(Mathf.Sin(round) * 2.6f, .15f, Mathf.Cos(round) * 2.6f + .4f), Quaternion.Euler(0, i * 120 + 60, 0),
                                     new Vector3(grown * .8f, grown, grown * (.7f + .3f * life)));
                    UltimateFx.Set(_smoke[i], life > 0 ? _age - HoldSeconds - i * .12f : -1f, Mathf.Lerp(.2f, 1f, Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.2f, 1f, life))));
                }
            }
            for (int i = 0; i < _dust.Length; i++)
            {
                float life = (_age - HoldSeconds - i * .07f) / (SinkSeconds * .9f), angle = (i * 60 + 20) * Mathf.Deg2Rad;
                float size = life <= 0 || life >= 1 ? 0 : (life < .25f ? life / .25f : 1 - (life - .25f) / .75f) * (.7f + .5f * Mathf.Clamp01(life));
                bool on = size > .04f;
                if (_dust[i].gameObject.activeSelf != on) _dust[i].gameObject.SetActive(on);
                float far = 3.1f + 1.2f * Mathf.Clamp01(life);
                _dust[i].localPosition = new Vector3(Mathf.Sin(angle) * far, .4f + Mathf.Clamp01(life) * 1.1f, Mathf.Cos(angle) * far + .4f);
                _dust[i].localScale = Vector3.one * size;
            }
        }
    }
}
