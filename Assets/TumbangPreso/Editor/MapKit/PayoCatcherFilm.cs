using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TumbangPreso.Map;
using TumbangPreso.Visual;
using UnityEditor;
using UnityEngine;

namespace TumbangPreso.EditorTools.MapKit
{
    /// <summary>
    /// THE FALL CATCHER (KANLIT), FILMED AND MEASURED, inside `probe fall` (`PayoPlayProbe.FallChecks` calls `Side`, `Tick` each
    /// frame, `EndSide` and `Finish`). Writes to `Logs/payo/unity/catcher/`:
    ///   * the first side (north): every 1/20 s of game time from the catch to 2.2 s after the set-down, from a camera
    ///     that tracks the body side-on; `strip.png` is every other one of them (0.1 s apart, read left to right, top to
    ///     bottom, times in `strip.txt`), `onion_*.png` four consecutive frames (0.05 s apart) tinted red, yellow, green,
    ///     blue and overlaid for the rise, the gulp, the carry and the pop, and four close stills (`still_*.png`);
    ///   * the second and third sides: the cost, in `cost.txt`: the spirit's own CPU time a frame (its `Ticks`, measured
    ///     with a stopwatch round its code), the frame time, and the editor's render counts (`UnityStats`) during the
    ///     effect against the frames of the same fall before the catch;
    ///   * the fourth side: the same at night (`PayoNight.Hold` 1), two stills.
    /// ⚠️ Probe tooling only: it moves the game camera for one render and puts it back, as the probe's own `Picture` does.
    /// </summary>
    internal static class PayoCatcherFilm
    {
        private const string Folder = "Logs/payo/unity/catcher";
        private const int FrameW = 400, FrameH = 225, StillW = 1280, StillH = 720;
        private const float Step = 0.05f, After = 2.2f;

        private enum Mode { None, Film, Cost, Night }

        private sealed class Frame { public float T; public Texture2D Tex; }

        private static Mode _mode;
        private static CharacterMotor _who;
        private static Camera _cam;
        private static bool _caught, _ended;
        private static float _t0, _tEnd, _next, _sideStart;
        private static Vector3 _side;
        private static readonly List<Frame> _frames = new List<Frame>();
        private static readonly HashSet<string> _stillsTaken = new HashSet<string>();
        private static readonly StringBuilder _log = new StringBuilder();
        // Cost: per frame, before the catch and while the spirit is on screen.
        private static long _ticksAtLast;
        private static int _costFrame;
        private static readonly List<float> _cpuOn = new List<float>(), _frameOn = new List<float>(), _frameOff = new List<float>();
        private static readonly List<int> _batchOn = new List<int>(), _batchOff = new List<int>(), _passOn = new List<int>(), _passOff = new List<int>(), _triOn = new List<int>(), _triOff = new List<int>();
        private static PayoNight _night;

        public static void Side(int index, string name, CharacterMotor who, Camera cam)
        {
            EndSide();
            _mode = index == 1 ? Mode.Film : index == 4 ? Mode.Night : Mode.Cost;
            _who = who; _cam = cam; _caught = false; _ended = false; _stillsTaken.Clear();
            _sideStart = Time.time; _ticksAtLast = PayoKanlit.Ticks;
            if (index == 1) { foreach (var f in _frames) Object.Destroy(f.Tex); _frames.Clear(); _log.Clear(); _cpuOn.Clear(); _frameOn.Clear(); _frameOff.Clear(); _batchOn.Clear(); _batchOff.Clear(); _passOn.Clear(); _passOff.Clear(); _triOn.Clear(); _triOff.Clear(); }
            if (_mode == Mode.Night)
            {
                _night = Object.FindAnyObjectByType<PayoNight>();
                if (_night != null) _night.Hold = 1f;
            }
            _log.AppendLine($"side {index} ({name}): {_mode}{(_mode == Mode.Night ? (_night != null ? ", night held at 1" : ", NO PayoNight to hold") : "")}");
        }

