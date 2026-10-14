using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ WHAT POSSESSED LOOKS LIKE TO EVERYONE (Nemu's LET THEM IN, `docs/reports/nemu-ultimate-2026-10-11/direction.md`
    /// section 11.6). The owner, 2026-10-13: *"i dont like hwo the ghosts js sit inside on the ppl"*, *"i wanted the ghost to go
    /// inside the ppl tapos may parang aura nalang sila that theyre nasasapian"*, *"HINDI UNG KITA UNG GHHOST IS IN THEM"*. So
    /// NO GHOST IS DRAWN on or in them (v1's ghost-shaped sheet is gone). They are themselves, plus:
    ///
    ///  * THE AURA (`NemuAura.Draw`): black smoke burning up off their silhouette like dark fire, its edges violet; a faint
    ///    violet halo round them that reads across the court; embers rising. The same drawing the cutscene ends on.
    ///  * POSSESSED EYES: their own painted eyes burn violet (`NemuEyeGlow`, the cutscene's), the WHOLE time they are
    ///    possessed, flaring with the tell and the surge. (Owner 2026-10-14 on play after v8: the floating glows sat on the
    ///    cheeks as pink blobs and the eyes were not violet: *"purple eyes entire time na possessed"*.)
    ///  * WHICH WAY THEY ARE DRAGGED: the smoke streams that way; faint bare footprints walk ahead of them that way.
    ///  * THE TELL: the aura swells and darkens, the embers quicken, the eyes flare.
    ///  * THE SURGE: a claw of smoke reaches out of them toward where it drags them, and is yanked back in as it ends.
    ///  * TRYING TO GET OUT (the owner: *"the ghosts are tryna get out of the body but they get sucked in anyways"*): between
    ///    surges, on typed rows, a claw pushes out of a side and is sucked back with a puff. A claw only: never a face or a body.
    ///  * WINNING: as the player wears the grip down the aura thins and flickers.
    ///  * THE END: the last 1.2 s the smoke burns off upward; the eyes go out with it.
    ///
    /// ⚠️ PRESENTATION ONLY, posed from the replicated POSSESSED clock and positions through the same pure function the
    /// simulating peer steers with (`CharacterMotor.PossessionPull`), so every screen shows the same drag. It never moves the
    /// body. ⚠️ The aura and the eyes are hidden from the victim's own first-person view (they would fill the lens); their
    /// screen gets its own treatment (`UI.TumpHudEffects`). The footprints stay: they help the victim read it too.
    /// </summary>
    public sealed class PossessedPresence : MonoBehaviour
    {
        private static readonly Color Print = new Color(.16f, .08f, .24f, 1f);

        private sealed class Part
        {
            public Transform T;
            public Renderer R;
            public MaterialPropertyBlock B;
        }

        private CharacterMotor _motor;
        private NemuSmokeBatch _smoke;
        private readonly Part[] _prints = new Part[4];
        private float _leaveAge = -1f;
        private static Shader _ghostShader;

        /// <summary>
        /// THE ESCAPES: (seconds into the possession, angle round the body from its facing in degrees, height as a share of the
        /// body, how far out in metres). Each seat starts 0.37 s later round the list, so four victims never strain together.
        /// </summary>
        private static readonly Vector4[] Escapes =
        {
            new Vector4(1.05f, -70f, .55f, .42f), new Vector4(3.15f, 120f, .62f, .36f), new Vector4(5.45f, -150f, .48f, .44f),
            new Vector4(7.75f, 40f, .66f, .38f), new Vector4(10.05f, -100f, .58f, .41f), new Vector4(11.95f, 160f, .52f, .35f),
        };
        private const float EscapeSeconds = .7f, SuckSeconds = .12f;

        /// <summary>0 to 1: how far out an escaping claw is (a slow push, then a fast suck back in), and which row.</summary>
        private static float EscapeAt(float elapsed, int seat, out Vector4 row)
        {
            row = Vector4.zero;
            foreach (var e in Escapes)
            {
                float u = elapsed - (e.x + .37f * seat);
                if (u < 0f || u >= EscapeSeconds) continue;
                row = e;
                float push = EscapeSeconds - SuckSeconds;
                return u < push ? Mathf.SmoothStep(0f, 1f, u / push) : 1f - (u - push) / SuckSeconds;
            }
            return 0f;
        }

        // The footprints ahead of the feet: (distance along the pull, side offset, phase in the walk loop). Typed.
        private static readonly Vector3[] PrintRows = { new Vector3(.45f, -.09f, 0f), new Vector3(.80f, .09f, .25f), new Vector3(1.15f, -.08f, .5f), new Vector3(1.50f, .10f, .75f) };

        public static PossessedPresence Attach(CharacterMotor motor)
        {
            if (motor == null || !Application.isPlaying) return null;
            var existing = motor.GetComponentInChildren<PossessedPresence>(true);
            if (existing != null) { existing._leaveAge = -1f; return existing; }
            var go = new GameObject("~PossessedPresence");
            go.transform.SetParent(motor.transform, false);
            var presence = go.AddComponent<PossessedPresence>();
            presence._motor = motor;
            presence.Build();
            return presence;
        }

        // ------------------------------------------------------------------ building

        private void Build()
        {
            _smoke = new NemuSmokeBatch(transform, "PossessedAura", 64, 80);
            for (int i = 0; i < PrintRows.Length; i++)
            {
                var go = new GameObject("Footprint" + i);
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(PrintRows[i].y < 0f ? 1f : -1f, 1f, 1f) * 1.25f;
                go.AddComponent<MeshFilter>().sharedMesh = ProcessionShapes.Sole;
                var r = go.AddComponent<MeshRenderer>();
                r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
                if (_ghostShader == null) _ghostShader = Resources.Load<Shader>("Shaders/ProcessionGhost");
                if (_ghostShader != null)
                {
                    var m = new Material(_ghostShader) { name = "Footprint" + i };
                    m.SetColor("_Tint", Print);
                    m.SetFloat("_FadeLow", -1f); m.SetFloat("_FadeHigh", -.9f); m.SetFloat("_RimAlpha", 0f); m.SetFloat("_Alpha", .55f);
                    r.sharedMaterial = m; VfxRenderTag.Own(go, m);
                }
                else VfxMaterial.Ghost(r, new Color(Print.r, Print.g, Print.b, .4f), .2f);
                _prints[i] = new Part { T = go.transform, R = r, B = new MaterialPropertyBlock() };
            }
        }

        private void OnDestroy() { _smoke?.Dispose(); _smoke = null; _eyes?.Remove(); _eyes = null; }

        // ------------------------------------------------------------------ posing

        private static float Ease(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }

        private void LateUpdate()
        {
            if (_motor == null) { Destroy(gameObject); return; }
            // The world while it holds: her dusk on every screen and the court she split kept open.
            if (_motor.IsPossessed) NemuWorld.Ensure();
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            float now = Time.time;
            bool possessed = _motor.IsPossessed;
            float elapsed = possessed ? _motor.PossessionElapsed : NemuRules.PossessedSeconds;
            int seat = _motor.PlayerSlot;

            // The end: the last 1.2 s of the clock, or a cleanse, a teleport or the round's end that took it early.
            float leave;
            if (possessed && elapsed < NemuRules.PossessedSeconds - NemuRules.LeaveSeconds) { leave = 0f; _leaveAge = -1f; }
            else
            {
                if (_leaveAge < 0f) _leaveAge = possessed ? elapsed - (NemuRules.PossessedSeconds - NemuRules.LeaveSeconds) : 0f;
                else _leaveAge += Time.deltaTime;
                leave = Mathf.Clamp01(_leaveAge / NemuRules.LeaveSeconds);
            }
            if (!possessed && leave >= 1f) { Destroy(gameObject); return; }

            float settle = Ease(possessed ? elapsed / NemuRules.SettleSeconds : 1f);
            float tell = possessed ? NemuRules.TellAmount(elapsed, seat) : 0f;
            float surge = possessed ? NemuRules.SurgeEnvelope(elapsed, seat) : 0f;
            float after = possessed ? SuckAfterSurge(elapsed, seat) : 0f;
            bool pulling = _motor.PossessionPull(out var pull);
            float strength = pull.magnitude;
            Vector3 dir = pulling ? pull / Mathf.Max(1e-4f, strength) : _motor.transform.forward;
            dir.y = 0f; dir = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector3.forward;
            float gripLook = Mathf.InverseLerp(NemuRules.GripFloor, 1f, _motor.PossessionGrip);

            var model = _motor.GetComponent<CharacterVisual>()?.Model;
            if (model != null && _measuredModel != model.transform) Measure(model.transform);
            Vector3 feet = _motor.transform.position;
            Vector3 forward = _motor.transform.forward; forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();

            bool hideOwn = HiddenFromThisViewer();
            _smoke.Begin();
            if (!hideOwn)
            {
                // THE AURA. The cutscene already showed it arriving, so play picks it up whole from the first frame (films r1 and
                // r9: anything keyed to the possession clock is still at zero on the hand-back frame). It burns off as it leaves.
                const float arrive = 1f;
                float amount = arrive * (1f - leave) * Mathf.Lerp(.5f, 1f, gripLook);
                var state = new NemuAura.State
                {
                    Feet = feet + Vector3.up * (Ease(leave) * .4f), Forward = forward, Stream = pulling ? dir : Vector3.zero,
                    Top = _top, Width = _width, Clock = now, Amount = amount, Swell = Mathf.Max(tell, after * .6f), Drag = surge,
                    Thin = _motor.PossessionResisting ? 1f - gripLook : 0f, Seat = seat, Reduced = reduced,
                };
                NemuAura.Draw(_smoke, state);

                // THE SURGE: a claw reaching out of them toward where it drags them, yanked back as the surge lets go.
                float lunge = Mathf.Max(surge, after * .5f) * settle * (1f - leave);
                NemuAura.Claw(_smoke, feet + Vector3.up * (_top * .55f) + dir * (_width * .15f), dir + Vector3.up * .15f,
                              (.35f + .35f * surge) * _width * 1.4f * Mathf.Clamp01(surge * 1.5f + after), .8f, Mathf.Clamp01(lunge * 2f), now, seat * .23f);

                // TRYING TO GET OUT: a claw pushes out of a side and is sucked back with a puff.
                Vector4 attempt = Vector4.zero;
                float escape = possessed ? EscapeAt(elapsed, seat, out attempt) : 0f;
                if (escape > .01f)
                {
                    Vector3 escDir = Quaternion.AngleAxis(attempt.y, Vector3.up) * forward;
                    Vector3 root = feet + Vector3.up * (_top * attempt.z) + escDir * (_width * .15f);
                    NemuAura.Claw(_smoke, root, escDir + Vector3.up * .2f, attempt.w * escape, .75f, Mathf.Clamp01(escape * 3f) * settle, now, attempt.y * .01f);
                }

            }
            _smoke.End(now);

            // POSSESSED EYES: their own painted eyes burning violet from the first frame of play to the end (hidden from their
            // own first-person view, where the head is not drawn either), flaring with the tell and the surge.
            float burn = hideOwn ? 0f : (1f - leave) * Mathf.Clamp01(.8f + .2f * tell + .2f * surge);
            _eyes?.Set(burn, now + seat);

            // The footprints walk ahead of the feet the way it pulls, one after another, brighter as it gets ready to surge.
            Vector3 across = new Vector3(dir.z, 0f, -dir.x);
            float printLevel = pulling ? (.18f + .3f * tell + .35f * surge) * settle * (1f - leave) : 0f;
            float printYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            for (int i = 0; i < _prints.Length; i++)
            {
                var row = PrintRows[i];
                Vector3 at = feet + dir * row.x + across * row.y;
                at.y = Slipper.GroundY(at + Vector3.up * .4f) + .012f;
                _prints[i].T.position = at;
                _prints[i].T.rotation = Quaternion.Euler(0f, printYaw, 0f);
                float loop = Mathf.Repeat(now * (1.2f + 1.6f * surge) - row.z, 1f);
                float step = reduced ? 1f : Mathf.Clamp01(1f - loop * 1.6f);
                float alpha = Mathf.Clamp01(printLevel * step);
                _prints[i].B.SetFloat("_Presence", alpha);
                _prints[i].R.SetPropertyBlock(_prints[i].B);
                _prints[i].R.enabled = alpha > .01f;
            }
        }

        /// <summary>1 the instant a surge lets go, falling to 0 over 0.18 s: the snap of being sucked back in.</summary>
        private static float SuckAfterSurge(float elapsed, int seat)
        {
            for (int i = 0; i < NemuRules.SurgesPerPossession; i++)
            {
                float u = elapsed - (NemuRules.SurgeStart(seat, i) + NemuRules.SurgeSeconds);
                if (u >= 0f && u < .18f) return 1f - u / .18f;
            }
            return 0f;
        }

        private bool HiddenFromThisViewer()
        {
            var cam = Camera.main;
            if (cam == null || !_motor.IsLocalViewer) return false;
            var rig = cam.GetComponent<CameraRig>();
            return rig != null && rig.IsFollowing(_motor) && rig.IsLocalFpp;
        }

        // ------------------------------------------------------------------ the body it is in, measured once at rest

        private Transform _measuredModel, _head;
        private NemuEyeGlow _eyes;
        private float _top = 1.7f, _width = .7f, _faceDepth = .3f, _faceTop = .5f;
        private Vector3 _faceLocal = Vector3.forward;

        /// <summary>
        /// The body's height above its feet, its width and how far the face is in front of the head bone, from its own
        /// surfaces (the cast's heads, hair and builds differ a lot). The held slipper is left out.
        /// </summary>
        private void Measure(Transform model)
        {
            _measuredModel = model; _head = null;
            // The eyes are drawn on this model's own head; a rebuilt model gets them again.
            _eyes?.Remove();
            _eyes = NemuEyeGlow.On(model.GetComponentsInChildren<Renderer>(true));
            foreach (var t in model.GetComponentsInChildren<Transform>(true)) if (t.name == "head") { _head = t; break; }
            Vector3 feet = _motor.transform.position;
            bool any = false; Bounds b = default;
            foreach (var r in model.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer || r.GetComponentInParent<Slipper>() != null) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) return;
            _top = Mathf.Clamp(b.max.y - feet.y, .9f, 2.6f);
            _width = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z), .4f, 1.2f);
            // The face, from the body's real surface (`NemuAura.MeasureFace`; the held slipper is left out there).
            if (_head != null)
                NemuAura.MeasureFace(_head, model.GetComponentsInChildren<Renderer>(false), _motor.transform.forward, out _faceLocal, out _faceDepth, out _faceTop);
        }
    }
}
