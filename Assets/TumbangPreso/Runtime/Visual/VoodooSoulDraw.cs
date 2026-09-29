using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE REACH IS THE VICTIM'S SOUL PULLED OUT OF THEM INTO HER DOLL, NOT A CORD (HERO-10 v3, 2026-09-29). The owner on films
    /// v12 to v16, where the reach was a thread from her palm to them: *"dont make the pulling thing look like a physical line i want it
    /// to look like sucking aura or smth"*, *"it sucks rn ur implementation"*, *"i want u to make her hold up her voodoo too towards the
    /// person when markingt hem"*. Then on v19, where it was blobs of coloured smoke round the victim and the doll sat in the air past
    /// her hand: *"this animation dont look that good yet"*, *"lock in thoroughly improve also hthe doll is floating"*. His table already
    /// said what it is: *"attach their soul to the voodoo doll"*.
    ///
    /// | Moment | What | Direction |
    /// |---|---|---|
    /// | the lock | the doll comes up GRIPPED in her left hand (her fist round its lower body), held out at them; a see-through GHOST of the victim, their own shape lit in the curse's colour, appears over their body | the doll toward them |
    /// | the hold (2 s) | the ghost is dragged OUT of their body toward the doll, further as it fills, smearing toward her with bands of light crawling along it; fine bright motes tear off the ghost and are sucked in braided currents into the doll, faster and smaller as they go in (DRAIN's currents twist round each other, HEX's waver straight); a light swells at the doll | out of them, INTO the doll |
    /// | the mark | the gulp: the ghost is yanked the rest of the way, shrinking and eaten away as it goes, into the doll, which flares; a last burst of motes follows | into the doll |
    /// | broken | the ghost snaps back into their body and fades; the motes lose the pull and drift up | back to them |
    ///
    /// Body-owned like `VoodooCursePresenter` (which adds it) and read off the body's REPLICATED reach (`CharacterMotor.Voodoo.cs`),
    /// so every screen draws the same thing and a rejoiner sees a reach already running. Nothing here is a line: the ghost is the
    /// victim's own shape (their skinned meshes baked each frame, `Shaders/VoodooGhost`) and the stream is separate motes
    /// (`Shaders/VoodooWisp`). On the VICTIM'S OWN screen their body is the lens, so the ghost is not drawn there; motes rise from under
    /// their view and stream away toward her.
    ///
    /// ⚠️ THE DOLL IS IN HER HAND EXACTLY WHILE THE SLIPPER IS AT HER BELT (`CharacterMotor.StowsCarriedSlipper`: the reach, DRAIN's
    /// wring and HEX's stab). It is the ultimate's doll at hand size (`PhaisterDollArt`, plan 9.10), its light in its openings.
    /// Everyone else sees it in her body's left fist; on her own screen her body is hidden and a copy rides her first-person left hand
    /// (`ViewmodelArms.HoldingProp` lifts it into view).
    /// </summary>
    public sealed class VoodooSoulDraw : MonoBehaviour
    {
        /// <summary>The held doll's height, metres: on her body (the cast's scale), and in her first-person hand.</summary>
        private const float BodyDollHeight = 0.62f, ViewDollHeight = 0.42f;

        /// <summary>How much of the doll's height sits below the top of her fist: she holds it round its legs and waist.</summary>
        private const float GripDepth = 0.3f;

        /// <summary>Motes a second at the start of the hold and when it is full.</summary>
        private const float EmitStart = 80f, EmitFull = 170f;

        /// <summary>How far the ghost has come out of their body toward the doll at the start and end of the hold, metres.</summary>
        private const float GhostOutStart = 0.3f, GhostOutFull = 1.25f;

        private const int MaxMotes = 420;
        private const float GulpSeconds = 0.3f, ReleaseSeconds = 0.5f, SnapSeconds = 0.25f;

        private struct Mote
        {
            public Vector3 Offset;     // from the ghost's centre (world axes), or in the victim's LENS frame when Lens
            public bool Lens;
            public float S, Rate, Seed, Size, Age;
            public int Strand;
            public Vector3 Pos, Vel;
            public bool Released;
        }

        /// <summary>
        /// One of the victim's meshes, redrawn as the ghost. ⚠️ A REAL RENDERER, NOT `Graphics.DrawMesh` (review v20: the ghost was
        /// on no frame at all, because a draw call queued in `LateUpdate` is not in a camera rendered before it).
        /// </summary>
        private sealed class GhostPart
        {
            public Renderer Source;
            public Mesh Baked;
            public GameObject Go;
            public MeshFilter Filter;
            public MeshRenderer Draw;
            public MaterialPropertyBlock Block;
        }

        private CharacterMotor _body;
        private ParticleSystem _ps;
        private ParticleSystem.Particle[] _out;
        private readonly List<Mote> _motes = new List<Mote>(MaxMotes);
        private int _victim = -1;
        private VoodooMarkKind _kind;
        private bool _wasReaching;
        private float _emitCarry;
        private float _flash;

        // The ghost.
        private enum GhostState { None, Pull, Yank, Snap }
        private GhostState _ghostState;
        private CharacterMotor _ghostOf;
        private readonly List<GhostPart> _ghost = new List<GhostPart>();
        private float _ghostAge;
        private float _ghostOut;
        private Vector3 _ghostCentre, _ghostOffset;
        private float _ghostScale = 1f;
        private readonly List<Vector3> _vertexScratch = new List<Vector3>(2048);

        // The doll in her hand.
        private GameObject _bodyDoll, _viewDoll;
        private float _bodyDollFoot;   // metres from the doll's pivot down to its feet, at its held scale
        private Transform _leftArm;
        private Vector3 _leftPalm;
        private ViewmodelArms _arms;
        private float _dollAge;

        private static Material _moteMaterial, _ghostMaterial;

        private static Material MoteMaterial => _moteMaterial != null ? _moteMaterial : (_moteMaterial = Make("Shaders/VoodooWisp", "VoodooWisp"));
        private static Material GhostMaterial => _ghostMaterial != null ? _ghostMaterial : (_ghostMaterial = Make("Shaders/VoodooGhost", "VoodooGhost"));

        private static Material Make(string path, string name)
        {
            var shader = Resources.Load<Shader>(path);
            return shader == null ? null : new Material(shader) { name = name };
        }

        private void Awake() => _body = GetComponent<CharacterMotor>();

        private void LateUpdate()
        {
            if (_body == null) return;
            float dt = Time.deltaTime;
            StepDoll(dt);
            StepDraw(dt);
        }

        private Color Hue => _kind == VoodooMarkKind.Drain ? VoodooCursePresenter.DrainHue : VoodooCursePresenter.HexHue;

        // ------------------------------------------------------------------ the doll in her fist

        private void StepDoll(float dt)
        {
            bool holding = _body.StowsCarriedSlipper && _body.isActiveAndEnabled;
            if (!holding) { LetDoll(); return; }
            _dollAge += dt;
            if (_bodyDoll == null && _viewDoll == null) HoldDoll();

            Vector3 at = Victim(out _) is CharacterMotor v ? v.transform.position : transform.position + transform.forward * 4f;
            float twitch = Twitch();
            if (_bodyDoll != null && _leftArm != null)
            {
                // Standing in her fist, gripped round its legs and waist, its face turned to whoever she holds it at.
                Vector3 palm = _leftArm.TransformPoint(_leftPalm);
                Vector3 toward = at - palm; toward.y = 0f;
                if (toward.sqrMagnitude < 0.01f) toward = transform.forward;
                _bodyDoll.transform.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up) * Quaternion.Euler(-4f + 12f * twitch, 0f, 8f * twitch);
                _bodyDoll.transform.position = palm + Vector3.up * (_bodyDollFoot - BodyDollHeight * GripDepth);
            }
            if (_viewDoll != null)
            {
                var view = Camera.main;
                if (view != null)
                {
                    // On her own screen it faces AWAY from her, at them, a little turned so its button eye shows.
                    _viewDoll.transform.rotation = Quaternion.LookRotation(view.transform.forward, view.transform.up) * Quaternion.Euler(8f * twitch, 155f, 6f * twitch);
                }
            }
        }

        /// <summary>The doll twitches in time with the victim's heartbeat while she reaches (plan 9.5), faster as it fills.</summary>
        private float Twitch()
        {
            if (!_body.IsVoodooReaching) return 0f;
            float rate = Mathf.Lerp(1.2f, 2.6f, _body.VoodooReachProgress);
            float beat = Mathf.Repeat(_dollAge * rate, 1f);
            return beat < 0.14f ? Mathf.Sin(beat / 0.14f * Mathf.PI) : 0f;
        }

        private void HoldDoll()
        {
            var art = PhaisterDollArt.LoadArt();
            var visual = GetComponent<CharacterVisual>();
            var skinned = visual != null && visual.Model != null ? visual.Model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (skinned != null)
                for (int i = 0; i < skinned.bones.Length; i++)
                    if (skinned.bones[i] != null && skinned.bones[i].name == "arm-left" && CharacterVisual.PalmCentre(skinned, i, out var palm))
                    { _leftArm = skinned.bones[i]; _leftPalm = palm; break; }

            bool mine = ViewmodelArms.IsFirstPersonFor(_body);
            if (_leftArm != null)
            {
                _bodyDoll = MakeDoll(art, null, BodyDollHeight, "VoodooHeldDoll", out _bodyDollFoot);
                // Her own lens is inside her body: that doll is for everyone else.
                if (_bodyDoll != null && mine) _bodyDoll.AddComponent<HiddenFromMainCamera>();
            }
            if (!mine) return;
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>())
            {
                if (arms == null || arms.BoundCharacter != _body) continue;
                var left = arms.LeftHandForProps();
                if (left == null) break;
                _viewDoll = MakeDoll(art, left, ViewDollHeight, "VoodooHeldDollFirstPerson", out float foot);
                if (_viewDoll != null)
                {
                    // In the fist, as on her body: its feet below the top of the hand.
                    float scale = Mathf.Max(0.0001f, left.lossyScale.y);
                    _viewDoll.transform.localPosition = arms.LeftPalmOffset() + new Vector3(0f, (foot - ViewDollHeight * GripDepth) / scale, 0f);
                    foreach (var t in _viewDoll.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = left.gameObject.layer;
                }
                arms.HoldingProp = true;
                _arms = arms;
                break;
            }
        }

        /// <summary>
        /// The ultimate's doll at <paramref name="height"/> metres, its light painted in its openings, no animator (it hangs limp).
        /// <paramref name="foot"/> is how far its pivot sits ABOVE its feet at that size, so a caller can stand it in a fist.
        /// </summary>
        private static GameObject MakeDoll(RosterEntryAsset art, Transform parent, float height, string name, out float foot)
        {
            foot = 0f;
            if (art == null || art.Model == null) return null;
            var go = Object.Instantiate(art.Model, parent, false);
            go.name = name;
            foreach (var animator in go.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = true;
            }
            ToonSkin.Apply(go, ToonSkin.PropOutlineWidth * 0.6f, art.Palette);
            PhaisterDollArt.ApplyGlow(go);
            // Sized off its own bounds, so a rebuilt model keeps its hand size.
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            var bounds = new Bounds(go.transform.position, Vector3.zero);
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
            }
            float tall = any ? Mathf.Max(0.01f, bounds.size.y) : 1f;
            float k = height / tall;
            foot = any ? (go.transform.position.y - bounds.min.y) * k : 0f;
            float parentScale = parent != null ? Mathf.Max(0.0001f, parent.lossyScale.y) : 1f;
            go.transform.localScale = Vector3.one * (k / parentScale);
            return go;
        }

        private void LetDoll()
        {
            _dollAge = 0f;
            if (_bodyDoll != null) Destroy(_bodyDoll);
            if (_viewDoll != null) Destroy(_viewDoll);
            _bodyDoll = _viewDoll = null;
            if (_arms != null) { _arms.HoldingProp = false; _arms = null; }
        }

        /// <summary>
        /// Where the soul goes: the doll's middle, on the screen that is looking (her first-person doll on hers, her body's doll on
        /// everyone else's), or her left fist if the doll is missing.
        /// </summary>
        private Vector3 Intake()
        {
            if (_viewDoll != null && _viewDoll.activeInHierarchy) return Centre(_viewDoll);
            if (_bodyDoll != null) return Centre(_bodyDoll);
            if (_leftArm != null) return _leftArm.TransformPoint(_leftPalm);
            return transform.position + Vector3.up * 1.1f + transform.forward * 0.5f;
        }

        private static Vector3 Centre(GameObject go)
        {
            var r = go.GetComponentInChildren<Renderer>();
            return r != null ? r.bounds.center : go.transform.position;
        }

        private CharacterMotor Victim(out bool lens)
        {
            lens = false;
            int seat = _body.IsVoodooReaching ? _body.VoodooReachTarget : _victim;
            if (seat < 0 || GameServices.Round == null) return null;
            var v = GameServices.Round.PlayerAt(seat);
            lens = v != null && ViewmodelArms.IsFirstPersonFor(v);
            return v;
        }

        // ------------------------------------------------------------------ the draw

        private void StepDraw(float dt)
        {
            bool reaching = _body.IsVoodooReaching && _body.isActiveAndEnabled;
            if (reaching && (!_wasReaching || _victim != _body.VoodooReachTarget || _kind != _body.VoodooReachKind))
            {
                _victim = _body.VoodooReachTarget; _kind = _body.VoodooReachKind;
                _emitCarry = 0f;
                BeginGhost(Victim(out _));
            }
            if (!reaching && _wasReaching) EndReach(_body.VoodooReachSucceeded);
            _wasReaching = reaching;

            var victim = Victim(out bool lens);
            Vector3 intake = Intake();
            float fill = reaching ? _body.VoodooReachProgress : 1f;

            StepGhost(dt, victim, lens, intake, fill);

            if (reaching && victim != null)
            {
                _emitCarry += dt * Mathf.Lerp(EmitStart, EmitFull, fill);
                while (_emitCarry >= 1f && _motes.Count < MaxMotes) { _emitCarry -= 1f; _motes.Add(Tear(victim, lens, fast: false)); }
                _emitCarry = Mathf.Min(_emitCarry, 1f);
            }
            _flash = Mathf.Max(0f, _flash - dt / GulpSeconds);

            if (_motes.Count == 0 && !reaching && _flash <= 0f) { if (_ps != null) _ps.Clear(); return; }
            Ensure();

            Color hue = Hue;
            int count = 0;
            if (_out == null || _out.Length < MaxMotes + 1) _out = new ParticleSystem.Particle[MaxMotes + 1];
            for (int i = _motes.Count - 1; i >= 0; i--)
            {
                var m = _motes[i];
                if (!StepMote(ref m, dt, victim, lens, intake)) { _motes.RemoveAt(i); continue; }
                _motes[i] = m;
            }
            foreach (var m in _motes)
            {
                float fadeIn = Mathf.Clamp01(m.Age / 0.12f);
                float fadeOut = m.Released ? 1f - Mathf.Clamp01(m.Age / ReleaseSeconds) : Mathf.Clamp01((1f - m.S) / 0.08f);
                float life = Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut));
                float size = m.Released ? m.Size : Mathf.Lerp(m.Size, m.Size * 0.35f, Mathf.Clamp01(m.S));
                // Hotter as it goes in.
                Color c = Color.Lerp(hue, Color.Lerp(hue, Color.white, 0.55f), Mathf.Clamp01(m.S * 1.3f - 0.2f));
                c.a = life;
                _out[count++] = Particle(m.Pos, m.Vel, size, c, m.Seed);
            }
            // The light at the doll: it swells while it drinks and flares at the mark.
            if (reaching || _flash > 0f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(7f, 15f, fill));
                bool view = _viewDoll != null;
                float size = (view ? 0.1f : 0.26f) * (0.6f + 0.7f * fill + 0.2f * pulse) + (view ? 0.28f : 0.8f) * _flash;
                Color c = Color.Lerp(hue, Color.white, 0.35f + 0.4f * _flash);
                c.a = Mathf.Clamp01(0.35f + 0.4f * fill + _flash);
                _out[count++] = Particle(intake, Vector3.zero, size, c, 0.37f);
            }
            _ps.SetParticles(_out, count);
        }

        // ------------------------------------------------------------------ the ghost

        private void BeginGhost(CharacterMotor victim)
        {
            ClearGhost();
            if (victim == null) return;
            var visual = victim.GetComponent<CharacterVisual>();
            var model = visual != null ? visual.Model : null;
            if (model == null) return;
            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                if (r == null || !r.enabled) continue;
                if (r is SkinnedMeshRenderer s) { if (s.sharedMesh == null) continue; }
                else if (r is MeshRenderer) { var f = r.GetComponent<MeshFilter>(); if (f == null || f.sharedMesh == null) continue; }
                else continue;
                // The doll's light meshes and anything already drawn as light are not part of a person.
                if (r.name == PhaisterDollArt.GlowMeshName || r.name == PhaisterDollArt.SpillMeshName) continue;
                var part = new GhostPart { Source = r, Baked = r is SkinnedMeshRenderer ? new Mesh { name = "VoodooGhost" } : null, Block = new MaterialPropertyBlock() };
                part.Go = new GameObject("VoodooGhost-" + r.name);
                part.Filter = part.Go.AddComponent<MeshFilter>();
                part.Draw = part.Go.AddComponent<MeshRenderer>();
                part.Draw.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                part.Draw.receiveShadows = false;
                part.Draw.enabled = false;
                _ghost.Add(part);
            }
            _ghostOf = victim;
            _ghostState = GhostState.Pull;
            _ghostAge = 0f;
            _ghostOut = 0f;
            _ghostScale = 1f;
        }

        private void ClearGhost()
        {
            foreach (var part in _ghost)
            {
                if (part.Baked != null) Destroy(part.Baked);
                if (part.Go != null) Destroy(part.Go);
            }
            _ghost.Clear();
            _ghostOf = null;
            _ghostState = GhostState.None;
        }

        private void StepGhost(float dt, CharacterMotor victim, bool lens, Vector3 intake, float fill)
        {
            if (_ghostState == GhostState.None || _ghostOf == null || _ghost.Count == 0) { if (_ghostState != GhostState.None) ClearGhost(); return; }
            _ghostAge += dt;
            var capsule = _ghostOf.GetComponent<CharacterController>();
            float height = capsule != null ? capsule.height * _ghostOf.transform.lossyScale.y : 1.6f;
            _ghostCentre = _ghostOf.transform.position + Vector3.up * height * 0.55f;
            Vector3 span = intake - _ghostCentre;
            Vector3 pull = span.sqrMagnitude > 1e-4f ? span.normalized : _ghostOf.transform.forward;

            float alpha, stretch, dissolve = 0f;
            switch (_ghostState)
            {
                case GhostState.Pull:
                {
                    // Coming out of them: eased so it tears free slowly, then leans harder as the hold fills.
                    float fade = Mathf.Clamp01(_ghostAge / 0.2f);
                    _ghostOut = Mathf.Lerp(GhostOutStart, GhostOutFull, 1f - (1f - fill) * (1f - fill));
                    _ghostScale = 1.03f;
                    alpha = fade * Mathf.Lerp(0.75f, 1f, fill);
                    stretch = Mathf.Lerp(0.3f, 0.85f, fill);
                    break;
                }
                case GhostState.Yank:
                {
                    // The gulp: dragged the rest of the way into the doll, shrinking and eaten away from the far end.
                    float u = Mathf.Clamp01(_ghostAge / GulpSeconds);
                    float e = u * u;
                    _ghostOut = Mathf.Lerp(GhostOutFull, span.magnitude, e);
                    _ghostScale = Mathf.Lerp(1.03f, 0.08f, e);
                    alpha = 1f - u * 0.3f;
                    stretch = Mathf.Lerp(1.2f, 0.4f, u);
                    dissolve = u * 0.9f;
                    if (u >= 1f) { ClearGhost(); return; }
                    break;
                }
                default:
                {
                    // Let go: it snaps back into them and fades.
                    float u = Mathf.Clamp01(_ghostAge / SnapSeconds);
                    _ghostOut = Mathf.Lerp(_ghostOut, 0f, 1f - Mathf.Exp(-dt * 18f));
                    alpha = 1f - u;
                    stretch = Mathf.Lerp(0.6f, 0f, u);
                    if (u >= 1f) { ClearGhost(); return; }
                    break;
                }
            }
            _ghostOffset = pull * _ghostOut;
            var material = GhostMaterial;
            Vector3 centre = _ghostCentre + _ghostOffset;
            foreach (var part in _ghost)
            {
                if (part.Source == null || part.Go == null) continue;
                // Their own eyes are the lens on their own screen: the ghost is for everyone else.
                bool show = !lens && material != null && part.Source.enabled;
                part.Draw.enabled = show;
                if (!show) continue;
                Mesh mesh;
                Vector3 scale;
                if (part.Baked != null)
                {
                    ((SkinnedMeshRenderer)part.Source).BakeMesh(part.Baked, true);
                    mesh = part.Baked;
                    scale = Vector3.one;
                }
                else
                {
                    mesh = part.Source.GetComponent<MeshFilter>().sharedMesh;
                    scale = part.Source.transform.lossyScale;
                }
                if (part.Filter.sharedMesh != mesh) part.Filter.sharedMesh = mesh;
                if (part.Draw.sharedMaterial != material)
                {
                    var mats = new Material[mesh.subMeshCount];
                    for (int m = 0; m < mats.Length; m++) mats[m] = material;
                    part.Draw.sharedMaterials = mats;
                }
                // Scaled about the body's middle and moved out toward the doll: T(centre) S(k) T(-middle) on the source's pose.
                var t = part.Go.transform;
                t.SetPositionAndRotation(centre + (part.Source.transform.position - _ghostCentre) * _ghostScale, part.Source.transform.rotation);
                t.localScale = scale * _ghostScale;
                part.Block.SetColor("_Color", Hue);
                part.Block.SetVector("_Pull", pull);
                part.Block.SetFloat("_Stretch", stretch);
                part.Block.SetVector("_Origin", centre);
                part.Block.SetFloat("_Alpha", alpha);
                part.Block.SetFloat("_Dissolve", dissolve);
                part.Draw.SetPropertyBlock(part.Block);
            }
        }

        /// <summary>A point on the ghost's surface, in the world, where it is drawn this frame (a mote tears off there).</summary>
        private bool GhostPoint(out Vector3 at)
        {
            at = Vector3.zero;
            if (_ghost.Count == 0) return false;
            var part = _ghost[Random.Range(0, _ghost.Count)];
            if (part.Source == null) return false;
            Mesh mesh; Matrix4x4 local;
            if (part.Baked != null)
            {
                mesh = part.Baked;
                local = Matrix4x4.TRS(part.Source.transform.position, part.Source.transform.rotation, Vector3.one);
            }
            else
            {
                mesh = part.Source.GetComponent<MeshFilter>().sharedMesh;
                local = part.Source.localToWorldMatrix;
            }
            if (mesh == null || mesh.vertexCount == 0 || !mesh.isReadable && part.Baked == null) return false;
            _vertexScratch.Clear();
            mesh.GetVertices(_vertexScratch);
            if (_vertexScratch.Count == 0) return false;
            Vector3 p = local.MultiplyPoint3x4(_vertexScratch[Random.Range(0, _vertexScratch.Count)]);
            Vector3 centre = _ghostCentre + _ghostOffset;
            at = centre + (p - _ghostCentre) * _ghostScale;
            return true;
        }

        // ------------------------------------------------------------------ the motes

        /// <summary>
        /// A mote torn off the ghost (everyone else's screen) or, on the VICTIM'S OWN screen, off their own chest just under their view.
        /// DRAIN tears mostly from the chest down, HEX from the head.
        /// </summary>
        private Mote Tear(CharacterMotor victim, bool lens, bool fast)
        {
            var m = new Mote { Seed = Random.value, Lens = lens, Strand = Random.Range(0, 3) };
            bool drain = _kind == VoodooMarkKind.Drain;
            if (lens)
            {
                // x across, y up, z ahead in the lens frame.
                m.Offset = new Vector3(Random.Range(-0.5f, 0.5f), drain ? Random.Range(-0.9f, -0.6f) : Random.Range(-0.66f, -0.42f), Random.Range(0.3f, 0.6f));
                m.Size = Random.Range(0.05f, 0.1f);
            }
            else
            {
                Vector3 at;
                bool ok = GhostPoint(out at);
                // Bias to the part of them each curse takes: a second try if the first landed outside it.
                float head = victim.transform.position.y + 1.0f;
                if (ok && (drain ? at.y > head + 0.2f : at.y < head - 0.1f)) ok = GhostPoint(out at);
                if (!ok) at = victim.transform.position + Vector3.up * (drain ? 0.7f : 1.4f);
                m.Offset = at - (_ghostCentre + _ghostOffset);
                m.Size = Random.Range(0.1f, 0.2f);
            }
            m.Rate = fast ? Random.Range(2.6f, 3.4f) : Random.Range(1.3f, 1.9f);
            if (fast) m.S = 0.2f;
            m.Pos = Origin(m, lens);
            return m;
        }

        private Vector3 Origin(in Mote m, bool lens)
        {
            if (m.Lens)
            {
                var view = Camera.main;
                if (lens && view != null)
                    return view.transform.position + view.transform.right * m.Offset.x + view.transform.up * m.Offset.y + view.transform.forward * m.Offset.z;
                return _ghostCentre;
            }
            return _ghostCentre + _ghostOffset + m.Offset;
        }

        private bool StepMote(ref Mote m, float dt, CharacterMotor victim, bool lens, Vector3 intake)
        {
            m.Age += dt;
            Vector3 was = m.Pos;
            if (m.Released)
            {
                // The pull is gone: it slows, drifts up and thins away.
                m.Vel = Vector3.Lerp(m.Vel, Vector3.up * 0.4f, 1f - Mathf.Exp(-dt * 5f));
                m.Pos += m.Vel * dt;
                return m.Age < ReleaseSeconds;
            }
            // Sucked: it leaves steadily and quickens all the way in.
            m.S += dt * m.Rate * (0.5f + 2.2f * m.S * m.S);
            if (m.S >= 1f) return false;
            Vector3 from = Origin(m, lens);
            Vector3 span = intake - from;
            Vector3 dir = span.sqrMagnitude > 1e-4f ? span.normalized : transform.forward;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = transform.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(side, dir).normalized;
            float s = m.S;
            // Off the ghost a little way first (it is torn off, not slid), then straight for the doll.
            Vector3 lift = (from - (_ghostCentre + _ghostOffset));
            lift = lift.sqrMagnitude > 1e-4f ? lift.normalized * 0.25f : Vector3.zero;
            Vector3 bend = from + lift + span * 0.35f + Vector3.up * 0.15f;
            float u = 1f - s;
            Vector3 p = u * u * from + 2f * u * s * bend + s * s * intake;
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(s)) * (1f - s * 0.6f);
            float phase = m.Strand * (Mathf.PI * 2f / 3f);
            if (_kind == VoodooMarkKind.Drain)
            {
                // DRAIN's three currents wind round each other on the way in (plan 9.3: DRAIN is a TWIST).
                float a = phase + s * 12f - Time.time * 3f;
                p += (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * 0.2f * envelope;
            }
            else
            {
                // HEX's waver in side by side, like breath drawn through a gap.
                p += (side * (Mathf.Sin(phase + s * 7f - Time.time * 4f) * 0.14f + (m.Strand - 1) * 0.08f)
                      + up * Mathf.Cos(phase * 1.7f + s * 5f) * 0.08f) * envelope;
            }
            p += side * ((m.Seed - 0.5f) * 0.12f * envelope);
            m.Pos = p;
            m.Vel = dt > 1e-5f ? (p - was) / dt : Vector3.zero;
            return true;
        }

        /// <summary>Silent on purpose: every hero skill sound is deleted until they are reworked (`AudioCues.IsSkillSfx`).</summary>
        private void EndReach(bool succeeded)
        {
            var victim = Victim(out bool lens);
            Vector3 intake = Intake();
            if (succeeded)
            {
                // The gulp: the ghost is yanked in, everything in flight rushes after it, and a last handful is ripped off them.
                if (_ghostState == GhostState.Pull) { _ghostState = GhostState.Yank; _ghostAge = 0f; }
                for (int i = 0; i < _motes.Count; i++) { var m = _motes[i]; m.Rate *= 3.5f; _motes[i] = m; }
                if (victim != null) for (int i = 0; i < 30 && _motes.Count < MaxMotes; i++) _motes.Add(Tear(victim, lens, fast: true));
                _flash = 1f;
            }
            else
            {
                if (_ghostState == GhostState.Pull) { _ghostState = GhostState.Snap; _ghostAge = 0f; }
                for (int i = 0; i < _motes.Count; i++) { var m = _motes[i]; m.Released = true; m.Age = 0f; m.Vel *= 0.25f; _motes[i] = m; }
            }
        }

        private static ParticleSystem.Particle Particle(Vector3 at, Vector3 velocity, float size, Color colour, float seed)
        {
            return new ParticleSystem.Particle
            {
                position = at,
                velocity = velocity,
                startSize = size,
                startColor = colour,
                remainingLifetime = 1f,
                startLifetime = 1f,
                randomSeed = (uint)(seed * 100000f),
            };
        }

        private void Ensure()
        {
            if (_ps != null) return;
            var go = new GameObject("VoodooSoulDraw");
            _ps = go.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = MaxMotes + 1;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            var emission = _ps.emission; emission.enabled = false;
            var shape = _ps.shape; shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.012f;
            r.lengthScale = 1.0f;
            r.sortMode = ParticleSystemSortMode.Distance;
            r.sharedMaterial = MoteMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.minParticleSize = 0f;
            r.maxParticleSize = 2f;
            _ps.Play();
        }

        private void OnDisable()
        {
            LetDoll();
            _motes.Clear();
            ClearGhost();
            _wasReaching = false;
            _flash = 0f;
            if (_ps != null) _ps.Clear();
        }

        private void OnDestroy()
        {
            LetDoll();
            ClearGhost();
            if (_ps != null) Destroy(_ps.gameObject);
        }
    }
}
