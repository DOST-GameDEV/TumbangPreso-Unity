using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// § THE RETRIEVAL ARC (owner, 2026-11-07: *"pickup is the highlight of tumbang preso so it has to feel and be
    /// communicated really well and feel well and rewarding"*). `docs/VISION.md` § 0: the tension is the retrieval.
    /// Under the rules the run has four beats, and until now only the grab itself had any presentation:
    ///
    ///  1. APPROACH. The moment your own tsinelas comes into reach it perks up: the drawn shoe hops off the street
    ///     with a soft tick and a tap on the pad. You know you can take it before you look at a prompt.
    ///  2. GRAB. `Carrier.PresentPickup`, on every peer, tiered by danger.
    ///  3. RUN. Holding a tsinelas inside the box with the can up is the only time an attacker can be tagged
    ///     (`CharacterMotor.IsTaggable`). `TumpHudEffects` already flashes and frames that onset.
    ///  4. SAFE. Leaving the box with the tsinelas ends the danger. This is the payoff, and it was a 28 per cent
    ///     whoosh and a puff (`CourtBoundaryPresentation`). Your own escape now lands in your own view: relief in
    ///     the lens, the whoosh in your head, the pad; a close one (the taya within `Balance.ClutchPickupMetres`
    ///     at some point in the run) says ESCAPED! and the Arena's stands go up for it, for anybody's.
    ///
    /// ⚠️ PRESENTATION ONLY AND LOCAL TO EACH PEER. Positions, holding and the box are replicated, so every machine
    /// can see the same arc; nothing here awards, moves or decides. The shoe's hop moves its drawn `Visual` child
    /// and never its transform, which is what reach, landing and the network read.
    /// </summary>
    [DefaultExecutionOrder(210)]
    public sealed class RetrievalMoments : MonoBehaviour
    {
        private const float PerkSeconds = 0.28f, PerkHeight = 0.12f;

        private Slipper _inReach;
        private Slipper _perked;
        private Transform _perkVisual;
        private Vector3 _perkRest;
        private float _perkLeft;

        private readonly CharacterMotor[] _actors = new CharacterMotor[Balance.PlayerCount];
        private readonly bool[] _hunted = new bool[Balance.PlayerCount];
        private readonly float[] _closest = new float[Balance.PlayerCount];
        private readonly int[] _epochs = new int[Balance.PlayerCount];
        private readonly int[] _teleports = new int[Balance.PlayerCount];

        /// <summary>Escapes presented on this peer, for tests.</summary>
        public static int EscapeCount { get; private set; }
        /// <summary>Of those, the close ones.</summary>
        public static int CloseEscapeCount { get; private set; }

        private void OnDisable()
        {
            EndPerk();
            ClearSeats();
        }

        private void Update()
        {
            var round = GameServices.Round;
            if (round == null || !round.RoundActive)
            {
                EndPerk();
                ClearSeats();
                _inReach = null;
                return;
            }

            CharacterMotor local = null;
            foreach (var p in round.Players)
                if (p != null && p.IsLocalViewer) { local = p; break; }

            StepApproach(local);
            StepPerk();
            StepEscapes(round);
        }

        // ------------------------------------------------------------------ 1. approach

        private void StepApproach(CharacterMotor local)
        {
            Slipper target = null;
            if (local != null && !local.IsDefender && !local.HoldingSlipper && local.CanAct())
            {
                float best = float.MaxValue;
                foreach (var s in BotSlipperInventory.All)
                {
                    if (s == null || !s.CanBeGrabbedBy(local)) continue;
                    float d = (s.transform.position - local.transform.position).sqrMagnitude;
                    if (d >= best) continue;
                    best = d;
                    target = s;
                }
            }

            if (target != null && target != _inReach)
            {
                BeginPerk(target);
                GameServices.Audio?.PlayUiVaried("ui_tick", 1.30f, 1.42f, 0.38f);
                InputLayer.Rumble.Pulse(0.0f, 0.14f, 0.04f);
            }

            _inReach = target;
        }

        private void BeginPerk(Slipper shoe)
        {
            EndPerk();
            if (Settings.SettingsStore.Current.ReducedEffects) return;
            var visual = shoe.transform.Find("Visual");
            if (visual == null) return;
            _perked = shoe;
            _perkVisual = visual;
            _perkRest = visual.localPosition;
            _perkLeft = PerkSeconds;
        }

        private void StepPerk()
        {
            if (_perkVisual == null) return;
            if (_perked == null || _perked.State != SlipperState.Loose) { EndPerk(); return; }

            _perkLeft -= Time.deltaTime;
            if (_perkLeft <= 0.0f) { EndPerk(); return; }

            float u = 1.0f - _perkLeft / PerkSeconds;
            // A quick hop with a smaller second bounce, the way a rubber sole lands.
            float h = u < 0.7f ? Mathf.Sin(u / 0.7f * Mathf.PI) : 0.28f * Mathf.Sin((u - 0.7f) / 0.3f * Mathf.PI);
            Vector3 lift = Vector3.up * (h * PerkHeight);
            var parent = _perkVisual.parent;
            _perkVisual.localPosition = _perkRest + (parent != null ? parent.InverseTransformVector(lift) : lift);
        }

        private void EndPerk()
        {
            if (_perkVisual != null) _perkVisual.localPosition = _perkRest;
            _perkVisual = null;
            _perked = null;
            _perkLeft = 0.0f;
        }

        // ------------------------------------------------------------------ 4. safe

        private void ClearSeats()
        {
            for (int i = 0; i < _hunted.Length; i++)
            {
                _actors[i] = null;
                _hunted[i] = false;
                _closest[i] = float.MaxValue;
            }
        }

        private void StepEscapes(RoundDirector round)
        {
            bool canUp = round.Lata != null && round.Lata.IsUpright;
            CharacterMotor taya = null;
            foreach (var p in round.Players)
                if (p != null && p.IsDefender) { taya = p; break; }

            foreach (var p in round.Players)
            {
                if (p == null || p.PlayerSlot < 0 || p.PlayerSlot >= _hunted.Length) continue;
                int seat = p.PlayerSlot;

                // A tag, a respawn or a body swap is a teleport, never an escape.
                bool continuous = _actors[seat] == p && _epochs[seat] == p.MovementEpoch
                                  && _teleports[seat] == p.PresentationTeleportSerial;
                if (!continuous) { _hunted[seat] = false; _closest[seat] = float.MaxValue; }

                bool hunted = canUp && !p.IsDefender && p.HoldingSlipper && p.IsInsideBox() && !p.IsStunned;
                if (hunted)
                {
                    if (!_hunted[seat]) _closest[seat] = float.MaxValue;
                    if (taya != null)
                    {
                        Vector3 d = p.transform.position - taya.transform.position;
                        d.y = 0.0f;
                        _closest[seat] = Mathf.Min(_closest[seat], d.magnitude);
                    }
                }
                else if (_hunted[seat] && continuous && canUp && p.HoldingSlipper && !p.IsInsideBox())
                {
                    Escaped(p, _closest[seat]);
                }

                _hunted[seat] = hunted;
                _actors[seat] = p;
                _epochs[seat] = p.MovementEpoch;
                _teleports[seat] = p.PresentationTeleportSerial;
            }
        }

        private static void Escaped(CharacterMotor who, float closestTaya)
        {
            bool close = closestTaya <= Balance.ClutchPickupMetres;
            EscapeCount++;
            if (close) CloseEscapeCount++;

            // The stands see everybody's run, not only yours.
            if (close && Map.ArenaCrowdAudio.Instance != null) Map.ArenaCrowdAudio.Erupt(0.5f, "close escape");

            var cam = Camera.main;
            var rig = cam != null ? cam.GetComponent<CameraSystem.CameraRig>() : null;
            if (rig == null || !rig.IsFollowing(who) || !who.IsLocalViewer) return;

            // Relief: the lens opens and lifts, the whoosh is in your own head, the pad lets go.
            GameServices.Audio?.PlayUiVaried("court_escape", 1.00f, 1.08f, close ? 0.95f : 0.65f);
            rig.ViewPunch(new Vector3(-1.8f, 0.0f, 0.0f), close ? 6.0f : 3.5f);
            InputLayer.Rumble.Pulse(close ? 0.30f : 0.10f, close ? 0.50f : 0.24f, close ? 0.14f : 0.08f);

            if (!close) return;
            GameServices.Audio?.PlayUiVaried("ui_valid", 1.05f, 1.12f, 0.8f);
            rig.GetComponent<ColourGrade>()?.PulseChromatic(0.16f, 0.2f);
            ComicPopup.Spawn(cam.transform.position + cam.transform.forward * 2.6f + Vector3.up * 0.25f,
                             "ESCAPED!", UI.UiTheme.Defense, 0.85f);
        }
    }
}
