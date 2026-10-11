using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ WHAT POSSESSED LOOKS LIKE TO EVERYONE (Nemu's PRUSISYON, `docs/reports/nemu-ultimate-2026-10-11/direction.md`
    /// section 5). The owner, 2026-10-11: *"i dont want ghost to be above the person like a jockey, i js wanted u to mimic
    /// the jockey's steering effect"*, *"the ghosts will all go INSIDE the ppl and theyre gonna look possessed"*, *"maybe
    /// they have some sort of purple eerie aura"*. So the soul is INSIDE them:
    ///
    ///  * THE GHOST IN THEM: a procession soul's sheet (`ProcessionShapes.Soul`), sized a little larger than their body and
    ///    centred on it. Their own body hides most of it, so what shows is a purple eerie aura in a ghost's shape round
    ///    their silhouette, glowing at its rim (`Shaders/ProcessionGhost`), breathing.
    ///  * POSSESSED EYES: two violet lights over their eyes, flickering.
    ///  * WHICH WAY THEY ARE DRAGGED: before a surge the ghost swells and burns brighter (the tell); in a surge it leans half
    ///    OUT of them toward where it is dragging them, then sinks back in. Faint bare footprints walk ahead of them that way.
    ///  * THE SOULS' MESSENGERS: two black moths (paru-paro) circle their head, and streak ahead along the pull in a surge.
    ///  * IN AND OUT: it sinks into them from above as it starts (the cutscene shows it arriving), and climbs out of them
    ///    over the last 1.2 s. As the player wears its grip down it thins and flickers.
    ///
    /// ⚠️ PRESENTATION ONLY, posed from the replicated POSSESSED clock and positions through the same pure function the
    /// simulating peer steers with (`CharacterMotor.PossessionPull`), so every screen shows the same lean. It never moves the
    /// body. ⚠️ The ghost and the eyes are hidden from the victim's own first-person view (they would fill the lens); their
    /// screen gets its own treatment (`UI.TumpHudEffects`). The moths and footprints stay: they help the victim read it too.
    /// </summary>
    public sealed class PossessedPresence : MonoBehaviour
    {
        // A DARK, cold violet (owner: *"darker purple"*, *"ang weird ng hue of that purple"*: the first pass read pink-lavender
        // and neon). The body is a smoky near-black veil that darkens what is behind it; the rim a deep violet, not a glow stick.
        private static readonly Color AuraBody = new Color(.07f, .02f, .12f, 1f);
        // Film n1: the edge was too faint to read across the court; a brighter violet, still dark-bodied.
        private static readonly Color AuraRim = new Color(.50f, .20f, .82f, 1f);
        private static readonly Color EyeLight = new Color(.62f, .30f, .98f, 1f);
        private static readonly Color MothWing = new Color(.07f, .05f, .09f, .92f);
        private static readonly Color Print = new Color(.16f, .08f, .24f, 1f);

        private sealed class Part
        {
            public Transform T;
            public Renderer R;
            public Color C;
            public bool Ghost;
            public MaterialPropertyBlock B;
        }

        private CharacterMotor _motor;
        private Part _ghost, _eyeL, _eyeR, _handA, _handB;
        private readonly Part[] _moths = new Part[4];
        private readonly Transform[] _mothBodies = new Transform[2];
        private readonly Part[] _prints = new Part[4];
        private float _leaveAge = -1f;
        private static Shader _glowShader, _ghostShader;

        /// <summary>
        /// THE ESCAPES (owner: *"the ghosts are tryna get out of the body but they get sucked in anyways"*): between surges the
        /// trapped soul pushes a hand out through the body and is sucked back in. Typed rows: (seconds into the possession,
        /// angle round the body from its facing in degrees, height as a share of the body, how far out in metres). Each seat
        /// starts 0.37 s later round the list, so four victims never strain on the same frame.
        /// </summary>
        private static readonly Vector4[] Escapes =
        {
            new Vector4(1.05f, -70f, .55f, .34f), new Vector4(3.15f, 120f, .62f, .28f), new Vector4(5.45f, -150f, .48f, .36f),
            new Vector4(7.75f, 40f, .66f, .30f), new Vector4(10.05f, -100f, .58f, .33f), new Vector4(11.95f, 160f, .52f, .27f),
        };
        private const float EscapeSeconds = .7f, SuckSeconds = .12f;

        /// <summary>0 to 1: how far out an escaping hand is (a slow push, then a fast suck back in), and which row.</summary>
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

        // The two moths' orbits round the head: (radius, height, angular speed deg/s, phase deg), and their wing beats (Hz). Typed.
        private static readonly Vector4[] MothOrbits = { new Vector4(.55f, .25f, 140f, 0f), new Vector4(.70f, -.05f, -110f, 160f) };
        private static readonly float[] MothBeats = { 9f, 7.5f };
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

        private Part Piece(string name, Mesh mesh, Vector3 scale, Color colour, bool ghost)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            if (ghost)
            {
                if (_ghostShader == null) _ghostShader = Resources.Load<Shader>("Shaders/ProcessionGhost");
                if (_ghostShader != null)
                {
                    var m = new Material(_ghostShader) { name = name };
                    m.SetColor("_Tint", colour);
                    r.sharedMaterial = m; VfxRenderTag.Own(go, m);
                }
                else VfxMaterial.Ghost(r, new Color(colour.r, colour.g, colour.b, .4f), .4f);
            }
            else VfxMaterial.Solid(r, colour);
            return new Part { T = go.transform, R = r, C = colour, Ghost = ghost, B = new MaterialPropertyBlock() };
        }

        private Part Glow(string name, Color colour, float falloff, float core)
        {
            if (_glowShader == null) _glowShader = Resources.Load<Shader>("Shaders/SpiritGlow");
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name; VfxMaterial.StripCollider(go);
            go.transform.SetParent(transform, false);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            if (_glowShader != null)
            {
                var m = new Material(_glowShader) { name = name };
                m.SetFloat("_Billboard", 1f); m.SetFloat("_Band", 0f); m.SetFloat("_Falloff", falloff);
                m.SetFloat("_Core", core); m.SetFloat("_Lift", .15f);
                r.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
            }
            else VfxMaterial.Ghost(r, colour, 1f);
            return new Part { T = go.transform, R = r, C = colour, B = new MaterialPropertyBlock() };
        }

        private void Build()
        {
            // THE GHOST IN THEM. A strong rim and a thin veil, so the aura reads as light round them, not a purple sheet.
            _ghost = Piece("GhostWithin", ProcessionShapes.Soul, Vector3.one, AuraBody, true);
            var m = _ghost.R.sharedMaterial;
            if (m != null && m.HasProperty("_RimColor"))
            {
                // Almost no veil: the face stays clear and only the edge glows, so it reads as INSIDE them, not over them.
                m.SetColor("_RimColor", AuraRim); m.SetFloat("_Alpha", .18f); m.SetFloat("_RimAlpha", 1f); m.SetFloat("_Glow", 1.1f);
                // The hem dissolves at their feet rather than standing on the court.
                m.SetFloat("_FadeLow", 0f); m.SetFloat("_FadeHigh", .16f);
            }
            // Its hands, which push out through the body when it tries to escape and when it lunges.
            _handA = Piece("EscapingHand", ProcessionShapes.Sleeve, Vector3.one, AuraBody, true);
            _handB = Piece("LungingHand", ProcessionShapes.Sleeve, Vector3.one, AuraBody, true);
            foreach (var h in new[] { _handA, _handB })
            {
                var hm = h.R.sharedMaterial;
                if (hm != null && hm.HasProperty("_RimColor"))
                { hm.SetColor("_RimColor", AuraRim); hm.SetFloat("_Alpha", .35f); hm.SetFloat("_RimAlpha", .9f); hm.SetFloat("_Glow", .95f); hm.SetFloat("_FadeLow", -1f); hm.SetFloat("_FadeHigh", -.9f); }
            }
            _eyeL = Glow("PossessedEyeLeft", EyeLight, 2.4f, .9f);
            _eyeR = Glow("PossessedEyeRight", EyeLight, 2.4f, .9f);
            for (int i = 0; i < 2; i++)
            {
                var moth = new GameObject("Moth" + i).transform; moth.SetParent(transform, false);
                _mothBodies[i] = moth;
                var body = Piece("Body", ProcessionShapes.Bead, new Vector3(.018f, .018f, .06f), MothWing, false);
                body.T.SetParent(moth, false);
                _moths[i * 2] = Piece("WingL", ProcessionShapes.Wing, new Vector3(-1f, 1f, 1f), MothWing, false);
                _moths[i * 2 + 1] = Piece("WingR", ProcessionShapes.Wing, Vector3.one, MothWing, false);
                _moths[i * 2].T.SetParent(moth, false); _moths[i * 2 + 1].T.SetParent(moth, false);
            }
            for (int i = 0; i < PrintRows.Length; i++)
            {
                var print = Piece("Footprint" + i, ProcessionShapes.Sole, new Vector3(PrintRows[i].y < 0f ? 1f : -1f, 1f, 1f) * 1.25f, Print, true);
                var pm = print.R.sharedMaterial;
                if (pm != null && pm.HasProperty("_RimAlpha"))
                { pm.SetFloat("_FadeLow", -1f); pm.SetFloat("_FadeHigh", -.9f); pm.SetFloat("_RimAlpha", 0f); pm.SetFloat("_Alpha", .55f); }
                _prints[i] = print;
            }
        }

        // ------------------------------------------------------------------ posing

        private static float Ease(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }

        private static void Presence(Part p, float alpha, float glow = -1f)
        {
            alpha = Mathf.Clamp01(alpha);
            if (p.Ghost) p.B.SetFloat("_Presence", alpha);
            if (glow >= 0f) p.B.SetFloat("_Glow", glow);
            p.R.SetPropertyBlock(p.B);
            p.R.enabled = alpha > .01f;
        }

        private static void Light(Part p, float strength)
        {
            if (p.R.sharedMaterial != null && p.R.sharedMaterial.shader == _glowShader)
            {
                var c = p.C; c.a = Mathf.Max(0f, strength);
                p.B.SetColor("_Color", c); p.R.SetPropertyBlock(p.B);
            }
            p.R.enabled = strength > .01f;
        }

        private void LateUpdate()
        {
            if (_motor == null) { Destroy(gameObject); return; }
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
            bool pulling = _motor.PossessionPull(out var pull);
            float strength = pull.magnitude;
            Vector3 dir = pulling ? pull / Mathf.Max(1e-4f, strength) : _motor.transform.forward;
            float gripLook = Mathf.InverseLerp(NemuRules.GripFloor, 1f, _motor.PossessionGrip);

            var model = _motor.GetComponent<CharacterVisual>()?.Model;
            if (model != null && _measuredModel != model.transform) Measure(model.transform);
            Vector3 feet = _motor.transform.position;
            Transform headBone = _head;
            Vector3 forward = _motor.transform.forward; forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, forward);

            // THE GHOST IN THEM, a little larger than their body so it shows round them. It sinks in from above as it arrives,
            // leans half out of them toward the pull in a surge, swells and burns on the tell, climbs out as it leaves.
            bool hideOwn = HiddenFromThisViewer();
            float breathe = reduced ? 0f : Mathf.Sin(now * 2.2f + seat) * .03f;
            float swell = 1f + .12f * tell + breathe;
            // It lunges half out toward the pull in a surge; as the surge lets go it is SUCKED back in: a hard snap with a squash.
            float after = possessed ? SuckAfterSurge(elapsed, seat) : 0f;
            Vector3 lean = dir * (.5f * surge * _width);
            swell *= 1f - .14f * after;
            float enter = (1f - settle) * (_top + .6f), exit = Ease(leave) * (_top + .4f);
            var g = _ghost.T;
            g.position = feet + Vector3.up * (enter + exit - .04f) + lean;
            Vector3 face = Vector3.Slerp(forward, new Vector3(dir.x, 0f, dir.z), .6f * surge);
            g.rotation = Quaternion.LookRotation(face.sqrMagnitude > 1e-4f ? face : forward, Vector3.up)
                         * Quaternion.Euler(22f * surge - 6f * tell, 0f, 0f);
            // Only a little larger than them: the glow hugs their outline. In a surge it is half out, so it shows whole.
            float hug = 1.07f + .1f * surge;
            g.localScale = new Vector3(_width * hug / .44f * swell, _top * 1.04f / .62f * (1f + .05f * tell), _width * hug / .44f * swell);
            float flicker = reduced || !_motor.PossessionResisting ? 1f : .7f + .3f * Mathf.Sin(now * 29f);
            float aura = settle * (1f - leave) * Mathf.Lerp(.45f, 1f, gripLook) * flicker;
            Presence(_ghost, hideOwn ? 0f : aura, (1.1f + 1.0f * tell + .5f * surge + 1.2f * after) * (reduced ? .6f : 1f));

            // ITS HANDS. An escape pushes one out through the body on its row's side and sucks it back; a surge drives the other
            // out ahead of the lean, reaching toward where it is dragging them.
            Vector4 attempt = Vector4.zero;
            float escape = possessed ? EscapeAt(elapsed, seat, out attempt) : 0f;
            Vector3 escapeDir = Quaternion.AngleAxis(attempt.y, Vector3.up) * forward;
            Reach(_handA, feet + Vector3.up * (_top * attempt.z), escapeDir, attempt.w * escape, hideOwn ? 0f : aura * Mathf.Clamp01(escape * 3f));
            Reach(_handB, feet + Vector3.up * (_top * .55f) + lean, dir, (.45f * _width) * surge, hideOwn ? 0f : aura * Mathf.Clamp01(surge * 2f));

            // POSSESSED EYES over theirs, on the front of the head.
            Vector3 headAt = headBone != null ? headBone.position : feet + Vector3.up * (_top - .4f);
            float headSize = Mathf.Clamp(feet.y + _top - headAt.y, .35f, 1.1f);
            Vector3 eyes = headAt + Vector3.up * (_eyeHeight * headSize) + forward * (_faceDepth + .02f);
            float eyeGap = .17f * headSize;
            float blink = reduced ? 1f : 1f - .85f * Mathf.Clamp01(1f - Mathf.Abs(Mathf.Repeat(now + seat * .7f, 2.9f) - 1.4f) / .06f);
            float eyeSize = .13f * headSize * (1f + .5f * tell);
            _eyeL.T.position = eyes - side * eyeGap; _eyeR.T.position = eyes + side * eyeGap;
            _eyeL.T.localScale = _eyeR.T.localScale = new Vector3(eyeSize, eyeSize * blink, eyeSize);
            float eyeLight = hideOwn ? 0f : settle * (1f - leave) * (1.1f + 1.2f * tell + .6f * surge) * (reduced ? .6f : 1f);
            Light(_eyeL, eyeLight); Light(_eyeR, eyeLight);

            // The moths circle the head, and streak ahead along the pull in a surge.
            Vector3 mothCentre = feet + Vector3.up * (_top + .1f);
            for (int i = 0; i < 2; i++)
            {
                var o = MothOrbits[i];
                float a = (o.w + o.z * now) * Mathf.Deg2Rad;
                Vector3 orbit = mothCentre + new Vector3(Mathf.Cos(a) * o.x, o.y + Mathf.Sin(now * 1.3f + i) * .08f, Mathf.Sin(a) * o.x);
                Vector3 streak = mothCentre + dir * (.9f + .25f * i) + Vector3.up * (.1f - .15f * i);
                var moth = _mothBodies[i];
                moth.position = Vector3.Lerp(orbit, streak, surge) + Vector3.up * exit;
                Vector3 heading = Vector3.Lerp(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * Mathf.Sign(o.z), dir, surge);
                if (heading.sqrMagnitude > 1e-4f) moth.rotation = Quaternion.LookRotation(heading, Vector3.up);
                float beat = reduced ? 20f : Mathf.Sin(now * MothBeats[i] * 6.283f) * 55f;
                _moths[i * 2].T.localRotation = Quaternion.Euler(0f, 0f, beat);
                _moths[i * 2 + 1].T.localRotation = Quaternion.Euler(0f, 0f, -beat);
                bool on = settle * (1f - leave) > .05f;
                foreach (var w in moth.GetComponentsInChildren<Renderer>()) w.enabled = on;
            }

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
                Presence(_prints[i], printLevel * step);
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

        /// <summary>A ghost hand pushed out from inside the body: rooted at <paramref name="from"/>, pointing out, reaching.</summary>
        private void Reach(Part hand, Vector3 from, Vector3 outward, float reach, float alpha)
        {
            outward.y = 0f;
            if (outward.sqrMagnitude < 1e-4f || reach <= .01f || alpha <= .01f) { Presence(hand, 0f); return; }
            outward.Normalize();
            // The sleeve starts inside the body (half its width in) so it reads as coming OUT of them, not floating beside.
            hand.T.position = from + outward * (_width * .25f);
            hand.T.rotation = Quaternion.LookRotation(outward + Vector3.up * .15f, Vector3.up);
            float length = _width * .25f + reach;
            hand.T.localScale = new Vector3(1.5f, 1.5f, length / .283f);
            Presence(hand, alpha, .95f);
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
        private float _top = 1.7f, _width = .7f, _faceDepth = .3f, _eyeHeight = .3f;

        /// <summary>
        /// The body's height above its feet, its width and how far the face is in front of the head bone, from its own
        /// surfaces (the cast's heads, hair and builds differ a lot). The held slipper is left out.
        /// </summary>
        private void Measure(Transform model)
        {
            _measuredModel = model; _head = null;
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
            if (_head != null)
            {
                // The face is the front of the head's own surfaces: the renderers skinned or parented under the head.
                Vector3 fwd = _motor.transform.forward; fwd.y = 0f; fwd.Normalize();
                float front = 0f; bool found = false;
                foreach (var r in _head.GetComponentsInChildren<Renderer>(false))
                {
                    if (r == null || r.GetComponentInParent<Slipper>() != null) continue;
                    float d = Vector3.Dot(r.bounds.center - _head.position, fwd) + Mathf.Abs(Vector3.Dot(r.bounds.extents, new Vector3(Mathf.Abs(fwd.x), 0f, Mathf.Abs(fwd.z))));
                    front = found ? Mathf.Max(front, d) : d; found = true;
                }
                if (found) _faceDepth = Mathf.Clamp(front, .12f, .6f);
            }
        }
    }
}
