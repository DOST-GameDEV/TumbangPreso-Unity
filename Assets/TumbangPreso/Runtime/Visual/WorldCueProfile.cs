using UnityEngine;

namespace TumbangPreso.Visual
{
    [CreateAssetMenu(menuName = "Tumbang Preso/World cue profile")]
    public sealed class WorldCueProfile : ScriptableObject
    {
        [Range(0,1)] public float WorldLighting=1;
        [Range(0,1)] public float CourtSurface=1;
        [Range(0,1)] public float HeroObjects=1;
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
