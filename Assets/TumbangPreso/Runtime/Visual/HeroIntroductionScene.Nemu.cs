using System.Collections.Generic;
using TumbangPreso.CameraSystem;
using UnityEngine;

namespace TumbangPreso.Visual
{
    public sealed partial class HeroIntroductionScene
    {
        // =========================================================================================
        // NEMU, LET THEM IN, 5.6 s (owner 2026-10-13; `docs/reports/nemu-ultimate-2026-10-11/direction.md` section 11).
        // v1 (PRUSISYON, a procession of lathed ghosts) was rejected whole: *"everything about it sucks"*, *"tangina nung
        // procession cringe na"*, *"hell i hate how the ghosts look"*, *"HINDI UNG KITA UNG GHHOST IS IN THEM"*.
        //
        // One sentence: she stops holding the dead back; her shadow floods the court, the dead claw up out of it and pour into
        // every player, and all that is left to see is the aura they cannot shake. The travelling thing is HER SHADOW: it leaves
        // her feet, runs out over the court and drains into the players.
        //
        //   A THE HUSH 0.00-1.10, close from her left: she looks up (0.15); finger to her lips (0.30); her eyes ignite violet
        //     (0.42) and the frame sinks into ink at its edges; smoke rises round her; a claw reaches up beside her (0.85); she
        //     lets her hand fall open: go (1.00).
        //   B THE FLOOD 1.10-2.90, low behind her right shoulder, craning up: the ink races out over the court; the dead claw up
        //     out of it with its edge in stop-motion jerks; every head snaps to the lens, every jaw drops and every arm reaches
        //     for it (2.35, held); they launch (2.55); one comes for the lens instead and its face fills the frame (2.47-2.88).
        //   C LET THEM IN 2.90-5.60, one continuous move riding them out: they pour into every player through the chest, five
        //     or six each, each tail spiralling in after it (3.10-3.75); the players jolt; the souls try to get out, a claw tears
        //     out of a back or a shoulder with the soul tearing half out behind it, screaming, and is yanked back (3.80-4.70);
        //     their eyes snap violet and they all turn to stare into the lens (4.00); the ink drains into them (4.20); what is
        //     left is the aura (`NemuAura`); hold; hand back with no black-out.
        // Preview v2 (owner: *"the ult cutscene looks so weird, the ghosts dont look that scary"*): the dead became gaunt and
        // towering with death-mask faces, stop-motion, the synchronised scream, the lunge, the souls tearing out, the stare.
        //
        // The dead (Fade's NIGHTFALL, research section 6): black smoke with a face and claws drawn in violet light. Every puff
        // and light is a particle laid out by hand from the scene clock (`NemuSmokeBatch`, `Shaders/NemuSmoke`).
        // ⚠️ THE PLAYERS ARE THE REAL ONES (`StageOthers`), animated with their own rigs' clips (`RootedMotion`). Nobody on
        // court: the dead dive into the ink at three typed spots. Play picks up with them possessed: never shown twice.
        // ⚠️ Nothing here runs on Update and nothing is random: every row is typed, every piece posed from the scene clock.
        // =========================================================================================
        private const float NmLook = .15f, NmHush = .30f, NmIgnite = .42f, NmSpread = .55f, NmTellClaw = .85f, NmGo = 1.0f;
        private const float NmFlood = 1.10f, NmFloodFull = 2.15f, NmSnap = 2.35f, NmLaunch = 2.55f, NmRide = 2.90f;
        private const float NmEnterFirst = 3.10f, NmEnterLast = 3.75f, NmFight = 3.80f, NmEyesOn = 4.00f, NmDrain = 4.20f;
        private const float NmSettle = 4.70f, NmRelease = 5.15f;
        /// <summary>
        /// THE LUNGE (owner on preview v2: *"the ghosts dont look that scary"*): after every head snaps to the lens, one of the
        /// dead comes for it, slow then all at once, jaw dropping, until its face fills the frame on the cut to C.
        /// </summary>
        private const float NmLungeFrom = 2.47f, NmLungeHit = 2.88f;
        /// <summary>How tall the lunging ghost is when its face fills the lens, and how far in front of the lens it stops.</summary>
        private const float NmLungeHeight = 3.6f, NmLungeStop = .8f;
        /// <summary>How far back in time each spine puff of a flying soul samples its path: its tail.</summary>
        private const float NmTailStep = .034f;
        private const int NmSpine = 9;

        /// <summary>A FILM OUTSIDE PLAY: the editor's cutscene film still names a companion here (no longer drawn by Nemu's).</summary>
        public static GhostPetCompanion FilmCompanion;

