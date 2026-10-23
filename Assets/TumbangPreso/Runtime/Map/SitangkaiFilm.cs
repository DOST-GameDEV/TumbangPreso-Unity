using UnityEngine;

namespace TumbangPreso.Map
{
    /// <summary>
    /// SITANGKAI (LagoonCove): CALM WONDER. Golden hour on the reef. The four come in from the stilt village on one
    /// banca, the kids on the boardwalk cheering them past; it runs up on the beach and they hop off into the sand. A
    /// lola carries the can out, sets it down in the middle of the box, looks them over, and points: the taya is hers
    /// to give. The score is a kulintang over a slow swell, an agung on her point.
    ///
    /// THE BEATS (seconds, full film / short film):
    ///   GLIDE   0.0 / none   Low over the reef by the village, looking into the low sun, the banca setting out from
    ///                         the houses; the camera glides south with it and turns to follow. SITANGKAI at 0.9.
    ///   BANCA   3.6 / none   Three cuts. Alongside on the water (3.6): the four aboard, each excited their own way
    ///                         (pointing at the court, waving at the kids, a cheer, shading the eyes). From the sand at
    ///                         the bow (5.0): the bow runs at the lens and grounds (5.55), everyone lurches. Beside the
    ///                         bow (5.9): they hop off one after another and land crouched in a puff of sand.
    ///   KIDS    6.9 / none   On the boardwalk by a stilt house: three kids jumping, waving and cheering, the beached
    ///                         banca and the four walking up the beach beyond them.
    ///   WALK    8.1 / none   In front of the four on the beach, into the sun: walking at the lens, waving back.
    ///   LOLA    9.6 / 1.6    Low by the can's spot: the lola walks in with the can held out, bends and sets it down
    ///                         (10.75, the clink), straightens with a hand on her back.
    ///   LOOK    11.0 / 1.6   Wide from the south-east: the four on their marks, all eyes on her; she looks them over.
    ///   POINT   12.0 / 2.6   Over her shoulder: she points at the taya; the agung; TAYA stamps (12.25). The taya is
    ///                         struck and sags. LAUGH (12.9): over the taya's shoulder at the three laughing at them,
    ///                         the lola cackling. TAYA (13.7 / 3.2): close on the taya shaking it off and squaring up.
    ///   CLOSE   15.2 / 4.8   Each attacker's own screen ends on themselves; the taya's and a watcher's hold the taya.
    ///
    /// The banca is a drawn copy of the village's nearest bangka (`Ghost`); the real one stays moored. The can is a
    /// drawn copy in her hands until it is set down, then the real one is drawn again. Everything goes with `End`.
    /// </summary>
    public sealed class SitangkaiFilm : MapFilm
    {
        protected override string Region => "TAWI-TAWI";
        protected override Color Accent => new Color(0.22f, 0.8f, 0.78f);
        protected override string Score => "sfx_film_sitangkai_score";
        protected override float CloseAt(bool full) => full ? 15.2f : 4.8f;

        private const float BancaA = 3.6f, BancaB = 5.0f, Grounds = 5.55f, BancaC = 5.9f, KidsAt = 6.9f, WalkAt = 8.1f;
        private const float LolaAt_ = 9.6f, SetDown = 10.75f, LookAt_ = 11.0f, PointAt_ = 12.0f, LaughAt_ = 12.9f, TayaCut = 13.7f;
        private const float HopGap = 0.22f, HopTime = 0.45f;
        private float _lolaAt, _setDown, _lookAt, _pointAt, _tayaCut;

        /// <summary>Golden hour: a low gold sun, a warm sea haze, the sky blue going to honey at the horizon (the map's
        /// own light is a pink sunset; this is the hour before it).</summary>
        private static readonly Mood Golden = new Mood
        {
            Sky = new Color(0.62f, 0.66f, 0.78f), Equator = new Color(0.86f, 0.7f, 0.52f), Ground = new Color(0.5f, 0.4f, 0.3f),
            Fog = new Color(1.0f, 0.8f, 0.58f), FogStart = 45.0f, FogEnd = 320.0f,
            Zenith = new Color(0.36f, 0.52f, 0.78f), Horizon = new Color(1.0f, 0.76f, 0.46f),
            Sun = new Color(1.0f, 0.78f, 0.5f), SunIntensity = 1.25f, SunElevation = 14.0f,
            Brightness = 1.02f, Saturation = 1.14f, Lamps = 0.6f, KeepYaw = true,
        };

        // ---- the banca
        private GameObject _boat;
        private float _water = -1.8f, _boatLength = 5.0f, _deck = 0.3f, _modelYaw;
        private Vector3 _boatCentreOffset;
        private Vector3 _p0, _p1, _p2, _landing;
        private readonly Renderer[] _wake = new Renderer[8];
        private readonly Renderer[] _spray = new Renderer[4];

        // ---- the four
        private readonly int[] _place = new int[Seats];      // each seat's place on the banca, bow first
        private int _aboard;
        private readonly Vector3[] _sand = new Vector3[Seats], _upBeach = new Vector3[Seats];
        private readonly Renderer[] _puff = new Renderer[Seats];

        // ---- the village
        private readonly Extra[] _kids = new Extra[3];
        private readonly Vector3[] _kidAt = new Vector3[3];
        private Vector3 _kidsMid;
        private Extra _lola;
        private Vector3 _lolaFrom, _lolaSpot;
        private GameObject _can;
        private Renderer[] _realCan = new Renderer[0];

        protected override void Prepare()
        {
            ChipRoles = false;
            _lolaAt = Still ? 0.0f : LolaAt_;
            _setDown = Still ? 0.0f : SetDown;
            _lookAt = Still ? 1.6f : LookAt_;
            _pointAt = Still ? 2.6f : PointAt_;
            _tayaCut = Still ? 3.2f : TayaCut;
            TitleAt(Still ? 0.2f : 0.9f, Still ? 1.4f : 3.0f);
            TayaAt(_pointAt + 0.25f, CloseAt(!Still) - _pointAt - 0.25f);
            if (!Still) { Cut(BancaA); Cut(BancaB); Cut(BancaC); Cut(KidsAt); Cut(WalkAt); Cut(LolaAt_); Cut(LookAt_); Cut(PointAt_); Cut(LaughAt_); }
            else { Cut(_lookAt); }
            Cut(_tayaCut);

            var water = GameObject.Find("Water");
            if (water != null) _water = water.transform.position.y;

            // The landing: the first dry sand walking north from the box, a little east of its middle.
            float landX = Centre.x + 1.0f;
            float landZ = Centre.z + 24.0f;
            for (float z = Centre.z + 12.0f; z < Centre.z + 45.0f; z += 0.5f)
                if (GroundAt(new Vector3(landX, 0.0f, z), out float y) && y < _water + 0.12f) { landZ = z; break; }
            _landing = new Vector3(landX, GroundAt(new Vector3(landX, 0.0f, landZ - 0.8f), out float sand) ? sand : Centre.y, landZ - 0.8f);

            BuildBoat(landX, landZ);
            BuildCast();
            BuildVillage();

            for (int s = 0; s < Seats; s++) if (Body(s) != null && !Still) ChipAt(s, WalkAt + 0.2f + 0.15f * _place[s], 1.15f);
            Clip("sfx_film_sitangkai_motor"); Clip("sfx_film_sitangkai_ground"); Clip("sfx_film_sitangkai_hop"); Clip("sfx_film_sitangkai_clink");
            Clip("sfx_film_sitangkai_gong"); Clip("sfx_film_sitangkai_kids"); Clip("sfx_film_sitangkai_giggle");
        }

