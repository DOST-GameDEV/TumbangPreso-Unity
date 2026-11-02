using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// VOODOO, HER PASSIVE: *"Whenever Phaister marks someone she takes 10% of their speed and they slow down by 10%"*. The owner
    /// chose the picture, after rejecting a violet shadow dragged along the floor: *"A few moths cling to the victim's heels and
    /// flutter as if weighing them down. Matching moths trail Phaister's feet in a light wake."* Three things, all the moth of her
    /// teleport (`PhaisterPassiveMoths.shader`), for as long as any mark of hers lives (`CharacterMotor.VoodooSpeedScale`):
    ///
    /// | Who | What | Why |
    /// |---|---|---|
    /// | the marked body | three dull moths on the heels, heads pointing AWAY from where the body is going, wings labouring | they are flying against the runner, so it reads as being held back, and it reads standing still because they keep heaving |
    /// | Phaister | a wake: moths shed from her heels one after another that peel off left and right and fall behind her, lit with her colour | they cannot keep up with her. It only appears when she moves |
    /// | between them | two moths fly over from the marked heels when the mark lands, filling with colour on the way | the moment the speed changes hands |
    ///
    /// ⚠️ ON THE BODY, NOT THE FLOOR. The clingers ride the leg bones (`BindLegs`) with a little lag, so a stride lifts them and the
    /// drop swings them. A body with no such bones (a bench capsule) gets a stride worked out from its speed.
    ///
    /// ⚠️ BIG AND FEW. Her teleport's own note: small moths "read as confetti" at 6 m. They stay under knee height, so Hex at the
    /// chest and Drain over the head have the upper body to themselves.
    ///
    /// Cost: one renderer, one material, one static mesh of ten moths (30 cards, 60 triangles, most of them collapsed to nothing
    /// at any moment). Three small arrays set a frame, no allocation.
    /// </summary>
    public sealed class PhaisterPassiveLook
    {
        private const int WakeCount = 4, TransferAt = 5, TransferCount = 2, ClingAt = 7, ClingCount = 3, Count = 10;

        /// <summary>How long one moth of the wake lives (s), and the speed (m/s) under which she sheds none.</summary>
        public const float WakeLife = 0.92f, WakeFromSpeed = 1.2f;

        /// <summary>Which leg (-1 left, 1 right), how far down it (1 the sole), how far behind the heel (m), size, wing beats a second, phase.</summary>
        private static readonly float[,] ClingRows =
        {
            { -1f, 0.88f, 0.09f, 1.08f, 2.7f, 0.0f },
            {  1f, 0.86f, 0.09f, 1.00f, 3.1f, 2.1f },
            {  1f, 0.55f, 0.11f, 0.84f, 3.5f, 4.0f },
        };
        private static readonly float[] WakeSizes = { 0.98f, 0.90f, 1.02f, 0.88f };

        private static readonly int SpotId = Shader.PropertyToID("_Spot"), AimId = Shader.PropertyToID("_Aim"), InfoId = Shader.PropertyToID("_Info");

        private static Mesh _mesh;
        private readonly GameObject _root, _go;
        private readonly Material _material;
        private readonly Vector4[] _spot = new Vector4[Count], _aim = new Vector4[Count], _info = new Vector4[Count];

        private readonly Vector3[] _cling = new Vector3[ClingCount];
        private readonly float[] _clingBeat = new float[ClingCount];
        private bool _clingPlaced;
        private readonly Vector3[] _wakeBorn = new Vector3[WakeCount], _wakeWay = new Vector3[WakeCount];
        private readonly float[] _wakeAge = new float[WakeCount], _wakeSpeed = new float[WakeCount], _wakeBeat = new float[WakeCount], _wakeSide = new float[WakeCount];
        private readonly bool[] _wakeAlive = new bool[WakeCount];
        private float _nextSide = 1f;
        private float _dragShow, _hasteShow, _stride, _clock;

        private Transform _legL, _legR;
        private Vector3 _axisL = Vector3.down, _axisR = Vector3.down;
        private float _reach;

        public PhaisterPassiveLook(Transform parent)
        {
            _root = new GameObject("PhaisterPassiveLook");
            _root.transform.SetParent(parent, false);
            if (_mesh == null) _mesh = Cards();
            _go = PhaisterLook.Draw("Moths", _root.transform, _mesh);
            _material = PhaisterLook.Paint(_go, "PhaisterPassiveMoths");
            for (int i = 0; i < WakeCount; i++) _wakeAge[i] = WakeLife * i / WakeCount;
            for (int i = 0; i < ClingCount; i++) _clingBeat[i] = ClingRows[i, 5];
            Hide();
        }

        private static Mesh Cards()
        {
            var v = new List<Vector3>(); var uv0 = new List<Vector2>(); var uv1 = new List<Vector2>(); var t = new List<int>();
            for (int r = 0; r < Count; r++)
                foreach (float part in new[] { -1f, 1f, 0f })
                {
                    int at = v.Count;
                    for (int c = 0; c < 4; c++)
                    {
                        v.Add(Vector3.zero);
                        uv0.Add(new Vector2(c == 1 || c == 2 ? 1f : 0f, c >= 2 ? 1f : 0f));
                        uv1.Add(new Vector2(part, r));
                    }
                    t.Add(at); t.Add(at + 2); t.Add(at + 1); t.Add(at); t.Add(at + 3); t.Add(at + 2);
                }
            var mesh = new Mesh { name = "PhaisterPassiveMoths", hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(v); mesh.SetUVs(0, uv0); mesh.SetUVs(1, uv1); mesh.SetTriangles(t, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2000f);
            return mesh;
        }

        /// <summary>
        /// Finds this body's leg bones under its drawn model: the cast's rig names them `leg-left` and `leg-right`, each one stiff
        /// bone from hip to sole. Called when a mark starts, because it walks the skins once. Null or a rig without them falls
        /// back to a stride worked out from speed.
        /// </summary>
        public void BindLegs(Transform model)
        {
            _legL = _legR = null; _reach = 0f;
            if (model == null) return;
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                var mesh = skin.sharedMesh;
                if (mesh == null) continue;
                var bones = skin.bones; var binds = mesh.bindposes;
                for (int i = 0; i < bones.Length && i < binds.Length; i++)
                {
                    if (bones[i] == null) continue;
                    bool left = bones[i].name == "leg-left";
                    if (!left && bones[i].name != "leg-right") continue;
                    // The same measure `SidewalkLife` takes: the skin that carries the leg reaches furthest, the others read negative.
                    float local = binds[i].inverse.MultiplyPoint3x4(Vector3.zero).y - mesh.bounds.min.y;
                    _reach = Mathf.Max(_reach, local * skin.transform.TransformVector(Vector3.up).magnitude);
                    var down = binds[i].MultiplyVector(Vector3.down);
                    down = down.sqrMagnitude > 1e-8f ? down.normalized : Vector3.down;
                    if (left) { if (_legL == null) { _legL = bones[i]; _axisL = down; } }
                    else if (_legR == null) { _legR = bones[i]; _axisR = down; }
                }
            }
            if (_reach < 0.15f || _reach > 1.5f) _legL = _legR = null;
        }

        /// <summary>A point on one leg, `along` of the way from hip to sole.</summary>
        private Vector3 OnLeg(float side, float along, Vector3 feet, Vector3 way, float run)
        {
            var bone = side < 0f ? _legL : _legR;
            if (bone != null) return bone.position + bone.TransformDirection(side < 0f ? _axisL : _axisR) * (_reach * along);
            float s = _stride + (side < 0f ? 0f : Mathf.PI);
            Vector3 across = Vector3.Cross(Vector3.up, way);
            return feet + across * (side * 0.14f) + way * (Mathf.Sin(s) * 0.34f * run)
                 + Vector3.up * ((1f - along) * 0.75f + 0.05f + Mathf.Max(0f, Mathf.Cos(s)) * 0.30f * run);
        }

        private static Vector3 Flat(Vector3 v, Vector3 otherwise)
        {
            v.y = 0f;
            if (v.sqrMagnitude > 0.04f) return v.normalized;
            otherwise.y = 0f;
            return otherwise.sqrMagnitude > 0.001f ? otherwise.normalized : Vector3.forward;
        }

        /// <summary>
        /// The marked body's end. <paramref name="ownEyes"/>: this screen is looking out of this body, so the moths stay smaller and
        /// further behind the heels, in case the player looks straight down, and stay out of the aim.
        /// </summary>
        public void PoseVictim(float age, Vector3 feet, Vector3 facing, Vector3 moving, bool show, bool ownEyes, float dt)
        {
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            _dragShow = Mathf.MoveTowards(_dragShow, show ? 1f : 0f, dt / (show ? 0.40f : 0.50f));
            if (_dragShow <= 0.001f)
            {
                for (int i = 0; i < ClingCount; i++) _spot[ClingAt + i].w = 0f;
                _clingPlaced = false;
                Push();
                return;
            }
            Wake();
            _clock += dt;
            float speed = new Vector2(moving.x, moving.z).magnitude;
            float run = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 3.4f, speed));
            _stride += dt * speed * 2.1f;
            Vector3 way = Flat(moving, facing), back = -way, across = Vector3.Cross(Vector3.up, way);
            float present = FxStudyKit.Ease(_dragShow);
            for (int i = 0; i < ClingCount; i++)
            {
                float side = ClingRows[i, 0], phase = ClingRows[i, 5];
                // Heaves, then rests: a moth holding on, not an idling machine. Running it never gets to rest.
                float tire = Mathf.Lerp(0.55f + 0.45f * Mathf.Sin(_clock * 1.7f + phase), 1f, run);
                float hz = ClingRows[i, 4] * Mathf.Lerp(0.85f, 1.25f, run);
                _clingBeat[i] += dt * hz * Mathf.PI * 2f;
                float beat = Mathf.Sin(_clingBeat[i]);
                // A tug, about once a second: the moth lurches backward and the row gives a little.
                float tug = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(_clock * 6.6f + phase * 1.7f), 4f) * (0.05f + 0.13f * run);
                float behind = ClingRows[i, 2] + tug + (ownEyes ? 0.28f : 0f);
                Vector3 leg = OnLeg(side, ClingRows[i, 1], feet, way, run);
                Vector3 target = leg + back * behind + across * (side * 0.05f) + Vector3.up * (0.035f * Mathf.Cos(_clingBeat[i]));
                // They arrive fluttering down from the shin and let go upward, so neither end is a pop.
                target += Vector3.up * ((1f - present) * 0.75f);
                if (ownEyes) target.y = Mathf.Min(target.y, feet.y + 0.30f);
                target.y = Mathf.Max(target.y, feet.y + 0.10f);
                if (!_clingPlaced || (_cling[i] - target).sqrMagnitude > 6f) _cling[i] = target;
                else _cling[i] = Vector3.Lerp(_cling[i], target, 1f - Mathf.Exp(-dt * 30f));

                // Heads away from the running way and pitched up: flying against the body they are stuck to. Standing, head up on the leg with the wings spread, the way a moth holds on, which also shows the wing to a low camera.
                Vector3 aim = Vector3.Lerp(Vector3.up + back * 0.4f + across * (side * 0.3f), back + Vector3.up * 0.8f, run);
                float angle = Mathf.Lerp(-8f, 58f, 0.5f + 0.5f * beat) * Mathf.Lerp(0.5f, 1f, tire);
                _spot[ClingAt + i] = new Vector4(_cling[i].x, _cling[i].y, _cling[i].z, ClingRows[i, 3] * present * (ownEyes ? 0.7f : 1f));
                _aim[ClingAt + i] = new Vector4(aim.x, aim.y, aim.z, angle * Mathf.Deg2Rad);
                _info[ClingAt + i] = new Vector4(1f - Mathf.Clamp01(_dragShow * 1.6f), 0f, side * (0.25f + 0.16f * beat), 0f);
            }
            _clingPlaced = true;
            Push();
        }

        /// <summary>
        /// Her end. <paramref name="age"/> is the age of her newest mark and <paramref name="from"/> where that body stands (the two
        /// moths of the transfer start there), <paramref name="moving"/> her velocity.
        /// </summary>
        public void PoseHaste(float age, Vector3 feet, Vector3 from, Vector3 moving, Vector3 facing, bool show, float dt)
        {
            dt = Mathf.Clamp(dt, 0f, 0.1f);
            _hasteShow = Mathf.MoveTowards(_hasteShow, show ? 1f : 0f, dt / (show ? 0.15f : 0.40f));
            float speed = new Vector2(moving.x, moving.z).magnitude;
            Vector3 way = Flat(moving, facing), across = Vector3.Cross(Vector3.up, way);
            bool anything = _hasteShow > 0.001f;
            if (!anything) for (int i = 0; i < WakeCount; i++) anything |= _wakeAlive[i];
            if (!anything)
            {
                for (int i = 0; i < ClingAt; i++) _spot[i].w = 0f;
                Push();
                return;
            }
            Wake();
            _clock += dt;
            float run = Mathf.InverseLerp(0.6f, 3.4f, speed);
            _stride += dt * speed * 2.1f;

            // The wake: each moth is born at a heel with her way and her speed, then loses to her. That she leaves them behind IS the read.
            for (int i = 0; i < WakeCount; i++)
            {
                _wakeAge[i] += dt;
                if (_wakeAge[i] >= WakeLife)
                {
                    _wakeAge[i] %= WakeLife;
                    _wakeAlive[i] = show && speed >= WakeFromSpeed;
                    if (_wakeAlive[i])
                    {
                        _nextSide = -_nextSide;
                        _wakeSide[i] = _nextSide;
                        _wakeBorn[i] = OnLeg(_nextSide, 0.86f, feet, way, run);
                        _wakeWay[i] = way;
                        _wakeSpeed[i] = speed;
                    }
                }
                if (!_wakeAlive[i]) { _spot[i].w = 0f; continue; }
                _wakeBeat[i] += dt * 6.2f * Mathf.PI * 2f;
                float u = _wakeAge[i] / WakeLife;
                Vector3 out_ = Vector3.Cross(Vector3.up, _wakeWay[i]) * _wakeSide[i];
                float along = _wakeSpeed[i] * WakeLife * (0.90f * u - 0.20f * u * u);
                float peel = 0.52f * FxStudyKit.EaseOut(u);
                float rise = 0.20f + 0.38f * u + 0.04f * Mathf.Sin(_wakeBeat[i] * 0.5f);
                Vector3 at = _wakeBorn[i] + _wakeWay[i] * along + out_ * peel + Vector3.up * rise;
                // Pitched up, so the open wings face a camera behind or beside her instead of going edge-on.
                Vector3 aim = _wakeWay[i] + out_ * 0.35f + Vector3.up * 0.8f;
                float size = WakeSizes[i] * FxStudyKit.EaseOut(Mathf.Clamp01(u / 0.14f)) * (1f - 0.25f * u);
                float angle = Mathf.Lerp(26f, 66f, 0.5f + 0.5f * Mathf.Sin(_wakeBeat[i]));
                _spot[i] = new Vector4(at.x, at.y, at.z, size);
                _aim[i] = new Vector4(aim.x, aim.y, aim.z, angle * Mathf.Deg2Rad);
                _info[i] = new Vector4(Mathf.Clamp01((u - 0.72f) / 0.28f), Mathf.Lerp(0.90f, 0.62f, u), _wakeSide[i] * 0.22f, 0.30f);
            }

            // The transfer: two moths off the marked heels, across and into hers, dull when they leave and full when they land.
            Vector3 heel = feet + Vector3.up * 0.32f - way * 0.15f, start = from + Vector3.up * 0.32f;
            float gap = Vector3.Distance(from, feet), flight = 0.62f + 0.035f * gap;
            for (int i = 0; i < TransferCount; i++)
            {
                float k = (age - i * 0.14f) / flight;
                if (!show || k <= 0f || k >= 1.25f) { _spot[TransferAt + i].w = 0f; continue; }
                float e = FxStudyKit.Ease(Mathf.Clamp01(k));
                Vector3 mid = Vector3.Lerp(start, heel, 0.5f) + Vector3.up * (0.75f + 0.06f * gap) + across * (i == 0 ? 0.55f : -0.55f);
                Vector3 at = Bez(start, mid, heel, e);
                Vector3 aim = k < 1f ? Bez(start, mid, heel, Mathf.Clamp01(e + 0.03f)) - at : way;
                float size = 1.0f * FxStudyKit.EaseOut(Mathf.Clamp01(k / 0.12f)) * (1f - FxStudyKit.Ease(Mathf.Clamp01((k - 1f) / 0.25f)));
                float angle = Mathf.Lerp(20f, 68f, 0.5f + 0.5f * Mathf.Sin(age * 7f * Mathf.PI * 2f + i * 2.4f));
                _spot[TransferAt + i] = new Vector4(at.x, at.y, at.z, size);
                _aim[TransferAt + i] = new Vector4(aim.x, aim.y, aim.z, angle * Mathf.Deg2Rad);
                _info[TransferAt + i] = new Vector4(0f, Mathf.Lerp(0f, 0.85f, e), (i == 0 ? 1f : -1f) * 0.5f * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI), 0.15f);
            }
            Push();
        }

        private static Vector3 Bez(Vector3 a, Vector3 m, Vector3 b, float e) => (1f - e) * (1f - e) * a + 2f * (1f - e) * e * m + e * e * b;

        private void Wake()
        {
            if (!_root.activeSelf) _root.SetActive(true);
        }

        private void Push()
        {
            if (_material == null) return;
            _material.SetVectorArray(SpotId, _spot);
            _material.SetVectorArray(AimId, _aim);
            _material.SetVectorArray(InfoId, _info);
        }

        public void Hide()
        {
            _dragShow = _hasteShow = 0f;
            _clingPlaced = false;
            for (int i = 0; i < Count; i++) _spot[i].w = 0f;
            for (int i = 0; i < WakeCount; i++) _wakeAlive[i] = false;
            Push();
            if (_root != null && _root.activeSelf) _root.SetActive(false);
        }

        public void Free() => PhaisterLook.Free(_root);
    }
}