        /// <summary>
        /// The dead, typed one by one: (angle off the line to the players in degrees, + toward her right; metres out along it,
        /// before scaling to where the players are; size; seed). Near ones on the camera's side are big in the lens, so the
        /// mass reads as a crowd at every depth. Sorted near to far: the ink reaches them in this order.
        /// </summary>
        private static readonly Vector4[] NmDeadRows =
        {
            new Vector4(38f, 1.9f, 1.35f, .11f), new Vector4(-30f, 2.3f, 1.15f, .23f), new Vector4(12f, 2.9f, 1.60f, .37f),
            new Vector4(52f, 3.2f, 1.05f, .41f), new Vector4(-12f, 3.6f, 1.30f, .53f), new Vector4(28f, 4.1f, 1.75f, .61f),
            new Vector4(-44f, 4.3f, 1.00f, .07f), new Vector4(4f, 4.8f, 1.20f, .73f), new Vector4(44f, 5.0f, 1.40f, .19f),
            new Vector4(-24f, 5.4f, 1.55f, .83f), new Vector4(18f, 5.9f, 1.10f, .29f), new Vector4(-6f, 6.3f, 1.90f, .47f),
            new Vector4(36f, 6.6f, 1.25f, .91f), new Vector4(-38f, 6.8f, 1.35f, .13f), new Vector4(8f, 7.2f, 1.00f, .67f),
            new Vector4(-18f, 7.6f, 1.45f, .31f), new Vector4(26f, 8.0f, 1.20f, .57f), new Vector4(-30f, 8.3f, 1.05f, .79f),
            new Vector4(2f, 8.6f, 1.60f, .97f), new Vector4(48f, 8.9f, 1.10f, .43f),
        };
        /// <summary>The order the dead arrive in (index into <see cref="NmDeadRows"/>), so neighbours never land together.</summary>
        private static readonly int[] NmArrivalOrder = { 5, 0, 11, 2, 8, 14, 4, 17, 9, 1, 12, 6, 15, 3, 18, 10, 7, 16, 13, 19 };
        /// <summary>With nobody on court the dead dive into the ink here: (angle off her facing, metres out).</summary>
        private static readonly Vector2[] NmEmptyDives = { new Vector2(-30f, 4.2f), new Vector2(25f, 5.0f), new Vector2(160f, 3.6f) };
        /// <summary>
        /// The souls trying to get OUT (owner: *"the ghosts are tryna get out of the body but they get sucked in anyways"*):
        /// (seconds after their own fight begins, angle round the body from its facing, height as a share of it, reach in metres).
        /// Each player starts 0.11 s later round the list, so no two strain on the same frame.
        /// </summary>
        private static readonly Vector4[] NmEscapes =
        {
            new Vector4(.05f, 160f, .62f, .55f), new Vector4(.30f, -70f, .74f, .48f), new Vector4(.52f, 40f, .55f, .60f),
            new Vector4(.70f, -140f, .80f, .52f),
        };
        /// <summary>Smoke drifting past the lens in the close shot: (from, screen x from, x to, y, share of eye-to-focus, size).</summary>
        private static readonly (float From, float X0, float X1, float Y, float Near, float Size)[] NmLensSmoke =
        {
            (0f, -.9f, .9f, .42f, .5f, .22f), (.3f, .95f, -.8f, -.38f, .55f, .28f), (.65f, -.95f, .7f, .36f, .5f, .2f),
        };
        /// <summary>The smoke boiling off the ink's rim: (angle, seed, size, height phase). Typed, each its own.</summary>
        private const int NmRimPuffs = 30;

        private sealed class NmDead
        {
            public Vector3 Base;          // where it rises, scene space
            public float Scale, Height, Seed, Rise, Launch, Arrive;
            public int Target = -1;       // index into _nmVictims, or -1 - dive spot
            public bool Lunge;            // the one that comes for the lens (`NmLungeFrom`)
        }

        private sealed class NmVictim
        {
            public GameObject Holder;
            public Transform AnimRoot, Model, Head;
            public Transform[] Bones;
            public Quaternion[] Tmp;
            public Vector3[] TmpPos;
            public AnimationClip Shock, Struggle;
            public Vector3 Home, FaceLocal;
            public float Yaw, Top = 1.7f, Width = .7f, FaceDepth = .3f, FaceTop = .5f;
            public readonly List<float> Entries = new List<float>(8);
            public int Seat;
        }

        private readonly List<NmDead> _nmDead = new List<NmDead>(20);
        private readonly List<NmVictim> _nmVictims = new List<NmVictim>(3);
        private NemuSmokeBatch _nmSmoke;
        private int _nmInkPool, _nmInkRim;
        private float _nmReach = 8f;
        private Vector3 _nmWay = Vector3.forward, _nmCentre = new Vector3(0f, 0f, 6f);
        private Vector3 _nmEye = new Vector3(-.72f, 1.08f, 1.75f), _nmLook = new Vector3(0f, .98f, 0f);
        // B's last eye and look, where C's ride starts (one continuous move).
        private Vector3 _nmBEndEye, _nmBEndLook;
        // Her face, measured once from her own surfaces (`NmMeasureFace`).
        private Vector3 _nmFaceLocal = Vector3.forward;
        private float _nmFaceDepth = .3f, _nmHeadTop = 1.2f;
        private Material _nmInk;

        // ------------------------------------------------------------------ build

        private void BuildNemu(CharacterMotor source)
        {
            _nmSmoke = new NemuSmokeBatch(_root.transform, "NmDead", 1600, 2000, 36);

            // HER SHADOW: a pool of ink with a violet hairline at its rim. It leaves her feet in A and floods the court in B.
            _nmInkPool = Add("NmInkPool", VfxShapes.Splat(28, .22f, 731), new Color(.012f, .004f, .022f, .93f), .02f, plain: true);
            ZackUnlitBackdrop(_nmInkPool);
            _nmInkRim = Add("NmInkRim", VfxShapes.Collar(56, .018f, .955f), new Color(.52f, .20f, .92f, .85f), .9f);

            // Her face, from her own head's surfaces.
            if (_head != null) NemuAura.MeasureFace(_head, _bodyRenderers, _root.transform.forward, out _nmFaceLocal, out _nmFaceDepth, out _nmHeadTop);

            // THE REAL PLAYERS. They are seen from the flood on (B), standing where they stood.
            StageOthers(3.0f, 8.5f);
            Vector3 centre = Vector3.zero; float far = 0f;
            for (int i = 0; i < _others.Count; i++)
            {
                _nmVictims.Add(BuildVictim(_others[i], i));
                centre += _others[i].Home; far = Mathf.Max(far, _others[i].Reach);
            }
            if (_nmVictims.Count > 0)
            {
                centre /= _nmVictims.Count; centre.y = 0f;
                _nmCentre = centre;
                if (centre.sqrMagnitude > .04f) _nmWay = centre.normalized;
                _nmReach = Mathf.Max(5.5f, far + 1.2f);
            }

            // THE DEAD, along the line to the players, scaled to where they stand (between her and them, a few beyond).
            float spread = Mathf.Clamp(_nmReach / 9.5f, .6f, 1.15f);
            for (int i = 0; i < NmDeadRows.Length; i++)
            {
                var row = NmDeadRows[i];
                float d = row.y * spread;
                var dead = new NmDead
                {
                    Base = Quaternion.Euler(0f, row.x, 0f) * _nmWay * d,
                    // Preview v2: at 1.35 a player's height they read as small black pets. The dead TOWER over the living.
                    Scale = row.z, Height = 2.0f * row.z, Seed = row.w,
                };
                // It claws up as the ink's edge passes it.
                dead.Rise = NmFlood + (NmFloodFull - NmFlood) * Mathf.Clamp01(d / _nmReach) + .04f;
                _nmDead.Add(dead);
            }
            for (int k = 0; k < NmArrivalOrder.Length; k++)
            {
                var dead = _nmDead[NmArrivalOrder[k]];
                float u = k / (float)(NmArrivalOrder.Length - 1);
                dead.Arrive = Mathf.Lerp(NmEnterFirst, NmEnterLast, u);
                dead.Launch = NmLaunch + .16f * u;
                dead.Target = _nmVictims.Count > 0 ? k % _nmVictims.Count : -1 - (k % NmEmptyDives.Length);
                if (dead.Target >= 0) _nmVictims[dead.Target].Entries.Add(dead.Arrive);
            }
            // The one that comes for the lens: near the line, among the first to rise, never sent into a player.
            {
                float d = 3.2f * spread;
                var lunger = new NmDead { Base = Quaternion.Euler(0f, -6f, 0f) * _nmWay * d, Scale = 1.4f, Height = 2.8f, Seed = .58f, Lunge = true };
                lunger.Rise = NmFlood + (NmFloodFull - NmFlood) * Mathf.Clamp01(d / _nmReach) + .04f;
                lunger.Launch = 99f; lunger.Arrive = 99f; lunger.Target = -1;
                _nmDead.Add(lunger);
            }
        }