        private Collider _ground;

        /// <summary>The cove's own ground (sand, grass, the seabed) under (x, z), ignoring every rock, palm and house
        /// on it (film r1: the highest surface was a palm's crown, 12 m up).</summary>
        private bool GroundAt(Vector3 at, out float y)
        {
            y = Centre.y;
            if (_ground == null) { var g = GameObject.Find("ground"); _ground = g != null ? g.GetComponent<Collider>() : null; }
            if (_ground == null) return false;
            if (!_ground.Raycast(new Ray(new Vector3(at.x, Centre.y + 60.0f, at.z), Vector3.down), out var hit, 120.0f)) return false;
            y = hit.point.y;
            return true;
        }

        /// <summary>Boards over the water under open sky at (x, z): the first thing a ray from the sky meets is a flat
        /// surface a little above the sea (a deck or a boardwalk, not a roof and not under one).</summary>
        private bool Deck(Vector3 at, out Vector3 deck)
        {
            deck = at;
            int count = Physics.RaycastNonAlloc(new Vector3(at.x, _water + 14.0f, at.z), Vector3.down, Hits, 20.0f, ~0, QueryTriggerInteraction.Ignore);
            float top = float.NegativeInfinity; RaycastHit first = default;
            for (int i = 0; i < count; i++)
                if (World(Hits[i].collider) && Hits[i].point.y > top) { top = Hits[i].point.y; first = Hits[i]; }
            if (float.IsNegativeInfinity(top) || first.normal.y < 0.85f || first.collider == _ground) return false;
            if (top < _water + 0.6f || top > _water + 3.5f) return false;
            deck = first.point;
            return true;
        }

        /// <summary>The floor under `at` nearest `near` (within 1.5 m), for decks and the sloping beach.</summary>
        private float FloorNear(Vector3 at, float near)
        {
            int count = Physics.RaycastNonAlloc(new Vector3(at.x, near + 1.5f, at.z), Vector3.down, Hits, 3.0f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity, floor = near;
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (!World(hit.collider) || hit.normal.y < 0.6f) continue;
                float d = Mathf.Abs(hit.point.y - near);
                if (d < best) { best = d; floor = hit.point.y; }
            }
            return floor;
        }

        // ------------------------------------------------------------------ building

        private void BuildBoat(float landX, float landZ)
        {
            // The banca is the film's own, built in the toon look like the village's (a white hull with painted bands,
            // a raised bow and stern, bamboo outriggers on two cross-arms): the cove's boats cannot be copied, as
            // they are static-batched and a "hull" mesh in play is the whole batch (film r1 to r3 measured 54 m and
            // then 227 m of boats).
            BuildBanca();
            // Out from the village, round past the boardwalk, and onto the beach heading south, the bow up the sand.
            _p2 = new Vector3(landX, _water, landZ + _boatLength * 0.32f);
            _p1 = new Vector3(landX - 6.0f, _water, landZ + 13.0f);
            _p0 = new Vector3(landX - 28.0f, _water, landZ + 20.0f);
            for (int i = 0; i < _wake.Length; i++) _wake[i] = Glow("Wake " + i, _p0, Vector2.one, Color.clear);
            for (int i = 0; i < _spray.Length; i++) _spray[i] = Glow("Spray " + i, _p0, Vector2.one, Color.clear);
        }

        private void BuildBanca()
        {
            var root = new GameObject("Film banca").transform;
            root.SetParent(transform, false);
            _boat = Own(root.gameObject);
            Color hull = new Color(0.96f, 0.94f, 0.89f), band = new Color(0.98f, 0.6f, 0.18f), blue = new Color(0.16f, 0.5f, 0.82f);
            Color wood = new Color(0.55f, 0.36f, 0.2f), bamboo = new Color(0.86f, 0.74f, 0.46f);
            const float L = 5.2f, W = 0.9f, H = 0.5f;
            _boatLength = L; _modelYaw = 0.0f; _boatCentreOffset = Vector3.zero;
            _deck = 0.18f;
            void Part(PrimitiveType shape, Vector3 at, Vector3 size, Color colour, Vector3 euler = default)
            {
                var go = Prop(shape, "Banca part", Vector3.zero, Quaternion.identity, size, colour);
                go.transform.SetParent(root, false);
                go.transform.localPosition = at; go.transform.localRotation = Quaternion.Euler(euler);
            }
            // The hull: a floor, two sides, the painted bands along them, a transom at the stern.
            Part(PrimitiveType.Cube, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(W * 0.7f, 0.12f, L * 0.78f), wood);
            foreach (float side in new[] { -1.0f, 1.0f })
            {
                Part(PrimitiveType.Cube, new Vector3(side * W * 0.45f, H * 0.5f, 0.0f), new Vector3(0.1f, H, L * 0.8f), hull, new Vector3(0.0f, 0.0f, side * -8.0f));
                Part(PrimitiveType.Cube, new Vector3(side * W * 0.5f, H * 0.62f, 0.0f), new Vector3(0.04f, 0.11f, L * 0.79f), band, new Vector3(0.0f, 0.0f, side * -8.0f));
                Part(PrimitiveType.Cube, new Vector3(side * W * 0.5f, H * 0.4f, 0.0f), new Vector3(0.04f, 0.06f, L * 0.79f), blue, new Vector3(0.0f, 0.0f, side * -8.0f));
                // The bow and stern sweep up to a point.
                Part(PrimitiveType.Cube, new Vector3(side * W * 0.22f, H * 0.62f, L * 0.45f), new Vector3(0.08f, H * 0.9f, L * 0.22f), hull, new Vector3(-18.0f, side * 22.0f, 0.0f));
                Part(PrimitiveType.Cube, new Vector3(side * W * 0.22f, H * 0.58f, -L * 0.45f), new Vector3(0.08f, H * 0.85f, L * 0.2f), hull, new Vector3(16.0f, side * -22.0f, 0.0f));
                // The outrigger float and its two cross-arms.
                Part(PrimitiveType.Cylinder, new Vector3(side * 2.0f, 0.06f, 0.0f), new Vector3(0.14f, L * 0.42f, 0.14f), bamboo, new Vector3(90.0f, 0.0f, 0.0f));
                foreach (float along in new[] { -0.95f, 0.95f })
                    Part(PrimitiveType.Cylinder, new Vector3(side * 1.05f, H + 0.05f - 0.25f * 0.0f, along), new Vector3(0.09f, 1.0f, 0.09f), bamboo, new Vector3(0.0f, 0.0f, side * 78.0f));
            }
            Part(PrimitiveType.Cube, new Vector3(0.0f, H * 0.45f, -L * 0.39f), new Vector3(W * 0.82f, H * 0.7f, 0.08f), blue);
            // A seat plank across the middle and a little engine housing at the stern.
            Part(PrimitiveType.Cube, new Vector3(0.0f, H * 0.55f, -0.2f), new Vector3(W * 0.82f, 0.06f, 0.3f), wood);
            Part(PrimitiveType.Cube, new Vector3(0.0f, H * 0.55f, -L * 0.33f), new Vector3(0.36f, 0.3f, 0.42f), new Color(0.3f, 0.3f, 0.32f));
        }