        public static void EndSide()
        {
            WorldOutline.FineDepthView = 0; WorldOutline.FineDepthTest = -1;
            PayoKanlit.HideForMeasure = false;
            if (_night != null) { _night.Hold = -1f; _night = null; }
            _mode = Mode.None; _who = null;
        }

        private static float Since => Time.time - _t0;

        public static void Tick()
        {
            if (_mode == Mode.None || _who == null) return;
            long ticks = PayoKanlit.Ticks;
            float cpu = (float)((ticks - _ticksAtLast) * 1000.0 / System.Diagnostics.Stopwatch.Frequency);
            _ticksAtLast = ticks;
            if (!_caught && _who.IsEdgeRecovering)
            {
                _caught = true; _t0 = Time.time; _next = _t0;
                var outward = _who.EdgeOutward.sqrMagnitude > 0.5f ? _who.EdgeOutward : Vector3.back;
                _side = Vector3.Cross(Vector3.up, outward).normalized;
            }
            if (_caught && !_ended && !_who.IsEdgeRecovering) { _ended = true; _tEnd = Time.time; }
            var spirit = Object.FindAnyObjectByType<PayoKanlit>();
            bool on = spirit != null && spirit.Busy;

            if (_mode == Mode.Cost)
            {
                // While the spirit is on screen its renderers are switched off for four frames in every eight (everything
                // else runs): neighbouring frames see the same scene, so the difference is the spirit's own draw cost.
                // The first frame after each switch is not counted (the counts are the frame before's).
                if (!_caught || !on) { PayoKanlit.HideForMeasure = false; _costFrame = 0; return; }
                _cpuOn.Add(cpu);
                bool hidden = PayoKanlit.HideForMeasure;
                if (_costFrame % 4 != 0)
                {
                    if (hidden) { _frameOff.Add(Time.unscaledDeltaTime * 1000f); _batchOff.Add(UnityStats.drawCalls); _passOff.Add(UnityStats.setPassCalls); _triOff.Add(UnityStats.triangles); }
                    else { _frameOn.Add(Time.unscaledDeltaTime * 1000f); _batchOn.Add(UnityStats.drawCalls); _passOn.Add(UnityStats.setPassCalls); _triOn.Add(UnityStats.triangles); }
                }
                _costFrame++;
                if (_costFrame % 4 == 0) PayoKanlit.HideForMeasure = !hidden;
                return;
            }
            if (!_caught) return;
            float s = Since;
            if (_mode == Mode.Film)
            {
                if (Time.time >= _next && (!_ended || Time.time - _tEnd < After))
                {
                    var tex = Render(FrameW, FrameH, 10f, 2.4f, 42f);
                    if (tex != null) _frames.Add(new Frame { T = s, Tex = tex });
                    _next += Step;
                    while (_next < Time.time) _next += Step;
                }
                Still("wrap", !_ended && s >= 0.6f, 5.8f, 1.2f, 40f);
                Still("carry", !_ended && s >= 1.9f, 6.5f, 1.6f, 40f);
                // The AO's depth, from the same place (owner, 2026-10-18: striping on Payo's AO): the old depth, the
                // fine one, where they agree, and each as a ramp.
                if (!_ended && s >= 2.0f && !_stillsTaken.Contains("ao_old"))
                {
                    WorldOutline.FineDepthTest = 0; Still("ao_old", true, 6.5f, 1.6f, 40f);
                    WorldOutline.FineDepthTest = 1; Still("ao_fine", true, 6.5f, 1.6f, 40f);
                    WorldOutline.FineDepthView = 1; Still("ao_agree", true, 6.5f, 1.6f, 40f);
                    WorldOutline.FineDepthView = 2; Still("ao_ramp_fine", true, 6.5f, 1.6f, 40f);
                    WorldOutline.FineDepthView = 3; Still("ao_ramp_old", true, 6.5f, 1.6f, 40f);
                    WorldOutline.FineDepthView = 0; WorldOutline.FineDepthTest = -1;
                }
                // Close to the deck, as a player sees it (the stripes showed there): the old depth and the fine one.
                if (_ended && Time.time - _tEnd >= 0.5f && !_stillsTaken.Contains("floor_old"))
                {
                    _stillsTaken.Add("floor_old");
                    var feet = _who.transform.position;
                    var ahead = _side.sqrMagnitude > 0.5f ? Vector3.Cross(_side, Vector3.up).normalized : Vector3.forward;
                    var eye = feet - ahead * 1.2f + Vector3.up * 1.7f; var look = feet + ahead * 1.4f;
                    WorldOutline.FineDepthTest = 0; SaveAt("floor_old", eye, look);
                    WorldOutline.FineDepthTest = 1; SaveAt("floor_fine", eye, look);
                    WorldOutline.FineDepthTest = -1;
                }
                Still("setdown", !_ended && s >= 3.2f, 6.5f, 1.6f, 40f);
                Still("letgo", _ended && Time.time - _tEnd >= 0.07f, 6.5f, 1.6f, 44f);
                Still("hop", _ended && Time.time - _tEnd >= 0.35f, 7f, 1.8f, 44f);
                Still("away", _ended && Time.time - _tEnd >= 1.1f, 9f, 1.0f, 46f);
            }
            else if (_mode == Mode.Night)
            {
                Still("night_carry", !_ended && s >= 1.9f, 6.5f, 1.6f, 40f);
                Still("night_letgo", _ended && Time.time - _tEnd >= 0.07f, 6.5f, 1.6f, 44f);
            }
        }

