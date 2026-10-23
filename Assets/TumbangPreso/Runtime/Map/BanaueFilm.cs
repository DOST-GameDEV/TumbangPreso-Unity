using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// BANAUE (Payo): MYTHIC AND CEREMONIAL. Dawn over the terraces, the valley full of mist. The camera climbs the
    /// terraces a gong stroke at a time to the stone plaza on the ridge; the great seated bulul catches the first sun.
    /// The four walk up in file, solemn, and take the four points of the carved sun disc round the can, a torch catching
    /// at each as they reach it. They raise their arms to the sky; the disc's rays wake gold; a pillar of light runs
    /// round the ring, slowing like a wheel, and stops on the taya. The score is gangsa gongs, a bamboo flute and a
    /// drone; every hit lands on a step of the climb or a turn of the light.
    ///
    /// THE BEATS (seconds, full film / short film):
    ///   MIST    0.0 / none   Low on the terraces south of the plaza, looking up them at the waterfall, climbing in
    ///                         four steps, a gong on each (0.3, 1.1, 1.9, 2.7), then tilting over to the plaza below
    ///                         with the misty valley and the dawn beyond it. BANAUE at 1.0.
    ///   BULUL   3.6 / none   Low at the feet of the seated bulul west of the plaza, up at its face: the first sun
    ///                         warms it (3.9) and the torches at its feet catch (4.3).
    ///   FILE    5.0 / none   The four walk up out of the south in file, heads bowed (5.0, low at the top of the
    ///                         stairs); then from above the ring (6.4) they part to the disc's four points, a torch
    ///                         catching at each as they reach it (6.9, 7.3, 7.7, 8.1).
    ///   RITE    8.4 / 0.0    Straight down on the disc, turning slowly: arms raised to the sky, the disc's rays
    ///                         waking gold from the middle out; then low at the ring's edge (9.4).
    ///   CHOOSE  10.2 / 1.6   A pillar of gold light runs round the ring from one to the next, slowing like a wheel
    ///                         (each it passes flinches), and stops on the taya (11.2): a flare, the gong, TAYA.
    ///   REACT   11.5 / 3.0   The taya struck and sagging; the three bow to them in mock ceremony, then laugh.
    ///   TAYA    12.6 / 3.4   Close on the taya, shaking it off and squaring up; the three walk back to their marks.
    ///   CLOSE   14.0 / 4.8   Each attacker's own screen ends on themselves; the taya's and a watcher's hold the taya.
    ///
    /// The torches and the disc's light are the film's own (the map's fires are the night's, off by day); the light is a
    /// mood, not a filter: dawn's low sun, the mist the fog's, warm lamps at 2 to 4. Everything goes with `End`.
    /// </summary>
    public sealed class BanaueFilm : MapFilm
    {
        protected override string Region => "IFUGAO";
        protected override Color Accent => new Color(1.0f, 0.78f, 0.3f);
        protected override string Score => "sfx_film_banaue_score";
        protected override float CloseAt(bool full) => full ? 14.0f : 4.8f;

        private const float BululAt = 3.6f, FileAt = 5.0f, FileHigh = 6.4f, RiteAt = 8.4f, RiteLow = 9.4f, ChooseAt_ = 10.2f, LandAt_ = 11.2f, ReactAt_ = 11.5f, TayaCut = 12.6f;
        private static readonly float[] Steps = { 0.3f, 1.1f, 1.9f, 2.7f };
        private float _riteAt, _chooseAt, _landAt, _reactAt, _tayaCut;

        /// <summary>Dawn in the mountains: a deep violet sky going to rose at the horizon, the sun just up and warm, the
        /// valley full of pale mist (the fog close), the stone cool until the light reaches it.</summary>
        /// (Film r1 had the fog from 12 m: the plaza itself sat in a pink wash, every colour pale. The mist is the
        /// valley's: from 30 m, a cooler grey-lilac, and the colour pushed.)
        private static readonly Mood Dawn = new Mood
        {
            Sky = new Color(0.5f, 0.55f, 0.74f), Equator = new Color(0.74f, 0.66f, 0.6f), Ground = new Color(0.38f, 0.36f, 0.36f),
            Fog = new Color(0.8f, 0.79f, 0.84f), FogStart = 30.0f, FogEnd = 260.0f,
            Zenith = new Color(0.2f, 0.28f, 0.58f), Horizon = new Color(1.0f, 0.74f, 0.56f),
            Sun = new Color(1.0f, 0.74f, 0.52f), SunIntensity = 1.2f, SunElevation = 9.0f,
            Brightness = 1.0f, Saturation = 1.1f, Lamps = 1.0f, KeepYaw = true,
        };

        // ---- the ring
        private readonly Vector3[] _spot = new Vector3[Seats], _from = new Vector3[Seats], _via = new Vector3[Seats];
        private readonly float[] _spotYaw = new float[Seats], _arrive = new float[Seats];
        private readonly int[] _file = new int[Seats];            // order in the file, the taya last
        private readonly int[] _round = new int[Seats];           // the light's order round the ring, ending on the taya
        private int _count;

        // ---- the fire and the light
        private readonly Light[] _torchLamp = new Light[Seats];
        private readonly Renderer[][] _flame = new Renderer[Seats][];
        private readonly Vector3[] _torchTop = new Vector3[Seats];
        private Renderer _ring, _core;
        private readonly Renderer[] _rays = new Renderer[12];
        private Renderer _pillar, _pillarFoot;
        private Light _pillarLamp, _bululSun;
        private readonly Light[] _bululTorch = new Light[2];
        private readonly Renderer[][] _bululFlame = new Renderer[2][];
        private Vector3 _bulul = new Vector3(-36.2f, 5.0f, 5.9f), _bululFace;
        private float _bululTop = 11.5f;

        protected override void Prepare()
        {
            ChipRoles = false;
            _riteAt = Still ? 0.0f : RiteAt;
            _chooseAt = Still ? 1.6f : ChooseAt_;
            _landAt = Still ? 2.6f : LandAt_;
            _reactAt = Still ? 3.0f : ReactAt_;
            _tayaCut = Still ? 3.4f : TayaCut;
            TitleAt(Still ? 0.2f : 1.0f, Still ? 1.3f : 2.6f);
            TayaAt(_landAt + 0.2f, CloseAt(!Still) - _landAt - 0.2f);
            if (!Still) { Cut(BululAt); Cut(FileAt); Cut(FileHigh); Cut(RiteAt); Cut(RiteLow); Cut(ChooseAt_); Cut(ReactAt_); Cut(TayaCut); }
            else { Cut(_chooseAt); Cut(_reactAt); Cut(_tayaCut); }

            PlanRing();
            BuildTorches();
            BuildDisc();
            FindBulul();
            for (int k = 0; k < _count; k++) if (!Still) ChipAt(_file[k], _arrive[_file[k]] - 0.6f, 1.4f);
            Clip("sfx_film_banaue_gong"); Clip("sfx_film_banaue_torch"); Clip("sfx_film_banaue_tick"); Clip("sfx_film_banaue_flare");
        }

        // ------------------------------------------------------------------ the ring

        private void PlanRing()
        {
            // The taya's point on the disc's south (they face north), the others round it in the order their marks
            // stand, west to east, at the disc's west, north and east.
            var others = new System.Collections.Generic.List<int>();
            for (int s = 0; s < Seats; s++) if (Body(s) != null && s != Taya) others.Add(s);
            others.Sort((a, b) => Mark(a).x.CompareTo(Mark(b).x));
            _count = 0;
            float[] angles = others.Count == 3 ? new[] { -90.0f, 0.0f, 90.0f } : others.Count == 2 ? new[] { -60.0f, 60.0f } : new[] { 0.0f };
            for (int i = 0; i < others.Count; i++) Spot(others[i], angles[Mathf.Min(i, angles.Length - 1)]);
            if (Body(Taya) != null) Spot(Taya, 180.0f);
            // The file: the three, then the taya last.
            for (int i = 0; i < others.Count; i++) _file[_count++] = others[i];
            if (Body(Taya) != null) _file[_count++] = Taya;
            for (int k = 0; k < _count; k++)
            {
                int s = _file[k];
                // Up out of the south in a line, then out round the disc to their point.
                _from[s] = Centre + new Vector3(0.35f * ((k % 2) * 2 - 1), 0.0f, -9.5f - 1.3f * k);
                _from[s] = ClearStart(_spot[s], _from[s]);
                Vector3 side = _spot[s] - Centre; side.y = 0.0f;
                _via[s] = Centre + Quaternion.Euler(0.0f, side.x < -0.1f ? -35.0f : side.x > 0.1f ? 35.0f : 75.0f, 0.0f) * (side.sqrMagnitude > 0.01f ? side.normalized : Vector3.back) * 3.6f;
                if (s == Taya) _via[s] = Centre + new Vector3(0.0f, 0.0f, -5.0f);
                _via[s].y = Floor(_via[s], Centre.y);
                _arrive[s] = FileHigh + 0.5f + 0.4f * k;
            }
            // The light's way round: from the one after the taya in the ring, round, ending on the taya.
            var ring = new System.Collections.Generic.List<int>();
            for (int s = 0; s < Seats; s++) if (Body(s) != null) ring.Add(s);
            ring.Sort((a, b) => Bearing(a).CompareTo(Bearing(b)));
            int start = ring.IndexOf(Taya);
            for (int i = 0; i < ring.Count; i++) _round[i] = ring[(start + 1 + i) % ring.Count];
        }

        private void Spot(int s, float angle)
        {
            Vector3 dir = Quaternion.Euler(0.0f, angle, 0.0f) * Vector3.forward;
            _spot[s] = Centre + dir * 2.6f;
            _spot[s].y = Floor(_spot[s], Centre.y);
            _spotYaw[s] = Quaternion.LookRotation(-dir).eulerAngles.y;
        }

        private float Bearing(int s) { Vector3 d = _spot[s] - Centre; return Mathf.Repeat(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, 360.0f); }

        // ------------------------------------------------------------------ building

        private void BuildTorches()
        {
            // A bamboo torch behind each point, its flame and its lamp dark until its walker reaches it.
            Color bamboo = new Color(0.72f, 0.58f, 0.32f), bind = new Color(0.36f, 0.22f, 0.12f);
            for (int k = 0; k < _count; k++)
            {
                int s = _file[k];
                Vector3 out_ = _spot[s] - Centre; out_.y = 0.0f; out_ = out_.sqrMagnitude > 0.01f ? out_.normalized : Vector3.back;
                Vector3 foot = _spot[s] + out_ * 1.6f + Vector3.Cross(Vector3.up, out_) * 0.9f;
                foot.y = Floor(foot, Centre.y);
                Prop(PrimitiveType.Cylinder, "Torch pole " + s, foot + Vector3.up * 0.95f, Quaternion.identity, new Vector3(0.12f, 0.95f, 0.12f), bamboo);
                Prop(PrimitiveType.Cylinder, "Torch cup " + s, foot + Vector3.up * 1.95f, Quaternion.identity, new Vector3(0.24f, 0.1f, 0.24f), bind);
                _torchTop[s] = foot + Vector3.up * 2.12f;
                _flame[s] = new[] { Glow("Flame " + s, _torchTop[s], Vector2.one, Color.clear), Glow("Flame core " + s, _torchTop[s], Vector2.one, Color.clear), Glow("Flame halo " + s, _torchTop[s], Vector2.one, Color.clear) };
                _torchLamp[s] = Lamp("Torch " + s, _torchTop[s] + Vector3.up * 0.2f, new Color(1.0f, 0.66f, 0.34f), 6.0f);
            }
        }

        private void BuildDisc()
        {
            // The disc's light: a gold ring on the stone round the can, a core, and twelve rays, all dark until the rite.
            Vector3 floor = Centre; floor.y = Floor(floor, Centre.y) + 0.02f;
            _ring = Glow("Disc ring", floor, new Vector2(7.6f, 7.6f), Color.clear, Vector3.up, Ring);
            _core = Glow("Disc core", floor + Vector3.up * 0.005f, new Vector2(2.6f, 2.6f), Color.clear, Vector3.up, Soft);
            foreach (var r in new[] { _ring, _core }) r.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            for (int i = 0; i < _rays.Length; i++)
            {
                float a = i * 360.0f / _rays.Length;
                Vector3 dir = Quaternion.Euler(0.0f, a, 0.0f) * Vector3.forward;
                _rays[i] = Glow("Disc ray " + i, floor + dir * 2.2f + Vector3.up * 0.004f, new Vector2(0.22f, 2.4f), Color.clear, Vector3.up, Texture2D.whiteTexture);
                _rays[i].transform.rotation = Quaternion.Euler(90.0f, a, 0.0f);
            }
            _pillar = Glow("Pillar", Centre, new Vector2(1.3f, 9.0f), Color.clear, null, Soft);
            _pillarFoot = Glow("Pillar foot", floor, new Vector2(2.0f, 2.0f), Color.clear, Vector3.up, Ring);
            _pillarFoot.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            _pillarLamp = Lamp("Pillar lamp", Centre + Vector3.up * 7.0f, new Color(1.0f, 0.82f, 0.46f), 12.0f, LightType.Spot, 24.0f);
        }

        private void FindBulul()
        {
            // The seated bulul west of the plaza (the art's own statue), else where the survey put it.
            foreach (var r in FindObjectsByType<Renderer>())
                if (r.name.StartsWith("bulul_seated") && r.bounds.center.x < Centre.x) { _bulul = r.bounds.center; _bululTop = r.bounds.max.y; break; }
            _bululFace = new Vector3(_bulul.x + 2.2f, _bululTop - 2.6f, _bulul.z);
            _bululSun = Lamp("Bulul sun", _bululFace + new Vector3(8.0f, 3.0f, -3.0f), new Color(1.0f, 0.7f, 0.42f), 22.0f, LightType.Spot, 30.0f);
            _bululSun.transform.rotation = Quaternion.LookRotation(_bululFace - _bululSun.transform.position);
            for (int i = 0; i < 2; i++)
            {
                Vector3 at = new Vector3(_bulul.x + 5.0f, 0.0f, _bulul.z + (i == 0 ? -3.4f : 3.4f));
                at.y = Floor(at, _bulul.y - 6.0f) + 1.6f;
                _bululFlame[i] = new[] { Glow("Bulul flame " + i, at, Vector2.one, Color.clear), Glow("Bulul flame core " + i, at, Vector2.one, Color.clear), Glow("Bulul halo " + i, at, Vector2.one, Color.clear) };
                _bululTorch[i] = Lamp("Bulul torch " + i, at + Vector3.up * 0.3f, new Color(1.0f, 0.6f, 0.26f), 10.0f);
            }
        }

        // ------------------------------------------------------------------ the frame

        protected override void Stage(float age)
        {
            Tint(Dawn, 1.0f);
            Fires(age);
            Disc(age);
            Walkers(age);

            if (Still) { StageShort(age); return; }
            if (age < BululAt) { Beat = "mist"; Mist(age); return; }
            if (age < FileAt) { Beat = "bulul"; BululShot(age); return; }
            if (age < RiteAt) { Beat = "file"; FileShots(age); return; }
            if (age < ChooseAt_) { Beat = "rite"; RiteShots(age); return; }
            if (age < ReactAt_) { Beat = "choose"; ChooseShot(age); return; }
            if (age < TayaCut) { Beat = "react"; ReactShot(age); return; }
            Beat = "taya";
            TayaShots(age);
        }

        private void StageShort(float age)
        {
            if (age < _chooseAt) { Beat = "rite"; Shoot(Centre + new Vector3(6.5f, 6.0f, -8.0f), Centre + new Vector3(0.0f, 0.6f, 0.0f), 50.0f); }
            else if (age < _reactAt) { Beat = "choose"; ChooseShot(age); }
            else if (age < _tayaCut) { Beat = "react"; ReactShot(age); }
            else { Beat = "taya"; TayaShots(age); }
        }

        // ---- the fires: a flame is three billboards (an outer tongue, a white core, a soft halo) and a lamp.

        private void Flame(Renderer[] flame, Light lamp, Vector3 top, float on, float age, float seed, float size = 1.0f)
        {
            if (flame == null) return;
            float flick = 0.86f + 0.14f * Mathf.Sin(age * 17.0f + seed) * Mathf.Sin(age * 7.3f + seed * 2.0f);
            float pop = 1.0f + 0.5f * Hit(on < 1.0f ? 0.0f : 0.0f, 0.05f, 6.0f);
            Billboard(flame[0], top + Vector3.up * 0.18f * size, new Color(1.0f, 0.5f, 0.12f, 0.9f * on), 0.62f * size * flick * on * pop);
            Billboard(flame[1], top + Vector3.up * 0.12f * size, new Color(1.0f, 0.93f, 0.7f, 0.95f * on), 0.3f * size * flick * on);
            Billboard(flame[2], top + Vector3.up * 0.2f * size, new Color(1.0f, 0.6f, 0.25f, 0.3f * on), 2.2f * size * flick * on);
            // (Film r2 at 3 with a 9 m reach: the four together washed the whole plaza orange.)
            if (lamp != null) lamp.intensity = 1.6f * on * flick;
        }

        private void Fires(float age)
        {
            for (int k = 0; k < _count; k++)
            {
                int s = _file[k];
                float lit = Still ? 1.0f : Span(age, _arrive[s], _arrive[s] + 0.25f);
                float catchUp = Still ? 0.0f : Hit(age - _arrive[s], 0.05f, 4.0f);
                Flame(_flame[s], _torchLamp[s], _torchTop[s], Mathf.Clamp01(lit * (1.0f + 0.6f * catchUp)), age, s * 1.7f, 1.0f + 0.5f * catchUp);
                if (Crossed(_arrive[s])) Cue("sfx_film_banaue_torch", 0.6f, 0.95f + 0.05f * k);
            }
            for (int i = 0; i < 2; i++)
            {
                float lit = Still ? 0.0f : Span(age, 4.3f + 0.15f * i, 4.55f + 0.15f * i);
                Flame(_bululFlame[i], _bululTorch[i], _bululFlame[i] != null ? _bululFlame[i][0].transform.position - Vector3.up * 0.18f * 1.4f : Vector3.zero, lit, age, 9.0f + i, 1.4f);
            }
            if (!Still && Crossed(4.3f)) Cue("sfx_film_banaue_torch", 0.7f, 0.85f);
            if (_bululSun != null) _bululSun.intensity = Still ? 0.0f : 3.2f * Span(age, 3.85f, 4.4f) * (1.0f - Span(age, FileAt, FileAt + 0.1f));
        }

        // ---- the disc and the choosing light.

        private void Disc(float age)
        {
            // The rite: from the middle outward, the core, then the rays one after another round, then the ring.
            float wake = Still ? 1.0f : Span(age, _riteAt + 0.4f, _riteAt + 1.6f);
            float breathe = 0.85f + 0.15f * Mathf.Sin(age * 3.0f);
            Color gold = new Color(1.0f, 0.78f, 0.32f);
            float flare = Hit(age - _landAt, 0.05f, 2.2f);
            _core.sharedMaterial.color = new Color(gold.r, gold.g, gold.b, 0.6f * wake * breathe + 0.4f * flare);
            _core.enabled = wake > 0.01f || flare > 0.01f;
            for (int i = 0; i < _rays.Length; i++)
            {
                float ray = Still ? 1.0f : Span(age, _riteAt + 0.6f + 0.06f * i, _riteAt + 0.85f + 0.06f * i);
                _rays[i].sharedMaterial.color = new Color(gold.r, gold.g, gold.b, 0.75f * ray * breathe);
                _rays[i].enabled = ray > 0.01f;
            }
            float ring = Still ? 1.0f : Span(age, _riteAt + 1.4f, _riteAt + 1.8f);
            _ring.sharedMaterial.color = new Color(gold.r, gold.g, gold.b, 0.8f * ring * breathe);
            _ring.enabled = ring > 0.01f;
            if (Crossed(_riteAt + 0.05f)) Cue("sfx_film_banaue_gong", 0.85f, 0.9f);

            // The choosing: the light runs round the ring, slowing, and lands on the taya.
            int on = Chosen(age, out float since);
            bool choosing = age >= _chooseAt && age < _tayaCut + 0.2f;
            if (choosing && on >= 0 && Body(on) != null)
            {
                Vector3 at = Mark(on) == Vector3.zero ? _spot[on] : (age < _tayaCut ? _spot[on] : Mark(on));
                float landed = age >= _landAt ? 1.0f : 0.0f;
                float strength = landed > 0.0f ? 1.0f + 0.8f * flare : 0.55f + 0.45f * Mathf.Exp(-since * 6.0f);
                Billboard(_pillar, at + Vector3.up * 4.4f, new Color(1.0f, 0.88f, 0.56f, 0.3f * strength), -1.0f);
                _pillar.transform.localScale = new Vector3(1.3f + 0.6f * flare, 9.0f, 1.0f);
                _pillarFoot.transform.position = new Vector3(at.x, Floor(at, Centre.y) + 0.03f, at.z);
                _pillarFoot.sharedMaterial.color = new Color(1.0f, 0.86f, 0.5f, 0.85f * Mathf.Min(1.0f, strength));
                _pillarFoot.transform.localScale = Vector3.one * (1.8f + 1.4f * flare);
                _pillarFoot.enabled = true;
                _pillarLamp.transform.position = at + new Vector3(0.0f, 7.0f, -1.5f);
                _pillarLamp.transform.rotation = Quaternion.LookRotation(at + Vector3.up - _pillarLamp.transform.position);
                _pillarLamp.intensity = 2.0f * Mathf.Min(1.4f, strength);
            }
            else { _pillar.enabled = false; _pillarFoot.enabled = false; _pillarLamp.intensity = 0.0f; }
            if (Crossed(_landAt)) { Cue("sfx_film_banaue_flare", 0.9f); Cue("sfx_film_banaue_gong", 0.95f, 0.8f); Punch(0.8f); Flash(new Color(1.0f, 0.86f, 0.5f), 0.15f); ChipRoles = true; }
        }

        /// <summary>Who the light is on (the round from the one after the taya, two laps, slowing, ending on the
        /// taya at `_landAt`), and how long it has been there. -1 before it starts.</summary>
        private int Chosen(float age, out float since)
        {
            since = 0.0f;
            if (age < _chooseAt || _count == 0) return -1;
            // Hops at times that stretch out like a wheel slowing: the gaps grow geometrically to the landing.
            int hops = Still ? _count : _count * 2;
            float total = _landAt - _chooseAt, first = 0.0f, ratio = 1.32f, sum = 0.0f;
            for (int i = 0; i < hops - 1; i++) sum += Mathf.Pow(ratio, i);
            first = total / Mathf.Max(0.001f, sum);
            float t = _chooseAt; int index = 0;
            for (int i = 0; i < hops - 1; i++)
            {
                float next = t + first * Mathf.Pow(ratio, i);
                if (age < next) { since = age - t; return _round[index % _count]; }
                t = next; index++;
                if (Crossed(t)) Cue("sfx_film_banaue_tick", 0.6f, 1.0f + 0.04f * (index % 4));
            }
            since = age - _landAt;
            return Taya;
        }

        // ---- the walkers.

        private void Walkers(float age)
        {
            int lit = Chosen(age, out float since);
            for (int k = 0; k < _count; k++)
            {
                int s = _file[k];
                if (!Posable(s)) continue;
                Greet(s, 0.001f);
                float start = FileAt - 0.2f + 0.35f * k;
                if (Still)
                {
                    if (age < _tayaCut) { Place(s, _spot[s], _spotYaw[s]); Animate(s, false); Ground(s, _spot[s].y); }
                    else { Home(s); Animate(s, false); }
                }
                else if (age < start) { ShowSeat(s, false); Place(s, _from[s], 0.0f); Animate(s, false); }
                else if (age < _arrive[s])
                {
                    ShowSeat(s, true);
                    // Up out of the south to the plaza, then round the disc to the point: a slow, even step.
                    float total = _arrive[s] - start;
                    float a = Vector3.Distance(_from[s], _via[s]), b = Vector3.Distance(_via[s], _spot[s]);
                    float d = (age - start) / total * (a + b);
                    Vector3 from = d < a ? _from[s] : _via[s], to = d < a ? _via[s] : _spot[s];
                    float p = d < a ? d / Mathf.Max(0.01f, a) : (d - a) / Mathf.Max(0.01f, b);
                    Vector3 at = Vector3.Lerp(from, to, p); at.y = Floor(at, Centre.y);
                    Vector3 way = to - from; way.y = 0.0f;
                    float yaw = way.sqrMagnitude > 0.01f ? Quaternion.LookRotation(way).eulerAngles.y : _spotYaw[s];
                    if (d >= a) yaw = Mathf.LerpAngle(yaw, _spotYaw[s], Smooth((p - 0.7f) / 0.3f));
                    Place(s, at, yaw, 3.0f);
                    Animate(s, true, 0.8f);
                    Ground(s, at.y);
                }
                else if (age < _tayaCut)
                {
                    ShowSeat(s, true);
                    Place(s, _spot[s], _spotYaw[s]); Animate(s, false); Ground(s, _spot[s].y);
                }
                else
                {
                    // Back to the marks: the three walk off to theirs; the taya backs to theirs, facing the three.
                    float p = Mathf.Clamp01((age - _tayaCut - 0.1f) / 1.3f);
                    if (p >= 1.0f) { Home(s); Animate(s, false); }
                    else Walk(s, _spot[s], Mark(s), p, MarkYaw(s));
                }
                Act(s, Acting(s, age, lit, since));
            }
        }

        /// <summary>
        /// THE RITE, ACTED (owner, 2026-10-23: emotion and readability are a top bar). The file: heads bowed, slow.
        /// At the points: hands together, still; the rite: both arms raised to the sky, heads back. The choosing: each
        /// the light passes flinches and shrinks; the rest lean in. The landing: the taya thrown back by it, then
        /// sagging; the three bow to them low, arms wide (a mock ceremony), and come up laughing; after the cut the
        /// taya shakes it off and squares up.
        /// </summary>
        private Feel Acting(int s, float age, int lit, float since)
        {
            Vector3 f = Facing(s), si = Side(s);
            if (!Still && age < _arrive[s]) return new Feel { Lean = 10.0f, Nod = 26.0f, Left = Vector3.down + f * 0.15f - si * 0.1f, Right = Vector3.down + f * 0.15f + si * 0.1f, Arms = 0.6f };
            var prayer = new Feel { Lean = 6.0f, Nod = 16.0f, Left = f + Vector3.up * 0.25f + si * 0.12f, Right = f + Vector3.up * 0.25f - si * 0.12f, Arms = 0.9f };
            var raised = new Feel { Lean = -14.0f, Nod = -32.0f, Left = Vector3.up - si * 0.45f, Right = Vector3.up + si * 0.45f, Arms = 1.0f };
            float rite = Still ? 0.0f : Span(age, _riteAt + 0.2f, _riteAt + 0.6f) * (1.0f - Span(age, _chooseAt - 0.2f, _chooseAt + 0.1f));
            Feel feel = Feel.Lerp(prayer, raised, rite);
            if (age < _landAt)
            {
                if (age >= _chooseAt)
                {
                    // Waiting on the light: leaning in; struck by it as it passes.
                    feel = Feel.Lerp(prayer, new Feel { Lean = 14.0f, Nod = 4.0f, Turn = lit >= 0 ? LookTurn(s, Chest(lit)) : 0.0f, Left = prayer.Left, Right = prayer.Right, Arms = 0.9f }, 0.7f);
                    if (lit == s) feel = Feel.Lerp(feel, Big(Flinch(s, since), 1.3f), Hit(since, 0.04f, 3.0f) * 1.2f);
                }
                return feel;
            }
            float ya = age - _landAt;
            if (s == Taya)
            {
                if (age < _tayaCut)
                {
                    var shock = Big(Shock(s), 1.45f);
                    shock.Hop = 0.24f * Hit(ya, 0.07f, 6.0f);
                    if (ya < 0.3f) return Feel.Lerp(prayer, shock, Smooth(ya / 0.08f));
                    var slump = Big(Slump(s, Mathf.Sin((ya - 0.3f) * 2.4f)), 1.3f);
                    return Feel.Lerp(shock, slump, Smooth((ya - 0.3f) / 0.45f));
                }
                float t = age - _tayaCut;
                var off = Feel.Lerp(Slump(s, 0.0f), HeadShake(s, t), Smooth(t / 0.2f));
                return Feel.Lerp(off, Defiant(s, Centre + new Vector3(0.0f, 1.3f, 9.0f)), Smooth((t - 0.75f) / 0.25f));
            }
            // The three: a deep mock bow to the taya, arms out wide, then up laughing.
            Vector3 tayaChest = Chest(Taya);
            var bow = new Feel { Lean = 48.0f, Nod = 20.0f, Turn = LookTurn(s, tayaChest) * 0.4f, Left = -si + Vector3.down * 0.3f - f * 0.3f, Right = si + Vector3.down * 0.3f - f * 0.3f, Arms = 1.0f };
            int kind = 0;
            for (int o = 0; o < Seats; o++) if (o != Taya && o != s && Body(o) != null && Bearing(o) < Bearing(s)) kind++;
            Feel laugh = kind == 0 ? Big(LaughAt(s, tayaChest, ya, true), 1.5f) : kind == 1 ? Big(DoubledOver(s, ya), 1.4f)
                : new Feel { Lean = -22.0f, Nod = -30.0f, Rock = 9.0f * Mathf.Sin(ya * 5.0f), Left = Vector3.up - si * 0.55f, Right = Vector3.up + si * 0.55f, Arms = 1.0f, Hop = 0.1f * Mathf.Abs(Mathf.Sin(ya * 10.0f)) };
            var now = Feel.Lerp(Feel.Lerp(feel, bow, Smooth((ya - 0.15f) / 0.3f)), laugh, Smooth((ya - 0.85f - 0.1f * kind) / 0.3f));
            if (age >= _tayaCut) { var walking = laugh; walking.Hop = 0.0f; walking.Lean *= 0.5f; now = Feel.Lerp(now, walking, 0.7f); }
            return now;
        }

        private static Feel Big(Feel f, float k)
        {
            f.Lean *= k; f.Twist *= k; f.Rock *= k; f.Nod *= k; f.Tilt *= k; f.Hop *= k;
            return f;
        }

        // ------------------------------------------------------------------ the shots

        private void Mist(float age)
        {
            // Up the terraces south of the plaza a step a gong, looking up them at the waterfall; then over the top,
            // tilting down to the plaza with the misty valley and the low sun beyond (film r1 climbed out of the
            // valley into the plaza's carved stone base and saw only its pillars).
            float climb = 0.0f;
            for (int i = 0; i < Steps.Length; i++) climb += Smooth((age - Steps[i]) / 0.55f) * 0.25f;
            climb = Mathf.Clamp01(climb * 0.85f + Span(age, 0.0f, BululAt) * 0.15f);
            Vector3 eye = Centre + new Vector3(Mathf.Lerp(-7.0f, -3.0f, climb), Mathf.Lerp(1.5f, 24.0f, climb), Mathf.Lerp(-20.0f, -38.0f, climb));
            Vector3 up = new Vector3(eye.x * 0.5f, eye.y + 4.0f, Centre.z - 80.0f);
            Vector3 plaza = Centre + new Vector3(0.0f, 0.0f, 6.0f);
            Vector3 aim = Vector3.Lerp(up, plaza, Span(age, 2.4f, BululAt - 0.1f));
            Shoot(eye, aim, Mathf.Lerp(52.0f, 50.0f, climb), Mathf.Lerp(-3.0f, 0.0f, climb));
            for (int i = 0; i < Steps.Length; i++) if (Crossed(Steps[i])) { Cue("sfx_film_banaue_gong", 0.6f + 0.08f * i, 1.0f + 0.06f * i); Punch(0.25f); }
        }

        private void BululShot(float age)
        {
            // Low at the bulul's feet, up at its face as the first sun warms it, the torches at its feet catching.
            float p = Smooth((age - BululAt) / (FileAt - BululAt));
            Vector3 eye = new Vector3(_bulul.x + Mathf.Lerp(10.5f, 9.5f, p), _bulul.y - 3.6f + Mathf.Lerp(0.0f, 0.6f, p), _bulul.z - Mathf.Lerp(3.5f, 2.6f, p));
            Shoot(ClearEye(_bululFace, eye), Vector3.Lerp(_bululFace, _bululFace - Vector3.up * 3.0f, 0.3f), 44.0f, 2.0f);
        }

        private void FileShots(float age)
        {
            if (age < FileHigh)
            {
                // Low at the top of the south stairs: the file rising into frame, heads bowed, at the lens.
                float p = Smooth((age - FileAt) / (FileHigh - FileAt));
                // (Film r1 square on: the first walker hid the rest; r2 three-quarter: the camera stood in a walker's
                // way and their belly filled the frame.) Side on from the east, off every path: the file in profile,
                // heads bowed, stepping up past the lens.
                Vector3 eye = Centre + new Vector3(Mathf.Lerp(5.4f, 5.0f, p), 1.15f, -7.4f);
                Shoot(ClearEye(Centre + Vector3.up, eye), Centre + new Vector3(-0.4f, 0.85f, -8.6f), 50.0f, 1.0f);
                return;
            }
            // Over the ring, high: they part to the four points; a torch catches at each.
            float q = Smooth((age - FileHigh) / (RiteAt - FileHigh));
            Vector3 e = Centre + new Vector3(Mathf.Lerp(-5.0f, -4.0f, q), Mathf.Lerp(6.5f, 6.0f, q), Mathf.Lerp(-7.5f, -6.4f, q));
            Shoot(e, Centre + Vector3.up * 0.4f, 50.0f);
        }

        private void RiteShots(float age)
        {
            if (age < RiteLow)
            {
                // Straight down on the disc, turning slowly: four figures at its points, arms raised, the rays waking.
                float p = (age - RiteAt) / (RiteLow - RiteAt);
                Vector3 eye = Centre + Vector3.up * Mathf.Lerp(11.5f, 10.0f, Smooth(p));
                Quaternion turn = Quaternion.Euler(0.0f, Mathf.Lerp(-10.0f, 14.0f, p), 0.0f);
                Shoot(eye, Centre + turn * Vector3.forward * 0.02f, 52.0f);
                return;
            }
            // Low at the ring's edge between two of them, the gold coming up the stone toward the lens.
            float q = Smooth((age - RiteLow) / (ChooseAt_ - RiteLow));
            // (Film r1 at 4 m: two raised arms filled the lens.)
            Vector3 e = Centre + new Vector3(Mathf.Lerp(6.6f, 6.1f, q), 0.9f, Mathf.Lerp(-6.2f, -5.7f, q));
            Shoot(ClearEye(Centre + Vector3.up, e), Centre + new Vector3(-0.3f, 1.0f, 0.4f), 50.0f, -2.0f);
        }

        private void ChooseShot(float age)
        {
            // From high on the north side, the four lit one after another round the ring, the taya facing the lens.
            float p = Still ? 0.0f : Smooth((age - _chooseAt) / (_reactAt - _chooseAt));
            Vector3 eye = Centre + new Vector3(Mathf.Lerp(-2.0f, -1.4f, p), Mathf.Lerp(5.2f, 4.6f, p), Mathf.Lerp(6.8f, 6.0f, p));
            Shoot(ClearEye(Centre + Vector3.up, eye), Centre + new Vector3(0.0f, 0.5f, -0.4f), 54.0f);
        }

        private void ReactShot(float age)
        {
            // The taya in the light, struck and sagging, the three's mock bow either side: high on the north side.
            float p = Still ? 0.0f : Smooth((age - _reactAt) / (_tayaCut - _reactAt));
            Vector3 taya = _spot[Taya];
            // (Film r1 from the north at 3 m: the north walker's head across the frame; r2 from the north-east: the taya
            // small and turned away.) High over the north point, looking down on the taya's face, the bows either side.
            Vector3 eye = Centre + new Vector3(Mathf.Lerp(1.0f, 0.7f, p), Mathf.Lerp(5.6f, 5.2f, p), Mathf.Lerp(3.4f, 3.0f, p));
            Shoot(ClearEye(taya + Vector3.up, eye), Vector3.Lerp(Centre + Vector3.up * 0.6f, taya + Vector3.up * 0.8f, 0.7f), 46.0f, Still ? 0.0f : -1.5f);
        }

        /// <summary>The taya from the front, to their right and a little high (film r1's framing from the north put the
        /// can large across the foreground: it stands between the taya and the three).</summary>
        private void TayaFrame(float local, out Vector3 eye, out Vector3 aim, out float fov)
        {
            Vector3 taya = Chest(Taya);
            eye = Mark(Taya) + new Vector3(2.5f, 1.45f, 2.4f);
            eye = Vector3.Lerp(eye, taya, 0.08f * Smooth(local));
            aim = taya + Vector3.up * 0.05f;
            fov = 40.0f;
        }

        private void TayaShots(float age)
        {
            float local = Still ? 0.0f : (age - _tayaCut) / (CloseAt(!Still) - _tayaCut);
            TayaFrame(local, out var eye, out var aim, out var fov);
            Shoot(eye, aim, fov);
        }

        protected override void TayaShot(float local, out Vector3 eye, out Vector3 aim, out float fov) => TayaFrame(1.0f, out eye, out aim, out fov);
    }
}
