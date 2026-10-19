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
    ///
    /// ⚠️⚠️ AND SINCE 2026-10-19 A MATCH DRAWS THE APPROVED WALL INSTEAD (<see cref="Approved"/>, on by default). The owner,
    /// after the bench film b8b: "bastion b8 can go in game now". So the wall in a match is the bench effect itself
    /// (`FxStudyDanteBastion.ForMatch`, b8: seven basalt slabs with magma in them, three wide heat columns rising out of
    /// the slabs, the shimmer between, the split it comes out of and its dust), not a copy of it, posed from this
    /// component's own clock. Its sizes ARE the rule (`FxStudyDanteBastion.Width`, `Forward`, `FieldTop` and `Life` are
    /// `GeoRules.BarrierWidth`, `BarrierForward`, 2.6 m and `BarrierSeconds`, and `DanteWardRuleTests` asserts it), so it
    /// is drawn at its approved size, unstretched, exactly where slippers turn back. It rises out of the court, cools in
    /// its last 1.5 s, and when the skill ends it stays where it stood and is eaten and sinks, outside first.
    /// Off, ASTRA's look below is built exactly as before.
    /// </summary>
    public sealed class DanteBastionWall : MonoBehaviour
    {
        /// <summary>The approved b8b wall (true) or ASTRA's slabs, heat field and stone rim (false). Read when a wall is built.</summary>
        public static bool Approved = true;
        public const float FieldTop = 2.6f;
        // The approved wall's end, after the skill ends: the slabs are under the court by 0.63 s and the last of the dust
        // kicked at their ends is gone by 0.90 s (`FxStudyDanteBastion`'s own beats).
        private const float ApprovedGone = .95f;
        private FxStudyDanteBastion _fx;
        private float _since;
        private int _approvedSlabs, _strikeNext;
        private readonly System.Collections.Generic.List<Vector3> _strikes = new System.Collections.Generic.List<Vector3>(FxStudyDanteBastion.MatchStrikes);
        /// <summary>True when this wall is the approved look.</summary>
        public bool DrawsApproved => _fx != null;
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
            if (Approved && wall.BuildApproved()) { wall.StepTo(0f); return wall; }
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
            wall.BuildCrust();
            wall.BuildTell();
            wall.StepTo(0f);
            return wall;
        }

        /// <summary>
        /// The approved wall, stood on his line. This object stands `BarrierForward` in front of him, his way; the bench
        /// effect stands at his feet with its +x his way and its wall `Forward` along it, so it hangs under a holder put
        /// back to his feet and turned a quarter. False (and nothing made) if its model is missing, so ASTRA's is built.
        /// </summary>
        private bool BuildApproved()
        {
            if (FxStudyDanteBastion.Wall(FxStudyDanteBastion.InGame) == null) return false;
            var model = Resources.Load<GameObject>(FxStudyDanteBastion.ModelB4);
            if (model != null) foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true)) if (filter.name.StartsWith("slab-")) _approvedSlabs++;
            var stand = new GameObject("BastionApproved").transform;
            stand.SetParent(transform, false);
            stand.localPosition = new Vector3(0f, 0f, -GeoRules.BarrierForward);
            stand.localRotation = Quaternion.Euler(0f, -90f, 0f);
            // The stone is shaded by a light of its own: in a match, the map's sun. Its material reads the light when it is
            // made, so the bench's light is put back at once and a bench film after a Play session is lit as it was filmed.
            Vector3 bench = FxStudyDanteBoulder.KeyLight;
            if (RenderSettings.sun != null) FxStudyDanteBoulder.KeyLight = -RenderSettings.sun.transform.forward;
            try { _fx = FxStudyDanteBastion.ForMatch(stand); }
            finally { FxStudyDanteBoulder.KeyLight = bench; }
            return true;
        }

        private void OnDestroy()
        {
            // In Play its materials go with it. Outside Play (a film) the film frees it first (`FreeLook`): nothing may be
            // destroyed at once from inside OnDestroy.
            if (_fx != null && Application.isPlaying) _fx.Dispose();
            _fx = null;
        }

        /// <summary>Frees the approved wall's own pieces now. For a film outside Play, before the wall is destroyed.</summary>
        public void FreeLook() { _fx?.Dispose(); _fx = null; }

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

        // THE STONE RIM (owner on kit v3, 2026-10-17: "refine pa gagu ano yan u js put 2d drawings on the hologram"). The crust
        // was a pattern in the field's flat shader, a painted sheet from the side. Now the rim is real basalt: typed chunks with
        // depth frame the field's sides and top, three shards jut in to break the line, and the molten joints show between them
        // (the shader). Each: across the field (0 left to 1 right), up it (0 slab tops to 1 its top), its width, height and depth
        // in metres, its roll and its seed.
        private static readonly (float u, float v, float w, float h, float d, float roll, int seed)[] CrustRows =
        {
            (.03f, .06f, .36f, .40f, .34f,  8f,  3), (.02f, .25f, .30f, .36f, .30f, -6f,  7), (.04f, .44f, .34f, .42f, .36f, 12f, 11),
            (.02f, .63f, .28f, .34f, .28f, -9f, 13), (.04f, .81f, .32f, .38f, .32f,  5f, 17), (.06f, .97f, .40f, .30f, .34f, 20f, 19),
            (.97f, .05f, .34f, .38f, .32f, -7f, 23), (.98f, .23f, .32f, .40f, .34f, 10f, 29), (.96f, .42f, .30f, .34f, .30f, -12f, 31),
            (.98f, .61f, .36f, .40f, .36f,  6f, 37), (.96f, .80f, .28f, .36f, .28f, -4f, 41), (.94f, .97f, .38f, .32f, .32f, -18f, 43),
            (.18f, .99f, .34f, .26f, .30f,  4f, 47), (.31f, 1.0f, .30f, .24f, .28f, -8f, 53), (.44f, .98f, .36f, .28f, .32f, 10f, 59),
            (.57f, 1.0f, .32f, .25f, .30f, -5f, 61), (.70f, .99f, .34f, .27f, .30f,  7f, 67), (.83f, 1.0f, .30f, .24f, .28f, -10f, 71),
            (.11f, .86f, .22f, .20f, .22f, 35f, 73), (.89f, .88f, .20f, .22f, .20f, -30f, 79), (.09f, .16f, .20f, .18f, .22f, -25f, 83),
        };
        private Transform[] _crust = new Transform[0];
        private Vector3[] _crustRest = new Vector3[0];

        private void BuildCrust()
        {
            float w = GeoRules.BarrierWidth, bottom = .95f, top = FieldTop;
            var holder = new GameObject("BastionStoneRim").transform;
            holder.SetParent(transform, false);
            var tones = new[] { new Color(.24f, .2f, .17f), new Color(.3f, .25f, .21f), new Color(.2f, .17f, .14f) };
            _crust = new Transform[CrustRows.Length]; _crustRest = new Vector3[CrustRows.Length];
            for (int i = 0; i < CrustRows.Length; i++)
            {
                var k = CrustRows[i];
                var go = new GameObject("RimStone" + i);
                go.transform.SetParent(holder, false);
                // A five-sided rock lying with its axis through the field (toward the thrower), so it has depth from every side.
                go.AddComponent<MeshFilter>().sharedMesh = VfxShapes.Prism(5, 1f, .72f, .32f, 0f, k.seed);
                var r = go.AddComponent<MeshRenderer>();
                VfxMaterial.Solid(r, tones[i % 3], .08f);
                _crustRest[i] = new Vector3((k.u - .5f) * w, Mathf.Lerp(bottom, top, k.v), 0f);
                _crust[i] = go.transform;
            }
        }

        /// <summary>The rim's stones from the field's rise, its warning and its sink: up off the slabs, a sag, then a fall.</summary>
        private void PoseCrust(float rise, float warn, float sink)
        {
            for (int i = 0; i < _crust.Length; i++)
            {
                var t = _crust[i]; if (t == null) continue;
                var k = CrustRows[i];
                // Each stone pops up as the field's top passes its height: a jolt, a small overshoot, a hold.
                float u = Mathf.Clamp01((rise - k.v * .9f) / .12f);
                float grow = u >= 1f ? 1f : u * u * (1f + .3f * (1f - u));
                // On the sink it drops out of the rim and tumbles, the top stones first, gone below the court (never faded).
                float fall = Mathf.Clamp01(sink * 1.8f - (1f - k.v) * .5f);
                var at = _crustRest[i] + Vector3.down * (warn * .05f + fall * fall * 3.2f);
                t.localPosition = at;
                t.localRotation = Quaternion.Euler(90f + fall * 70f * (i % 2 == 0 ? 1f : -1f), (k.seed % 7 - 3) * 4f, k.roll + fall * 40f);
                // The prism's axis is its height: lying through the field, so its depth is the axis and its face is w by h.
                // At 0.8 of their typed size, so the molten joint behind them shows in the gaps (kit film r9: they closed up solid).
                t.localScale = new Vector3(k.w, k.d, k.h) * (.8f * Mathf.Max(0f, grow));
                bool on = grow > .01f && at.y > -.2f;
                if (t.gameObject.activeSelf != on) t.gameObject.SetActive(on);
            }
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
            if (_released < 0f) { if (_fx == null) Follow(); WatchReflects(); }
            if (_released >= 0f && _age - _released > (_fx != null ? ApprovedGone : SinkSeconds + .1f)) Destroy(gameObject);
        }

        /// <summary>For a film outside Play, where `Update` does not run: one frame of `dt` seconds, as `Update` plays it.</summary>
        public void Advance(float dt) { _age += dt; StepTo(_age); }

        // The court's height under the wall, in its own space (0 if nothing is under it).
        private float _ground;

        /// <summary>Posed from the age alone.</summary>
        public void StepTo(float age)
        {
            // The wall follows him exactly; its pieces stand on the court under it, whatever his feet's height.
            float ground = Slipper.GroundY(transform.position);
            float dy = (ground - transform.position.y) / Mathf.Max(.0001f, transform.lossyScale.y);
            if (float.IsInfinity(ground) || float.IsNaN(dy)) dy = 0f;
            _ground = dy;
            for (int c = 0; c < transform.childCount; c++)
            {
                var child = transform.GetChild(c);
                var lp = child.localPosition; lp.y = dy + (child == (_tell != null ? _tell.transform : null) ? .012f : 0f); child.localPosition = lp;
            }
            if (_fx != null)
            {
                // The approved wall's clock: standing, the age (held at its life, should the end come a frame late); once
                // the skill has ended, its life plus the time since, so it is eaten and sinks on its own beats.
                _since = _released < 0f ? Mathf.Min(age, FxStudyDanteBastion.Life) : FxStudyDanteBastion.Life + (age - _released);
                _fx.SampleMatch(_since, _strikes);
                return;
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
            PoseCrust(Mathf.Clamp01((age - .3f) / .2f), warn, sink);
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
            if ((_fieldMaterial == null && _fx == null) || _age < .3f) return;
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

        /// <summary>A slipper turned back off the wall at `world`: the approved wall lights there and kicks its dust. For a
        /// film outside Play as well, where no real slipper flies.</summary>
        public void Strike(Vector3 world) => Flash(transform.InverseTransformPoint(world), world);

        private void Flash(Vector3 local, Vector3 world)
        {
            if (_fx != null)
            {
                // Where it struck, in the approved wall's own terms: across from its middle, up from the court, and when.
                var at = new Vector3(Mathf.Clamp(local.x, -GeoRules.BarrierWidth * .5f, GeoRules.BarrierWidth * .5f), Mathf.Clamp(local.y - _ground, 0f, FieldTop), _since);
                if (_strikes.Count < FxStudyDanteBastion.MatchStrikes) _strikes.Add(at); else _strikes[_strikeNext] = at;
                _strikeNext = (_strikeNext + 1) % FxStudyDanteBastion.MatchStrikes;
                return;
            }
            float u = Mathf.InverseLerp(-GeoRules.BarrierWidth * .5f, GeoRules.BarrierWidth * .5f, local.x);
            float v = Mathf.InverseLerp(.95f, FieldTop, local.y);
            _fieldMaterial.SetVector("_Hit", new Vector4(u, Mathf.Clamp01(v), 0f, 0f));
            DanteStatusShatter.Rubble(world, transform.forward, 4, .7f, true);
        }

        /// <summary>For a review: the last reflect's age on the field (9 when none).</summary>
        public float LastReflectAge
        {
            get
            {
                if (_fx != null) return _strikes.Count == 0 ? 9f : _since - _strikes[(_strikeNext + FxStudyDanteBastion.MatchStrikes - 1) % FxStudyDanteBastion.MatchStrikes].z;
                return _fieldMaterial != null ? _fieldMaterial.GetVector("_Hit").z : 9f;
            }
        }
        public int SlabCount { get { if (_fx != null) return _approvedSlabs; int n = 0; foreach (var t in _slabs) if (t != null) n++; return n; } }
    }
}
