using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// ERMITA (IlalimNgTulay): TENSE, AN UNDERGROUND RIVALRY. Night under the LRT viaduct on Taft. The two sides walk
    /// out of the dark toward the can; the train comes in overhead, its window light strobing through the pillars
    /// across their faces, and the beat drops as it passes the court; in the quiet after, one sodium lamp buzzes on
    /// over the taya. The score is a low pulse that builds with the train and drops with it.
    ///
    /// THE BEATS (seconds, full film / short film):
    ///   UNDER    0.0 / 0.0   Under the deck between the pillars, low, sliding north along them and turning west to
    ///                         the court across the road: sodium light, wet-dark asphalt. ERMITA at 0.9.
    ///   STANDOFF 3.4 / none  Out of the dark: the attackers walk in from the north, the taya from the south, toward
    ///                         the can. Three cuts: the three walking at the lens, abreast, chins up (3.4); the taya
    ///                         walking at the lens, hunched, alone (4.6); then over the taya's shoulder at the three
    ///                         as they plant and square up across the can (5.7).
    ///   TRAIN    7.2 / 1.6   The consist rolls in from the north along the viaduct. A crane up from behind the taya
    ///                         (the deck hides the consist from the ground): the four below, and the train sliding in
    ///                         over the deck past the three; then faces in its window strobe. DROP at 9.0: beside the
    ///                         deck at rail height south of the court, the consist coming at the lens and thundering
    ///                         past it, the beat dropping.
    ///   TAYA     10.4 / 4.0  Quiet. A sodium lamp over the taya stutters on; the TAYA plate stamps.
    ///   CLOSE    12.8 / 5.4  Each attacker's own screen ends on themselves; the taya's and a watcher's hold the taya.
    ///
    /// The train is a drawn copy of the map's own consist (`Ghost`): the real one, a mechanic (`LrtTrainFlyby`,
    /// `OverheadPassWindow`), is not moved, signalled or sounded; it is only hidden for the film. The window light is a
    /// film-owned spot riding the copy, shadowed, so the pillars throw moving bars across the court. The strobe's
    /// strength follows the Flash intensity setting, and under reduced motion it is a slow wash, not a strobe.
    /// </summary>
    public sealed class ErmitaFilm : MapFilm
    {
        protected override string Region => "MAYNILA";
        protected override Color Accent => new Color(1.0f, 0.62f, 0.22f);
        protected override string Score => "sfx_film_ermita_score";
        protected override float CloseAt(bool full) => full ? 12.8f : 5.4f;

        private const float StandAt = 3.4f, TrainAt = 7.2f, DropAt = 9.0f, DropEnd = 9.8f, TayaAt_ = 10.4f;
        private const float StandWalk = 3.2f, TrainSpeed = 13.0f;
        private const float StandB = 4.6f, StandC = 5.7f;
        private float _trainAt, _dropAt, _tayaAt;

        /// <summary>Night: deep blue, the moon for a sun, the street lamps up hard (run 4 read dusk-purple: no red in the
        /// sky or the fog now, and the warmth only in the sodium pools).</summary>
        private static readonly Mood Night = new Mood
        {
            Sky = new Color(0.05f, 0.08f, 0.19f), Equator = new Color(0.06f, 0.09f, 0.18f), Ground = new Color(0.04f, 0.04f, 0.06f),
            Fog = new Color(0.03f, 0.05f, 0.12f), FogStart = 14.0f, FogEnd = 120.0f,
            Zenith = new Color(0.01f, 0.02f, 0.07f), Horizon = new Color(0.05f, 0.09f, 0.20f),
            Sun = new Color(0.45f, 0.6f, 1.0f), SunIntensity = 0.18f, SunElevation = 38.0f,
            Brightness = 0.82f, Saturation = 1.08f, Lamps = 2.6f, KeepYaw = true,
        };

        // ---- the viaduct and the train
        private float _trackX, _trackY, _viaductX;
        private GameObject _train;
        private Light _windows, _belly;
        private readonly System.Collections.Generic.List<Vector3> _pillars = new System.Collections.Generic.List<Vector3>();

        // ---- the street
        private Light[] _sodium = new Light[0];
        private Renderer[] _sodiumHeads = new Renderer[0], _sodiumPools = new Renderer[0];
        private Light _tayaLamp;
        private Renderer _tayaLampHead, _tayaPool;

        // ---- the walks
        private readonly Vector3[] _from = new Vector3[Seats];

        protected override void Prepare()
        {
            ChipRoles = false;
            _trainAt = Still ? 1.6f : TrainAt;
            _dropAt = Still ? 2.9f : DropAt;
            _tayaAt = Still ? 4.0f : TayaAt_;
            TitleAt(Still ? 0.2f : 0.9f, Still ? 1.4f : 3.0f);
            TayaAt(_tayaAt + 0.35f, CloseAt(!Still) - _tayaAt - 0.35f);
            if (!Still) { Cut(StandAt); Cut(StandB); Cut(StandC); Cut(TrainAt); Cut(8.0f); Cut(8.55f); Cut(DropEnd); }
            else { Cut(_trainAt); }
            Cut(_tayaAt);

            // The line: from the map's own consist, or the rebuilt Ilalim's numbers (`LrtTrainFlyby` in the scene).
            var flyby = FindFirstObjectByType<LrtTrainFlyby>();
            _viaductX = flyby != null ? flyby.CentreX : Centre.x + 23.0f;
            _trackX = _viaductX + (flyby != null ? Mathf.Abs(flyby.TrackX) : 2.35f);
            _trackY = flyby != null ? flyby.TrackY : 8.95f;
            if (flyby != null)
            {
                _train = Ghost(flyby.gameObject, "Film consist");
                Hide(flyby.GetComponentsInChildren<Renderer>(true));
            }
            if (_train != null) _train.SetActive(false);
            _windows = Lamp("Train windows", Centre, new Color(0.86f, 0.93f, 1.0f), 46.0f, LightType.Spot, 64.0f);
            _windows.shadows = LightShadows.Soft; _windows.shadowStrength = 0.9f;
            _belly = Lamp("Train belly", Centre, new Color(0.7f, 0.8f, 1.0f), 16.0f);

            // The pillars under the deck: tall, narrow, standing under the viaduct near the court.
            foreach (var r in FindObjectsByType<Renderer>())
            {
                var b = r.bounds;
                if (b.size.y < 4.5f || b.size.x > 3.2f || b.size.z > 3.2f || b.size.x < 0.4f) continue;
                if (Mathf.Abs(b.center.x - _viaductX) > 6.5f || Mathf.Abs(b.center.z - Centre.z) > 45.0f || b.max.y < _trackY - 2.5f) continue;
                _pillars.Add(new Vector3(b.center.x, b.min.y, b.center.z));
            }

            // Sodium lamps round the court and a cold strip under the deck: the night's light.
            var at = new[] { new Vector3(-8.5f, 6.0f, -8.5f), new Vector3(8.5f, 6.0f, -8.5f), new Vector3(-8.5f, 6.0f, 9.5f), new Vector3(8.5f, 6.0f, 9.5f), new Vector3(_viaductX - Centre.x - 3.0f, 7.0f, 0.0f) };
            _sodium = new Light[at.Length]; _sodiumHeads = new Renderer[at.Length]; _sodiumPools = new Renderer[at.Length];
            for (int i = 0; i < at.Length; i++)
            {
                bool cold = i == at.Length - 1;
                _sodium[i] = Lamp("Sodium " + i, Centre + at[i], cold ? new Color(0.72f, 0.86f, 1.0f) : new Color(1.0f, 0.58f, 0.2f), cold ? 16.0f : 15.0f);
                _sodiumHeads[i] = Glow("Sodium head " + i, Centre + at[i], Vector2.one * 0.9f, Color.clear);
                if (cold) continue;
                // An orange pool on the asphalt under each: the night's only warmth, and where the players stand in it.
                Vector3 floor = Centre + new Vector3(at[i].x * 0.85f, 0.0f, at[i].z * 0.85f);
                floor.y = Floor(floor, Centre.y) + 0.012f;
                _sodiumPools[i] = Glow("Sodium pool " + i, floor, new Vector2(8.5f, 8.5f), Color.clear, Vector3.up, Soft);
                _sodiumPools[i].transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            }

            // The taya's lamp, overhead, off until the quiet after the train.
            Vector3 tayaFeet = Mark(Taya);
            _tayaLamp = Lamp("Taya lamp", tayaFeet + new Vector3(0.0f, 6.5f, -1.2f), new Color(1.0f, 0.64f, 0.3f), 12.0f, LightType.Spot, 34.0f);
            _tayaLamp.transform.rotation = Quaternion.LookRotation(tayaFeet + Vector3.up * 0.9f - _tayaLamp.transform.position);
            _tayaLampHead = Glow("Taya lamp head", _tayaLamp.transform.position, Vector2.one * 0.8f, Color.clear);
            _tayaPool = Glow("Taya lamp pool", tayaFeet, new Vector2(2.6f, 2.6f), Color.clear, Vector3.up, Soft);
            _tayaPool.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);

            // Where each walks in from: the attackers out of the dark north of the court, the taya from its south.
            for (int s = 0; s < Seats; s++)
            {
                if (Body(s) == null) continue;
                Vector3 mark = Mark(s);
                Vector3 away = s == Taya ? Vector3.back : Vector3.forward;
                _from[s] = Still ? mark : ClearStart(mark, mark + away * (s == Taya ? 4.5f : 5.0f) + Vector3.right * (mark.x - Centre.x) * 0.4f);
            }
            for (int s = 0; s < Seats; s++) if (Body(s) != null && !Still) ChipAt(s, StandAt + 0.6f + 0.25f * ((s - Taya + Seats) % Seats), 2.6f);

            Clip("sfx_lrt_pass"); Clip("sfx_film_ermita_horn"); Clip("sfx_film_ermita_drop"); Clip("sfx_film_ermita_buzz"); Clip("sfx_film_ermita_step");
        }

        // ------------------------------------------------------------------ the frame

        protected override void Stage(float age)
        {
            Tint(Night, 1.0f);
            Street(age);
            Train(age);
            TayaLamp(age);
            Walkers(age);

            if (Still) { StageShort(age); return; }
            if (age < StandAt) { Beat = "under"; Under(age); return; }
            if (age < TrainAt) { Beat = "standoff"; Standoff(age); return; }
            if (age < _tayaAt) { Beat = age >= DropAt && age < DropEnd ? "drop" : "train"; TrainShots(age); return; }
            Beat = "taya";
            TayaShots(age);
        }

        private void StageShort(float age)
        {
            if (age < _tayaAt)
            {
                Beat = age < _trainAt ? "under" : "train";
                Shoot(Centre + new Vector3(-9.5f, 2.2f, -9.0f), Centre + new Vector3(4.0f, 2.2f, 2.0f), 56.0f);
            }
            else { Beat = "taya"; TayaShots(age); }
        }

        // ---- UNDER: between the pillars, along them, turning to the court.
        private void Under(float age)
        {
            float p = Smooth(age / StandAt);
            Vector3 eye = new Vector3(_viaductX - 1.5f, Centre.y + Mathf.Lerp(1.6f, 2.4f, p), Centre.z + Mathf.Lerp(-24.0f, -9.0f, p));
            Vector3 along = eye + new Vector3(-1.5f, 1.2f, 14.0f);
            Vector3 court = Centre + new Vector3(0.0f, 1.2f, 1.5f);
            Shoot(eye, Vector3.Lerp(along, court, Smooth((age - 1.2f) / (StandAt - 1.2f))), Mathf.Lerp(58.0f, 46.0f, p), Mathf.Lerp(-4.0f, 0.0f, p));
        }

        // ---- STANDOFF: three cuts as the two sides walk to the can (run 4: walkers small at the frame edges, a head
        // filling the frame, a shoulder blocking the subject; each cut is now built on the bodies it is about).
        private void Standoff(float age)
        {
            if (age < StandB)
            {
                // The three walking at the lens, abreast, out of the dark: the camera gives ground as they come.
                float p = Smooth((age - StandAt) / (StandB - StandAt));
                Vector3 mid = AttackersMid();
                Vector3 eye = mid + new Vector3(0.6f, 0.7f, Mathf.Lerp(-5.8f, -4.4f, p));
                Shoot(eye, mid + new Vector3(0.0f, 1.15f, 0.0f), 46.0f, 1.5f);
            }
            else if (age < StandC)
            {
                // The taya alone, walking at the lens, hunched, low and close.
                float p = Smooth((age - StandB) / (StandC - StandB));
                Vector3 taya = Here(Taya);
                Vector3 eye = taya + new Vector3(-0.6f, 0.6f, Mathf.Lerp(3.6f, 2.9f, p));
                Shoot(eye, taya + new Vector3(0.0f, 1.05f, 0.0f), 40.0f, -2.0f);
            }
            else
            {
                // Over the taya's shoulder at the three as they plant: the taya's head and shoulder big on the left, the
                // three squared up beyond the can (run 6: a profile from the kerb left them small at the edges).
                float p = Smooth((age - StandC) / (TrainAt - StandC));
                Vector3 taya = Here(Taya);
                Vector3 eye = taya + new Vector3(-0.95f, Mathf.Lerp(1.55f, 1.45f, p), Mathf.Lerp(-1.9f, -1.6f, p));
                Shoot(eye, AttackersMid() + new Vector3(-0.4f, 0.95f, 0.0f), 34.0f, 1.0f);
            }
            if (Crossed(StandAt + 0.1f)) Cue("sfx_film_ermita_step", 0.6f);
        }

        /// <summary>The middle of the three attackers as drawn this frame.</summary>
        private Vector3 AttackersMid()
        {
            Vector3 sum = Vector3.zero; int n = 0;
            for (int s = 0; s < Seats; s++) if (s != Taya && Body(s) != null) { sum += Here(s); n++; }
            return n > 0 ? sum / n : Centre + Vector3.forward * 9.0f;
        }

        private int NearestAttacker()
        {
            int best = -1; float x = float.PositiveInfinity;
            for (int s = 0; s < Seats; s++) if (s != Taya && Body(s) != null && Mathf.Abs(Mark(s).x - Centre.x - 1.0f) < x) { x = Mathf.Abs(Mark(s).x - Centre.x - 1.0f); best = s; }
            return best < 0 ? Taya : best;
        }

        /// <summary>Where the model of a seat is this frame (its posed root, or its mark).</summary>
        private Vector3 Here(int seat) => Body(seat) != null ? Drawn(seat).center - Vector3.up * Drawn(seat).extents.y : Centre;

        private void Walkers(float age)
        {
            for (int s = 0; s < Seats; s++)
            {
                if (!Posable(s)) continue;
                Greet(s, 0.001f);
                // Out of the dark one after another, the taya first, at a steady, deliberate pace.
                float start = StandAt - 0.2f + 0.15f * ((s - Taya + Seats) % Seats);
                float p = Mathf.Clamp01((age - start) / StandWalk);
                if (Still || p >= 1.0f) { Home(s); Animate(s, false); }
                else
                {
                    ShowSeat(s, age >= StandAt - 0.4f);
                    if (p <= 0.0f) { Place(s, _from[s], MarkYaw(s)); Animate(s, false); }
                    else Walk(s, _from[s], Mark(s), p, MarkYaw(s));
                }
                Act(s, Acting(s, age, p));
            }
        }

        /// <summary>
        /// THE RIVALRY, ACTED (owner, 2026-10-23: expressiveness and readability are a top bar). Walking in, each
        /// side keeps its eyes on the other; planted, they square up and glare across the can. The train over them:
        /// every head snaps up and they brace as it thunders past. The lamp: the taya lifts their head into the light
        /// and points at the three; the attackers fold their arms and rock back, smirking.
        /// </summary>
        private Feel Acting(int s, float age, float walked)
        {
            Vector3 across = s == Taya ? Centre + new Vector3(0.0f, 1.2f, 9.0f) : Chest(Taya);
            Feel feel;
            if (walked < 1.0f && !Still)
            {
                // Walking in: the three with their chests out and a roll in the shoulders, the taya hunched and coming
                // head down, eyes up, at the three.
                float step = Mathf.Sin(age * 8.4f + s);
                feel = s == Taya
                    ? new Feel { Lean = 12.0f, Nod = 14.0f, Turn = LookTurn(s, across) * 0.5f, Rock = 3.0f * step }
                    : new Feel { Lean = -7.0f, Nod = -9.0f, Turn = LookTurn(s, across) * 0.8f, Rock = 7.0f * step, Twist = 6.0f * step };
            }
            else feel = Planted(s, across, age);
            // The train overhead: heads up, a brace.
            float over = !Still && age > _dropAt - 0.6f && age < DropEnd + 0.3f ? Smooth((age - (_dropAt - 0.6f)) / 0.3f) * (1.0f - Smooth((age - DropEnd) / 0.3f)) : 0.0f;
            if (over > 0.0f)
            {
                Vector3 si = Side(s), f = Facing(s);
                var brace = new Feel { Lean = 10.0f, Nod = -32.0f, Turn = 20.0f * (s % 2 == 0 ? 1 : -1), Left = Vector3.up * 0.6f + f * 0.5f - si * 0.4f, Right = Vector3.up * 0.6f + f * 0.5f + si * 0.4f, Arms = 0.8f };
                feel = Feel.Lerp(feel, brace, over);
            }
            float lamp = age - _tayaAt;
            if (lamp > 0.0f)
            {
                if (s == Taya)
                {
                    // Head hung in the dark, then up into the light as it strikes, then a finger at the three.
                    var hung = new Feel { Nod = 30.0f, Lean = 12.0f };
                    var up = new Feel { Nod = -12.0f, Lean = -6.0f };
                    feel = Feel.Lerp(hung, up, Smooth((lamp - 0.35f) / 0.3f));
                    feel = Feel.Lerp(feel, Defiant(s, across), Smooth((lamp - 0.9f) / 0.25f));
                }
                else feel = Feel.Lerp(feel, ArmsCrossed(s, Chest(Taya)), Smooth((lamp - 0.2f - 0.12f * s) / 0.3f));
            }
            return feel;
        }

        /// <summary>Planted at the can: each of the three squares up their own way (knuckles, a neck roll, a glare), the
        /// taya glares back.</summary>
        private Feel Planted(int s, Vector3 across, float age)
        {
            var glare = Glare(s, across, age + s);
            if (s == Taya || Still) return glare;
            int k = (s - Taya + Seats) % Seats;
            Vector3 f = Facing(s), si = Side(s), up = Vector3.up;
            if (k == 1)
            {
                // Cracking the knuckles: both fists together at the chest, a grind.
                float grind = Mathf.Sin(age * 14.0f) * 0.12f;
                return new Feel { Lean = 6.0f, Nod = 12.0f, Turn = LookTurn(s, across), Left = f + up * 0.15f + si * (0.25f + grind), Right = f + up * 0.15f - si * (0.25f - grind), Arms = 0.95f };
            }
            if (k == 2)
            {
                // A slow neck roll, chin up, fists low and loose.
                return new Feel { Lean = -4.0f, Nod = -6.0f + 10.0f * Mathf.Sin(age * 3.0f), Tilt = 20.0f * Mathf.Sin(age * 3.0f + 1.2f), Turn = LookTurn(s, across) * 0.8f, Left = glare.Left, Right = glare.Right, Arms = 0.7f };
            }
            return glare;
        }

        // ---- TRAIN: the consist over the viaduct, its windows strobing over the court, the drop as it passes.
        /// <summary>The consist's middle: from the north, level with the court at the drop.</summary>
        private float TrainZ(float age) => Centre.z - (age - _dropAt) * TrainSpeed;

        private void Train(float age)
        {
            bool running = age >= _trainAt - 0.8f && age <= _tayaAt + 0.6f;
            float z = TrainZ(age);
            if (_train != null)
            {
                _train.SetActive(running);
                if (running) _train.transform.SetPositionAndRotation(new Vector3(_trackX, _trackY, z), Quaternion.Euler(0.0f, 180.0f, 0.0f));
            }
            if (Crossed(_trainAt - 0.3f)) Cue("sfx_lrt_pass", 0.9f, 1.15f);
            if (Crossed(Still ? 0.6f : 1.4f)) Cue("sfx_film_ermita_horn", 0.55f);
            if (Crossed(_dropAt)) { Cue("sfx_film_ermita_drop", 0.9f); Punch(1.1f); }

            // The windows: bright bands passing a gap at a time. A wash, not a strobe, under reduced motion; the
            // strength follows the Flash setting. Strongest while the consist is level with the court.
            float near = running ? Mathf.Clamp01(1.0f - Mathf.Abs(z - Centre.z) / 42.0f) : 0.0f;
            float bands = Still ? 0.75f : 0.5f + 0.5f * Mathf.Sign(Mathf.Sin(z * Mathf.PI * 2.0f / 4.2f)) * Mathf.Abs(Mathf.Sin(z * Mathf.PI * 2.0f / 4.2f)) * 1.6f;
            bands = Mathf.Clamp01(bands);
            float level = near * Mathf.Lerp(0.35f, 1.0f, FlashLevel);
            _windows.transform.position = new Vector3(_trackX - 1.2f, _trackY + 1.6f, z);
            _windows.transform.rotation = Quaternion.LookRotation(Centre + new Vector3(0.0f, 0.4f, 1.0f) - _windows.transform.position);
            _windows.intensity = 9.0f * level * Mathf.Lerp(0.25f, 1.0f, bands);
            _belly.transform.position = new Vector3(_trackX, _trackY - 0.4f, z);
            _belly.intensity = 3.0f * near;
            if (!Still && near > 0.05f) Flash(new Color(0.75f, 0.85f, 1.0f), 0.07f * near * bands);
            // The drop: one white hit as it passes over.
            if (age >= _dropAt && age < _dropAt + 0.2f) Flash(Color.white, 0.22f * (1.0f - (age - _dropAt) / 0.2f) * (Still ? 0.4f : 1.0f));
            ScoreLevel = age >= _trainAt && age < _dropAt ? Mathf.Lerp(0.85f, 0.55f, Span(age, _trainAt, _dropAt)) : 0.85f;
        }

        private void TrainShots(float age)
        {
            if (age < 8.0f)
            {
                // A crane up from behind the taya: the four below, the three facing us across the can, and the consist
                // sliding in over the deck beyond them as the camera clears its parapet (run 6: from across the street
                // at five metres the deck still hid it).
                // Up fast, so the consist is over the parapet for most of the shot (film r7: at 5 m it still hid it).
                float p = Smooth((age - TrainAt) / 0.55f);
                Vector3 eye = Centre + new Vector3(-3.0f, Mathf.Lerp(5.0f, 9.6f, p), -10.0f);
                Vector3 train = new Vector3(_trackX, _trackY + 1.8f, TrainZ(age));
                Vector3 court = Centre + new Vector3(0.0f, 0.8f, 5.0f);
                Shoot(ClearEye(Centre + Vector3.up, eye), Vector3.Lerp(court, train, Mathf.Lerp(0.25f, 0.42f, p)), 58.0f, 2.0f);
            }
            else if (age < 8.55f)
            {
                // The taya's face in the strobe.
                Framed("strobe taya", Taya, 0.95f, (age - 8.0f) / 0.55f, out var eye, out var aim, out var fov);
                Shoot(eye, aim + Vector3.up * 0.25f, fov * 0.8f);
            }
            else if (age < DropAt)
            {
                // An attacker's, answering.
                int near = NearestAttacker();
                Framed("strobe attacker", near, 0.95f, (age - 8.55f) / 0.45f, out var eye, out var aim, out var fov);
                Shoot(eye, aim + Vector3.up * 0.25f, fov * 0.8f, -3.0f);
            }
            else if (age < DropEnd)
            {
                // DROP: beside the deck at rail height, south of the court: the consist comes at the lens and
                // thunders past it (run 4 put the eye inside the deck and the frame washed grey).
                float p = (age - DropAt) / (DropEnd - DropAt);
                Vector3 eye = new Vector3(_viaductX - 6.0f, _trackY + 1.8f, Centre.z - 8.0f - 0.6f * p);
                float nose = Mathf.Max(TrainZ(age) - 5.0f, eye.z - 3.0f);
                Shoot(eye, new Vector3(_trackX, _trackY + 1.8f, nose), 62.0f, Mathf.Lerp(-7.0f, 0.0f, p));
            }
            else
            {
                // The tail going away south; the four in the flicker, high and wide from the kerb.
                float p = Smooth((age - DropEnd) / (_tayaAt - DropEnd));
                Vector3 eye = Centre + new Vector3(10.5f, Mathf.Lerp(3.4f, 4.0f, p), -9.0f);
                Shoot(ClearEye(Centre + Vector3.up, eye), Centre + new Vector3(-0.5f, 0.6f, 2.0f), 48.0f);
            }
        }

        // ---- TAYA: the lamp stutters on over them.
        private void TayaLamp(float age)
        {
            float since = age - _tayaAt;
            float on = since < 0.0f ? 0.0f : Stutter(since);
            _tayaLamp.intensity = 4.0f * on;
            Billboard(_tayaLampHead, _tayaLamp.transform.position, new Color(1.0f, 0.72f, 0.38f, 0.9f * on), 0.8f);
            Vector3 feet = Mark(Taya); feet.y = Floor(feet, Centre.y) + 0.015f;
            _tayaPool.transform.position = feet;
            _tayaPool.sharedMaterial.color = new Color(1.0f, 0.6f, 0.26f, 0.45f * on);
            _tayaPool.enabled = on > 0.01f;
            if (Crossed(_tayaAt + 0.02f)) Cue("sfx_film_ermita_buzz", 0.75f);
            if (since >= 0.0f) ChipRoles = true;
        }

        /// <summary>A sodium lamp striking: two false starts, a flicker, then on (held on under reduced motion).</summary>
        private float Stutter(float since)
        {
            if (Still) return Smooth(since / 0.3f);
            if (since < 0.08f) return 0.8f;
            if (since < 0.2f) return 0.05f;
            if (since < 0.27f) return 0.6f;
            if (since < 0.42f) return 0.1f;
            return Mathf.Clamp01(0.85f + 0.15f * Mathf.Sin(since * 47.0f) * Mathf.Exp(-(since - 0.42f) * 3.0f) + Smooth((since - 0.42f) / 0.3f) * 0.15f);
        }

        private void TayaShots(float age)
        {
            float local = Still ? 0.0f : (age - _tayaAt) / (CloseAt(!Still) - _tayaAt);
            Framed("taya lamp", Taya, 0.7f, local, out var eye, out var aim, out var fov);
            Shoot(eye, aim, fov);
            if (Crossed(_tayaAt + 0.45f)) Punch(0.6f);
        }

        protected override void TayaShot(float local, out Vector3 eye, out Vector3 aim, out float fov) => Framed("taya lamp", Taya, 0.7f, 1.0f, out eye, out aim, out fov);

        // ---- the street's own light: sodium heads glowing, a slow hum in them.
        private void Street(float age)
        {
            for (int i = 0; i < _sodium.Length; i++)
            {
                float hum = Still ? 1.0f : 0.93f + 0.07f * Mathf.Sin(age * (7.0f + i) + i);
                bool cold = i == _sodium.Length - 1;
                _sodium[i].intensity = (cold ? 1.6f : 2.2f) * hum;
                Billboard(_sodiumHeads[i], _sodium[i].transform.position, cold ? new Color(0.75f, 0.88f, 1.0f, 0.7f * hum) : new Color(1.0f, 0.68f, 0.32f, 0.85f * hum), 0.9f);
                if (_sodiumPools[i] != null) _sodiumPools[i].sharedMaterial.color = new Color(1.0f, 0.55f, 0.18f, 0.32f * hum);
            }
        }
    }
}
