using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️⚠️ THE RIDER: what POSSESSED looks like to everyone (Nemu's PRUSISYON, `docs/reports/nemu-ultimate-2026-10-11/
    /// direction.md` section 5). A Kandila soul, one of the procession's child-sized sheet ghosts, sits on the body's
    /// shoulders with its stubby hands over their eyes (the Jockey's silhouette), its candle floating beside it, two
    /// black moths (paru-paro, the souls' messengers) circling, and a short trail of faint footprints on the court
    /// AHEAD of them in the pull's direction. Every picture answers a question a player has:
    ///
    ///  * who is possessed: the rider and the moths;
    ///  * which way they are dragged: the rider leans and reaches that way, and the footprints walk that way;
    ///  * is a surge coming: it rears back and its candle flares, the footprints brighten (`NemuRules.TellAmount`);
    ///  * is it surging: it thrusts forward and the moths streak ahead (`NemuRules.SurgeEnvelope`);
    ///  * is the player winning: it thins and its candle gutters as grip falls (exact on the simulating peer);
    ///  * when does it end: the last 1.2 s it climbs out, its candle snuffs and a smoke thread rises.
    ///
    /// ⚠️ PRESENTATION ONLY. Everything is posed from the replicated POSSESSED clock and positions through the same pure
    /// function the simulating peer steers with (`CharacterMotor.PossessionPull`), so every screen leans it the same way.
    /// It never moves the body. ⚠️ Hidden from its own victim in first person (it would sit in their lens); their screen
    /// gets the hands instead (`UI.PossessedScreen`). The footprints stay: they help the victim read it too.
    /// ⚠️ Typed part by part (`docs/HERO_KIT_METHOD.md` section 0.2): no part is stamped from a loop of one shape.
    /// </summary>
    public sealed class PossessedRider : MonoBehaviour
    {
        // The procession's palette (direction.md section 1): pale lilac grey bodies, her violet eyes, candle amber.
        private static readonly Color Sheet = new Color(.84f, .80f, .90f, .62f);
        private static readonly Color SheetShade = new Color(.70f, .66f, .80f, .62f);
        private static readonly Color Eye = new Color(.46f, .22f, .70f, 1f);
        private static readonly Color Wax = new Color(.95f, .90f, .78f, 1f);
        private static readonly Color Flame = new Color(1f, .62f, .18f, 1f);
        private static readonly Color MothWing = new Color(.07f, .05f, .09f, .92f);
        private static readonly Color Print = new Color(.55f, .52f, .64f, 1f);
        private static readonly Color Smoke = new Color(.62f, .60f, .66f, .5f);

        private sealed class Part
        {
            public Transform T;
            public Renderer R;
            public Color C;
            public bool Ghost;
            public MaterialPropertyBlock B;
        }

        private CharacterMotor _motor;
        private Transform _root, _body, _armL, _armR, _candle;
        private readonly List<Part> _sheetParts = new List<Part>(12);
        private Part _flame, _flameGlow, _smoke;
        private readonly Part[] _moths = new Part[4];
        private readonly Part[] _prints = new Part[4];
        private float _leaveAge = -1f, _snuffAge = -1f;
        private static Shader _glowShader, _ghostShader;

        // The two moths' orbits round the head: (radius, height, angular speed deg/s, phase deg), and their wing beats (Hz). Typed.
        private static readonly Vector4[] MothOrbits = { new Vector4(.55f, .25f, 140f, 0f), new Vector4(.70f, -.05f, -110f, 160f) };
        private static readonly float[] MothBeats = { 9f, 7.5f };
        // The footprints ahead of the feet: (distance along the pull, side offset, phase in the walk loop). Typed.
        private static readonly Vector3[] PrintRows = { new Vector3(.45f, -.09f, 0f), new Vector3(.80f, .09f, .25f), new Vector3(1.15f, -.08f, .5f), new Vector3(1.50f, .10f, .75f) };

        public static PossessedRider Attach(CharacterMotor motor)
        {
            if (motor == null || !Application.isPlaying) return null;
            var existing = motor.GetComponentInChildren<PossessedRider>(true);
            if (existing != null) { existing.Restart(); return existing; }
            var go = new GameObject("~PossessedRider");
            go.transform.SetParent(motor.transform, false);
            var rider = go.AddComponent<PossessedRider>();
            rider._motor = motor;
            rider.Build();
            return rider;
        }

        private void Restart() { _leaveAge = -1f; _snuffAge = -1f; }

        // ------------------------------------------------------------------ building

        /// <summary>A smooth piece (`ProcessionShapes`). `ghost` draws it as light (`ProcessionGhost`); otherwise it is a solid
        /// object in the scene's light (the wax, the moths) or an emissive one (the eyes, the flame).</summary>
        private Part Piece(Transform parent, string name, Mesh mesh, Vector3 at, Vector3 scale, Quaternion turn, Color colour,
                           bool ghost, float emission = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = at; go.transform.localScale = scale; go.transform.localRotation = turn;
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
                else VfxMaterial.Ghost(r, new Color(colour.r, colour.g, colour.b, .45f), .3f);
            }
            else VfxMaterial.Solid(r, colour, emission);
            return new Part { T = go.transform, R = r, C = colour, Ghost = ghost, B = new MaterialPropertyBlock() };
        }

        private Part Glow(Transform parent, string name, Color colour, float falloff, float core)
        {
            if (_glowShader == null) _glowShader = Resources.Load<Shader>("Shaders/SpiritGlow");
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name; VfxMaterial.StripCollider(go);
            go.transform.SetParent(parent, false);
            var r = go.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            if (_glowShader != null)
            {
                var m = new Material(_glowShader) { name = name };
                m.SetFloat("_Billboard", 1f); m.SetFloat("_Band", 0f); m.SetFloat("_Falloff", falloff);
                m.SetFloat("_Core", core); m.SetFloat("_Lift", 0f);
                r.sharedMaterial = m;
                VfxRenderTag.Own(go, m);
            }
            else VfxMaterial.Ghost(r, colour, 1f);
            return new Part { T = go.transform, R = r, C = colour, B = new MaterialPropertyBlock() };
        }

        private void Build()
        {
            _root = transform;
            _body = new GameObject("Soul").transform; _body.SetParent(_root, false);
            // THE SHEET: one smooth hooded drape with a rippling hem, a touch narrower front to back. Its mesh origin is the
            // hem, so it is lowered to sit its hood over the victim's head and its drape over their shoulders and back.
            var sheet = Piece(_body, "Sheet", ProcessionShapes.Soul, new Vector3(0f, -.33f, 0f), Vector3.one, Quaternion.identity, Sheet, true);
            sheet.R.sharedMaterial.SetFloat("_FadeLow", -.04f); sheet.R.sharedMaterial.SetFloat("_FadeHigh", .12f);
            _sheetParts.Add(sheet);
            // Its two eyes, her violet, set into the hood's face (+z, the way the victim faces). Not a matched pair: the right
            // sits a little lower and smaller, so the face reads as a face and not a pattern.
            _sheetParts.Add(Piece(_body, "EyeLeft", ProcessionShapes.Bead, new Vector3(-.055f, .18f, .128f), new Vector3(.045f, .062f, .02f), Quaternion.identity, Eye, false, 1.3f));
            _sheetParts.Add(Piece(_body, "EyeRight", ProcessionShapes.Bead, new Vector3(.058f, .172f, .127f), new Vector3(.04f, .056f, .02f), Quaternion.identity, Eye, false, 1.3f));
            // THE GRIP: two soft sleeves ending in mittens, reaching forward and down over the victim's eyes. Not mirrored: the
            // left reaches lower and the right holds higher, so it reads as clinging, not as a pair of handles.
            _armL = new GameObject("ArmLeft").transform; _armL.SetParent(_body, false); _armL.localPosition = new Vector3(-.14f, 0f, .07f);
            _armL.localRotation = Quaternion.Euler(40f, 14f, 0f);
            _sheetParts.Add(Piece(_armL, "Sleeve", ProcessionShapes.Sleeve, Vector3.zero, Vector3.one, Quaternion.identity, Sheet, true));
            _armR = new GameObject("ArmRight").transform; _armR.SetParent(_body, false); _armR.localPosition = new Vector3(.135f, .02f, .065f);
            _armR.localRotation = Quaternion.Euler(31f, -11f, 0f);
            _sheetParts.Add(Piece(_armR, "Sleeve", ProcessionShapes.Sleeve, Vector3.zero, new Vector3(.95f, .95f, .92f), Quaternion.identity, Sheet, true));
            // ITS CANDLE floats at its right shoulder: the family's accent, and its grip readout.
            _candle = new GameObject("Candle").transform; _candle.SetParent(_body, false); _candle.localPosition = new Vector3(.27f, .02f, -.06f);
            _sheetParts.Add(Piece(_candle, "Wax", ProcessionShapes.Candle, Vector3.zero, Vector3.one, Quaternion.identity, Wax, false, .15f));
            _flame = Piece(_candle, "Flame", ProcessionShapes.Flame, new Vector3(0f, .152f, 0f), Vector3.one, Quaternion.identity, Flame, false, 2.2f);
            _flameGlow = Glow(_candle, "FlameGlow", Flame, 2.6f, .85f);
            _flameGlow.T.localPosition = new Vector3(0f, .19f, 0f);
            _smoke = Piece(_candle, "Smoke", ProcessionShapes.Sleeve, new Vector3(0f, .17f, 0f), new Vector3(.25f, .25f, 1.4f), Quaternion.Euler(-90f, 0f, 0f), Smoke, true);
            _smoke.R.enabled = false;
            // THE MOTHS: two wings each, black, hinged at a small body.
            for (int i = 0; i < 2; i++)
            {
                var moth = new GameObject("Moth" + i).transform; moth.SetParent(_root, false);
                Piece(moth, "Body", ProcessionShapes.Bead, Vector3.zero, new Vector3(.018f, .018f, .06f), Quaternion.identity, MothWing, false);
                _moths[i * 2] = Piece(moth, "WingL", ProcessionShapes.Wing, Vector3.zero, new Vector3(-1f, 1f, 1f), Quaternion.identity, MothWing, false);
                _moths[i * 2 + 1] = Piece(moth, "WingR", ProcessionShapes.Wing, Vector3.zero, Vector3.one, Quaternion.identity, MothWing, false);
            }
            // THE FOOTPRINTS: four bare soles (the dead walk barefoot), laid on the court ahead of the feet. A right foot is a
            // left foot mirrored.
            for (int i = 0; i < PrintRows.Length; i++)
            {
                var print = Piece(_root, "Footprint" + i, ProcessionShapes.Sole, Vector3.zero,
                                  new Vector3(PrintRows[i].y < 0f ? 1f : -1f, 1f, 1f) * 1.25f, Quaternion.identity, Print, true);
                print.R.sharedMaterial.SetFloat("_FadeLow", -1f); print.R.sharedMaterial.SetFloat("_FadeHigh", -.9f);
                print.R.sharedMaterial.SetFloat("_RimAlpha", 0f);
                print.R.sharedMaterial.SetFloat("_Alpha", .55f);
                _prints[i] = print;
            }
        }

        // ------------------------------------------------------------------ posing

        private static float Ease(float u) { u = Mathf.Clamp01(u); return u * u * (3f - 2f * u); }

        private void SetAlpha(Part p, float alpha)
        {
            alpha = Mathf.Clamp01(alpha);
            if (p.Ghost) p.B.SetFloat("_Presence", alpha);
            p.R.SetPropertyBlock(p.B);
            p.R.enabled = alpha > .01f;
        }

        private void LateUpdate()
        {
            if (_motor == null) { Destroy(gameObject); return; }
            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            float now = Time.time;
            bool possessed = _motor.IsPossessed;
            float elapsed = possessed ? _motor.PossessionElapsed : NemuRules.PossessedSeconds;
            int seat = _motor.PlayerSlot;

            // The end: the last 1.2 s of the clock, or a cleanse/tag/round end that took it early.
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
            float grip = _motor.PossessionGrip;
            float gripLook = Mathf.InverseLerp(NemuRules.GripFloor, 1f, grip);

            // Seat it on the head, following the real head bone where there is one.
            var model = _motor.GetComponent<CharacterVisual>()?.Model;
            Transform headBone = model != null ? FindHead(model.transform) : null;
            Vector3 headAt = headBone != null ? headBone.position : _motor.transform.position + Vector3.up * 1.55f;
            Vector3 forward = _motor.transform.forward; forward.y = 0f;
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.forward;
            forward.Normalize();
            // It arrives from above (re-forming out of its wisp) and leaves upward.
            float rise = (1f - settle) * .6f + Ease(leave) * .9f;
            _body.position = headAt + Vector3.up * (.16f + rise) - forward * .1f;
            // It turns to face where it pulls (part way: it is still holding the head) and leans into it.
            Vector3 face = Vector3.Slerp(forward, new Vector3(dir.x, 0f, dir.z), .55f * Mathf.Clamp01(strength * 2f));
            if (face.sqrMagnitude < 1e-4f) face = forward;
            float pitch = 18f * strength - 16f * tell + 22f * surge;
            float bob = reduced ? 0f : Mathf.Sin(now * 2.6f + seat) * .02f;
            _body.rotation = Quaternion.LookRotation(face, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);
            _body.position += Vector3.up * bob;
            // The tell: it inhales (swells) and rears. The surge: its arms thrust.
            float swell = 1f + .12f * tell - .06f * surge;
            _body.localScale = new Vector3(swell, 1f + .08f * tell, swell);
            _armL.localPosition = new Vector3(-.14f, 0f, .07f + .07f * surge - .04f * tell);
            _armR.localPosition = new Vector3(.135f, .02f, .065f + .06f * surge - .03f * tell);

            // Its body thins as its grip falls; flickers while the player fights it; fades in and out.
            bool hideOwn = HiddenFromThisViewer();
            float flicker = reduced || !_motor.PossessionResisting ? 1f : .75f + .25f * Mathf.Sin(now * 31f);
            float bodyAlpha = settle * (1f - leave) * Mathf.Lerp(.45f, 1f, gripLook) * flicker;
            foreach (var p in _sheetParts) SetAlpha(p, hideOwn ? 0f : bodyAlpha);

            // The candle: steady, flaring on the tell, guttering low as grip falls, snuffed as it leaves.
            float snuff = Mathf.Clamp01((leave - .35f) / .15f);
            if (snuff > 0f && _snuffAge < 0f) _snuffAge = 0f;
            if (_snuffAge >= 0f) _snuffAge += Time.deltaTime;
            float gutter = reduced ? 1f : .85f + .15f * Mathf.Sin(now * 13f + seat * 2f) * Mathf.Sin(now * 7.3f);
            float flameSize = (1f + .9f * tell + .3f * surge) * Mathf.Lerp(.55f, 1f, gripLook) * gutter * (1f - snuff);
            _flame.T.localScale = Vector3.one * Mathf.Max(.001f, flameSize);
            SetAlpha(_flame, hideOwn ? 0f : settle * (1f - snuff));
            _flameGlow.T.localScale = Vector3.one * (.22f * flameSize + .001f);
            if (_flameGlow.R.sharedMaterial != null && _flameGlow.R.sharedMaterial.shader == _glowShader)
            {
                var c = Flame; c.a = hideOwn ? 0f : settle * (1f - snuff) * (.9f + 1.4f * tell) * (reduced ? .5f : 1f);
                _flameGlow.B.SetColor("_Color", c); _flameGlow.R.SetPropertyBlock(_flameGlow.B);
                _flameGlow.R.enabled = c.a > .01f;
            }
            else SetAlpha(_flameGlow, hideOwn ? 0f : settle * (1f - snuff));
            // A thread of smoke rises off the snuffed wick.
            float smoke = _snuffAge >= 0f ? Mathf.Clamp01(1f - _snuffAge / .9f) : 0f;
            _smoke.T.localPosition = new Vector3(0f, .17f + (_snuffAge >= 0f ? _snuffAge * .35f : 0f), 0f);
            SetAlpha(_smoke, hideOwn ? 0f : smoke * (reduced ? .5f : 1f));
            _candle.localPosition = new Vector3(.27f, .02f + (reduced ? 0f : Mathf.Sin(now * 1.9f + 1f) * .025f), -.06f);

            // The moths circle the head, and streak ahead along the pull in a surge.
            for (int i = 0; i < 2; i++)
            {
                var o = MothOrbits[i];
                var moth = _moths[i * 2].T.parent;
                float a = (o.w + o.z * now) * Mathf.Deg2Rad;
                Vector3 orbit = headAt + new Vector3(Mathf.Cos(a) * o.x, o.y + Mathf.Sin(now * 1.3f + i) * .08f, Mathf.Sin(a) * o.x);
                Vector3 streak = headAt + dir * (.9f + .25f * i) + Vector3.up * (.1f - .15f * i);
                Vector3 at = Vector3.Lerp(orbit, streak, surge) + Vector3.up * rise;
                Vector3 heading = Vector3.Lerp(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a)) * Mathf.Sign(o.z), dir, surge);
                moth.position = at;
                if (heading.sqrMagnitude > 1e-4f) moth.rotation = Quaternion.LookRotation(heading, Vector3.up);
                float beat = reduced ? 20f : Mathf.Sin(now * MothBeats[i] * 6.283f) * 55f;
                _moths[i * 2].T.localRotation = Quaternion.Euler(0f, 0f, beat);
                _moths[i * 2 + 1].T.localRotation = Quaternion.Euler(0f, 0f, -beat);
                float mothAlpha = settle * (1f - leave);
                foreach (var w in moth.GetComponentsInChildren<Renderer>()) w.enabled = mothAlpha > .05f;
            }

            // The footprints walk ahead of the feet the way it pulls, one after another, brighter as it gets ready to surge.
            Vector3 feet = _motor.transform.position;
            Vector3 side = new Vector3(dir.z, 0f, -dir.x);
            float printLevel = pulling ? (.18f + .3f * tell + .35f * surge) * settle * (1f - leave) : 0f;
            float printYaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            for (int i = 0; i < _prints.Length; i++)
            {
                var row = PrintRows[i];
                Vector3 at = feet + dir * row.x + side * row.y;
                at.y = Slipper.GroundY(at + Vector3.up * .4f) + .012f;
                _prints[i].T.position = at;
                _prints[i].T.rotation = Quaternion.Euler(0f, printYaw, 0f);
                // Each print appears in turn and fades, a walk that keeps leaving.
                float loop = Mathf.Repeat(now * (1.2f + 1.6f * surge) - row.z, 1f);
                float step = reduced ? 1f : Mathf.Clamp01(1f - loop * 1.6f);
                SetAlpha(_prints[i], printLevel * step);
            }
        }

        private bool HiddenFromThisViewer()
        {
            var cam = Camera.main;
            if (cam == null || _motor.PlayerSlot != NetAuthority.LocalSlot) return false;
            var rig = cam.GetComponent<CameraRig>();
            return rig != null && rig.IsFollowing(_motor) && rig.IsLocalFpp;
        }

        private Transform _foundHead;
        private Transform _searchedModel;
        private Transform FindHead(Transform model)
        {
            if (_searchedModel == model) return _foundHead;
            _searchedModel = model; _foundHead = null;
            foreach (var t in model.GetComponentsInChildren<Transform>(true))
                if (t.name == "head") { _foundHead = t; break; }
            return _foundHead;
        }
    }
}
