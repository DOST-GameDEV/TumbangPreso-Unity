using System;
using UnityEngine;

namespace TumbangPreso.UI
{
    /// <summary>
    /// Where every piece of her login goes, read from the measurement file the
    /// extractor writes rather than from numbers typed in here.
    ///
    /// ⚠️⚠️ THE BOX IN THE JSON IS BOTH THE CUT AND THE PLACEMENT, AND THAT IS
    /// WHY IT IS GENERATED. `tools/extract_login_v3.py` cuts each sprite out of
    /// her element sheet at the same coordinates the piece occupies in her
    /// composition, which was measured rather than assumed: compositing every
    /// piece back at its own box and comparing against 41.png leaves only the
    /// text she typed on top. A layout typed by hand here is a second copy of
    /// that measurement, and the second copy is the one that drifts.
    ///
    /// ⚠️ `Width`/`Height` ARE THE PLACEMENT AND `SourceWidth`/`SourceHeight` ARE
    /// THE PIXELS. They differ for exactly two pieces, the green and the orange
    /// action plates, which she drew at 389 wide and placed at 424. That is a
    /// uniform 1.09 scale of her art, fitted by search, not a stretch: the two
    /// axes agreed to within a pixel. Drawing with `preserveAspect` keeps it that
    /// way, and this is the field that would catch anybody changing one axis.
    /// </summary>
    public static class OwnerLoginLayout
    {
        [Serializable] public sealed class Piece
        {
            public string name;
            public float x, y, width, height, faceX, faceY;
            public float sourceWidth, sourceHeight;
            public Rect Rect => new Rect(x, y, width, height);
            public Vector2 Size => new Vector2(width, height);
            public Vector2 Source => new Vector2(sourceWidth > 0 ? sourceWidth : width,
                                                 sourceHeight > 0 ? sourceHeight : height);
            public Vector2 Face => new Vector2(faceX, faceY);
        }

        [Serializable] private sealed class Book { public Piece[] pieces; }

        private static Book _book;

        public static Piece Get(string name)
        {
            if (_book == null)
            {
                var text = Resources.Load<TextAsset>("UI/owner-menu-edits/login-layout-v3");
                if (text == null) throw new InvalidOperationException(
                    "login-layout-v3 is missing; run tools/extract_login_v3.py");
                _book = JsonUtility.FromJson<Book>(text.text);
            }
            foreach (var piece in _book.pieces) if (piece.name == name) return piece;
            throw new InvalidOperationException("Missing supplied login layout: " + name);
        }

        public static void Place(Transform target, string name)
        {
            var p = Get(name);
            OwnerUiLayout.Place((RectTransform)target, p.x, p.y, p.width, p.height);
        }
    }
}
