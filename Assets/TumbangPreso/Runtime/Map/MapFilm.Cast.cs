using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Map
{
    public abstract partial class MapFilm
    {
        // ------------------------------------------------------------------ the players' models
        //
        // ONLY THE DRAWN MODEL MOVES (as in `ArenaIntro`): each seat's `CharacterVisual.ModelRoot` is posed after
        // the animator and put back; the body, its collider and its replicated state stand on the mark throughout.
        // The animator is held in its arrival idle (`SetArrivalPose`) and run on by this film's own time, because
        // the game's clock is held for the whole film.

        private readonly CharacterMotor[] _body = new CharacterMotor[Seats];
        private readonly Transform[] _root = new Transform[Seats];
        private readonly Vector3[] _rest = new Vector3[Seats];
        private readonly Quaternion[] _restTurn = new Quaternion[Seats];
        private readonly CharacterAnimator[] _anim = new CharacterAnimator[Seats];
        private readonly Vector3[] _was = new Vector3[Seats];
        private readonly bool[] _hadWas = new bool[Seats];
        private readonly bool[] _walking = new bool[Seats];
        private readonly Renderer[][] _seatRenderers = new Renderer[Seats][];
        private readonly bool[][] _seatShown = new bool[Seats][];
        private readonly bool[] _seatHidden = new bool[Seats];
        private bool _posing;
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        /// <summary>A scratch buffer for a director's own casts.</summary>
        protected RaycastHit[] Hits => _hits;

        /// <summary>A seat's body (null for an empty seat), and where it stands for the round.</summary>
        protected CharacterMotor Body(int seat) => seat >= 0 && seat < Seats ? _body[seat] : null;
        protected Vector3 Mark(int seat) => _body[seat] != null ? _body[seat].transform.position : Centre;
        protected float MarkYaw(int seat) => _body[seat] != null ? _body[seat].transform.eulerAngles.y : 0.0f;
        protected bool Posable(int seat) => seat >= 0 && seat < Seats && _root[seat] != null;

        private void TakeModels(CharacterMotor[] players)
        {
            for (int s = 0; s < Seats; s++)
            {
                _body[s] = null; _root[s] = null; _anim[s] = null; _hadWas[s] = false; _walking[s] = false;
                _seatRenderers[s] = null; _seatHidden[s] = false;
                var body = s < players.Length ? players[s] : null;
                if (body == null || !body.gameObject.activeInHierarchy) continue;
                _body[s] = body;
                var visual = body.GetComponent<CharacterVisual>();
                var root = visual != null ? visual.ModelRoot : null;
                // A model hung on the seat itself cannot move without the body: it stays on its mark.
                if (root == null || root == body.transform) continue;
                _root[s] = root;
                _rest[s] = root.localPosition; _restTurn[s] = root.localRotation;
                _anim[s] = body.GetComponent<CharacterAnimator>();
                _anim[s]?.SetArrivalPose(s, 0.001f);
                FindActingBones(s, visual.Model);
                _seatRenderers[s] = root.GetComponentsInChildren<Renderer>(true);
                _seatShown[s] = new bool[_seatRenderers[s].Length];
                for (int i = 0; i < _seatRenderers[s].Length; i++) _seatShown[s][i] = _seatRenderers[s][i].enabled;
            }
            _actBase.Clear(); _actWritten.Clear();
            _posing = true;
        }

        private void ReleaseModels()
        {
            if (!_posing) return;
            _posing = false;
            for (int s = 0; s < Seats; s++)
            {
                if (_root[s] != null) { _root[s].localPosition = _rest[s]; _root[s].localRotation = _restTurn[s]; }
                ShowSeat(s, true);
                if (_anim[s] != null) { _anim[s].ClearArrivalGround(); _anim[s].SetArrivalGesture(0); _anim[s].SetArrivalPose(s, 0.0f); }
                _anim[s] = null; _root[s] = null; _body[s] = null;
            }
        }

        /// <summary>The model of `seat` stood at `at` (world), facing world `yaw` degrees, leaning forward `lean` and
        /// rolling `roll`; `lift` is a hop over the ground it stands on.</summary>
        protected void Place(int seat, Vector3 at, float yaw, float lean = 0.0f, float roll = 0.0f)
        {
            if (!Posable(seat)) return;
            var root = _root[seat]; var body = _body[seat];
            var parent = root.parent;
            Quaternion parentTurn = parent != null ? parent.rotation : Quaternion.identity;
            Vector3 offset = at - body.transform.position;
            root.localPosition = _rest[seat] + (parent != null ? parent.InverseTransformVector(offset) : offset);
            root.localRotation = Quaternion.Inverse(parentTurn) * Quaternion.Euler(lean, yaw - body.transform.eulerAngles.y, roll) * parentTurn * _restTurn[seat];
        }

        /// <summary>Back exactly on its body, as the round will draw it.</summary>
        protected void Home(int seat)
        {
            if (!Posable(seat)) return;
            _root[seat].localPosition = _rest[seat]; _root[seat].localRotation = _restTurn[seat];
            _hadWas[seat] = false;
        }

        /// <summary>Run the seat's own idle (or walk, when `walking`) on by this frame. `metres` is how far the model
        /// moved over the ground this frame, so the walk's stride matches its travel.</summary>
        protected void Animate(int seat, bool walking, float rate = 1.0f)
        {
            var anim = _anim[seat];
            if (anim == null) return;
            if (_walking[seat] != walking) { anim.SetArrivalGait(walking); _walking[seat] = walking; }
            float dt = FrameDelta;
            if (walking && _root[seat] != null)
            {
                Vector3 now = _root[seat].position;
                float metres = _hadWas[seat] ? Vector3.ProjectOnPlane(now - _was[seat], Vector3.up).magnitude : 0.0f;
                _was[seat] = now; _hadWas[seat] = true;
                anim.AdvanceHeldTravel(dt * rate, Mathf.Min(metres, 0.5f));
            }
            else { _hadWas[seat] = false; anim.AdvanceHeld(dt * rate); }
        }

        /// <summary>The arrival's greeting on this seat (0 none to 1 full), with one of its gestures (0 to 5).</summary>
        protected void Greet(int seat, float weight, int gesture = 0)
        {
            var anim = _anim[seat];
            if (anim == null) return;
            anim.SetArrivalGesture(gesture);
            anim.SetArrivalPose(seat, Mathf.Max(0.001f, weight));
        }

        /// <summary>Keeps the soles on `floor` (world y) while the model is posed off its mark.</summary>
        protected void Ground(int seat, float floor)
        {
            var anim = _anim[seat];
            if (anim == null || _root[seat] == null) return;
            anim.SetArrivalGround(_root[seat], floor);
            anim.RefreshArrivalGround();
        }

        /// <summary>Hides or shows a seat's whole model (inside a doorway, on a jeepney's bench).</summary>
        protected void ShowSeat(int seat, bool shown)
        {
            var list = _seatRenderers[seat];
            if (list == null || _seatHidden[seat] == !shown) return;
            _seatHidden[seat] = !shown;
            for (int i = 0; i < list.Length; i++) if (list[i] != null) list[i].enabled = shown && _seatShown[seat][i];
        }

        /// <summary>Walk `seat` from `from` to `to` over `p` (0 to 1), facing its travel and easing into its mark's
        /// facing over the last fifth. Animated and grounded.</summary>
        protected void Walk(int seat, Vector3 from, Vector3 to, float p, float finalYaw)
        {
            float eased = Smooth(p);
            Vector3 at = Vector3.Lerp(from, to, eased);
            Vector3 way = to - from; way.y = 0.0f;
            float travel = way.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(way).eulerAngles.y : finalYaw;
            float yaw = Mathf.LerpAngle(travel, finalYaw, Smooth((p - 0.78f) / 0.22f));
            bool walking = p > 0.0f && p < 0.97f;
            at.y = Floor(at, to.y);
            Place(seat, at, yaw, walking ? 4.0f : 0.0f);
            Animate(seat, walking);
            Ground(seat, at.y);
        }

        /// <summary>The walkable floor under `at` within half a metre of `near` (world y), ignoring every body,
        /// slipper and can; `near` when there is none.</summary>
        protected float Floor(Vector3 at, float near)
        {
            int count = Physics.RaycastNonAlloc(new Vector3(at.x, near + 1.25f, at.z), Vector3.down, _hits, 2.5f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity, floor = near;
            for (int i = 0; i < count; i++)
            {
                var hit = _hits[i];
                if (!World(hit.collider) || hit.normal.y < 0.65f) continue;
                float d = Mathf.Abs(hit.point.y - near);
                if (d > 0.5f || d >= best) continue;
                best = d; floor = hit.point.y;
            }
            return floor;
        }

        protected static bool World(Collider collider) => collider != null
            && collider.GetComponentInParent<CharacterMotor>() == null
            && collider.GetComponentInParent<Slipper>() == null && collider.GetComponentInParent<Lata>() == null;

        /// <summary>The farthest point from `mark` toward `from` that a body can walk straight from (no wall
        /// between, floor all the way), so an entrance is shortened rather than walked through a wall.</summary>
        protected Vector3 ClearStart(Vector3 mark, Vector3 from)
        {
            Vector3 best = mark;
            for (int k = 1; k <= 12; k++)
            {
                Vector3 at = Vector3.Lerp(mark, from, k / 12.0f);
                at.y = Floor(at, mark.y);
                Vector3 a = best + Vector3.up * 0.45f, b = best + Vector3.up * 1.6f, dir = at - best;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    int count = Physics.CapsuleCastNonAlloc(a, b, 0.3f, dir.normalized, _hits, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
                    bool blocked = false;
                    for (int i = 0; i < count; i++) if (World(_hits[i].collider)) { blocked = true; break; }
                    if (blocked) break;
                }
                best = at;
            }
            return best;
        }

        // ------------------------------------------------------------------ owned things

        private readonly List<Object> _owned = new List<Object>();
        private readonly List<Renderer> _hidden = new List<Renderer>();
        private readonly List<bool> _hiddenWas = new List<bool>();

        /// <summary>Destroyed by `End`.</summary>
        protected T Own<T>(T thing) where T : Object { if (thing != null) _owned.Add(thing); return thing; }

        private void DestroyOwned()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
                if (_owned[i] != null) Destroy(_owned[i] is Component c ? c.gameObject : _owned[i]);
            _owned.Clear();
        }

        /// <summary>Stops drawing scene renderers for the film; `End` draws them as they were.</summary>
        protected void Hide(IEnumerable<Renderer> renderers)
        {
            foreach (var r in renderers)
            {
                if (r == null || _hidden.Contains(r)) continue;
                _hidden.Add(r); _hiddenWas.Add(r.enabled); r.enabled = false;
            }
        }

        private void RestoreHidden()
        {
            for (int i = 0; i < _hidden.Count; i++) if (_hidden[i] != null) _hidden[i].enabled = _hiddenWas[i];
            _hidden.Clear(); _hiddenWas.Clear();
        }

        /// <summary>
        /// A drawn copy of a scene object and nothing else: every behaviour, collider, body, light, sound and
        /// animator stripped before it ever wakes, so a copy of the LRT consist or a jeepney can be moved through the
        /// film without hitting, sounding or ticking. Owned.
        /// </summary>
        protected GameObject Ghost(GameObject source, string name)
        {
            if (source == null) return null;
            bool was = source.activeSelf;
            source.SetActive(false);
            GameObject copy;
            try { copy = Instantiate(source, transform, true); }
            finally { source.SetActive(was); }
            copy.name = name;
            for (int pass = 0; pass < 3; pass++)
            {
                var parts = copy.GetComponentsInChildren<Component>(true);
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    var part = parts[i];
                    if (part == null || part is Transform || part is MeshFilter || part is MeshRenderer || part is SkinnedMeshRenderer || part is LODGroup) continue;
                    try { DestroyImmediate(part); } catch { }
                }
            }
            copy.SetActive(true);
            foreach (var r in copy.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            return Own(copy);
        }

        // ------------------------------------------------------------------ extras
        //
        // The people the films add (the kid with the chalk, the lola with the can, the kids on the stilt houses) are
        // built as `SidewalkLife` builds its sidewalk: a roster rig at the cast's scale, dressed in its own palette by
        // `ToonSkin`, its Animator off and its clips SAMPLED onto it by this film's time. Nothing of theirs plays.

        protected sealed class Extra
        {
            public GameObject Root, Model;
            public AnimationClip[] Clips;
            public float Scale;

            /// <summary>Stands it at `at` facing world `yaw`, with a hop of `lift`.</summary>
            public void Pose(Vector3 at, float yaw, float lean = 0.0f, float roll = 0.0f)
            {
                if (Root == null) return;
                Root.transform.SetPositionAndRotation(at, Quaternion.Euler(lean, yaw, roll));
            }

            /// <summary>Samples clip `name` (idle, walk, sprint, sit, emote-yes, emote-no, pick-up) at `time` seconds, looping.</summary>
            public void Play(string name, float time)
            {
                if (Model == null || Clips == null) return;
                AnimationClip clip = null;
                foreach (var c in Clips) if (c != null && c.name.EndsWith(name, System.StringComparison.OrdinalIgnoreCase)) { clip = c; break; }
                if (clip == null) foreach (var c in Clips) if (c != null && c.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0) { clip = c; break; }
                if (clip == null) return;
                float length = Mathf.Max(0.05f, clip.length);
                clip.SampleAnimation(Model, clip.isLooping || name == "idle" || name == "walk" || name == "sprint" ? Mathf.Repeat(time, length) : Mathf.Clamp(time, 0.0f, length));
            }

            public void Show(bool shown) { if (Root != null && Root.activeSelf != shown) Root.SetActive(shown); }
        }

        /// <summary>A person for the film from the roster (`ids` in order of preference; any that a seat already plays
        /// is passed over, so no extra wears a player's face). Null when none could be built. Owned.</summary>
        protected Extra Person(string name, float scale, params string[] ids)
        {
            var book = RosterBook.Load();
            if (book == null) return null;
            RosterEntryAsset art = null;
            foreach (var id in ids)
            {
                var candidate = book.FindPersonArt(id);
                if (candidate == null || candidate.Model == null || PlayedBySeat(id)) continue;
                art = candidate; break;
            }
            if (art == null) return null;
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            var model = Instantiate(art.Model, root.transform, false);
            model.name = "Model";
            model.transform.localScale = Vector3.one * CharacterVisual.PersonScale * scale;
            model.transform.localRotation = Quaternion.Euler(0.0f, CharacterVisual.PersonModelYaw, 0.0f);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) Destroy(c);
            ToonSkin.Apply(model, ToonSkin.PersonOutlineWidth, art.Palette != null && art.Palette.Length == 16 ? art.Palette : null);
            var animator = model.GetComponent<Animator>();
            if (animator != null) animator.enabled = false;
            Own(root);
            var extra = new Extra { Root = root, Model = model, Clips = art.Clips, Scale = scale };
            extra.Play("idle", 0.0f);
            return extra;
        }

        private bool PlayedBySeat(string id)
        {
            string spoken = id.Replace('_', ' ');
            for (int s = 0; s < Seats; s++)
                if (_body[s] != null && string.Equals(_body[s].CharacterName(), spoken, System.StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // ------------------------------------------------------------------ simple props

        private static Material _flatSource;

        /// <summary>A plain coloured mesh in the world's toon look (two-band shade and the ink outline). Owned.</summary>
        protected GameObject Prop(PrimitiveType shape, string name, Vector3 at, Quaternion turn, Vector3 size, Color colour, bool outline = true)
        {
            var go = GameObject.CreatePrimitive(shape);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(at, turn);
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            if (_flatSource == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _flatSource = new Material(shader) { name = "Film flat" };
            }
            var m = new Material(_flatSource) { name = "Film " + name };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", colour);
            if (m.HasProperty("_Color")) m.SetColor("_Color", colour);
            r.sharedMaterial = m;
            Own(m);
            // ⚠️ ALWAYS THROUGH THE TOON SKIN: a plain URP/Lit material draws magenta in this project (2026-10-23, film r2).
            ToonSkin.Apply(r, outline ? ToonSkin.PersonOutlineWidth * 0.8f : 0.00001f);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            return Own(go);
        }

        private static Material _glowSource;

        /// <summary>An unlit see-through quad (light, glow, chalk, dust): faces `facing` or, with none, the camera. Owned.</summary>
        protected Renderer Glow(string name, Vector3 at, Vector2 size, Color colour, Vector3? facing = null, Texture texture = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) Destroy(col);
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            go.transform.localScale = new Vector3(size.x, size.y, 1.0f);
            if (facing.HasValue) go.transform.rotation = Quaternion.LookRotation(-facing.Value);
            var r = go.GetComponent<Renderer>();
            if (_glowSource == null) _glowSource = new Material(Shader.Find("Sprites/Default")) { name = "Film glow" };
            var m = new Material(_glowSource) { name = "Film " + name, mainTexture = texture != null ? texture : Soft };
            m.color = colour;
            r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            Own(m);
            Own(go);
            return r;
        }

        /// <summary>Points a glow quad at the camera and sets its colour.</summary>
        protected void Billboard(Renderer glow, Vector3 at, Color colour, float scale = -1.0f)
        {
            if (glow == null || Camera == null) return;
            glow.transform.position = at;
            glow.transform.rotation = Quaternion.LookRotation(at - Camera.transform.position, Camera.transform.up);
            if (scale > 0.0f) glow.transform.localScale = new Vector3(scale, scale, 1.0f);
            glow.sharedMaterial.color = colour;
            glow.enabled = colour.a > 0.003f;
        }

        private static Texture2D _soft, _disc, _ring;

        /// <summary>A round soft-edged dot, for glows and dust.</summary>
        protected static Texture2D Soft => _soft != null ? _soft : (_soft = Radial(64, r => Mathf.Pow(Mathf.Clamp01(1.0f - r), 2.2f)));
        /// <summary>A hard disc with a soft rim, for sprites and pools of light.</summary>
        protected static Texture2D Disc => _disc != null ? _disc : (_disc = Radial(64, r => Mathf.Clamp01((1.0f - r) * 10.0f)));
        /// <summary>A thin ring.</summary>
        protected static Texture2D Ring => _ring != null ? _ring : (_ring = Radial(128, r => Mathf.Clamp01(1.0f - Mathf.Abs(r - 0.86f) * 18.0f)));

        private static Texture2D Radial(int size, System.Func<float, float> alpha)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "Film radial" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2.0f - 1.0f, dy = (y + 0.5f) / size * 2.0f - 1.0f;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(255 * alpha(Mathf.Sqrt(dx * dx + dy * dy))));
                }
            t.SetPixels32(px); t.Apply(false, true);
            return t;
        }

        /// <summary>A film-owned lamp (point or spot). Owned.</summary>
        protected Light Lamp(string name, Vector3 at, Color colour, float range, LightType type = LightType.Point, float angle = 60.0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var light = go.AddComponent<Light>();
            light.type = type; light.color = colour; light.range = range; light.intensity = 0.0f;
            if (type == LightType.Spot) light.spotAngle = angle;
            light.shadows = LightShadows.None;
            Own(go);
            return light;
        }

        /// <summary>The seat's drawn body in the world, from its renderers (`Renderer.bounds`, never a baked mesh: see
        /// `MatchArrivalPresentation.FrameOwn`).</summary>
        protected Bounds Drawn(int seat)
        {
            var body = Body(seat);
            var bounds = new Bounds(Mark(seat) + Vector3.up, new Vector3(0.9f, 1.9f, 0.9f));
            var model = body != null ? body.GetComponent<CharacterVisual>()?.Model : null;
            if (model == null) return bounds;
            bool found = false;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !(r is SkinnedMeshRenderer || r is MeshRenderer)) continue;
                if (!found) { bounds = r.bounds; found = true; } else bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }
    }
}
