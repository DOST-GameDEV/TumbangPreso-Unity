using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ MANIKA MISCHIEF, SEEN (HERO-10, plan 4.2 and 4.5). The doll is the gameplay object `Abilities.VoodooDoll` (host
    /// resolves the hit by distance); this is everything a player SEES of it, on every peer:
    ///
    /// | State | What moves |
    /// |---|---|
    /// | Flying | the modelled manika tumbles end over end on the doll's arc, a violet hex-smoke trail curling behind it |
    /// | Stealing (0.25 s) | on the victim: it slaps onto them and their colour drains INTO it, a swirl turning clockwise |
    /// | Returning (0.35 s) | it flies back to HER LEFT HAND on a low arc, now wearing the victim's colour |
    /// | Held (4 s, the Disoriented time) | in her left hand she turns its head slowly back and forth (third person on her body, first person in her own viewmodel hand, owner: *"show it FPP and TPP ... i want ppl to see and hher to see that shees using it"*) |
    /// | Held, on the VICTIM's screen | v8 (plan 4.2 row 8): a doll of THEMSELVES peeks in at the lower edge of their frame in glimpses, its head being twisted in time with hers, pins in it (`VictimGlimpse`) |
    /// | Crumbling (0.5 s) | it pops its stitches and falls away as ash |
    /// | A miss | it lands, sits up, looks left and right, then crumbles |
    ///
    /// ⚠️ THE STEAL IS TRIGGERED BY THE STATUS, NOT BY A MESSAGE. Only the host decides who was hit; every peer sees that
    /// player's Disoriented timer arrive in `SyncUnit`, and `PhaisterStatusPresenter` hands the nearest waiting doll to them.
    /// A doll that hears nothing within `MissWait` of landing plays the miss. No new wire traffic.
    /// </summary>
    public sealed class PhaisterManika : MonoBehaviour
    {
        private enum State { Flying, Waiting, Stealing, Returning, Held, Crumbling, Missed }

        public const float MissWait = 0.40f, StealSeconds = 0.25f, ReturnSeconds = 0.35f, CrumbleSeconds = 0.50f, MissSeconds = 1.0f;
        public const float FlyScale = 1.9f, HeldScale = 0.9f;   // v2: at 1.4 the flying doll was a 0.4 m speck at court distance

        private static readonly List<PhaisterManika> Live = new List<PhaisterManika>();

        private State _state;
        private float _stateAge;
        private int _owner;
        private GameObject _model, _fppCopy, _victimCopy;
        private Transform _head, _fppHead, _victimHead;
        private CharacterMotor _victim;
        private Vector3 _from;
        private readonly List<Transform> _trail = new List<Transform>();
        private int _trailNext;

        public int OwnerSlot => _owner;
        public bool Waiting => _state == State.Flying || _state == State.Waiting;

        public static PhaisterManika Spawn(int ownerSlot)
        {
            var go = new GameObject("PhaisterManika");
            var m = go.AddComponent<PhaisterManika>();
            m._owner = ownerSlot;
            m._model = PhaisterProp.Spawn("manika", go.transform, null, PhaisterProp.InsectOutlineWidth * 1.3f);
            if (m._model != null) m._model.transform.localScale = Vector3.one * FlyScale;
            m._head = m._model != null ? PhaisterProp.Find(m._model, "head") : null;
            // The hex-smoke trail: eight puffs typed as sizes, reused round-robin as it flies.
            foreach (float size in new[] { 0.16f, 0.13f, 0.15f, 0.11f, 0.14f, 0.10f, 0.12f, 0.09f })
            {
                var puff = VfxShapes.Lay(null, "HexSmoke", VfxShapes.Splat(9, 0.3f, m._trail.Count + 3), size, 0f);
                VfxMaterial.Ghost(puff.GetComponent<Renderer>(), new Color(0.55f, 0.28f, 0.78f, 0f), 0.4f);
                puff.SetActive(false);
                m._trail.Add(puff.transform);
            }
            Live.Add(m);
            return m;
        }

        /// <summary>The nearest doll still flying or waiting to hear who it hit, within <paramref name="reach"/> of <paramref name="at"/>.</summary>
        public static PhaisterManika Nearest(Vector3 at, float reach)
        {
            PhaisterManika best = null; float bestD = reach;
            foreach (var m in Live)
            {
                if (m == null || !m.Waiting) continue;
                float d = Vector3.Distance(m.transform.position, at);
                if (d <= bestD) { best = m; bestD = d; }
            }
            return best;
        }

        /// <summary>Called by the gameplay doll every step while it flies.</summary>
        public void Follow(Vector3 position, Quaternion tumble)
        {
            if (_state != State.Flying) return;
            transform.position = position;
            if (_model != null) _model.transform.rotation = tumble;
        }

        /// <summary>The gameplay doll has landed or struck: wait a moment for the status to say who it took.</summary>
        public void Landed(Vector3 at)
        {
            if (_state != State.Flying) return;
            transform.position = at;
            Enter(State.Waiting);
        }

        /// <summary>The victim's Disoriented timer arrived: steal their look and fly home.</summary>
        public void Steal(CharacterMotor victim)
        {
            if (!Waiting || victim == null) return;
            _victim = victim;
            _from = transform.position;
            Enter(State.Stealing);
            var dress = PhaisterProp.ClothedIn(VictimColour(victim));
            if (_model != null) ToonSkin.Apply(_model, PhaisterProp.InsectOutlineWidth * 1.3f, dress);
            GameServices.Audio?.PlayAt("sfx_phaister_manika_steal", victim.transform.position);
        }

        private void Enter(State s) { _state = s; _stateAge = 0f; }

        private void OnDestroy()
        {
            Live.Remove(this);
            foreach (var p in _trail) if (p != null) Destroy(p.gameObject);
            if (_fppCopy != null) Destroy(_fppCopy);
            if (_victimCopy != null) Destroy(_victimCopy);
            if (_arms != null) _arms.HoldingProp = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _stateAge += dt;
            StepTrail(dt);
            switch (_state)
            {
                case State.Waiting:
                    if (_stateAge >= MissWait) Enter(State.Missed);
                    break;
                case State.Stealing:
                {
                    // Slapped onto the victim's chest; a clockwise swirl of their colour drains into it (the swirl is the
                    // doll turning a full turn on the spot while it pulses up to size).
                    // ⚠️ v3 (film v8): on the victim's OWN screen the doll at their chest, pulsing to 1.35 of its flying size,
                    // swallowed their lens: a whole frame of flat mustard. It slaps 0.45 m in front of their chest now (the side it
                    // came from), pulses less, and on their own view shrinks away inside a metre of their eye.
                    Vector3 chest = _victim != null ? _victim.transform.position + Vector3.up * 1.0f : _from;
                    Vector3 facing = _from - chest; facing.y = 0f;
                    if (facing.sqrMagnitude > 0.01f) chest += facing.normalized * 0.45f;
                    float u = Mathf.Clamp01(_stateAge / StealSeconds);
                    transform.position = Vector3.Lerp(_from, chest, Mathf.Clamp01(u * 3f));
                    if (_model != null)
                    {
                        _model.transform.rotation = Quaternion.Euler(0f, -360f * u, 0f);
                        float nearEye = 1f;
                        var rig = FindFirstObjectByType<CameraRig>();
                        var cam = UnityEngine.Camera.main;
                        if (rig != null && cam != null && rig.Following == _victim)
                            nearEye = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1.1f, Vector3.Distance(transform.position, cam.transform.position)));
                        _model.transform.localScale = Vector3.one * Mathf.Max(0.0001f, FlyScale * (1f + 0.2f * Mathf.Sin(u * Mathf.PI)) * nearEye);
                    }
                    if (u >= 1f) { _from = transform.position; Enter(State.Returning); }
                    break;
                }
                case State.Returning:
                {
                    // Home on a low arc to her left palm, shrinking to the size she holds it at.
                    var hand = LeftPalm();
                    Vector3 to = hand != null ? hand.position : _from;
                    float u = Mathf.Clamp01(_stateAge / ReturnSeconds);
                    float e = 1f - (1f - u) * (1f - u);
                    transform.position = Vector3.Lerp(_from, to, e) + Vector3.up * Mathf.Sin(u * Mathf.PI) * 0.6f;
                    if (_model != null)
                    {
                        _model.transform.rotation = Quaternion.Euler(0f, -360f * u, 20f * Mathf.Sin(u * 6f));
                        _model.transform.localScale = Vector3.one * Mathf.Lerp(FlyScale, HeldScale, e);
                    }
                    if (u >= 1f) { Enter(State.Held); AttachFirstPerson(); AttachVictimGlimpse(); }
                    break;
                }
                case State.Held:
                    Hold();
                    if (_stateAge > 0.5f && (_victim == null || !_victim.IsDisoriented) || _stateAge > Core.StatusRules.DisorientedSeconds + 0.5f)
                    {
                        Enter(State.Crumbling);
                        GameServices.Audio?.PlayAt("sfx_phaister_manika_crumble", transform.position);
                    }
                    break;
                case State.Crumbling:
                {
                    Hold();
                    float u = Mathf.Clamp01(_stateAge / CrumbleSeconds);
                    Crumble(_model, u);
                    Crumble(_fppCopy, u);
                    Crumble(_victimCopy, u);
                    if (u >= 1f) Destroy(gameObject);
                    break;
                }
                case State.Missed:
                {
                    // It sits up, looks left and right, then crumbles where it fell.
                    float u = _stateAge / MissSeconds;
                    if (_model != null)
                    {
                        float sit = Mathf.Clamp01(_stateAge / 0.2f);
                        _model.transform.rotation = Quaternion.Slerp(_model.transform.rotation, Quaternion.identity, sit);
                        _model.transform.localScale = Vector3.one * FlyScale;
                    }
                    if (_head != null) _head.localRotation = Quaternion.Euler(0f, 45f * Mathf.Sin(Mathf.Clamp01((_stateAge - 0.2f) / 0.5f) * Mathf.PI * 2f), 0f);
                    if (u > 0.5f) Crumble(_model, (u - 0.5f) * 2f);
                    if (u >= 1f) Destroy(gameObject);
                    break;
                }
            }
        }

        /// <summary>In her left palm: the head turning slowly back and forth, now and then a sharp twist (a prick).</summary>
        private void Hold()
        {
            var hand = LeftPalm();
            if (hand != null)
            {
                transform.position = hand.position;
                if (_model != null) _model.transform.rotation = hand.rotation * Quaternion.Euler(0f, 180f, 0f);
            }
            float twist = 35f * Mathf.Sin(_stateAge * 2.2f) + (Mathf.Repeat(_stateAge, 1.3f) < 0.08f ? 25f : 0f);
            if (_head != null) _head.localRotation = Quaternion.Euler(0f, twist, 0f);
            if (_fppHead != null) _fppHead.localRotation = Quaternion.Euler(0f, twist, 0f);
            var cam = UnityEngine.Camera.main;
            if (_fppCopy != null && cam != null)
                _fppCopy.transform.rotation = Quaternion.LookRotation(cam.transform.position - _fppCopy.transform.position, cam.transform.up);
            VictimGlimpse(cam, twist);
        }

        /// <summary>
        /// ⚠️ THE VICTIM'S SIDE (HERO-10, plan 4.2 row 8: "a glimpse of a doll of themselves at the edge"). On the screen of the player
        /// the doll took, a copy of it (in THEIR colours: it is them) peeks up from the lower right corner of the frame in glimpses
        /// (in 0.2 s, held about 0.7 s, out 0.2 s, every 1.6 s), its head turning with the twist of her thumb and jerking at each
        /// prick. It is 0.6 m in front of their lens and drawn only by their own camera (`MainCameraOnly`).
        /// </summary>
        private void AttachVictimGlimpse()
        {
            if (_model == null || _victim == null) return;
            var rig = FindFirstObjectByType<CameraRig>();
            if (rig == null || rig.Following != _victim) return;
            _victimCopy = Instantiate(_model);
            _victimCopy.name = "ManikaVictimGlimpse";
            _victimCopy.transform.localScale = Vector3.one * 0.85f;
            _victimHead = PhaisterProp.Find(_victimCopy, "head");
            // A second pin, going into it as she twists it: voodoo, not a toy (film v9 read as a toy on their screen).
            var pin = PhaisterProp.Spawn("hatpin", _victimCopy.transform, null, PhaisterProp.InsectOutlineWidth);
            if (pin != null)
            {
                pin.transform.localPosition = new Vector3(0.09f, 0.20f, 0.05f);
                pin.transform.localRotation = Quaternion.Euler(0f, 0f, 58f);
                pin.transform.localScale = Vector3.one * 0.5f;
            }
            _victimCopy.AddComponent<MainCameraOnly>();
        }

        private void VictimGlimpse(UnityEngine.Camera cam, float twist)
        {
            if (_victimCopy == null) return;
            if (cam == null) { _victimCopy.SetActive(false); return; }
            float cycle = Mathf.Repeat(_stateAge, 1.6f);
            float peek = cycle < 0.2f ? cycle / 0.2f : cycle < 0.9f ? 1f : cycle < 1.1f ? 1f - (cycle - 0.9f) / 0.2f : 0f;
            peek = peek * peek * (3f - 2f * peek);
            _victimCopy.SetActive(peek > 0.01f);
            if (peek <= 0.01f) return;
            const float Depth = 0.6f;
            float halfH = Depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad), halfW = halfH * cam.aspect;
            var ct = cam.transform;
            // From below the frame's lower right corner up into it, cut by the frame's edge: it is AT their screen, not on the court
            // (film v8: at 0.55 and 0.78 across it stood like a small doll on the road).
            Vector2 at = new Vector2(0.86f, Mathf.Lerp(-1.45f, -0.66f, peek));
            _victimCopy.transform.position = ct.position + ct.forward * Depth + ct.right * at.x * halfW + ct.up * at.y * halfH;
            _victimCopy.transform.rotation = Quaternion.LookRotation(ct.position - _victimCopy.transform.position, ct.up) * Quaternion.Euler(0f, 0f, -12f);
            if (_victimHead != null) _victimHead.localRotation = Quaternion.Euler(0f, twist, 0f);
        }

        private static void Crumble(GameObject model, float u)
        {
            if (model == null) return;
            var s = model.transform.localScale;
            float k = 1f - u;
            model.transform.localScale = new Vector3(s.x * (1f + 0.02f), Mathf.Max(0.0001f, s.y * k), s.z * (1f + 0.02f));
            if (u >= 1f) model.SetActive(false);
        }

        private void StepTrail(float dt)
        {
            bool flying = _state == State.Flying || _state == State.Returning;
            if (flying && _trail.Count > 0 && Mathf.Repeat(_stateAge, 0.04f) < dt)
            {
                var puff = _trail[_trailNext++ % _trail.Count];
                puff.gameObject.SetActive(true);
                puff.position = transform.position;
                puff.rotation = Quaternion.Euler(Random.Range(0f, 360f), Random.Range(0f, 360f), 0f);
                puff.localScale = Vector3.one * 0.12f;
                PhaisterProp.SetAlpha(puff.GetComponent<Renderer>().sharedMaterial, 0.55f);
            }
            foreach (var p in _trail)
            {
                if (p == null || !p.gameObject.activeSelf) continue;
                var m = p.GetComponent<Renderer>().sharedMaterial; var col = m.color;
                col.a = Mathf.Max(0f, col.a - dt * 1.8f); PhaisterProp.SetAlpha(m, col.a);
                p.localScale *= 1f + dt * 1.5f;
                p.position += Vector3.up * dt * 0.25f;
                if (col.a <= 0f) p.gameObject.SetActive(false);
            }
        }

        private ViewmodelArms _arms;
        private Transform _palm;
        private Transform LeftPalm()
        {
            if (_palm != null) return _palm;
            var owner = GameServices.Round?.PlayerAt(_owner);
            var visual = owner != null ? owner.GetComponent<CharacterVisual>() : null;
            var model = visual != null ? visual.Model : null;
            var skinned = model != null ? model.GetComponentInChildren<SkinnedMeshRenderer>() : null;
            if (skinned == null) return null;
            // The bone her left HAND is on: the forearm on a rig with elbows (`CharacterVisual.HandBone`).
            if (!CharacterVisual.HandBone(skinned, "left", out int hand, out Vector3 palm)) return null;
            var anchor = new GameObject("ManikaPalm").transform;
            anchor.SetParent(skinned.bones[hand], false);
            anchor.localPosition = palm + Vector3.up * CharacterVisual.HandTopLift;
            _palm = anchor;
            return _palm;
        }

        /// <summary>Her own screen: a copy of the doll in her first-person left hand, so she sees she is using it.</summary>
        private void AttachFirstPerson()
        {
            if (_model == null) return;
            var owner = GameServices.Round?.PlayerAt(_owner);
            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>())
            {
                if (arms == null || owner == null || arms.BoundCharacter != owner) continue;
                var hand = arms.LeftHandForProps();
                if (hand == null) continue;
                _fppCopy = Instantiate(_model, hand, false);
                _fppCopy.name = "ManikaFirstPerson";
                // v2 (film v4: at 0.55, turned edge-on, it was a thin orange sliver at her fingertip): bigger, sitting ON the palm,
                // and turned to her eye every frame in `Hold`, so she sees its face and the head she is twisting.
                _fppCopy.transform.localPosition = arms.LeftPalmOffset() + new Vector3(0.0f, 0.10f, 0.0f);
                _fppCopy.transform.localScale = Vector3.one * 1.6f;
                foreach (var r in _fppCopy.GetComponentsInChildren<Renderer>()) r.gameObject.layer = hand.gameObject.layer;
                _fppHead = PhaisterProp.Find(_fppCopy, "head");
                arms.HoldingProp = true;
                _arms = arms;
                break;
            }
        }

        /// <summary>The victim's colour: their palette's most saturated bright slot (a hero's outfit hue).</summary>
        private static Color VictimColour(CharacterMotor victim)
        {
            var palette = victim.GetComponent<CharacterVisual>()?.AppliedPalette;
            Color best = new Color(0.80f, 0.55f, 0.40f); float bestScore = 0f;
            if (palette == null) return best;
            for (int i = 0; i < palette.Length; i++)
            {
                Color.RGBToHSV(palette[i], out _, out float s, out float v);
                float score = s * Mathf.Clamp01((v - 0.25f) * 2f);
                if (score > bestScore) { bestScore = score; best = palette[i]; }
            }
            return best;
        }
    }
}