        private NmVictim BuildVictim(Other other, int index)
        {
            var v = new NmVictim { Holder = other.Holder, Home = other.Home, Yaw = other.Yaw, Seat = index };
            CharacterMotor live = null;
            var round = GameServices.Round;
            if (round != null)
                foreach (var p in round.Players)
                    if (p != null && other.Holder.name == "IntroductionOther-P" + (p.PlayerSlot + 1)) { live = p; v.Seat = p.PlayerSlot; break; }
            var animator = live != null ? live.GetComponent<CharacterVisual>()?.Model?.GetComponentInChildren<Animator>() : null;
            if (animator != null)
            {
                foreach (var t in other.Holder.GetComponentsInChildren<Transform>(true)) if (t.name == animator.transform.name) { v.AnimRoot = t; break; }
                var set = GeneratedMotionAssets.For(RootedMotion.Folder, DanceClip.ResourceName(animator.transform));
                if (set != null && set.Clips != null)
                    foreach (var clip in set.Clips)
                    {
                        if (clip == null) continue;
                        if (clip.name == RootedMotion.Breakout) v.Shock = clip;
                        else if (clip.name == RootedMotion.Struggle) v.Struggle = clip;
                    }
            }
            if (other.Holder.transform.childCount > 0) v.Model = other.Holder.transform.GetChild(0);
            foreach (var t in other.Holder.GetComponentsInChildren<Transform>(true)) if (t.name == "head") { v.Head = t; break; }
            v.Bones = other.Holder.GetComponentsInChildren<Transform>(true);
            v.Tmp = new Quaternion[v.Bones.Length]; v.TmpPos = new Vector3[v.Bones.Length];
            bool any = false; Bounds b = default;
            foreach (var r in other.Renderers) { if (r == null) continue; if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds); }
            if (any)
            {
                v.Top = Mathf.Clamp(b.max.y - other.Holder.transform.position.y, .9f, 2.6f);
                v.Width = Mathf.Clamp(Mathf.Max(b.size.x, b.size.z), .4f, 1.2f);
            }
            if (v.Head != null) NemuAura.MeasureFace(v.Head, other.Renderers, other.Holder.transform.forward, out v.FaceLocal, out v.FaceDepth, out v.FaceTop);
            // Hidden until the flood: switched off at the holder (`SetVisibleForCapture` would turn renderers back on).
            other.Holder.SetActive(false);
            return v;
        }

        // ------------------------------------------------------------------ sample

        private Vector3 NmW(Vector3 local) => _root.transform.TransformPoint(local);
        private Vector3 NmWDir(Vector3 local) => _root.transform.TransformDirection(local);

        /// <summary>The ink's radius at this moment: it leaves her feet in A and floods out to past the players in B.</summary>
        private float NmInkRadius(float t)
        {
            float spread = Ease(NmSpread, NmFlood, t) * .9f;
            float u = Mathf.Clamp01((t - NmFlood) / (NmFloodFull - NmFlood));
            float flood = 1f - (1f - u) * (1f - u); // races out, then slows as it reaches them
            return Mathf.Lerp(spread, _nmReach, flood);
        }

