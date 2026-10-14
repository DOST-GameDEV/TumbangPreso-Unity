using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ THE WORLD WHILE NEMU'S LET THEM IN HOLDS (v9, 2026-10-14, `docs/reports/nemu-ultimate-2026-10-11/direction.md` section
    /// 18). The owner: *"u can change ren the color of the game for everyone by changing the shader or smth during her ult"* and
    /// *"keep the groound open during the duration of the possession"*. While anyone is possessed, on every screen:
    ///  * THE DUSK: every match camera's frame goes through `Shaders/NemuInk` with `_Eerie` (the sky bruised violet-black, the
    ///    world drained and cooled), the same grade the cutscene ends on, from `ColourGrade`.
    ///  * THE OPEN COURT: the fissures she split (`NemuCrackField`, the cutscene's own, from her feet at her facing) stay open.
    /// Both let go with the last possession's own leave (`NemuRules.LeaveSeconds`). PRESENTATION ONLY: posed from the
    /// replicated possession clocks, so every peer sees the same; it touches no rule and no state.
    /// </summary>
    public sealed class NemuWorld : MonoBehaviour
    {
        /// <summary>The fissures' length, the same in the cutscene, so the court she split is the court that stays open.</summary>
        public const float CrackLength = 9f;

        private static NemuWorld _live;
        private static Material _ink;
        private NemuCrackField _field;

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
            float weight = 0f;
            CharacterMotor nemu = null;
            if (round != null && round.RoundActive)
                foreach (var p in round.Players)
                {
                    if (p == null || !p.gameObject.activeInHierarchy) continue;
                    if (p.IsPossessed)
                        weight = Mathf.Max(weight, Mathf.Clamp01((NemuRules.PossessedSeconds - p.PossessionElapsed) / NemuRules.LeaveSeconds));
                    else if (nemu == null && p.GetComponent<Abilities.HeroAbilitySystem>()?.HeroId == "nemu") nemu = p;
                }
            Weight = weight;
            if (weight <= 0f) { Destroy(gameObject); return; }
            if (_field == null && nemu != null)
            {
                var at = nemu.transform.position;
                at.y = Slipper.GroundY(at + Vector3.up * .3f);
                transform.SetPositionAndRotation(at, Quaternion.Euler(0f, nemu.transform.eulerAngles.y, 0f));
                _field = new NemuCrackField(transform, CrackLength);
            }
            _field?.Set(CrackLength, weight, Time.time);
        }

        private void OnDestroy()
        {
            _field?.Dispose(); _field = null;
            if (_live == this) { _live = null; Weight = 0f; }
        }
    }
}
