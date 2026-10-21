using System.Text;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.Run
{
    /// <summary>
    /// The race's few words on screen: the clock, the stage's name, place, checkpoint n of N, a wrong-way call, and the
    /// result at the finish. Built from the game's own pieces (`TumpUiFactory`, the theme's fonts and cream ink with a
    /// dark outline, which is how `TumpMatchReadout` writes over the world), so it is not a new look. The countdown
    /// and the short calls ("BACK TO THE CHECKPOINT", "FINISH") go through `UI.Hud`, the game's own.
    ///
    /// The Training Range's readout already hides the tag game's clock and scoreboard, and this sits where they were.
    /// </summary>
    public sealed class RunHud : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _time, _stage, _place, _check, _wrong, _hint, _resultTitle, _resultRows, _resultFoot;
        private RectTransform _result;
        private readonly StringBuilder _text = new StringBuilder(256);
        private int _shownCentis = -1, _shownNode = -1, _shownPlace = -1, _shownCount = -1;
        private bool _built;

        public void Build(RunMode mode)
        {
            if (_built) return;
            _built = true;
            _canvas = TumpUiFactory.Canvas(transform, "PaeteRunHud", 104);
            // A readout, not a menu: it takes no focus and no clicks.
            var focus = _canvas.GetComponent<InputLayer.ScreenFocus>(); if (focus != null) focus.enabled = false;
            var caster = _canvas.GetComponent<GraphicRaycaster>(); if (caster != null) caster.enabled = false;
            var root = _canvas.transform;
            var theme = TumpUiTheme.Current;

            _time = Ink(root, "RaceClock", "0:00.00", 58, true);
            TumpUiFactory.Anchor(_time.rectTransform, new Vector2(.5f, 1), new Vector2(0, -62), new Vector2(460, 76));
            _stage = Ink(root, "RaceStage", "", 26, false);
            TumpUiFactory.Anchor(_stage.rectTransform, new Vector2(.5f, 1), new Vector2(0, -116), new Vector2(1300, 40));
            _place = Ink(root, "RacePlace", "", 50, true);
            _place.alignment = TextAnchor.MiddleLeft;
            TumpUiFactory.Anchor(_place.rectTransform, new Vector2(0, 1), new Vector2(250, -66), new Vector2(380, 70));
            _check = Ink(root, "RaceCheckpoint", "", 26, false);
            _check.alignment = TextAnchor.MiddleLeft;
            TumpUiFactory.Anchor(_check.rectTransform, new Vector2(0, 1), new Vector2(250, -116), new Vector2(380, 40));
            _hint = Ink(root, "RaceHint", "Backspace: back to your last checkpoint", 20, false);
            _hint.alignment = TextAnchor.MiddleLeft;
            TumpUiFactory.Anchor(_hint.rectTransform, new Vector2(0, 1), new Vector2(250, -150), new Vector2(380, 30));
            _wrong = Ink(root, "RaceWrongWay", "WRONG WAY", 64, true);
            _wrong.color = theme.HotOrange;
            TumpUiFactory.Anchor(_wrong.rectTransform, new Vector2(.5f, .72f), Vector2.zero, new Vector2(900, 90));
            _wrong.enabled = false;

            _result = TumpUiFactory.Rect(root, "RaceResult");
            TumpUiFactory.Anchor(_result, new Vector2(.5f, .5f), new Vector2(0, 30), new Vector2(900, 560));
            var back = TumpUiFactory.Ground(_result, new Color(0.10f, 0.09f, 0.05f, 0.82f), false);
            back.raycastTarget = false;
            _resultTitle = Ink(_result, "Title", "FINISH", 76, true);
            TumpUiFactory.Anchor(_resultTitle.rectTransform, new Vector2(.5f, 1), new Vector2(0, -70), new Vector2(820, 96));
            _resultRows = Ink(_result, "Rows", "", 34, false);
            _resultRows.alignment = TextAnchor.UpperCenter;
            TumpUiFactory.Anchor(_resultRows.rectTransform, new Vector2(.5f, 1), new Vector2(0, -290), new Vector2(820, 320));
            _resultFoot = Ink(_result, "Foot", "R or Enter: run it again", 28, false);
            TumpUiFactory.Anchor(_resultFoot.rectTransform, new Vector2(.5f, 0), new Vector2(0, 44), new Vector2(820, 44));
            _result.gameObject.SetActive(false);
        }

        private static Text Ink(Transform root, string name, string words, int size, bool main)
        {
            var text = TumpUiFactory.Text(root, name, words, size, main, main);
            text.color = TumpUiTheme.Current.Cream; text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, .95f); outline.effectDistance = new Vector2(2, -2);
            return text;
        }

        /// <summary>m:ss.cc</summary>
        public static string Clock(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int centis = Mathf.FloorToInt(seconds * 100f);
            return (centis / 6000) + ":" + ((centis / 100) % 60).ToString("00") + "." + (centis % 100).ToString("00");
        }

        public static string Ordinal(int place)
        {
            int tens = place % 100;
            string end = tens >= 11 && tens <= 13 ? "th" : (place % 10) == 1 ? "st" : (place % 10) == 2 ? "nd" : (place % 10) == 3 ? "rd" : "th";
            return place + end;
        }

        public void Tick(RunMode mode)
        {
            if (!_built || mode == null || mode.Data == null) return;
            var me = mode.Local;
            bool racing = mode.State == RunMode.Phase.Racing || mode.State == RunMode.Phase.Finished;
            int centis = Mathf.FloorToInt(mode.RaceTime * 100f);
            if (centis != _shownCentis) { _shownCentis = centis; _time.text = Clock(mode.RaceTime); }
            if (me == null) return;

            if (me.Node != _shownNode)
            {
                _shownNode = me.Node;
                var node = mode.Data.checkpoints[me.Node];
                _check.text = "CHECKPOINT " + node.main + " / " + mode.MainCheckpoints;
                string words = node.stage ?? "";
                int colon = words.IndexOf(':');
                // The stage's name is the part before its colon ("CHAINED BOULDERS: boulders hung on chains..."); a
                // stage with no colon is a plain sentence and is shown whole.
                _stage.text = node.id == mode.Data.finish ? "" : (node.shortcut >= 0 ? "SHORTCUT  " + node.name : "LEG " + node.leg + "  " + (colon > 0 ? words.Substring(0, colon) : words).ToUpperInvariant());
            }
            if (me.Place != _shownPlace || mode.Racers.Count != _shownCount)
            {
                _shownPlace = me.Place; _shownCount = mode.Racers.Count;
                _place.text = Ordinal(me.Place) + " of " + mode.Racers.Count;
            }
            bool wrong = mode.State == RunMode.Phase.Racing && !me.Returning && me.WrongWay > 1.2f;
            if (_wrong.enabled != wrong) _wrong.enabled = wrong;
            if (_hint.enabled != (mode.State == RunMode.Phase.Racing)) _hint.enabled = mode.State == RunMode.Phase.Racing;
            if (_check.enabled != racing) { _check.enabled = racing; _place.enabled = racing; }

            bool done = mode.State == RunMode.Phase.Finished;
            if (_result.gameObject.activeSelf != done)
            {
                _result.gameObject.SetActive(done);
                if (done) WriteResult(mode);
            }
        }

        private void WriteResult(RunMode mode)
        {
            _text.Clear();
            for (int place = 1; place <= mode.Racers.Count; place++)
                foreach (var r in mode.Racers)
                {
                    if (r.Place != place) continue;
                    string who = r.Body != null ? r.Body.DisplayName() : "?";
                    _text.Append(Ordinal(place)).Append("   ").Append(who).Append("   ");
                    _text.Append(r.Finished ? Clock(r.FinishTime) : "still running");
                    _text.Append("   ").Append(r.Falls).Append(r.Falls == 1 ? " fall" : " falls").Append('\n');
                }
            _resultRows.text = _text.ToString();
        }
    }
}