        private void SampleNemu(float t)
        {
            _nmSmoke.Begin();
            float present = StagePresence(t, .2f);
            Vector3 up = Vector3.up;

            // ------------------------------------------------------------- her shadow: the pool and its rim
            float drain = Ease(NmDrain, NmDrain + .55f, t);
            float radius = NmInkRadius(t);
            float pool = Ease(NmSpread, NmSpread + .2f, t) * (1f - drain) * present;
            Place(_nmInkPool, up * .022f, new Vector3(radius * 1.08f, 1f, radius * 1.08f), Quaternion.Euler(0f, 23f, 0f), pool);
            Place(_nmInkRim, up * .03f, new Vector3(radius, 1f, radius), Quaternion.identity, pool * (.5f + .5f * Ease(NmSpread, NmFlood, t)));
            // Smoke boiling off its edge, densest while it is moving.
            float moving = Ease(NmSpread, NmSpread + .2f, t) * (1f - Ease(NmFloodFull, NmFloodFull + .5f, t) * .6f) * (1f - drain);
            for (int j = 0; j < NmRimPuffs && radius > .25f; j++)
            {
                float a = (j * 12f + (j % 3) * 4.5f) * Mathf.Deg2Rad;
                float phase = Mathf.Repeat(t * .8f + j * .377f, 1f);
                Vector3 at = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * (radius * (.97f + .05f * Mathf.Sin(j * 2.3f)))
                           + up * (.1f + phase * .55f);
                _nmSmoke.Puff(NmW(at), (.55f + .35f * Mathf.Repeat(j * .61f, 1f)) * Mathf.Lerp(.5f, 1f, Mathf.Clamp01(radius / 3f)),
                              moving * .6f * Mathf.Sin(phase * Mathf.PI), j * .0371f, phase * .6f, .9f, j * 31f);
            }
            // In A the shadow is under the frame: smoke rises past her from it.
            float aRise = Ease(NmSpread, NmSpread + .25f, t) * (1f - Ease(NmFlood + .2f, NmFlood + .6f, t));
            // (film r1: rising to her head it swallowed her face in the close shot; it stays below her chest now.)
            for (int j = 0; j < 6; j++)
            {
                float phase = Mathf.Repeat(t * .9f + j * .29f, 1f);
                float a = (150f + j * 44f) * Mathf.Deg2Rad;
                Vector3 at = new Vector3(Mathf.Sin(a) * .6f, .05f + phase * .55f, Mathf.Cos(a) * .45f);
                _nmSmoke.Puff(NmW(at), .26f + .18f * phase, aRise * .6f * Mathf.Sin(phase * Mathf.PI), .5f + j * .07f, phase * .7f, .9f, j * 53f);
            }

            // ------------------------------------------------------------- her eyes, the one hot light
            if (_head != null)
            {
                Vector3 look = _head.rotation * _nmFaceLocal; look.y = 0f;
                if (look.sqrMagnitude < 1e-4f) look = _root.transform.forward;
                look.Normalize();
                // (film r11: at a quarter of the head they sat on her bangs, above her eyes.)
                Vector3 eyes = _head.position + Vector3.up * (_nmHeadTop * .13f) + look * (_nmFaceDepth + .015f);
                float lit = Ease(NmIgnite, NmIgnite + .08f, t) * (1f + 1.2f * Flash(t, NmIgnite + .06f, .12f)) * (1f - Ease(NmRelease, Seconds, t));
                // (film r10: at .09 they sat on her bangs as two big violet almonds.)
                NemuAura.Eyes(_nmSmoke, eyes, look, _nmHeadTop * .30f, .062f, lit * 2.1f, NmW(_nmEye));
            }

            // ------------------------------------------------------------- the claw that rises beside her (the tell)
            {
                float up1 = Ease(NmTellClaw - .12f, NmTellClaw + .08f, t), down = Ease(NmGo, NmFlood + .05f, t);
                float amount = up1 * (1f - down);
                Vector3 root = new Vector3(-.55f, .05f + 1.0f * up1 - .9f * down, .35f);
                Vector3 reach = (up + new Vector3(.25f, 0f, .15f)).normalized;
                NemuAura.Claw(_nmSmoke, NmW(root), NmWDir(reach), .35f, .7f, amount, t, .21f);
            }

            // ------------------------------------------------------------- smoke drifting past the lens (A)
            {
                Vector3 eye = _nmEye, focus = _nmLook;
                Vector3 toward = (focus - eye).normalized;
                Vector3 right = Vector3.Cross(up, toward).normalized;
                foreach (var m in NmLensSmoke)
                {
                    float u = Mathf.Clamp01((t - m.From) / 1.1f);
                    if (t < m.From || t > m.From + 1.1f || t > NmFlood) continue;
                    Vector3 at = eye + toward * (Vector3.Distance(eye, focus) * m.Near) + right * Mathf.Lerp(m.X0, m.X1, u) * .6f + up * (m.Y * .4f);
                    _nmSmoke.Puff(NmW(at), m.Size, .5f * Mathf.Sin(u * Mathf.PI), m.From + .3f, u * .5f, .8f, u * 90f);
                }
            }

            // ------------------------------------------------------------- the dead
            for (int i = 0; i < _nmDead.Count; i++) SampleDead(_nmDead[i], t);

            // ------------------------------------------------------------- the players
            for (int i = 0; i < _nmVictims.Count; i++) SampleVictim(_nmVictims[i], i, t);

            _nmSmoke.End(t);
        }

        /// <summary>Where a soul goes in: a staged player's chest, or (nobody there) the court at a typed spot.</summary>
        private Vector3 NmTargetPoint(int target, out float width)
        {
            width = .7f;
            if (target >= 0 && target < _nmVictims.Count)
            {
                var v = _nmVictims[target]; width = v.Width;
                return v.Home + Vector3.up * (v.Top * .52f);
            }
            var dive = NmEmptyDives[Mathf.Clamp(-1 - target, 0, NmEmptyDives.Length - 1)];
            return Quaternion.Euler(0f, dive.x, 0f) * Vector3.forward * dive.y;
        }