        private void BuildCast()
        {
            // Aboard bow to stern in the order they will spread over the sand, west to east.
            var order = new System.Collections.Generic.List<int>();
            for (int s = 0; s < Seats; s++) if (Body(s) != null) order.Add(s);
            order.Sort((a, b) => Mark(a).x.CompareTo(Mark(b).x));
            _aboard = order.Count;
            for (int k = 0; k < order.Count; k++)
            {
                int s = order[k];
                _place[s] = k;
                float across = order.Count <= 1 ? 0.0f : Mathf.Lerp(-1.6f, 1.6f, k / (float)(order.Count - 1));
                Vector3 at = _landing + new Vector3(across, 0.0f, -1.2f - 0.25f * k);
                at.y = GroundAt(at, out float y) ? y : _landing.y;
                _sand[s] = at;
                Vector3 up = at + new Vector3(across * 0.3f, 0.0f, -6.5f);
                up.y = GroundAt(up, out float y2) ? y2 : at.y;
                _upBeach[s] = up;
                _puff[s] = Glow("Sand puff " + s, at, Vector2.one, Color.clear);
            }
        }

        private void BuildVillage()
        {
            // Three kids on the open deck of the stilt house nearest the beach (or the boardwalk beside it): boards
            // over the water, open sky above, the nearest to the landing first, a metre or more apart.
            Vector3 house = Centre + new Vector3(-29.2f, 0.0f, 35.9f);
            var houses = GameObject.Find("Houses");
            if (houses != null)
            {
                float best = float.PositiveInfinity;
                foreach (Transform h in houses.transform)
                {
                    if (!h.name.StartsWith("house_water")) continue;
                    float d = Vector3.Distance(new Vector3(h.position.x, 0.0f, h.position.z), new Vector3(_landing.x, 0.0f, _landing.z));
                    if (d < best) { best = d; house = new Vector3(h.position.x, 0.0f, h.position.z); }
                }
            }
            var decks = new System.Collections.Generic.List<Vector3>();
            for (float dx = -7.0f; dx <= 7.0f; dx += 0.5f)
                for (float dz = -7.0f; dz <= 7.0f; dz += 0.5f)
                {
                    Vector3 probe = house + new Vector3(dx, 0.0f, dz);
                    if (!Deck(probe, out var deck)) continue;
                    decks.Add(deck);
                }
            decks.Sort((p, q) => Vector3.Distance(p, _landing).CompareTo(Vector3.Distance(q, _landing)));
            var chosen = new System.Collections.Generic.List<Vector3>();
            foreach (var d in decks)
            {
                bool apart = true;
                foreach (var c in chosen) if (Vector3.Distance(c, d) < 1.0f) apart = false;
                if (apart) chosen.Add(d);
                if (chosen.Count == _kids.Length) break;
            }
            MatchArrivalPresentation.Trace($"Sitangkai film kids: house {house:F1}, {decks.Count} deck points, chosen {string.Join(" ", chosen)}");
            string[][] who = { new[] { "totoy", "tikboy", "jun_jun", "bebang" }, new[] { "bebang", "jun_jun", "totoy", "tikboy" }, new[] { "jun_jun", "tikboy", "bebang", "totoy" } };
            Vector3 sum = Vector3.zero; int n = 0;
            for (int i = 0; i < _kids.Length; i++)
            {
                Vector3 at = i < chosen.Count ? chosen[i] : house + new Vector3(3.0f + i, 0.2f, -4.0f);
                _kidAt[i] = at;
                _kids[i] = Person("Film kid " + i, 0.62f + 0.04f * i, who[i]);
                if (_kids[i] != null) { _kids[i].Show(false); sum += at; n++; }
            }
            _kidsMid = n > 0 ? sum / n : house;

            // The lola, out from the west with the can, to stand just west of it.
            _lola = Person("Film lola", 0.96f, "lola_pacing", "aling_nena");
            _lolaSpot = Centre + new Vector3(-0.8f, 0.0f, 0.55f);
            _lolaSpot.y = Floor(_lolaSpot, Centre.y);
            _lolaFrom = Centre + new Vector3(-5.2f, 0.0f, 2.2f);
            _lolaFrom.y = Floor(_lolaFrom, Centre.y);
            var lata = GameServices.Round != null && GameServices.Round.Lata != null ? GameServices.Round.Lata : FindFirstObjectByType<Lata>();
            if (lata != null && !Still)
            {
                _realCan = lata.GetComponentsInChildren<Renderer>();
                _can = DrawnCopy(lata.gameObject, "Film can");
            }
        }

        /// <summary>
        /// Just the meshes of `source`, as drawn, under a new owned root at its pivot. Not `Ghost`: that switches its
        /// source off and on to copy it, and the can's own enable and disable must never run for a film.
        /// </summary>
        private GameObject DrawnCopy(GameObject source, string name)
        {
            var root = new GameObject(name);
            root.transform.SetParent(transform, false);
            root.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            foreach (var filter in source.GetComponentsInChildren<MeshFilter>())
            {
                var from = filter.GetComponent<MeshRenderer>();
                if (from == null || !from.enabled || filter.sharedMesh == null) continue;
                var part = new GameObject(filter.name);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = source.transform.InverseTransformPoint(filter.transform.position);
                part.transform.localRotation = Quaternion.Inverse(source.transform.rotation) * filter.transform.rotation;
                part.transform.localScale = filter.transform.lossyScale;
                part.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                part.AddComponent<MeshRenderer>().sharedMaterials = from.sharedMaterials;
            }
            root.SetActive(false);
            return Own(root);
        }

        // ------------------------------------------------------------------ the frame

        protected override void Stage(float age)
        {
            Tint(Golden, Still ? 1.0f : Mathf.Lerp(0.85f, 1.0f, Span(age, 0.0f, 6.0f)));
            Boat(age);
            Four(age);
            Kids(age);
            Lola(age);
            ScoreLevel = 0.85f;

            if (Still) { StageShort(age); return; }
            if (age < BancaA) { Beat = "glide"; Glide(age); return; }
            if (age < KidsAt) { Beat = "banca"; BancaShots(age); return; }
            if (age < WalkAt) { Beat = "kids"; KidsShot(age); return; }
            if (age < LolaAt_) { Beat = "walk"; WalkShot(age); return; }
            if (age < LookAt_) { Beat = "lola"; LolaShot(age); return; }
            if (age < PointAt_) { Beat = "look"; LookShot(age); return; }
            PointShots(age);
        }

