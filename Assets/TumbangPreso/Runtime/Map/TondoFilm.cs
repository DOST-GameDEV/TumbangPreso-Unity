using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// TONDO (Eskinita): NOSTALGIC AND PLAYFUL. Dusk in the barangay; the camera slides in under the drying laundry,
    /// a kid chalks the box, each player steps out of their own doorway into a huddle, and the taya is counted out
    /// with "Maiba taya". The music is a plucked lullaby; the light goes from late gold to dusk, the doorways lit.
    ///
    /// THE BEATS (seconds, full film / short film):
    ///   SAMPAY   0.0 / none  Low in the south alley, looking up through shirts and towels on the lines at a violet
    ///                         sky, sliding north and tilting down to the court. TONDO is brushed on at 0.9.
    ///   CHALK    3.6 / none  Inside the box, low: a kid runs its south line with a stick of chalk, the line drawing
    ///                         itself behind him in a trail of dust, and skids up to the lens.
    ///   DOORS    6.4 / none  Four doors in the house fronts, one per seat, 0.9 s apart: the door cracks, swings,
    ///                         warm light spills, and the player steps out and walks to the huddle by the can.
    ///   MAIBA    11.3 / 1.8  From the ground inside the ring beside the can, looking up: the four leaning in over the
    ///                         lens against the dusk, hands meeting over it. MAI · BA · TA · YA, a clap and a warm spot
    ///                         on each player in turn round the ring; the fourth lands on round 1's taya. On YA, a cut
    ///                         high to the north side (the taya faces north): the taya struck and sagging, the two
    ///                         flanking attackers laughing at them, the counter dancing.
    ///   TAYA     13.05 / 3.5 Close on the taya, the TAYA plate stamping, as the attackers scatter to their marks
    ///                         behind and the taya steps back to theirs.
    ///   CLOSE    14.9 / 4.9  Each attacker's own screen ends on themselves; the taya's and a watcher's hold the taya.
    ///
    /// Nobody knows who the taya is until the count lands, so the chips show names only until then (`ChipRoles`), and
    /// the nameplate rings are down for every map film (`CharacterNameplate`). The doors are the film's own: a frame,
    /// a hinged panel in the seat's colour and a lit room behind, set flush on a house front the seat can walk
    /// straight out of (`FindDoor`); a seat with none walks in from out of frame. Every one goes with `End`.
    /// </summary>
    public sealed class TondoFilm : MapFilm
    {
        protected override string Region => "MAYNILA";
        protected override Color Accent => new Color(1.0f, 0.55f, 0.26f);
        protected override string Score => "sfx_film_tondo_score";
        protected override float CloseAt(bool full) => full ? 16.2f : 5.9f;

        private const float ChalkAt = 3.6f, DoorsAt = 6.4f, DoorGap = 0.9f, StepOut = 0.32f, ToHuddle = 2.0f;
        private const float MaibaAt = 11.3f, TayaCut = 14.3f, Scatter = 1.6f;
        private static readonly string[] Syllables = { "MAI", "BA", "TA", "YA" };
        private float[] _syllableAt;
        private float _tayaCut;

        /// <summary>Late gold going to dusk: a violet sky, the sun low and red, the lamps up.</summary>
        private static readonly Mood Dusk = new Mood
        {
            Sky = new Color(0.44f, 0.38f, 0.60f), Equator = new Color(0.66f, 0.44f, 0.42f), Ground = new Color(0.42f, 0.30f, 0.24f),
            Fog = new Color(0.88f, 0.58f, 0.48f), FogStart = 30.0f, FogEnd = 230.0f,
            Zenith = new Color(0.26f, 0.25f, 0.50f), Horizon = new Color(1.0f, 0.60f, 0.40f),
            Sun = new Color(1.0f, 0.56f, 0.32f), SunIntensity = 0.95f, SunElevation = 8.0f,
            Brightness = 0.96f, Saturation = 1.1f, Lamps = 1.6f, KeepYaw = true,
        };

        // ---- the doors and the huddle
        private sealed class Door
        {
            public bool Found;
            public Vector3 Sill, Out, Start;   // the wall's foot, its outward normal, where the walk starts
            public Transform Hinge;
            public Renderer Room;   // the lit room behind the door
            public Light Spill;
            public float Yaw;
        }
        private readonly Door[] _doors = new Door[Seats];
        private readonly int[] _doorOrder = new int[Seats];
        private int _doorCount;
        private readonly Vector3[] _ring = new Vector3[Seats];
        private readonly float[] _ringYaw = new float[Seats];
        private readonly int[] _countOrder = new int[Seats];
        private Vector3 _huddle;

        // ---- the chalk
        private Extra _kid;
        private Renderer _chalkLine;
        private readonly Renderer[] _dust = new Renderer[6];
        private Renderer _glint;
        private Renderer _realLine;
        private Vector3 _lineFrom, _lineTo;

        // ---- the laundry in front of the lens
        private Transform[] _cloth = new Transform[0];
        private float[] _clothPhase = new float[0];

        // ---- the count
        private Light _spot;
        private Renderer _pool;
        private AudioClip _kidTaya;

        protected override void Prepare()
        {
            ChipRoles = false;
            _syllableAt = Still ? new[] { 2.2f, 2.6f, 3.0f, 3.4f } : new[] { 11.75f, 12.15f, 12.55f, 12.95f };
            _tayaCut = Still ? 4.5f : TayaCut;
            TitleAt(Still ? 0.2f : 0.9f, Still ? 1.6f : 3.0f);
            TayaAt(_tayaCut, CloseAt(!Still) - _tayaCut);
            if (!Still) { Cut(ChalkAt); for (int i = 0; i < Seats; i++) Cut(DoorsAt + i * DoorGap); Cut(DoorsAt + 4 * DoorGap + 0.2f); }
            Cut(Still ? 1.8f : MaibaAt); Cut(_syllableAt[3]); Cut(_tayaCut);

            // The huddle: a ring round the can itself, the taya's place on its south, the others round it
            // in the order their marks stand, so nobody crosses anyone on the way out.
            _huddle = Centre;
            _huddle.y = Floor(_huddle, Centre.y);
            var order = new System.Collections.Generic.List<int>();
            for (int s = 0; s < Seats; s++) if (Body(s) != null && s != Taya) order.Add(s);
            order.Sort((a, b) => Mark(a).x.CompareTo(Mark(b).x));
            for (int s = 0; s < Seats; s++)
            {
                float angle;
                if (s == Taya) angle = 180.0f;
                else { int k = order.IndexOf(s); angle = order.Count <= 1 ? 0.0f : Mathf.Lerp(-95.0f, 95.0f, k / (float)(order.Count - 1)); }
                Vector3 dir = Quaternion.Euler(0.0f, angle, 0.0f) * Vector3.forward;
                // 1.6 m: room for the lens between them in the count (film r7: at 1.3 the torsos filled it).
                _ring[s] = _huddle + dir * 1.6f;
                _ring[s].y = Floor(_ring[s], Centre.y);
                _ringYaw[s] = Quaternion.LookRotation(-dir).eulerAngles.y;
            }
            // Counted out round the ring, the fourth on the taya.
            for (int i = 0; i < Seats; i++) _countOrder[i] = (Taya + 1 + i) % Seats;

            // Doors: attackers first in seat order, the taya last (nobody knows yet that it is the taya).
            _doorCount = 0;
            for (int i = 1; i <= Seats; i++) { int s = (Taya + i) % Seats; if (Body(s) != null) _doorOrder[_doorCount++] = s; }
            for (int s = 0; s < Seats; s++) _doors[s] = new Door();
            if (!Still)
                for (int k = 0; k < _doorCount; k++)
                {
                    int s = _doorOrder[k];
                    FindDoor(s, _doors[s]);
                    if (_doors[s].Found) BuildDoor(s, _doors[s]);
                    else
                    {
                        // No house front to come out of: they come in from out of frame along the court.
                        Vector3 away = Mark(s) - Centre; away.y = 0.0f; away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.forward;
                        _doors[s].Start = ClearStart(_ring[s], _ring[s] + away * 7.0f);
                        _doors[s].Yaw = Quaternion.LookRotation(-away).eulerAngles.y;
                    }
                    ChipAt(s, DoorsAt + k * DoorGap + 0.45f, 1.9f);
                }

            // The chalk: the box's south line is drawn afresh by a kid; the real line waits under it.
            foreach (var r in FindObjectsByType<Renderer>())
                if (r.transform.parent != null && r.transform.parent.name == "Chalk" && Mathf.Abs(r.bounds.center.z - (Centre.z - 7.0f)) < 0.3f && r.bounds.size.x > 8.0f)
                { _realLine = r; break; }
            float lineY = (_realLine != null ? _realLine.bounds.max.y : Centre.y + 0.02f) + 0.004f;
            float lineZ = _realLine != null ? _realLine.bounds.center.z : Centre.z - 7.0f;
            _lineFrom = new Vector3(Centre.x + 6.9f, lineY, lineZ); _lineTo = new Vector3(Centre.x - 3.4f, lineY, lineZ);
            _kid = Person("Film kid", 0.64f, "totoy", "jun_jun", "tikboy", "bebang");
            if (!Still)
            {
                if (_realLine != null) Hide(new[] { _realLine });
                _chalkLine = Glow("Fresh chalk", _lineFrom, new Vector2(0.01f, 0.15f), new Color(1.0f, 1.0f, 0.97f, 1.0f), Vector3.up, Texture2D.whiteTexture);
                _chalkLine.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
                for (int i = 0; i < _dust.Length; i++) _dust[i] = Glow("Chalk dust " + i, _lineFrom, Vector2.one * 0.3f, Color.clear);
                _glint = Glow("Chalk glint", _lineFrom, Vector2.one * 0.3f, Color.clear);
                BuildLaundry();
            }

            // The count: a warm spot that hops round the ring and stays on the taya.
            // ⚠️ A WARM TINT, NOT A FLOODLIGHT (owner, 2026-10-23, of film r2's taya frame: "is this a bug"): at 9 the
            // toon shade blew the taya out to a pale, see-through look. Two is lamplight he still reads through.
            _spot = Lamp("Count spot", Centre + Vector3.up * 7.0f, new Color(1.0f, 0.84f, 0.6f), 12.0f, LightType.Spot, 22.0f);
            _pool = Glow("Count pool", Centre, new Vector2(1.6f, 1.6f), Color.clear, Vector3.up, Ring);
            _pool.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            var life = FindFirstObjectByType<SidewalkLife>();
            _kidTaya = life != null && life.KidTaya != null && life.KidTaya.Length > 0 ? life.KidTaya[0] : null;
            Clip("sfx_film_tondo_clap"); Clip("sfx_film_tondo_chalk"); Clip("sfx_film_tondo_door"); Clip("sfx_film_tondo_taya");
        }

        // ------------------------------------------------------------------ the frame

        protected override void Stage(float age)
        {
            Tint(Dusk, Still ? 1.0f : Mathf.Lerp(0.72f, 1.0f, Span(age, 0.0f, MaibaAt)));
            SwayLaundry(age);

            if (Still)
            {
                if (age < 1.8f)
                {
                    Beat = "establish";
                    HoldInRing();
                    if (_kid != null) _kid.Show(false);
                    Shoot(_huddle + new Vector3(-4.4f, 2.4f, -6.8f), _huddle + Vector3.up * 0.8f, 46.0f);
                }
                else Count(age);
                return;
            }

            if (age < ChalkAt) { Beat = "sampay"; Sampay(age); HoldAtDoors(); return; }
            if (age < DoorsAt) { Beat = "chalk"; Chalk(age); HoldAtDoors(); return; }
            Chalked();
            if (age < MaibaAt) { Beat = "doors"; Doors(age); return; }
            Count(age);
        }

        // ---- SAMPAY: under the laundry, up at the sky, then down to the court.
        private void Sampay(float age)
        {
            float p = Smooth(age / ChalkAt);
            Vector3 eye = Centre + new Vector3(Mathf.Lerp(0.7f, -0.4f, p), Mathf.Lerp(1.35f, 2.5f, p), Mathf.Lerp(-14.0f, -5.0f, p));
            Vector3 aim = Vector3.Lerp(Centre + new Vector3(0.2f, 7.5f, -7.5f), Centre + new Vector3(0.0f, 1.1f, 6.0f), Smooth((age - 0.6f) / (ChalkAt - 0.6f)));
            Shoot(eye, aim, Mathf.Lerp(64.0f, 52.0f, p), Mathf.Lerp(5.0f, 0.0f, p));
            if (_kid != null) _kid.Show(false);
        }

        // ---- CHALK: the kid runs the line across the frame toward the lens; it draws itself behind him.
        private void Chalk(float age)
        {
            float local = (age - ChalkAt) / (DoorsAt - ChalkAt);
            float run = Mathf.Clamp01(local / 0.8f);
            float eased = run < 0.85f ? run / 0.85f * 0.93f : 0.93f + 0.07f * Smooth((run - 0.85f) / 0.15f);
            Vector3 head = Vector3.Lerp(_lineFrom, _lineTo, eased);
            float stood = Smooth((local - 0.84f) / 0.16f);
            if (_kid != null)
            {
                _kid.Show(true);
                bool skid = run >= 0.97f;
                // Crouched over the line on its inside, the chalk in the hand that trails on it.
                Vector3 at = head + new Vector3(0.0f, -0.004f, 0.3f);
                at.y = Floor(at, Centre.y);
                _kid.Pose(at, Mathf.Lerp(-90.0f, -150.0f, stood), Mathf.Lerp(skid ? 10.0f : 24.0f, 0.0f, stood), skid ? -6.0f * (1.0f - stood) : 0.0f);
                if (stood > 0.0f) _kid.Play("emote-yes", (local - 0.84f) * (DoorsAt - ChalkAt));
                else _kid.Play(skid ? "idle" : "sprint", age * 1.3f);
            }
            float length = Vector3.Distance(_lineFrom, head);
            if (_chalkLine != null)
            {
                _chalkLine.enabled = length > 0.01f;
                _chalkLine.transform.position = Vector3.Lerp(_lineFrom, head, 0.5f);
                // Wider than the real line it stands in for: from knee height the true width read faint (run 4).
                _chalkLine.transform.localScale = new Vector3(Mathf.Max(0.01f, length), 0.26f, 1.0f);
            }
            // A glint where the chalk meets the concrete.
            Billboard(_glint, head + Vector3.up * 0.1f, new Color(1.0f, 1.0f, 0.95f, run < 0.98f ? 1.0f : 0.0f), 0.55f + 0.12f * Mathf.Sin(age * 40.0f));
            for (int i = 0; i < _dust.Length; i++)
            {
                // Puffs shed at the chalk's tip, each rising and spreading over its own half second.
                float born = Mathf.Floor((age * 8.0f - i) / _dust.Length) * _dust.Length + i;
                float since = age - born / 8.0f;
                Vector3 at = Vector3.Lerp(_lineFrom, _lineTo, Mathf.Clamp01(eased - since * 0.3f)) + Vector3.up * (0.05f + since * 0.45f) + new Vector3(0.0f, 0.0f, 0.08f * Mathf.Sin(born));
                float a = run < 0.98f ? 0.6f * (1.0f - Mathf.Clamp01(since / 0.6f)) : 0.0f;
                Billboard(_dust[i], at, new Color(1.0f, 0.97f, 0.92f, a), 0.24f + since * 0.6f);
            }
            if (Crossed(ChalkAt + 0.05f)) Cue("sfx_film_tondo_chalk", 0.7f);
            if (Crossed(ChalkAt + 0.8f * (DoorsAt - ChalkAt))) Punch(0.5f);

            // Inside the box, knee high, looking down the line: the fresh trail across the frame, the kid running at us.
            float p = Smooth(local);
            Vector3 eye = new Vector3(_lineTo.x - 1.5f, _lineFrom.y + Mathf.Lerp(1.25f, 1.0f, p), _lineTo.z + 2.4f);
            Vector3 aim = Vector3.Lerp(new Vector3(_lineFrom.x - 2.5f, _lineFrom.y + 0.2f, _lineFrom.z), head + Vector3.up * 0.45f, Mathf.Lerp(0.35f, 0.9f, p));
            Shoot(eye, aim, Mathf.Lerp(50.0f, 42.0f, p), -2.0f);
        }

        private void Chalked()
        {
            if (_chalkLine != null && _chalkLine.enabled) { _chalkLine.enabled = false; foreach (var d in _dust) if (d != null) d.enabled = false; if (_glint != null) _glint.enabled = false; }
            if (_realLine != null && !_realLine.enabled) _realLine.enabled = true;
            if (_kid != null) _kid.Show(false);
        }

        // ---- DOORS: under a second each, the door swinging and the player stepping out.
        private float DoorTime(int seat)
        {
            for (int k = 0; k < _doorCount; k++) if (_doorOrder[k] == seat) return DoorsAt + k * DoorGap;
            return DoorsAt;
        }

        private const float GatherAt = DoorsAt + 4 * DoorGap + 0.2f;

        private void Doors(float age)
        {
            if (age >= GatherAt)
            {
                // The four converging on the can, from high over the box's south-east corner.
                Walkers(age);
                float g = Smooth((age - GatherAt) / (MaibaAt - GatherAt));
                Shoot(Centre + new Vector3(Mathf.Lerp(5.5f, 4.6f, g), Mathf.Lerp(5.2f, 4.4f, g), Mathf.Lerp(-7.5f, -6.4f, g)), Centre + new Vector3(-0.4f, 0.6f, 1.2f), 50.0f);
                return;
            }
            int k = Mathf.Clamp((int)((age - DoorsAt) / DoorGap), 0, _doorCount - 1);
            int seat = _doorOrder[k];
            Walkers(age);
            var door = _doors[seat];
            float local = (age - DoorTime(seat)) / DoorGap;
            if (door.Found)
            {
                // From the alley, off the door's shoulder, as it opens: eye height a little over a player's head.
                Vector3 side = Vector3.Cross(Vector3.up, door.Out);
                Vector3 aim = door.Sill + door.Out * Mathf.Lerp(0.4f, 1.1f, Smooth(local)) + Vector3.up * 1.0f;
                Vector3 eye = door.Sill + door.Out * 3.1f + side * (k % 2 == 0 ? 1.2f : -1.2f) + Vector3.up * Mathf.Lerp(1.55f, 1.7f, local);
                Shoot(ClearEye(aim, eye), aim, 44.0f);
            }
            else
            {
                Framed("door " + seat, seat, 0.62f, local, out var eye, out var aim, out var fov);
                Shoot(eye, aim, fov);
            }
        }

        /// <summary>Every seat in its door until its time, then out and walking to its place in the ring.</summary>
        private void Walkers(float age)
        {
            for (int s = 0; s < Seats; s++)
            {
                if (!Posable(s)) continue;
                var door = _doors[s];
                float local = age - DoorTime(s);
                float swing = DoorSwing(local);
                if (door.Hinge != null) door.Hinge.localRotation = Quaternion.Euler(0.0f, -100.0f * swing, 0.0f);
                if (door.Spill != null) door.Spill.intensity = 2.2f * Mathf.Clamp01(swing * 1.6f);
                if (door.Found && Crossed(DoorTime(s) + 0.08f)) Cue("sfx_film_tondo_door", 0.55f, 0.92f + 0.05f * s);
                float walk = (local - StepOut) / ToHuddle;
                ShowSeat(s, local > (door.Found ? 0.22f : 0.0f));
                if (walk <= 0.0f) { Place(s, door.Start, door.Yaw); Animate(s, false); continue; }
                if (walk >= 1.0f) { Place(s, _ring[s], _ringYaw[s]); Animate(s, false); Ground(s, _ring[s].y); continue; }
                Walk(s, door.Start, _ring[s], walk, _ringYaw[s]);
            }
        }

        /// <summary>The door: a crack of anticipation, the swing, a bounce on its stop.</summary>
        private static float DoorSwing(float local)
        {
            if (local <= 0.0f) return 0.0f;
            if (local < 0.12f) return 0.08f * Smooth(local / 0.12f);
            if (local < 0.18f) return 0.08f;
            return Mathf.Clamp01(Back((local - 0.18f) / 0.3f, 1.2f));
        }

        private void HoldAtDoors()
        {
            for (int s = 0; s < Seats; s++)
            {
                if (!Posable(s)) continue;
                ShowSeat(s, false);
                Place(s, _doors[s].Start, _doors[s].Yaw);
                Animate(s, false);
            }
        }

        private void HoldInRing()
        {
            for (int s = 0; s < Seats; s++)
            {
                if (!Posable(s)) continue;
                Place(s, _ring[s], _ringYaw[s]); Animate(s, false); Ground(s, _ring[s].y);
            }
        }

        // ---- MAIBA TAYA, then the scatter: counted out round the ring, the last on the taya, then everyone to their mark.
        private void Count(float age)
        {
            bool counting = age < _syllableAt[3];
            Beat = counting ? "maiba" : age < _tayaCut ? "reaction" : "taya";
            int pointed = -1; float since = 99.0f;
            for (int i = 0; i < Syllables.Length; i++)
            {
                int seat = _countOrder[i];
                if (age >= _syllableAt[i]) { pointed = seat; since = age - _syllableAt[i]; }
                WordAt(Syllables[i], Syllables[i], i == 3 ? new Color(0.36f, 0.66f, 1.0f) : Ivory, _syllableAt[i], i == 3 ? 1.0f : 0.48f,
                       new Vector2(-330.0f + 220.0f * i, -230.0f), i == 3 ? 150 : 118, i % 2 == 0 ? -7.0f : 6.0f);
                if (Crossed(_syllableAt[i]))
                {
                    Cue("sfx_film_tondo_clap", 0.8f, 1.0f + 0.04f * i);
                    if (i == 3) { Cue("sfx_film_tondo_taya", 0.8f); Punch(0.8f); if (_kidTaya != null) Cue(_kidTaya, 0.75f); }
                }
            }
            if (age >= _syllableAt[3]) ChipRoles = true;

            // The scatter: from the cut to the taya, each to their mark (the attackers at a run, the taya backing off).
            float scatter = Still ? 1.0f : Mathf.Clamp01((age - _tayaCut - 0.1f) / Scatter);
            float ya = age - _syllableAt[3];
            for (int s = 0; s < Seats; s++)
            {
                if (!Posable(s)) continue;
                Greet(s, 0.001f);
                if (age < _tayaCut || scatter <= 0.0f) { Place(s, _ring[s], _ringYaw[s]); Animate(s, false); Ground(s, _ring[s].y); }
                else if (scatter >= 1.0f) { Home(s); Animate(s, false); }
                else if (s == Taya)
                {
                    // The taya backs off to their mark, facing the three: a few steps, not a turn of the back.
                    float q = Smooth(Mathf.Clamp01(scatter * 1.4f));
                    Vector3 at = Vector3.Lerp(_ring[s], Mark(s), q); at.y = Floor(at, Centre.y);
                    Place(s, at, MarkYaw(s), q < 0.98f ? -4.0f : 0.0f);
                    Animate(s, q < 0.98f); Ground(s, at.y);
                }
                else Walk(s, _ring[s], Mark(s), scatter, MarkYaw(s));
                Act(s, Acting(s, age, ya, pointed, since, scatter));
            }

            // The spot on whoever the count is on, and on the taya after.
            if (pointed >= 0 && Body(pointed) != null)
            {
                Vector3 feet = Drawn(pointed).center; feet.y = Floor(feet, Centre.y);
                float on = pointed == Taya ? 1.0f : 0.25f + 0.75f * Mathf.Exp(-since * 5.0f);
                _spot.transform.position = feet + new Vector3(0.0f, 7.5f, -2.5f);
                _spot.transform.rotation = Quaternion.LookRotation(feet + Vector3.up - _spot.transform.position);
                _spot.intensity = 2.0f * on;
                _pool.transform.position = feet + Vector3.up * 0.015f;
                _pool.sharedMaterial.color = new Color(1.0f, 0.86f, 0.55f, 0.6f * on);
                _pool.transform.localScale = Vector3.one * (1.6f + 0.6f * Mathf.Exp(-since * 8.0f));
                _pool.enabled = true;
            }

            // The kid on the bench by the box's west side: watching, then jumping and cheering at YA.
            if (_kid != null && !Still)
            {
                _kid.Show(age < _tayaCut);
                Vector3 at = Centre + new Vector3(-4.6f, 0.0f, 2.2f); at.y = Floor(at, Centre.y);
                float jump = ya > 0.0f ? Mathf.Abs(Mathf.Sin(ya * 8.0f)) * 0.25f * Mathf.Exp(-ya * 0.8f) : 0.0f;
                _kid.Pose(at + Vector3.up * jump, 90.0f, ya > 0.0f ? -8.0f : 6.0f);
                _kid.Play(ya > 0.0f ? "emote-yes" : "idle", ya > 0.0f ? ya : age);
            }

            if (counting)
            {
                // Over the huddle from its north side, under the street's wires (r9: at 4.7 m a wire crossed the lens), which the taya faces: the four heads in a ring, the hands
                // meeting over the can, each flinch a body jerking out of the ring; a slow drift round as the count goes.
                // (Run 4 shot it from outside at ground level and read backs; r7-r8 from the ground inside, looking up,
                // and the bodies crowded the lens.)
                float p = Still ? 0.0f : Smooth((age - MaibaAt) / (_syllableAt[3] - MaibaAt));
                Vector3 eye = _huddle + Quaternion.Euler(0.0f, Mathf.Lerp(-12.0f, 12.0f, p), 0.0f) * new Vector3(0.0f, 3.5f, 3.7f);
                Shoot(ClearEye(_huddle + Vector3.up, eye), _huddle + new Vector3(0.0f, 0.7f, -0.35f), 56.0f);
            }
            else if (age < _tayaCut)
            {
                // YA: high on the north side, which the taya faces: the taya struck and sagging, the laughs either side.
                float push = Still ? 0.0f : Smooth(ya / 1.3f);
                Vector3 taya = _ring[Taya] + Vector3.up * 0.95f;
                // Off to the side and high, over the counter's raised fist (film r7: it filled the foreground).
                // (film r8: on the east side the street's wires crossed the lens)
                Vector3 eye = _huddle + new Vector3(-1.7f, Mathf.Lerp(3.9f, 3.6f, push), Mathf.Lerp(4.6f, 4.1f, push));
                Shoot(ClearEye(taya, eye), Vector3.Lerp(_huddle + Vector3.up * 0.8f, taya, 0.6f), 50.0f, Still ? 0.0f : -2.0f);
            }
            else
            {
                float local = Still ? 0.0f : (age - _tayaCut) / (CloseAt(true) - _tayaCut);
                Framed("taya reveal", Taya, 0.66f, local, out var eye, out var aim, out var fov);
                Shoot(eye, aim, fov);
            }
        }

        /// <summary>The counter: the attacker across the ring from the taya, who points round it.</summary>
        private int Counter
        {
            get
            {
                int best = -1; float far = -1.0f;
                for (int s = 0; s < Seats; s++)
                    if (s != Taya && Body(s) != null && Vector3.Distance(_ring[s], _ring[Taya]) > far) { far = Vector3.Distance(_ring[s], _ring[Taya]); best = s; }
                return best;
            }
        }

        /// <summary>
        /// THE COUNT, ACTED (owner, 2026-10-23: "make everyone as expressive as possible ... laughing at taya").
        ///   Before YA: all lean into the ring, hands in, tighter with each syllable; the counter points round it; whoever
        ///     the syllable lands on flinches back, hands up, "not me!", and sags with relief as it moves on.
        ///   YA: the taya is struck (thrown back, arms flung out) and then deflates (head hung, arms dangling, a sway);
        ///     the attackers explode into a jumping cheer, then one points and laughs at the taya, one doubles over, the
        ///     counter dances a fist pump.
        ///   The scatter: the taya shakes it off and squares up, a finger at the three; they run off still laughing.
        /// </summary>
        private Feel Acting(int s, float age, float ya, int pointed, float since, float scatter)
        {
            int counter = Counter;
            Vector3 tayaChest = Chest(Taya);
            if (age < _syllableAt[3])
            {
                float tension = Mathf.Clamp01((age - _syllableAt[0] + 0.4f) / (_syllableAt[3] - _syllableAt[0] + 0.4f));
                var feel = HandsIn(s, _huddle, tension);
                if (s == counter && pointed >= 0 && pointed != s)
                {
                    // The counting finger, on whoever has the syllable, a little jab with each one.
                    float jab = Hit(since, 0.05f, 6.0f);
                    feel.Right = (Chest(pointed) - Chest(s)).normalized + Vector3.up * 0.1f * jab;
                    feel.Lean += 6.0f * jab; feel.Turn = LookTurn(s, Chest(pointed));
                }
                else if (pointed >= 0 && s != pointed) feel.Turn = LookTurn(s, Chest(pointed)) * 0.6f;
                if (s == pointed) feel = Feel.Lerp(feel, Big(Flinch(s, since), 1.35f), Hit(since, 0.05f, 2.4f) * 1.2f);
                return feel;
            }
            if (s == Taya)
            {
                if (age < _tayaCut)
                {
                    // Struck (thrown back off their feet for a beat), then deflating, bigger than life: read at the
                    // shot's distance, not up close (run 4 read small).
                    var shock = Big(Shock(s), 1.4f);
                    shock.Hop = 0.2f * Hit(ya, 0.07f, 6.0f);
                    if (ya < 0.3f) return Feel.Lerp(new Feel(), shock, Smooth(ya / 0.08f));
                    var slump = Big(Slump(s, Mathf.Sin((ya - 0.3f) * 2.4f)), 1.3f);
                    return Feel.Lerp(shock, slump, Smooth((ya - 0.3f) / 0.45f));
                }
                // Shaking it off, then squaring up at the three.
                float t = age - _tayaCut;
                var off = Feel.Lerp(Slump(s, 0.0f), HeadShake(s, t), Smooth(t / 0.2f));
                return Feel.Lerp(off, Defiant(s, Centre + new Vector3(0.0f, 1.3f, 9.0f)), Smooth((t - 0.75f) / 0.25f));
            }
            // The attackers: a jumping cheer, then each their own laugh.
            int kind = s == counter ? 0 : (counter >= 0 && Mark(s).x < Mark(counter).x ? 1 : 2);
            Feel own = Big(kind == 0 ? FistPump(s, ya) : kind == 1 ? LaughAt(s, tayaChest, ya, true) : DoubledOver(s, ya), 1.35f);
            var cheer = Big(Cheer(s, ya, 0.4f + 0.04f * s), 1.3f);
            var now = Feel.Lerp(cheer, own, Smooth((ya - 0.55f - 0.1f * kind) / 0.25f));
            if (age >= _tayaCut)
            {
                // Running off: the laugh rides on the run, lighter, looking back at the taya.
                var running = LaughAt(s, tayaChest, ya, kind != 2);
                running.Hop = 0.0f; running.Lean *= 0.5f;
                now = Feel.Lerp(now, running, 0.7f);
                now.Arms *= 1.0f - 0.4f * scatter;
            }
            return now;
        }

        /// <summary>A feeling pushed further: every bend and the hop by `k` (the aims, which point at things, are kept).</summary>
        private static Feel Big(Feel f, float k)
        {
            f.Lean *= k; f.Twist *= k; f.Rock *= k; f.Nod *= k; f.Tilt *= k; f.Hop *= k;
            return f;
        }

        private static readonly Color Ivory = new Color(1.0f, 0.98f, 0.94f);

        protected override void TayaShot(float local, out Vector3 eye, out Vector3 aim, out float fov)
        {
            Framed("taya reveal", Taya, 0.66f, 1.0f, out eye, out aim, out fov);
            if (_spot != null)
            {
                Vector3 feet = Mark(Taya);
                _spot.transform.position = feet + new Vector3(0.0f, 7.5f, -2.5f);
                _spot.transform.rotation = Quaternion.LookRotation(feet + Vector3.up - _spot.transform.position);
                _spot.intensity = 2.0f;
                _pool.transform.position = feet + Vector3.up * 0.015f;
            }
        }

        // ------------------------------------------------------------------ the doors, built

        /// <summary>
        /// The house front this seat can walk straight out of: tried round the seat (its own side first), a wall within
        /// 16 m that stands a storey (hit at 1 m and at 2.1 m), facing back toward the court, with floor and nothing
        /// in the way to its place in the ring, and not crowding another seat's door.
        /// </summary>
        private void FindDoor(int seat, Door door)
        {
            Vector3 mark = Mark(seat);
            float side = mark.x - Centre.x < -0.5f ? -1.0f : mark.x - Centre.x > 0.5f ? 1.0f : (seat % 2 == 0 ? 1.0f : -1.0f);
            float beyond = mark.z - Centre.z >= 0.0f ? 1.0f : -1.0f;
            Vector3 prefer = new Vector3(side, 0.0f, beyond * 0.4f).normalized;
            var tries = new System.Collections.Generic.List<Vector3>();
            for (int a = 0; a < 16; a++) tries.Add(Quaternion.Euler(0.0f, a * 22.5f, 0.0f) * Vector3.forward);
            tries.Sort((x, y) => Vector3.Dot(y, prefer).CompareTo(Vector3.Dot(x, prefer)));
            foreach (var origin in new[] { mark, _ring[seat] })
                foreach (var dir in tries)
                {
                    if (!Wall(origin + Vector3.up * 1.0f, dir, out var wall)) continue;
                    if (!Wall(origin + Vector3.up * 2.1f, dir, out var high) || Mathf.Abs(high.distance - wall.distance) > 0.6f) continue;
                    Vector3 normal = new Vector3(wall.normal.x, 0.0f, wall.normal.z);
                    if (normal.sqrMagnitude < 0.5f || Vector3.Dot(normal.normalized, -dir) < 0.6f) continue;
                    normal.Normalize();
                    Vector3 sill = new Vector3(wall.point.x, Floor(wall.point + normal * 0.3f, origin.y), wall.point.z);
                    Vector3 start = sill + normal * 0.5f;
                    if (Vector3.Distance(ClearStart(_ring[seat], start), start) > 0.35f) continue;
                    bool crowded = false;
                    for (int o = 0; o < Seats; o++) if (o != seat && _doors[o] != null && _doors[o].Found && Vector3.Distance(_doors[o].Sill, sill) < 1.5f) crowded = true;
                    if (crowded) continue;
                    door.Found = true; door.Sill = sill; door.Out = normal; door.Start = start;
                    door.Yaw = Quaternion.LookRotation(normal).eulerAngles.y;
                    return;
                }
        }

        private bool Wall(Vector3 from, Vector3 dir, out RaycastHit wall)
        {
            int count = Physics.RaycastNonAlloc(from, dir, Hits, 16.0f, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity; wall = default;
            for (int i = 0; i < count; i++)
                if (World(Hits[i].collider) && Hits[i].distance < nearest) { nearest = Hits[i].distance; wall = Hits[i]; }
            return !float.IsInfinity(nearest) && nearest > 1.6f && Mathf.Abs(wall.normal.y) < 0.3f;
        }

        private void BuildDoor(int seat, Door door)
        {
            Quaternion facing = Quaternion.LookRotation(door.Out);
            Vector3 right = facing * Vector3.right;
            Color wood = new Color(0.36f, 0.22f, 0.14f);
            Vector3 face = door.Sill + door.Out * 0.04f;
            // The lit room behind the door (a warm wash, darker toward its top), then the frame round it.
            // The lit room behind the door: a warm block set into the wall, lit by the spill (film r3: a see-through
            // quad there read as a blank pale panel).
            door.Room = Prop(PrimitiveType.Cube, "Door " + seat + " room", face + Vector3.up * 0.98f - door.Out * 0.01f, facing, new Vector3(0.94f, 1.88f, 0.02f), new Color(1.0f, 0.66f, 0.32f), false).GetComponent<Renderer>();
            Prop(PrimitiveType.Cube, "Door " + seat + " lintel", face + Vector3.up * 2.0f, facing, new Vector3(1.24f, 0.16f, 0.12f), wood);
            Prop(PrimitiveType.Cube, "Door " + seat + " jamb L", face + Vector3.up * 1.0f - right * 0.55f, facing, new Vector3(0.14f, 2.0f, 0.12f), wood);
            Prop(PrimitiveType.Cube, "Door " + seat + " jamb R", face + Vector3.up * 1.0f + right * 0.55f, facing, new Vector3(0.14f, 2.0f, 0.12f), wood);
            Prop(PrimitiveType.Cube, "Door " + seat + " step", door.Sill + door.Out * 0.2f + Vector3.up * 0.04f, facing, new Vector3(1.3f, 0.08f, 0.36f), new Color(0.62f, 0.56f, 0.52f));
            // The panel swings on its left-hand hinge, outward, in the seat's own colour, with two raised panels.
            var hinge = new GameObject("Door " + seat + " hinge").transform;
            hinge.SetParent(transform, false);
            hinge.SetPositionAndRotation(face - right * 0.47f + door.Out * 0.02f, facing);
            Own(hinge.gameObject);
            Color paint = Color.Lerp(UI.PlayerIdentity.Colour(seat), new Color(0.9f, 0.85f, 0.75f), 0.25f);
            Part(hinge, "panel", new Vector3(0.47f, 0.95f, 0.0f), new Vector3(0.94f, 1.88f, 0.06f), paint);
            Part(hinge, "upper", new Vector3(0.47f, 1.36f, 0.04f), new Vector3(0.62f, 0.62f, 0.03f), Color.Lerp(paint, Color.white, 0.18f));
            Part(hinge, "lower", new Vector3(0.47f, 0.52f, 0.04f), new Vector3(0.62f, 0.62f, 0.03f), Color.Lerp(paint, Color.white, 0.18f));
            Part(hinge, "knob", new Vector3(0.82f, 0.95f, 0.07f), new Vector3(0.08f, 0.08f, 0.06f), new Color(0.95f, 0.78f, 0.3f));
            door.Hinge = hinge;
            door.Spill = Lamp("Door " + seat + " spill", door.Sill + door.Out * 0.7f + Vector3.up * 1.4f, new Color(1.0f, 0.72f, 0.42f), 4.0f);
        }

        private void Part(Transform parent, string name, Vector3 at, Vector3 size, Color colour)
        {
            var part = Prop(PrimitiveType.Cube, "Door " + name, Vector3.zero, Quaternion.identity, size, colour);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = at;
            part.transform.localRotation = Quaternion.identity;
        }

        // ------------------------------------------------------------------ the laundry by the lens

        private void BuildLaundry()
        {
            var cloth = new System.Collections.Generic.List<Transform>();
            var phase = new System.Collections.Generic.List<float>();
            Color[] colours =
            {
                new Color(0.86f, 0.42f, 0.40f), new Color(0.94f, 0.82f, 0.48f), new Color(0.46f, 0.62f, 0.84f), new Color(0.95f, 0.93f, 0.88f),
                new Color(0.58f, 0.76f, 0.54f), new Color(0.88f, 0.60f, 0.74f), new Color(0.96f, 0.64f, 0.36f),
            };
            int n = 0;
            foreach (float z in new[] { -12.6f, -10.4f, -8.2f })
            {
                float y = 3.3f + 0.1f * n;
                Prop(PrimitiveType.Cube, "Line " + n, Centre + new Vector3(0.0f, y, z), Quaternion.identity, new Vector3(4.8f, 0.015f, 0.015f), new Color(0.22f, 0.2f, 0.2f), false);
                for (int k = 0; k < 6; k++)
                {
                    if ((n * 2 + k) % 5 == 4) continue;
                    float x = -2.0f + 0.8f * k + 0.12f * Mathf.Sin(n * 3.1f + k);
                    var pivot = new GameObject("Cloth pivot").transform;
                    pivot.SetParent(transform, false);
                    pivot.position = Centre + new Vector3(x, y, z);
                    Own(pivot.gameObject);
                    Color c = colours[(n * 5 + k * 3) % colours.Length];
                    int kind = (n + k) % 3;
                    if (kind == 0)
                    {
                        // A shirt: the body, two sleeves out to the side.
                        Hang(pivot, new Vector3(0.0f, -0.25f, 0.0f), new Vector3(0.36f, 0.42f, 0.015f), c);
                        Hang(pivot, new Vector3(-0.24f, -0.1f, 0.0f), new Vector3(0.14f, 0.16f, 0.015f), c);
                        Hang(pivot, new Vector3(0.24f, -0.1f, 0.0f), new Vector3(0.14f, 0.16f, 0.015f), c);
                    }
                    else if (kind == 1)
                    {
                        // A towel with a band across it.
                        Hang(pivot, new Vector3(0.0f, -0.3f, 0.0f), new Vector3(0.3f, 0.56f, 0.015f), c);
                        Hang(pivot, new Vector3(0.0f, -0.46f, -0.009f), new Vector3(0.3f, 0.06f, 0.012f), Color.Lerp(c, Color.white, 0.55f));
                    }
                    else
                    {
                        // Shorts: a waist and two legs.
                        Hang(pivot, new Vector3(0.0f, -0.07f, 0.0f), new Vector3(0.32f, 0.12f, 0.015f), c);
                        Hang(pivot, new Vector3(-0.09f, -0.22f, 0.0f), new Vector3(0.14f, 0.2f, 0.015f), c);
                        Hang(pivot, new Vector3(0.09f, -0.22f, 0.0f), new Vector3(0.14f, 0.2f, 0.015f), c);
                    }
                    Hang(pivot, new Vector3(-0.1f, 0.0f, 0.0f), new Vector3(0.03f, 0.06f, 0.03f), new Color(0.95f, 0.85f, 0.3f));
                    cloth.Add(pivot); phase.Add(n * 1.7f + k * 0.9f);
                }
                n++;
            }
            _cloth = cloth.ToArray(); _clothPhase = phase.ToArray();
        }

        private void Hang(Transform pivot, Vector3 at, Vector3 size, Color colour)
        {
            var piece = Prop(PrimitiveType.Cube, "Cloth", Vector3.zero, Quaternion.identity, size, colour);
            piece.transform.SetParent(pivot, false);
            piece.transform.localPosition = at;
        }

        private void SwayLaundry(float age)
        {
            for (int i = 0; i < _cloth.Length; i++)
                if (_cloth[i] != null)
                    _cloth[i].localRotation = Quaternion.Euler(Still ? 5.0f : 9.0f * Mathf.Sin(age * 1.6f + _clothPhase[i]) + 5.0f, 0.0f, Still ? 0.0f : 3.5f * Mathf.Sin(age * 1.1f + _clothPhase[i] * 0.7f));
        }
    }
}
