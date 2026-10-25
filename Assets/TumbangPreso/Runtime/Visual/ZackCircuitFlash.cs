using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// QUICK CIRCUIT'S LOOK IN A MATCH: the effects study's `FxStudyZackCircuit`, as the owner approved it on the bench
    /// ("zack looks good to go", 2026-10-11), set down where Zack stands and driven by his real body.
    ///
    /// IT IS A LOOK ONLY. The cut, its distance, its timing and who may cast it are `ZackHeroKit.QuickCircuitAbility`'s
    /// and know nothing of this; nothing here is sent anywhere. Every peer already runs that ability's `Activate` at
    /// the press and its `OnActivate` when the 0.15 s tell is over, so every peer draws this from those two calls:
    ///   * `Tell` at the press: the cracks on his body, the gathering light and the leader toward the side he will go;
    ///   * `Cut` when he is thrown: the afterimage where he stood, the strike on the road, and the streak.
    /// ⚠️ THE STREAK'S FRONT IS HIS BODY. It reads where he IS along the cut each frame (his transform, which every peer
    /// has), never lets the front go back, and never draws past him: a cut stopped short by a wall is a short streak.
    /// A tell whose cut never comes (he was Zapped in the 0.15 s) simply ends: no afterimage, no strike, no streak.
    ///
    /// The Overclock second cut is a second cast and gets a second one of these.
    /// </summary>
    public sealed class ZackCircuitFlash : MonoBehaviour
    {
        /// <summary>
        /// ⚠️ THE ONE SWITCH. On, QUICK CIRCUIT is drawn as above. Off, it is as it was before 2026-10-11: no effect in
        /// the world at all, only his body's own motion, his arms and the sound. ON IN THIS WORKTREE at the owner's
        /// word. TO GO BACK, MAKE THE LINE BELOW `false`.
        /// </summary>
        public static bool Streak = true;

        /// <summary>A film's: how much of each piece is light laid on (below 0 is the game's own, `AmihanWhirlwind.DayLight`).</summary>
        public static float StagedLight = -1f;

        private static readonly List<ZackCircuitFlash> Live = new List<ZackCircuitFlash>();

        // ⚠️ A CAST MAKES NOTHING THAT A CAST BEFORE IT MADE. The first version built the whole effect new on every press
        // (some twenty meshes and twenty-five materials) and baked his body twice into two NEW meshes, and threw all of
        // it away 0.9 s later: 4 to 5 ms a press and 1.6 ms a cut, measured in the editor with his body moving. Now:
        //   * an effect that has played is put away whole (its objects switched off under a holder) and the next cast
        //     takes it back, with its meshes and materials as they were: they only ever held the same shapes;
        //   * his body is baked into meshes that are kept for HIM (for the tell and for the afterimage, two of each, taken
        //     by turns, so a second cut whose tell overlaps the first's afterimage never draws into a mesh still on
        //     show) and baked into again on his next cast, with the same lists each time, so nothing is allocated.
        // A pool and a body's meshes belong to the scene they were made in; a film's preview scene keeps its own.
        private const int Spare = 4;
        private sealed class Pool { public Transform Holder; public readonly Stack<FxStudyZackCircuit> Idle = new Stack<FxStudyZackCircuit>(); }
        private static readonly Dictionary<long, Pool> Pools = new Dictionary<long, Pool>();
        private sealed class Kept { public GameObject Model; public readonly Mesh[] Body = new Mesh[2], Ghost = new Mesh[2]; public int Next; }
        private static readonly List<Kept> Bodies = new List<Kept>();
        private bool _reused;

        private FxStudyZackCircuit _fx;
        private CharacterMotor _who;
        private Transform _body;
        private GameObject _model;
        private Mesh _bodyMesh, _ghostMesh;
        private long _poolKey;
        private int _slot;
        private float _light;
        private Vector3 _from, _dir, _eye;
        private float _age, _front, _movedAt, _stoppedAt = -1f;
        // A film's: stood and stepped by hand outside Play, where nothing frees itself and no scene's physics is its own.
        private bool _cut, _staged;

        /// <summary>The press: the tell begins. `ctx` is the cast as the ability was handed it.</summary>
        public static void Tell(AbilityContext ctx)
        {
            if (!Streak || !Application.isPlaying || ctx == null || ctx.Motor == null) return;
            // The same sum `QuickCircuitAbility.OnActivate` makes, so the leader points where he will be thrown.
            Vector3 right = Vector3.Cross(Vector3.up, ctx.Forward).normalized;
            if (right.sqrMagnitude < 0.5f) right = ctx.Motor.transform.right;
            float side = Vector3.Dot(ctx.AimPoint - ctx.Position, right) < 0f ? -1f : 1f;
            Build(ctx.Motor, ctx.Motor.transform, ModelOf(ctx.Motor), right * side, false, false);
        }

        /// <summary>The tell is over and he has been thrown along `dir`.</summary>
        public static void Cut(CharacterMotor who, Vector3 dir)
        {
            if (!Streak || !Application.isPlaying || who == null) return;
            ZackCircuitFlash flash = null;
            for (int i = Live.Count - 1; i >= 0 && flash == null; i--)
                if (Live[i] != null && Live[i]._who == who && !Live[i]._cut) flash = Live[i];
            // (A cut with no tell before it: a peer that joined inside the 0.15 s. It gets the cut alone.)
            if (flash == null) flash = Build(who, who.transform, ModelOf(who), dir, true, false);
            if (flash != null) flash.Launch(dir);
        }

        /// <summary>For a film made outside Play: the tell begun on a body that has no motor, stood where that body is.
        /// It never frees itself; step it with `StepTo`, throw it with `StagedCut`, end it with `Free`.</summary>
        public static ZackCircuitFlash Stage(Transform body, GameObject model, Vector3 dir) => Build(null, body, model, dir, false, true);
        /// <summary>A film's cut: call it when the film's clock reaches the end of the tell.</summary>
        public void StagedCut() => Launch(_dir);
        /// <summary>A film's camera, and whether it is the caster's own eyes; poses it again for that view.</summary>
        public void StagedView(Vector3 eye, bool own) { _eye = eye; _fx.Own = own; Pose(); }

        private static GameObject ModelOf(CharacterMotor who)
        {
            var visual = who != null ? who.GetComponent<CharacterVisual>() : null;
            return visual != null ? visual.Model : null;
        }

        private static ZackCircuitFlash Build(CharacterMotor who, Transform body, GameObject model, Vector3 dir, bool cutOnly, bool staged)
        {
            dir.y = 0f;
            if (body == null || dir.sqrMagnitude < 1e-4f) return null;
            var go = new GameObject("ZackCircuitFlash");
            if (staged) { go.hideFlags = HideFlags.DontSave; UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, body.gameObject.scene); }
            var flash = go.AddComponent<ZackCircuitFlash>();
            flash._who = who; flash._body = body; flash._model = model; flash._staged = staged;
            flash.Place(dir.normalized);
            // Half of it covers, as the approved whirlwind's does in daylight: light alone goes pale on a sunlit road.
            flash._light = StagedLight >= 0f ? StagedLight : AmihanWhirlwind.DayLight;
            flash._poolKey = ((long)go.scene.GetHashCode() << 20) ^ Mathf.RoundToInt(flash._light * 1000f);
            flash._fx = Take(flash._poolKey, go.transform, flash._light, out flash._reused);
            flash.Face();
            flash._fx.Own = who != null && ViewmodelArms.IsFirstPersonFor(who);
            var kept = KeptFor(model);
            if (kept != null) { flash._slot = kept.Next; kept.Next ^= 1; }
            if (!cutOnly && kept != null)
            {
                flash._bodyMesh = Bake(model, go.transform, kept.Body[flash._slot]) ? kept.Body[flash._slot] : null;
                flash._fx.Body(flash._bodyMesh);
            }
            else flash._fx.Body(null);
            flash._fx.Ghost(null);
            // (Tagged once, the first time this effect is made; a reused one already is.)
            if (!flash._reused) foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
            if (!staged) Live.Add(flash);
            flash.Pose();
            return flash;
        }

        // Stands the effect where he is now, its +x down the cut. (x is `dir` when z is `dir` crossed with up.)
        private void Place(Vector3 dir)
        {
            _dir = dir;
            _from = _staged ? _body.position : VfxShapes.GroundPoint(_body.position);
            transform.SetPositionAndRotation(_from, Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up), Vector3.up));
        }

        // The study's z is his front or his back, by which side he is thrown to. His chest's light goes on his FRONT,
        // which is where a target sees him from.
        private void Face() => _fx.Front = Vector3.Dot(transform.forward, _body.forward) >= 0f ? 1f : -1f;

        private void Launch(Vector3 dir)
        {
            dir.y = 0f;
            // He may have crept in the tell, and the cut's side is the ability's to say: stand it again where he leaves from.
            if (dir.sqrMagnitude > 1e-4f) Place(dir.normalized);
            Face();
            _cut = true;
            _age = FxStudyZackCircuit.Tell; _front = 0f; _movedAt = _age; _stoppedAt = -1f;
            // THE AFTERIMAGE IS HIS OWN BODY, as it is posed this frame, left standing where he leaves from.
            var kept = KeptFor(_model);
            _ghostMesh = kept != null && Bake(_model, transform, kept.Ghost[_slot]) ? kept.Ghost[_slot] : null;
            _fx.Ghost(_ghostMesh);
            Pose();
        }

        private void Update() => StepTo(_age + Time.deltaTime);

        /// <summary>Plays it to `seconds` after the press (a film steps it by hand; a match's `Update` does the same).</summary>
        public void StepTo(float seconds)
        {
            _age = Mathf.Max(0f, seconds);
            if (_body == null) { End(); return; }
            // A tell whose cut has not come a quarter of a second late is a cast that was stopped. It ends with the tell.
            if (!_cut && _age > FxStudyZackCircuit.Tell + 0.25f) { End(); return; }
            if (Pose() > 0.50f || _age > 2.5f) End();
        }

        // Its effect is put away BEFORE its object goes: once an object is being destroyed its children cannot be moved out.
        private void End() { if (_staged) return; Free(); Destroy(gameObject); }

        // Poses the study for now and says how long ago he stopped.
        private float Pose()
        {
            // Until he is thrown the clock is held just inside the tell: the cut's pictures wait for the cut itself.
            float t = _cut ? _age : Mathf.Min(_age, FxStudyZackCircuit.Tell - 0.002f), cut = t - FxStudyZackCircuit.Tell;
            if (_cut)
            {
                float now = Vector3.Dot(_body.position - _from, _dir);
                if (now > _front + 0.004f) { _front = now; _movedAt = t; }
                else if (_stoppedAt < 0f && cut > 0.06f && t - _movedAt > 0.07f) _stoppedAt = _movedAt;
            }
            // The rule stops him 0.365 s after he leaves, and that is when the endings play. Only a stop well before
            // that (a wall, a body) moves them: then they play from when he really stopped.
            float stopped = cut - FxStudyZackCircuit.Slide;
            if (_stoppedAt >= 0f && _stoppedAt - FxStudyZackCircuit.Tell < FxStudyZackCircuit.Slide - 0.08f) stopped = Mathf.Max(stopped, t - _stoppedAt);
            Vector3 eye = _eye;
            if (!_staged)
            {
                var lens = Camera.main;
                eye = lens != null ? lens.transform.position : transform.position + new Vector3(0f, 1.6f, -6f);
            }
            _fx.Pose(t, eye, _front / FxStudyZackCircuit.Cut, stopped);
            return stopped;
        }

        // An effect for a new cast: one put away by an earlier cast if there is one, else a new one.
        private static FxStudyZackCircuit Take(long key, Transform under, float light, out bool reused)
        {
            if (Pools.TryGetValue(key, out var pool) && pool.Holder != null && pool.Idle.Count > 0)
            {
                var fx = pool.Idle.Pop();
                if (fx != null && fx.Root != null)
                {
                    fx.Root.SetParent(under, false);
                    fx.Root.gameObject.SetActive(true);
                    reused = true;
                    return fx;
                }
            }
            reused = false;
            return new FxStudyZackCircuit(under, false, light);
        }

        // Puts a played effect away for the next cast, or frees it when enough are already put away.
        private void Give()
        {
            if (_fx == null) return;
            var fx = _fx; _fx = null;
            if (!Pools.TryGetValue(_poolKey, out var pool) || pool.Holder == null)
            {
                var holder = new GameObject("ZackCircuit spare") { hideFlags = HideFlags.DontSave };
                if (gameObject.scene.IsValid()) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(holder, gameObject.scene);
                if (Application.isPlaying && !_staged) DontDestroyOnLoad(holder);
                pool = new Pool { Holder = holder.transform };
                Pools[_poolKey] = pool;
            }
            if (pool.Idle.Count >= Spare || fx.Root == null) { fx.Dispose(); return; }
            fx.Root.gameObject.SetActive(false);
            fx.Root.SetParent(pool.Holder, false);
            pool.Idle.Push(fx);
        }

        // The two meshes kept for this body, made the first time it casts. Bodies that are gone are let go here.
        private static Kept KeptFor(GameObject model)
        {
            if (model == null) return null;
            Kept found = null;
            for (int i = Bodies.Count - 1; i >= 0; i--)
            {
                var kept = Bodies[i];
                if (kept.Model == null) { foreach (var m in kept.Body) Kill(m); foreach (var m in kept.Ghost) Kill(m); Bodies.RemoveAt(i); continue; }
                if (kept.Model == model) found = kept;
            }
            if (found != null) return found;
            found = new Kept { Model = model };
            for (int i = 0; i < 2; i++)
            {
                found.Body[i] = new Mesh { name = "ZackCircuitBody", hideFlags = HideFlags.HideAndDontSave }; found.Body[i].MarkDynamic();
                found.Ghost[i] = new Mesh { name = "ZackCircuitGhost", hideFlags = HideFlags.HideAndDontSave }; found.Ghost[i].MarkDynamic();
            }
            Bodies.Add(found);
            return found;
        }

        // The lists every bake writes through, made once.
        private static readonly List<Vector3> BakeVertices = new List<Vector3>(), BakeNormals = new List<Vector3>(), SkinVertices = new List<Vector3>(), SkinNormals = new List<Vector3>();
        private static readonly List<Vector2> BakeUvs = new List<Vector2>();
        private static readonly List<Color> BakeColours = new List<Color>();
        private static readonly List<int> BakeTriangles = new List<int>(), SkinTriangles = new List<int>();
        private static Mesh _skinned;

        /// <summary>
        /// A body as it is posed this frame, written into `into` in `space`'s own coordinates. ⚠️ IT IS GIVEN ITS OWN
        /// PICTURE COORDINATES: u runs UP him from his feet (0) to his crown (1) and v round him. His model's own
        /// coordinates are a paint atlas, on which the cracks would be confetti and the erosion could not climb him from
        /// the feet; the bench's stand-in is laid out this same way. Each point is shaded by which way it looks, since
        /// the ribbon shader has no light. False if there is no skinned body to bake. Its triangles are written only the
        /// first time: a body's shape of triangles does not change from one pose to the next.
        /// </summary>
        public static bool Bake(GameObject model, Transform space, Mesh into)
        {
            if (model == null || space == null || into == null) return false;
            BakeVertices.Clear(); BakeNormals.Clear(); BakeTriangles.Clear();
            bool fresh = into.vertexCount == 0;
            if (_skinned == null) _skinned = new Mesh { name = "ZackCircuitSkin", hideFlags = HideFlags.HideAndDontSave };
            foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (skin.sharedMesh == null) continue;
                skin.BakeMesh(_skinned, true);
                // Baked for its own transform, scale and all: that transform's whole matrix then puts it in the world.
                // (Placed without the scale, he came out two fifths of his size: the first court films had an afterimage
                // the height of his knees, and nobody could tell, because nobody could see it.)
                Matrix4x4 to = space.worldToLocalMatrix * skin.transform.localToWorldMatrix;
                int first = BakeVertices.Count;
                _skinned.GetVertices(SkinVertices); _skinned.GetNormals(SkinNormals);
                for (int i = 0; i < SkinVertices.Count; i++)
                {
                    BakeVertices.Add(to.MultiplyPoint3x4(SkinVertices[i]));
                    BakeNormals.Add(i < SkinNormals.Count ? to.MultiplyVector(SkinNormals[i]).normalized : Vector3.up);
                }
                if (fresh)
                    for (int sub = 0; sub < _skinned.subMeshCount; sub++)
                    {
                        _skinned.GetTriangles(SkinTriangles, sub);
                        foreach (int index in SkinTriangles) BakeTriangles.Add(first + index);
                    }
            }
            if (BakeVertices.Count == 0) return false;
            // A body that came back with another number of points (another model) starts its mesh again.
            if (!fresh && into.vertexCount != BakeVertices.Count) { into.Clear(); return Bake(model, space, into); }

            float low = float.MaxValue, high = float.MinValue;
            foreach (var p in BakeVertices) { low = Mathf.Min(low, p.y); high = Mathf.Max(high, p.y); }
            float tall = Mathf.Max(0.2f, high - low);
            BakeUvs.Clear(); BakeColours.Clear();
            Vector3 sun = new Vector3(0.25f, 0.85f, -0.45f).normalized;
            for (int i = 0; i < BakeVertices.Count; i++)
            {
                Vector3 p = BakeVertices[i];
                // The stand-in's own layout, at his real height: the same picture lands on him the same size.
                BakeUvs.Add(new Vector2((p.y - low) / tall, 0.5f + 0.5f * (p.x + p.z * 0.6f) / 0.5f));
                float shade = Mathf.Lerp(0.40f, 1.00f, Mathf.Clamp01(0.5f + 0.5f * Mathf.Abs(Vector3.Dot(BakeNormals[i], sun)) + 0.25f * BakeNormals[i].y));
                BakeColours.Add(new Color(shade, shade, shade, 1f));
            }
            if (fresh && BakeVertices.Count > 65000) into.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            into.SetVertices(BakeVertices); into.SetNormals(BakeNormals); into.SetUVs(0, BakeUvs); into.SetColors(BakeColours);
            if (fresh) into.SetTriangles(BakeTriangles, 0);
            into.RecalculateBounds();
            return true;
        }

        private static void Kill(Object thing)
        {
            if (thing == null) return;
            if (Application.isPlaying) Destroy(thing); else DestroyImmediate(thing);
        }

        /// <summary>Frees what it built. A match's `OnDestroy` calls it; a film calls it by hand, since outside Play nothing
        /// calls `OnDestroy`.</summary>
        public void Free()
        {
            Live.Remove(this);
            // The effect is put away for the next cast; the body's meshes are his and are kept for his next cast too.
            if (_fx != null) { _fx.Body(null); _fx.Ghost(null); Give(); }
            _bodyMesh = null; _ghostMesh = null;
        }

        // Destroyed some other way (the scene went): nothing can be moved out now, so it is freed where it is.
        private void OnDestroy()
        {
            Live.Remove(this);
            if (_fx != null) { _fx.Dispose(); _fx = null; }
        }

        /// <summary>Frees every effect put away for a later cast (a film's, whose scene is about to close).</summary>
        public static void ClearSpares()
        {
            foreach (var pool in Pools.Values) while (pool.Idle.Count > 0) pool.Idle.Pop()?.Dispose();
            Pools.Clear();
        }
    }
}
