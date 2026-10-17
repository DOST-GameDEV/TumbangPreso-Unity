using System;
using UnityEngine;
using TumbangPreso.Visual;

namespace TumbangPreso.Map
{
    /// <summary>
    /// THE PEOPLE AND ANIMALS OF THE PAYO MAP, from the art's own list (`payo_art.json`, "life": a kind, a place, a
    /// facing and a line saying what each should do). The art's figures are rigid single meshes with no bones (its note:
    /// "sway, bob, slide or swap them as the other maps' ambient life does"), so each KIND is a small rule that moves the
    /// whole figure: a new json re-places them and this file does not change.
    ///
    ///   villagers  stand and shift their weight, sit on the walls, carry a basket across the forecourt and back, bend
    ///              planting rice and step along the row; a child waves. THEY WATCH THE GAME: a throw turns the standing
    ///              ones toward it, a KNOCKED CAN makes them jump (the sitters get up, the children jump longest), a
    ///              tag makes them sag, and a round's end has them all up. At dusk they walk off home and are gone by night.
    ///   carabao    stand chewing or lie in the paddy, breathing; chickens peck, hop and scatter from a knocked can;
    ///              the dog lies by the mortar and trots to the rim when the can falls; egrets stalk and strike (day);
    ///              hawks ride a 60 m ring over the valley, banked (day).
    ///
    /// The reactions are the game's own shared hook (`MatchFlair.Presented`, what the Arena's crowd and the sidewalk's
    /// spectators read) and the match's round end: every peer has them, nothing is sent.
    /// ⚠️ PRESENTATION ONLY: no collider, a private random, nothing gameplay reads. In the menus' map preview they stand still.
    /// </summary>
    [DefaultExecutionOrder(670)]
    public sealed class PayoActors : MonoBehaviour
    {
        public enum Kind { Stand, Watch, Sit, Child, Carry, Plant, CarabaoStand, CarabaoWallow, Chicken, Dog, Egret, Hawk }

        [Serializable] public sealed class Actor { public string Name; public Kind Kind; public Transform Root; public Vector3 Home; public float Yaw; }

        public Actor[] Actors = Array.Empty<Actor>();
        /// <summary>How far a carrier walks before turning back, and the ring a hawk rides, metres.</summary>
        public float CarryRun = 9.0f, HawkRing = 60.0f;

        /// <summary>The kind a json word names, or false for a word this does not know (the builder then warns and leaves it standing).</summary>
        public static bool TryKind(string word, out Kind kind)
        {
            switch ((word ?? "").ToLowerInvariant())
            {
                case "villager_stand": kind = Kind.Stand; return true;
                case "villager_watch": kind = Kind.Watch; return true;
                case "villager_sit": kind = Kind.Sit; return true;
                case "child_wave": kind = Kind.Child; return true;
                case "villager_carry": kind = Kind.Carry; return true;
                case "villager_plant": kind = Kind.Plant; return true;
                case "carabao_stand": kind = Kind.CarabaoStand; return true;
                case "carabao_wallow": kind = Kind.CarabaoWallow; return true;
                case "chicken": case "chicken_white": kind = Kind.Chicken; return true;
                case "dog": kind = Kind.Dog; return true;
                case "egret": kind = Kind.Egret; return true;
                case "hawk": kind = Kind.Hawk; return true;
                default: kind = Kind.Stand; return false;
            }
        }

        private float _clock, _cheerAt = -99.0f, _sagAt = -99.0f, _lookAt = -99.0f;
        private float _cheerFor = 2.4f;
        private Vector3 _lookPoint;
        private MatchDirector _match;

        /// <summary>For a probe: how many are out, and how many seconds ago they last cheered.</summary>
        public string Report()
        {
            int shown = 0;
            foreach (var a in Actors) if (a != null && a.Root != null && a.Root.gameObject.activeSelf) shown++;
            return $"PayoActors: {shown} of {Actors.Length} figures out; last cheer {(_cheerAt < 0.0f ? "never" : (_clock - _cheerAt).ToString("0.0") + " s ago")}";
        }

