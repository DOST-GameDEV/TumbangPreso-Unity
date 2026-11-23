using System.Collections.Generic;
using TumbangPreso.Core;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.Abilities
{
    /// <summary>
    /// ⚠️⚠️ MAYON AWAKENS v2, THE MOUNTAIN RISES (owner 2026-10-24: *"the ult of sean doesnt earn its place"*, held against
    /// Paete's and Phaister's; he chose "The mountain rises"). A small Mayon (since 2026-11-23 the remodelled concept B,
    /// `SeanVolcanoLook`: `Models/SeanProps/Volcano/mayon_b`, 3 m to its rim, the character round its foot)
    /// bursts up out of the court where he chose, throws everyone within 9 m straight out and SEARS them, then erupts for 8 s,
    /// lobbing a volcanic bomb every 1.2 s at the nearest opponent; then it cools and sinks back into the court. While it
    /// erupts he is Molten (`SeanHeroKit.IsMolten`). Like Paete's guardian tree, it is a THING in the court everyone plays round.
    ///
    /// ⚠️ EVERY PEER BUILDS ITS OWN FROM THE ACCEPTED CAST, ON ITS OWN CLOCK, AT THE SAME SPOT (`SpotFor`: the commit's aim and
    /// the synced can). The host alone decides who each bomb is thrown at and resolves every hit; each bomb reaches the other
    /// peers as `MatchFlair.Kind.SeanVolcanoBomb` (seat, target, landing point), and every peer flies it from its own crater.
    /// The rise's throw, its SEARED and every bomb's push ride the bodies' own sync, as every other host blast does.
    ///
    /// ⚠️ PLAY PICKS UP WHERE THE CUTSCENE ENDS (`docs/HERO_KIT_METHOD.md` § 6 rule 7). The cutscene shows it bursting up, so the
    /// live one is spawned already standing (`handBack`) and throws everyone at once: the throw is seen once.
    ///
    /// ⚠️ IT IS SOLID. A capsule round its foot (`SeanRules.VolcanoSolidRadius`) stops bodies, slippers and the can; it never
    /// stands within `SeanRules.VolcanoClearOfCan` of the can. A late joiner does not see one already standing (it lives 9 s;
    /// nothing in `WorldEffectSnapshot` carries it).
    /// </summary>
    public sealed class SeanVolcano : MonoBehaviour
    {
        private static readonly List<SeanVolcano> Live = new List<SeanVolcano>();

        /// <summary>A fresh cast that skipped the cutscene (a probe) still rises, over this long, before it throws.</summary>
        public const float RiseSeconds = .35f;

        /// <summary>
        /// ⚠️ HOW FAR INTO ITS BIRTH THE CUTSCENE LEAVES IT (2026-11-23, the cutscene rebuilt round this volcano): the
        /// cutscene's mountain begins to rise at `HeroIntroductionScene.SeanRiseAt` and the cutscene ends at its own length,
        /// so on a hand-back the look (the court and rocks round its foot, its lava, `SeanEruption`) is posed this many seconds
        /// on, and play's first frame is the cutscene's last. Look only: its age, its bombs and its throw are untouched.
        /// </summary>
        public const float HandBackLead = SeanRules.CutsceneSeconds - HeroIntroductionScene.SeanRiseAt;

        public int OwnerSlot { get; private set; } = -1;
        public Vector3 Foot { get; private set; }
        /// <summary>Seconds since it burst up (negative while it is still rising).</summary>
        public float Age => _age;
        public Vector3 Crater => Foot + Vector3.up * (SeanRules.VolcanoHeight + .1f);

        private CharacterMotor _owner;
        private float _age;
        private bool _risen;
        private SeanVolcanoLook.Live _look;
        private SeanEruption _eruption;
        // ⚠️ HIS RED SKY, WORN FOR THE VOLCANO'S WHOLE LIFE (owner 2026-11-23: *"keep the sky until the end of the effect in live
        // too outside of cutscene"*): the cutscene's sky carried on from the hand-back and gone as it sinks (`SeanSkyLive`).
        // Look only, built on every peer from this volcano's own clock.
        private SeanSkyLive _sky;
        // Which volcano is newest: only the newest draws the one sky (two skies would multiply the street by two grades).
        private static int _serials;
        private int _serial;
        private bool _handBack;
        // When the court round it began to burst: with the mountain's own rise on a fresh cast, at the hand-back otherwise.
        private float _burstFrom;
        /// <summary>Films only (old beside new): spawn the v2 mountain instead of the B look. Off in every match.</summary>
        public static bool UseV2Look;
        /// <summary>The B look this volcano wears (null on the v2 fallback). Films read its report and drawn height.</summary>
        public SeanVolcanoLook.Live Look => _look;
        private Transform _body, _summit;
        private Vector3 _bodyScale = Vector3.one, _bodyOffset;
        private float _kick;
        private Collider _solid;
        private readonly Dictionary<int, int> _thrownAt = new Dictionary<int, int>();
        // Who the rise threw, SEARED once their slide is over (host): SEARED's slow also slows a slide, and applied with the
        // throw it cut the 4.3 m throw to 2 m (`SeanKitPlayProbe`, 2026-10-24).
        private readonly List<CharacterMotor> _thrown = new List<CharacterMotor>();
        private bool _seared;

        private void OnEnable() => Live.Add(this);
        private void OnDisable() => Live.Remove(this);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLive() => Live.Clear();

        /// <summary>
        /// Where it comes up: where he chose (the ring he held), between the aim ranges, on the court, never on the can. The
        /// cutscene asks this too, so its staged mountain stands exactly where the live one will.
        /// </summary>
        public static Vector3 SpotFor(Vector3 feet, Vector3 forward, Vector3 aimPoint, Lata lata)
        {
            Vector3 d = aimPoint - feet; d.y = 0f;
            if (d.sqrMagnitude < .04f) { d = forward; d.y = 0f; if (d.sqrMagnitude < 1e-4f) d = Vector3.forward; d = d.normalized * SeanRules.VolcanoAimMaxRange; }
            float range = Mathf.Clamp(d.magnitude, SeanRules.VolcanoAimMinRange, SeanRules.VolcanoAimMaxRange);
            Vector3 p = feet + d.normalized * range;
            if (lata != null)
            {
                Vector3 can = lata.transform.position;
                SeanRules.VolcanoSpotClearOfCan(p.x, p.z, can.x, can.z, feet.x, feet.z, out float x, out float z);
                p.x = x; p.z = z;
            }
            p = AIController.ClampToPlayable(p, SeanRules.VolcanoSolidRadius);
            p.y = Slipper.GroundY(p);
            return p;
        }

        public static SeanVolcano Spawn(CharacterMotor owner, Vector3 foot, bool handBack)
        {
            var go = new GameObject("SeanVolcano");
            go.transform.position = foot;
            var v = go.AddComponent<SeanVolcano>();
            v._owner = owner; v.OwnerSlot = owner != null ? owner.PlayerSlot : -1; v.Foot = foot;
            v._serial = ++_serials;
            v._age = handBack ? 0f : -RiseSeconds;
            // On a hand-back the look carries on from where the cutscene left it (`HandBackLead`).
            v._burstFrom = handBack ? -HandBackLead : v._age;
            v._handBack = handBack;
            v.BuildBody();
            if (handBack) v.Rise();
            v.Pose();
            return v;
        }

        /// <summary>Every peer: a bomb the host announced leaves this owner's crater for <paramref name="at"/>.</summary>
        public static void LaunchBomb(int ownerSlot, int targetSlot, Vector3 at)
        {
            foreach (var v in Live)
            {
                if (v == null || v.OwnerSlot != ownerSlot || !v._risen) continue;
                SeanVolcanicBomb.FromVolcano(v._owner, v.Crater, at);
                v._kick = 1f;
                GameServices.Audio?.PlayAt("sfx_cast_sean_cannon", v.Crater);
                return;
            }
        }

        /// <summary>Every volcano this seat raised goes at once (a round reset, the kit cancelled).</summary>
        public static void RetireFor(int ownerSlot)
        {
            for (int i = Live.Count - 1; i >= 0; i--)
            {
                var v = Live[i];
                if (v == null || v.OwnerSlot != ownerSlot) continue;
                v.gameObject.SetActive(false);
                Object.Destroy(v.gameObject);
            }
        }

        private void OnDestroy() { _look?.Dispose(); _eruption?.Dispose(); _sky?.Dispose(); _sky = null; }

        private void BuildBody()
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);
            // ⚠️ CONCEPT B, COLOUR SET 2 (owner 2026-11-23: "go with b", "i like warm basalt colour set"): the clean Mayon with
            // the character round its foot (`SeanVolcanoLook`). It is authored at play's size with its origin at the foot's
            // middle, so it stands on the foot as it is: the fit is 1 (measured, `SeanVolcanoLook.Live.Fit`), no offset.
            // The cutscene still stages the v2 `mayon` (HeroIntroductionScene.Sean.cs, rebuilt separately); if the B model
            // is ever missing, play falls back to that one here too.
            _look = UseV2Look ? null : SeanVolcanoLook.Build(transform, _body, Foot);
            if (_look != null)
            {
                _bodyScale = Vector3.one * _look.Fit;
                _bodyOffset = Vector3.zero;
                // The birth round it (ash, dust, the wave's dust, the bombs): the cutscene's own, posed from the same number.
                _eruption = SeanEruption.Build(transform);
            }
            var model = _look == null ? SeanProp.Spawn("mayon", _body, SeanProp.CourtMountainPalette) : null;
            if (model != null)
            {
                // ⚠️ FITTED FROM ITS DRAWN BOUNDS (the cutscene learned it on film s14: the builder's "1.0 tall" is not what draws).
                var renderers = model.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length > 0)
                {
                    var b = renderers[0].bounds;
                    foreach (var r in renderers) b.Encapsulate(r.bounds);
                    float tall = b.size.y;
                    if (tall > .001f)
                    {
                        float k = SeanRules.VolcanoHeight / (tall * .97f / 1.225f);
                        // Its foot's width stays inside the solid it stands on, a little over: the cone is wider at its base.
                        _bodyScale = new Vector3(k, k, k);
                        _bodyOffset = new Vector3(-(b.center.x - transform.position.x) * k, -(b.min.y - transform.position.y) * k - .05f,
                                                  -(b.center.z - transform.position.z) * k);
                    }
                }
            }
            _sky = SeanSkyLive.Build(transform, Foot, _owner != null ? _owner.transform.position : (Vector3?)null, _handBack);
            _summit = new GameObject("Summit").transform;
            _summit.SetParent(transform, false);
            _summit.localPosition = Vector3.up * SeanRules.VolcanoHeight;
            AbilityVfx.AttachAura(_summit, AbilityVfx.Aura.FireEmber, SeanRules.MoltenSeconds + RiseSeconds + SeanRules.VolcanoSinkSeconds);

            var solid = new GameObject("Solid");
            solid.transform.SetParent(transform, false);
            var capsule = solid.AddComponent<CapsuleCollider>();
            capsule.radius = SeanRules.VolcanoSolidRadius;
            capsule.height = SeanRules.VolcanoHeight;
            capsule.center = Vector3.up * SeanRules.VolcanoHeight * .5f;
            _solid = capsule;
            _solid.enabled = false;
        }

        /// <summary>It bursts up: the court breaks, everyone within 9 m is thrown out (host), and it is solid from here.</summary>
        private void Rise()
        {
            if (_risen) return;
            _risen = true;
            _solid.enabled = true;
            GameServices.Audio?.PlayAt("hero_sean_ult", Foot);
            GameServices.Audio?.PlayAt("sfx_explosion_heavy", Foot);
            // ⚠️ ON A HAND-BACK THE CUTSCENE HAS ALREADY SHOWN THE BIRTH, and its eruption carries on here (`SeanEruption`, the
            // wave's dust reaching the 9 m it throws to), so the stock fire burst and the flat flash ring, which would be a
            // second birth on the hand-back's first frame, are only for a fresh cast with no cutscene before it (a probe, or
            // the v2 look's fallback). Look only: the throw below is the same either way.
            if (!_handBack || _eruption == null)
            {
                // The burst sized to the crater: at the 9 m reach its debris flew through every nearby lens as metre-wide shards
                // (film v5). The reach itself is the ground ring below.
                HeroHazards.CreateExplosionVisual(Foot, 3.5f, "", HeroHazards.ExplosionStyle.Fire);
                SeanGroundMark.Spawn(Foot, SeanRules.VolcanoBlastRadius, SeanGroundMark.Mode.Flash, .5f, SeanImpact.Accent);
            }
            ShakeNearby(.5f, .45f);
            if (_owner != null)
                SeanHeroKit.HostBlast(_owner, Foot, SeanRules.VolcanoBlastRadius, SeanRules.VolcanoBlastPush,
                    SeanRules.VolcanoBlastStaggerSeconds, knocksCan: false, MatchFlair.Kind.SeanBlast, hit: _thrown);
        }

        private void ShakeNearby(float scale, float seconds)
        {
            var main = UnityEngine.Camera.main;
            var rig = main != null ? main.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig == null) return;
            float near = Mathf.Clamp01(1f - Vector3.Distance(main.transform.position, Foot) / 20f);
            rig.Shake(Mathf.Lerp(.4f, 1f, near) * scale * 2f, seconds);
        }

        private void Update()
        {
            float dt = PresentationClock.Held ? 0f : Time.deltaTime;
            if (dt <= 0f) return;
            float before = _age;
            _age += dt;
            if (!_risen && _age >= 0f) Rise();
            if (_risen && NetAuthority.ShouldResolve() && SeanRules.BombDue(before, _age, out int index)) HostLob(index);
            if (_risen && !_seared && _age >= Combat.SpeedForDistance(SeanRules.VolcanoBlastPush) / Balance.Friction)
            {
                _seared = true;
                foreach (var p in _thrown) if (p != null) p.ApplySeared(SeanRules.VolcanoSearedSeconds);
                _thrown.Clear();
            }
            _kick = Mathf.MoveTowards(_kick, 0f, dt * 4f);
            Pose();
            if (_age >= SeanRules.MoltenSeconds + SeanRules.VolcanoSinkSeconds) Destroy(gameObject);
        }

        /// <summary>The host picks who this bomb is for: the opponent it has thrown at least, nearest first, within reach.</summary>
        private void HostLob(int index)
        {
            if (_owner == null) return;
            CharacterMotor best = null;
            int fewest = int.MaxValue; float nearest = float.MaxValue;
            foreach (var p in SeanHeroKit.Rivals(_owner))
            {
                if (p.IsDefender == _owner.IsDefender) continue;   // his own side is never a target
                Vector3 to = p.transform.position - Foot;
                if (!SeanRules.WithinReach(to.y, SeanRules.BlastReachHeight * 2f)) continue;
                to.y = 0f;
                float d = to.magnitude;
                if (d > SeanRules.VolcanoBombRange || d < SeanRules.VolcanoSolidRadius) continue;
                _thrownAt.TryGetValue(p.PlayerSlot, out int times);
                if (times < fewest || (times == fewest && d < nearest)) { best = p; fewest = times; nearest = d; }
            }
            if (best == null) return;
            _thrownAt[best.PlayerSlot] = fewest + 1;
            Vector3 land = best.transform.position;
            if (VfxShapes.TryGroundUnder(land, 1.5f, 4f, out float y)) land.y = y;
            MatchFlair.Announce(MatchFlair.Kind.SeanVolcanoBomb, _owner.PlayerSlot, best.PlayerSlot, land, index);
        }

        private void Pose()
        {
            // Rising: up out of the court with one overshoot. Erupting: a recoil on each bomb. Sinking: back down into the court.
            float up;
            if (_age < 0f)
            {
                float u = 1f + _age / RiseSeconds;
                up = 1f - Mathf.Pow(1f - Mathf.Clamp01(u), 3f) + .12f * Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI);
            }
            else up = 1f;
            float sink = Mathf.Clamp01((_age - SeanRules.MoltenSeconds) / SeanRules.VolcanoSinkSeconds);
            if (sink > 0f && _solid != null && _solid.enabled) _solid.enabled = false;
            float squash = 1f - .06f * _kick;
            if (_body != null)
            {
                _body.localPosition = _bodyOffset + Vector3.down * (SeanRules.VolcanoHeight * (1f - up) + SeanRules.VolcanoHeight * 1.05f * sink * sink);
                _body.localScale = new Vector3(_bodyScale.x * (2f - squash), _bodyScale.y * squash, _bodyScale.z * (2f - squash));
            }
            if (_summit != null) _summit.gameObject.SetActive(sink < .5f && _age > -RiseSeconds * .5f);
            // ⚠️ WHAT STANDS ROUND IT BURSTS UP WITH IT AND GOES WITH IT (brief, v6): Dante's rocks and the heaved court come up
            // fast, past their seat and home, from the moment it is spawned (with the mountain on a fresh cast; round the
            // mountain the cutscene handed back otherwise), the lava runs down from the crater over the first second, and
            // all of it sinks and is eaten as the mountain goes. Look only: the solid, the bombs and every timing above are
            // untouched.
            // Its lava's clock is the same number, so the crust the cutscene was drifting keeps drifting from where it was.
            _look?.Pose(_age - _burstFrom, _age - _burstFrom, sink, Mathf.Max(0f, _age - SeanRules.MoltenSeconds));
            if (_eruption != null)
            {
                float since = _age - _burstFrom;
                if (since <= SeanEruption.EndSeconds) _eruption.Pose(since);
                else { _eruption.Dispose(); _eruption = null; }
            }
            if (_sky != null)
            {
                // The cutscene's clock carried on: `SeanRules.CutsceneSeconds + age` after a hand-back, from the rise on a fresh
                // cast. It holds while it stands and is gone as the mountain sinks (gone by the time it is destroyed).
                float clock = HeroIntroductionScene.SeanRiseAt + (_age - _burstFrom);
                float gone = SeanSkyLive.Ease(SeanRules.MoltenSeconds - .25f, SeanRules.MoltenSeconds + SeanRules.VolcanoSinkSeconds - .05f, _age);
                bool lead = true;
                foreach (var other in Live) if (other != null && other != this && other._serial > _serial) { lead = false; break; }
                _sky.Pose(clock, _age - (_handBack ? 0f : -RiseSeconds), gone, lead);
            }
        }
    }
}
