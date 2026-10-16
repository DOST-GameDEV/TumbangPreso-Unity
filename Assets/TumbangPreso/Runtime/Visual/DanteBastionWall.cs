using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BASTION, the ground stands up in front of him (`docs/reports/dante-presentation-2026-10-16/plan.md` section 4; owner approved
    /// 2026-10-16: waist-high basalt slabs plus a see-through heat field with a molten rim and a clear middle). The visual is
    /// exactly the reflect area (`DanteHeroKit.Barrier`: `GeoRules.BarrierWidth` wide at `GeoRules.BarrierForward`, up to 2.6 m).
    ///  * TELL (0 to .2 s): cracks run out sideways from his stamp along the line where the wall will stand.
    ///  * RELEASE (.2 to .45 s): seven slabs (`bastion.glb`, typed) rise out of the court in a ripple, centre first then
    ///    outward, each a jolt then a hold; the heat field draws up from their tops.
    ///  * FOLLOW: as he moves the slabs drag through the court, kicking grit at their feet.
    ///  * CONTACT: a reflected slipper lights a crack star on the field where it struck, cooling over 1 s, and four chips spray.
    ///  * WARNING (last 1.5 s): the seams and the rim cool, the slabs sag 5 cm.
    ///  * DISSIPATE: the wall is left where it stood and sinks back into the court in the same ripple, outside first.
    /// Everything is posed from its age; every peer sees the reflect from the slippers it watches cross the line.
    /// </summary>
    public sealed class DanteBastionWall : MonoBehaviour
    {
        public const float FieldTop = 2.6f;
        private const float RiseFrom = .2f, RiseStep = .035f, RiseJolt = .07f, WarnSeconds = 1.5f, SinkSeconds = .5f;
        // Centre first, then outward: the order each of the seven slabs rises (slab index -> rank).
        private static readonly int[] RiseRank = { 3, 2, 1, 0, 1, 2, 3 };

        private Transform _owner;
        private float _age, _life, _released = -1f, _grain;
        private Transform[] _slabs = new Transform[0];
        private Vector3[] _rest = new Vector3[0];
        private float[] _height = new float[0];
        private Renderer[] _seams = new Renderer[0];
        private GameObject _field, _tell;
        private Material _fieldMaterial, _tellMaterial;
        private Vector3 _lastPos;
        private readonly System.Collections.Generic.Dictionary<Slipper, float> _seen = new System.Collections.Generic.Dictionary<Slipper, float>();

        public static DanteBastionWall Build(Transform owner, float life)
        {
            // The name is the contract the follow tests and the old barrier shared ("DanteBarrier").
            var root = new GameObject("DanteBarrier");
            root.transform.SetParent(owner, false);
            root.transform.localPosition = new Vector3(0f, 0f, GeoRules.BarrierForward);
            var wall = root.AddComponent<DanteBastionWall>();
            wall._owner = owner; wall._life = life; wall._lastPos = owner.position;
            var model = DanteProp.Spawn("bastion", root.transform);
            if (model != null)
            {
                wall._slabs = new Transform[7]; wall._rest = new Vector3[7]; wall._height = new float[7]; wall._seams = new Renderer[7];
                for (int i = 0; i < 7; i++)
                {
                    var t = DanteProp.Find(model, "slab-" + i);
                    wall._slabs[i] = t;
                    if (t == null) continue;
                    wall._rest[i] = t.localPosition;
                    var r = t.GetComponentInChildren<MeshRenderer>();
                    wall._height[i] = r != null ? r.bounds.size.y + .1f : 1f;
                    var seam = DanteProp.Find(model, "glow-seam-" + i);
                    wall._seams[i] = seam != null ? seam.GetComponent<Renderer>() : null;
                }
            }
            wall.BuildField();
            wall.BuildTell();
            wall.StepTo(0f);
            return wall;
        }

        private void BuildField()
        {
            var shader = Resources.Load<Shader>("Shaders/DanteHeatField");
            if (shader == null) return;
            _field = new GameObject("BastionHeatField");
            _field.transform.SetParent(transform, false);
            float w = GeoRules.BarrierWidth, bottom = .95f, top = FieldTop;
            var mesh = new Mesh { name = "Bastion heat field" };
            mesh.SetVertices(new[] { new Vector3(-w * .5f, bottom, 0f), new Vector3(w * .5f, bottom, 0f), new Vector3(w * .5f, top, 0f), new Vector3(-w * .5f, top, 0f) });
            mesh.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateBounds();
            _field.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = _field.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            _fieldMaterial = new Material(shader) { name = "Bastion heat field" };
            _fieldMaterial.SetFloat("_Aspect", w / (top - bottom));
            _fieldMaterial.SetVector("_Hit", new Vector4(.5f, .5f, 9f, 0f));
            r.sharedMaterial = _fieldMaterial;
            VfxRenderTag.Own(_field, _fieldMaterial);
            VfxRenderTag.Attach(_field);
        }

        // The tell: two cracks run out sideways from under his stamp, along the wall's line, each with a typed kink.
        private void BuildTell()
        {
            _tell = new GameObject("BastionTellCracks");
            _tell.transform.SetParent(transform, false);
            var verts = new System.Collections.Generic.List<Vector3>(); var tris = new System.Collections.Generic.List<int>();
            void Strip(Vector2[] path, float w)
            {
                for (int i = 0; i < path.Length - 1; i++)
                {
                    var a = path[i]; var b = path[i + 1]; var d = (b - a).normalized; var s = new Vector2(-d.y, d.x);
                    float w0 = w * (1f - i / (float)path.Length), w1 = w * (1f - (i + 1) / (float)path.Length);
                    int n = verts.Count;
                    verts.Add(new Vector3(a.x - s.x * w0, 0, a.y - s.y * w0)); verts.Add(new Vector3(b.x - s.x * w1, 0, b.y - s.y * w1));
                    verts.Add(new Vector3(b.x + s.x * w1, 0, b.y + s.y * w1)); verts.Add(new Vector3(a.x + s.x * w0, 0, a.y + s.y * w0));
                    tris.AddRange(new[] { n, n + 2, n + 1, n, n + 3, n + 2 });
                }
            }
            // From the stamp (behind the wall line, at his feet) out to both ends of the wall.
            Strip(new[] { new Vector2(0f, -1.1f), new Vector2(.30f, -.2f), new Vector2(.85f, .06f), new Vector2(1.70f, -.04f) }, .035f);
            Strip(new[] { new Vector2(0f, -1.1f), new Vector2(-.24f, -.25f), new Vector2(-.92f, .04f), new Vector2(-1.70f, .07f) }, .032f);
            var mesh = new Mesh { name = "Bastion tell cracks" };
            mesh.SetVertices(verts); mesh.SetTriangles(tris, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            _tell.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = _tell.AddComponent<MeshRenderer>();
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            VfxMaterial.Ghost(r, new Color(1f, .43f, .08f, .92f), .55f);
            _tellMaterial = r.sharedMaterial;
        }

        /// <summary>The ability ended: the wall stays where it stood and sinks into the court, outside first.</summary>
        public void Release()
        {
            if (_released >= 0f) return;
            _released = _age;
            transform.SetParent(null, true);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            StepTo(_age);
            if (_released < 0f) { Follow(); WatchReflects(); }
            if (_released >= 0f && _age - _released > SinkSeconds + .1f) Destroy(gameObject);
        }

        /// <summary>Posed from the age alone.</summary>
        public void StepTo(float age)
        {
            // The wall follows him exactly; its pieces stand on the court under it, whatever his feet's height.
            float ground = Slipper.GroundY(transform.position);
            float dy = (ground - transform.position.y) / Mathf.Max(.0001f, transform.lossyScale.y);
            if (float.IsInfinity(ground) || float.IsNaN(dy)) dy = 0f;
            for (int c = 0; c < transform.childCount; c++)
            {
                var child = transform.GetChild(c);
                var lp = child.localPosition; lp.y = dy + (child == (_tell != null ? _tell.transform : null) ? .012f : 0f); child.localPosition = lp;
            }
            float warn = Mathf.Clamp01((age - (_life - WarnSeconds)) / WarnSeconds);
            float sink = _released >= 0f ? (age - _released) / SinkSeconds : 0f;
            for (int i = 0; i < _slabs.Length; i++)
            {
                var t = _slabs[i]; if (t == null) continue;
                int rank = RiseRank[i];
                float start = RiseFrom + rank * RiseStep;
                float u = Mathf.Clamp01((age - start) / RiseJolt);
                float up = u >= 1f ? 1f + Mathf.Max(0f, 1f - (age - start - RiseJolt) / .08f) * .04f : u * u;   // a jolt, a small overshoot, a hold
                // Sinks in the same ripple, outside first.
                float out_ = Mathf.Clamp01(sink * 1.6f - (3 - rank) * .18f);
                float sag = warn * .05f;
                t.localPosition = _rest[i] + Vector3.down * (_height[i] * (1f - Mathf.Min(1f, up)) + sag + out_ * out_ * _height[i])
                                  + Vector3.up * (Mathf.Max(0f, up - 1f) * _height[i]);
                t.localRotation = Quaternion.Euler(out_ * (i % 2 == 0 ? 9f : -7f), 0f, out_ * (i < 3 ? -6f : 6f));
                if (_seams[i] != null && _seams[i].sharedMaterial != null)
                    _seams[i].sharedMaterial.SetFloat("_Intensity", (u >= 1f ? 1f : 0f) * (1f - .65f * warn) * (1f - Mathf.Clamp01(out_ * 2f)));
            }
            if (_fieldMaterial != null)
            {
                float rise = Mathf.Clamp01((age - .3f) / .2f) * (1f - Mathf.Clamp01(sink * 2.5f));
                _field.SetActive(rise > .01f);
                _fieldMaterial.SetFloat("_Rise", rise);
                _fieldMaterial.SetFloat("_Cool", warn);
                _fieldMaterial.SetFloat("_Calm", Settings.SettingsStore.Current.ReducedEffects ? 1f : 0f);
                var hit = _fieldMaterial.GetVector("_Hit"); hit.z += Time.deltaTime; _fieldMaterial.SetVector("_Hit", hit);
            }
            if (_tell != null)
            {
                float run = Mathf.Clamp01(age / .2f), cool = Mathf.Clamp01((age - .45f) / .5f);
                _tell.SetActive(age < 1f && _released < 0f);
                _tell.transform.localScale = new Vector3(run, 1f, 1f);
                if (_tellMaterial != null) _tellMaterial.SetColor("_EmissionColor", Color.Lerp(new Color(1f, .42f, .06f), new Color(.55f, .16f, .06f) * .3f, cool) * .9f);
            }
        }

        /// <summary>It follows him: when he moves, grit kicks at the slabs' feet as they drag through the court.</summary>
        private void Follow()
        {
            if (_owner == null) return;
            float dt = Time.deltaTime; if (dt <= 0f) return;
            float speed = (_owner.position - _lastPos).magnitude / dt;
            _lastPos = _owner.position;
            _grain -= dt;
            if (speed > 1.2f && _grain <= 0f && _age > .45f)
            {
                // Alternate ends of the wall, at the slabs' feet.
                float side = (Mathf.FloorToInt(_age * 7f) % 2 == 0 ? -1f : 1f) * (.4f + .25f * (Mathf.FloorToInt(_age * 3f) % 3));
                DanteBoulderGrit.Drop(transform.TransformPoint(new Vector3(side, .08f, .2f)), true);
                _grain = .12f;
            }
        }

        /// <summary>Every peer sees the reflect from the slipper it watches: one flash per approach that turns back at the line.</summary>
        private void WatchReflects()
        {
            if (_fieldMaterial == null || _age < .3f) return;
            foreach (var s in Object.FindObjectsByType<Slipper>(FindObjectsSortMode.None))
            {
                if (s == null || s.State != SlipperState.InFlight) { if (s != null) _seen.Remove(s); continue; }
                Vector3 local = transform.InverseTransformPoint(s.transform.position);
                bool inBand = Mathf.Abs(local.z) < .55f && Mathf.Abs(local.x) < GeoRules.BarrierWidth * .5f && local.y < FieldTop + .2f;
                float along = Vector3.Dot(s.Velocity, transform.forward);
                if (!inBand) continue;
                if (along < 0f) { _seen[s] = 0f; continue; }
                if (_seen.TryGetValue(s, out _) )
                {
                    _seen.Remove(s);
                    Flash(local, s.transform.position);
                }
            }
        }

        private void Flash(Vector3 local, Vector3 world)
        {
            float u = Mathf.InverseLerp(-GeoRules.BarrierWidth * .5f, GeoRules.BarrierWidth * .5f, local.x);
            float v = Mathf.InverseLerp(.95f, FieldTop, local.y);
            _fieldMaterial.SetVector("_Hit", new Vector4(u, Mathf.Clamp01(v), 0f, 0f));
            DanteStatusShatter.Rubble(world, transform.forward, 4, .7f, true);
        }

        /// <summary>For a review: the last reflect's age on the field (9 when none).</summary>
        public float LastReflectAge => _fieldMaterial != null ? _fieldMaterial.GetVector("_Hit").z : 9f;
        public int SlabCount { get { int n = 0; foreach (var t in _slabs) if (t != null) n++; return n; } }
    }
}