        private bool IsPreview() => MatchInstaller.PreviewOnly || gameObject.layer == UI.MapPreviewSurface.PreviewLayer;

        private void OnEnable() { MatchFlair.Presented += OnFlair; }

        private void OnDisable()
        {
            MatchFlair.Presented -= OnFlair;
            if (_match != null) { _match.IntermissionStarted -= OnRoundEnd; _match.MatchEnded -= OnMatchEnd; }
            _match = null;
        }

        private void OnFlair(MatchFlair.Kind kind, int actor, int subject, Vector3 at, float strength)
        {
            if (kind == MatchFlair.Kind.LataDown) { _cheerAt = _clock; _cheerFor = 2.6f; _lookAt = _clock; _lookPoint = at; }
            else if (kind == MatchFlair.Kind.Tag) _sagAt = _clock;
            else if (kind == MatchFlair.Kind.Throw || kind == MatchFlair.Kind.NearMiss) { _lookAt = _clock; _lookPoint = at; }
        }

        private void OnRoundEnd(int nextRound, int nextDefender) { _cheerAt = _clock; _cheerFor = 4.0f; }
        private void OnMatchEnd(int winner) { _cheerAt = _clock; _cheerFor = 8.0f; }

        /// <summary>The same reaction a knocked can gives, for a probe.</summary>
        public void React() => OnFlair(MatchFlair.Kind.LataDown, -1, -1, Vector3.zero, 1.0f);

        private static float Hash(int i, int salt)
        {
            unchecked
            {
                uint h = (uint)(i * 374761393 + salt * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0x1000000;
            }
        }

        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3.0f - 2.0f * t); }