        private void StageShort(float age)
        {
            if (age < _lookAt)
            {
                Beat = "establish";
                Vector3 eye = _landing + new Vector3(4.0f, 2.2f, 1.5f);
                Shoot(eye, Centre + new Vector3(0.0f, 0.9f, 3.0f), 52.0f);
            }
            else if (age < _tayaCut) { Beat = age < _pointAt ? "look" : "point"; LookShot(age); }
            else { Beat = "taya"; TayaShots(age); }
        }

        // ---- the banca: out of the village, past the boardwalk, onto the beach.

        private float BoatT(float age) => Still ? 1.0f : 1.0f - Mathf.Pow(1.0f - Mathf.Clamp01(age / Grounds), 1.7f);

        private Vector3 BoatAt(float t) => (1 - t) * (1 - t) * _p0 + 2 * (1 - t) * t * _p1 + t * t * _p2;

        private Vector3 BoatForward(float t)
        {
            Vector3 d = 2 * (1 - t) * (_p1 - _p0) + 2 * t * (_p2 - _p1);
            d.y = 0.0f;
            return d.sqrMagnitude > 0.001f ? d.normalized : Vector3.back;
        }

        /// <summary>The banca's frame this frame: where its middle floats and which way it heads, with its bob, the
        /// lurch as it grounds and the bow riding up the sand.</summary>
        private void BoatFrame(float age, out Vector3 at, out Quaternion turn)
        {
            float t = BoatT(age);
            at = BoatAt(t);
            Vector3 fwd = BoatForward(Mathf.Max(t, 0.001f));
            float aground = Still ? 1.0f : Span(age, Grounds - 0.25f, Grounds + 0.1f);
            float bob = Still ? 0.0f : Mathf.Sin(age * 2.1f) * 0.05f * (1.0f - aground);
            float pitch = Mathf.Sin(age * 1.6f + 0.7f) * 1.6f * (1.0f - aground) - 3.5f * aground;
            float roll = Mathf.Sin(age * 1.3f) * 2.2f * (1.0f - aground);
            float jolt = Still ? 0.0f : Hit(age - Grounds, 0.05f, 6.0f);
            at.y = _water + bob + 0.12f * aground + 0.05f * jolt;
            turn = Quaternion.LookRotation(fwd) * Quaternion.Euler(pitch - 2.5f * jolt, 0.0f, roll);
        }

        private void Boat(float age)
        {
            if (_boat == null) return;
            BoatFrame(age, out var at, out var turn);
            Quaternion model = turn * Quaternion.Euler(0.0f, _modelYaw, 0.0f);
            _boat.transform.SetPositionAndRotation(at - model * _boatCentreOffset, model);
            if (Crossed(0.05f)) Cue("sfx_film_sitangkai_motor", 0.55f);
            if (Crossed(Grounds)) { Cue("sfx_film_sitangkai_ground", 0.8f); Punch(0.6f); }

            // The wake: foam left behind on the water, spreading and fading; spray off the bow while it runs.
            float speed = Still ? 0.0f : 1.0f - Span(age, Grounds - 0.6f, Grounds);
            Vector3 fwd = turn * Vector3.forward;
            for (int i = 0; i < _wake.Length; i++)
            {
                float back = 0.6f + i * 0.9f;
                float tBack = Mathf.Clamp01(BoatT(Mathf.Max(0.0f, age - back * 0.12f)));
                Vector3 p = BoatAt(tBack) - BoatForward(Mathf.Max(tBack, 0.001f)) * (_boatLength * 0.5f);
                p.y = _water + 0.04f;
                float a = 0.55f * speed * (1.0f - i / (float)_wake.Length);
                Billboard(_wake[i], p, new Color(1.0f, 1.0f, 0.97f, a), 0.7f + 0.35f * i);
            }
            for (int i = 0; i < _spray.Length; i++)
            {
                float side = i % 2 == 0 ? 1.0f : -1.0f;
                Vector3 p = at + fwd * (_boatLength * 0.42f) + Vector3.Cross(Vector3.up, fwd) * side * (0.45f + 0.15f * Mathf.Sin(age * 9.0f + i)) + Vector3.up * (0.15f + 0.1f * (i / 2));
                float burst = Still ? 0.0f : Hit(age - Grounds, 0.04f, 3.5f) * 0.9f;
                Billboard(_spray[i], p, new Color(1.0f, 1.0f, 1.0f, Mathf.Max(0.45f * speed, burst)), 0.5f + 0.6f * burst);
            }
        }

        // ---- the four: aboard, the hop off, the walk up the beach, then their marks.

        private float HopAt(int s) => BancaC + 0.1f + HopGap * _place[s];

        private void Four(float age)
        {
            BoatFrame(age, out var boatAt, out var boatTurn);
            Vector3 fwd = boatTurn * Vector3.forward; fwd.y = 0.0f; fwd.Normalize();
            float boatYaw = Quaternion.LookRotation(fwd).eulerAngles.y;
            for (int s = 0; s < Seats; s++)
            {
                if (!Posable(s)) continue;
                Greet(s, 0.001f);
                ShowSeat(s, true);
                float hop = age - HopAt(s);
                bool onMarks = Still || age >= LolaAt_;
                if (onMarks) { Home(s); Animate(s, false); }
                else if (hop < 0.0f)
                {
                    // Aboard: bow first, standing in a line down the middle.
                    float along = _aboard <= 1 ? 0.0f : Mathf.Lerp(0.3f, -0.3f, _place[s] / (float)(_aboard - 1)) * _boatLength;
                    Vector3 at = boatAt + boatTurn * new Vector3(0.0f, _deck, along);
                    Place(s, at, boatYaw + (_place[s] == 1 ? -25.0f : 0.0f));
                    Animate(s, false);
                    Ground(s, at.y);
                }
                else if (hop < HopTime)
                {
                    // Over the gunwale and down onto the sand.
                    float p = hop / HopTime;
                    float along = _aboard <= 1 ? 0.0f : Mathf.Lerp(0.3f, -0.3f, _place[s] / (float)(_aboard - 1)) * _boatLength;
                    Vector3 from = boatAt + boatTurn * new Vector3(0.0f, _deck, along);
                    Vector3 at = Vector3.Lerp(from, _sand[s], Smooth(p)) + Vector3.up * (Mathf.Sin(p * Mathf.PI) * 0.75f);
                    Vector3 way = _sand[s] - from; way.y = 0.0f;
                    Place(s, at, way.sqrMagnitude > 0.01f ? Quaternion.LookRotation(way).eulerAngles.y : boatYaw, -10.0f * Mathf.Sin(p * Mathf.PI));
                    Animate(s, false);
                }
                else
                {
                    // Up the beach toward the court, at a walk, from the landing.
                    float p = Mathf.Clamp01((hop - HopTime - 0.25f) / (LolaAt_ - HopAt(s) - HopTime - 0.25f));
                    Stride(s, _sand[s], _upBeach[s], p);
                }
                Act(s, Acting(s, age, hop));
            }
            for (int s = 0; s < Seats; s++)
            {
                if (_puff[s] == null || Body(s) == null) continue;
                float since = age - HopAt(s) - HopTime;
                float a = Still ? 0.0f : 0.7f * Hit(since, 0.03f, 3.0f);
                Billboard(_puff[s], _sand[s] + Vector3.up * (0.15f + Mathf.Max(0.0f, since) * 0.4f), new Color(0.93f, 0.82f, 0.62f, a), 0.6f + Mathf.Max(0.0f, since) * 1.4f);
                if (Crossed(HopAt(s) + HopTime)) Cue("sfx_film_sitangkai_hop", 0.6f, 0.92f + 0.06f * _place[s]);
            }
        }

