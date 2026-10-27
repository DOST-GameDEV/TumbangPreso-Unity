using System.Collections.Generic;
using System.IO;
using TumbangPreso.Map;
using UnityEngine;

namespace TumbangPreso.PlayTests.Trailer
{
    /// <summary>
    /// TRAILER, ACTS 1 AND 2, "LOLO'S TSINELAS" (docs/trailer/STORY.md is the brief; every shot serves a beat there).
    /// Totoy is always the taya; Lolo Kanor gives him the tsinelas he threw in the first Pandaigdig; the same throw
    /// passes through 1974, 1996 and today; Totoy throws it. Real gameplay comes from `TrailerGameplay`; these are the
    /// story cinematics. Each shot owns a span of this film's clock; tools/trailer_animatic.py cuts them to the sheet.
    ///
    /// Acting is keyed full-body poses (`TrailerBody`, `Track`) over the cast's clips: anticipation, peak, overshoot,
    /// settle; nobody frozen; crowds staggered. The street is at golden hour, the TV at blue hour (`OwnsLight`).
    ///
    /// THE SHOTS (film seconds) and their beats:
    ///   laugh    C   The kids point and laugh at Totoy; he slumps by the can.
    ///   gift     D   On the bench: Lolo takes worn tsinelas from his bag and hands them over; Totoy looks up.
    ///   sole     D   Macro: the sole in Totoy's hands, PANDAIGDIG 1974 written on it.
    ///   y1974    E   (newsreel) Young Kanor's throw; the can flies; the crowd rushes in and lifts him.
    ///   y1996    F   (VHS) A boy on the same street, the same throw, side on like Totoy's.
    ///   jeep     G   A jeepney painted RAGO 7 with his portrait, a dolly along its side.
    ///   display  G   Kids crowd the signature tsinelas display, pointing up.
    ///   statue   G   The barangay's Gintong Lata monument, a low hero angle.
    ///   crane    G   Up over the barangay: games on every patch of street.
    ///   lineup   H   Totoy holds Lolo's tsinelas, looks back at him, Lolo nods, Totoy turns to the can.
    ///   breath   I   From Totoy's eyes: a slow push on the can, the taya crouched. Silence.
    ///   windup   J   Side on, close: dip, charge, whip; the same pose as 1974 and 1996.
    ///   throw    J   From the can: the tsinelas spins into the lens in slow motion.
    ///   payoff   M   The can clatters toward the lens; the camera pushes in as the kids mob Totoy.
    ///   lolo     M   Lolo on his feet by the bench: a proud nod, then a fist.
    ///   tv       O   Night, the reverse angle: the barangay's faces lit by the TV, Lolo and Totoy front and centre; erupt.
    ///   tvscreen O   Over their shoulders: the Arena on the sari-sari TV, PANDAIGDIG 2026 LIVE.
    /// </summary>
    public sealed class TrailerStreetFilm : MapFilm
    {
        public static readonly (string name, float from, float to)[] Shots =
        {
            ("laugh", 0.0f, 2.4f), ("gift", 2.4f, 7.4f), ("sole", 7.4f, 9.4f), ("y1974", 9.4f, 13.8f), ("y1996", 13.8f, 16.4f),
            ("jeep", 16.4f, 18.4f), ("display", 18.4f, 20.2f), ("statue", 20.2f, 22.0f), ("crane", 22.0f, 24.4f),
            ("lineup", 24.4f, 27.6f), ("breath", 27.6f, 29.0f), ("windup", 29.0f, 31.2f), ("throw", 31.2f, 34.4f),
            ("payoff", 34.4f, 37.6f), ("lolo", 37.6f, 39.4f), ("tv", 39.4f, 45.0f), ("tvscreen", 45.0f, 46.8f),
        };
        public const float Length = 46.8f;

        protected override string Region => "TRAILER";
        protected override Color Accent => new Color(1.0f, 0.55f, 0.26f);
        protected override string Score => null;
        protected override float CloseAt(bool full) => 999.0f;
        protected override bool OwnsLight => true;

        private static readonly Mood Golden = new Mood
        {
            Sky = new Color(0.52f, 0.46f, 0.66f), Equator = new Color(0.78f, 0.54f, 0.44f), Ground = new Color(0.48f, 0.34f, 0.26f),
            Fog = new Color(0.98f, 0.70f, 0.52f), FogStart = 40.0f, FogEnd = 260.0f,
            Zenith = new Color(0.36f, 0.42f, 0.70f), Horizon = new Color(1.0f, 0.68f, 0.44f),
            Sun = new Color(1.0f, 0.66f, 0.38f), SunIntensity = 1.15f, SunElevation = 14.0f,
            Brightness = 1.0f, Saturation = 1.12f, Lamps = 1.3f, KeepYaw = true,
        };

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

        private Actor _totoy, _taya, _kanor, _youngKanor, _tikboy, _bebang, _junjun, _maring;
        private readonly List<Actor> _crowd = new List<Actor>(), _ring = new List<Actor>(), _players = new List<Actor>();
        private GameObject _slipper, _gift, _rack, _tv, _can, _jeep, _statue;
        private Renderer _screen;
        private Light _screenLight;
        private Renderer[] _realCan = new Renderer[0];
        private Vector3 _store, _storeOut, _releaseFrom, _board, _bench, _benchFacing, _jeepAt, _jeepSide, _statueAt, _jeepSize;
        private float _ground;

        private static readonly string[] Kids = { "jun_jun", "tikboy", "bebang", "maring", "inday" };
        private static readonly string[] Adults = { "kuya_boy", "ate_girlie", "aling_nena", "bayan", "lola_pacing" };

        // The throwing line and the kid's mark, shared by every throw so the motif lines up shot to shot.
        private Vector3 Line => On(Centre + new Vector3(-0.2f, 0, 7.5f));