        private void LateUpdate()
        {
            if (IsPreview()) return;
            var match = GameServices.Match;
            if (match != _match)
            {
                if (_match != null) { _match.IntermissionStarted -= OnRoundEnd; _match.MatchEnded -= OnMatchEnd; }
                _match = match;
                if (match != null) { match.IntermissionStarted += OnRoundEnd; match.MatchEnded += OnMatchEnd; }
            }
            _clock += PresentationClock.Held ? Mathf.Min(Time.unscaledDeltaTime, 0.05f) : Time.deltaTime;
            float night = PayoNight.Factor;

            for (int i = 0; i < Actors.Length; i++)
            {
                var a = Actors[i];
                if (a == null || a.Root == null) continue;
                float s0 = Hash(i, 1), s1 = Hash(i, 2), s2 = Hash(i, 3);
                bool person = a.Kind <= Kind.Plant;
                bool dayBird = a.Kind == Kind.Egret || a.Kind == Kind.Hawk;

                // Who is out: people go home through the dusk, the day's birds go in, the chickens roost late.
                float leave = person ? Smooth((night - 0.3f - 0.15f * s0) / 0.35f) : 0.0f;
                bool show = person ? leave < 0.999f : dayBird ? night < 0.55f : a.Kind == Kind.Chicken ? night < 0.85f : true;
                if (a.Root.gameObject.activeSelf != show) a.Root.gameObject.SetActive(show);
                if (!show) continue;

                Vector3 at = a.Home; float yaw = a.Yaw, pitch = 0.0f, roll = 0.0f;
                Vector3 forward = Quaternion.Euler(0.0f, a.Yaw, 0.0f) * Vector3.forward, right = Quaternion.Euler(0.0f, a.Yaw, 0.0f) * Vector3.right;
                float t = _clock + s0 * 20.0f;

                // The crowd's moments, each figure a beat late.
                float since = _clock - _cheerAt - s1 * 0.3f, cheer = since >= 0.0f && since < _cheerFor ? 1.0f - since / _cheerFor : 0.0f;
                float sag = Mathf.Clamp01(1.0f - (_clock - _sagAt - s1 * 0.2f) / 1.4f) * (_clock - _sagAt - s1 * 0.2f >= 0.0f ? 1.0f : 0.0f);
                float look = Mathf.Clamp01(1.0f - (_clock - _lookAt) / 3.5f) * Smooth((_clock - _lookAt) / 0.5f);
                float hop = cheer > 0.0f ? Mathf.Abs(Mathf.Sin(since * (5.0f + 2.0f * s2))) * cheer : 0.0f;

                switch (a.Kind)
                {
                    case Kind.Stand:
                    case Kind.Watch:
                        yaw += Mathf.Sin(t * 0.31f) * 7.0f;
                        at.y += Mathf.Sin(t * 1.1f) * 0.012f + hop * 0.2f;
                        roll = Mathf.Sin(t * 0.47f) * 1.6f;
                        pitch = (a.Kind == Kind.Watch ? 4.0f : 0.0f) + sag * 10.0f;
                        if (look > 0.0f)
                        {
                            Vector3 to = _lookPoint - a.Home; to.y = 0.0f;
                            if (to.sqrMagnitude > 0.01f) yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, look);
                        }
                        break;
                    case Kind.Sit:
                        // Up off the wall when the can goes down, bouncing as they clap, then down again.
                        at.y += Mathf.Sin(t * 0.9f) * 0.008f + Smooth(cheer * 3.0f) * 0.32f + hop * 0.08f;
                        yaw += Mathf.Sin(t * 0.23f) * 4.0f;
                        pitch = sag * 8.0f;
                        break;
                    case Kind.Child:
                    {
                        // Waves in bursts; jumps longest of anybody.
                        float wave = Mathf.Sin(t * 0.5f) > 0.2f ? Mathf.Sin(t * 12.0f) * 9.0f : 0.0f;
                        roll = wave;
                        at.y += Mathf.Abs(Mathf.Sin(t * 2.6f)) * 0.04f + hop * 0.36f;
                        yaw += Mathf.Sin(t * 0.7f) * 12.0f;
                        break;
                    }
                    case Kind.Carry:
                    {
                        // Out along the way it faces and back, slowly, turning at each end.
                        float lap = CarryRun * 2.0f / 0.7f, u = Mathf.Repeat(t, lap) / lap;
                        float along = u < 0.5f ? Smooth(u * 2.0f) : 1.0f - Smooth((u - 0.5f) * 2.0f);
                        at += forward * (along * CarryRun);
                        yaw += u < 0.5f ? Smooth((u - 0.46f) / 0.04f) * 180.0f : 180.0f + Smooth((u - 0.96f) / 0.04f) * 180.0f;
                        at.y += Mathf.Abs(Mathf.Sin(t * 5.0f)) * 0.02f;
                        roll = Mathf.Sin(t * 5.0f) * 1.5f;
                        break;
                    }
                    case Kind.Plant:
                    {
                        // Bend, plant, straighten, step on along the row.
                        float cycle = 4.6f + s2, u = Mathf.Repeat(t, cycle) / cycle; int step = Mathf.FloorToInt(t / cycle);
                        pitch = (u < 0.2f ? Smooth(u / 0.2f) : u < 0.6f ? 1.0f : 1.0f - Smooth((u - 0.6f) / 0.2f)) * 40.0f * (1.0f - cheer);
                        float row = Mathf.PingPong(step * 0.45f + Smooth((u - 0.82f) / 0.18f) * 0.45f, 3.0f) - 1.5f;
                        at += right * row;
                        at.y += hop * 0.15f;
                        break;
                    }
                    case Kind.CarabaoStand:
                        yaw += Mathf.Sin(t * 0.21f) * 3.0f;
                        at.y += Mathf.Sin(t * 2.4f) * 0.006f;
                        pitch = Mathf.Sin(t * 0.33f) * 1.5f;
                        break;
                    case Kind.CarabaoWallow:
                        roll = Mathf.Sin(t * 0.4f) * 3.0f;
                        at.y += Mathf.Sin(t * 0.8f) * 0.012f;
                        break;
                    case Kind.Chicken:
                    {
                        // A new spot every few seconds, reached in a hop; two quick pecks between; a dash from a knocked can.
                        float every = 4.0f + 3.0f * s1; int spot = Mathf.FloorToInt(t / every); float u = Mathf.Repeat(t, every) / every;
                        Vector3 Spot(int k) => new Vector3(Hash(i * 31 + k, 5) - 0.5f, 0.0f, Hash(i * 31 + k, 6) - 0.5f) * 1.6f;
                        float move = Smooth(u / 0.18f);
                        Vector3 from = Spot(spot - 1), to = Spot(spot);
                        at += Vector3.Lerp(from, to, move);
                        at.y += Mathf.Sin(move * Mathf.PI) * 0.07f;
                        Vector3 way = to - from;
                        if (way.sqrMagnitude > 0.001f) yaw = Mathf.Atan2(way.x, way.z) * Mathf.Rad2Deg;
                        float peck = u > 0.35f && u < 0.8f ? Mathf.Max(0.0f, Mathf.Sin((u - 0.35f) * 28.0f)) : 0.0f;
                        pitch = peck * 32.0f;
                        if (cheer > 0.0f) { Vector3 away = a.Home; away.y = 0.0f; at += away.normalized * (Mathf.Sin(Mathf.Clamp01(since / 1.2f) * Mathf.PI) * 1.8f); at.y += hop * 0.12f; pitch = 0.0f; }
                        break;
                    }
                    case Kind.Dog:
                    {
                        // Lies breathing; when the can falls it trots toward the pit, barks (three hops) and goes back.
                        at.y += Mathf.Sin(t * 1.6f) * 0.006f;
                        float run = since >= 0.0f && since < 5.0f ? Mathf.Sin(Mathf.Clamp01(since / 5.0f) * Mathf.PI) : 0.0f;
                        if (run > 0.0f)
                        {
                            Vector3 to = -a.Home; to.y = 0.0f;
                            at += to.normalized * (Smooth(run * 1.6f) * 3.0f);
                            yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                            at.y += since > 1.2f && since < 3.4f ? Mathf.Abs(Mathf.Sin(since * 7.0f)) * 0.1f : Mathf.Abs(Mathf.Sin(since * 9.0f)) * 0.03f;
                        }
                        break;
                    }
                    case Kind.Egret:
                    {
                        // Stalks a step at a time, and strikes.
                        float every = 3.0f + 2.0f * s1; int step = Mathf.FloorToInt(t / every); float u = Mathf.Repeat(t, every) / every;
                        float walk = (Mathf.PingPong(step * 0.35f + Smooth(u / 0.3f) * 0.35f, 2.4f) - 1.2f);
                        at += forward * walk;
                        float strike = step % 4 == 3 && u > 0.5f && u < 0.72f ? Mathf.Sin((u - 0.5f) / 0.22f * Mathf.PI) : 0.0f;
                        pitch = strike * 46.0f;
                        at.y += Mathf.Sin(Smooth(u / 0.3f) * Mathf.PI) * 0.03f;
                        break;
                    }
                    case Kind.Hawk:
                    {
                        // A ring over the valley, wings still, banked into the turn, rising and sinking on the air.
                        float turn = t * (8.5f / Mathf.Max(10.0f, HawkRing)) + s1 * 6.28f;
                        Vector3 centre = a.Home - right * HawkRing;
                        at = centre + (Mathf.Cos(turn) * right + Mathf.Sin(turn) * forward) * HawkRing;
                        at.y = a.Home.y + Mathf.Sin(t * 0.17f) * 6.0f;
                        Vector3 ahead = -Mathf.Sin(turn) * right + Mathf.Cos(turn) * forward;
                        yaw = Mathf.Atan2(ahead.x, ahead.z) * Mathf.Rad2Deg;
                        roll = 20.0f + Mathf.Sin(t * 0.5f) * 5.0f;
                        break;
                    }
                }

                if (person && leave > 0.0f)
                {
                    // Home at dusk: away from the pit, turned to go, and gone.
                    Vector3 away = a.Home; away.y = 0.0f;
                    away = away.sqrMagnitude > 0.01f ? away.normalized : Vector3.back;
                    at += away * (leave * 7.0f);
                    yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(away.x, away.z) * Mathf.Rad2Deg, Smooth(leave * 4.0f));
                    at.y += Mathf.Abs(Mathf.Sin(t * 5.0f)) * 0.02f;
                }

                a.Root.SetPositionAndRotation(at, Quaternion.Euler(0.0f, yaw, 0.0f) * Quaternion.Euler(pitch, 0.0f, roll));
            }
        }
    }
}