        /// <summary>A walk over the sloping sand, kept on it.</summary>
        private void Stride(int seat, Vector3 from, Vector3 to, float p)
        {
            Vector3 at = Vector3.Lerp(from, to, p);
            at.y = FloorNear(at, Mathf.Lerp(from.y, to.y, p));
            Vector3 way = to - from; way.y = 0.0f;
            float yaw = way.sqrMagnitude > 0.01f ? Quaternion.LookRotation(way).eulerAngles.y : MarkYaw(seat);
            bool walking = p > 0.0f && p < 1.0f;
            Place(seat, at, yaw, walking ? 4.0f : 0.0f);
            Animate(seat, walking);
            Ground(seat, at.y);
        }

        /// <summary>
        /// THE ARRIVAL, ACTED (owner, 2026-10-23: emotion and readability are a top bar). Aboard, each their own
        /// excitement: the bow points at the court and bounces; the next waves big at the kids; the next cheers, arms
        /// up; the stern shades their eyes against the sun, then pumps a fist. Grounding: everyone lurches. The hop:
        /// arms up and out, a crouch on landing. Up the beach: waving back at the kids, a skip, a stretch. On the marks:
        /// every eye on the lola, leaning in, hands together; then her point (see `Reveal`).
        /// </summary>
        private Feel Acting(int s, float age, float hop)
        {
            int k = _place[s];
            Vector3 court = Centre + Vector3.up * 1.0f;
            Feel feel;
            if (!Still && age < LolaAt_)
            {
                if (hop < 0.0f)
                {
                    Vector3 f = Facing(s), si = Side(s);
                    if (k == 0)
                    {
                        float bounce = Mathf.Abs(Mathf.Sin(age * 6.0f));
                        feel = new Feel { Lean = 14.0f, Nod = -8.0f, Right = (court - Chest(s)).normalized + Vector3.up * 0.25f, Left = Vector3.down - f * 0.2f - si * 0.3f, Arms = 1.0f, Hop = 0.06f * bounce };
                    }
                    else if (k == 1)
                    {
                        float wave = Mathf.Sin(age * 9.0f);
                        Vector3 kids = _kidsMid + Vector3.up * 1.0f;
                        feel = new Feel { Lean = -6.0f, Nod = -10.0f, Turn = LookTurn(s, kids), Rock = 8.0f * wave, Right = Vector3.up + (kids - Chest(s)).normalized * 0.5f + si * 0.5f * wave, Left = Vector3.down + si * -0.2f, Arms = 1.0f };
                    }
                    else if (k == 2) feel = Big(Cheer(s, age, 0.5f), 1.2f);
                    else
                    {
                        // Shading the eyes against the low sun, looking for the court; then a fist pump.
                        var shade = new Feel { Lean = 6.0f, Nod = -6.0f, Turn = LookTurn(s, court), Right = f * 0.4f + Vector3.up * 0.9f - si * 0.6f, Left = Vector3.down - si * 0.2f, Arms = 1.0f };
                        feel = Feel.Lerp(shade, Big(FistPump(s, age), 1.2f), Span(age, 4.2f, 4.5f));
                    }
                    // The grounding: thrown forward, arms out to catch themselves.
                    float jolt = Hit(age - Grounds, 0.05f, 4.0f);
                    if (jolt > 0.0f)
                    {
                        Vector3 f2 = Facing(s), s2 = Side(s);
                        var lurch = new Feel { Lean = 34.0f, Nod = 12.0f, Left = f2 + Vector3.up * 0.2f - s2 * 0.8f, Right = f2 + Vector3.up * 0.2f + s2 * 0.8f, Arms = 1.0f };
                        feel = Feel.Lerp(feel, lurch, jolt);
                    }
                    return feel;
                }
                if (hop < HopTime + 0.35f)
                {
                    // The jump: arms flung up and out; a crouch on landing.
                    Vector3 si = Side(s);
                    var air = new Feel { Lean = -10.0f, Nod = -14.0f, Left = Vector3.up * 0.8f - si, Right = Vector3.up * 0.8f + si, Arms = 1.0f };
                    var land = new Feel { Lean = 30.0f, Nod = 8.0f, Left = Facing(s) * 0.6f + Vector3.down - si * 0.4f, Right = Facing(s) * 0.6f + Vector3.down + si * 0.4f, Arms = 0.9f };
                    return Feel.Lerp(air, land, Hit(hop - HopTime, 0.04f, 5.0f) * 1.1f + (hop > HopTime ? 0.0f : 0.0f));
                }
                // Up the beach: each their own way, all happy.
                float t = age;
                Vector3 kidsAt = _kidsMid + Vector3.up * 1.0f;
                Vector3 fa = Facing(s), sa = Side(s);
                if (k == 0) return new Feel { Lean = -4.0f, Nod = -10.0f, Turn = LookTurn(s, kidsAt), Right = Vector3.up + sa * 0.45f * Mathf.Sin(t * 10.0f), Left = Vector3.down - sa * 0.2f, Arms = 1.0f };
                if (k == 1) return Big(FistPump(s, t, 0.42f), 1.15f);
                if (k == 2) { float skip = Mathf.Abs(Mathf.Sin(t * 7.0f)); return new Feel { Lean = -8.0f, Nod = -12.0f, Rock = 10.0f * Mathf.Sin(t * 7.0f), Left = Vector3.up * 0.3f - sa, Right = Vector3.up * 0.3f + sa, Arms = 0.9f, Hop = 0.12f * skip }; }
                return new Feel { Lean = -12.0f, Nod = -20.0f, Left = Vector3.up * 0.9f - sa * 0.7f, Right = Vector3.up * 0.9f + sa * 0.7f, Arms = 0.95f };
            }
            return Reveal(s, age);
        }

