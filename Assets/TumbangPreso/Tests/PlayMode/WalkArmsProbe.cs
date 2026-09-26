using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// ⚠️⚠️ EVERY BODY WALKING, FROM THE FRONT AND THE SIDE, WITH THE GAP BETWEEN EACH HAND AND THE HIP MEASURED.
    ///
    /// Owner, 2026-09-26: *"the walkingh animation looks so weird, hands close to body"* (ASKS-0926). REFINE-2.9c filmed
    /// every body SPRINTING front-on (`CastAndMotionReel`), never walking, and `LocomotionArmsProbe` photographs one body
    /// side-on, which is the one view in which a hand pressed to the hip cannot be seen. This walks each roster body at
    /// the lens and past it, at a fixed 30 frames per game second (`Time.captureFramerate`, so a slow software renderer
    /// films the same gait as a fast machine), and measures, every frame, how far the innermost point of each HAND sits
    /// outside the outermost point of the body (torso, hips, legs) at the same height, in centimetres, in the
    /// character's own frame. Negative means the hand is inside the body's silhouette seen from the front.
    ///
    /// Geometry is read off the visible skin the way `CharacterVisual.PalmCentre` reads it: each vertex follows its
    /// dominant bone, `bone.localToWorld * bindpose * v`, which is exact for these rigidly skinned voxel bodies.
    /// Runs only with TUMP_WALK_FILM=1; pictures and `gaps.csv` under `$TUMP_EVIDENCE/walk-arms` (default Logs).
    /// `$TUMP_WALK_TAG` versions the filenames (chat clients cache by name).
    /// </summary>
    public sealed class WalkArmsProbe
    {
        private int _seat; private bool _spectator, _bots;

        [UnitySetUp] public IEnumerator Before() { _seat = GameLaunch.SoloSeat; _spectator = GameLaunch.Spectator; _bots = GameLaunch.AllBots; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { yield return PlayModeWorld.Reset(); GameLaunch.SoloSeat = _seat; GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _bots; }

        private const int Tile = 400, Settle = 24, Shots = 4, ShotEvery = 5;

        [UnityTest, Timeout(1800000)]
        public IEnumerator EveryBodyWalksWithItsHandsClearOfItsHips()
        {
            if (Environment.GetEnvironmentVariable("TUMP_WALK_FILM") != "1") { Assert.Ignore("A film: set TUMP_WALK_FILM=1."); yield break; }
            string root = Environment.GetEnvironmentVariable("TUMP_EVIDENCE");
            string output = Path.Combine(string.IsNullOrEmpty(root) ? "Logs" : root, "walk-arms");
            string tag = Environment.GetEnvironmentVariable("TUMP_WALK_TAG") ?? "v1";
            Directory.CreateDirectory(output);

            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.SoloSeat = 1; GameLaunch.Spectator = false; GameLaunch.AllBots = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSecondsRealtime(.5f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            yield return new WaitForSecondsRealtime(.3f);
            var who = GameServices.Round.PlayerAt(1);
            who.IsBot = true;
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            foreach (var p in GameServices.Round.Players) if (p != who) p.Teleport(new Vector3(20 + p.PlayerSlot * 3, .2f, -20));
            // Empty-handed: the carry pose holds the right arm on purpose and is not the question here.
            var carrier = who.GetComponent<Carrier>();
            if (carrier.Held != null) foreach (var r in carrier.Held.GetComponentsInChildren<Renderer>()) r.enabled = false;

            var witness = new GameObject("Walk witness").AddComponent<Camera>();
            witness.CopyFrom(Camera.main); witness.enabled = false; witness.tag = "Untagged";
            witness.cullingMask &= ~(1 << 5); witness.fieldOfView = 34;
            witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var rt = new RenderTexture(Tile, Tile, 24);
            var sheet = new Texture2D(Tile * Shots, Tile * 2, TextureFormat.RGB24, false);
            var animator = who.GetComponent<CharacterAnimator>();
            var late = who.gameObject.AddComponent<LateHook>();
            var book = RosterBook.Load();
            var report = new StringBuilder("mode,body,frames,speed,leftGapMinCm,rightGapMinCm,leftGapAtHipCm,rightGapAtHipCm,armSwingAmountMin,leftSpread,leftShiftCm,rightSpread,rightShiftCm\n");
            int previousRate = Time.captureFramerate;
            Time.captureFramerate = 30;
            int filmed = 0;
            try
            {
                foreach (var mode in new[] { GameMode.HeroStrike, GameMode.Classic })
                {
                    var people = Roster.GetPeople(mode);
                    for (int index = 0; index < people.Count; index++)
                    {
                        var id = people[index].Id;
                        var entry = book.People.FirstOrDefault(p => p.Id == id);
                        if (entry == null) continue;
                        who.CharacterIndex = index;
                        who.GetComponent<CharacterVisual>().ApplyModel(entry.Model, entry.Tint, entry.Clips, entry.Palette, entry.PetModel);
                        yield return null; yield return null;

                        who.Teleport(new Vector3(-6, .2f, -14)); who.transform.rotation = Quaternion.identity;
                        var body = new BodyGeometry();
                        float lMin = 99, rMin = 99, lHip = 99, rHip = 99, amount = 1, speed = 0; int frames = 0, f = 0;
                        // ⚠️ MEASURED AND PHOTOGRAPHED IN LateUpdate, LAST (order 10000). A coroutine resumes before the arm
                        // layer poses the bones, so `LocomotionArmsProbe`'s first three runs measured the clip underneath.
                        late.Tick = () =>
                        {
                            if (f < Settle) return;
                            if (!body.Ready && !body.Resolve(animator)) return;
                            frames++;
                            amount = Mathf.Min(amount, animator.LocomotionArmAmount);
                            speed = Mathf.Max(speed, new Vector3(who.Velocity.x, 0, who.Velocity.z).magnitude);
                            body.Measure(who.transform, out float gl, out float gr, out float hl, out float hr);
                            lMin = Mathf.Min(lMin, gl); rMin = Mathf.Min(rMin, gr); lHip = Mathf.Min(lHip, hl); rHip = Mathf.Min(rHip, hr);
                            int k = f - Settle;
                            if (k % ShotEvery != 0 || k / ShotEvery >= Shots * 2) return;
                            int shot = k / ShotEvery;
                            bool front = shot < Shots;
                            var at = who.transform.position;
                            var eye = front
                                ? at + who.transform.forward * 4.2f + Vector3.up * 1.15f + who.transform.right * .15f
                                : at + who.transform.right * 4.2f + Vector3.up * 1.15f + who.transform.forward * .3f;
                            witness.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(at + Vector3.up * .85f - eye));
                            PaeteKitPlayProbe.RenderFilmView(witness, rt);
                            var was = RenderTexture.active; RenderTexture.active = rt;
                            sheet.ReadPixels(new Rect(0, 0, Tile, Tile), (shot % Shots) * Tile, front ? Tile : 0);
                            RenderTexture.active = was;
                        };
                        for (f = 0; f < Settle + Shots * ShotEvery * 2 + 1; f++)
                        {
                            who.Intent.Parked = false; who.Stamina.RefillAndClearFatigue();
                            who.Intent.Move = Vector2.up; who.Intent.Set(Verb.Sprint, false);
                            who.Intent.FaceAimPoint = true; who.Intent.AimPoint = who.transform.position + Vector3.forward * 20;
                            yield return null;
                        }
                        late.Tick = null;
                        sheet.Apply();
                        File.WriteAllBytes(Path.Combine(output, $"{mode}-{id}_{tag}.png"), sheet.EncodeToPNG());
                        var fit = animator.ArmFitDiagnostics;
                        report.AppendLine(string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3:F2},{4:F1},{5:F1},{6:F1},{7:F1},{8:F2},{9:F0},{10:F1},{11:F0},{12:F1}",
                            mode, id, frames, speed, lMin * 100, rMin * 100, lHip * 100, rHip * 100, amount, fit.x, fit.y * 100, fit.z, fit.w * 100));
                        File.WriteAllText(Path.Combine(output, $"gaps_{tag}.csv"), report.ToString());
                        who.Intent.Move = Vector2.zero;
                        filmed++;
                    }
                }
            }
            finally
            {
                Time.captureFramerate = previousRate;
                File.WriteAllText(Path.Combine(output, $"gaps_{tag}.csv"), report.ToString());
                Object.Destroy(witness.gameObject); rt.Release(); Object.Destroy(rt); Object.Destroy(sheet);
            }
            Assert.Greater(filmed, 0, "Nothing was filmed.");
        }

        /// <summary>Runs the frame's measurement after every other LateUpdate, the arm layer's included.</summary>
        [DefaultExecutionOrder(10000)] private sealed class LateHook : MonoBehaviour
        {
            public Action Tick;
            private void LateUpdate() => Tick?.Invoke();
        }

        /// <summary>The visible skin's vertices grouped by bone, for the hand-to-hip gap.</summary>
        private sealed class BodyGeometry
        {
            public bool Ready;
            private SkinnedMeshRenderer _skin;
            private Vector3[] _vertices;
            private int[] _bone;
            private bool[] _handL, _handR, _arm;
            private Matrix4x4[] _binds;

            public bool Resolve(CharacterAnimator anim)
            {
                foreach (var skin in anim.GetComponentsInChildren<SkinnedMeshRenderer>(false))
                {
                    if (!skin.enabled || skin.bones == null || skin.sharedMesh == null) continue;
                    if (Array.IndexOf(skin.bones, anim.SwingArmLeft) < 0) continue;
                    _skin = skin; break;
                }
                if (_skin == null) return false;
                var mesh = _skin.sharedMesh;
                _vertices = mesh.vertices; _binds = mesh.bindposes;
                var weights = mesh.boneWeights;
                if (weights == null || weights.Length != _vertices.Length) return false;
                int armL = Array.IndexOf(_skin.bones, anim.SwingArmLeft), armR = Array.IndexOf(_skin.bones, anim.SwingArmRight);
                _bone = new int[_vertices.Length]; _arm = new bool[_vertices.Length];
                _handL = new bool[_vertices.Length]; _handR = new bool[_vertices.Length];
                for (int i = 0; i < _vertices.Length; i++) { _bone[i] = weights[i].boneIndex0; _arm[i] = _bone[i] == armL || _bone[i] == armR; }
                MarkHand(armL, anim.LeftArmAlong, _handL);
                MarkHand(armR, anim.RightArmAlong, _handR);
                Ready = true;
                return true;
            }

            /// <summary>The far eighth of the limb along its own axis, as `CharacterVisual.PalmCentre` takes it.</summary>
            private void MarkHand(int bone, Vector3 along, bool[] into)
            {
                float lo = float.MaxValue, hi = float.MinValue;
                for (int i = 0; i < _vertices.Length; i++)
                {
                    if (_bone[i] != bone) continue;
                    float a = Vector3.Dot(_binds[bone].MultiplyPoint3x4(_vertices[i]), along);
                    lo = Mathf.Min(lo, a); hi = Mathf.Max(hi, a);
                }
                float cut = hi - (hi - lo) / 8f;
                for (int i = 0; i < _vertices.Length; i++)
                    if (_bone[i] == bone && Vector3.Dot(_binds[bone].MultiplyPoint3x4(_vertices[i]), along) >= cut) into[i] = true;
            }

            /// <summary>
            /// gapL/gapR: over the whole hand, the innermost hand point against the body's outermost point on that side
            /// within the hand's height band (the front-on silhouette). hipL/hipR: the same, only while the hand is
            /// within 12 cm fore or aft of the body's centre, the moment it passes the hip.
            /// </summary>
            public void Measure(Transform character, out float gapL, out float gapR, out float hipL, out float hipR)
            {
                gapL = gapR = hipL = hipR = 99f;
                var bones = _skin.bones;
                var toChar = character.worldToLocalMatrix;
                int n = _vertices.Length;
                var local = new Vector3[n];
                for (int i = 0; i < n; i++)
                {
                    var b = bones[_bone[i]];
                    if (b == null) continue;
                    local[i] = toChar.MultiplyPoint3x4(b.localToWorldMatrix.MultiplyPoint3x4(_binds[_bone[i]].MultiplyPoint3x4(_vertices[i])));
                }
                Side(local, _handL, out gapL, out hipL);
                Side(local, _handR, out gapR, out hipR);
            }

            private void Side(Vector3[] local, bool[] hand, out float gap, out float hip)
            {
                gap = hip = 99f;
                float yLo = float.MaxValue, yHi = float.MinValue, inner = float.MaxValue, side = 0, z = 0; int count = 0;
                for (int i = 0; i < local.Length; i++)
                {
                    if (!hand[i]) continue;
                    yLo = Mathf.Min(yLo, local[i].y); yHi = Mathf.Max(yHi, local[i].y);
                    side += local[i].x; z += local[i].z; count++;
                }
                if (count == 0) return;
                float s = Mathf.Sign(side); z /= count;
                for (int i = 0; i < local.Length; i++) if (hand[i]) inner = Mathf.Min(inner, local[i].x * s);
                float outer = float.MinValue;
                for (int i = 0; i < local.Length; i++)
                {
                    if (_arm[i] || local[i].y < yLo || local[i].y > yHi) continue;
                    outer = Mathf.Max(outer, local[i].x * s);
                }
                if (outer == float.MinValue) return;
                gap = inner - outer;
                if (Mathf.Abs(z) < .12f) hip = gap;
            }
        }
    }
}
