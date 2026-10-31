using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// BOULDER IN A MATCH: the rocks the owner approved on the bench and the court (`FxStudyDanteBoulder`, films c3 and
    /// u4, 2026-10-12: "looks good. just make sure its optimized"), put on a real tsinelas.
    ///
    /// It stands beside <see cref="DanteBoulderCoating"/> (the stone inlay that was the whole look before) and is reached
    /// from the same two places: <see cref="TrackWorld"/> from `Slipper.Awake` for the shoe in the world, and
    /// <see cref="MatchOwner"/> from `ViewmodelArms.MatchSkin` for the copy in the first-person hand.
    ///
    /// ⚠️ ONE SWITCH, <see cref="Rocks"/>. On, the rocks are drawn and the inlay is not; off, the inlay is drawn as it
    /// always was and nothing here makes anything.
    ///
    /// ⚠️ THE SHOE SHOWS ONLY IN HIS OWN HAND. The owner: "can you make it so that the slipper is visible only in the
    /// dante fpv?" The copy in the first-person hand wears the approved four rocks, its toe and strap out in front, so
    /// he knows what he is holding. The shoe in the world (what every other player and every spectator sees, and what
    /// flies) wears the WORLD stone: the same four seated a little forward, one more over the toe and one over the
    /// strap, so it is a boulder and no shoe shows. That stone is its own model and its own baked mask
    /// (`tools/build_dante_rocks.py world`) and measures 0.51 m long, inside the shoe's 0.51 m hit capsule.
    ///
    /// ⚠️ LOOK ONLY, AND IT ONLY EVER READS WHAT EVERY PEER ALREADY HAS: the shoe's `Affinity`, its `State` and its
    /// `Holder`, exactly what the inlay reads. It asks the rules nothing and tells them nothing. So it draws the same
    /// on the host, on a client and for someone who joined late, with no message of its own:
    ///   * the shoe BECOMES loaded while it is held        the ground splits under it and the rocks heave up and close on
    ///                                                     it, one at a time, the last lighting the gaps (the cast);
    ///   * it is found already loaded (a late join, a      the rocks are simply there;
    ///     snapshot, a shoe that changed hands)
    ///   * it flies while loaded                           three small puffs of dust are left behind and below it;
    ///   * it STOPS being loaded on a body                 the rocks BURST off it (`DanteBoulderMoment`, the hit of the
    ///                                                     approved effect, stood where the shoe is);
    ///   * it stops being loaded any other way             the rocks are eaten where they are and are gone (the load
    ///                                                     ran out or the round was reset): no burst.
    /// A loaded shoe that is dropped keeps its rocks, as it kept its inlay: the load is the shoe's.
    ///
    /// ⚠️ HOW A HIT IS TOLD FROM AN EXPIRY, WITH NOTHING ADDED TO THE WIRE. All a peer is told is that the load ended
    /// (the host spends it in `Slipper.HostBodyAffinity` and the shoe's state goes round as it always did). So at that
    /// moment this asks two things it can see for itself: was this shoe IN FLIGHT within the last 0.4 s, and is there
    /// a body other than its thrower within 1.2 m of it. Both: it struck somebody, and it bursts there. A load that
    /// runs out in a hand, on the ground, or in the air with nobody near, is an expiry and is only eaten.
    ///
    /// ⚠️ THE ROCKS RIDE THE SHOE'S OWN MESH. They hang under the mesh's transform, turned and sized from the mesh's own
    /// bounds (its long way is the toe, its thin way is up; they were carved round a shoe 0.44 m long), so they fit the
    /// shoe in the world and the differently sized copy in the hand without either being told about the other.
    ///
    /// Recording prepares the hidden rocks before its first pose, so loading a shoe cannot change a recorded skeleton.
    /// One renderer a rock on one shared material; what differs between two stones (how hot, how far eaten) goes
    /// through a property block. Nothing is allocated while it plays. Its objects are the shoe's children and go with
    /// it; `OnDestroy` frees them when this is removed on its own.
    /// </summary>
    public sealed class DanteBoulderStone : MonoBehaviour
    {
        /// <summary>The rocks (true) or the old stone inlay (false). Read every frame by both, so it can be flipped in Play.</summary>
        public static bool Rocks = true;

        // The rocks in the order they land, and when each leaves the ground and arrives, in seconds after the first leaves:
        // the bench effect's own beats (`FxStudyDanteBoulder`: the first leaves at 0.20, the last is home at 0.92).
        private static readonly string[] HandNames = { "jaw", "knuckle-l", "knuckle-r", "crown" };
        private static readonly float[] HandLeave = { 0.00f, 0.08f, 0.16f, 0.24f }, HandArrive = { 0.34f, 0.47f, 0.59f, 0.72f };
        // The world's six: the two that close it land between the sides and the big one, which is still last.
        private static readonly string[] WorldNames = { "jaw", "knuckle-l", "knuckle-r", "toe", "bridge", "crown" };
        private static readonly float[] WorldLeave = { 0.00f, 0.07f, 0.14f, 0.20f, 0.26f, 0.32f }, WorldArrive = { 0.34f, 0.44f, 0.53f, 0.60f, 0.66f, 0.72f };
        private const int Most = 6;
        private const float CarvedRound = 0.44f, SoleHalf = 0.024f, Eaten = 0.50f;
        // The split opens this long before the first rock leaves it (the effect's cast is 0.08 s before its first rock).
        private const float SplitFirst = 0.08f;
        private static readonly int HeatId = Shader.PropertyToID("_Heat"), FlashId = Shader.PropertyToID("_Flash"), GoneId = Shader.PropertyToID("_Gone");
        // His dust, in the cloud's three tones: the same as the effect's own puffs.
        private static readonly Color DustLit = new Color(0.80f, 0.70f, 0.54f), DustShade = new Color(0.62f, 0.51f, 0.38f), DustLine = new Color(0.42f, 0.33f, 0.24f);

        private Slipper _shoe;
        private bool _world;
        private MeshFilter _target;
        private Mesh _fitted;
        private Renderer _source;

        private Transform _anchor;
        private string[] _names = HandNames;
        private float[] _leave = HandLeave, _arrive = HandArrive;
        private readonly Transform[] _rock = new Transform[Most];
        private readonly Renderer[] _drawn = new Renderer[Most];
        private readonly Vector3[] _seat = new Vector3[Most];
        private MaterialPropertyBlock _block;

        private bool _was;             // loaded last frame
        private bool _seen;            // has looked at the shoe at least once (the first look is never a cast)
        private float _since = 100f;   // seconds since the first rock left the ground
        private float _gone = -1f;     // seconds since the load ended; below 0 while it holds
        private float _flewAt = -100f; // when it was last seen in flight (s), and the way it was going then
        private Vector3 _flying = Vector3.forward, _lastAt;
        private int _thrower = -1;
        private float _inFlight;       // how long this flight has lasted, and how many of its puffs are out
        private int _puffed;

        public static void TrackWorld(Slipper shoe)
        {
            DanteBoulderCoating.TrackWorld(shoe);
            var stone = shoe.GetComponent<DanteBoulderStone>();
            if (stone == null) stone = shoe.gameObject.AddComponent<DanteBoulderStone>();
            stone._world = true;
            stone._shoe = shoe;
        }

        public static void PrepareWorldShape(Slipper shoe)
        {
            if(shoe==null)return;
            TrackWorld(shoe);
            var stone=shoe.GetComponent<DanteBoulderStone>();
            if(stone._anchor==null&&stone.Fit())stone.Show(false);
        }

        // Only called on an owned inactive rendering template, never a live shoe.
        public static void PrepareRecordedShape(GameObject root)
        {
            var target=root.GetComponentInChildren<MeshFilter>(true);if(target==null)return;
            var stone=target.gameObject.AddComponent<DanteBoulderStone>();
            stone.enabled=false;stone._world=true;stone._target=target;
            if(stone.Fit())stone.Show(false);
        }

        public static void MatchOwner(MeshFilter target, Slipper shoe)
        {
            DanteBoulderCoating.MatchOwner(target, shoe);
            if (target == null) return;
            var stone = target.GetComponent<DanteBoulderStone>();
            if (stone == null)
            {
                if (shoe == null || shoe.Affinity != SlipperAffinity.Concussed) return;
                stone = target.gameObject.AddComponent<DanteBoulderStone>();
            }
            stone._shoe = shoe;
            stone._target = target;
        }

        private void LateUpdate()
        {
            bool loaded = Rocks && _shoe != null && _shoe.isActiveAndEnabled && _shoe.Affinity == SlipperAffinity.Concussed
                && (_world || _shoe.State == SlipperState.Held);
            float dt = Time.deltaTime;
            if (loaded && !_was)
            {
                // It became loaded in front of us while held: that is the cast. Found loaded on the first look, or
                // loaded in the air or on the ground, it is a shoe that was loaded before we were watching.
                bool cast = _seen && _shoe.State == SlipperState.Held;
                _since = cast ? -SplitFirst : 100f;
                _gone = -1f;
                // The ground under it splits and throws the rocks up: the cast of the approved effect, stood at his feet.
                if (cast && _world && Fit())
                {
                    Vector3 at = _shoe.transform.position;
                    var holder = _shoe.Holder;
                    float ground = holder != null ? holder.transform.position.y : Slipper.GroundY(at);
                    DanteBoulderMoment.Cast(at, holder != null ? holder.transform.forward : _shoe.transform.forward, ground, _anchor.lossyScale.x);
                }
            }
            else if (!loaded && _was)
            {
                _gone = 0f;
                // Spent on a body: the rocks leave the shoe at once and the hit is played where it is.
                if (_world && Rocks && _shoe != null && _anchor != null && Time.time - _flewAt < 0.4f && BodyNear())
                {
                    DanteBoulderMoment.Hit(_shoe.transform.position, _flying, _anchor.lossyScale.x);
                    _gone = Eaten + 1f;
                }
            }
            _was = loaded; _seen = true;
            if (_world && _shoe != null) Flight(loaded, dt);
            // The copy in the hand keeps the world shoe's time: the cast is seen once, by the stone that is always there,
            // and the hand's copy (made only once the shoe is loaded) plays the same moment of it.
            if (!_world && loaded && _shoe.TryGetComponent(out DanteBoulderStone onShoe) && onShoe != this) _since = onShoe._since;

            if (!loaded && (_gone < 0f || _gone > Eaten)) { Show(false); _gone = -1f; return; }
            if (!Fit()) { Show(false); return; }
            if (loaded) { if (_world) _since += dt; } else _gone += dt;
            Pose(loaded);
        }

        // What it notes while it flies, for the hit; and the dust it leaves.
        private void Flight(bool loaded, float dt)
        {
            Vector3 at = _shoe.transform.position;
            if (_shoe.State == SlipperState.InFlight)
            {
                Vector3 way = _shoe.Velocity.sqrMagnitude > 0.01f ? _shoe.Velocity : at - _lastAt;
                if (way.sqrMagnitude > 1e-6f) _flying = way.normalized;
                _flewAt = Time.time; _thrower = _shoe.ThrowerSlot;
                _inFlight += dt;
                // ⚠️ BEHIND AND BELOW IT, AND NOT NEAR THE HAND THAT THREW IT: the first a tenth of a second out, so none
                // is ever in front of the thrower's own eyes. Three, and that is all a flight leaves.
                if (loaded && _puffed < 3 && _inFlight >= 0.10f + 0.09f * _puffed)
                {
                    var puff = CloudPuffField.Puff.Jump(at - _flying * 0.12f + Vector3.down * 0.16f);
                    puff.Scale = 0.36f; puff.Life = 0.36f; puff.Reach = 0.14f; puff.Lift = 0.05f; puff.Turn = 2.1f * _puffed; puff.Share = 0.75f;
                    puff.Lit = DustLit; puff.Shade = DustShade; puff.Line = DustLine;
                    CloudPuffs.Spawn(puff);
                    _puffed++;
                }
            }
            else { _inFlight = 0f; _puffed = 0; }
            _lastAt = at;
        }

        // ⚠️ WHEN ITS SHOE IS SWITCHED OFF IT FORGETS. The hand's copy lives on the first-person shoe, which is switched
        // off whenever the hand is empty; this then stops being run, and whatever it remembered was still there when
        // the hand next held a shoe. The owner, playing: "the rocks in the fpv also only disappear when dante picks up
        // the slippers again". So an empty hand has no rocks at once (they are the hand shoe's children and go with
        // it), and the next shoe starts from nothing: loaded, its rocks are simply there; not loaded, there are none.
        private void OnDisable()
        {
            Show(false);
            _was = false; _seen = false; _gone = -1f;
        }

        private void OnDestroy()
        {
            if (_anchor != null) Destroy(_anchor.gameObject);
        }

        // Somebody other than its thrower within reach of where it is now.
        private bool BodyNear()
        {
            var round = GameServices.Round;
            if (round == null) return false;
            Vector3 at = _shoe.transform.position;
            foreach (var body in round.Players)
            {
                if (body == null || !body.gameObject.activeInHierarchy || body.PlayerSlot == _thrower) continue;
                Vector3 d = body.transform.position + Vector3.up * 0.7f - at;
                if (d.sqrMagnitude < 1.2f * 1.2f) return true;
            }
            return false;
        }

        private void Show(bool on)
        {
            if (_anchor != null && _anchor.gameObject.activeSelf != on) _anchor.gameObject.SetActive(on);
        }

        // Finds the shoe's mesh and seats the rocks on the mesh's own bounds.
        private bool Fit()
        {
            if (_target == null && _world) _target = _shoe.GetComponentInChildren<MeshFilter>(true);
            if (_target == null || _target.sharedMesh == null) return false;
            if (_anchor == null && !Make()) return false;
            if (_anchor.parent != _target.transform) _anchor.SetParent(_target.transform, false);
            if (_fitted != _target.sharedMesh)
            {
                _fitted = _target.sharedMesh;
                _source = _target.GetComponent<Renderer>();
                Bounds box = _fitted.bounds;
                Vector3 size = box.size;
                // The same reading of the mesh the inlay makes: its long way is the toe, its thin way is up.
                Vector3 toe = size.x >= size.y && size.x >= size.z ? Vector3.right : size.y >= size.z ? Vector3.up : Vector3.forward;
                Vector3 up = size.y <= size.x && size.y <= size.z ? Vector3.up : size.x <= size.z ? Vector3.right : Vector3.forward;
                if (Mathf.Abs(Vector3.Dot(toe, up)) > .5f) up = toe == Vector3.up ? Vector3.right : Vector3.up;
                float length = Mathf.Abs(Vector3.Dot(size, toe)), thick = Mathf.Abs(Vector3.Dot(size, up));
                float fit = length / CarvedRound;
                _anchor.localRotation = Quaternion.LookRotation(toe, up);
                _anchor.localScale = Vector3.one * fit;
                // The rocks' own middle is the middle of the sole: the mesh's middle, down to its underside, up half a sole.
                _anchor.localPosition = box.center - up * (thick * 0.5f - SoleHalf * fit);
            }
            // They are seen exactly where the shoe is: on its layer, and not at all where the shoe itself is not drawn
            // (a held shoe is hidden from its own holder's eyes, who sees the copy in his hand instead).
            int layer = _target.gameObject.layer;
            bool seen = _source == null || (_source.enabled && !_source.forceRenderingOff);
            for (int i = 0; i < _names.Length; i++)
            {
                if (_drawn[i] == null) continue;
                if (_drawn[i].gameObject.layer != layer) _drawn[i].gameObject.layer = layer;
                if (_drawn[i].forceRenderingOff == seen) _drawn[i].forceRenderingOff = !seen;
            }
            Show(true);
            return true;
        }

        private bool Make()
        {
            // The world's shoe wears the stone that covers it; the hand's wears the approved four. If the world's stone
            // is not in the build, the world wears the four as well.
            GameObject source = _world ? Resources.Load<GameObject>(FxStudyDanteBoulder.WorldModel) : null;
            bool covers = source != null;
            if (source == null) source = Resources.Load<GameObject>(FxStudyDanteBoulder.Model);
            // The stone is shaded by a light of its own (`FxStudyRock.shader`): in a match, the map's sun.
            if (RenderSettings.sun != null) FxStudyDanteBoulder.KeyLight = -RenderSettings.sun.transform.forward;
            var material = FxStudyDanteBoulder.Stone(true, covers);
            if (source == null || material == null) return false;
            if (covers) { _names = WorldNames; _leave = WorldLeave; _arrive = WorldArrive; }
            var model = Instantiate(source, _target.transform, false);
            model.name = "BoulderStone";
            VfxRenderTag.Attach(model);
            _anchor = model.transform;
            _block = new MaterialPropertyBlock();
            foreach (var part in model.GetComponentsInChildren<Transform>(true))
            {
                if (part == _anchor) continue;
                int at = System.Array.IndexOf(_names, part.name);
                // The rubble is the hit's, which is played apart from the shoe: not drawn here.
                if (at < 0) { part.gameObject.SetActive(false); continue; }
                _rock[at] = part; _seat[at] = part.localPosition;
                var renderer = part.GetComponent<Renderer>();
                if (renderer == null) continue;
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                _drawn[at] = renderer;
            }
            return true;
        }

        private void Pose(bool loaded)
        {
            // Where the rocks come from: straight down from the shoe, at its holder's feet (in the hand's copy, below the
            // view), in the rocks' own space.
            float drop = 0.85f;
            if (_world && _shoe.Holder != null) drop = Mathf.Max(0.3f, _shoe.transform.position.y - _shoe.Holder.transform.position.y);
            Vector3 ground = _anchor.InverseTransformPoint(_anchor.position + Vector3.down * drop);
            float eaten = loaded ? 0f : Mathf.Clamp01(_gone / Eaten);
            int count = _names.Length;
            float lit = _since - _arrive[count - 1];
            for (int i = 0; i < count; i++)
            {
                var rock = _rock[i];
                if (rock == null) continue;
                float u = FxStudyKit.Run(_since, _leave[i], _arrive[i]);
                bool out_ = _since >= _leave[i];
                if (rock.gameObject.activeSelf != out_) rock.gameObject.SetActive(out_);
                if (!out_) continue;
                // A slow heave first (an eighth of the way in a third of the time), then it speeds up all the way in:
                // out of the ground to one side, over a point above, down into its seat.
                float p = u < 0.34f ? 0.13f * FxStudyKit.Ease(u / 0.34f) : 0.13f + 0.87f * FxStudyKit.EaseIn((u - 0.34f) / 0.66f);
                Vector3 from = ground + new Vector3(0.26f * Mathf.Sin(i * 2.4f + 1f), 0f, 0.5f * (i - (count - 1) * 0.5f) / ((count - 1) * 0.5f));
                Vector3 over = Vector3.Lerp(from, _seat[i], 0.45f) + new Vector3(0.36f * Mathf.Cos(i * 1.7f), 0.40f + 0.08f * i, 0.30f * Mathf.Sin(i * 1.9f));
                rock.localPosition = Vector3.Lerp(Vector3.Lerp(from, over, p), Vector3.Lerp(over, _seat[i], p), p);
                rock.localRotation = Quaternion.Euler(140f * (1f - p) * (i % 2 == 0 ? 1f : -1f), 220f * (1f - p), 90f * (1f - p) * Mathf.Sin(i * 1.3f));
                // It lands a little big and is pressed home.
                float landed = FxStudyKit.EaseOut(FxStudyKit.Run(_since, _arrive[i], _arrive[i] + 0.16f));
                rock.localScale = Vector3.one * (Mathf.Lerp(0.60f, 1f, FxStudyKit.Ease(u)) * (u >= 1f ? Mathf.Lerp(1.16f, 1f, landed) : 1f));
                if (_drawn[i] == null) continue;
                // The gaps: dark until the big rock is home, then lit from it outward; a flash, then breathing.
                float mine = lit - 0.035f * (count - 1 - i);
                float heat = !loaded ? Mathf.Clamp01(1f - eaten * 3f)
                           : mine < 0f ? 0f : Mathf.Min(1f, mine / 0.05f) * (0.88f + 0.12f * Mathf.Sin(Time.time * 5.2f + i)) + 1.6f * Mathf.Exp(-6f * mine);
                _block.SetFloat(HeatId, heat);
                _block.SetFloat(FlashId, u >= 1f ? 0.45f * (1f - landed) : 0f);
                _block.SetFloat(GoneId, eaten);
                _drawn[i].SetPropertyBlock(_block);
            }
        }
    }

    /// <summary>
    /// ONE MOMENT OF THE APPROVED BOULDER, PLAYED IN THE WORLD: the cast, or the hit. It stands a copy of the effect the
    /// owner approved (`FxStudyDanteBoulder`, asked for that one part) where it happens, plays it on the game's clock
    /// from that part's own start, and frees it when it is over. The effect is the same code the films were made with,
    /// so what is seen is what was approved:
    ///   * THE CAST: the ground split under the shoe with its three small stones, the dust where each rock lands, the
    ///     crumbs and grit and the blink as the gaps light. (The rocks themselves are on the real shoe.)
    ///   * THE HIT: the rocks thrown back and to the sides, low; the rubble; the small star; the dust down and out.
    /// Played from about two seconds; the copy is then put aside and played again at the next cast or hit
    /// (`FxStudyDanteBoulder.Pool`), so a match makes each part once.
    /// </summary>
    public sealed class DanteBoulderMoment : MonoBehaviour
    {
        private FxStudyDanteBoulder _fx;
        private float _age, _until;

        public static void Cast(Vector3 shoe, Vector3 facing, float ground, float fit)
        {
            var held = new Vector3(0f, Mathf.Max(0.15f, shoe.y - ground), 0f);
            // The effect's cast is at 0.20 s and its split opens a little before; its split has closed by 2.10.
            Play("BoulderCast", new Vector3(shoe.x, ground, shoe.z), facing, held, held + new Vector3(4.2f, 0f, 0f), fit,
                 FxStudyDanteBoulder.PartCast, FxStudyDanteBoulder.CastMoment - 0.08f, 2.15f);
        }

        public static void Hit(Vector3 at, Vector3 flying, float fit)
        {
            float ground = Slipper.GroundY(at);
            var struck = new Vector3(0f, Mathf.Max(0.15f, at.y - ground), 0f);
            Play("BoulderHit", new Vector3(at.x, ground, at.z), flying, struck + new Vector3(-4.2f, 0.05f, 0f), struck, fit,
                 FxStudyDanteBoulder.PartHit, FxStudyDanteBoulder.HitMoment, -1f);
        }

        private static void Play(string name, Vector3 floor, Vector3 along, Vector3 hand, Vector3 target, float fit, int part, float from, float until)
        {
            along.y = 0f;
            if (along.sqrMagnitude < 1e-6f) along = Vector3.forward;
            var go = new GameObject(name) { hideFlags = HideFlags.DontSave };
            // The effect throws along its own +x and stands on its own floor.
            go.transform.SetPositionAndRotation(floor, Quaternion.LookRotation(along.normalized, Vector3.up) * Quaternion.Euler(0f, -90f, 0f));
            VfxRenderTag.Attach(go);
            if (RenderSettings.sun != null) FxStudyDanteBoulder.KeyLight = -RenderSettings.sun.transform.forward;
            var moment = go.AddComponent<DanteBoulderMoment>();
            // A copy that played before is placed again rather than made (`FxStudyDanteBoulder.ForMoment`).
            moment._fx = FxStudyDanteBoulder.ForMoment(go.transform, hand, target, fit, part);
            moment._age = from;
            moment._until = until > 0f ? until : moment._fx.Seconds;
            moment._fx.Sample(moment._age, moment.Eye());
        }

        private Vector3 Eye()
        {
            var view = Camera.main;
            return view != null ? view.transform.position : transform.position + Vector3.up * 1.5f + Vector3.back * 4f;
        }

        private void LateUpdate()
        {
            _age += Time.deltaTime;
            // Over: the copy is put aside for the next cast or hit BEFORE this object goes, so it does not go with it.
            if (_fx == null || _age >= _until) { FxStudyDanteBoulder.Release(_fx); _fx = null; Destroy(gameObject); return; }
            _fx.Sample(_age, Eye());
        }

        // ⚠️ NO LEFTOVERS (owner, 2026-10-19: "random ability vfx are stuck floating in the map"). The moment is DontSave, so
        // one still playing when Play ends outlives it in the editor and stays drawn: a probe on Payo found BoulderCast and
        // BoulderHit chunks, clouds and cracks left standing. End every moment with Play, and sweep any that survived an
        // earlier Play (this editor enters Play without a domain reload) before the first cast. The look is unchanged.
        private void OnApplicationQuit() => Destroy(gameObject);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SweepLeftovers()
        {
            foreach (var old in Resources.FindObjectsOfTypeAll<DanteBoulderMoment>())
                if (old != null) DestroyImmediate(old.gameObject);
        }

        // Gone before it was over (its scene closed): the copy goes with it.
        private void OnDestroy()
        {
            _fx?.Dispose();
            _fx = null;
        }
    }
}