        protected override void Prepare()
        {
            _ground = Floor(Centre, Centre.y);
            FindStore();
            FindBench();

            _totoy = Cast("Totoy", 0.64f, "totoy");
            _taya = Cast("Taya kid", 0.6f, "jun_jun");
            _kanor = Cast("Lolo Kanor", 1.0f, "mang_kanor");
            _youngKanor = Cast("Young Kanor", 0.95f, "kuya_boy");
            _tikboy = Cast("Tikboy", 0.64f, "tikboy");
            _bebang = Cast("Bebang", 0.64f, "bebang");
            _junjun = Cast("Jun-Jun", 0.6f, "jun_jun");
            _maring = Cast("Maring", 0.64f, "maring");
            for (int i = 0; i < 9; i++)
                _crowd.Add(Cast("TV crowd " + i, i % 3 == 0 ? 0.64f : 1.0f, i % 3 == 0 ? Kids[i % Kids.Length] : Adults[i % Adults.Length]));
            for (int i = 0; i < 18; i++)
                _ring.Add(Cast("Crowd " + i, i % 4 == 0 ? 0.64f : 1.0f, i % 4 == 0 ? Kids[i % Kids.Length] : Adults[(i * 3) % Adults.Length]));
            for (int i = 0; i < 12; i++)
                _players.Add(Cast("Street game " + i, i % 3 == 0 ? 1.0f : 0.64f, i % 3 == 0 ? Adults[i % Adults.Length] : Kids[i % Kids.Length]));

            _slipper = SlipperModel("Totoy's tsinelas", 1.0f);
            _gift = BuildGift();
            BuildRack();
            BuildTv();
            BuildJeep();
            BuildStatue();
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

        private void FindBench()
        {
            _bench = Centre + new Vector3(-5.2f, 0, 1.5f);
            foreach (var t in FindObjectsByType<Transform>())
                if (t.name.StartsWith("prop_bench_wood")) { _bench = t.position; break; }
            Vector3 toCourt = Centre - _bench; toCourt.y = 0.0f;
            _benchFacing = toCourt.sqrMagnitude > 0.01f ? toCourt.normalized : Vector3.right;
            _bench.y = Floor(_bench + _benchFacing * 0.6f, _ground);
        }

        private GameObject SlipperModel(string name, float scale, int index = 0)
        {
            var book = RosterBook.Load();
            var art = book != null ? book.SlipperArt(index) : null;
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

        /// <summary>Lolo's tsinelas: the worn pair, PANDAIGDIG '74 and his name written on the sole.</summary>
        private GameObject BuildGift()
        {
            var root = Own(new GameObject("Lolo's tsinelas"));
            root.transform.SetParent(transform, false);
            var shoe = SlipperModel("Lolo's shoe", 1.0f);
            shoe.transform.SetParent(root.transform, false);
            shoe.transform.localPosition = Vector3.zero; shoe.transform.localRotation = Quaternion.identity;
            var bounds = new Bounds(shoe.transform.position, Vector3.zero);
            foreach (var r in shoe.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            // Written on the underside, read when the sole is turned to the lens.
            _soleYear = Label(root.transform, "Sole year", new Vector3(0, bounds.min.y - root.transform.position.y - 0.004f, 0.02f), Quaternion.Euler(-90, 0, 180), "PANDAIGDIG '74", 0.022f, new Color(0.15f, 0.1f, 0.08f));
            _soleName = Label(root.transform, "Sole name", new Vector3(0, bounds.min.y - root.transform.position.y - 0.004f, -0.05f), Quaternion.Euler(-90, 0, 180), "KANOR", 0.03f, new Color(0.6f, 0.08f, 0.08f));
            root.SetActive(false);
            return root;
        }

        private Transform Label(Transform parent, string name, Vector3 local, Quaternion turn, string text, float height, Color colour)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local; go.transform.localRotation = turn;
            var mesh = go.AddComponent<TextMesh>();
            var font = Resources.Load<Font>("UI/fonts/PaalalabasDisplayWide");
            if (font != null) { mesh.font = font; go.GetComponent<MeshRenderer>().sharedMaterial = font.material; }
            mesh.text = text; mesh.anchor = TextAnchor.MiddleCenter; mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 120; mesh.characterSize = height / 12.0f * 1.4f; mesh.color = colour;
            return go.transform;
        }

        private Transform _soleYear, _soleName;

        private Renderer Picture(Transform parent, string name, Vector3 at, Quaternion turn, Vector2 size, string resource)
        {
            var tex = Resources.Load<Texture2D>(resource);
            var glow = Glow(name, at, size, Color.white, null, tex != null ? tex : Texture2D.whiteTexture);
            glow.transform.SetParent(parent, true);
            glow.transform.rotation = turn;
            return glow;
        }

        /// <summary>The signature display outside the sari-sari: a board, a painted header, eight pairs under name cards.</summary>
        private void BuildRack()
        {
            _rack = Own(new GameObject("Signature tsinelas display"));
            _rack.transform.SetParent(transform, false);
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            Quaternion face = Quaternion.LookRotation(-_storeOut);
            _board = _store + _storeOut * 1.7f + Vector3.up * 1.3f;
            Part(_rack, Prop(PrimitiveType.Cube, "Board", _board, face, new Vector3(2.5f, 1.5f, 0.06f), new Color(0.55f, 0.36f, 0.22f)));
            Part(_rack, Prop(PrimitiveType.Cube, "Leg L", On(_board - side * 1.1f) + Vector3.up * 0.3f, face, new Vector3(0.08f, 0.6f, 0.08f), new Color(0.4f, 0.26f, 0.16f)));
            Part(_rack, Prop(PrimitiveType.Cube, "Leg R", On(_board + side * 1.1f) + Vector3.up * 0.3f, face, new Vector3(0.08f, 0.6f, 0.08f), new Color(0.4f, 0.26f, 0.16f)));
            Vector3 header = _board + Vector3.up * 0.98f;
            Part(_rack, Prop(PrimitiveType.Cube, "Header", header, face, new Vector3(2.6f, 0.42f, 0.07f), new Color(0.78f, 0.12f, 0.16f)));
            Label(_rack.transform, "Header text", _rack.transform.InverseTransformPoint(header + _storeOut * 0.045f), face, "PANDAIGDIG 2026 EDITION", 0.16f, new Color(1.0f, 0.86f, 0.4f));
            string[] names = { "RAGO 7", "DANTE 1", "NEMU 4", "PHAISTER 9", "AMIHAN 3", "PAETE 8", "ZACK 5", "YASMIN 2" };
            Color[] cards = { new Color(0.95f, 0.45f, 0.2f), new Color(0.55f, 0.5f, 0.45f), new Color(0.45f, 0.35f, 0.7f), new Color(0.75f, 0.3f, 0.6f),
                              new Color(0.35f, 0.7f, 0.75f), new Color(0.4f, 0.7f, 0.3f), new Color(0.95f, 0.8f, 0.2f), new Color(0.6f, 0.85f, 0.95f) };
            int kinds = Mathf.Max(1, Core.Roster.Slippers.Count);
            for (int i = 0; i < names.Length; i++)
            {
                int row = i / 4, col = i % 4;
                Vector3 peg = _board + side * (-0.9f + col * 0.6f) + Vector3.up * (0.42f - row * 0.72f) + _storeOut * 0.05f;
                Part(_rack, Prop(PrimitiveType.Cube, "Card " + names[i], peg, face, new Vector3(0.5f, 0.14f, 0.02f), cards[i]));
                Label(_rack.transform, "Name " + names[i], _rack.transform.InverseTransformPoint(peg + _storeOut * 0.014f), face, names[i], 0.075f, new Color(0.08f, 0.04f, 0.04f));
                for (int k = 0; k < 2; k++)
                {
                    var shoe = SlipperModel(names[i] + " " + k, 0.75f, i % kinds);
                    shoe.transform.SetParent(_rack.transform, true);
                    shoe.transform.position = peg + side * (k == 0 ? -0.11f : 0.11f) + Vector3.down * 0.3f + _storeOut * 0.05f;
                    shoe.transform.rotation = Quaternion.LookRotation(Vector3.down, _storeOut) * Quaternion.Euler(0, 0, k == 0 ? 6 : -6);
                }
            }
            _rack.SetActive(false);
        }

        private static void Part(GameObject parent, GameObject g) { if (g != null) g.transform.SetParent(parent.transform, true); }

        private void BuildTv()
        {
            _tv = Own(new GameObject("Sari-sari TV"));
            _tv.transform.SetParent(transform, false);
            Vector3 at = _store + _storeOut * 2.2f;
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            Quaternion facing = Quaternion.LookRotation(_storeOut);
            Part(_tv, Prop(PrimitiveType.Cube, "TV table", at + Vector3.up * 0.4f, facing, new Vector3(1.3f, 0.8f, 0.7f), new Color(0.45f, 0.3f, 0.2f)));
            Part(_tv, Prop(PrimitiveType.Cube, "TV body", at + Vector3.up * 1.3f, facing, new Vector3(1.25f, 0.95f, 0.6f), new Color(0.12f, 0.12f, 0.14f)));
            Part(_tv, Prop(PrimitiveType.Cube, "TV antenna L", at + Vector3.up * 1.95f + side * 0.18f, facing * Quaternion.Euler(0, 0, 25), new Vector3(0.02f, 0.55f, 0.02f), new Color(0.6f, 0.6f, 0.6f)));
            Part(_tv, Prop(PrimitiveType.Cube, "TV antenna R", at + Vector3.up * 1.95f - side * 0.18f, facing * Quaternion.Euler(0, 0, -25), new Vector3(0.02f, 0.55f, 0.02f), new Color(0.6f, 0.6f, 0.6f)));
            // The broadcast: the Arena frame this run already rendered, and the network's own lower third.
            Texture2D feed = null;
            string frame = Path.Combine("Logs", "trailer", "frames", "arena", "frame_0060.png");
            if (File.Exists(frame)) { feed = new Texture2D(2, 2); feed.LoadImage(File.ReadAllBytes(frame)); Own(feed); }
            Vector3 face = at + Vector3.up * 1.3f + _storeOut * 0.305f;
            _screen = Glow("TV screen", face, new Vector2(1.06f, 0.76f), Color.white, _storeOut, feed != null ? (Texture)feed : Texture2D.whiteTexture);
            _screen.transform.SetParent(_tv.transform, true);
            var bar = Glow("TV lower third", face + Vector3.down * 0.28f + _storeOut * 0.004f, new Vector2(0.9f, 0.1f), new Color(0.75f, 0.1f, 0.14f, 1.0f), _storeOut, Texture2D.whiteTexture);
            bar.transform.SetParent(_tv.transform, true);
            _screen.sharedMaterial.renderQueue = 2990; bar.sharedMaterial.renderQueue = 2995;
            var words = Label(_tv.transform, "TV lower third text", _tv.transform.InverseTransformPoint(face + Vector3.down * 0.28f + _storeOut * 0.008f), Quaternion.LookRotation(-_storeOut), "PANDAIGDIG 2026  LIVE", 0.055f, Color.white);
            var wordsRenderer = words.GetComponent<MeshRenderer>();
            wordsRenderer.material.renderQueue = 3005;
            _screenLight = Lamp("TV glow", at + Vector3.up * 1.3f + _storeOut * 0.9f, new Color(0.6f, 0.78f, 1.0f), 8.0f);
            _screenLight.transform.SetParent(_tv.transform, true);
            _tv.SetActive(false);
        }

        /// <summary>A jeepney from the map, drawn only, parked at the court's edge and painted for Rago. The first
        /// candidate whose drawn size is a jeepney's (3 to 8 m long) is used; what was found is logged.</summary>
        private void BuildJeep()
        {
            GameObject source = null;
            foreach (var t in FindObjectsByType<Transform>())
            {
                if (!t.name.StartsWith("jeepney")) continue;
                var rs = t.GetComponentsInChildren<Renderer>(true);
                if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                float length = Mathf.Max(b.size.x, b.size.z);
                Debug.Log($"[TrailerStreetFilm] jeepney candidate {t.name}: {rs.Length} renderers, size {b.size}, centre {b.center}");
                if (length > 3.0f && length < 8.0f) { source = t.gameObject; break; }
            }
            if (source == null) { Debug.Log("[TrailerStreetFilm] no jeepney found"); return; }
            _jeep = Ghost(source, "RAGO 7 jeepney");
            _jeep.transform.SetParent(transform, true);
            // The map keeps this jeepney as a template with its parts switched off: switch every part on.
            foreach (var part in _jeep.GetComponentsInChildren<Transform>(true)) part.gameObject.SetActive(true);
            foreach (var r in _jeep.GetComponentsInChildren<Renderer>(true)) r.enabled = true;
            var rends = _jeep.GetComponentsInChildren<Renderer>(true);
            Bounds Drawn() { var b = rends[0].bounds; foreach (var r in rends) b.Encapsulate(r.bounds); return b; }
            var bounds = Drawn();
            if (bounds.size.x > bounds.size.z) { _jeep.transform.rotation = Quaternion.Euler(0, 90, 0) * _jeep.transform.rotation; bounds = Drawn(); }
            _jeepAt = On(Centre + new Vector3(-4.4f, 0, 3.6f));
            _jeep.transform.position += new Vector3(_jeepAt.x - bounds.center.x, _jeepAt.y - bounds.min.y, _jeepAt.z - bounds.center.z);
            bounds = Drawn();
            Debug.Log($"[TrailerStreetFilm] jeepney parked: {source.name} size {bounds.size} centre {bounds.center}");
            _jeepAt = bounds.center; _jeepSize = bounds.size;
            _jeepSide = Vector3.right;
            Vector3 panel = new Vector3(bounds.max.x + 0.03f, bounds.min.y + bounds.size.y * 0.42f, bounds.center.z);
            Quaternion outward = Quaternion.LookRotation(-_jeepSide);
            var band = Prop(PrimitiveType.Cube, "Livery band", panel, outward, new Vector3(bounds.size.z * 0.8f, bounds.size.y * 0.24f, 0.03f), new Color(0.85f, 0.15f, 0.12f));
            band.transform.SetParent(_jeep.transform, true);
            var name = Label(_jeep.transform, "Livery name", Vector3.zero, Quaternion.identity, "RAGO 7", bounds.size.y * 0.16f, new Color(1.0f, 0.86f, 0.3f));
            name.SetPositionAndRotation(panel + _jeepSide * 0.03f + Vector3.forward * bounds.size.z * 0.12f, Quaternion.LookRotation(-_jeepSide));
            Picture(_jeep.transform, "Livery portrait", panel + _jeepSide * 0.035f - Vector3.forward * bounds.size.z * 0.24f, Quaternion.LookRotation(-_jeepSide), Vector2.one * bounds.size.y * 0.36f, "UI/portraits/sean");
            _jeep.SetActive(false);
        }

        /// <summary>The barangay's Gintong Lata: a gold, dented can (a can's proportions, rolled rims) on a stone plinth.</summary>
        private void BuildStatue()
        {
            _statue = Own(new GameObject("Gintong Lata monument"));
            _statue.transform.SetParent(transform, false);
            _statueAt = On(_store + _storeOut * 4.6f + Vector3.Cross(Vector3.up, _storeOut) * 3.6f);
            Quaternion face = Quaternion.LookRotation(_storeOut);
            Color gold = new Color(1.0f, 0.78f, 0.22f), deep = new Color(0.82f, 0.58f, 0.14f);
            Part(_statue, Prop(PrimitiveType.Cube, "Plinth", _statueAt + Vector3.up * 0.5f, face, new Vector3(0.9f, 1.0f, 0.9f), new Color(0.62f, 0.6f, 0.56f)));
            Part(_statue, Prop(PrimitiveType.Cube, "Plinth cap", _statueAt + Vector3.up * 1.04f, face, new Vector3(1.05f, 0.08f, 1.05f), new Color(0.5f, 0.48f, 0.45f)));
            float baseY = 1.08f, height = 0.62f, radius = 0.25f;
            Part(_statue, Prop(PrimitiveType.Cylinder, "Golden can", _statueAt + Vector3.up * (baseY + height * 0.5f), face, new Vector3(radius * 2, height * 0.5f, radius * 2), gold));
            Part(_statue, Prop(PrimitiveType.Cylinder, "Rim top", _statueAt + Vector3.up * (baseY + height - 0.01f), face, new Vector3(radius * 2.08f, 0.015f, radius * 2.08f), deep));
            Part(_statue, Prop(PrimitiveType.Cylinder, "Rim bottom", _statueAt + Vector3.up * (baseY + 0.01f), face, new Vector3(radius * 2.08f, 0.015f, radius * 2.08f), deep));
            Part(_statue, Prop(PrimitiveType.Cylinder, "Band", _statueAt + Vector3.up * (baseY + height * 0.5f), face, new Vector3(radius * 2.02f, 0.1f, radius * 2.02f), deep));
            Part(_statue, Prop(PrimitiveType.Cube, "Dent", _statueAt + Vector3.up * (baseY + height * 0.62f) + _storeOut * (radius - 0.02f), face * Quaternion.Euler(0, 0, 25), new Vector3(0.12f, 0.1f, 0.05f), deep));
            Vector3 plaque = _statueAt + Vector3.up * 0.55f + _storeOut * 0.46f;
            Part(_statue, Prop(PrimitiveType.Cube, "Plaque", plaque, face, new Vector3(0.74f, 0.34f, 0.02f), new Color(0.35f, 0.26f, 0.12f)));
            Label(_statue.transform, "Plaque title", _statue.transform.InverseTransformPoint(plaque + _storeOut * 0.013f + Vector3.up * 0.07f), Quaternion.LookRotation(-_storeOut), "ANG GINTONG LATA", 0.06f, new Color(1.0f, 0.82f, 0.4f));
            Label(_statue.transform, "Plaque line", _statue.transform.InverseTransformPoint(plaque + _storeOut * 0.013f - Vector3.up * 0.08f), Quaternion.LookRotation(-_storeOut), "PANDAIGDIG 1974  BRGY. 143", 0.035f, new Color(0.95f, 0.85f, 0.6f));
            _statue.SetActive(false);
        }

        // ------------------------------------------------------------------ the frame

        private string _shot = "";

        protected override void Stage(float age)
        {
            for (int s = 0; s < Seats; s++) ShowSeat(s, false);
            string shot = Shots[Shots.Length - 1].name; float local = age - Shots[Shots.Length - 1].from;
            foreach (var s in Shots) if (age >= s.from && age < s.to) { shot = s.name; local = age - s.from; break; }
            Beat = shot;
            if (shot != _shot) { _shot = shot; HideAll(); }
            Tint(shot == "tv" || shot == "tvscreen" ? Evening : Golden, 1.0f);

            switch (shot)
            {
                case "laugh": Laugh(local); break;
                case "gift": Gift(local); break;
                case "sole": Sole(local); break;
                case "y1974": Y1974(local); break;
                case "y1996": Y1996(local); break;
                case "jeep": Jeep(local); break;
                case "display": Display(local); break;
                case "statue": Statue(local); break;
                case "crane": Crane(local); break;
                case "lineup": Lineup(local); break;
                case "breath": Breath(local); break;
                case "windup": Throw(_totoy, local, true); break;
                case "throw": Throw(_totoy, local, false); break;
                case "payoff": Payoff(local); break;
                case "lolo": LoloReacts(local); break;
                case "tv": Tv(local, false); break;
                case "tvscreen": Tv(local + 2.6f, true); break;
            }
        }

        private void HideAll()
        {
            if (_soleName != null) _soleName.gameObject.SetActive(_shot == "sole");
            if (_soleYear != null) _soleYear.gameObject.SetActive(_shot == "sole");
            foreach (var a in new[] { _totoy, _taya, _kanor, _youngKanor, _tikboy, _bebang, _junjun, _maring }) a?.E?.Show(false);
            foreach (var list in new[] { _crowd, _ring, _players }) foreach (var a in list) a?.E?.Show(false);
            foreach (var g in new[] { _slipper, _gift, _rack, _tv, _can, _jeep, _statue }) if (g != null) g.SetActive(false);
        }

        // ---- helpers

        private Vector3 On(Vector3 at) { at.y = Floor(at, _ground); return at; }

        private static float Yaw(Vector3 from, Vector3 to) { Vector3 d = to - from; d.y = 0.0f; return d.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(d).eulerAngles.y : 0.0f; }

        private static void Perform(Actor a, Vector3 at, float yaw, string clip, float time, in Pose pose)
        {
            if (a?.E == null) return;
            a.E.Show(true);
            a.E.Pose(at, yaw);
            a.B?.Reset();
            a.E.Play(clip, time);
            a.B?.Apply(pose, a.E.Root.transform);
        }

        private static Pose Alive(Pose p, float t, float seed, float amount = 1.0f)
        {
            p.Lean += 1.6f * amount * Mathf.Sin(t * 2.1f + seed);
            p.Roll += 2.2f * amount * Mathf.Sin(t * 0.9f + seed * 1.7f);
            p.Nod += 1.4f * amount * Mathf.Sin(t * 1.3f + seed * 0.6f);
            p.Turn += 4.0f * amount * (Mathf.PerlinNoise(t * 0.45f, seed) - 0.5f);
            return p;
        }

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

        private static void Hold(GameObject thing, Vector3 at, Quaternion turn)
        {
            if (thing == null) return;
            thing.SetActive(true);
            thing.transform.SetPositionAndRotation(at, turn);
        }

        private void InHand(Actor a, GameObject thing)
        {
            if (a?.B == null || thing == null) return;
            Vector3 hand = a.B.Hand(out var pointing);
            Hold(thing, hand + pointing * 0.06f, Quaternion.LookRotation(pointing, a.Facing));
        }

        /// <summary>Cheering, with staggered double jumps.</summary>
        private static Pose Celebrate(Pose from, float since, float height, int seed)
        {
            if (since <= 0.0f) return from;
            var pose = Pose.Mix(from, Pose.Cheer, Track.Shape(Ease.Back, Mathf.Clamp01(since / 0.25f)));
            pose.Arm = (pose.Arm + Vector3.forward * 0.3f * Mathf.Sin(since * 11.0f + seed)).normalized;
            pose.Hop = Jump(since, height, out float s1) + Jump(since - 0.78f, height * 0.75f, out float s2);
            pose.Sink = s1 + s2;
            return pose;
        }

        private static Pose LaughAtPose(float t, Vector3 aimLocal)
        {
            float ha = Mathf.Abs(Mathf.Sin(t * 10.5f));
            return new Pose
            {
                Lean = -8 - 12 * ha, Nod = -16 - 10 * ha, Roll = 6 * Mathf.Sin(t * 5.2f), Tilt = 10 * Mathf.Sin(t * 5.2f), Hop = 0.04f * ha,
                Arm = aimLocal, Fore = aimLocal, Off = new Vector3(-0.2f, -0.6f, 0.6f), OffFore = new Vector3(0.7f, -0.2f, 0.6f),
            };
        }

        private static readonly Pose Slumped = new Pose
        {
            Lean = 22, Nod = 30, Roll = 4, Sink = 0.03f,
            Arm = new Vector3(0.1f, -1.0f, 0.12f), Fore = new Vector3(0.05f, -1.0f, 0.15f), Off = new Vector3(-0.1f, -1.0f, 0.12f), OffFore = new Vector3(-0.05f, -1.0f, 0.15f),
        };

        // ---- C · laugh
        private void Laugh(float t)
        {
            HoldCan(On(Centre + new Vector3(0.6f, 0, -0.2f)) + Vector3.up * 0.06f, Quaternion.Euler(0, 210, 90));
            Vector3 boy = On(Centre + new Vector3(-0.5f, 0, -0.6f));
            var slump = new Track().Key(0.0f, Pose.Ready).Key(0.5f, Pose.Ready).Key(1.1f, Slumped, Ease.Smooth).At(t);
            Perform(_totoy, boy, 15.0f, "idle", t, Alive(slump, t, 1.0f, 0.4f));
            Actor[] kids = { _tikboy, _bebang, _junjun };
            Vector3[] spots = { On(Centre + new Vector3(-1.4f, 0, 2.9f)), On(Centre + new Vector3(-0.1f, 0, 3.4f)), On(Centre + new Vector3(1.2f, 0, 2.7f)) };
            for (int i = 0; i < kids.Length; i++)
            {
                float since = t - 0.35f - i * 0.12f;
                var pose = since > 0.0f ? LaughAtPose(since + i, new Vector3(0.1f, 0.15f, 1.0f)) : Pose.Rest;
                if (i == 2 && since > 0.4f) pose = Pose.Mix(pose, new Pose { Lean = 34, Nod = 6, Arm = new Vector3(0.2f, -0.7f, 0.6f), Fore = new Vector3(-0.6f, -0.3f, 0.7f), Off = new Vector3(-0.2f, -0.7f, 0.6f), OffFore = new Vector3(0.6f, -0.3f, 0.7f), Hop = 0.03f * Mathf.Abs(Mathf.Sin(t * 11f)) }, 0.8f);
                Perform(kids[i], spots[i], Yaw(spots[i], boy), "idle", t + i, Alive(pose, t, 2.0f + i));
            }
            float p = Smooth(t / 2.4f);
            Vector3 eye = Centre + new Vector3(Mathf.Lerp(3.4f, 3.1f, p), 1.05f, Mathf.Lerp(-0.4f, -0.1f, p));
            Shoot(eye + Wobble(t, 0.02f, 3.0f), Centre + new Vector3(-0.55f, 0.7f, 1.1f), 44.0f, -1.0f);
        }

        // ---- D · gift
        private static readonly Track LoloGive = new Track()
            .Key(0.0f, new Pose { Lean = 6, Turn = 25, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore })
            .Key(0.9f, new Pose { Lean = 18, Twist = -20, Turn = -10, Nod = 18, Arm = new Vector3(0.6f, -0.75f, -0.2f), Fore = new Vector3(0.3f, -0.9f, 0.1f), Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Smooth)
            .Key(1.6f, new Pose { Lean = 10, Twist = -8, Turn = 10, Nod = 8, Arm = new Vector3(0.35f, -0.4f, 0.6f), Fore = new Vector3(0.1f, 0.1f, 0.95f), Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Smooth)
            .Key(2.3f, new Pose { Lean = 14, Twist = 14, Turn = 30, Nod = 10, Arm = new Vector3(-0.3f, -0.3f, 0.9f), Fore = new Vector3(-0.4f, 0.0f, 0.9f), Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Back)
            .Key(3.2f, new Pose { Lean = 14, Twist = 14, Turn = 30, Nod = 10, Arm = new Vector3(-0.3f, -0.3f, 0.9f), Fore = new Vector3(-0.4f, 0.0f, 0.9f), Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Linear)
            .Key(3.8f, new Pose { Lean = 4, Turn = 28, Nod = 4, Arm = Pose.Down, Fore = Pose.DownFore, Off = new Vector3(-0.6f, -0.2f, 0.7f), OffFore = new Vector3(-0.5f, 0.5f, 0.6f) }, Ease.Smooth)
            .Key(5.0f, new Pose { Lean = 4, Turn = 28, Nod = 4, Arm = Pose.Down, Fore = Pose.DownFore, Off = new Vector3(-0.6f, -0.2f, 0.7f), OffFore = new Vector3(-0.5f, 0.5f, 0.6f) }, Ease.Linear);

        private static readonly Track TotoyTake = new Track()
            .Key(0.0f, new Pose { Lean = 18, Nod = 26, Arm = new Vector3(0.25f, -0.7f, 0.6f), Fore = new Vector3(-0.5f, -0.2f, 0.8f), Off = new Vector3(-0.25f, -0.7f, 0.6f), OffFore = new Vector3(0.5f, -0.2f, 0.8f) })
            .Key(1.7f, new Pose { Lean = 16, Nod = 24, Arm = new Vector3(0.25f, -0.7f, 0.6f), Fore = new Vector3(-0.5f, -0.2f, 0.8f), Off = new Vector3(-0.25f, -0.7f, 0.6f), OffFore = new Vector3(0.5f, -0.2f, 0.8f) }, Ease.Linear)
            .Key(2.3f, new Pose { Lean = 4, Nod = -4, Turn = -32, Arm = new Vector3(0.25f, -0.7f, 0.6f), Fore = new Vector3(-0.5f, -0.2f, 0.8f), Off = new Vector3(-0.25f, -0.7f, 0.6f), OffFore = new Vector3(0.5f, -0.2f, 0.8f) }, Ease.Back)
            .Key(3.1f, new Pose { Lean = 8, Nod = 4, Turn = -26, Arm = new Vector3(0.3f, -0.4f, 0.8f), Fore = new Vector3(-0.1f, 0.2f, 0.95f), Off = new Vector3(-0.25f, -0.7f, 0.6f), OffFore = new Vector3(0.5f, -0.2f, 0.8f) }, Ease.Smooth)
            .Key(3.9f, new Pose { Lean = 14, Nod = 22, Turn = 0, Arm = new Vector3(0.25f, -0.5f, 0.75f), Fore = new Vector3(-0.3f, 0.3f, 0.9f), Off = new Vector3(-0.25f, -0.5f, 0.75f), OffFore = new Vector3(0.3f, 0.3f, 0.9f) }, Ease.Smooth)
            .Key(5.0f, new Pose { Lean = 14, Nod = 22, Turn = 0, Arm = new Vector3(0.25f, -0.5f, 0.75f), Fore = new Vector3(-0.3f, 0.3f, 0.9f), Off = new Vector3(-0.25f, -0.5f, 0.75f), OffFore = new Vector3(0.3f, 0.3f, 0.9f) }, Ease.Linear);

        private Vector3 BenchSide => Vector3.Cross(Vector3.up, _benchFacing);

        private void Gift(float t)
        {
            Vector3 boy = _bench - BenchSide * 0.42f, lolo = _bench + BenchSide * 0.42f;
            float yaw = Yaw(_bench, _bench + _benchFacing);
            var tp = TotoyTake.At(t);
            Perform(_totoy, boy, yaw, "sit", t, Alive(tp, t, 4.0f, 0.4f));
            Perform(_kanor, lolo, yaw, "sit", t, Alive(LoloGive.At(t), t, 5.0f, 0.4f));
            // The can rests on Totoy's knees until he lets it go to take the tsinelas.
            if (_totoy?.B != null && t < 2.9f)
            {
                Vector3 lap = (_totoy.B.Hand(out _) + _totoy.B.OffHand()) * 0.5f;
                HoldCan(lap + Vector3.up * 0.02f, Quaternion.Euler(0, yaw + 200.0f, 0));
            }
            else HoldCan(On(boy - _benchFacing * 0.1f - BenchSide * 0.35f), Quaternion.Euler(0, yaw + 200.0f, 0));
            // The tsinelas: out of Lolo's bag, held out, into Totoy's hands.
            if (t > 1.0f)
            {
                var holder = t < 2.95f ? _kanor : _totoy;
                InHand(holder, _gift);
            }
            float p = Smooth(t / 5.0f);
            Vector3 front = _bench + _benchFacing * Mathf.Lerp(2.9f, 2.3f, p) + Vector3.up * 0.75f + BenchSide * 0.25f;
            Shoot(front + Wobble(t, 0.015f, 6.0f), _bench + Vector3.up * 0.62f + BenchSide * 0.05f, Mathf.Lerp(34.0f, 30.0f, p));
        }

        // ---- D · sole
        private void Sole(float t)
        {
            Vector3 boy = _bench - BenchSide * 0.42f;
            float yaw = Yaw(_bench, _bench + _benchFacing);
            var hold = new Pose { Lean = 22, Nod = 34, Arm = new Vector3(0.25f, -0.45f, 0.8f), Fore = new Vector3(-0.35f, 0.25f, 0.9f), Off = new Vector3(-0.25f, -0.45f, 0.8f), OffFore = new Vector3(0.35f, 0.25f, 0.9f) };
            Perform(_totoy, boy, yaw, "sit", t, Alive(hold, t, 7.0f, 0.3f));
            if (_totoy?.B == null || _gift == null) return;
            Vector3 hands = (_totoy.B.Hand(out _) + _totoy.B.OffHand()) * 0.5f;
            Vector3 at = hands + _benchFacing * 0.08f + Vector3.up * 0.04f;
            Vector3 eye = at + Vector3.up * 0.5f + _benchFacing * 0.3f + BenchSide * 0.06f;
            Vector3 toLens = (eye - at).normalized;
            // He turns it over: the sole rolls up to face the lens.
            float turn = Smooth((t - 0.15f) / 0.8f);
            Quaternion lying = Quaternion.LookRotation(BenchSide, Vector3.up);
            Quaternion shown = Quaternion.FromToRotation(lying * Vector3.down, toLens) * lying;
            Hold(_gift, at, Quaternion.Slerp(lying, shown, turn));
            Vector3 view = (at - eye).normalized;
            Vector3 up = Vector3.ProjectOnPlane(-_benchFacing, view).normalized;
            float shown01 = Smooth((t - 0.6f) / 0.4f);
            foreach (var (label, offset) in new[] { (_soleName, 0.045f), (_soleYear, -0.03f) })
            {
                if (label == null) continue;
                label.gameObject.SetActive(shown01 > 0.01f);
                label.SetPositionAndRotation(at + toLens * 0.03f + up * offset, Quaternion.LookRotation(view, up));
            }
            Shoot(eye + Wobble(t, 0.004f, 8.0f), at, Mathf.Lerp(34.0f, 28.0f, Smooth(t / 2.0f)));
        }

        // ---- the throw everybody makes
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

        /// <summary>One throw, any thrower, from `Line` at the can. `side`: the close side-on angle; else from the can.</summary>
        private void Throw(Actor who, float t, bool side, GameObject thing = null)
        {
            thing = thing != null ? thing : _gift != null ? _gift : _slipper;
            HoldCan(Centre, Quaternion.Euler(0, 180, 0));
            Vector3 start = Line;
            Vector3 at = Vector3.Lerp(start, On(start + Vector3.back * 0.35f), Smooth((t - 0.95f) / 0.4f));
            var pose = ThrowTrack.At(t);
            if (t > 0.8f && t < 1.0f) pose.Arm = (pose.Arm + 0.03f * new Vector3(Mathf.Sin(t * 83f), Mathf.Sin(t * 71f + 1.3f), 0)).normalized;
            Perform(who, at, 180.0f, "idle", t, Alive(pose, t, 2.0f, 0.3f));

            Vector3 eye = Centre + new Vector3(0.2f, 0.55f, -0.6f);
            if (_soleName != null) _soleName.gameObject.SetActive(false);
            if (_soleYear != null) _soleYear.gameObject.SetActive(false);
            if (t < Release) { InHand(who, thing); if (who?.B != null) _releaseFrom = who.B.Hand(out _); }
            else
            {
                float s = Mathf.Clamp01((t - Release) / (3.2f - Release));
                float flight = 1.0f - Mathf.Pow(1.0f - s, 2.6f);
                Vector3 to = eye + (at + Vector3.up * 0.9f - eye).normalized * 0.3f;
                Hold(thing, Vector3.Lerp(_releaseFrom, to, flight) + Vector3.up * Mathf.Sin(flight * Mathf.PI) * 0.45f, Quaternion.Euler(flight * 1080.0f + 40.0f, 25.0f, flight * 220.0f));
            }
            float push = Smooth(t / 1.1f);
            if (side)
            {
                if (thing != null && t >= Release + 0.25f) thing.SetActive(false);
                Vector3 sideEye = start + new Vector3(-2.3f, 1.0f, -1.6f) + Vector3.Lerp(Vector3.zero, new Vector3(0.35f, -0.05f, 0.3f), push);
                Shoot(sideEye + Wobble(t, 0.025f, 5.0f), at + Vector3.up * 0.75f + new Vector3(0.1f, 0, -0.3f), Mathf.Lerp(36.0f, 30.0f, push), 2.0f);
            }
            else Shoot(eye + new Vector3(0, 0, 0.12f * push) + Wobble(t, 0.015f, 4.0f), at + Vector3.up * 0.95f, Mathf.Lerp(22.0f, 26.0f, Smooth((t - Release) / 1.5f)));
            if (t >= Release && t < Release + 0.06f) Punch(0.25f);
        }

        // ---- E · 1974
        private void Y1974(float t)
        {
            const float Lifted = 2.0f;
            for (int i = 0; i < _ring.Count; i++)
            {
                // Two banks along the lane, three deep at the line end, clear of the throw and the lens.
                int bank = i % 2 == 0 ? 1 : -1, k = i / 2;
                Vector3 home = On(Centre + new Vector3(bank * (3.4f + (k % 3) * 0.75f), 0, -1.5f + (k / 3) * 3.4f + (k % 3) * 0.6f));
                Vector3 to = On(Line + (home - Line).normalized * (0.9f + (i % 3) * 0.35f));
                bool rushes = Vector3.Distance(home, Line) < 8.0f;
                float run = rushes ? Smooth((t - Lifted + 0.4f - (i % 4) * 0.08f) / 0.7f) : 0.0f;
                Vector3 at = Vector3.Lerp(home, to, run);
                var pose = new Pose { Lean = 8, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore };
                pose = Celebrate(pose, t - 1.5f - (i % 5) * 0.06f, 0.2f, i);
                bool moving = run > 0.02f && run < 0.98f;
                Perform(_ring[i], at, moving ? Yaw(home, to) : Yaw(at, run > 0.5f ? Line : Line + Vector3.back * 3.5f), moving ? "sprint" : "idle", t + i * 0.21f, Alive(pose, t, i * 0.9f));
            }
            if (t < Lifted) Throw(_youngKanor, t, true, _slipper);
            else
            {
                float up = Track.Shape(Ease.Back, Mathf.Clamp01((t - Lifted) / 0.45f));
                var pose = Pose.Cheer; pose.Hop = 0.6f * up + 0.06f * Mathf.Sin(t * 7.0f);
                Perform(_youngKanor, Line, 180.0f, "idle", t, Alive(pose, t, 3.0f));
                if (_slipper != null) _slipper.SetActive(false);
            }
            if (t > 1.5f)
            {
                float k = t - 1.5f;
                HoldCan(Centre + new Vector3(-0.8f * k, Mathf.Max(0.0f, 2.6f * k - 4.9f * k * k), -1.6f * k), Quaternion.Euler(k * 700, 40, k * 300));
            }
            // Side on for the throw (the motif), then up and back as the crowd floods in.
            float crane = Smooth((t - 1.6f) / 1.8f);
            if (crane > 0.0f)
            {
                Vector3 side = Line + new Vector3(-2.3f, 1.0f, -1.6f) + new Vector3(0.35f, -0.05f, 0.3f);
                Vector3 high = Line + new Vector3(-4.5f, 4.6f, -5.0f);
                Shoot(Vector3.Lerp(side, high, crane) + Wobble(t, 0.05f, 10.0f), Line + Vector3.up * Mathf.Lerp(0.75f, 1.2f, crane), Mathf.Lerp(30.0f, 40.0f, crane));
            }
        }

        // ---- F · 1996
        private void Y1996(float t)
        {
            Throw(_tikboy, t, true, _slipper);
            Vector3 a = On(Line + new Vector3(1.6f, 0, 1.3f)), b = On(Line + new Vector3(2.4f, 0, 0.3f));
            Perform(_bebang, a, Yaw(a, Centre), "idle", t, Alive(Celebrate(Pose.Rest, t - 1.3f, 0.2f, 1), t, 8.0f));
            Perform(_junjun, b, Yaw(b, Centre), "idle", t, Alive(Celebrate(Pose.Rest, t - 1.38f, 0.22f, 2), t, 9.0f));
        }

        // ---- G · the world it built
        private void Jeep(float t)
        {
            if (_jeep == null) return;
            _jeep.SetActive(true);
            // A dolly along the painted side, low, the name and Rago's face sliding past.
            float p = Smooth(t / 2.0f);
            float half = _jeepSize.z * 0.5f;
            Vector3 eye = new Vector3(_jeepAt.x + _jeepSize.x * 0.5f + 2.4f, _jeepAt.y - _jeepSize.y * 0.15f, _jeepAt.z + Mathf.Lerp(half + 0.6f, -0.2f, p));
            Vector3 aim = new Vector3(_jeepAt.x + _jeepSize.x * 0.5f, _jeepAt.y - _jeepSize.y * 0.05f, _jeepAt.z + Mathf.Lerp(half * 0.4f, -half * 0.5f, p));
            Shoot(eye + Wobble(t, 0.02f, 11.0f), aim, 42.0f);
            // Two kids run past the jeep with their tsinelas.
            Vector3 from = On(new Vector3(_jeepAt.x + _jeepSize.x * 0.5f + 0.7f, 0, _jeepAt.z - half - 1.5f)), to = On(new Vector3(_jeepAt.x + _jeepSize.x * 0.5f + 0.7f, 0, _jeepAt.z + half + 2.0f));
            Perform(_bebang, Vector3.Lerp(from, to, t / 2.0f), Yaw(from, to), "sprint", t, Pose.Rest);
            Perform(_junjun, Vector3.Lerp(from, to, (t - 0.35f) / 2.0f) + Vector3.right * 0.5f, Yaw(from, to), "sprint", t + 0.2f, Pose.Rest);
        }

        private void Display(float t)
        {
            _rack.SetActive(true);
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            Actor[] kids = { _totoy, _tikboy, _bebang, _maring };
            for (int i = 0; i < kids.Length; i++)
            {
                Vector3 at = On(_board + _storeOut * (1.6f + (i % 2) * 0.3f) + side * (-1.0f + i * 0.66f));
                float since = t - 0.3f - i * 0.15f;
                var pose = new Pose { Nod = -30, Lean = -6, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore };
                if (since > 0.0f) pose = Pose.Mix(pose, new Pose { Nod = -34, Lean = -10, Hop = 0.05f * Mathf.Abs(Mathf.Sin(since * 8f + i)), Arm = new Vector3(0.2f, 0.85f, 0.5f), Fore = new Vector3(0.1f, 0.9f, 0.4f), Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Track.Shape(Ease.Back, Mathf.Clamp01(since / 0.3f)));
                Perform(kids[i], at, Yaw(at, at - _storeOut), "idle", t + i, Alive(pose, t, 9.0f + i, 0.6f));
            }
            float p = Smooth(t / 1.8f);
            Vector3 eye = _board + _storeOut * Mathf.Lerp(4.2f, 3.6f, p) + Vector3.up * 0.55f + side * 0.2f;
            Shoot(eye + Wobble(t, 0.02f, 12.0f), _board + Vector3.down * 0.15f, Mathf.Lerp(40.0f, 36.0f, p), 1.0f);
        }

        private void Statue(float t)
        {
            _statue.SetActive(true);
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            // Low hero angle, slow orbit: the gold can against the warm sky.
            float p = Smooth(t / 1.8f);
            Vector3 dir = Quaternion.Euler(0, Mathf.Lerp(-22.0f, 6.0f, p), 0) * _storeOut;
            Shoot(_statueAt + dir * 3.4f + Vector3.up * 0.75f + Wobble(t, 0.02f, 13.0f), _statueAt + Vector3.up * 1.25f, 34.0f, -1.5f);

        }

        private void Crane(float t)
        {
            // Four games around the street: a taya by a can and two throwers each, every one mid-throw at its own time.
            Vector3[] courts = { Centre + new Vector3(-3.5f, 0, -3.0f), Centre + new Vector3(3.2f, 0, 2.0f), Centre + new Vector3(-2.8f, 0, 6.5f), Centre + new Vector3(3.0f, 0, 8.5f) };
            for (int g = 0; g < courts.Length; g++)
            {
                Vector3 can = On(courts[g]);
                for (int k = 0; k < 3; k++)
                {
                    int i = g * 3 + k;
                    if (i >= _players.Count) break;
                    if (k == 0)
                    {
                        Vector3 at = On(can + new Vector3(0.5f, 0, -0.5f));
                        Perform(_players[i], at, Yaw(at, can + Vector3.forward * 3.0f), "idle", t, Alive(Pose.Ready, t, i));
                    }
                    else
                    {
                        Vector3 at = On(can + new Vector3(k == 1 ? -0.9f : 0.9f, 0, 3.2f));
                        float cycle = Mathf.Repeat(t + g * 0.7f + k * 0.45f, 2.2f);
                        Perform(_players[i], at, Yaw(at, can), "idle", t, Alive(ThrowTrack.At(cycle), t, i, 0.3f));
                    }
                }
            }
            float p = Smooth(t / 2.4f);
            Shoot(Centre + new Vector3(Mathf.Lerp(-2.0f, -6.0f, p), Mathf.Lerp(3.0f, 15.0f, p), Mathf.Lerp(-6.0f, -11.0f, p)), Centre + new Vector3(0, 0, Mathf.Lerp(1.0f, 2.5f, p)), Mathf.Lerp(48.0f, 56.0f, p));
        }

        // ---- H · lineup
        private void Lineup(float t)
        {
            HoldCan(Centre, Quaternion.Euler(0, 180, 0));
            Vector3 at = Line;
            Vector3 toLolo = _bench - at; toLolo.y = 0;
            float lookBack = Smooth((t - 0.3f) / 0.4f) * (1.0f - Smooth((t - 1.6f) / 0.45f));
            float yaw = Mathf.LerpAngle(180.0f, Yaw(at, _bench), lookBack * 0.75f);
            var pose = new Pose { Lean = 2, Nod = -4 + 10 * (1 - lookBack), Arm = new Vector3(0.45f, -0.5f, 0.5f), Fore = new Vector3(0.2f, 0.2f, 0.95f), Off = Pose.OffDown, OffFore = Pose.OffDownFore };
            if (t > 2.1f) pose = Pose.Mix(pose, new Pose { Lean = 10, Nod = 8, Sink = 0.05f, Twist = 12, Arm = new Vector3(0.45f, -0.5f, 0.5f), Fore = new Vector3(0.2f, 0.2f, 0.95f), Off = new Vector3(-0.3f, 0.0f, 0.95f), OffFore = new Vector3(-0.2f, 0.1f, 0.97f) }, Track.Shape(Ease.Back, Mathf.Clamp01((t - 2.1f) / 0.35f)));
            Perform(_totoy, at, yaw, "idle", t, Alive(pose, t, 15.0f, 0.5f));
            InHand(_totoy, _gift);
            // Lolo on the bench gives one slow nod.
            float yawL = Yaw(_bench, at);
            float nod = Mathf.Sin(Mathf.Clamp01((t - 0.9f) / 0.8f) * Mathf.PI) * 18.0f;
            Perform(_kanor, _bench + BenchSide * 0.42f, yawL, "sit", t, Alive(new Pose { Nod = nod, Lean = 6, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore }, t, 16.0f, 0.4f));
            // Over Totoy's shoulder toward Lolo, then round to his face as he turns to the can.
            float swing = Smooth((t - 1.7f) / 1.2f);
            Vector3 behind = (at - _bench); behind.y = 0; behind.Normalize();
            Vector3 front = Vector3.back;
            float a0 = Mathf.Atan2(behind.x, behind.z) * Mathf.Rad2Deg, a1 = Mathf.Atan2(front.x, front.z) * Mathf.Rad2Deg;
            float angle = Mathf.LerpAngle(a0 + 20.0f, a1 + 15.0f, swing);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 eye = at + dir * Mathf.Lerp(2.5f, 1.7f, swing) + Vector3.up * Mathf.Lerp(1.05f, 0.8f, swing);
            Shoot(eye + Wobble(t, 0.02f, 17.0f), Vector3.Lerp(_bench + Vector3.up * 0.8f, at + Vector3.up * 0.75f, Smooth((t - 1.4f) / 1.0f)), Mathf.Lerp(30.0f, 32.0f, swing));
        }

        // ---- I · breath
        private void Breath(float t)
        {
            HoldCan(Centre, Quaternion.Euler(0, 180, 0));
            Vector3 taya = On(Centre + new Vector3(0.6f, 0, -1.6f));
            Perform(_taya, taya, 0.0f, "idle", t, Alive(Pose.Ready, t, 18.0f, 1.2f));
            float p = Smooth(t / 1.4f);
            Vector3 eye = Line + Vector3.up * Mathf.Lerp(0.95f, 0.9f, p) + Vector3.back * Mathf.Lerp(0.0f, 1.6f, p);
            Shoot(eye + Wobble(t, 0.01f, 19.0f), Centre + Vector3.up * 0.15f, Mathf.Lerp(30.0f, 16.0f, p));
        }

        // ---- M · payoff
        private void Payoff(float t)
        {
            // The can tumbles toward the lens; the camera pushes up the court as the kids mob Totoy.
            float k = t;
            HoldCan(Centre + new Vector3(-0.3f * k, Mathf.Max(0.0f, 1.8f * k - 4.9f * k * k) + 0.05f, -1.4f * Mathf.Min(k, 1.0f)), Quaternion.Euler(Mathf.Min(k, 0.9f) * 700, 30, 90 * Smooth(k / 0.6f)));
            Vector3 boy = Line;
            Perform(_totoy, boy, 180.0f, "idle", t, Alive(Celebrate(Pose.Follow, t - 0.5f, 0.35f, 0), t, 20.0f));
            Actor[] kids = { _tikboy, _bebang, _junjun };
            Vector3[] from = { On(Centre + new Vector3(-3.0f, 0, 3.0f)), On(Centre + new Vector3(2.8f, 0, 3.4f)), On(Centre + new Vector3(1.6f, 0, 10.0f)) };
            for (int i = 0; i < kids.Length; i++)
            {
                Vector3 to = On(boy + Quaternion.Euler(0, 120 * i - 30, 0) * Vector3.forward * 0.9f);
                float run = Smooth((t - 0.4f - i * 0.1f) / 0.9f);
                bool moving = run > 0.02f && run < 0.98f;
                var pose = Celebrate(Pose.Rest, t - 1.3f - i * 0.08f, 0.25f, i);
                Perform(kids[i], Vector3.Lerp(from[i], to, run), moving ? Yaw(from[i], to) : Yaw(to, boy), moving ? "sprint" : "idle", t, Alive(pose, t, 21.0f + i));
            }
            float push = Smooth((t - 0.6f) / 1.8f);
            Vector3 low = Centre + new Vector3(0.3f, 0.35f, -1.9f);
            Vector3 near = Line + new Vector3(0.7f, 1.0f, -3.0f);
            Shoot(Vector3.Lerp(low, near, push) + Wobble(t, 0.03f, 23.0f), Vector3.Lerp(Centre + Vector3.up * 0.2f, boy + Vector3.up * 0.85f, Smooth((t - 0.3f) / 0.7f)), Mathf.Lerp(32.0f, 36.0f, push));
        }

        private static readonly Track LoloProud = new Track()
            .Key(0.0f, new Pose { Lean = 6, Nod = -4, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore })
            .Key(0.35f, new Pose { Lean = 10, Nod = 16, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Smooth)
            .Key(0.7f, new Pose { Lean = 4, Nod = -6, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Smooth)
            .Key(1.05f, new Pose { Lean = -4, Nod = -12, Twist = 8, Arm = new Vector3(0.35f, 0.2f, 0.6f), Fore = new Vector3(0.1f, 0.95f, 0.2f), Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Back)
            .Key(1.8f, new Pose { Lean = -4, Nod = -12, Twist = 8, Arm = new Vector3(0.35f, 0.2f, 0.6f), Fore = new Vector3(0.1f, 0.95f, 0.2f), Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Ease.Linear);

        private void LoloReacts(float t)
        {
            Vector3 stand = On(_bench + _benchFacing * 0.5f + BenchSide * 0.42f);
            var p = LoloProud.At(t);
            if (t > 1.05f) p.Fore = (p.Fore + Vector3.forward * 0.25f * Mathf.Abs(Mathf.Sin((t - 1.05f) * 7.0f))).normalized;
            Perform(_kanor, stand, Yaw(stand, Line), "idle", t, Alive(p, t, 22.0f, 0.5f));
            Vector3 toLine = (Line - stand); toLine.y = 0; toLine.Normalize();
            Vector3 eye = stand + toLine * 2.0f + Vector3.up * 1.0f + Vector3.Cross(Vector3.up, toLine) * 0.4f;
            Shoot(eye + Vector3.Lerp(Vector3.zero, -toLine * 0.25f, Smooth(t / 1.8f)) + Wobble(t, 0.015f, 26.0f), stand + Vector3.up * 1.05f, 32.0f);
        }

        // ---- O · tv
        private const float Erupt = 2.6f;

        private void Tv(float t, bool screen)
        {
            _tv.SetActive(true);
            Vector3 tv = _store + _storeOut * 2.2f;
            Vector3 side = Vector3.Cross(Vector3.up, _storeOut);
            float flicker = 0.9f + 0.1f * Mathf.PerlinNoise(t * 9.0f, 0.3f);
            float burst = t > Erupt ? Hit(t - Erupt, 0.05f, 2.5f) : 0.0f;
            _screen.sharedMaterial.color = Color.Lerp(Color.white * 0.95f, Color.white * 1.3f, burst) * flicker;
            _screenLight.intensity = 3.5f * flicker + 5.0f * burst;

            for (int i = 0; i < _crowd.Count; i++)
            {
                int row = i < 4 ? 0 : 1;
                float across = row == 0 ? (i < 2 ? -2.0f + i * 0.85f : 1.2f + (i - 2) * 0.85f) : (-2.4f + (i - 4) * 1.2f);
                Vector3 at = On(tv + _storeOut * (3.4f + row * 1.1f) + side * across);
                bool sits = row == 0 && i % 2 == 1;
                var pose = (i % 2 == 0) ? Pose.Nervous : new Pose { Lean = 14, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore };
                pose.Lean += 6.0f * Span(t, 1.0f, Erupt);
                float since = t - Erupt - (i % 4) * 0.07f - row * 0.05f;
                pose = sits && since > 0.0f ? Pose.Mix(pose, Pose.Cheer, Track.Shape(Ease.Back, Mathf.Clamp01(since / 0.25f))) : Celebrate(pose, since, 0.25f + 0.08f * (i % 3), i);
                Perform(_crowd[i], at, Yaw(at, tv), sits ? "sit" : "idle", t + i * 0.37f, Alive(pose, t, i * 1.3f));
            }
            // Front and centre: Lolo on a stool, Totoy beside him holding the tsinelas.
            Vector3 lolo = On(tv + _storeOut * 2.45f + side * 0.45f), boy = On(tv + _storeOut * 2.3f - side * 0.45f);
            var lp = new Pose { Lean = 12, Nod = -4, Arm = Pose.Down, Fore = Pose.DownFore, Off = Pose.OffDown, OffFore = Pose.OffDownFore };
            if (t > Erupt) lp = Pose.Mix(lp, new Pose { Lean = -4, Nod = -12, Arm = new Vector3(0.35f, 0.2f, 0.6f), Fore = new Vector3(0.1f, 0.95f, 0.2f), Off = Pose.OffDown, OffFore = Pose.OffDownFore }, Track.Shape(Ease.Back, Mathf.Clamp01((t - Erupt) / 0.3f)));
            Perform(_kanor, lolo, Yaw(lolo, tv), "sit", t, Alive(lp, t, 24.0f, 0.5f));
            var bp = new Pose { Lean = 16, Nod = -6, Arm = new Vector3(0.25f, -0.5f, 0.75f), Fore = new Vector3(-0.3f, 0.3f, 0.9f), Off = new Vector3(-0.25f, -0.5f, 0.75f), OffFore = new Vector3(0.3f, 0.3f, 0.9f) };
            bp = Celebrate(bp, t - Erupt, 0.42f, 7);
            Perform(_totoy, boy, Yaw(boy, tv), "idle", t, Alive(bp, t, 25.0f));
            InHand(_totoy, _gift);

            if (screen)
            {
                // Over their shoulders: what they are watching.
                // Behind Lolo and Totoy, over their heads: the set, its antenna, the room, the Arena on the screen.
                // High enough that jumping heads stay under the screen (the lower third's words draw without depth).
                Vector3 eye = tv + _storeOut * 3.05f + Vector3.up * 2.15f + side * 0.15f;
                Shoot(eye + Vector3.Lerp(Vector3.zero, -_storeOut * 0.45f, Smooth((t - 2.6f) / 1.8f)) + Wobble(t, 0.01f, 27.0f), tv + Vector3.up * 1.15f, 30.0f);
            }
            else
            {
                // The reverse angle: from beside the set, their faces in its light, pushing in on Lolo and Totoy.
                float p = Smooth(t / 3.6f);
                Vector3 eye = tv + _storeOut * Mathf.Lerp(0.35f, 0.75f, p) + Vector3.up * Mathf.Lerp(1.25f, 1.15f, p) + side * 1.0f;
                Shoot(eye + Wobble(t, 0.02f, 12.0f), (lolo + boy) * 0.5f + Vector3.up * 0.9f + _storeOut * 0.5f, Mathf.Lerp(48.0f, 38.0f, p));
            }
            if (t > Erupt && t < Erupt + 0.06f) Punch(0.7f);
        }
    }
}
