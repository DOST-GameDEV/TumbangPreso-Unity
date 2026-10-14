using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE WORLD WHILE NEMU'S LET THEM IN HOLDS (v10, 2026-10-14, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section
    /// 19). While anyone is possessed, on every screen:
    ///  * THE DUSK: every match camera's frame goes through `Shaders/NemuInk` with `_Eerie` (the sky bruised violet-black, the
    ///    world drained and cooled), the same grade the cutscene ends on, from `ColourGrade`. (Owner: *"u can change ren the
    ///    color of the game for everyone"*.)
    ///  * THE GATE: the tear she opened (`NemuPortal`) hangs open over the court beyond the possessed. (Owner on v9: *"siguro wag
    ///    na floor cracks"*, *"some sort of otherwordly portal somewhere where the ghosts come from"*.)
    ///  * THE SOULS: the cutscene's storm goes on circling the court round it (*"continue showing for the whole 15 seconds kahit
    ///    tapos na"*). As the possession runs down they go back into the gate one at a time, so the court can be read as a
    ///    clock (*"as ult lasts makikita mo na the ghosts slowly fade"*); the last three, and the gate, only go with the last
    ///    possession's own leave (*"ghosts only fade when posession completley disappears"*).
    /// PRESENTATION ONLY: posed from the replicated possession clocks, so every peer sees the same; it touches no rule and no state.
    /// </summary>
    public sealed class NemuWorld : MonoBehaviour
    {
        private const float GateSize = 3.4f, GateHeight = 4.2f;
        private const int Souls = 10, ReducedSouls = 5, Kept = 3, Tail = 8;
        private const float TailStep = .035f, ReturnSeconds = 1.1f;

        private static NemuWorld _live;
        private static Material _ink;
        private NemuPortal _gate;
        private NemuSmokeBatch _smoke;
        private Vector3 _centre, _gateAt;
        private float _reach = 5f;
        private bool _placed;
        private readonly Vector3[] _tail = new Vector3[Tail];
        private readonly bool[] _on = new bool[Tail];

        /// <summary>0 to 1: how far the world is in Nemu's dusk now.</summary>
        public static float Weight { get; private set; }

        /// <summary>Called by every possessed body's presence each frame; makes the world's one keeper if there is none.</summary>
        public static void Ensure()
        {
            if (_live != null || !Application.isPlaying) return;
            _live = new GameObject("~NemuWorld").AddComponent<NemuWorld>();
        }

        /// <summary>How much of the dusk a camera's frame takes: only the match's own view (not portraits or map previews).</summary>
        public static float WeightFor(Camera camera) => camera != null && camera == Camera.main ? Weight : 0f;

        /// <summary>The dusk over a frame.</summary>
        public static void Blit(RenderTexture source, RenderTexture destination, float weight)
        {
            if (_ink == null)
            {
                var shader = Resources.Load<Shader>("Shaders/NemuInk");
                if (shader == null) { Graphics.Blit(source, destination); return; }
                _ink = new Material(shader) { name = "NemuWorldDusk", hideFlags = HideFlags.HideAndDontSave };
            }
            _ink.SetFloat("_Eerie", Mathf.Clamp01(weight));
            _ink.SetFloat("_Ink", 0f); _ink.SetFloat("_Black", 0f); _ink.SetFloat("_Night", 0f);
            _ink.SetFloat("_Impact", 0f); _ink.SetFloat("_Lines", 0f);
            _ink.SetFloat("_Clock", Time.time);
            Graphics.Blit(source, destination, _ink);
        }

        private void LateUpdate()
        {
            var round = GameServices.Round;
            float weight = 0f, clock = 0f, left = 0f;
            CharacterMotor nemu = null;
            Vector3 centre = Vector3.zero; int held = 0;
            if (round != null && round.RoundActive)
                foreach (var p in round.Players)
                {
                    if (p == null || !p.gameObject.activeInHierarchy) continue;
                    if (p.IsPossessed)
                    {
                        float remaining = NemuRules.PossessedSeconds - p.PossessionElapsed;
                        weight = Mathf.Max(weight, Mathf.Clamp01(remaining / NemuRules.LeaveSeconds));
                        // The storm keeps the time of the possession with the most left.
                        if (remaining > left) { left = remaining; clock = p.PossessionElapsed; }
                        centre += p.transform.position; held++;
                    }
                    else if (nemu == null && p.GetComponent<Abilities.HeroAbilitySystem>()?.HeroId == "nemu") nemu = p;
                }
            Weight = weight;
            if (weight <= 0f) { Destroy(gameObject); return; }

            if (!_placed && held > 0)
            {
                // Over the court beyond the possessed (seen from where she cast), as the cutscene hung it.
                _placed = true;
                _centre = centre / held;
                _centre.y = Slipper.GroundY(_centre + Vector3.up * .5f);
                Vector3 way = nemu != null ? _centre - nemu.transform.position : Vector3.forward;
                way.y = 0f; way = way.sqrMagnitude > .04f ? way.normalized : Vector3.forward;
                _gateAt = _centre + way * 4.2f + Vector3.up * GateHeight;
                _reach = 3.5f;
                foreach (var p in round.Players)
                    if (p != null && p.IsPossessed) _reach = Mathf.Max(_reach, Vector3.Distance(Flat(p.transform.position), Flat(_centre)) + 2f);
                _reach = Mathf.Min(_reach, 8f);
                _gate = new NemuPortal(transform, _gateAt, GateSize);
                _smoke = new NemuSmokeBatch(transform, "NemuWorldSouls", 220, 120, 60);
            }
            if (!_placed) return;

            bool reduced = Settings.SettingsStore.Current.ReducedEffects;
            // The court's clock: the rest of the storm a little fainter as it runs down, all of it gone with the last leave.
            float runDown = Mathf.Clamp01(left / NemuRules.PossessedSeconds);
            float fade = weight * (.6f + .4f * runDown);
            _gate?.Set(1f, weight, .25f * Flash(clock, 0f, .8f), Time.time);

            var cam = Camera.main;
            Vector3 eye = cam != null ? cam.transform.position : _centre + Vector3.up * 1.6f;
            _smoke.Begin();
            int count = reduced ? ReducedSouls : Souls;
            for (int i = 0; i < count; i++)
            {
                float gone = Leaves(i, count);
                for (int k = 0; k < Tail; k++) _on[k] = Soul(i, clock - k * TailStep, gone, out _tail[k]);
                if (!_on[0]) continue;
                float home = gone < 90f ? 1f - Mathf.Clamp01((clock - gone) / ReturnSeconds) : 1f;
                float size = (.34f + .1f * NemuSouls.Hash(i, 21)) * (.5f + .5f * home);
                NemuSouls.Draw(_smoke, _tail, _on, Tail, i + 1, size, fade * .8f * Mathf.Sqrt(home), .9f, Time.time, eye);
            }
            // The gate's own light on the court under it and round its lip.
            _smoke.Light(new Vector3(_gateAt.x, _centre.y + .12f, _gateAt.z), 2.2f, .45f * weight, NemuSmokeBatch.Violet, default, 1.5f);
            _smoke.Light(_gateAt, GateSize * .9f, .3f * weight, NemuSmokeBatch.Violet);
            _smoke.End(Time.time);
        }

        /// <summary>When soul <paramref name="i"/> goes back into the gate (seconds into the possession): the last of them first,
        /// from 3 s on, the last one by 13 s; the first <see cref="Kept"/> never (they go with the leave).</summary>
        private static float Leaves(int i, int count)
        {
            if (i < Kept || count <= Kept) return 99f;
            int going = count - Kept;
            float order = (count - 1 - i) / (float)Mathf.Max(1, going - 1);
            return Mathf.Lerp(3f, NemuRules.PossessedSeconds - 2f, order);
        }

        /// <summary>Where soul <paramref name="i"/> is at <paramref name="s"/> seconds into the possession (world): circling the
        /// court high, wide and spread out, dipping now and then; once it leaves, drawn back into the gate. False once in.</summary>
        private bool Soul(int i, float s, float gone, out Vector3 at)
        {
            float h1 = NemuSouls.Hash(i, 1), h2 = NemuSouls.Hash(i, 2), h3 = NemuSouls.Hash(i, 3);
            float radius = _reach * Mathf.Lerp(.55f, 1.1f, Mathf.Sqrt(h2));
            float speed = (3f + 1.5f * h3) / Mathf.Max(1f, radius) * (i % 3 == 0 ? -1f : 1f);
            float phase = i / (float)Souls * Mathf.PI * 2f;
            Vector3 Circle(float time)
            {
                float a = phase + speed * time;
                float r = radius * (1f + .1f * Mathf.Sin(time * .9f + phase));
                float h = 2.8f + 2.6f * h1 + .5f * Mathf.Sin(time * 1.3f + phase * 3f) - 1.4f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(time * .45f + phase * 2f)), 8f);
                return _centre + new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r);
            }
            if (s < gone) { at = Circle(s); return true; }
            float u = (s - gone) / ReturnSeconds;
            if (u >= 1f) { at = default; return false; }
            Vector3 from = Circle(gone);
            float e = u * u * (3f - 2f * u);
            at = Vector3.Lerp(from, _gateAt, e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * 1.2f);
            return true;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
        private static float Flash(float t, float at, float width) { float u = (t - at) / width; return u < 0f || u > 1f ? 0f : 1f - u; }

        private void OnDestroy()
        {
            _gate?.Dispose(); _gate = null;
            _smoke?.Dispose(); _smoke = null;
            if (_live == this) { _live = null; Weight = 0f; }
        }
    }
}
