using System.Collections.Generic;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    public static partial class PaeteAbilityFilm
    {
        /// <summary>
        /// The guardian as the ULTIMATE'S CUTSCENE stages it (`HeroIntroductionScene.Paete`): `Staged`, on a clock 1.45
        /// times its own, making its rise's effects on the stage (`PaeteSentryBody.StageFx`) and having them stepped by
        /// the stage (`PaeteFx.StepStage`). This is the only way to look at that path outside Play: the cutscene itself
        /// (his performance, Makiling, the cameras) is not here. Then, past the cutscene's end, the same tree's EXIT at
        /// its own pace, so the whole of it going back under the court can be seen.
        /// </summary>
        private sealed class SentryStageFilm : FxFilm
        {
            private const float ArriveAt = 0.3f, Pace = 1.45f, ExitFrom = 4.2f;
            private PaeteSentryBody _body;
            private Transform _stage;

            public override string Name => "stagefx";
            public override float Seconds => 6.4f;
            public override float[] StripTimes => new[] { 0.32f, 0.50f, 0.60f, 0.80f, 0.90f, 1.10f, 1.20f, 1.40f, 1.55f, 1.75f, 2.4f, 4.5f, 4.8f, 5.0f, 5.2f, 5.5f };
            public override (Vector3 eye, Vector3 look, float fov)[] Views => new[]
            {
                (new Vector3(8.5f, 4.6f, 12.5f), new Vector3(0f, 3.4f, 0f), 46f),
                (new Vector3(3.2f, 1.6f, 6.4f), new Vector3(0f, 1.5f, 0f), 50f),
            };

            public override void Begin(FxStage stage)
            {
                _stage = stage.New("~SentryStage").transform;
                _body = PaeteSentryBody.Build(_stage);
                _body.Staged = true;
                _body.StageFx = _stage;
                _body.SetFacing(Vector3.forward);
                _body.SetTargets(new List<CharacterMotor>());
            }

            public override void Step(FxStage stage, float t, float dt)
            {
                // The rise on the cutscene's faster clock; then, from `ExitFrom`, the last second of its life at its own pace.
                float age = t < ExitFrom ? (t - ArriveAt) * Pace : _body.LifeSeconds - 0.5f + (t - ExitFrom);
                if (t >= ExitFrom + 1.15f) { _body.gameObject.SetActive(false); }
                else _body.Pose(age, Vector3.zero);
                PaeteFx.StepStage(_stage, dt);
            }
        }
    }
}
