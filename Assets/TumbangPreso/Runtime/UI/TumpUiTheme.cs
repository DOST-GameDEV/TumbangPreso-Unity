using UnityEngine;

namespace TumbangPreso.UI
{
    [CreateAssetMenu(menuName = "TUMP/UI/Theme", fileName = "TumpUiTheme")]
    public sealed class TumpUiTheme : ScriptableObject
    {
        [Header("Owner palette · sampled from the supplied colour sheet")]
        public Color Brick = new Color32(163, 26, 36, 255);
        public Color Orange = new Color32(236, 96, 21, 255);
        public Color Apricot = new Color32(248, 179, 102, 255);
        public Color Cream = new Color32(252, 211, 159, 255);
        public Color OliveSand = new Color32(145, 132, 80, 255);
        public Color Yellow = new Color32(247, 183, 34, 255);
        public Color Lime = new Color32(216, 208, 1, 255);
        public Color HotOrange = new Color32(243, 89, 1, 255);
        public Color Olive = new Color32(109, 119, 56, 255);
        public Color DeepOlive = new Color32(66, 66, 30, 255);

        [Header("Typography · Darumadrop is the main game UI face")]
        [Tooltip("Primary face for titles, controls, settings and scoreboard labels at generous sizes.")]
        public Font HeadingFont;
        [Tooltip("Supporting face only for genuinely dense or small secondary descriptions.")]
        public Font ReadingFont;
        public Font ReadingBoldFont;
        public int DisplaySize = 62;
        public int HeadingSize = 40;
        public int BodySize = 28;
        public int CaptionSize = 24;

        [Header("Shape and spacing")]
        [Range(2, 8)] public float InkWidth = 4;
        [Range(0, 14)] public float PrintDrop = 6;
        [Range(0, 24)] public float ContourCharacter = 10;
        public float ScreenMargin = 64;
        public float Gap = 20;
        public float ControlHeight = 76;
        public float PrimaryHeight = 104;
        public Vector2 ReferenceResolution = new Vector2(1920, 1080);

        [Header("Replaceable artwork")]
        public Sprite Logo;
        public Sprite SlipperMark;
        public Texture2D StreetBackground;
        [Tooltip("Separate transparent physical props for the illustrated background, never UI controls.")]
        public Texture2D StreetProps;
        public Rect StreetCanRect = new Rect(235, 119, 455, 655);
        public Rect StreetSlipperRect = new Rect(967, 107, 729, 621);

        private static TumpUiTheme _current;
        public static TumpUiTheme Current
        {
            get
            {
                if (_current != null) return _current;
                _current = Resources.Load<TumpUiTheme>("UI/brand/TumpUiTheme");
                if (_current == null)
                {
                    _current = CreateInstance<TumpUiTheme>();
                    _current.hideFlags = HideFlags.DontSave;
                }
                return _current;
            }
        }
        public Font Display => HeadingFont != null ? HeadingFont : Resources.Load<Font>("UI/fonts/DarumadropOne-Regular");
        public Font Body => ReadingFont != null ? ReadingFont : Resources.Load<Font>("UI/fonts/WorkSans-Regular");
        public Font Bold => ReadingBoldFont != null ? ReadingBoldFont : Resources.Load<Font>("UI/fonts/WorkSans-Bold");
    }
}
