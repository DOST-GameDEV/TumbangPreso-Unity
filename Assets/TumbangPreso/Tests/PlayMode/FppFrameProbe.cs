using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// Names every object the first-person camera can actually see, and where in the frame it
    /// sits, to `Logs/fpp-frame.txt`.
    ///
    /// ⚠️⚠️ IT EXISTS BECAUSE A SCREENSHOT CANNOT NAME A MESH. 🧑 2026-08-16, on an arena
    /// capture: *"look the hands are floating this still isnt good"* — and there IS a third
    /// shape in the bottom-left corner of the port's frame that is not in
    /// `Logs/shots-godot/g04-ready.png`, which shows exactly two forearms and nothing else.
    /// Reasoning about what it might be from the pixels is how this port has repeatedly fixed
    /// the wrong thing: the viewmodel arms, the self-hidden body, a neighbouring seat and a
    /// dropped tsinelas are four different objects that all render as a pale slab with an ink
    /// border at this size.
    ///
    /// So: ask the camera. Every renderer whose bounds are in front of the near plane and
    /// inside the frustum gets one line with its FULL hierarchy path, its viewport rect, its
    /// world size and its shadow-casting mode — the last because the first-person self-hide is
    /// `ShadowsOnly` rather than disabled, so "hidden" and "not there" look identical to every
    /// other check.
    ///
    /// ⚠️ IT ASSERTS ALMOST NOTHING. The value is the list. The one thing it does assert is the
    /// property the report is about: that nothing unexpected is drawing in the bottom corners,
    /// where the arms live.
    /// </summary>
    public class FppFrameProbe
    {
        private const string OutPath = "Logs/fpp-frame.txt";

        [UnityTest]
        public IEnumerator NothingUnexpectedDrawsInTheFirstPersonFrame()
        {
            var load = SceneManager.LoadSceneAsync("Eskinita", LoadSceneMode.Single);
            while (load != null && !load.isDone) yield return null;

            for (int i = 0; i < 30; i++) yield return null;

            var rig = Object.FindFirstObjectByType<CameraSystem.CameraRig>();
            Assert.IsNotNull(rig, "no camera rig in the arena");

            var cam = rig.Camera;
            Assert.IsNotNull(cam, "the rig has no camera");

            var lines = new StringBuilder();
            lines.AppendLine("Everything the FPP camera can see, nearest first.");
            lines.AppendLine();
            lines.AppendLine($"camera at {cam.transform.position}, fov {cam.fieldOfView}, " +
                             $"near {cam.nearClipPlane}");
            lines.AppendLine($"following {(rig.Following != null ? rig.Following.name : "nothing")}");
            lines.AppendLine();

            // ⚠️ WHERE EVERYBODY IS STANDING, because "somebody is in my face" is a SPAWN
            // question before it is a rendering one. The three attackers share one line at
            // `AttackerSpawnSpacing`, so any pair closer than that has been moved by something.
            lines.AppendLine("seats:");

            var seats = new List<CharacterMotor>();

            foreach (var m in Object.FindObjectsByType<CharacterMotor>(FindObjectsSortMode.None))
                if (m.IsPerson) seats.Add(m);

            seats.Sort((a, b) => a.PlayerSlot.CompareTo(b.PlayerSlot));

            foreach (var m in seats)
            {
                lines.AppendLine($"  slot {m.PlayerSlot} {(m.IsDefender ? "TAYA    " : "attacker")} " +
                                 $"{m.name,-8} at {m.transform.position.ToString("0.00")}");
            }

            for (int i = 0; i < seats.Count; i++)
            {
                for (int j = i + 1; j < seats.Count; j++)
                {
                    float d = Vector3.Distance(seats[i].transform.position,
                                               seats[j].transform.position);

                    lines.AppendLine($"  slot {seats[i].PlayerSlot} to slot {seats[j].PlayerSlot}: " +
                                     $"{d:0.00} m");
                }
            }

            lines.AppendLine();
            lines.AppendLine($"{"dist",6} {"vx",6} {"vy",6} {"size",22} {"shadow",12}  path");
            lines.AppendLine(new string('-', 128));

            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            var seen = new List<(float dist, string line, string path, Vector3 view)>();

            foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,
                                                                 FindObjectsSortMode.None))
            {
                if (r == null || !r.enabled) continue;
                if (!GeometryUtility.TestPlanesAABB(planes, r.bounds)) continue;

                Vector3 view = cam.WorldToViewportPoint(r.bounds.center);
                if (view.z <= 0.0f) continue;

                // Only things close enough to BE the viewmodel or the player's own body. The
                // street behind them is not what the report is about and would bury the list.
                if (view.z > 6.0f) continue;

                string path = Path(r.transform);

                seen.Add((view.z,
                          $"{view.z,6:0.00} {view.x,6:0.00} {view.y,6:0.00} " +
                          $"{r.bounds.size.ToString("0.00"),22} " +
                          $"{r.shadowCastingMode,12}  {path}",
                          path,
                          view));
            }

            seen.Sort((a, b) => a.dist.CompareTo(b.dist));

            foreach (var s in seen) lines.AppendLine(s.line);

            Directory.CreateDirectory("Logs");
            File.WriteAllText(OutPath, lines.ToString());

            Debug.Log($"[FppFrame] wrote {OutPath}\n{lines}");

            // ⚠️⚠️ THE ASSERTION IS NARROW ON PURPOSE, AND ITS FIRST VERSION WAS NOT. It flagged
            // the ROAD — `Kalsada_109` at 0.55 m, which is the ground under the player's feet on
            // a 95° lens — and a neighbouring seat whose bounds CENTRE projects to viewport
            // x = -1.19, a full screen width off the left edge.
            //
            // ⚠️ AND A SKINNED RENDERER'S `bounds` ARE ITS BIND POSE, NOT ITS CURRENT ONE. These
            // rigs bind arms-out, so `body-mesh` reports 1.90 m wide against a 1.80 m spawn
            // spacing — every seat's box overlaps its neighbour's whatever the animation is
            // actually doing. `TestPlanesAABB` against that box says "possibly visible" for a
            // body that is drawn well outside the frame, so the frustum test alone cannot
            // conclude anything about what a player SEES.
            //
            // So this asserts the one thing that is unambiguous: nothing may be drawn whose
            // centre is ON SCREEN and within arm's reach of the eye, other than the viewmodel.
            // Everything else is a dump for a human to read.
            var strangers = new List<string>();

            foreach (var s in seen)
            {
                if (s.dist > 0.6f) continue;
                if (s.view.x < 0.0f || s.view.x > 1.0f) continue;
                if (s.view.y < 0.0f || s.view.y > 1.0f) continue;
                if (s.path.Contains("Viewmodel") || s.path.Contains("HeldSlipper")) continue;

                // The floor is allowed to be under your feet.
                if (s.path.Contains("Kalsada") || s.path.Contains("Markings")
                    || s.path.Contains("Floor") || s.path.Contains("Slab")) continue;

                strangers.Add($"{s.path} at viewport {s.view.x:0.00},{s.view.y:0.00}, " +
                              $"{s.dist:0.00} m from the eye");
            }

            Assert.IsEmpty(strangers,
                "something that is not part of the viewmodel is drawing in the player's face:\n  "
                + string.Join("\n  ", strangers)
                + "\nRead Logs/fpp-frame.txt for the whole frame.");
        }

        private static string Path(Transform t)
        {
            var parts = new List<string>();

            for (var step = t; step != null; step = step.parent) parts.Add(step.name);

            parts.Reverse();
            return string.Join("/", parts);
        }
    }
}
