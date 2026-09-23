using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI.Hub
{
    /// <summary>
    /// The UX-1 icon family: one stroke weight, round-ish ends, drawn as mesh.
    ///
    /// ⚠️ PROJECT-GENERATED ICONS ARE SWAPPABLE PLACEHOLDERS (`AGENTS.md`), and these are that.
    /// They share one construction with `TumpAbilitySymbol.HudStyle`'s rule (one stroke weight, one
    /// corner treatment) so the front end and the match read as one family; the owner's own art,
    /// where it exists, always wins over a glyph here.
    ///
    /// ⚠️ EVERY GLYPH IS AUTHORED IN A UNIT SQUARE FROM -0.5 TO 0.5 and scaled by the SHORT side of
    /// its rect, so a glyph in a wide rect stays square instead of stretching.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class HubGlyph : MaskableGraphic
    {
        public enum Mark
        {
            None, Back, Menu, Plus, Close, Star, StarFilled, Lock, Check, Left, Right, Cap, Shop, Task,
            Tree, Gear, Person, Friends, Trophy, Crown, Expand, Slipper, Can, Bots, Globe, House, Key,
            Play, Pencil, Info, Clock, Hero, Party, Book, Exit, Dot, Down, Eye
        }

        public Mark Kind;

        /// <summary>Stroke weight as a fraction of the glyph's size. 0.085 is the family's weight.</summary>
        public float Weight = 0.085f;

        public override Texture mainTexture => s_WhiteTexture;

        private VertexHelper _vh;
        private Vector2 _centre;
        private float _scale;
        private Color32 _colour;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            _vh = vh;
            _centre = r.center;
            _scale = Mathf.Min(r.width, r.height);
            _colour = color;
            if (_scale < 2) return;

            switch (Kind)
            {
                case Mark.Back: Line(.30f, 0, -.28f, 0); Line(-.28f, 0, -.04f, .24f); Line(-.28f, 0, -.04f, -.24f); break;
                case Mark.Menu: Line(-.30f, .22f, .30f, .22f); Line(-.30f, 0, .30f, 0); Line(-.30f, -.22f, .30f, -.22f); break;
                case Mark.Plus: Line(-.28f, 0, .28f, 0); Line(0, -.28f, 0, .28f); break;
                case Mark.Close: Line(-.24f, -.24f, .24f, .24f); Line(-.24f, .24f, .24f, -.24f); break;
                case Mark.Star: Star(false); break;
                case Mark.StarFilled: Star(true); break;
                case Mark.Lock:
                    Fill(-.26f, -.34f, .26f, -.34f, .26f, .06f, -.26f, .06f);
                    Arc(0, .06f, .17f, 0, 180, 10);
                    break;
                case Mark.Check: Line(-.30f, .02f, -.08f, -.22f); Line(-.08f, -.22f, .32f, .26f); break;
                case Mark.Left: Line(.10f, .28f, -.16f, 0); Line(-.16f, 0, .10f, -.28f); break;
                case Mark.Right: Line(-.10f, .28f, .16f, 0); Line(.16f, 0, -.10f, -.28f); break;
                case Mark.Cap: Cap(); break;
                case Mark.Shop:
                    // A sari-sari stall: an awning of three scallops over a counter.
                    Line(-.36f, .20f, .36f, .20f);
                    Arc(-.24f, .20f, .12f, 180, 360, 8); Arc(0, .20f, .12f, 180, 360, 8); Arc(.24f, .20f, .12f, 180, 360, 8);
                    Line(-.36f, .20f, -.30f, .38f); Line(.36f, .20f, .30f, .38f); Line(-.30f, .38f, .30f, .38f);
                    Line(-.28f, .04f, -.28f, -.36f); Line(.28f, .04f, .28f, -.36f); Line(-.36f, -.36f, .36f, -.36f);
                    break;
                case Mark.Task:
                    Line(-.26f, .34f, .26f, .34f); Line(.26f, .34f, .26f, -.36f); Line(.26f, -.36f, -.26f, -.36f); Line(-.26f, -.36f, -.26f, .34f);
                    Line(-.14f, -.02f, -.03f, -.14f); Line(-.03f, -.14f, .15f, .12f);
                    break;
                case Mark.Tree:
                    Line(0, -.38f, 0, .02f); Line(0, .02f, -.24f, .22f); Line(0, .02f, .24f, .22f); Line(0, -.14f, .20f, -.02f);
                    Dot(-.24f, .26f, .09f); Dot(.24f, .26f, .09f); Dot(.22f, -.02f, .07f); Dot(0, .08f, .07f);
                    break;
                case Mark.Gear:
                    Arc(0, 0, .22f, 0, 360, 18);
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI / 4;
                        Line(Mathf.Cos(a) * .24f, Mathf.Sin(a) * .24f, Mathf.Cos(a) * .36f, Mathf.Sin(a) * .36f);
                    }
                    Dot(0, 0, .07f);
                    break;
                case Mark.Person: Arc(0, .16f, .15f, 0, 360, 16); Arc(0, -.36f, .30f, 20, 160, 10); break;
                case Mark.Friends:
                    Arc(-.14f, .14f, .12f, 0, 360, 14); Arc(-.14f, -.36f, .24f, 25, 155, 8);
                    Arc(.20f, .20f, .10f, 0, 360, 12); Arc(.20f, -.20f, .18f, 30, 150, 8);
                    break;
                case Mark.Trophy:
                    Line(-.22f, .34f, .22f, .34f); Arc(0, .34f, .22f, 180, 360, 10);
                    Arc(-.24f, .20f, .09f, 90, 270, 6); Arc(.24f, .20f, .09f, -90, 90, 6);
                    Line(0, .12f, 0, -.24f); Line(-.18f, -.32f, .18f, -.32f);
                    break;
                case Mark.Crown: Fill(-.34f, -.22f, .34f, -.22f, .36f, .24f, .16f, .02f, 0, .30f, -.16f, .02f, -.36f, .24f); break;
                case Mark.Expand:
                    Line(-.36f, .36f, -.14f, .36f); Line(-.36f, .36f, -.36f, .14f);
                    Line(.36f, .36f, .14f, .36f); Line(.36f, .36f, .36f, .14f);
                    Line(-.36f, -.36f, -.14f, -.36f); Line(-.36f, -.36f, -.36f, -.14f);
                    Line(.36f, -.36f, .14f, -.36f); Line(.36f, -.36f, .36f, -.14f);
                    break;
                case Mark.Slipper:
                    // A tsinelas seen from above: the sole and its V-strap.
                    Arc(0, .12f, .18f, 0, 180, 10); Arc(0, -.16f, .16f, 180, 360, 10);
                    Line(-.18f, .12f, -.16f, -.16f); Line(.18f, .12f, .16f, -.16f);
                    Line(-.10f, .20f, 0, .04f); Line(.10f, .20f, 0, .04f);
                    break;
                case Mark.Can:
                    Arc(0, .26f, .22f, 0, 360, 16, .35f);
                    Line(-.22f, .26f, -.22f, -.26f); Line(.22f, .26f, .22f, -.26f);
                    Arc(0, -.26f, .22f, 180, 360, 10, .35f);
                    break;
                case Mark.Bots:
                    Line(-.28f, .20f, .28f, .20f); Line(.28f, .20f, .28f, -.28f); Line(.28f, -.28f, -.28f, -.28f); Line(-.28f, -.28f, -.28f, .20f);
                    Line(0, .20f, 0, .34f); Dot(-.12f, -.02f, .06f); Dot(.12f, -.02f, .06f);
                    break;
                case Mark.Globe:
                    Arc(0, 0, .34f, 0, 360, 22); Arc(0, 0, .14f, 90, 270, 8, 2.4f); Arc(0, 0, .14f, -90, 90, 8, 2.4f);
                    Line(-.34f, 0, .34f, 0);
                    break;
                case Mark.House:
                    Line(-.34f, .02f, 0, .34f); Line(0, .34f, .34f, .02f);
                    Line(-.24f, .08f, -.24f, -.34f); Line(.24f, .08f, .24f, -.34f); Line(-.24f, -.34f, .24f, -.34f);
                    break;
                case Mark.Key:
                    Arc(-.18f, .10f, .15f, 0, 360, 14);
                    Line(-.06f, 0, .34f, -.26f); Line(.20f, -.16f, .26f, -.06f); Line(.28f, -.22f, .34f, -.12f);
                    break;
                case Mark.Play: Fill(-.20f, .32f, .30f, 0, -.20f, -.32f); break;
                case Mark.Pencil: Line(-.28f, -.28f, .22f, .22f); Line(.22f, .22f, .30f, .14f); Line(-.28f, -.28f, -.32f, -.36f); break;
                case Mark.Info: Arc(0, 0, .34f, 0, 360, 22); Line(0, -.18f, 0, .06f); Dot(0, .18f, .05f); break;
                case Mark.Clock: Arc(0, 0, .34f, 0, 360, 22); Line(0, 0, 0, .20f); Line(0, 0, .14f, -.08f); break;
                case Mark.Hero: Fill(-.32f, .16f, -.08f, .34f, .08f, .34f, .32f, .16f, .20f, -.10f, 0, -.34f, -.20f, -.10f); break;
                case Mark.Party: Arc(-.18f, .12f, .10f, 0, 360, 12); Arc(.18f, .12f, .10f, 0, 360, 12); Arc(0, -.12f, .10f, 0, 360, 12); Line(-.34f, -.34f, .34f, -.34f); break;
                case Mark.Book:
                    Line(0, .30f, 0, -.34f);
                    Line(0, .30f, -.34f, .36f); Line(-.34f, .36f, -.34f, -.28f); Line(-.34f, -.28f, 0, -.34f);
                    Line(0, .30f, .34f, .36f); Line(.34f, .36f, .34f, -.28f); Line(.34f, -.28f, 0, -.34f);
                    break;
                case Mark.Exit:
                    Line(.04f, .34f, -.30f, .34f); Line(-.30f, .34f, -.30f, -.34f); Line(-.30f, -.34f, .04f, -.34f);
                    Line(-.06f, 0, .36f, 0); Line(.36f, 0, .20f, .16f); Line(.36f, 0, .20f, -.16f);
                    break;
                case Mark.Dot: Dot(0, 0, .40f); break;
                case Mark.Down: Line(-.28f, .10f, 0, -.16f); Line(0, -.16f, .28f, .10f); break;
                case Mark.Eye: Arc(0, -.30f, .44f, 40, 140, 10); Arc(0, .30f, .44f, 220, 320, 10); Dot(0, 0, .12f); break;
            }
        }

        // ------------------------------------------------------------------ primitives

        private Vector2 P(float x, float y) => _centre + new Vector2(x, y) * _scale;

        private void Line(float x0, float y0, float x1, float y1)
        {
            Vector2 a = P(x0, y0), b = P(x1, y1);
            Vector2 d = b - a;
            if (d.sqrMagnitude < 0.0001f) return;
            float half = Weight * _scale * 0.5f;
            Vector2 n = new Vector2(-d.y, d.x).normalized * half;
            Vector2 t = d.normalized * half * 0.6f;

            // Extended a little past each end so joints overlap instead of notching.
            a -= t; b += t;
            int i = _vh.currentVertCount;
            _vh.AddVert(a + n, _colour, Vector2.zero);
            _vh.AddVert(b + n, _colour, Vector2.zero);
            _vh.AddVert(b - n, _colour, Vector2.zero);
            _vh.AddVert(a - n, _colour, Vector2.zero);
            _vh.AddTriangle(i, i + 1, i + 2);
            _vh.AddTriangle(i, i + 2, i + 3);
        }

        private void Arc(float cx, float cy, float radius, float fromDeg, float toDeg, int steps, float squash = 1.0f)
        {
            float from = fromDeg * Mathf.Deg2Rad, to = toDeg * Mathf.Deg2Rad;
            for (int s = 0; s < steps; s++)
            {
                float a = Mathf.Lerp(from, to, s / (float)steps);
                float b = Mathf.Lerp(from, to, (s + 1) / (float)steps);
                Line(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius * squash,
                     cx + Mathf.Cos(b) * radius, cy + Mathf.Sin(b) * radius * squash);
            }
        }

        private void Dot(float cx, float cy, float radius)
        {
            const int steps = 14;
            int start = _vh.currentVertCount;
            _vh.AddVert(P(cx, cy), _colour, Vector2.zero);
            for (int s = 0; s < steps; s++)
            {
                float a = s * Mathf.PI * 2 / steps;
                _vh.AddVert(P(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius), _colour, Vector2.zero);
            }
            for (int s = 0; s < steps; s++) _vh.AddTriangle(start, start + 1 + s, start + 1 + (s + 1) % steps);
        }

        /// <summary>A convex filled polygon from x,y pairs, fanned from its centroid.</summary>
        private void Fill(params float[] xy)
        {
            int count = xy.Length / 2;
            Vector2 centre = Vector2.zero;
            for (int k = 0; k < count; k++) centre += new Vector2(xy[k * 2], xy[k * 2 + 1]);
            centre /= count;
            int start = _vh.currentVertCount;
            _vh.AddVert(P(centre.x, centre.y), _colour, Vector2.zero);
            for (int k = 0; k < count; k++) _vh.AddVert(P(xy[k * 2], xy[k * 2 + 1]), _colour, Vector2.zero);
            for (int k = 0; k < count; k++) _vh.AddTriangle(start, start + 1 + k, start + 1 + (k + 1) % count);
        }

        private void Star(bool filled)
        {
            var pts = new float[20];
            for (int k = 0; k < 10; k++)
            {
                float a = Mathf.PI / 2 + k * Mathf.PI / 5;
                float rad = k % 2 == 0 ? .38f : .16f;
                pts[k * 2] = Mathf.Cos(a) * rad;
                pts[k * 2 + 1] = Mathf.Sin(a) * rad - .02f;
            }
            if (filled)
            {
                // A star is not convex, so it is five triangles and a pentagon.
                for (int k = 0; k < 10; k += 2)
                {
                    int prev = (k + 9) % 10, next = (k + 1) % 10;
                    Fill(pts[k * 2], pts[k * 2 + 1], pts[next * 2], pts[next * 2 + 1], pts[prev * 2], pts[prev * 2 + 1]);
                }
                Fill(pts[2], pts[3], pts[6], pts[7], pts[10], pts[11], pts[14], pts[15], pts[18], pts[19]);
            }
            else
            {
                for (int k = 0; k < 10; k++)
                {
                    int n = (k + 1) % 10;
                    Line(pts[k * 2], pts[k * 2 + 1], pts[n * 2], pts[n * 2 + 1]);
                }
            }
        }

        /// <summary>A tansan, the crimped bottle cap: a ring with teeth, and the cap's face.</summary>
        private void Cap()
        {
            const int teeth = 12;
            for (int k = 0; k < teeth * 2; k++)
            {
                float a = k * Mathf.PI / teeth, b = (k + 1) * Mathf.PI / teeth;
                float ra = k % 2 == 0 ? .38f : .31f, rb = (k + 1) % 2 == 0 ? .38f : .31f;
                Line(Mathf.Cos(a) * ra, Mathf.Sin(a) * ra, Mathf.Cos(b) * rb, Mathf.Sin(b) * rb);
            }
            Dot(0, 0, .18f);
        }

        public void Redraw() => SetVerticesDirty();
    }
}
