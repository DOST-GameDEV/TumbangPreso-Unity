using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE REACH IS A SOUL BEING SUCKED INTO HER DOLL, NOT A CORD (HERO-10 v3, 2026-09-29). The owner on films v12 to v16, where
    /// the reach was a thread from her palm to them: *"dont make the pulling thing look like a physical line i want it to look like
    /// sucking aura or smth"*, *"it sucks rn ur implementation"*, and *"i want u to make her hold up her voodoo too towards the person
    /// when markingt hem"*. His table already said it: *"attach their soul to the voodoo doll"*.
    ///
    /// | Moment | What | Direction |
    /// |---|---|---|
    /// | the lock | her doll comes up in her hand, held out at them; their aura lights up round their body | the doll toward them |
    /// | the hold (2 s) | wisps of their aura peel off their body (DRAIN from the chest, HEX from the head), linger, then are SUCKED along a curve into the doll, faster and thinner as they near it; more and more as it fills. DRAIN's stream twists (its shape is a twist), HEX's wavers straight in. A swirl of light glows at the doll, growing | from them INTO the doll |
    /// | the mark | a gulp: everything in flight rushes in, a last burst is ripped off them, the doll flares | into the doll |
    /// | broken | the pull lets go: the wisps slow, drift up and thin away | nowhere |
    ///
    /// Body-owned like `VoodooCursePresenter` (which adds it) and read off the body's REPLICATED reach (`CharacterMotor.Voodoo.cs`),
    /// so every screen draws the same thing and a rejoiner sees a reach already running. There is no line anywhere: every piece is a
    /// separate wisp (`Shaders/VoodooWisp`), so it reads as something drawn out of them, never as a rope.
    ///
    /// ⚠️ THE DOLL IS IN HER HANDS EXACTLY WHILE THE SLIPPER IS AT HER BELT (`CharacterMotor.StowsCarriedSlipper`: the reach, DRAIN's
    /// wring and HEX's stab), which is the one moment her hands need to be free for it. It is the ultimate's doll at hand size
    /// (`PhaisterDollArt`, plan 9.10: *"The small doll at her hip will be the same design at hand size"*), with its light in its
    /// openings, so the soul visibly goes INTO it. Everyone else sees it in her body's left hand; on her own screen her body is hidden
    /// and a copy rides her first-person left hand (`ViewmodelArms.HoldingProp` lifts it into view).
    /// </summary>
    public sealed class VoodooSoulDraw : MonoBehaviour
    {
        /// <summary>The held doll's height, metres: on her body (the cast's scale), and in her first-person hand.</summary>
        private const float BodyDollHeight = 0.58f, ViewDollHeight = 0.30f;

        /// <summary>How far past her palm, toward them, the held doll sits (v18: at the palm it sat by her cheek and read as hugged).</summary>
        private const float DollOut = 0.42f;

        /// <summary>Wisps a second at the start of the hold and when it is full.</summary>
        private const float EmitStart = 50f, EmitFull = 120f;

        private const int MaxWisps = 420, AuraGlows = 9;
        private const float GulpSeconds = 0.28f, ReleaseSeconds = 0.5f;

        private struct Wisp
        {
            public Vector3 Offset;     // from the victim's feet (world axes), or in the victim's LENS frame when Lens
            public bool Lens;
            public float S, Rate, Seed, Size, Age, Fade;
            public Vector3 Pos, Vel;
            public byte Mode;          // 0 drawn in, 1 released, 2 the glow at the doll
        }

        private CharacterMotor _body;
        private ParticleSystem _ps;
        private ParticleSystem.Particle[] _out;
        private readonly List<Wisp> _wisps = new List<Wisp>(MaxWisps);
        private int _victim = -1;
        private VoodooMarkKind _kind;
        private bool _wasReaching;
        private float _emitCarry;
        private float _flash;          // the doll's flare at the mark, 1 falling to 0
        private Vector3 _lastVictimAt;
        private bool _lastVictimLens;

        // The doll in her hands.
        private GameObject _bodyDoll, _viewDoll;
        private Transform _leftArm;
        private Vector3 _leftPalm;
        private ViewmodelArms _arms;
        private float _dollAge;

        private static Material _material;

        private static Material WispMaterial
        {
            get
            {
                if (_material != null) return _material;
                var shader = Resources.Load<Shader>("Shaders/VoodooWisp");
                if (shader == null) return null;
                _material = new Material(shader) { name = "VoodooWisp" };
                return _material;
            }
        }

        private void Awake() => _body = GetComponent<CharacterMotor>();

        private void LateUpdate()
        {
            if (_body == null) return;
            float dt = Time.deltaTime;
            StepDoll(dt);
            StepDraw(dt);
        }

        // ------------------------------------------------------------------ the doll in her hands

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
                // Upright in her left hand, its face turned to whoever she holds it at (she shows them who it is).
                Vector3 palm = _leftArm.TransformPoint(_leftPalm);
                Vector3 toward = at - palm; toward.y = 0f;
                if (toward.sqrMagnitude < 0.01f) toward = transform.forward;
                _bodyDoll.transform.position = palm + Vector3.up * (BodyDollHeight * 0.1f) + toward.normalized * DollOut;
                _bodyDoll.transform.rotation = Quaternion.LookRotation(toward.normalized, Vector3.up) * Quaternion.Euler(-6f + 14f * twitch, 0f, 9f * twitch);
            }
            if (_viewDoll != null)
            {
                var view = Camera.main;
                if (view != null)
                {
                    // On her own screen it faces AWAY from her, at them: she is holding it up at them, and she sees its back and
                    // its lit seams, a little turned so its button eye shows.
                    Vector3 fwd = view.transform.forward;
                    _viewDoll.transform.rotation = Quaternion.LookRotation(fwd, view.transform.up) * Quaternion.Euler(8f * twitch, 155f, 6f * twitch);
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
                _bodyDoll = MakeDoll(art, null, BodyDollHeight, "VoodooHeldDoll");
                // Her own lens is inside her body: that doll is for everyone else.
                if (_bodyDoll != null && mine) _bodyDoll.AddComponent<HiddenFromMainCamera>();
            }
            if (!mine) return;
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>())
            {
                if (arms == null || arms.BoundCharacter != _body) continue;
                var left = arms.LeftHandForProps();
                if (left == null) break;
                _viewDoll = MakeDoll(art, left, ViewDollHeight, "VoodooHeldDollFirstPerson");
                if (_viewDoll != null)
                {
                    _viewDoll.transform.localPosition = arms.LeftPalmOffset() + new Vector3(0f, ViewDollHeight * 0.45f, 0f);
                    foreach (var t in _viewDoll.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = left.gameObject.layer;
                }
                arms.HoldingProp = true;
                _arms = arms;
                break;
            }
        }

        /// <summary>The ultimate's doll at <paramref name="height"/> metres, its light painted in its openings, no animator (it hangs limp).</summary>
        private static GameObject MakeDoll(RosterEntryAsset art, Transform parent, float height, string name)
        {
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
            float parentScale = parent != null ? Mathf.Max(0.0001f, parent.lossyScale.y) : 1f;
            go.transform.localScale = Vector3.one * (height / tall / parentScale);
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
        /// everyone else's), or her left hand if the doll is missing.
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

        // ------------------------------------------------------------------ the soul, drawn out of them

        private CharacterMotor Victim(out bool lens)
        {
            lens = false;
            int seat = _body.IsVoodooReaching ? _body.VoodooReachTarget : _victim;
            if (seat < 0 || GameServices.Round == null) return null;
            var v = GameServices.Round.PlayerAt(seat);
            lens = v != null && ViewmodelArms.IsFirstPersonFor(v);
            return v;
        }

        private void StepDraw(float dt)
        {
            bool reaching = _body.IsVoodooReaching && _body.isActiveAndEnabled;
            if (reaching && (!_wasReaching || _victim != _body.VoodooReachTarget || _kind != _body.VoodooReachKind))
            {
                _victim = _body.VoodooReachTarget; _kind = _body.VoodooReachKind;
                _emitCarry = 0f;
            }
            if (!reaching && _wasReaching) EndReach(_body.VoodooReachSucceeded);
            _wasReaching = reaching;

            var victim = Victim(out bool lens);
            if (victim != null) { _lastVictimAt = victim.transform.position; _lastVictimLens = lens; }
            Vector3 intake = Intake();

            if (reaching && victim != null)
            {
                float fill = _body.VoodooReachProgress;
                _emitCarry += dt * Mathf.Lerp(EmitStart, EmitFull, fill * fill);
                while (_emitCarry >= 1f && _wisps.Count < MaxWisps) { _emitCarry -= 1f; _wisps.Add(Peel(victim, lens, intake, fast: false)); }
                _emitCarry = Mathf.Min(_emitCarry, 1f);
            }
            _flash = Mathf.Max(0f, _flash - dt / GulpSeconds);

            if (_wisps.Count == 0 && !reaching && _flash <= 0f) { if (_ps != null) _ps.Clear(); return; }
            Ensure();

            Color hue = _kind == VoodooMarkKind.Drain ? VoodooCursePresenter.DrainHue : VoodooCursePresenter.HexHue;
            int count = 0;
            if (_out == null || _out.Length < MaxWisps + AuraGlows + 1) _out = new ParticleSystem.Particle[MaxWisps + AuraGlows + 1];
            for (int i = _wisps.Count - 1; i >= 0; i--)
            {
                var w = _wisps[i];
                if (!StepWisp(ref w, dt, victim, lens, intake)) { _wisps.RemoveAt(i); continue; }
                _wisps[i] = w;
            }
            foreach (var w in _wisps)
            {
                float fadeIn = Mathf.Clamp01(w.Age / 0.18f);
                float fadeOut = w.Mode == 1 ? 1f - Mathf.Clamp01(w.Age / ReleaseSeconds) : Mathf.Clamp01((1f - w.S) / 0.1f);
                float life = Mathf.Clamp01(Mathf.Min(fadeIn, fadeOut) * w.Fade) * 0.8f;
                float size = w.Mode == 1 ? w.Size * (1f + w.Age) : Mathf.Lerp(w.Size, w.Size * 0.18f, Mathf.Pow(Mathf.Clamp01(w.S), 1.4f));
                // Close to the doll the light gets hotter: it is going in.
                Color c = Color.Lerp(hue, Color.Lerp(hue, Color.white, 0.45f), Mathf.Clamp01((w.S - 0.6f) / 0.4f));
                c.a = life;
                _out[count++] = Particle(w.Pos, w.Vel, size, c, w.Seed);
            }
            // THEIR AURA (v18: v17 had wisps and nothing round the body they came from): a few big soft glows clinging round
            // them, breathing with the heartbeat and leaning toward her, brighter as it fills. Not on their own screen, where their
            // body is the lens.
            if (reaching && victim != null && !lens)
            {
                float fill = _body.VoodooReachProgress;
                Vector3 toward = intake - victim.transform.position; toward.y = 0f;
                toward = toward.sqrMagnitude > 0.01f ? toward.normalized : victim.transform.forward;
                float beat = Mathf.Repeat(Time.time * Mathf.Lerp(1.2f, 2.6f, fill), 1f);
                float thump = beat < 0.16f ? Mathf.Sin(beat / 0.16f * Mathf.PI) : 0f;
                for (int k = 0; k < AuraGlows && count < _out.Length; k++)
                {
                    float a = k / (float)AuraGlows * Mathf.PI * 2f + Time.time * 0.9f;
                    Vector3 round = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.34f;
                    float h = _kind == VoodooMarkKind.Drain ? 0.55f + 0.35f * Mathf.Sin(a * 2f) : 1.25f + 0.3f * Mathf.Sin(a * 2f);
                    Vector3 at = victim.transform.position + round + Vector3.up * h + toward * (0.12f + 0.25f * fill);
                    Color g = hue; g.a = (0.34f + 0.3f * fill) * (0.8f + 0.4f * thump);
                    _out[count++] = Particle(at, toward * 0.6f, 0.95f + 0.35f * fill + 0.2f * thump, g, k * 0.13f);
                }
            }
            // The swirl at the doll: it glows while it fills and flares at the mark.
            if (reaching || _flash > 0f)
            {
                float fill = reaching ? _body.VoodooReachProgress : 1f;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * Mathf.Lerp(7f, 15f, fill));
                bool view = _viewDoll != null;
                float size = (view ? 0.12f : 0.34f) * (0.7f + 0.6f * fill + 0.2f * pulse) + (view ? 0.3f : 0.9f) * _flash;
                Color c = Color.Lerp(hue, Color.white, 0.3f + 0.4f * _flash);
                c.a = Mathf.Clamp01(0.45f + 0.35f * fill + _flash);
                _out[count++] = Particle(intake, Vector3.zero, size, c, 0.37f);
            }
            _ps.SetParticles(_out, count);
        }

        /// <summary>
        /// A wisp peeled off the victim. On everyone else's screen it leaves their BODY, from all round it but mostly the side facing
        /// her (DRAIN from the chest, HEX from the head). On the VICTIM'S OWN screen their body is hidden and their eyes are the lens,
        /// so it leaves from just under and beside their view (their own chest) and is seen streaming AWAY from them toward her.
        /// </summary>
        private Wisp Peel(CharacterMotor victim, bool lens, Vector3 intake, bool fast)
        {
            var w = new Wisp { Seed = Random.value, Fade = 1f, Mode = 0, Lens = lens };
            bool drain = _kind == VoodooMarkKind.Drain;
            if (lens)
            {
                // x across, y up, z ahead in the lens frame.
                w.Offset = new Vector3(Random.Range(-0.55f, 0.55f), drain ? Random.Range(-0.95f, -0.6f) : Random.Range(-0.7f, -0.42f), Random.Range(0.25f, 0.6f));
                w.Size = Random.Range(0.2f, 0.34f);
            }
            else
            {
                Vector3 toward = intake - victim.transform.position; toward.y = 0f;
                toward = toward.sqrMagnitude > 0.01f ? toward.normalized : victim.transform.forward;
                // Round the body, weighted to her side: a cosine lobe, so the aura visibly leans toward her.
                float angle = Random.Range(-1f, 1f); angle = angle * Mathf.Abs(angle) * 150f;
                Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * toward;
                float height = drain ? Random.Range(0.25f, 1.15f) : Random.Range(0.95f, 1.85f);
                if (Random.value < 0.22f) height = Random.Range(0.1f, 1.8f); // a few from anywhere on them: the whole aura goes
                w.Offset = dir * Random.Range(0.32f, 0.55f) + Vector3.up * height;
                w.Size = Random.Range(0.36f, 0.62f);
            }
            // Most linger by the body a moment before the pull takes them (the aura); some are torn straight off.
            // v19 (film v18: with many lingering, the hold read as scattered petals; the gulp, one quick flow, read right): most
            // flow steadily, a few cling a moment first.
            w.Rate = fast ? Random.Range(2.2f, 3.0f) : Random.value < 0.18f ? Random.Range(0.6f, 0.8f) : Random.Range(1.15f, 1.5f);
            if (fast) w.S = 0.25f;
            w.Pos = Origin(w, victim, lens);
            return w;
        }

        private Vector3 Origin(in Wisp w, CharacterMotor victim, bool lens)
        {
            if (w.Lens)
            {
                var view = Camera.main;
                if (lens && view != null)
                    return view.transform.position + view.transform.right * w.Offset.x + view.transform.up * w.Offset.y + view.transform.forward * w.Offset.z;
                return _lastVictimAt + Vector3.up * 1.2f;
            }
            return (victim != null ? victim.transform.position : _lastVictimAt) + w.Offset;
        }

        private bool StepWisp(ref Wisp w, float dt, CharacterMotor victim, bool lens, Vector3 intake)
        {
            w.Age += dt;
            Vector3 was = w.Pos;
            if (w.Mode == 1)
            {
                // Released: the pull is gone; it slows, drifts up and thins away.
                w.Vel = Vector3.Lerp(w.Vel, Vector3.up * 0.35f, 1f - Mathf.Exp(-dt * 4f));
                w.Pos += w.Vel * dt;
                return w.Age < ReleaseSeconds;
            }
            // Sucked: slow at first (it clings to them), then faster and faster into the doll.
            w.S += dt * w.Rate * (0.42f + 3.4f * w.S * w.S);
            if (w.S >= 1f) return false;
            Vector3 from = Origin(w, victim, lens);
            Vector3 span = intake - from;
            Vector3 dir = span.sqrMagnitude > 1e-4f ? span.normalized : transform.forward;
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            if (side.sqrMagnitude < 1e-4f) side = transform.right;
            side.Normalize();
            Vector3 up = Vector3.Cross(side, dir).normalized;
            // Off their body first (outward and up), then curving into the doll: a funnel, wide at them, a point at the doll.
            Vector3 outward = from - (victim != null ? victim.transform.position : _lastVictimAt);
            outward.y = 0f;
            outward = outward.sqrMagnitude > 1e-4f ? outward.normalized : -dir;
            Vector3 bend = from + outward * 0.45f + Vector3.up * 0.3f + span * 0.25f;
            float s = w.S, u = 1f - s;
            Vector3 p = u * u * from + 2f * u * s * bend + s * s * intake;
            float envelope = Mathf.Sin(Mathf.PI * Mathf.Clamp01(s)) * (1f - s);
            float spin = w.Seed * Mathf.PI * 2f;
            if (_kind == VoodooMarkKind.Drain)
            {
                // DRAIN twists: the stream winds round its own axis as it goes (plan 9.3, DRAIN is a TWIST).
                float a = spin + s * 11f + Time.time * 2.5f;
                p += (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * 0.26f * envelope;
            }
            else
            {
                // HEX wavers in straight, like breath drawn through a gap.
                p += (side * Mathf.Sin(spin + s * 6f - Time.time * 4f) * 0.22f + up * Mathf.Cos(spin * 1.7f + s * 4f) * 0.12f) * envelope;
            }
            w.Pos = p;
            w.Vel = dt > 1e-5f ? (p - was) / dt : Vector3.zero;
            return true;
        }

        private void EndReach(bool succeeded)
        {
            var victim = Victim(out bool lens);
            Vector3 intake = Intake();
            if (succeeded)
            {
                // The gulp: everything in flight rushes in, and a last handful is ripped off them.
                for (int i = 0; i < _wisps.Count; i++) { var w = _wisps[i]; w.Rate *= 4.5f; w.S = Mathf.Max(w.S, 0.35f); _wisps[i] = w; }
                if (victim != null) for (int i = 0; i < 22 && _wisps.Count < MaxWisps; i++) _wisps.Add(Peel(victim, lens, intake, fast: true));
                _flash = 1f;
                GameServices.Audio?.PlayAt("sfx_phaister_mark", victim != null ? victim.transform.position : _lastVictimAt);
            }
            else
            {
                for (int i = 0; i < _wisps.Count; i++) { var w = _wisps[i]; w.Mode = 1; w.Age = 0f; w.Vel *= 0.25f; _wisps[i] = w; }
                GameServices.Audio?.PlayAt("sfx_phaister_reach_snap", intake);
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
            go.transform.SetParent(null, false);
            _ps = go.AddComponent<ParticleSystem>();
            _ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.maxParticles = MaxWisps + AuraGlows + 1;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;
            var emission = _ps.emission; emission.enabled = false;
            var shape = _ps.shape; shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.06f;
            r.lengthScale = 1.6f;
            r.sortMode = ParticleSystemSortMode.Distance;
            r.sharedMaterial = WispMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.minParticleSize = 0f;
            r.maxParticleSize = 2f;
            _ps.Play();
        }

        private void OnDisable()
        {
            LetDoll();
            _wisps.Clear();
            _wasReaching = false;
            _flash = 0f;
            if (_ps != null) _ps.Clear();
        }

        private void OnDestroy()
        {
            LetDoll();
            if (_ps != null) Destroy(_ps.gameObject);
        }
    }
}