        /// <summary>The head of a soul at <paramref name="time"/>: standing over its spot, then its flight, then in them.</summary>
        private Vector3 NmPath(NmDead d, float time, Vector3 rest, Vector3 target)
        {
            if (time <= d.Launch) return rest;
            if (time >= d.Arrive) return target;
            float u = (time - d.Launch) / (d.Arrive - d.Launch);
            float e = u * u * (3f - 2f * u);
            // A swoop: up and over, curving to its own side, then down into the chest.
            Vector3 flat = target - rest; flat.y = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, flat.sqrMagnitude > 1e-4f ? flat.normalized : _nmWay);
            Vector3 mid = (rest + target) * .5f + Vector3.up * (.9f + .1f * flat.magnitude) + side * ((d.Seed - .5f) * 2.4f);
            float a = 1f - e;
            return a * a * rest + 2f * a * e * mid + e * e * target;
        }

        private void SampleDead(NmDead d, float t)
        {
            if (t < d.Rise - .05f) return;
            float drainAll = Mathf.Clamp01((t - d.Arrive) / .42f);
            if (drainAll >= 1f) return;
            Vector3 up = Vector3.up;
            float s = d.Scale;
            // STOP-MOTION (preview v2: smooth swaying read as calm). The dead climb out of the ink in jerks and hold, and
            // their sway is stepped at about eight poses a second, each on its own beat; the shader twitches their heads.
            float smooth = Ease(d.Rise, d.Rise + .62f, t);
            float emerge = Mathf.Lerp(smooth, Mathf.Floor(smooth * 6f + .5f) / 6f, .7f);
            float fly = Ease(d.Launch, d.Launch + .2f, t);
            Vector3 target = NmTargetPoint(d.Target, out _);

            float stepped = Mathf.Floor(t * 8f + d.Seed * 5f) / 8f;
            float writhe = Mathf.Sin(stepped * 1.9f + d.Seed * 17f);
            Vector3 toLens = _nmEye - d.Base; toLens.y = 0f; toLens = toLens.sqrMagnitude > 1e-4f ? toLens.normalized : -_nmWay;
            // Before the snap each looks its own way (up, round, at the others); at 2.35 every head snaps to the lens at once.
            float snap = Ease(NmSnap - .03f, NmSnap + .06f, t);
            Vector3 wander = Quaternion.Euler(0f, 75f * Mathf.Sin(t * 1.3f + d.Seed * 9f) + 50f * (d.Seed - .5f), 0f) * toLens;
            Vector3 standFace = Vector3.Slerp(wander, toLens, snap).normalized;
            Vector3 rest = d.Base + up * d.Height;

            // ---- ONE GHOST (`NemuSmokeBatch.Ghost`, `Shaders/NemuSmoke` mode 2). Owner on film r3: *"these doesnnt read as
            // ghosts anymroe"*: a column of puffs with dots of light was smoke, not the dead. Each is now one ghost's silhouette
            // (hood, body flowing into a tattered tail, raised claws, the burning face) made of the same black smoke.
            Vector3 head = NmPath(d, t, rest, target), before = NmPath(d, t - .05f, rest, target);
            Vector3 flightDir = (head - before).sqrMagnitude > 1e-6f ? (head - before).normalized : _nmWay;
            // Film r5: stretched by half and at full size in flight they became huge black birds over the lens. A little
            // stretch, and they gather smaller as they close on the player, so the dive reads as the dead going in.
            float flightU = Mathf.Clamp01((t - d.Launch) / Mathf.Max(.05f, d.Arrive - d.Launch));
            float stretch = .22f * fly * (1f - drainAll);
            float h = d.Height * Mathf.Lerp(1f, .55f, flightU * fly) * (1f - .7f * drainAll);
            // Standing: rising out of the ink (the part not risen yet is under the court, which hides it), hunched toward
            // where it looks. Flying: its head leads and the body streams behind.
            // On the snap they all lurch a step toward the lens at once.
            float lurch = Ease(NmSnap - .02f, NmSnap + .07f, t);
            Vector3 standCentre = d.Base + up * (d.Height * .5f - d.Height * (1f - emerge) * 1.05f) + standFace * (.08f * s) + toLens * (.45f * s * lurch);
            Vector3 flyCentre = head - flightDir * (h * (1f + stretch) * .26f);
            if (drainAll > 0f) flyCentre = Vector3.Lerp(flyCentre, target, drainAll);
            Vector3 centre = Vector3.Lerp(standCentre, flyCentre, fly);
            // On the lens: upright and swaying while it stands; turned so its head points where it flies.
            Vector3 cf = (_nmLook - _nmEye); cf = cf.sqrMagnitude > 1e-4f ? cf.normalized : Vector3.forward;
            Vector3 cr = Vector3.Cross(up, cf); cr = cr.sqrMagnitude > 1e-4f ? cr.normalized : Vector3.right;
            Vector3 cu = Vector3.Cross(cf, cr);
            float flyAngle = Mathf.Atan2(Vector3.Dot(flightDir, cr), Vector3.Dot(flightDir, cu)) * Mathf.Rad2Deg;
            float standAngle = writhe * 7f;
            // Swooping, not laid flat (film r8: horizontal they read as black birds).
            float angle = Mathf.LerpAngle(standAngle, flyAngle, fly * .55f) + 380f * drainAll;
            // Its face shows only toward the lens (a ghost turned away shows the back of its hood).
            Vector3 faceDir = fly > .5f ? flightDir : standFace; faceDir.y = 0f;
            faceDir = faceDir.sqrMagnitude > 1e-4f ? faceDir.normalized : toLens;
            Vector3 toEye = _nmEye - centre; toEye.y = 0f; toEye = toEye.sqrMagnitude > 1e-4f ? toEye.normalized : -faceDir;
            float facing = Mathf.Clamp01((Vector3.Dot(faceDir, toEye) + .2f) * 1.6f);
            float ignite = Ease(d.Rise + .25f, d.Rise + .4f, t);
            float flare = .72f + .28f * Mathf.Max(Flash(t, NmSnap + .06f, .2f) * snap, snap * .4f);
            float shown = Mathf.Clamp01(emerge * 1.8f) * (1f - drainAll);
            // Arms: clawing up out of the ink as it climbs, then hanging long at its sides; on the snap every one reaches for
            // the lens, and in flight they reach AHEAD (film r6: spread out they read as wings). Jaws: hanging a little open,
            // then every jaw drops at once on the held beat and stays screaming through the flight.
            float climbing = Mathf.Clamp01(1f - emerge) * Mathf.Clamp01(emerge * 4f);
            float hang = .1f + .1f * Mathf.Max(0f, writhe);
            float arms = Mathf.Max(Mathf.Lerp(hang, 1f, Mathf.Max(lurch, fly)), climbing * .8f) * (1f - .6f * drainAll);
            float scream = Mathf.Max(.15f + .15f * Mathf.Max(0f, -writhe), Mathf.Max(lurch, fly));
            if (d.Lunge && t >= NmLungeFrom)
            {
                // ---- THE LUNGE: still for a beat, then all at once at the lens; its face fills the frame and it goes on
                // THROUGH the lens into C (film r10: stopping and vanishing on the cut read as a pop, not a hit).
                float through = Mathf.Clamp01((t - NmLungeHit) / (NmRide + .12f - NmLungeHit));
                if (through >= 1f) return;
                float u = Mathf.Clamp01((t - NmLungeFrom) / (NmLungeHit - NmLungeFrom));
                float e = u * u * u;
                Vector3 toward = (_nmLook - _nmEye); toward = toward.sqrMagnitude > 1e-4f ? toward.normalized : _nmWay;
                float hit = Mathf.Lerp(d.Height, NmLungeHeight, e) * (1f + .3f * through);
                Vector3 onLens = _nmEye + toward * Mathf.Lerp(NmLungeStop, .2f, through) - up * (hit * .335f);
                Vector3 from = standCentre;
                Vector3 at = Vector3.Lerp(from, onLens, e) + up * (Mathf.Sin(u * Mathf.PI) * .4f);
                float twitchAngle = (Mathf.Repeat(Mathf.Floor(t * 12f) * .618f, 1f) - .5f) * 10f * (1f - e);
                _nmSmoke.Ghost(NmW(at), hit, 0f, twitchAngle, .97f, d.Seed, 1f, 1f, 1f);
                return;
            }
            _nmSmoke.Ghost(NmW(centre), h, stretch, angle, .95f * shown, d.Seed, facing * ignite * flare, arms, scream);

            // ---- smoke: wisps boiling off it where it stands in the ink; the trail it leaves in flight
            if (fly < 1f)
                for (int j = 0; j < 3; j++)
                {
                    float phase = Mathf.Repeat(t * .8f + j * .33f + d.Seed, 1f);
                    float a = (j * 120f + d.Seed * 200f) * Mathf.Deg2Rad;
                    Vector3 at = d.Base + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (.25f * s) + up * (.05f + phase * .5f * s);
                    _nmSmoke.Puff(NmW(at), .35f * s, .55f * (1f - fly) * Mathf.Clamp01(emerge * 3f) * Mathf.Sin(phase * Mathf.PI),
                                  d.Seed + j * .1f, phase * .6f, .8f, j * 70f);
                }
            if (fly > 0f)
                for (int k = 2; k < NmSpine; k++)
                {
                    float share = k / (float)(NmSpine - 1);
                    Vector3 path = NmPath(d, t - k * NmTailStep * (1f + .3f * s), rest, target);
                    if (drainAll > 0f)
                    {
                        Vector3 off = path - target;
                        path = target + Quaternion.AngleAxis(420f * drainAll, up) * off * Mathf.Pow(1f - drainAll, .7f);
                    }
                    _nmSmoke.Puff(NmW(path - flightDir * (h * .3f)), s * Mathf.Lerp(.42f, .16f, share),
                                  fly * Mathf.Lerp(.6f, .25f, share) * (1f - drainAll), d.Seed + k * .071f, share * .5f + drainAll * .4f,
                                  .85f, d.Seed * 360f + k * 41f + t * 25f);
                }

            // ---- the burst as it goes in: black smoke thrown out of the chest, a violet flash
            if (d.Target >= 0 && t >= d.Arrive && t < d.Arrive + .45f)
            {
                float u = (t - d.Arrive) / .45f;
                for (int j = 0; j < 7; j++)
                {
                    float a = (j * 51.4f + d.Seed * 90f) * Mathf.Deg2Rad;
                    Vector3 outDir = new Vector3(Mathf.Cos(a), .35f + .3f * Mathf.Sin(j * 1.7f), Mathf.Sin(a));
                    _nmSmoke.Puff(NmW(target + outDir * (.15f + .7f * u)), .28f + .3f * u, .7f * (1f - u), d.Seed + j * .09f, u * .8f, 1f, j * 50f);
                }
                _nmSmoke.Light(NmW(target), .9f * (1f - u * .5f), 1.4f * (1f - u) * (_reducedEffects ? .5f : 1f), NemuSmokeBatch.Violet);
            }
        }

        private void SampleVictim(NmVictim v, int index, float t)
        {
            bool on = t >= NmFlood;
            if (v.Holder.activeSelf != on) v.Holder.SetActive(on);
            if (!on) return;
            float first = v.Entries.Count > 0 ? v.Entries[0] : NmEnterFirst;
            float fightFrom = NmFight + .11f * index;

            // THE JOLTS, one per soul going in, and one per claw that tears out.
            float jolt = 0f;
            foreach (float a in v.Entries) jolt = Mathf.Max(jolt, Mathf.Clamp01(1f - Mathf.Abs(t - (a + .05f)) / .16f));
            float escapeJolt = 0f;
            foreach (var e in NmEscapes)
            {
                float at = fightFrom + e.x;
                escapeJolt = Mathf.Max(escapeJolt, Mathf.Clamp01(1f - Mathf.Abs(t - (at + .2f)) / .14f));
            }
            // THE STARE (preview v2: the close hold on a player in a haze read as nothing happening). Their eyes snap violet and
            // in the same beat every one of them stops fighting and turns its head and body to the lens, and keeps following it.
            float stareAt = NmEyesOn + .06f * index;
            float stare = Ease(stareAt, stareAt + .12f, t);
            float held = Ease(first + .1f, first + .35f, t) * (1f - .75f * stare);
            if (v.AnimRoot != null)
            {
                if (v.Struggle != null && held > 0f) NmBlend(v, v.Struggle, Mathf.Repeat((t - first) * 1.1f + .3f * index, v.Struggle.length), Mathf.Clamp01(held * .8f + escapeJolt * .3f));
                if (v.Shock != null && jolt > 0f) NmBlend(v, v.Shock, .12f + .015f * Mathf.Sin(t * 41f), jolt);
            }
            Vector3 toLens = _nmEye - v.Home; toLens.y = 0f;
            float lensYaw = toLens.sqrMagnitude > 1e-4f ? Mathf.Atan2(toLens.x, toLens.z) * Mathf.Rad2Deg : v.Yaw;
            float yaw = Mathf.LerpAngle(v.Yaw, lensYaw, stare);
            v.Holder.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;

            // THE AURA: what is left of the souls in them (`NemuAura`, the same drawing play keeps).
            float auraOn = Ease(first, first + .5f, t) * (1f - Ease(Seconds - .3f, Seconds, t) * .3f);
            float pulse = 0f;
            foreach (float a in v.Entries) pulse = Mathf.Max(pulse, Mathf.Clamp01(1f - (t - a) / .35f) * (t >= a ? 1f : 0f));
            var state = new NemuAura.State
            {
                Feet = NmW(v.Home), Forward = NmWDir(forward), Stream = Vector3.zero, Top = v.Top, Width = v.Width, Clock = t,
                Amount = auraOn, Swell = Mathf.Clamp01(pulse + escapeJolt * .6f), Drag = 0f, Thin = 0f, Seat = v.Seat, Reduced = _reducedEffects,
            };
            NemuAura.Draw(_nmSmoke, state);

            // THEY FIGHT IT: a claw tears out of a back, a shoulder, a chest, and is yanked back in.
            foreach (var e in NmEscapes)
            {
                float u = t - (fightFrom + e.x);
                if (u < 0f || u > .46f) continue;
                // (film r11: out in .26 s the souls were a faint smear; they strain out for .36 s now and are snapped back in .1.)
                float outU = u < .36f ? Mathf.SmoothStep(0f, 1f, u / .36f) : 1f - (u - .36f) / .1f;
                Vector3 dir = Quaternion.Euler(0f, e.y, 0f) * forward + Vector3.up * .25f;
                Vector3 root = v.Home + Vector3.up * (v.Top * e.z) + Quaternion.Euler(0f, e.y, 0f) * forward * (v.Width * .2f);
                NemuAura.Claw(_nmSmoke, NmW(root), NmWDir(dir), e.w * outU, .8f, Mathf.Clamp01(outU * 2f), t, e.y * .01f + index * .3f);
                // ...and the soul behind it tears half out, screaming, before it is dragged back in (the owner: *"the ghosts
                // are tryna get out of the body but they get sucked in anyways"*). Only in the cutscene, only for this instant:
                // in play nothing but the aura and its claws is ever drawn on them.
                Vector3 outDir = dir.normalized;
                float soulH = v.Top * Mathf.Lerp(.7f, 1.5f, outU);
                Vector3 soul = v.Home + Vector3.up * (v.Top * e.z) + outDir * (v.Width * .15f + outU * v.Top * .6f);
                Vector3 vf = _nmLook - _nmEye; vf = vf.sqrMagnitude > 1e-4f ? vf.normalized : Vector3.forward;
                Vector3 vr = Vector3.Cross(Vector3.up, vf); vr = vr.sqrMagnitude > 1e-4f ? vr.normalized : Vector3.right;
                Vector3 vu = Vector3.Cross(vf, vr);
                float lean = Mathf.Atan2(Vector3.Dot(outDir, vr), Vector3.Dot(outDir, vu)) * Mathf.Rad2Deg;
                _nmSmoke.Ghost(NmW(soul), soulH, .15f * outU, lean * .6f, Mathf.Clamp01(outU * 2.5f), .2f + index * .23f + e.x,
                               Mathf.Clamp01(outU * 2f), 1f, 1f);
                // The snap back: a puff where it went in.
                if (u > .36f) _nmSmoke.Puff(NmW(root + dir.normalized * (v.Width * .25f)), .35f, .6f * (1f - (u - .36f) / .1f), .3f + index * .1f, .4f, 1f, u * 400f);
            }

            // THEIR EYES snap violet, on their own face.
            float eyes = Ease(NmEyesOn + .06f * index, NmEyesOn + .06f * index + .08f, t)
                       * (1f + 1.2f * Flash(t, NmEyesOn + .06f * index + .06f, .12f));
            if (eyes > 0f)
            {
                Vector3 look; Vector3 eyeAt;
                if (v.Head != null)
                {
                    look = v.Head.rotation * v.FaceLocal; look.y = 0f;
                    look = look.sqrMagnitude > 1e-4f ? look.normalized : NmWDir(forward);
                    eyeAt = v.Head.position + Vector3.up * (v.FaceTop * .28f) + look * (v.FaceDepth + .015f);
                }
                else { look = NmWDir(forward); eyeAt = NmW(v.Home + Vector3.up * (v.Top * .78f) + forward * (v.Width * .5f)); }
                NemuAura.Eyes(_nmSmoke, eyeAt, look, v.FaceTop * .32f, .1f, eyes * 1.9f, NmW(_nmEye));
            }
        }

        private static void NmBlend(NmVictim v, AnimationClip clip, float time, float weight)
        {
            int n = v.Bones.Length;
            for (int i = 0; i < n; i++) { v.Tmp[i] = v.Bones[i].localRotation; v.TmpPos[i] = v.Bones[i].localPosition; }
            var holderPos = v.Holder.transform.localPosition; var holderRot = v.Holder.transform.localRotation;
            Vector3 modelPos = v.Model != null ? v.Model.localPosition : Vector3.zero;
            Quaternion modelRot = v.Model != null ? v.Model.localRotation : Quaternion.identity;
            clip.SampleAnimation(v.AnimRoot.gameObject, time);
            v.Holder.transform.localPosition = holderPos; v.Holder.transform.localRotation = holderRot;
            if (v.Model != null) { v.Model.localPosition = modelPos; v.Model.localRotation = modelRot; }
            if (weight >= .999f) return;
            for (int i = 0; i < n; i++)
            {
                v.Bones[i].localRotation = Quaternion.Slerp(v.Tmp[i], v.Bones[i].localRotation, weight);
                v.Bones[i].localPosition = Vector3.Lerp(v.TmpPos[i], v.Bones[i].localPosition, weight);
            }
        }

        // ------------------------------------------------------------------ camera and grade

        /// <summary>
        /// B and C are one continuous move, computed from where the players really are. B: low behind her right shoulder,
        /// craning up as the dead rise. C: from there out after the dead, ending beside the first player (2.4 m off, 50 degrees
        /// round from the line they come in on, never in their path), then easing back to hold every player.
        /// </summary>
        private void NemuFrame(int index, float t, ref Vector3 eye, ref Vector3 look, ref float fov)
        {
            if (_nmVictims.Count > 0 && index >= 1)
            {
                Vector3 way = _nmWay, right = Vector3.Cross(Vector3.up, way);
                float lookOut = Mathf.Clamp(_nmCentre.magnitude, 4f, 7.5f);
                Vector3 bEye0 = -way * 2.4f + right * 1.1f + Vector3.up * .8f, bEye1 = -way * 2.2f + right * 1.5f + Vector3.up * 2.3f;
                Vector3 bLook0 = way * (lookOut * .8f) + Vector3.up * 1.0f, bLook1 = way * lookOut + Vector3.up * .65f;
                _nmBEndEye = bEye1; _nmBEndLook = bLook1;
                if (index == 1)
                {
                    float u = Ease(NmFlood, NmRide, t);
                    eye = Vector3.Lerp(bEye0, bEye1, u); look = Vector3.Lerp(bLook0, bLook1, u); fov = Mathf.Lerp(50f, 54f, u);
                }
                else
                {
                    var first = _nmVictims[0].Home;
                    Vector3 line = first; line.y = 0f; line = line.sqrMagnitude > .01f ? line.normalized : way;
                    Vector3 beside = first + Quaternion.Euler(0f, 50f, 0f) * (-line) * 2.4f + Vector3.up * 1.5f;
                    float ride = Ease(NmRide, NmEnterFirst + .2f, t), back = Ease(NmSettle - .15f, NmRelease, t);
                    Vector3 off = beside - first; off.y = 0f; off = off.sqrMagnitude > .01f ? off.normalized : -way;
                    Vector3 settle = beside + off * 1.4f + Vector3.up * .35f;
                    Vector3 settleLook = Vector3.Lerp(first + Vector3.up * 1.0f, _nmCentre + Vector3.up * .9f, .45f);
                    eye = Vector3.Lerp(Vector3.Lerp(_nmBEndEye, beside, ride), settle, back);
                    look = Vector3.Lerp(Vector3.Lerp(_nmBEndLook, first + Vector3.up * 1.0f, ride), settleLook, back);
                    fov = Mathf.Lerp(Mathf.Lerp(54f, 48f, ride), 52f, back);
                    eye = KeepLensOffOthers(eye, 1.6f);
                    // Film r1: the ride's first frames went through her head. Keep the lens off her too, and above her.
                    Vector3 flat = new Vector3(eye.x, 0f, eye.z);
                    if (flat.magnitude < 1.8f) eye += (flat.sqrMagnitude > 1e-4f ? flat.normalized : -way) * (1.8f - flat.magnitude);
                    eye.y = Mathf.Max(eye.y, 1.6f);
                }
            }
            if (!_reducedEffects && index == 1)
            {
                // The lunge lands: the lens is knocked back.
                float hit = Mathf.Clamp01(1f - Mathf.Abs(t - NmLungeHit) / .06f);
                var knock = new Vector3(Mathf.Sin(t * 97f), Mathf.Sin(t * 89f + .7f), 0f) * (.05f * hit);
                eye += knock; look += knock * .4f;
            }
            if (!_reducedEffects && index == 2)
            {
                // A tremor with each soul that goes into the first player.
                float jolt = 0f;
                if (_nmVictims.Count > 0) foreach (float a in _nmVictims[0].Entries) jolt = Mathf.Max(jolt, Mathf.Clamp01(1f - Mathf.Abs(t - (a + .03f)) / .1f));
                var tremble = new Vector3(Mathf.Sin(t * 71f), Mathf.Sin(t * 83f + 1.3f), 0f) * (.035f * jolt);
                eye += tremble; look += tremble * .5f;
            }
            _nmEye = eye; _nmLook = look;
        }

        /// <summary>The ink at the frame's edges (`Shaders/NemuInk`): it breathes in when her eyes ignite and lets go at the end.</summary>
        private void NemuPostProcess(RenderTexture frame, Camera camera, float t)
        {
            float pulse = 0f;
            if (_nmVictims.Count > 0) foreach (float a in _nmVictims[0].Entries) pulse = Mathf.Max(pulse, Mathf.Clamp01(1f - Mathf.Abs(t - a) / .12f));
            float ink = Ease(NmIgnite, NmIgnite + .35f, t) * (.88f + .1f * pulse) * (1f - Ease(NmRelease, Seconds - .05f, t));
            if (ink <= .01f) return;
            if (_nmInk == null)
            {
                var shader = Resources.Load<Shader>("Shaders/NemuInk");
                if (shader == null) return;
                _nmInk = new Material(shader) { name = "NemuInk" };
            }
            _nmInk.SetFloat("_Ink", ink);
            _nmInk.SetFloat("_Clock", t);
            _nmInk.SetFloat("_Aspect", frame.height > 0 ? frame.width / (float)frame.height : 16f / 9f);
            var tmp = RenderTexture.GetTemporary(frame.descriptor);
            Graphics.Blit(frame, tmp, _nmInk);
            Graphics.Blit(tmp, frame);
            RenderTexture.ReleaseTemporary(tmp);
        }

        /// <summary>
        /// The world dims but keeps its colour (Black Swan: a dark frame with one hot violet). v1's saturation 0.38 turned every
        /// violet to glass, and film r3's 0.62 turned the eyes lilac: darkness comes from brightness alone. Back to the map's own grade as play returns.
        /// </summary>
        private void NemuGrade(float t, out float brightness, out float saturation)
        {
            float dark = Ease(NmIgnite, NmIgnite + .4f, t) * (1f - Ease(NmRelease, Seconds, t));
            // Preview v2: at .34 the plaza still read as an afternoon. Night falls with her eyes.
            brightness = Mathf.Lerp(1f, .22f, dark);
            saturation = Mathf.Lerp(1f, .92f, dark);
        }

        private void NemuDispose()
        {
            _nmSmoke?.Dispose(); _nmSmoke = null;
            if (_nmInk != null) { ObjectDestroy(_nmInk); _nmInk = null; }
        }
    }
}
