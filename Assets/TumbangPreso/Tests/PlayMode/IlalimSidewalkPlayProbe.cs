using System;
using System.Collections;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// THE SIDEWALK PEOPLE'S ARMS IN REAL PLAY (owner 2026-10-01: "the feet are moving but the
    /// hands arent", seen in Play). The builder's probe and films step `SidewalkLife.Simulate` by
    /// hand outside Play; this enters Play on the Ilalim rebuild (the shipped IlalimNgTulay scene), lets the life run on its
    /// own `Update` for 25 s, and reads every walker's limbs in a LateUpdate at execution order
    /// 32000 (after the animation system and every other LateUpdate; WaitForEndOfFrame never fires
    /// in batch mode), so whatever an Animator wrote would be in it):
    /// each arm's swing, the left arm against the left leg (opposite phase reads near -1), and the
    /// planted sole's slip. It also checks that no rig Animator under the life is enabled.
    /// Writes Logs/ilalim-unity/videos_v4/play_arms.txt.
    /// </summary>
    [Category("WallClock")]
    public sealed class IlalimSidewalkPlayProbe
    {
        // The rebuild IS the shipped Ilalim since ILALIM-1.6 (2026-10-01).
        private const string Scene = "Assets/TumbangPreso/Scenes/Maps/IlalimNgTulay.unity";
        private const string Report = "Logs/ilalim-unity/videos_v4/play_arms.txt";

        [UnityTest, Timeout(240000)]
        public IEnumerator SidewalkWalkersSwingTheirArmsInPlay()
        {
#if UNITY_EDITOR
            LogAssert.ignoreFailingMessages = true;
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(Scene, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            var life = Object.FindFirstObjectByType<SidewalkLife>();
            Assert.IsNotNull(life, "No SidewalkLife in " + Scene);
            Time.timeScale = 1f;
            yield return null;
            int n = life.PeopleCount;
            var frames = new int[n]; var lo = new float[n]; var hi = new float[n];
            var sx = new double[n]; var sy = new double[n]; var sxx = new double[n]; var syy = new double[n]; var sxy = new double[n];
            var slip = new double[n]; var slipN = new int[n]; var lastSole = new Vector3[n]; var lastLeft = new bool[n]; var had = new bool[n];
            var lastPos = new Vector3[n];
            for (int i = 0; i < n; i++) { lo[i] = float.MaxValue; hi[i] = float.MinValue; }
            int animatorsOn = 0, allFrames = 0;
            float clockFrom = life.Clock;
            // Read in a LateUpdate that runs after every other one (and after the animation
            // system): WaitForEndOfFrame never fires in batch mode.
            var recorder = new GameObject("Sidewalk arm recorder").AddComponent<LateRecorder>();
            recorder.OnLate = dt =>
            {
                allFrames++;
                foreach (var a in life.GetComponentsInChildren<Animator>(true)) if (a.enabled) animatorsOn++;
                for (int i = 0; i < n; i++)
                {
                    if (!life.PersonShown(i)) { had[i] = false; continue; }
                    var p = life.PersonPosition(i);
                    bool walking = life.PersonLocomotion(i) > .95f && life.PersonSpeed(i) > .3f && life.PersonStepping(i);
                    if (walking && life.PersonLimbs(i, out float al, out float ar, out float ll, out float lr))
                    {
                        frames[i]++; lo[i] = Mathf.Min(lo[i], al); hi[i] = Mathf.Max(hi[i], al);
                        sx[i] += al; sy[i] += ll; sxx[i] += al * al; syy[i] += ll * ll; sxy[i] += al * ll;
                    }
                    if (life.PersonSole(i, out var sole, out bool left))
                    {
                        var go = p - lastPos[i]; go.y = 0f;
                        if (walking && had[i] && left == lastLeft[i] && go.sqrMagnitude > 1e-8f && dt > 0f)
                        {
                            var v = (sole - lastSole[i]) / dt; v.y = 0f;
                            slip[i] += Vector3.Dot(v, go.normalized); slipN[i]++;
                        }
                        lastSole[i] = sole; lastLeft[i] = left; had[i] = walking;
                    }
                    lastPos[i] = p;
                }
            };
            float until = Time.realtimeSinceStartup + 25f;
            while (Time.realtimeSinceStartup < until) { Time.timeScale = 1f; yield return null; }
            recorder.OnLate = null;
            var sb = new StringBuilder("ILALIM SIDEWALK LIFE IN PLAY (EditorSceneManager.LoadSceneAsyncInPlayMode, SidewalkLife's own Update, read in a LateUpdate at execution order 32000, after the animation system)\n");
            sb.AppendLine(FormattableString.Invariant($"Frames: {allFrames} over 25 s of play; the life's clock ran {life.Clock - clockFrom:F1} s. Rig Animators found enabled (summed over frames): {animatorsOn}."));
            Object.Destroy(recorder.gameObject);
            int good = 0;
            for (int i = 0; i < n; i++)
            {
                if (frames[i] < 2) { sb.AppendLine($"  {life.PersonName(i),-12} {life.PersonGait(i),-12} no walking frames"); continue; }
                double mx = sx[i] / frames[i], my = sy[i] / frames[i];
                double vx = sxx[i] / frames[i] - mx * mx, vy = syy[i] / frames[i] - my * my, cov = sxy[i] / frames[i] - mx * my;
                double corr = vx > 1e-9 && vy > 1e-9 ? cov / Math.Sqrt(vx * vy) : 0;
                if (frames[i] >= 60 && corr < -.5 && hi[i] - lo[i] > 15f) good++;
                sb.AppendLine(FormattableString.Invariant($"  {life.PersonName(i),-12} {life.PersonGait(i),-12} {frames[i],5} walking frames, left arm {lo[i]:+0;-0}..{hi[i]:+0;-0} deg, left arm vs left leg {corr:+0.00;-0.00}, planted sole along travel {(slipN[i] > 0 ? slip[i] / slipN[i] : 0):+0.00;-0.00} m/s"));
            }
            sb.AppendLine($"Walkers swinging their arms in opposite phase (60+ frames, correlation under -0.5, 15+ degrees of swing): {good}.");
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, sb.ToString());
            Debug.Log(sb.ToString());
            Assert.AreEqual(0, animatorsOn, "A rig Animator under the sidewalk life is enabled: it would write the idle over the drawn arms.");
            Assert.Greater(good, 0, "No sidewalk walker swung its arms in opposite phase in Play:\n" + sb);
#else
            Assert.Ignore("Editor only: loads the sample scene by path.");
            yield break;
#endif
        }
    }

    /// <summary>Marks each frame once every LateUpdate (and the animation system) has run.</summary>
    [DefaultExecutionOrder(32000)]
    internal sealed class LateRecorder : MonoBehaviour
    {
        public Action<float> OnLate;
        private void LateUpdate() => OnLate?.Invoke(Time.deltaTime);
    }
}