        /// <summary>
        /// On the marks, the lola's moment. Before her point: everyone leaning toward her, hands clasped, the three
        /// rocking on their toes. Her point: the taya struck, thrown back, then sagging, head hung; the three explode
        /// into a cheer and then laugh at the taya (one points, one doubles over, one pumps a fist). After the cut to
        /// the taya: they shake it off and square up at the three.
        /// </summary>
        private Feel Reveal(int s, float age)
        {
            Vector3 lola = (_lola != null ? _lola.Root.transform.position : _lolaSpot) + Vector3.up * 0.8f;
            float since = age - _pointAt;
            Vector3 f = Facing(s), si = Side(s);
            if (since < 0.0f)
            {
                // Waiting on her: leaning in, hands clasped at the chest, a nervous rock.
                float rock = Mathf.Sin(age * (4.0f + s));
                return new Feel { Lean = 12.0f, Nod = 4.0f, Turn = LookTurn(s, lola), Rock = 4.0f * rock, Left = f + Vector3.up * 0.3f + si * 0.15f, Right = f + Vector3.up * 0.3f - si * 0.15f, Arms = 0.9f, Hop = 0.03f * Mathf.Max(0.0f, rock) };
            }
            Vector3 tayaChest = Chest(Taya);
            if (s == Taya)
            {
                if (age < _tayaCut)
                {
                    var shock = Big(Shock(s), 1.4f);
                    shock.Hop = 0.18f * Hit(since, 0.07f, 6.0f);
                    if (since < 0.3f) return Feel.Lerp(new Feel(), shock, Smooth(since / 0.08f));
                    var slump = Big(Slump(s, Mathf.Sin((since - 0.3f) * 2.4f)), 1.3f);
                    return Feel.Lerp(shock, slump, Smooth((since - 0.3f) / 0.45f));
                }
                float t = age - _tayaCut;
                var off = Feel.Lerp(Slump(s, 0.0f), HeadShake(s, t), Smooth(t / 0.2f));
                return Feel.Lerp(off, Defiant(s, Centre + new Vector3(0.0f, 1.3f, 9.0f)), Smooth((t - 0.75f) / 0.25f));
            }
            // Each attacker their own laugh, by their order among the three (film r2: two of three read the same and
            // the middle one read flat): a pointing laugh, doubled over, and head thrown back with both fists up.
            int kind = 0;
            for (int o = 0; o < Seats; o++) if (o != Taya && o != s && Body(o) != null && _place[o] < _place[s]) kind++;
            Feel own;
            if (kind == 0) own = Big(LaughAt(s, tayaChest, since, true), 1.5f);
            else if (kind == 1) own = Big(DoubledOver(s, since), 1.4f);
            else
            {
                float ha = Mathf.Abs(Mathf.Sin(since * 10.0f));
                own = new Feel { Lean = -22.0f - 10.0f * ha, Nod = -30.0f - 8.0f * ha, Rock = 9.0f * Mathf.Sin(since * 5.0f), Left = Vector3.up - si * 0.55f, Right = Vector3.up + si * 0.55f, Arms = 1.0f, Hop = 0.1f * ha };
            }
            var cheer = Big(Cheer(s, since, 0.4f + 0.04f * s), 1.3f);
            return Feel.Lerp(cheer, own, Smooth((since - 0.45f - 0.1f * kind) / 0.25f));
        }

        private static Feel Big(Feel f, float k)
        {
            f.Lean *= k; f.Twist *= k; f.Rock *= k; f.Nod *= k; f.Tilt *= k; f.Hop *= k;
            return f;
        }

        // ---- the kids on the boardwalk.

        private void Kids(float age)
        {
            Vector3 boat = BoatAt(BoatT(age)) + Vector3.up;
            Vector3 beach = Still ? _landing : (age < Grounds ? boat : Vector3.Lerp(_landing, _upBeach[0], Span(age, Grounds, LolaAt_)) + Vector3.up);
            bool shown = !Still && age < LolaAt_ + 0.1f || (age >= _pointAt && age < _tayaCut + 0.1f);
            for (int i = 0; i < _kids.Length; i++)
            {
                var kid = _kids[i];
                if (kid == null) continue;
                kid.Show(shown);
                if (!shown) continue;
                Vector3 look = beach - _kidAt[i]; look.y = 0.0f;
                float yaw = look.sqrMagnitude > 0.01f ? Quaternion.LookRotation(look).eulerAngles.y : 90.0f;
                kid.Pose(_kidAt[i], yaw);
                float t = age + i * 0.37f;
                kid.Play(i == 1 ? "emote-yes" : "idle", i == 1 ? Mathf.Repeat(t, 1.2f) : t);
                Vector3 si = Side(kid), f = Facing(kid);
                Feel feel;
                if (i == 0)
                {
                    // Jumping, both arms up, waving.
                    float phase = Mathf.Repeat(t, 0.46f) / 0.46f, wave = Mathf.Sin(t * 12.0f) * 0.3f;
                    feel = new Feel { Lean = -14.0f, Nod = -20.0f, Left = Vector3.up * 0.75f - si * (0.95f + wave), Right = Vector3.up * 0.75f + si * (0.95f - wave), Arms = 1.0f, Hop = Mathf.Sin(phase * Mathf.PI) * 0.4f };
                }
                else if (i == 1)
                {
                    // One arm waving high and wide, the other fist up, bouncing (film r2: a point at the beach was at
                    // the lens and read as nothing).
                    float bounce = Mathf.Abs(Mathf.Sin(t * 8.0f)), wave = Mathf.Sin(t * 11.0f);
                    feel = new Feel { Lean = -8.0f, Nod = -16.0f, Tilt = 12.0f * wave, Right = Vector3.up * 0.8f + si * (0.6f + 0.6f * wave), Left = Vector3.up * 0.5f - si * 1.0f, Arms = 1.0f, Hop = 0.22f * bounce };
                }
                else
                {
                    // Both arms waving big over the head, side to side.
                    float sway = Mathf.Sin(t * 6.5f);
                    feel = new Feel { Lean = -6.0f, Nod = -14.0f, Rock = 12.0f * sway, Left = Vector3.up * 0.8f + si * (-0.75f + 0.6f * sway), Right = Vector3.up * 0.8f + si * (0.75f + 0.6f * sway), Arms = 1.0f, Hop = 0.12f * Mathf.Abs(sway) };
                }
                Act(kid, feel);
            }
            if (Crossed(Still ? 99.0f : KidsAt + 0.05f)) Cue("sfx_film_sitangkai_kids", 0.75f);
            if (Crossed(_pointAt + 0.55f)) Cue("sfx_film_sitangkai_giggle", 0.6f);
        }

        // ---- the lola and the can.

