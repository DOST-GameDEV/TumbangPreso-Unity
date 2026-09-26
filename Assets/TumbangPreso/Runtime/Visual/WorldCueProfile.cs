using UnityEngine;

namespace TumbangPreso.Visual
{
    [CreateAssetMenu(menuName = "Tumbang Preso/World cue profile")]
    public sealed class WorldCueProfile : ScriptableObject
    {
        [Range(0,1)] public float WorldLighting=1;
        [Range(0,1)] public float LagoonDeckDetail=1;
        [Range(0,1)] public float CourtSurface=1;
        [Range(0,1)] public float HeroObjects=1;
        [Range(0,1)] public float InkEffects=1;
        [Range(0,1)] public float ExchangePoses=1;
        [Range(0,1)] public float RoundRhythm=1;
        [Range(0,1)] public float EnvironmentAppeal=1;
        [Range(0,1)] public float ImpactSeparation=1;
        [Range(0,1)] public float ViewmodelFraming=1;
        [Range(0,.3f)] public float ViewmodelRim=.16f;
        // Experiments remain off until the owner's later default choice.
        [Range(0,1)] public float SpeedLines;
        [Range(0,1)] public float UltimateDesaturation;
        [Range(0,1)] public float SoundPips;
        [Range(0,1)] public float TayaTarget = 1;
        [Range(0,1)] public float RestoreClock = 1;
        [Range(0,1)] public float DistanceReadability = 1;
        [Range(0,1)] public float Boundary = 1;
        [Range(0,1)] public float NearestExit = 1;
        [Range(0,1)] public float Escape = .8f;
        [Range(0,.2f)] public float DangerAudio = .06f;
        public Color Chalk = new Color(.96f,.92f,.81f,1);
        public Color Ink = new Color(.14f,.11f,.075f,1);
        /// <summary>
        /// ⚠️⚠️ THE WEIGHT THE WORLD LOOK IS ACTUALLY DRAWN AT. READ THIS, NEVER
        /// <see cref="WorldLighting"/> ALONE, in anything that draws. `WorldLighting` is the
        /// profile's authoring and diagnostic switch (the graphics probe and the stage tests set it
        /// to 0 to photograph a map's own lighting); the player's Graphics tab choice is
        /// <see cref="Settings.LightingStyles.LookWeight"/>. Either one at 0 is each map's authored
        /// lighting, the lighting on `main`, so the product is the only honest answer to "how much
        /// of the look is on". A consumer that read the field alone would keep the bright contact
        /// shadows under a player who picked Classic.
        /// </summary>
        public static float LightingWeight => Mathf.Clamp01(Current.WorldLighting) * Settings.LightingStyles.LookWeight;
        private static WorldCueProfile _current;
        public static WorldCueProfile Current
        {
            get
            {
                if (_current != null && _current.hideFlags != HideFlags.HideAndDontSave) return _current;
                var authored = Resources.Load<WorldCueProfile>("WorldCueProfile");
                if (authored != null)
                {
                    if (_current != null)
                    {
                        if (Application.isPlaying) Destroy(_current); else DestroyImmediate(_current);
                    }
                    return _current = authored;
                }
                if (_current == null)
                {
                    _current = CreateInstance<WorldCueProfile>();
                    _current.hideFlags = HideFlags.HideAndDontSave;
                }
                return _current;
            }
        }
    }
}
