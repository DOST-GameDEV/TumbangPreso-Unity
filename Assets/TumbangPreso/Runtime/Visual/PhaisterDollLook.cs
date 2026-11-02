using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// WHAT TELLS THE PLAYER WHAT HER VOODOO DOLL IS, in direction A of her kit (`PhaisterLook`). The model and its light are
    /// the owner's reviewed work (`PhaisterDollArt`) and the control and its wires are his too; this is the effects round
    /// them. Each one is a rule of the doll's (`VoodooDollBody`):
    ///
    /// | Rule | What says so |
    /// |---|---|
    /// | it wakes beside her as its own fighter | THE WAKE: moths burst out of it, the approved cloud rolls off its feet, a seal lights under it and is eaten |
    /// | it is hers for the whole round | THE RETINUE: a few of her moths circle its crown as long as it lives |
    /// | its points are hers | THE SOUL PAID: on a score, moths leave it and fly to HER, wherever she is |
    /// | tagging it pays nobody, stuns it 5 s | THE STUN: its moths drop dead and go grey, a puff of grey cloud, a thin grey smoke for the stun, and the moths rise again one by one, the last as it gets up: the stun's clock |
    ///
    /// The tagged doll had a grey stitched X over its head. The owner rejected moons and Xs as clip art, so the stun is now what
    /// happens to the doll and not a symbol over it, and the wires going slack (`VoodooSkyCircle`) already say the same.
    ///
    /// All static meshes moved in the vertex program, hidden when idle. A pure function of the ages handed in.
    /// </summary>
    public sealed class PhaisterDollLook
    {
        public const float WakeSeconds = 1.5f, SoulSeconds = 0.95f;
        private const float SealSize = 1.55f;
        private const int RetinueCount = 6;
        private static Mesh _retinueMesh, _wakeMesh, _soulMesh;

        private readonly GameObject _root, _retinueGo, _wakeGo, _soulGo, _sealGo, _sealBackGo;
        private readonly Material _retinue, _wake, _soul, _seal, _sealBack;
        private readonly FxCloud _wakeCloud, _tagCloud, _smoulder;
        private readonly Transform _wakeCloudAt, _tagCloudAt, _smoulderAt;
        private readonly GameObject _payGo;
        private readonly FxPiece _payGlow;
        private readonly System.Collections.Generic.List<Object> _ownedGlow = new System.Collections.Generic.List<Object>();

        /// <summary>A row of moths from a fixed seed. Every row is fixed at build, so all peers draw the same swarm.</summary>
        internal static Mesh MothMesh(string name, int count, float small, float large, int salt)
        {
            var rows = new PhaisterLook.MothRow[count];
            for (int i = 0; i < count; i++)
                rows[i] = new PhaisterLook.MothRow
                {
                    Delay = FxStudyKit.Hash(i, salt) * 1.25f,
                    LaneSide = (FxStudyKit.Hash(i, salt + 1) - 0.5f) * 0.9f,
                    LaneUp = 0f,
                    Phase = i / (float)count * 360f + FxStudyKit.Hash(i, salt + 2) * 40f,
                    LeaveHeight = FxStudyKit.Hash(i, salt + 3),
                    KnitHeight = FxStudyKit.Hash(i, salt + 4),
                    LeaveSide = FxStudyKit.Hash(i, salt + 5),
                    Size = Mathf.Lerp(small, large, FxStudyKit.Hash(i, salt + 6)),
                    WingHz = Mathf.Lerp(3.8f, 5.4f, FxStudyKit.Hash(i, salt + 7)),
                };
            return PhaisterLook.Moths(name, rows);
        }

        public PhaisterDollLook(Transform parent)
        {
            _root = new GameObject("PhaisterDollLook") { hideFlags = HideFlags.DontSave };
            if (parent != null) _root.transform.SetParent(parent, false);
            if (_retinueMesh == null) _retinueMesh = MothMesh("PhaisterDollRetinue", RetinueCount, 0.62f, 0.80f, 411);
            if (_wakeMesh == null) _wakeMesh = MothMesh("PhaisterDollWake", 12, 0.75f, 1.05f, 521);
            if (_soulMesh == null) _soulMesh = MothMesh("PhaisterDollSoul", 6, 0.65f, 0.85f, 631);

            _retinue = Moths("Retinue", _retinueMesh, 1f, out _retinueGo);
            _wake = Moths("Wake", _wakeMesh, 3f, out _wakeGo);
            _soul = Moths("SoulPaid", _soulMesh, 2f, out _soulGo);
            if (_wake != null) _wake.SetFloat("_Life", 1.15f);
            if (_soul != null) _soul.SetFloat("_Life", SoulSeconds - 0.25f);

            var mask = FxStudyKit.Mask("phaister_seal");
            _sealBackGo = PhaisterLook.Draw("WakeSealDark", _root.transform, PhaisterLook.Quad);
            _sealBack = PhaisterLook.Paint(_sealBackGo, "PhaisterSeal");
            if (_sealBack != null) { _sealBack.SetFloat("_Mode", 1f); _sealBack.SetFloat("_Open", 0f); _sealBack.renderQueue = 3018; }
            _sealGo = PhaisterLook.Draw("WakeSeal", _root.transform, PhaisterLook.Quad);
            _seal = PhaisterLook.Paint(_sealGo, "PhaisterSeal");
            if (_seal != null) { _seal.SetTexture("_SealTex", mask); _seal.SetFloat("_Mode", 0f); _seal.SetVector("_Chan", new Vector4(1, 1, 1, 0)); _seal.renderQueue = 3019; }
            // While it is stunned a thin grey smoke rises off it, the whole stun: from any distance it is out of the fight.
            _smoulderAt = Holder("StunSmoke");
            _smoulder = FxCloud.Build(_smoulderAt, "StunSmokeBalls", 53, 30, new Color(0.62f, 0.58f, 0.66f), new Color(0.34f, 0.31f, 0.39f), new Color(0.16f, 0.13f, 0.20f), 0.012f, 0.05f, 0.18f);
            _payGlow = PhaisterLook.Glow(Holder("PaidGlowAt"), "PaidGlow", "glow_soft", new Color(1f, 0.72f, 0.95f), new Color(0.80f, 0.14f, 0.82f), 34, false, _ownedGlow);
            _payGo = _root.transform.Find("PaidGlowAt").gameObject;
            _wakeCloudAt = Holder("WakeCloud"); _tagCloudAt = Holder("TagCloud");
            _wakeCloud = FxCloud.Build(_wakeCloudAt, "WakeCloudBalls", 51, 220, new Color(0.74f, 0.55f, 0.86f), new Color(0.44f, 0.25f, 0.60f), new Color(0.22f, 0.09f, 0.32f), 0.012f, 0.07f, 0.30f);
            _tagCloud = FxCloud.Build(_tagCloudAt, "TagCloudBalls", 52, 170, new Color(0.62f, 0.58f, 0.66f), new Color(0.34f, 0.31f, 0.39f), new Color(0.16f, 0.13f, 0.20f), 0.012f, 0.06f, 0.27f);
        }

        private Transform Holder(string name)
        {
            var t = new GameObject(name) { hideFlags = HideFlags.DontSave }.transform;
            t.SetParent(_root.transform, false);
            return t;
        }

        private Material Moths(string name, Mesh mesh, float mode, out GameObject go)
        {
            go = PhaisterLook.Draw(name, _root.transform, mesh);
            var m = PhaisterLook.Paint(go, "PhaisterDollMoths");
            if (m != null) m.SetFloat("_Mode", mode);
            return m;
        }

        private static void On(GameObject go, bool on) { if (go.activeSelf != on) go.SetActive(on); }

        /// <param name="clock">Seconds since it woke, on any clock that does not stop.</param>
        /// <param name="feet">The doll's feet.</param>
        /// <param name="crown">Over its head, where its retinue circles.</param>
        /// <param name="tagAge">Seconds since it was tagged, negative when it has not been; `stun` is how long that stun lasts.</param>
        /// <param name="scoreAge">Seconds since it last scored, negative for never.</param>
        /// <param name="her">Where she stands: the payment flies there.</param>
        public void Pose(float clock, Vector3 feet, Vector3 crown, float tagAge, float stun, float scoreAge, Vector3 her, Vector3? eye = null)
        {
            // THE WAKE.
            bool wake = clock < WakeSeconds + 0.3f;
            On(_wakeGo, wake); On(_sealGo, wake); On(_sealBackGo, wake); On(_wakeCloudAt.gameObject, wake);
            if (wake)
            {
                if (_wake != null)
                {
                    _wake.SetFloat("_Age", clock);
                    _wake.SetVector("_A", new Vector4(feet.x, feet.y + 0.6f, feet.z, 1.9f));
                    _wake.SetFloat("_Scale", 1f);
                }
                float bloom = FxStudyKit.EaseOut(FxStudyKit.Run(clock, 0f, 0.28f));
                float eat = FxStudyKit.Ease(FxStudyKit.Run(clock, 0.55f, WakeSeconds));
                foreach (var m in new[] { _seal, _sealBack })
                {
                    if (m == null) continue;
                    m.SetVector("_Centre", feet + Vector3.up * 0.035f);
                    m.SetFloat("_Size", SealSize * Mathf.Lerp(0.55f, 1f, bloom) * Mathf.Lerp(1f, 1.14f, eat));
                    m.SetFloat("_Age", clock);
                    m.SetFloat("_Spin", clock * 0.7f);
                    m.SetFloat("_Erode", eat);
                    m.SetFloat("_Bright", 1f + 1.2f * Mathf.Exp(-clock * 5f));
                    // The dark under the seal is only there while the seal is, and goes first.
                    m.SetFloat("_Alpha", m == _sealBack ? bloom * 0.7f * (1f - FxStudyKit.Run(eat, 0f, 0.55f)) : bloom * (1f - FxStudyKit.Ease(FxStudyKit.Run(eat, 0.35f, 0.8f))));
                }
                _wakeCloudAt.position = feet;
                var e = FxCloud.Emitter.Puff(Vector3.up * 0.05f, Vector3.up * 0.7f, 0f, 0.5f, 1.05f);
                e.Spread = 0.8f; e.Widen = 0.5f; e.Small = 0.07f; e.Large = 0.30f; e.Gone = 0.04f;
                _wakeCloud.Pose(clock, e);
            }

            // THE RETINUE, and THE STUN that kills it.
            bool tagged = tagAge >= 0f && tagAge < stun + 0.8f;
            if (_retinue != null)
            {
                _retinue.SetFloat("_Age", clock);
                // Level with the control (it hangs `MarionetteControl.AboveCrown` over the crown), past its bar's 0.93 m half width plus a wing.
                _retinue.SetVector("_A", new Vector4(crown.x, crown.y + 0.75f, crown.z, 1.3f));
                _retinue.SetVector("_B", new Vector4(feet.x, feet.y, feet.z, 0f));
                _retinue.SetFloat("_Since", tagged ? tagAge : -1f);
                _retinue.SetFloat("_Stun", stun);
                _retinue.SetFloat("_Count", RetinueCount);
                _retinue.SetFloat("_Scale", FxStudyKit.Ease(FxStudyKit.Run(clock, 0.5f, 1.2f)));
            }
            On(_tagCloudAt.gameObject, tagged && tagAge < 1.5f);
            On(_smoulderAt.gameObject, tagged);
            if (tagged)
            {
                _smoulderAt.position = feet;
                var plume = FxCloud.Emitter.Column(Vector3.up * 1.2f, Vector3.up * 1.5f);
                plume.Spread = 0.2f; plume.Life = 1.4f; plume.Widen = 0.22f; plume.Weave = 0.16f; plume.Small = 0.05f; plume.Large = 0.18f; plume.Gone = 0.04f;
                plume.From = 0.2f; plume.To = stun - 0.3f;
                _smoulder.Pose(tagAge, plume);
                if (tagAge < 1.5f)
                {
                    _tagCloudAt.position = feet;
                    var e = FxCloud.Emitter.Puff(Vector3.up * 0.75f, Vector3.up * 0.8f, 0f, 0.3f, 0.85f);
                    e.Spread = 0.5f; e.Widen = 0.42f; e.Small = 0.06f; e.Large = 0.27f; e.Gone = 0.03f;
                    _tagCloud.Pose(tagAge, e);
                }
            }

            // THE SOUL PAID: from the doll's chest to hers.
            bool paying = scoreAge >= 0f && scoreAge < SoulSeconds + 0.5f;
            On(_soulGo, paying);
            if (paying && _soul != null)
            {
                _soul.SetFloat("_Age", scoreAge);
                _soul.SetFloat("_Scale", 1f);
                _soul.SetVector("_A", (Vector4)(feet + Vector3.up * 0.9f));
                _soul.SetVector("_B", (Vector4)(her + Vector3.up * 1.0f));
            }
            // Where they land: she flares the moment the first arrives, and that is what says the point went to HER.
            float flare = paying ? FxStudyKit.Run(scoreAge, SoulSeconds - 0.42f, SoulSeconds - 0.28f) * (1f - FxStudyKit.Ease(FxStudyKit.Run(scoreAge, SoulSeconds - 0.25f, SoulSeconds + 0.35f))) : 0f;
            On(_payGo, flare > 0.01f);
            if (flare > 0.01f)
            {
                _payGo.transform.position = her + Vector3.up * 1.0f;
                _payGo.transform.localScale = Vector3.one * (1.7f + 1.3f * FxStudyKit.EaseOut(FxStudyKit.Run(scoreAge, SoulSeconds - 0.42f, SoulSeconds + 0.2f)));
                _payGlow.Face(PhaisterLook.EyeOr(eye));
                _payGlow.Frame(scoreAge, flare);
            }
        }

        /// <summary>
        /// THE END OF THE ROUND: the doll is taken away the same frame, so what is left is a puff of the approved cloud and its moths
        /// scattering. It starts part way in so the cloud is already there when the body goes. Frees itself.
        /// </summary>
        public static PhaisterDollExit PlayExit(Vector3 feet)
        {
            var go = new GameObject("PhaisterDollExit") { hideFlags = HideFlags.DontSave };
            var exit = go.AddComponent<PhaisterDollExit>();
            exit.Build(feet);
            return exit;
        }

        internal static Mesh ExitMesh => _wakeMesh != null ? _wakeMesh : (_wakeMesh = MothMesh("PhaisterDollWake", 12, 0.75f, 1.05f, 521));

        public void Destroy()
        {
            _wakeCloud?.Dispose();
            _tagCloud?.Dispose();
            _smoulder?.Dispose();
            foreach (var o in _ownedGlow) PhaisterLook.Free(o);
            PhaisterLook.Free(_root);
        }
    }

    /// <summary>The doll's way out at the round's end (`PhaisterDollLook.PlayExit`): a pure function of its age, driven by its own clock in a match and by the caller on a film.</summary>
    public sealed class PhaisterDollExit : MonoBehaviour
    {
        public const float Seconds = 1.35f, StartAge = 0.32f;
        private Material _moths;
        private FxCloud _cloud;
        private Vector3 _feet;
        private float _age = StartAge;

        internal void Build(Vector3 feet)
        {
            _feet = feet;
            transform.position = feet;
            var moths = PhaisterLook.Draw("ExitMoths", transform, PhaisterDollLook.ExitMesh);
            _moths = PhaisterLook.Paint(moths, "PhaisterDollMoths");
            if (_moths != null) { _moths.SetFloat("_Mode", 3f); _moths.SetFloat("_Life", 1.1f); _moths.SetVector("_A", new Vector4(feet.x, feet.y + 0.8f, feet.z, 2.4f)); _moths.SetFloat("_Scale", 1f); }
            var at = new GameObject("ExitCloud") { hideFlags = HideFlags.DontSave };
            at.transform.SetParent(transform, false);
            _cloud = FxCloud.Build(at.transform, "ExitCloudBalls", 54, 200, new Color(0.74f, 0.55f, 0.86f), new Color(0.44f, 0.25f, 0.60f), new Color(0.22f, 0.09f, 0.32f), 0.012f, 0.08f, 0.40f);
            Step(StartAge);
        }

        public void Step(float age)
        {
            _age = age;
            if (_moths != null) _moths.SetFloat("_Age", age);
            var e = FxCloud.Emitter.Puff(Vector3.up * 0.75f, Vector3.up * 0.9f, 0f, 0.28f, 0.95f);
            e.Spread = 0.6f; e.Widen = 0.6f; e.Small = 0.08f; e.Large = 0.40f; e.Gone = 0.05f;
            _cloud?.Pose(age, e);
        }

        private void Update()
        {
            // Unscaled: the round's end may slow or stop the clock, and this must still finish.
            Step(_age + Time.unscaledDeltaTime);
            if (_age >= Seconds) Destroy(gameObject);
        }

        private void OnDestroy() { _cloud?.Dispose(); }
    }
}
