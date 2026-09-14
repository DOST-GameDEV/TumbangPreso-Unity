using System;
using System.Collections.Generic;
using UnityEngine;

namespace TumbangPreso.UI
{
    [CreateAssetMenu(menuName="TUMP/UI/Layout overrides",fileName="OwnerUiOverrides")]
    public sealed class OwnerUiOverrideBook : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public bool Enabled=true;
            public string CanvasName;
            public string ElementPath;
            public bool ReplaceText;
            public string ExpectedText;
            [TextArea]public string Replacement;
            public Font Font;
            public int FontSize;
            public bool ChangeInk;
            public Color Ink=Color.white;
            public bool Move;
            public Vector2 Offset;
            public bool Resize;
            public Vector2 Size;
            public Sprite Artwork;
            public bool ChangePaperTreatment;
            public OwnerUiPaper.Treatment PaperTreatment;
        }
        public List<Entry> Entries=new List<Entry>();
    }
}
