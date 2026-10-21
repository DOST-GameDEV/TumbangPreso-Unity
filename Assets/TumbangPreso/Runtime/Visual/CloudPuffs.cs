using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// SMALL PUFFS OF THE CLOUD, MANY AT ONCE: one mesh, one material, nothing made when a puff is asked for
    /// (`FxCloudPuffs.shader` has how).
    ///
    /// The owner passed the cloud (`FxCloud`) and asked for small ones of it where the game drew flat dust sprites: "i
    /// want a smaller version of this to replace the 2d clouds in dante's ability", and "can you also use this to
    /// replace the jump-land particles that are using the same 2d cloud". And of all of it: "remember this needs to be
    /// heavily optimized".
    ///
    /// ⚠️⚠️ A LANDING IS THE COMMONEST THING IN A MATCH. Every character does it, bots too, several a second. The dust it
    /// replaced built an object, a particle system and its modules on every one. So here a puff is FIVE NUMBERS WRITTEN
    /// INTO A ROW: the field has <see cref="Slots"/> rows, a mesh holding every ball of every row (built once and shared
    /// by every field), and one material. <see cref="Spawn"/> fills the next row and hands the rows to the material.
    /// The shader runs each puff's life from the moment in its row. There is no object, mesh, material or list per
    /// puff, and no work per puff per frame: a frame costs one number (the clock) while any puff is live and nothing
    /// when none is.
    ///
    /// ⚠️ THE ROWS ARE A RING, AND THAT IS THE CAP. The twenty-fifth puff takes the row of the oldest. A puff lives about
    /// a third of a second, so twenty-four at once is far more than four players and their bots can ask for; if they
    /// ever did, the oldest dust goes a little early and nothing else happens.
    ///
    /// It is a picture: no collider, no rule, nothing on the wire. Whoever spawns a puff decides when (the landing dust
    /// is spawned from the same relayed sound cue the flat dust was, so every peer sees what it saw before).
    /// </summary>
    public sealed class CloudPuffField
    {
        public const string ShaderName = "TumbangPreso/FxCloudPuffs";
        /// <summary>How many puffs can be live at once, and how many balls a puff is. `SLOTS` in the shader is the first.</summary>
        public const int Slots = 24, Balls = 12;

        private static readonly int NowId = Shader.PropertyToID("_PuffNow"), AtId = Shader.PropertyToID("_PuffAt"),
            ShapeId = Shader.PropertyToID("_PuffShape"), LitId = Shader.PropertyToID("_PuffLit"),
            ShadeId = Shader.PropertyToID("_PuffShade"), LineId = Shader.PropertyToID("_PuffLine");
        // Indices of one row (twelve balls of ten sides by six), and of the whole field.
        private const int RowIndices = Balls * 10 * 6 * 6, AllIndices = Slots * RowIndices;
        private Mesh _mesh;
        // ⚠️ ONLY THE ROWS THAT ARE LIVE ARE DRAWN. Every row is in the one mesh and the shader collapses a row with no puff
        // in it, but a collapsed ball is still its vertices run through the vertex program: with all twenty-four rows
        // drawn whenever any one puff was live, one landing cost the card all 34,560 triangles. So the field keeps which
        // rows are live (the newest `_live` rows of the ring, ending at the one before `_next`) and narrows the one draw
        // to that run of indices. Nothing is uploaded for it: the index buffer stays as it is, only where the draw
        // starts and how far it goes change. (A run that wraps round the end of the ring is drawn whole.)
        private int _oldest, _live;
        private readonly float[] _ends = new float[Slots];

        /// <summary>One puff. Sizes are in metres at a <see cref="Scale"/> of 1; scale is how a puff is made smaller or
        /// larger, and its balls, its reach and its line all go with it.</summary>
        public struct Puff
        {
            /// <summary>Where on the ground it is (world).</summary>
            public Vector3 At;
            public float Scale;
            /// <summary>How long a ball lives (s). The puff is over a little after that (the last balls leave a moment late).</summary>
            public float Life;
            /// <summary>How far a ball is kicked outward along the ground, and how far the puff lifts, by the end (m).</summary>
            public float Reach, Lift;
            /// <summary>Its turn about upright (radians), so two puffs at one place are not the same puff.</summary>
            public float Turn;
            /// <summary>The share of its balls that are drawn (1 all, 0.5 half): a quieter puff.</summary>
            public float Share;
            /// <summary>Light tone, shadow tone, and the line's: a darker tone of the same colour.</summary>
            public Color Lit, Shade, Line;

            /// <summary>
            /// The dust a landing kicks up: a low ring of small balls thrown outward along the ground, gone in about a
            /// third of a second, never higher than a shin. Chalk dust, the colour the flat dust it replaced had.
            /// ⚠️ A STARTING LOOK. The owner has passed the cloud and asked for this; he has not yet seen this.
            /// </summary>
            public static Puff Landing(Vector3 at) => new Puff
            {
                At = at, Scale = 1f, Life = 0.34f, Reach = 0.42f, Lift = 0.10f, Turn = 0f, Share = 1f,
                Lit = new Color(0.93f, 0.87f, 0.73f), Shade = new Color(0.77f, 0.70f, 0.57f), Line = new Color(0.56f, 0.49f, 0.39f),
            };

            /// <summary>The dust left at the feet by a jump: the same, smaller, shorter and kicked less far.</summary>
            public static Puff Jump(Vector3 at)
            {
                var p = Landing(at);
                p.Scale = 0.72f; p.Life = 0.28f; p.Reach = 0.26f; p.Lift = 0.16f;
                return p;
            }
        }

        private readonly GameObject _go;
        private readonly MeshRenderer _renderer;
        private readonly Material _material;
        private readonly Vector4[] _at = new Vector4[Slots], _shape = new Vector4[Slots], _lit = new Vector4[Slots],
                                   _shade = new Vector4[Slots], _line = new Vector4[Slots];
        private int _next, _spawned;
        private float _liveUntil = float.NegativeInfinity;

        public bool Valid => _go != null && _material != null;
        /// <summary>How many triangles the one mesh is (every ball of every row; it is drawn twice, balls and line).</summary>
        public const int Triangles = AllIndices / 3;
        /// <summary>How many of them the field draws now (only its live rows).</summary>
        public int DrawnTriangles { get; private set; }

        public CloudPuffField(Transform parent, string name = "Cloud puffs")
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null) return;
            // Its own mesh, because which of its rows are drawn is its own: a field is made once a match.
            _mesh = Build();
            _material = new Material(shader) { name = name, hideFlags = HideFlags.HideAndDontSave, renderQueue = 2501 };
            _go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            if (parent != null) _go.transform.SetParent(parent, false);
            _go.AddComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = _go.AddComponent<MeshRenderer>();
            _renderer.sharedMaterial = _material;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            // The puffs are placed in the world by the shader, wherever this object is: it is never culled by where it is.
            _renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 4000f);
            _renderer.enabled = false;
            VfxRenderTag.Attach(_go);
            // The material is this field's own and nothing else would free it when the object goes with its parent.
            var owned = _go.AddComponent<CloudPuffFieldMaterial>(); owned.Owned = _material; owned.OwnedMesh = _mesh;
            Clear();
        }

        /// <summary>Every ball of every row, as a unit ball at the origin with its draws and its row in its points.</summary>
        private static Mesh Build()
        {
            // Ten sides by six: at the size of a puff's balls the facets cannot be seen.
            const int around = 10, up = 6;
            int columns = around + 1, rows = up + 1;
            var unit = new Vector3[columns * rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < columns; c++)
                {
                    float tilt = Mathf.Lerp(-90f, 90f, r / (float)up) * Mathf.Deg2Rad, turn = (c % around) / (float)around * Mathf.PI * 2f;
                    unit[r * columns + c] = new Vector3(Mathf.Cos(tilt) * Mathf.Cos(turn), Mathf.Sin(tilt), Mathf.Cos(tilt) * Mathf.Sin(turn));
                }
            var points = new List<Vector3>(); var a = new List<Vector4>(); var b = new List<Vector4>(); var cc = new List<Vector4>();
            var faces = new List<int>();
            for (int slot = 0; slot < Slots; slot++)
                for (int i = 0; i < Balls; i++)
                {
                    // Each row is its own arrangement: twenty-four different puffs before one repeats.
                    int n = i * 13 + slot * 101 + 7;
                    // Round the ring evenly, each a little off its place; a few large, most middling.
                    float bearing = (i + 0.8f * (FxStudyKit.Hash(n, 7) - 0.5f)) / Balls * Mathf.PI * 2f;
                    var life = new Vector4(FxStudyKit.Hash(n, 1), Mathf.Pow(FxStudyKit.Hash(n, 3), 1.5f), FxStudyKit.Hash(n, 14), FxStudyKit.Hash(n, 15));
                    var path = new Vector4(bearing, 0.55f + 0.45f * FxStudyKit.Hash(n, 10), FxStudyKit.Hash(n, 8), FxStudyKit.Hash(n, 11));
                    // The order the balls are dropped in when only a share is drawn is shuffled, so half a puff is still a ring.
                    var which = new Vector4((((i * 7) % Balls) + 0.5f) / Balls, slot, 0f, 0f);
                    int first = points.Count;
                    for (int k = 0; k < unit.Length; k++) { points.Add(unit[k]); a.Add(life); b.Add(path); cc.Add(which); }
                    for (int r = 0; r < up; r++)
                        for (int c = 0; c < around; c++)
                        {
                            int v = first + r * columns + c;
                            faces.Add(v); faces.Add(v + columns); faces.Add(v + 1);
                            faces.Add(v + 1); faces.Add(v + columns); faces.Add(v + columns + 1);
                        }
                }
            var mesh = new Mesh { name = "cloud-puffs", hideFlags = HideFlags.HideAndDontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(points); mesh.SetUVs(0, a); mesh.SetUVs(1, b); mesh.SetUVs(2, cc);
            mesh.SetTriangles(faces, 0, false);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4000f);
            // (Kept readable: the draw's run of indices is set on it as rows come and go.)
            return mesh;
        }

        /// <summary>
        /// Starts a puff at `now` on the field's clock (the same clock <see cref="Tick"/> is handed). Nothing is made: the
        /// next row is written and the rows are handed to the material.
        /// </summary>
        public void Spawn(in Puff puff, float now)
        {
            if (!Valid) return;
            int slot = _next;
            _next = (_next + 1) % Slots;
            _spawned++;
            _at[slot] = new Vector4(puff.At.x, puff.At.y, puff.At.z, now);
            _shape[slot] = new Vector4(puff.Scale, puff.Life, puff.Reach, puff.Lift);
            _lit[slot] = new Vector4(puff.Lit.r, puff.Lit.g, puff.Lit.b, puff.Turn);
            _shade[slot] = new Vector4(puff.Shade.r, puff.Shade.g, puff.Shade.b, puff.Share);
            _line[slot] = new Vector4(puff.Line.r, puff.Line.g, puff.Line.b, 0f);
            Send();
            _ends[slot] = now + puff.Life * 1.2f;
            if (_live < Slots) _live++; else _oldest = (_oldest + 1) % Slots;
            if (_live == 1) _oldest = slot;
            Narrow();
            // The last balls leave 18 percent of a life late.
            _liveUntil = Mathf.Max(_liveUntil, now + puff.Life * 1.2f);
            _material.SetFloat(NowId, now);
            if (!_renderer.enabled) _renderer.enabled = true;
        }

        /// <summary>
        /// Several puffs at once, each at its own moment on the field's clock, handed to the material ONCE. The same rows
        /// `Spawn` writes, in the same order; only the upload is shared. (Added for an effect that writes a whole cast of
        /// puffs in one frame, where a separate upload a puff made a measured 3 ms spike.)
        /// </summary>
        public void SpawnAll(IReadOnlyList<(Puff puff, float now)> puffs)
        {
            if (!Valid || puffs == null || puffs.Count == 0) return;
            float latest = float.NegativeInfinity;
            for (int i = 0; i < puffs.Count; i++)
            {
                var (puff, now) = puffs[i];
                int slot = _next;
                _next = (_next + 1) % Slots;
                _spawned++;
                _at[slot] = new Vector4(puff.At.x, puff.At.y, puff.At.z, now);
                _shape[slot] = new Vector4(puff.Scale, puff.Life, puff.Reach, puff.Lift);
                _lit[slot] = new Vector4(puff.Lit.r, puff.Lit.g, puff.Lit.b, puff.Turn);
                _shade[slot] = new Vector4(puff.Shade.r, puff.Shade.g, puff.Shade.b, puff.Share);
                _line[slot] = new Vector4(puff.Line.r, puff.Line.g, puff.Line.b, 0f);
                _liveUntil = Mathf.Max(_liveUntil, now + puff.Life * 1.2f);
                latest = Mathf.Max(latest, now);
            }
            Send();
            _material.SetFloat(NowId, latest);
            if (!_renderer.enabled) _renderer.enabled = true;
        }

        /// <summary>The clock, once a frame. While a puff is live it is handed to the material; when the last has gone the
        /// field stops being drawn. A caller whose game is paused hands the same number and the puffs hold.</summary>
        public void Tick(float now)
        {
            if (!Valid || !_renderer.enabled) return;
            if (now > _liveUntil) { _renderer.enabled = false; _live = 0; return; }
            bool retired = false;
            while (_live > 0 && now > _ends[_oldest]) { _oldest = (_oldest + 1) % Slots; _live--; retired = true; }
            if (retired) Narrow();
            _material.SetFloat(NowId, now);
        }

        private void Narrow()
        {
            int start = 0, count = AllIndices;
            if (_live > 0 && _oldest + _live <= Slots) { start = _oldest * RowIndices; count = _live * RowIndices; }
            _mesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(start, count),
                UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds | UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices);
            DrawnTriangles = count / 3;
        }

        /// <summary>For a film, which asks for moments in any order: the clock is set and the field is drawn, whatever
        /// the moment.</summary>
        public void Hold(float now)
        {
            if (!Valid) return;
            _material.SetFloat(NowId, now);
            if (!_renderer.enabled) _renderer.enabled = true;
            // A film asks for any moment, so every row is drawn.
            _mesh.SetSubMesh(0, new UnityEngine.Rendering.SubMeshDescriptor(0, AllIndices), UnityEngine.Rendering.MeshUpdateFlags.DontRecalculateBounds | UnityEngine.Rendering.MeshUpdateFlags.DontValidateIndices);
            DrawnTriangles = AllIndices / 3;
        }

        /// <summary>Every puff gone at once (the round ended; the flat dust it replaced went then too).</summary>
        public void Clear()
        {
            if (!Valid) return;
            for (int i = 0; i < Slots; i++) { _at[i] = new Vector4(0f, 0f, 0f, -100000f); _shape[i] = new Vector4(1f, 1f, 0f, 0f); }
            Send();
            _liveUntil = float.NegativeInfinity;
            _renderer.enabled = false;
            _live = 0;
        }

        private void Send()
        {
            _material.SetVectorArray(AtId, _at); _material.SetVectorArray(ShapeId, _shape); _material.SetVectorArray(LitId, _lit);
            _material.SetVectorArray(ShadeId, _shade); _material.SetVectorArray(LineId, _line);
        }

        /// <summary>For a capture that hides what is live while it films something else.</summary>
        public void Show(bool visible) { if (_renderer != null) _renderer.forceRenderingOff = !visible; }
        public int Spawned => _spawned;
        public bool Live => _renderer != null && _renderer.enabled;

        public void Dispose()
        {
            if (_go != null) Kill(_go);
            else Kill(_material);
        }

        private static void Kill(Object thing)
        {
            if (thing == null) return;
            if (Application.isPlaying) Object.Destroy(thing); else Object.DestroyImmediate(thing);
        }
    }

    /// <summary>Frees a field's material and mesh when the field's object is destroyed, by whatever destroyed it.</summary>
    [ExecuteAlways]
    internal sealed class CloudPuffFieldMaterial : MonoBehaviour
    {
        public Material Owned;
        public Mesh OwnedMesh;
        private void OnDestroy()
        {
            if (Owned != null) { if (Application.isPlaying) Destroy(Owned); else DestroyImmediate(Owned); }
            if (OwnedMesh != null) { if (Application.isPlaying) Destroy(OwnedMesh); else DestroyImmediate(OwnedMesh); }
        }
    }

    /// <summary>
    /// THE MATCH'S ONE FIELD OF CLOUD PUFFS, and the call any effect in a match makes to get a puff:
    /// <c>CloudPuffs.Spawn(CloudPuffField.Puff.Landing(groundPoint))</c>, or the same with its fields changed (scale,
    /// tones, reach). It makes itself the first time it is asked, under the court's contact marks where there are any
    /// (so whatever hides those for a cutscene or a replay hides these with them), and keeps the game's own clock: scaled
    /// time, so a pause holds the dust in the air as it held the flat dust this replaced.
    /// </summary>
    public sealed class CloudPuffs : MonoBehaviour
    {
        private static CloudPuffs _current;
        private CloudPuffField _field;
        private float _now;

        /// <summary>How many puffs this match has been asked for (a probe's count; nothing reads it in play).</summary>
        public static int Spawned => _current != null && _current._field != null ? _current._field.Spawned : 0;

        /// <summary>Makes the match's field now, empty, so the first landing does not (the loading screen calls it:
        /// `HeroFxPrewarm`).</summary>
        public static void Prepare()
        {
            if (_current != null) return;
            var go = new GameObject("Cloud puffs owner") { hideFlags = HideFlags.DontSave };
            var marks = WorldContactPresentation.Current;
            if (marks != null) go.transform.SetParent(marks.transform, false);
            _current = go.AddComponent<CloudPuffs>();
            _current._field = new CloudPuffField(go.transform);
        }

        public static void Spawn(in CloudPuffField.Puff puff)
        {
            if (_current == null)
            {
                var go = new GameObject("Cloud puffs owner") { hideFlags = HideFlags.DontSave };
                var marks = WorldContactPresentation.Current;
                if (marks != null) go.transform.SetParent(marks.transform, false);
                _current = go.AddComponent<CloudPuffs>();
                _current._field = new CloudPuffField(go.transform);
            }
            _current._field.Spawn(puff, _current._now);
        }

        private void Update()
        {
            if (_field == null || !_field.Live) return;
            // The flat dust went when the round did. So does this.
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
