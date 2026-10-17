using TumbangPreso.Core;
using UnityEngine;

namespace TumbangPreso.Visual
{
    /// <summary>
    /// ⚠️ AMIHAN'S WHIRLWIND, DRAWN AS A WHIRLWIND. It is behind a switch (`AmihanGaleFront.Whirlwind`); the front it
    /// replaces is still there, one word away.
    ///
    /// The owner, of the ability's name against what it drew (five ribbons, a skirt and threads): "the ability is called
    /// whirlwind? then make it look like a whirlwind". Three small ones were stood abreast on the old band, each
    /// stretched thin to stand over a body, and he said of them: "this looks horrible. the whirlwind should be larger.
    /// maybe just do 1 large whirlwind only. feel free to rework the ability itself". So there is ONE, and the rule was
    /// changed to be the ground it stands on (`AmihanRules.WhirlwindRadius`: a circle that travels).
    ///
    /// ⚠️ THE WHIRLWIND ITSELF IS NOT MADE HERE. It is the effects study's (`FxStudyWhirlwind`), which he approved on the
    /// bench as film h8: "that looks amazing as is". This CALLS that, once, and changes nothing of its shape: it is
    /// made bigger EVENLY, never stretched (the stretch is what broke the look when there were three), and it wears
    /// the same colours the bench does (`FxStudyWhirlwind.Look.Picture`). The one thing that differs from the bench is
    /// how much of it is light laid on (`DayLight`).
    ///
    /// ⚠️ THE SHAPE TELLS THE TRUTH, BY CONSTRUCTION. The ground swirl it stands on is drawn exactly as wide as the
    /// circle the rule hits with: both are `AmihanRules.WhirlwindRadius`, and there is no second number to drift. Its
    /// horn is narrower than its swirl at every height, so nothing overhangs what it hits.
    ///
    /// Where it is, is the front's (`AmihanGaleFront.FrontAt`, the same sum the rule uses): this hangs under it.
    /// Nothing here is on the wire and nothing here decides anything.
    /// </summary>
    public sealed class AmihanWhirlwind
    {
        /// <summary>A film or a replay has no main camera: it says where the lens is, and the motes turn to it.</summary>
        public static Vector3? StagedEye;
        /// <summary>
        /// ⚠️ FOR A FILM ONLY, read when one is built, and left as they are in play: how much of it is light laid on
        /// (below 0 is `DayLight`), and how wide its swirl is in metres (below 0 is the rule's own width), so a film can
        /// stand another setting beside the one a match draws.
        /// </summary>
        public static float StagedLight = -1f, StagedAcross = -1f;

        // The bench whirlwind's ground swirl reaches 1.07 m from its middle (and its horn stands 2.2 m).
        private const float BenchReach = 1.03f * 1.04f;
        // ⚠️ KEEP. Of the court film this value was filmed for (one2) the owner said: "looks good"; and after playing it:
        // "whirlwind is already good". It is backed up in `tools/cutscene_rework/approved/whirlwind_final_2026-10-11/`.
        // ⚠️ HOW MUCH OF IT IS LIGHT IN A MATCH. On the bench 0.85 of every piece is light laid on (it was approved so, on a
        // dark floor under a camera that blooms). Light laid on a sunlit road adds almost nothing and goes pale, so in a
        // match that share is this, and the rest of each piece COVERS what is behind it: the same pieces and colours,
        // but there in daylight. Filmed on Eskinita at 0.85 and at 0.50: at 0.50 it holds its colour.
        public const float DayLight = 0.50f;

        private readonly Transform _moving;
        private readonly FxStudyWhirlwind _wind;
        private readonly float _life, _size;

        public static AmihanWhirlwind Build(Transform front, float life) => new AmihanWhirlwind(front, life);

        private AmihanWhirlwind(Transform front, float life)
        {
            _life = life;
            var go = new GameObject("AmihanWhirlwind");
            _moving = go.transform;
            _moving.SetParent(front, false);
            var look = FxStudyWhirlwind.Look.Picture;
            look.Light = StagedLight >= 0f ? StagedLight : DayLight;
            _wind = new FxStudyWhirlwind(_moving, look);
            // Bigger evenly: its swirl as wide as the circle the rule hits with. ⚠️ KEEP: 4.2 m across (`WhirlwindRadius` 2.1)
            // and never stretched is what he approved ("make it larger", then "looks good"); the size is the rule's to change.
            _size = (StagedAcross > 0f ? StagedAcross * 0.5f : AmihanRules.WhirlwindRadius) / BenchReach;
            _wind.Root.localScale = Vector3.one * _size;
            // An effect, not a prop: the ground queries and the prop checks pass over it.
            foreach (var renderer in go.GetComponentsInChildren<MeshRenderer>(true)) VfxRenderTag.Attach(renderer.gameObject);
        }

        /// <summary>
        /// Poses it for the gale's age. ⚠️ A PURE FUNCTION OF THAT AGE: no clock of its own and nothing kept from the
        /// frame before, so a replay can scrub it and every peer draws the same frame.
        /// It stands up in the first fifth of a second (the circle already catches a body on its first frame, so it is
        /// a third of its height at once), travels as it was approved, and thins out over the last third of a second by
        /// the whirlwind's own way of coming apart. Nothing new was designed for either end.
        /// </summary>
        public void Pose(float seconds)
        {
            if (_moving == null || _wind == null) return;
            float t = Mathf.Clamp(seconds, 0f, _life);
            Vector3 eye;
            if (StagedEye.HasValue) eye = StagedEye.Value;
            else { var lens = Camera.main; eye = lens != null ? lens.transform.position : _moving.position + new Vector3(0f, 1.6f, -6f); }
            float stand = FxStudyKit.EaseOut(FxStudyKit.Run(t, 0f, 0.20f)), undone = FxStudyKit.Ease(FxStudyKit.Run(t, _life - 0.35f, _life));
            _wind.Root.localScale = new Vector3(_size * Mathf.Lerp(0.6f, 1f, stand), _size * Mathf.Lerp(0.33f, 1f, stand), _size * Mathf.Lerp(0.6f, 1f, stand));
            _wind.Pose(t, eye, Mathf.Lerp(0.5f, 1f, stand), undone);
        }

        /// <summary>Frees the whirlwind's own meshes and materials. The pictures it wears are everybody's.</summary>
        public void Free() { _wind?.Dispose(); }
    }
}