        private void Lola(float age)
        {
            if (_lola == null) return;
            bool shown = Still || age >= _lolaAt - 0.6f;
            _lola.Show(shown);
            float walk = Still ? 1.0f : Mathf.Clamp01((age - (_lolaAt - 0.6f)) / (_setDown - 0.45f - (_lolaAt - 0.6f)));
            Vector3 taya = Chest(Taya);
            Vector3 toTaya = taya - _lolaSpot; toTaya.y = 0.0f;
            float faceTaya = Quaternion.LookRotation(toTaya).eulerAngles.y;
            float faceCan = Quaternion.LookRotation(new Vector3(Centre.x - _lolaSpot.x, 0.0f, Centre.z - _lolaSpot.z)).eulerAngles.y;
            float faceThree = Quaternion.LookRotation(Vector3.forward).eulerAngles.y;
            Vector3 at; float yaw;
            if (walk < 1.0f)
            {
                at = Vector3.Lerp(_lolaFrom, _lolaSpot, Smooth(walk) * 0.15f + walk * 0.85f);
                at.y = Floor(at, Centre.y);
                Vector3 way = _lolaSpot - _lolaFrom; way.y = 0.0f;
                yaw = Mathf.LerpAngle(Quaternion.LookRotation(way).eulerAngles.y, faceCan, Smooth((walk - 0.8f) / 0.2f));
                _lola.Pose(at, yaw);
                _lola.Play("walk", age * 0.9f);
            }
            else
            {
                at = _lolaSpot;
                // Looking the four over (the three, then round), then turning to point at the taya.
                float look = age - _lookAt;
                yaw = Still ? Mathf.LerpAngle(faceThree, faceTaya, Span(age, _pointAt - 0.5f, _pointAt - 0.1f))
                    : age < _lookAt ? faceCan
                    : Mathf.LerpAngle(Mathf.LerpAngle(faceCan, faceThree + 20.0f * Mathf.Sin(look * 2.4f), Span(look, 0.0f, 0.35f)), faceTaya, Span(age, _pointAt - 0.35f, _pointAt - 0.05f));
                _lola.Pose(at, yaw);
                _lola.Play("idle", age);
            }
            Vector3 f = Facing(_lola), si = Side(_lola);
            Vector3 chest = _lola.Root.transform.position + Vector3.up * 0.75f;
            Feel feel;
            float since = age - _pointAt;
            if (!Still && age < _setDown + 0.25f)
            {
                // Carrying the can in both hands, then bending to set it down.
                float bend = Span(age, _setDown - 0.45f, _setDown) * (1.0f - Span(age, _setDown + 0.05f, _setDown + 0.25f));
                feel = new Feel { Lean = 6.0f + 44.0f * bend, Nod = 10.0f + 10.0f * bend, Left = f * 0.9f + Vector3.down * (0.35f + 0.6f * bend) + si * 0.12f, Right = f * 0.9f + Vector3.down * (0.35f + 0.6f * bend) - si * 0.12f, Arms = 0.95f };
            }
            else if (since < 0.0f)
            {
                // Straightening with a hand on the small of her back, then looking them over, a finger up: "hmm".
                float rub = Span(age, _setDown + 0.05f, _setDown + 0.3f) * (1.0f - Span(age, _lookAt + 0.3f, _lookAt + 0.6f));
                var back = new Feel { Lean = -10.0f, Nod = -4.0f, Right = -f * 0.8f + Vector3.down * 0.5f + si * 0.3f, Left = Vector3.down, Arms = 0.9f };
                var hmm = new Feel { Lean = 4.0f, Nod = 6.0f, Tilt = 10.0f * Mathf.Sin(age * 2.0f), Right = f * 0.3f + Vector3.up * 0.9f + si * 0.2f, Left = f * 0.5f + Vector3.down * 0.3f - si * 0.3f, Arms = 0.9f };
                feel = Feel.Lerp(hmm, back, rub);
            }
            else
            {
                // THE POINT: a wind-up, the arm shot straight at the taya, held; then a cackle at them.
                Vector3 aim = (taya - chest).normalized;
                float shot = Back(Mathf.Clamp01(since / 0.18f), 1.4f);
                var point = new Feel { Lean = 14.0f * shot, Nod = 4.0f, Right = Vector3.Slerp(si * 0.4f + Vector3.up, aim, Mathf.Clamp01(shot)), Left = Vector3.down - f * 0.2f - si * 0.3f, Arms = 1.0f };
                float ha = Mathf.Abs(Mathf.Sin(since * 9.0f));
                var cackle = new Feel { Lean = -14.0f - 10.0f * ha, Nod = -28.0f - 8.0f * ha, Rock = 6.0f * Mathf.Sin(since * 4.5f), Right = aim, Left = f * 0.5f + Vector3.down * 0.8f, Arms = 1.0f, Hop = 0.03f * ha };
                feel = Feel.Lerp(point, cackle, Span(since, 0.9f, 1.2f));
            }
            Act(_lola, feel);

            // The can: in her hands until it is set down, then the real one.
            bool held = !Still && age < _setDown;
            if (_can != null)
            {
                _can.SetActive(held && shown);
                if (held)
                {
                    float bend = Span(age, _setDown - 0.45f, _setDown);
                    Vector3 hands = Vector3.Lerp(_lola.Root.transform.position + Vector3.up * 0.42f + f * 0.36f, new Vector3(Centre.x, Centre.y, Centre.z), bend);
                    _can.transform.SetPositionAndRotation(hands, Quaternion.Euler(0.0f, yaw, 0.0f));
                }
            }
            foreach (var r in _realCan) if (r != null) r.enabled = !held;
            if (Crossed(_setDown)) { Cue("sfx_film_sitangkai_clink", 0.8f); Punch(0.35f); }
            if (Crossed(_pointAt)) { Cue("sfx_film_sitangkai_gong", 0.9f); Punch(0.7f); ChipRoles = true; }
        }

        protected override void Restore()
        {
            foreach (var r in _realCan) if (r != null) r.enabled = true;
        }

        // ------------------------------------------------------------------ the shots

        private void Glide(float age)
        {
            // Low over the reef by the village, into the low sun; gliding south with the banca, turning to follow it.
            float p = Smooth(age / BancaA);
            // (Film r4: gliding to 5 m of the banca put a body across the corner of the frame; this stays 10 m off.)
            Vector3 eye = new Vector3(Mathf.Lerp(_p0.x + 18.0f, _p0.x + 26.0f, p), _water + Mathf.Lerp(0.9f, 2.2f, p), Mathf.Lerp(_p0.z + 8.0f, _p0.z - 2.0f, p));
            Vector3 village = _p0 + new Vector3(-10.0f, 3.0f, 8.0f);
            Vector3 boat = BoatAt(BoatT(age)) + Vector3.up * 1.0f;
            Shoot(eye, Vector3.Lerp(village, boat, Smooth((age - 0.7f) / 2.4f)), Mathf.Lerp(56.0f, 46.0f, p), Mathf.Lerp(-3.0f, 0.0f, p));
        }

        private void BancaShots(float age)
        {
            BoatFrame(age, out var at, out var turn);
            Vector3 fwd = turn * Vector3.forward; fwd.y = 0.0f; fwd.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, fwd);
            if (age < BancaB)
            {
                // Alongside on the water: the four aboard against the sea, the camera riding with them.
                float p = Smooth((age - BancaA) / (BancaB - BancaA));
                Vector3 eye = at + right * Mathf.Lerp(4.4f, 3.8f, p) + fwd * Mathf.Lerp(1.2f, 0.4f, p) + Vector3.up * 1.5f;
                Shoot(ClearEye(at + Vector3.up * 1.2f, eye), at + Vector3.up * (_deck + 0.75f), 44.0f, 1.5f);
            }
            else if (age < BancaC)
            {
                // From the sand ahead of the bow, low: it comes at the lens and runs aground.
                // (Film r4 at 1.2 m sat in the boat's path: the bow and a back filled the frame.)
                Vector3 eye = _landing - Vector3.forward * 4.2f + Vector3.right * 1.8f;
                eye.y = FloorNear(eye, _landing.y) + 0.7f;
                Vector3 aim = at + Vector3.up * (_deck + 0.6f) + fwd * (_boatLength * 0.2f);
                Shoot(eye, aim, 50.0f, -2.0f);
            }
            else
            {
                // Beside the bow on the beach: the hops, one after another, into the sand.
                float p = Smooth((age - BancaC) / (KidsAt - BancaC));
                Vector3 mid = (_sand[0] + _sand[Mathf.Min(Seats - 1, 3)]) * 0.5f;
                Vector3 eye = _landing + new Vector3(Mathf.Lerp(5.6f, 5.0f, p), 0.0f, Mathf.Lerp(-2.2f, -2.6f, p));
                eye.y = FloorNear(eye, _landing.y) + 1.25f;
                Shoot(eye, Vector3.Lerp(at, mid, 0.55f) + Vector3.up * 0.75f, 48.0f);
            }
        }

