using System.Collections.Generic;
using TumbangPreso.Map;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>
    /// TRAILER, ACTS 1 AND 2, THE STREET (Eskinita / Tondo). See docs/trailer/TRAILER_PLAN.md and docs/trailer/ANIMATIC.md.
    /// Each shot owns a span of this film's clock; tools/trailer_animatic.py cuts the spans into trailer order and adds
    /// cards, grade and sound. The match's four seats are hidden: these shots belong to the neighbourhood.
    ///
    /// Acting is keyed full-body poses (`TrailerBody`, `Track`) laid over the cast's own clips: anticipation before
    /// every action, overshoot and settle after it, nobody frozen. The street is lit at golden hour (`OwnsLight`).
    ///
    /// THE SHOTS (film seconds):
    ///   can      0.0  2.4   Extreme close-up of the can on the court, low, a slow push in.
    ///   kid      2.4  4.8   Low behind the can and the crouched taya kid: Totoy tosses his tsinelas, catches, settles.
    ///   throw    4.8  8.0   Beside the can: dip, charge, whip, follow-through; the tsinelas spins into the lens in slow motion.
    ///   elder    8.0 11.6   Mang Kanor walks in, crouches, sets the tilted can upright, pats the taya kid's head.
    ///   rack    11.6 13.8   Signature tsinelas hung for sale at the sari-sari, swaying, a slow truck along them.
    ///   tv      13.8 19.0   The barangay round one TV; push over the heads to Totoy; the whole crowd erupts.
    ///   archive 19.0 22.2   (graded as 1974 newsreel) A man lifts the can overhead, the crowd jumps.
    ///   vhs     22.2 24.8   (graded as 1990s tape) Tikboy copies a famous throw; Bebang laughs at him.
    ///   windup  24.8 27.0   The same throw as `throw`, side on and close: dip, charge, whip (cut into the throw before release).
    /// </summary>
    public sealed class TrailerStreetFilm : MapFilm
    {
        public static readonly (string name, float from, float to)[] Shots =
        {
            ("can", 0.0f, 2.4f), ("kid", 2.4f, 4.8f), ("throw", 4.8f, 8.0f), ("elder", 8.0f, 11.6f),
            ("rack", 11.6f, 13.8f), ("tv", 13.8f, 19.0f), ("archive", 19.0f, 22.2f), ("vhs", 22.2f, 24.8f),
            ("windup", 24.8f, 27.0f),
        };
        public const float Length = 27.0f;

        protected override string Region => "TRAILER";
        protected override Color Accent => new Color(1.0f, 0.55f, 0.26f);
        protected override string Score => null;
        protected override float CloseAt(bool full) => 999.0f;
        protected override bool OwnsLight => true;

        /// <summary>Late gold: the sun low and warm, long shadows, lamps just coming up.</summary>
        private static readonly Mood Golden = new Mood
        {
            Sky = new Color(0.52f, 0.46f, 0.66f), Equator = new Color(0.78f, 0.54f, 0.44f), Ground = new Color(0.48f, 0.34f, 0.26f),
            Fog = new Color(0.98f, 0.70f, 0.52f), FogStart = 40.0f, FogEnd = 260.0f,
            Zenith = new Color(0.36f, 0.42f, 0.70f), Horizon = new Color(1.0f, 0.68f, 0.44f),
            Sun = new Color(1.0f, 0.66f, 0.38f), SunIntensity = 1.15f, SunElevation = 14.0f,
            Brightness = 1.0f, Saturation = 1.12f, Lamps = 1.3f, KeepYaw = true,
        };

        /// <summary>Blue hour for the TV: the sky gone, the screen and the shop lights doing the work.</summary>
        private static readonly Mood Evening = new Mood
        {
            Sky = new Color(0.30f, 0.30f, 0.52f), Equator = new Color(0.46f, 0.36f, 0.42f), Ground = new Color(0.28f, 0.22f, 0.22f),
            Fog = new Color(0.52f, 0.42f, 0.54f), FogStart = 25.0f, FogEnd = 190.0f,
            Zenith = new Color(0.14f, 0.16f, 0.36f), Horizon = new Color(0.86f, 0.48f, 0.42f),
            Sun = new Color(0.9f, 0.5f, 0.42f), SunIntensity = 0.45f, SunElevation = 5.0f,
            Brightness = 0.92f, Saturation = 1.1f, Lamps = 2.2f, KeepYaw = true,
        };

        private sealed class Actor
        {
            public Extra E;
            public TrailerBody B;
            public Vector3 Facing => E != null && E.Root != null ? E.Root.transform.forward : Vector3.forward;
        }

        private Actor _totoy, _taya, _kanor, _lola, _tikboy, _bebang, _berto;
        private readonly List<Actor> _crowd = new List<Actor>(), _ring = new List<Actor>();
        private GameObject _slipper, _rack, _tv, _can;
        private readonly List<Transform> _pairs = new List<Transform>();
        private Renderer _screen;
        private Light _screenLight;
        private Renderer[] _realCan = new Renderer[0];
        private Vector3 _store, _storeOut, _releaseFrom;
        private float _ground;

        private static readonly string[] Kids = { "jun_jun", "tikboy", "bebang", "maring", "inday" };
        private static readonly string[] Adults = { "kuya_boy", "ate_girlie", "aling_nena", "bayan", "mang_kanor", "lola_pacing" };

        protected override void Prepare()
        {
            _ground = Floor(Centre, Centre.y);
            FindStore();

            _totoy = Cast("Totoy", 0.64f, "totoy");
            _taya = Cast("Taya kid", 0.6f, "jun_jun");
            _kanor = Cast("Mang Kanor", 1.0f, "mang_kanor");
            _lola = Cast("Lola Pacing", 1.0f, "lola_pacing");
            _tikboy = Cast("Tikboy", 0.64f, "tikboy");
            _bebang = Cast("Bebang", 0.64f, "bebang");
            _berto = Cast("Berto", 1.0f, "bayan");
            for (int i = 0; i < 9; i++)
                _crowd.Add(Cast("TV crowd " + i, i % 3 == 0 ? 0.64f : 1.0f, i % 3 == 0 ? Kids[i % Kids.Length] : Adults[i % Adults.Length]));
            for (int i = 0; i < 18; i++)
                _ring.Add(Cast("Archive crowd " + i, i % 4 == 0 ? 0.64f : 1.0f, i % 4 == 0 ? Kids[i % Kids.Length] : Adults[(i * 5) % Adults.Length]));

            _slipper = SlipperModel("Totoy's tsinelas", 1.0f);
            BuildRack();
            BuildTv();
            var lata = GameServices.Round != null ? GameServices.Round.Lata : null;
            if (lata != null) { _can = Ghost(lata.gameObject, "Trailer can"); _realCan = lata.GetComponentsInChildren<Renderer>(); Hide(_realCan); }
        }

        private Actor Cast(string name, float scale, string id)
        {
            var e = Person(name, scale, id);
            return new Actor { E = e, B = e != null ? new TrailerBody(e.Model) : null };
        }

        private void FindStore()
        {
            _store = Centre + new Vector3(0.0f, 0.0f, 15.0f);
            foreach (var t in FindObjectsByType<Transform>())
                if (t.name.StartsWith("prop_sarisari_front")) { _store = t.position; break; }
            _storeOut = Centre - _store; _storeOut.y = 0.0f;
            _storeOut = _storeOut.sqrMagnitude > 0.01f ? _storeOut.normalized : Vector3.back;
            _store.y = Floor(_store + _storeOut * 2.0f, _ground);
        }

        private GameObject SlipperModel(string name, float scale)
        {
            var book = RosterBook.Load();
            var art = book != null ? book.SlipperArt(0) : null;
            GameObject model;
            if (art != null && art.Model != null)
            {
                model = Instantiate(art.Model, transform, false);
                foreach (var c in model.GetComponentsInChildren<Collider>(true)) Destroy(c);
                foreach (var b in model.GetComponentsInChildren<MonoBehaviour>(true)) b.enabled = false;
                var body = model.GetComponent<Rigidbody>(); if (body != null) Destroy(body);
                Visual.ToonSkin.ApplySlipper(model, Visual.ToonSkin.PersonOutlineWidth);
            }
            else model = Prop(PrimitiveType.Cube, name, Vector3.zero, Quaternion.identity, new Vector3(0.11f, 0.03f, 0.26f), new Color(0.2f, 0.45f, 0.9f));
            model.name = name;
            model.transform.localScale *= scale;
            Own(model);
            return model;
        }

        private void BuildRack()
        {
            _rack = Own(new GameObject("Tsinelas rack"));
            _rack.transform.SetParent(transform, false);
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            Vector3 origin = _store + _storeOut * 1.1f + Vector3.up * 1.75f;
            Prop(PrimitiveType.Cube, "Rack line", origin, Quaternion.LookRotation(side), new Vector3(0.02f, 0.02f, 3.0f), new Color(0.25f, 0.2f, 0.18f)).transform.SetParent(_rack.transform, true);
            for (int i = 0; i < 9; i++)
            {
                var hook = new GameObject("Rack hook " + i).transform;
                hook.SetParent(_rack.transform, false);
                hook.position = origin + side * (-1.2f + i * 0.3f);
                hook.rotation = Quaternion.LookRotation(-_storeOut);
                var pair = SlipperModel("Rack pair " + i, 0.5f);
                pair.transform.SetParent(hook, true);
                pair.transform.position = hook.position + Vector3.down * 0.17f;
                pair.transform.rotation = Quaternion.LookRotation(Vector3.down, _storeOut);
                foreach (var r in pair.GetComponentsInChildren<Renderer>())
                {
                    var block = new MaterialPropertyBlock();
                    r.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", Color.HSVToRGB(Mathf.Repeat(0.02f + i * 0.13f, 1.0f), 0.6f, 0.95f));
                    r.SetPropertyBlock(block);
                }
                _pairs.Add(hook);
            }
            _rack.SetActive(false);
        }

        private void BuildTv()
        {
            _tv = Own(new GameObject("Sari-sari TV"));
            _tv.transform.SetParent(transform, false);
            Vector3 at = _store + _storeOut * 2.2f;
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            Quaternion facing = Quaternion.LookRotation(_storeOut);
            Prop(PrimitiveType.Cube, "TV table", at + Vector3.up * 0.4f, facing, new Vector3(1.1f, 0.8f, 0.6f), new Color(0.45f, 0.3f, 0.2f)).transform.SetParent(_tv.transform, true);
            Prop(PrimitiveType.Cube, "TV body", at + Vector3.up * 1.15f, facing, new Vector3(0.95f, 0.7f, 0.55f), new Color(0.12f, 0.12f, 0.14f)).transform.SetParent(_tv.transform, true);
            Prop(PrimitiveType.Cube, "TV antenna L", at + Vector3.up * 1.68f + side * 0.15f, facing * Quaternion.Euler(0, 0, 25), new Vector3(0.02f, 0.5f, 0.02f), new Color(0.6f, 0.6f, 0.6f)).transform.SetParent(_tv.transform, true);
            Prop(PrimitiveType.Cube, "TV antenna R", at + Vector3.up * 1.68f - side * 0.15f, facing * Quaternion.Euler(0, 0, -25), new Vector3(0.02f, 0.5f, 0.02f), new Color(0.6f, 0.6f, 0.6f)).transform.SetParent(_tv.transform, true);
            _screen = Glow("TV screen", at + Vector3.up * 1.15f + _storeOut * 0.285f, new Vector2(0.78f, 0.55f), new Color(0.55f, 0.75f, 1.0f, 1.0f), _storeOut, Texture2D.whiteTexture);
            _screen.transform.SetParent(_tv.transform, true);
            _screenLight = Lamp("TV glow", at + Vector3.up * 1.2f + _storeOut * 0.7f, new Color(0.6f, 0.78f, 1.0f), 7.0f);
            _screenLight.transform.SetParent(_tv.transform, true);
            _tv.SetActive(false);
        }

        // ------------------------------------------------------------------ the frame

        private string _shot = "";

        protected override void Stage(float age)
        {
            for (int s = 0; s < Seats; s++) ShowSeat(s, false);
            string shot = Shots[Shots.Length - 1].name; float local = age - Shots[Shots.Length - 1].from;
            foreach (var s in Shots) if (age >= s.from && age < s.to) { shot = s.name; local = age - s.from; break; }
            Beat = shot;
            if (shot != _shot) { HideAll(); _shot = shot; }
            Tint(shot == "tv" ? Evening : Golden, 1.0f);

            switch (shot)
            {
                case "can": Can(local); break;
                case "kid": Kid(local); break;
                case "throw": Throw(local, false); break;
                case "elder": Elder(local); break;
                case "rack": Rack(local); break;
                case "tv": Tv(local); break;
                case "archive": Archive(local); break;
                case "vhs": Vhs(local); break;
                case "windup": Throw(local, true); break;
            }
        }

        private void HideAll()
        {
            foreach (var a in new[] { _totoy, _taya, _kanor, _lola, _tikboy, _bebang, _berto }) a?.E?.Show(false);
            foreach (var a in _crowd) a?.E?.Show(false);
            foreach (var a in _ring) a?.E?.Show(false);
            foreach (var g in new[] { _slipper, _rack, _tv, _can }) if (g != null) g.SetActive(false);
        }

        // ---- helpers

        private Vector3 On(Vector3 at) { at.y = Floor(at, _ground); return at; }

        private static float Yaw(Vector3 from, Vector3 to) { Vector3 d = to - from; d.y = 0.0f; return d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d).eulerAngles.y : 0.0f; }

        /// <summary>Stands `a` at `at` facing `yaw`, samples `clip` at `time`, lays `pose` on top.</summary>
        private static void Perform(Actor a, Vector3 at, float yaw, string clip, float time, in Pose pose)
        {
            if (a?.E == null) return;
            a.E.Show(true);
            a.E.Pose(at, yaw);
            a.B?.Reset();
            a.E.Play(clip, time);
            a.B?.Apply(pose, a.E.Root.transform);
        }

        /// <summary>Breathing and weight shift: a living idle so no one is ever a statue.</summary>
        private static Pose Alive(Pose p, float t, float seed, float amount = 1.0f)
        {
            p.Lean += 1.6f * amount * Mathf.Sin(t * 2.1f + seed);
            p.Roll += 2.2f * amount * Mathf.Sin(t * 0.9f + seed * 1.7f);
            p.Nod += 1.4f * amount * Mathf.Sin(t * 1.3f + seed * 0.6f);
            p.Turn += 4.0f * amount * (Mathf.PerlinNoise(t * 0.45f, seed) - 0.5f);
            return p;
        }

        /// <summary>A jump: crouch, launch, hang, land with a squash. `t` from the start of the crouch.</summary>
        private static float Jump(float t, float height, out float sink)
        {
            sink = 0.0f;
            if (t < 0.0f) return 0.0f;
            if (t < 0.12f) { sink = 0.09f * Mathf.Sin(t / 0.12f * Mathf.PI * 0.5f); return 0.0f; }
            float air = t - 0.12f, up = 0.46f;
            if (air < up) return height * 4.0f * (air / up) * (1.0f - air / up);
            float land = air - up;
            if (land < 0.18f) sink = 0.07f * Mathf.Sin(land / 0.18f * Mathf.PI);
            return 0.0f;
        }

        private static Vector3 Wobble(float t, float amount, float seed = 0.0f) =>
            new Vector3(Mathf.PerlinNoise(t * 0.7f, seed) - 0.5f, Mathf.PerlinNoise(seed + 3.1f, t * 0.8f) - 0.5f, Mathf.PerlinNoise(t * 0.6f, seed + 7.3f) - 0.5f) * amount;

        private void HoldCan(Vector3 at, Quaternion turn)
        {
            if (_can == null) return;
            _can.SetActive(true);
            _can.transform.SetPositionAndRotation(at, turn);
        }

        private void HoldSlipper(Vector3 at, Quaternion turn)
        {
            if (_slipper == null) return;
            _slipper.SetActive(true);
            _slipper.transform.SetPositionAndRotation(at, turn);
        }

        private void InHand(Actor a)
        {
            if (a?.B == null) return;
            Vector3 hand = a.B.Hand(out var pointing);
            HoldSlipper(hand + pointing * 0.06f, Quaternion.LookRotation(pointing, a.Facing));
        }

        // ---- 1 · can
        private void Can(float t)
        {
            HoldCan(Centre, Quaternion.identity);
            float p = Smooth(t / 2.4f);
            Vector3 focus = Centre + Vector3.up * 0.12f;
            Perform(_taya, On(Centre + new Vector3(0.4f, 0, -5.5f)), 0.0f, "idle", t, Alive(Pose.Ready, t, 1.0f));
            Shoot(Centre + new Vector3(Mathf.Lerp(-0.8f, -0.48f, p), 0.1f, Mathf.Lerp(1.25f, 0.82f, p)) + Wobble(t, 0.012f), focus + Vector3.up * 0.03f, 26.0f, -2.0f);
        }

        // ---- 2 · kid
        private void Kid(float t)
        {
            HoldCan(Centre, Quaternion.identity);
            Vector3 kid = On(Centre + new Vector3(-0.2f, 0, 7.5f));
            // Two tosses and catches, the hand dipping to receive each, the head following the shoe up and down.
            float toss = Mathf.Repeat(t, 0.95f) / 0.95f;
            int n = Mathf.FloorToInt(t / 0.95f);
            float air = Mathf.Clamp01((toss - 0.1f) / 0.75f);
            float rise = 4.0f * air * (1.0f - air);
            float catchDip = toss < 0.1f ? Mathf.Sin(toss / 0.1f * Mathf.PI) : toss > 0.85f ? Mathf.Sin((toss - 0.85f) / 0.15f * Mathf.PI) * 0.6f : 0.0f;
            var hold = new Pose { Arm = new Vector3(0.4f, -0.45f, 0.62f), Fore = new Vector3(0.05f, 0.35f + 0.25f * (1 - catchDip), 0.95f), Off = Pose.OffDown, OffFore = Pose.OffDownFore, Nod = -6.0f - 14.0f * rise, Sink = 0.03f * catchDip };
            if (t > 2.0f) hold = Pose.Mix(hold, new Pose { Arm = new Vector3(0.45f, -0.5f, 0.5f), Fore = new Vector3(0.2f, 0.2f, 0.95f), Off = new Vector3(-0.3f, 0.0f, 0.95f), OffFore = new Vector3(-0.2f, 0.1f, 0.97f), Lean = 6, Nod = 6, Twist = 12 }, Smooth((t - 2.0f) / 0.35f));
            Perform(_totoy, kid, 180.0f, "idle", t, Alive(hold, t, 2.0f, 0.6f));
            if (t < 1.9f && _totoy?.B != null)
            {
                Vector3 hand = _totoy.B.Hand(out _);
                HoldSlipper(hand + Vector3.up * (0.05f + 0.55f * rise), Quaternion.Euler(-90.0f + (n + air) * 360.0f, 180.0f, 0.0f));
            }
            else InHand(_totoy);

            Vector3 taya = On(Centre + new Vector3(-0.95f, 0, 0.9f));
            var ready = Pose.Ready; ready.Turn = 8.0f * Mathf.Sin(t * 1.4f);
            Perform(_taya, taya, 8.0f, "idle", t, Alive(ready, t, 3.0f, 1.4f));

            float p = Smooth(t / 2.4f);
            Shoot(Centre + new Vector3(Mathf.Lerp(1.2f, 1.05f, p), 0.32f, Mathf.Lerp(-3.6f, -3.2f, p)) + Wobble(t, 0.015f, 1.0f), kid + Vector3.up * 0.7f + new Vector3(-0.5f, 0, 0), Mathf.Lerp(19.0f, 16.0f, p), -1.5f);
        }

        // ---- 3 · throw
        private static readonly Track ThrowTrack = new Track()
            .Key(0.00f, new Pose { Arm = new Vector3(0.45f, -0.5f, 0.5f), Fore = new Vector3(0.2f, 0.2f, 0.95f), Off = new Vector3(-0.3f, 0.0f, 0.95f), OffFore = new Vector3(-0.2f, 0.1f, 0.97f), Lean = 6, Twist = 12 })
            .Key(0.28f, new Pose { Arm = new Vector3(0.4f, -0.7f, 0.3f), Fore = new Vector3(0.3f, -0.2f, 0.9f), Off = new Vector3(-0.35f, -0.4f, 0.8f), OffFore = new Vector3(-0.2f, -0.1f, 0.97f), Lean = 14, Sink = 0.08f, Nod = 6 }, Ease.Smooth)
            .Key(0.80f, Pose.Charge, Ease.Out)
            .Key(1.00f, Pose.Charge, Ease.Linear)
            .Key(1.08f, Pose.Contact, Ease.In)
            .Key(1.30f, Pose.Follow, Ease.Whip)
            .Key(1.85f, Pose.Follow, Ease.Linear)
            .Key(2.60f, new Pose { Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore, Lean = 10, Nod = -8, Sink = 0.02f }, Ease.Smooth);
        private const float Release = 1.04f;

        private void Throw(float t, bool side)
        {
            HoldCan(Centre, Quaternion.identity);
            Vector3 start = On(Centre + new Vector3(-0.2f, 0, 7.5f));
            Vector3 kid = Vector3.Lerp(start, On(start + Vector3.back * 0.35f), Smooth((t - 0.95f) / 0.4f));
            var pose = ThrowTrack.At(t);
            if (t > 0.8f && t < 1.0f) pose.Arm = (pose.Arm + 0.03f * new Vector3(Mathf.Sin(t * 83f), Mathf.Sin(t * 71f + 1.3f), 0)).normalized;
            Perform(_totoy, kid, 180.0f, "idle", t, Alive(pose, t, 2.0f, 0.3f));

            Vector3 eye = Centre + new Vector3(0.2f, 0.55f, -0.6f);
            if (t < Release) { InHand(_totoy); if (_totoy?.B != null) _releaseFrom = _totoy.B.Hand(out _); }
            else
            {
                // Out of the hand fast, then the world slows: the tsinelas eases toward the lens in slow motion.
                float s = Mathf.Clamp01((t - Release) / (3.2f - Release));
                float flight = 1.0f - Mathf.Pow(1.0f - s, 2.6f);
                Vector3 to = eye + (kid + Vector3.up * 0.9f - eye).normalized * 0.3f;
                Vector3 at = Vector3.Lerp(_releaseFrom, to, flight) + Vector3.up * Mathf.Sin(flight * Mathf.PI) * 0.45f;
                HoldSlipper(at, Quaternion.Euler(flight * 1080.0f + 40.0f, 25.0f, flight * 220.0f));
            }
            float push = Smooth(t / 1.1f);
            if (side)
            {
                if (_slipper != null && t >= Release + 0.25f) _slipper.SetActive(false);
                Vector3 sideEye = start + new Vector3(-2.3f, 1.0f, -1.6f) + Vector3.Lerp(Vector3.zero, new Vector3(0.35f, -0.05f, 0.3f), push);
                Shoot(sideEye + Wobble(t, 0.025f, 5.0f), kid + Vector3.up * 0.75f + new Vector3(0.1f, 0, -0.3f), Mathf.Lerp(36.0f, 30.0f, push), 2.0f);
            }
            else Shoot(eye + new Vector3(0, 0, 0.12f * push) + Wobble(t, 0.015f, 4.0f), kid + Vector3.up * 0.95f, Mathf.Lerp(22.0f, 26.0f, Smooth((t - Release) / 1.5f)));
            if (t >= Release && t < Release + 0.06f) Punch(0.25f);
        }

        // ---- 4 · elder
        private static readonly Track KanorTrack = new Track()
            .Key(0.0f, Pose.Rest)
            .Key(1.05f, Pose.Rest)
            .Key(1.5f, new Pose { Lean = 38, Nod = 18, Sink = 0.28f, Arm = new Vector3(-0.3f, -0.85f, 0.45f), Fore = new Vector3(-0.35f, -0.8f, 0.5f), Off = new Vector3(-0.2f, -0.9f, 0.3f), OffFore = new Vector3(-0.1f, -0.8f, 0.5f) }, Ease.Smooth)
            .Key(1.95f, new Pose { Lean = 34, Nod = 14, Sink = 0.26f, Arm = new Vector3(-0.3f, -0.8f, 0.5f), Fore = new Vector3(-0.3f, -0.75f, 0.55f), Off = new Vector3(-0.2f, -0.9f, 0.3f), OffFore = new Vector3(-0.1f, -0.8f, 0.5f) }, Ease.Linear)
            .Key(2.4f, new Pose { Lean = 4, Nod = -6, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Back)
            .Key(2.75f, new Pose { Lean = 14, Nod = 16, Arm = Pose.Down, Fore = Pose.DownFore, Off = new Vector3(-0.12f, 0.25f, 0.96f), OffFore = new Vector3(-0.05f, -0.55f, 0.83f) }, Ease.Smooth)
            .Key(3.6f, new Pose { Lean = 14, Nod = 16, Arm = Pose.Down, Fore = Pose.DownFore, Off = new Vector3(-0.12f, 0.25f, 0.96f), OffFore = new Vector3(-0.05f, -0.55f, 0.83f) }, Ease.Linear);

        private void Elder(float t)
        {
            // The can has been knocked crooked; he walks in, crouches, sets it upright, then pats the kid's head.
            float set = Smooth((t - 1.55f) / 0.35f);
            HoldCan(Centre + Vector3.up * 0.02f * (1 - set), Quaternion.Slerp(Quaternion.Euler(0, 30, 24), Quaternion.Euler(0, 30, 0), set));

            Vector3 stop = On(Centre + new Vector3(0.48f, 0, 0.38f));
            Vector3 enter = On(Centre + new Vector3(2.6f, 0, 1.4f));
            Vector3 at = Vector3.Lerp(enter, stop, Smooth(t / 1.0f));
            bool walking = t < 1.0f;
            var kp = KanorTrack.At(t);
            float pat = t > 2.75f ? Mathf.Abs(Mathf.Sin((t - 2.75f) * 9.0f)) : 0.0f;
            if (t > 2.75f) kp.OffFore = (kp.OffFore + Vector3.up * 0.35f * pat).normalized;
            float yaw = walking ? Yaw(enter, stop) : Mathf.LerpAngle(Yaw(enter, stop), Yaw(stop, Centre), Smooth((t - 1.0f) / 0.4f));
            Perform(_kanor, at, yaw, walking ? "walk" : "idle", t, Alive(kp, t, 5.0f, walking ? 0.0f : 0.6f));

            Vector3 kidAt = On(Centre + new Vector3(-0.22f, 0, -0.3f));
            var kidPose = Pose.Rest; kidPose.Nod = -16 + 10 * pat; kidPose.Hop = 0.04f * pat; kidPose.Turn = 20;
            Perform(_taya, kidAt, Yaw(kidAt, stop), t > 3.0f ? "emote-yes" : "idle", t > 3.0f ? t - 3.0f : t, Alive(kidPose, t, 6.0f));

            Vector3 seat = On(Centre + new Vector3(-4.6f, 0, 2.8f));
            foreach (var tr in FindObjectsByType<Transform>()) if (tr.name.StartsWith("prop_monobloc")) { seat = tr.position; break; }
            float fan = Mathf.Sin(t * 10.0f);
            var lola = new Pose { Nod = 4, Tilt = 4 * fan, Arm = new Vector3(0.4f, -0.2f, 0.7f), Fore = new Vector3(0.1f + 0.35f * fan, 0.85f, 0.4f), Off = Pose.OffDown, OffFore = new Vector3(0.3f, -0.4f, 0.8f) };
            Perform(_lola, seat, Yaw(seat, Centre), "sit", t, Alive(lola, t, 7.0f, 0.5f));

            float p = Smooth(t / 3.6f);
            Shoot(Centre + new Vector3(Mathf.Lerp(3.0f, 2.4f, p), 1.0f, Mathf.Lerp(-2.7f, -2.2f, p)) + Wobble(t, 0.03f, 8.0f), Centre + new Vector3(Mathf.Lerp(0.6f, -0.2f, p), 0.6f, 0.4f), 40.0f);
        }

        // ---- 5 · rack
        private void Rack(float t)
        {
            _rack.SetActive(true);
            for (int i = 0; i < _pairs.Count; i++)
                _pairs[i].rotation = Quaternion.LookRotation(-_storeOut) * Quaternion.Euler(6.0f * Mathf.Sin(t * 2.3f + i * 0.8f), 0, 4.0f * Mathf.Sin(t * 1.7f + i * 1.3f));
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            Vector3 line = _store + _storeOut * 1.1f + Vector3.up * 1.6f;
            float p = Smooth(t / 2.2f);
            Shoot(line + _storeOut * 2.0f + side * Mathf.Lerp(-1.1f, -0.3f, p) + Vector3.down * 0.3f + Wobble(t, 0.02f, 9.0f), line + side * Mathf.Lerp(-0.7f, 0.3f, p) + Vector3.down * 0.15f, 30.0f, 1.5f);
        }

        // ---- 6 · tv
        private const float Erupt = 3.5f;

        private void Tv(float t)
        {
            _tv.SetActive(true);
            Vector3 tv = _store + _storeOut * 2.2f;
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            float flicker = 0.85f + 0.15f * Mathf.PerlinNoise(t * 9.0f, 0.3f);
            float burst = t > Erupt ? Hit(t - Erupt, 0.05f, 2.5f) : 0.0f;
            _screen.sharedMaterial.color = Color.Lerp(new Color(0.5f, 0.72f, 1.0f), new Color(1.0f, 0.97f, 0.85f), burst) * flicker;
            _screenLight.intensity = 3.0f * flicker + 5.0f * burst;

            for (int i = 0; i < _crowd.Count; i++)
            {
                int row = i < 4 ? 0 : 1;
                float across = row == 0 ? (i < 2 ? -1.6f + i * 0.85f : 0.75f + (i - 2) * 0.85f) : (i < 6 ? -2.3f + (i - 4) * 0.95f : 0.85f + (i - 6) * 0.95f);
                Vector3 at = On(tv + _storeOut * (2.1f + row * 1.2f) + side * across);
                bool sits = row == 0 && i % 2 == 1;
                var pose = (i % 2 == 0) ? Pose.Nervous : new Pose { Lean = 14, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore };
                pose.Lean += 6.0f * Span(t, 1.5f, Erupt);
                float since = t - Erupt - (i % 4) * 0.07f - row * 0.05f;
                if (since > 0.0f)
                {
                    pose = Pose.Mix(pose, Pose.Cheer, Track.Shape(Ease.Back, Mathf.Clamp01(since / 0.25f)));
                    pose.Arm = (pose.Arm + Vector3.forward * 0.3f * Mathf.Sin(since * 11.0f + i)).normalized;
                    if (!sits) { pose.Hop = Jump(since, 0.25f + 0.08f * (i % 3), out float s1) + Jump(since - 0.75f, 0.18f, out float s2); pose.Sink = s1 + s2; }
                }
                Perform(_crowd[i], at, Yaw(at, tv), sits ? "sit" : "idle", t + i * 0.37f, Alive(pose, t, i * 1.3f));
            }
            Vector3 kid = On(tv + _storeOut * 1.05f + side * 0.1f);
            var kp = Pose.Nervous; kp.Lean += 10.0f * Span(t, 0.0f, Erupt); kp.Nod = -6.0f;
            float ks = t - Erupt;
            if (ks > 0.0f)
            {
                kp = Pose.Mix(kp, Pose.Cheer, Track.Shape(Ease.Back, Mathf.Clamp01(ks / 0.2f)));
                kp.Hop = Jump(ks, 0.42f, out float s1) + Jump(ks - 0.8f, 0.3f, out float s2); kp.Sink = s1 + s2;
            }
            Perform(_totoy, kid, Yaw(kid, tv), "idle", t, Alive(kp, t, 11.0f));

            float p = Smooth(t / 3.6f);
            Vector3 wide = tv + _storeOut * 7.8f + Vector3.up * 2.6f + side * 1.0f;
            Vector3 close = tv + _storeOut * 4.9f + Vector3.up * 2.35f + side * 0.1f;
            Shoot(Vector3.Lerp(wide, close, p) + Wobble(t, 0.03f, 12.0f), Vector3.Lerp(tv + Vector3.up * 1.0f, kid + Vector3.up * 0.9f + (tv - kid) * 0.4f, p), Mathf.Lerp(44.0f, 30.0f, p));
            if (t > Erupt && t < Erupt + 0.06f) Punch(0.7f);
        }

        // ---- 7 · archive
        private static readonly Track LiftTrack = new Track()
            .Key(0.0f, new Pose { Lean = 26, Nod = 16, Sink = 0.16f, Arm = new Vector3(0.25f, -0.8f, 0.55f), Fore = new Vector3(-0.4f, -0.4f, 0.8f), Off = new Vector3(-0.25f, -0.8f, 0.55f), OffFore = new Vector3(0.4f, -0.4f, 0.8f) })
            .Key(0.45f, new Pose { Lean = 30, Nod = 18, Sink = 0.2f, Arm = new Vector3(0.25f, -0.8f, 0.55f), Fore = new Vector3(-0.4f, -0.4f, 0.8f), Off = new Vector3(-0.25f, -0.8f, 0.55f), OffFore = new Vector3(0.4f, -0.4f, 0.8f) })
            .Key(1.0f, Pose.Lift, Ease.Back)
            .Key(3.2f, Pose.Lift, Ease.Linear);

        private void Archive(float t)
        {
            const float Lifted = 1.0f;
            for (int i = 0; i < _ring.Count; i++)
            {
                float angle = i * 360.0f / _ring.Count + 8.0f;
                float radius = 4.2f + (i % 2) * 1.2f;
                Vector3 at = On(Centre + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius);
                var pose = new Pose { Lean = 10, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore };
                float since = t - Lifted - (i % 5) * 0.06f;
                if (since > 0.0f)
                {
                    pose = Pose.Mix(pose, Pose.Cheer, Track.Shape(Ease.Back, Mathf.Clamp01(since / 0.25f)));
                    pose.Hop = Jump(since, 0.22f, out float s1) + Jump(since - 0.8f, 0.2f, out float s2); pose.Sink = s1 + s2;
                }
                Perform(_ring[i], at, Yaw(at, Centre), "idle", t + i * 0.21f, Alive(pose, t, i * 0.9f));
            }
            var bp = LiftTrack.At(t);
            if (t > Lifted) bp.Hop = 0.05f * Mathf.Abs(Mathf.Sin((t - Lifted) * 5.0f));
            Perform(_berto, On(Centre), 180.0f, "idle", t, Alive(bp, t, 13.0f, 0.5f));
            if (_berto?.B != null)
            {
                Vector3 hands = (_berto.B.Hand(out _) + _berto.B.OffHand()) * 0.5f;
                HoldCan(hands + Vector3.up * 0.06f, Quaternion.Euler(0, 30, 0));
            }
            float p = Smooth(t / 3.2f);
            Shoot(Centre + new Vector3(Mathf.Lerp(-5.6f, -4.9f, p), Mathf.Lerp(5.0f, 4.4f, p), Mathf.Lerp(-6.2f, -5.5f, p)) + Wobble(t, 0.06f, 14.0f), Centre + Vector3.up * 1.0f, 44.0f);
        }

        // ---- 8 · vhs
        private void Vhs(float t)
        {
            Vector3 a = On(Centre + new Vector3(-0.8f, 0, 4.0f)), b = On(Centre + new Vector3(1.1f, 0, 4.7f));
            var pose = ThrowTrack.At(Mathf.Min(t * 1.15f, 2.6f));
            pose.Twist *= 1.3f; pose.Lean *= 1.2f;
            Perform(_tikboy, a, 180.0f, "idle", t, Alive(pose, t, 15.0f, 0.4f));
            float ha = Mathf.Abs(Mathf.Sin(t * 10.5f));
            var laugh = new Pose
            {
                Lean = -8 - 12 * ha, Nod = -16 - 10 * ha, Roll = 6 * Mathf.Sin(t * 5.2f), Tilt = 10 * Mathf.Sin(t * 5.2f), Hop = 0.04f * ha,
                Arm = new Vector3(-0.6f, 0.15f, 0.8f), Fore = new Vector3(-0.55f, 0.2f, 0.8f), Off = new Vector3(0.2f, -0.6f, 0.6f), OffFore = new Vector3(0.7f, -0.2f, 0.6f),
            };
            Perform(_bebang, b, Yaw(b, a), "idle", t, t > 1.1f ? laugh : Alive(Pose.Rest, t, 16.0f));
            Shoot(Centre + new Vector3(0.7f, 1.15f, 0.6f) + Wobble(t * 3.0f, 0.22f, 16.0f), Centre + new Vector3(0.15f, 0.8f, 4.35f), 34.0f, (Mathf.PerlinNoise(t, 3.0f) - 0.5f) * 5.0f);
        }
    }
}