        private static void Still(string name, bool now, float distance, float lift, float fov)
        {
            if (!now || _stillsTaken.Contains(name)) return;
            _stillsTaken.Add(name);
            var tex = Render(StillW, StillH, distance, lift, fov);
            if (tex == null) return;
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Folder + "/still_" + name + ".png", tex.EncodeToPNG());
            Object.Destroy(tex);
            _log.AppendLine($"  still_{name}.png at {(_ended ? "set-down + " + (Time.time - _tEnd).ToString("0.00", CultureInfo.InvariantCulture) : "catch + " + Since.ToString("0.00", CultureInfo.InvariantCulture))} s");
        }

        // The game camera, side-on to the carry, tracking the body, for one render.
        private static void SaveAt(string name, Vector3 eye, Vector3 look)
        {
            if (_cam == null) return;
            var savedPos = _cam.transform.position; var savedRot = _cam.transform.rotation; float savedFov = _cam.fieldOfView;
            var rt = RenderTexture.GetTemporary(StillW, StillH, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _cam.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(look - eye));
            _cam.fieldOfView = 60f;
            var keep = _cam.targetTexture;
            _cam.targetTexture = rt; _cam.Render(); _cam.targetTexture = keep;
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(StillW, StillH, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, StillW, StillH), 0, 0); tex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            _cam.transform.SetPositionAndRotation(savedPos, savedRot); _cam.fieldOfView = savedFov;
            Directory.CreateDirectory(Folder);
            File.WriteAllBytes(Folder + "/still_" + name + ".png", tex.EncodeToPNG());
            Object.Destroy(tex);
        }

        private static Texture2D Render(int w, int h, float distance, float lift, float fov)
        {
            if (_cam == null || _who == null) return null;
            var target = _who.transform.position + Vector3.up * 1.5f;   // between the body and Kanlit over it
            // From the watcher's side (the spirit shows its face to the game camera), turned 25 degrees so its profile reads.
            var bearing = _cam.transform.position - target; bearing.y = 0f;
            bearing = bearing.sqrMagnitude > 0.01f ? Quaternion.Euler(0f, 25f, 0f) * bearing.normalized : _side;
            // Down in the shaft the watcher's side is a wall: there the camera looks along the open gap, side-on.
            if (_who.transform.position.y < -1.5f) bearing = _side;
            var at = target + bearing * distance + Vector3.up * lift;
            var savedPos = _cam.transform.position; var savedRot = _cam.transform.rotation; float savedFov = _cam.fieldOfView;
            var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _cam.transform.SetPositionAndRotation(at, Quaternion.LookRotation(target - at));
            _cam.fieldOfView = fov;
            var keep = _cam.targetTexture;
            _cam.targetTexture = rt; _cam.Render(); _cam.targetTexture = keep;
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            _cam.transform.SetPositionAndRotation(savedPos, savedRot); _cam.fieldOfView = savedFov;
            return tex;
        }

        public static string Finish()
        {
            EndSide();
            Directory.CreateDirectory(Folder);
            var said = new StringBuilder("KANLIT FILM AND COST (Logs/payo/unity/catcher):\n");
            said.Append(_log);
            if (_frames.Count > 0)
            {
                // The strip: every other frame, 0.1 s apart, eight to a row.
                var pick = new List<Frame>();
                for (int i = 0; i < _frames.Count; i += 2) pick.Add(_frames[i]);
                const int Cols = 8;
                int rows = (pick.Count + Cols - 1) / Cols;
                var strip = new Texture2D(Cols * FrameW, rows * FrameH, TextureFormat.RGB24, false);
                var clear = new Color32[strip.width * strip.height];
                for (int i = 0; i < clear.Length; i++) clear[i] = new Color32(24, 24, 28, 255);
                strip.SetPixels32(clear);
                var times = new StringBuilder("cell (row, column from top left): seconds after the catch\n");
                for (int i = 0; i < pick.Count; i++)
                {
                    int r = i / Cols, c = i % Cols;
                    strip.SetPixels(c * FrameW, (rows - 1 - r) * FrameH, FrameW, FrameH, pick[i].Tex.GetPixels());
                    times.AppendLine($"({r + 1}, {c + 1}): {pick[i].T.ToString("0.00", CultureInfo.InvariantCulture)}");
                }
                strip.Apply();
                File.WriteAllBytes(Folder + "/strip.png", strip.EncodeToPNG());
                File.WriteAllText(Folder + "/strip.txt", times.ToString());
                Object.Destroy(strip);
                said.AppendLine($"  strip.png: {pick.Count} frames 0.1 s apart from the catch ({_frames.Count} captured at {Step:0.00} s)");
                float end = _tEnd - _t0;
                Onion("dive", 0.12f); Onion("wrap", 0.35f); Onion("carry", 2.0f); Onion("letgo", end > 0f ? end + 0.05f : 3.55f); Onion("hop", end > 0f ? end + 0.45f : 4.0f);
                said.AppendLine("  onion_dive/wrap/carry/letgo/hop.png: four consecutive frames 0.05 s apart, tinted red, yellow, green, blue, overlaid");
                foreach (var f in _frames) Object.Destroy(f.Tex);
                _frames.Clear();
            }
            else said.AppendLine("  NO FRAMES were captured (the north side was not caught, or not tried)");

            var cost = new StringBuilder();
            cost.AppendLine("KANLIT'S COST, MEASURED in Play in the open editor (probe fall, sides 2 and 3), " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
            cost.AppendLine($"frames measured during the carries and pops: {_frameOn.Count} with Kanlit drawn, {_frameOff.Count} with its renderers switched off (alternating every four frames, same scene)");
            cost.AppendLine($"Kanlit CPU a frame (its Update + LateUpdate, stopwatch): mean {Mean(_cpuOn):0.000} ms, 95th percentile {Pct(_cpuOn, 0.95f):0.000} ms, max {Max(_cpuOn):0.000} ms");
            cost.AppendLine($"frame time (editor, unscaled dt): drawn {Mean(_frameOn):0.00} ms, not drawn {Mean(_frameOff):0.00} ms (editor frame times are noisy; the probe also runs every frame)");
            cost.AppendLine($"draw calls: not drawn {MeanI(_batchOff):0.0}, drawn {MeanI(_batchOn):0.0} (delta {MeanI(_batchOn) - MeanI(_batchOff):+0.0;-0.0})");
            cost.AppendLine($"set-pass calls: not drawn {MeanI(_passOff):0.0}, drawn {MeanI(_passOn):0.0} (delta {MeanI(_passOn) - MeanI(_passOff):+0.0;-0.0})");
            cost.AppendLine($"triangles: not drawn {MeanI(_triOff):0}, drawn {MeanI(_triOn):0} (delta {MeanI(_triOn) - MeanI(_triOff):+0;-0})");
            cost.AppendLine("  (the game view renders the scene with shadows and the ink pass, so a draw can count more than once)");
            int buddyTris = Tris(PayoKanlitMeshes.Buddy), vineTris = Tris(PayoKanlitMeshes.Vine);
            cost.AppendLine($"by construction: Kanlit {buddyTris} triangles x 1 pass (no ink line; the two hidden faces fold to a point), vine {vineTris} x 1 pass (its ungrown part folds to the path); 2 shared materials, 0 per-frame allocations, 0 vertex uploads; per frame: 2 transforms placed, 2 property-block writes");
            cost.AppendLine("the puffs are the game's existing CloudPuffs field (its own 2 draws while puffs live)");
            File.WriteAllText(Folder + "/cost.txt", cost.ToString());
            said.Append(cost);
            return said.ToString().TrimEnd();
        }

        private static int Tris(Mesh m) { int n = 0; for (int i = 0; i < m.subMeshCount; i++) n += (int)m.GetIndexCount(i) / 3; return n; }

        private static void Onion(string name, float at)
        {
            int first = -1;
            for (int i = 0; i < _frames.Count; i++) if (_frames[i].T >= at) { first = i; break; }
            if (first < 0 || first + 3 >= _frames.Count) return;
            Color[] tints = { new Color(1f, 0.55f, 0.55f), new Color(1f, 1f, 0.5f), new Color(0.55f, 1f, 0.55f), new Color(0.55f, 0.65f, 1f) };
            var sum = new Color[FrameW * FrameH];
            var row = new Texture2D(FrameW * 4, FrameH, TextureFormat.RGB24, false);
            for (int k = 0; k < 4; k++)
            {
                var px = _frames[first + k].Tex.GetPixels();
                row.SetPixels(k * FrameW, 0, FrameW, FrameH, px);
                for (int i = 0; i < px.Length; i++) sum[i] += px[i] * tints[k] * 0.32f;
            }
            var onion = new Texture2D(FrameW, FrameH, TextureFormat.RGB24, false);
            onion.SetPixels(sum); onion.Apply(); row.Apply();
            // The overlay and, under it, the four frames side by side.
            var both = new Texture2D(FrameW * 4, FrameH * 2, TextureFormat.RGB24, false);
            var fill = new Color[both.width * both.height];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color(0.1f, 0.1f, 0.11f);
            both.SetPixels(fill);
            both.SetPixels(0, 0, FrameW * 4, FrameH, row.GetPixels());
            both.SetPixels(FrameW * 3 / 2, FrameH, FrameW, FrameH, onion.GetPixels());
            both.Apply();
            File.WriteAllBytes(Folder + "/onion_" + name + ".png", both.EncodeToPNG());
            Object.Destroy(onion); Object.Destroy(row); Object.Destroy(both);
        }

        private static float Mean(List<float> v) { if (v.Count == 0) return 0f; double s = 0; foreach (var x in v) s += x; return (float)(s / v.Count); }
        private static float MeanI(List<int> v) { if (v.Count == 0) return 0f; double s = 0; foreach (var x in v) s += x; return (float)(s / v.Count); }
        private static float Max(List<float> v) { float m = 0f; foreach (var x in v) m = Mathf.Max(m, x); return m; }
        private static float Pct(List<float> v, float p) { if (v.Count == 0) return 0f; var c = new List<float>(v); c.Sort(); return c[Mathf.Clamp((int)(p * (c.Count - 1)), 0, c.Count - 1)]; }
    }
}