        private void KidsShot(float age)
        {
            // Off the deck's edge in front of them, at their height: three faces cheering at the beach (at the lens),
            // the stilt house and the village behind them in the low sun.
            float p = Smooth((age - KidsAt) / (WalkAt - KidsAt));
            Vector3 toBeach = _landing - _kidsMid; toBeach.y = 0.0f; toBeach.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, toBeach);
            Vector3 eye = _kidsMid + toBeach * Mathf.Lerp(3.4f, 3.0f, p) + side * 0.9f + Vector3.up * 0.95f;
            Shoot(ClearEye(_kidsMid + Vector3.up * 0.7f, eye), _kidsMid + Vector3.up * 0.65f, 46.0f, -2.0f);
        }

        private void WalkShot(float age)
        {
            // In front of them on the beach, into the sun: walking at the lens, waving back at the kids.
            float p = Smooth((age - WalkAt) / (LolaAt_ - WalkAt));
            Vector3 mid = Vector3.zero; int n = 0;
            for (int s = 0; s < Seats; s++) if (Body(s) != null) { mid += Drawn(s).center; n++; }
            mid = n > 0 ? mid / n : _landing;
            Vector3 eye = mid + new Vector3(0.8f, 0.0f, -Mathf.Lerp(5.6f, 4.8f, p));
            eye.y = FloorNear(eye, mid.y - 0.6f) + 0.85f;
            Shoot(eye, mid + Vector3.up * 0.25f, 50.0f, 1.0f);
        }

        private void LolaShot(float age)
        {
            // Low, ahead of her on her line (film r1 sat behind her and saw only her back): she walks at the lens with
            // the can held out, and bends to set it down just in front of it.
            float p = Smooth((age - LolaAt_) / (LookAt_ - LolaAt_));
            Vector3 way = _lolaSpot - _lolaFrom; way.y = 0.0f; way.Normalize();
            Vector3 eye = _lolaSpot + way * Mathf.Lerp(2.9f, 2.6f, p) + Vector3.Cross(Vector3.up, way) * 0.5f + Vector3.up * Mathf.Lerp(1.0f, 0.95f, p);
            Vector3 lola = _lola != null ? _lola.Root.transform.position : _lolaSpot;
            Vector3 aim = Vector3.Lerp(lola + Vector3.up * 0.75f, Centre + Vector3.up * 0.35f, Span(age, SetDown - 0.6f, SetDown));
            Shoot(eye, aim, 46.0f);
        }

        private void LookShot(float age)
        {
            // From behind the three, over their heads (film r1 and r2 from the south and the east saw her back or her
            // small in the middle): the three's backs leaning in along the bottom, her facing us as she looks them
            // over, the taya behind her waiting.
            float p = Still ? 0.0f : Smooth((age - _lookAt) / (_pointAt - _lookAt));
            // (Film r3 and r4 from behind the three: their heads filled the bottom and she read small.) Now where the
            // three stand toward her, at her eye level: her looking us over, the finger up, the taya waiting behind.
            Vector3 lola = _lolaSpot + Vector3.up * 0.8f;
            Vector3 eye = _lolaSpot + new Vector3(Mathf.Lerp(2.0f, 1.8f, p), Mathf.Lerp(1.2f, 1.15f, p), Mathf.Lerp(3.7f, 3.3f, p));
            Shoot(eye, lola + new Vector3(0.2f, -0.05f, -0.4f), 42.0f);
        }

        /// <summary>Front three-quarter on her from the east: her face and her arm shooting across the frame at the
        /// taya, struck on the left (film r1 over her shoulder: her head covered the taya; r5 behind her right
        /// shoulder: the short arm was hidden behind her head).</summary>
        private void OverHerShoulder(float age, out Vector3 eye, out Vector3 aim)
        {
            Vector3 taya = Chest(Taya);
            Vector3 d = taya - _lolaSpot; d.y = 0.0f; d.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, d);
            float p = Still ? 0.0f : Smooth((age - _pointAt) / (LaughAt_ - _pointAt));
            eye = _lolaSpot - right * Mathf.Lerp(3.0f, 2.7f, p) + d * 0.9f + Vector3.up * 1.25f;
            aim = Vector3.Lerp(_lolaSpot + Vector3.up * 0.75f, taya, 0.45f);
        }

        private void PointShots(float age)
        {
            if (age < LaughAt_)
            {
                Beat = "point";
                OverHerShoulder(age, out var eye, out var aim);
                Shoot(eye, aim, 52.0f, -1.5f);
            }
            else if (age < TayaCut)
            {
                // Three-quarter on the three from the east: the point and the laughs cross the frame toward the taya,
                // sagging beyond them by the cackling lola (film r1's over-the-shoulder put the taya's horn across
                // half the frame; r3's square-on had the point and the doubled-over laugh both at the lens, as blobs).
                Beat = "laugh";
                float p = Smooth((age - LaughAt_) / (TayaCut - LaughAt_));
                // (Film r4 sat in front of the three looking away from them.) From the east, side on to the whole
                // court: the three on the right, the lola in the middle, the taya sagging on the left.
                // (Film r5 at 7 m cut the three and the taya at the frame's edges; r6 at 10 m made them tiny.) The
                // three alone, front three-quarter from the south-east: their points and laughs crossing the frame
                // toward the taya, who is just off it to the left.
                Vector3 eye = Centre + new Vector3(Mathf.Lerp(4.6f, 4.3f, p), Mathf.Lerp(1.5f, 1.45f, p), Mathf.Lerp(3.9f, 4.3f, p));
                Shoot(ClearEye(Centre + Vector3.up, eye), Centre + new Vector3(-0.5f, 0.9f, 8.6f), 50.0f, 1.0f);
            }
            else { Beat = "taya"; TayaShots(age); }
        }

        /// <summary>The taya from the front, to their right: clear of the lola standing north-west of them (film r1:
        /// the framing that scores only the players put her hip across the whole frame).</summary>
        private void TayaFrame(float local, out Vector3 eye, out Vector3 aim, out float fov)
        {
            Vector3 taya = Chest(Taya);
            eye = Mark(Taya) + new Vector3(1.85f, 1.3f, 3.0f);
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
