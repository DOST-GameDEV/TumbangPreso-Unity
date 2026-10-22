using System.Collections;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// The taya's identity marks on every body (owner, 2026-10-22: *"make sure it looks good for ALL characters kasi
    /// iba iba size nila"*, *"paete isnt even human"*). Each hero stands as taya; the band must wrap that rig's own
    /// head, or be absent on a rig with no head bone, and a close shot of every head is saved for review.
    /// </summary>
    public sealed class TayaIdentityTests
    {
        [UnitySetUp] public IEnumerator Before() => PlayModeWorld.Reset();
        [UnityTearDown] public IEnumerator After() => PlayModeWorld.Reset();

        private static IEnumerator Open()
        {
            SceneFlow.Networked = false; SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.Classic));
            GameLaunch.SoloSeat = 1; GameLaunch.GuidedTutorial = false;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita); yield return new WaitForSeconds(.2f);
            foreach (var ai in Object.FindObjectsByType<AIController>()) ai.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            Object.FindAnyObjectByType<SliceRunner>().Begin(); yield return null;
            foreach (var actor in GameServices.Round.Players) { actor.Intent.Clear(); actor.ClearStun(); actor.ClearTrip(); }
        }

        [UnityTest]
        public IEnumerator RoundStartRevealsTheTaya()
        {
            int before = TayaReveal.Played;
            yield return Open();
            float until = Time.unscaledTime + 4;
            while (TayaReveal.Played == before && Time.unscaledTime < until) yield return null;
            Assert.Greater(TayaReveal.Played, before, "A live round must reveal its taya.");
            Assert.IsNotNull(TayaIdentity.Ring, "The taya wears a ground ring.");
        }

        /// <summary>
        /// Who can be tagged, and whether the taya can tag at all, read off the ground (owner, 2026-10-22: *"easier to tell
        /// if someone can be taya and if someone can't"*): a taggable attacker stands in the danger arcs, a safe one
        /// does not, and a downed can disarms the taya and clears every arc. Frames in Logs/taya-identity for review.
        /// </summary>
        [UnityTest]
        public IEnumerator TaggableAttackersStandInDangerArcsAndADownedCanDisarmsTheTaya()
        {
            yield return Open();
            var round = GameServices.Round; var can = round.Lata;
            float until = Time.unscaledTime + 4;
            while (can.IsProtected && Time.unscaledTime < until) yield return null;
            CharacterMotor taya = null, runner = null;
            // Not this screen's own seat: its body is hidden from its own first-person camera.
            foreach (var actor in round.Players) { if (actor.IsDefender) taya = actor; else if (runner == null && actor.PlayerSlot != GameLaunch.SoloSeat) runner = actor; }
            Assert.IsNotNull(taya); Assert.IsNotNull(runner);
            Vector3 mark = can.transform.position;
            runner.Teleport(mark + Vector3.back * 1.1f + Vector3.right * .6f);
            taya.Teleport(mark + Vector3.back * 2.6f + Vector3.left * .8f);
            runner.HoldingSlipper = true;
            for (int i = 0; i < 3; i++) yield return null;
            Assert.IsTrue(runner.IsInsideBox(), "The staged runner stands inside the box.");
            Assert.IsTrue(runner.IsTaggable(), "Inside the box with a slipper is taggable.");
            Assert.IsTrue(TayaIdentity.DangerShown(runner), "A taggable attacker must stand in the danger arcs.");
            Assert.IsFalse(TayaIdentity.TayaDisarmed);
            yield return new WaitForSecondsRealtime(.3f);
            var shot = new GameObject("TayaStateShot").AddComponent<Camera>();
            try
            {
                Vector3 middle = (runner.transform.position + taya.transform.position) * .5f + Vector3.up * .5f;
                Vector3 from = middle + new Vector3(3.2f, 3.4f, -3.6f);
                shot.transform.SetPositionAndRotation(from, Quaternion.LookRotation(middle - from));
                shot.fieldOfView = 42; shot.nearClipPlane = .05f;
                yield return GameplayShots.Render(shot, "state-taggable", false, outDir: "Logs/taya-identity", width: 960, height: 540);

                runner.HoldingSlipper = false;
                yield return null; yield return null;
                Assert.IsFalse(runner.IsTaggable());
                Assert.IsFalse(TayaIdentity.DangerShown(runner), "A safe attacker loses the arcs at once.");
                yield return new WaitForSecondsRealtime(.4f);
                yield return GameplayShots.Render(shot, "state-safe", false, outDir: "Logs/taya-identity", width: 960, height: 540);

                runner.HoldingSlipper = true;
                can.HostKnockDown(runner.PlayerSlot);
                Assert.IsFalse(can.IsUpright);
                yield return new WaitForSecondsRealtime(.4f);
                Assert.IsTrue(TayaIdentity.TayaDisarmed, "A downed can disarms the taya.");
                Assert.IsFalse(TayaIdentity.DangerShown(runner), "Nobody can be tagged while the can is down.");
                yield return GameplayShots.Render(shot, "state-disarmed", false, outDir: "Logs/taya-identity", width: 960, height: 540);
            }
            finally { runner.HoldingSlipper = false; Object.Destroy(shot.gameObject); }
        }

        [UnityTest] public IEnumerator Band_dante() => CheckBand("dante");
        [UnityTest] public IEnumerator Band_nemu() => CheckBand("nemu");
        [UnityTest] public IEnumerator Band_phaister() => CheckBand("phaister");
        [UnityTest] public IEnumerator Band_amihan() => CheckBand("amihan");
        [UnityTest] public IEnumerator Band_paete() => CheckBand("paete");
        [UnityTest] public IEnumerator Band_cheska() => CheckBand("cheska");
        [UnityTest] public IEnumerator Band_sean() => CheckBand("sean");
        [UnityTest] public IEnumerator Band_zack() => CheckBand("zack");
        [UnityTest] public IEnumerator Band_rafi() => CheckBand("rafi");

        private static IEnumerator CheckBand(string id)
        {
            yield return Open();
            var round = GameServices.Round;
            CharacterMotor taya = null;
            foreach (var actor in round.Players) if (actor != null && actor.IsDefender) taya = actor;
            Assert.IsNotNull(taya);
            var art = RosterBook.Load().FindPersonArt(id);
            Assert.IsNotNull(art, id);
            taya.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
            for (int i = 0; i < 4; i++) yield return null;

            var ring = TayaIdentity.Ring;
            Assert.IsNotNull(ring, id + ": no ground ring");
            Vector3 flat = ring.transform.position - taya.transform.position; flat.y = 0;
            Assert.Less(flat.magnitude, .01f, id + ": the ring must sit under the taya");

            // The head as the model draws it: everything above the `head` bone. The close-up comes first so every hero
            // has one to review whatever the asserts say (every rig differs, Paete is not human).
            var visual = taya.GetComponent<CharacterVisual>();
            Transform head = null;
            foreach (var skin in visual.Model.GetComponentsInChildren<SkinnedMeshRenderer>())
            { int index = System.Array.FindIndex(skin.bones, x => x != null && x.name == "head"); if (index >= 0) { head = skin.bones[index]; break; } }
            Assert.IsNotNull(head, id + ": every playable rig has a head bone");
            float neck = head.position.y, top = neck, reach = 0;
            foreach (var renderer in visual.Model.GetComponentsInChildren<Renderer>())
                if (renderer.enabled && (renderer is SkinnedMeshRenderer || renderer is MeshRenderer) && renderer.GetComponent<VfxRenderTag>() == null
                    && renderer.bounds.max.y > neck + .1f)
                {
                    var b = renderer.bounds;
                    top = Mathf.Max(top, b.max.y);
                    reach = Mathf.Max(reach, Mathf.Abs(b.min.x - head.position.x), Mathf.Abs(b.max.x - head.position.x),
                        Mathf.Abs(b.min.z - head.position.z), Mathf.Abs(b.max.z - head.position.z));
                }
            Bounds headBounds = new Bounds(new Vector3(head.position.x, (neck + top) * .5f, head.position.z), new Vector3(.3f, top - neck, .3f));
            DumpHeadRig(id, taya, visual, head);
            var shot = new GameObject("TayaBandShot").AddComponent<Camera>();
            try
            {
                Vector3 aim = headBounds.center;
                float size = Mathf.Max(.45f, headBounds.size.y);
                Vector3 from = aim + (taya.transform.forward * .9f + taya.transform.right * .45f + Vector3.up * .25f).normalized * (size * 3.2f + .4f);
                shot.transform.SetPositionAndRotation(from, Quaternion.LookRotation(aim - from));
                shot.fieldOfView = 40; shot.nearClipPlane = .02f;
                yield return GameplayShots.Render(shot, id, false, outDir: "Logs/taya-identity", width: 800, height: 800);
                Vector3 side = aim + taya.transform.right * (size * 3.4f + .4f);
                shot.transform.SetPositionAndRotation(side, Quaternion.LookRotation(aim - side));
                yield return GameplayShots.Render(shot, id + "-side", false, outDir: "Logs/taya-identity", width: 800, height: 800);
            }
            finally { Object.Destroy(shot.gameObject); }

            var band = TayaIdentity.Band;
            Assert.IsNotNull(band, id + ": the taya wears the headband");
            var bb = band.bounds;
            string measures = id + " (" + (visual.SourceModel != null ? visual.SourceModel.name : "?") + ")" + System.FormattableString.Invariant(
                $": neck {neck:F3} top {top:F3} band y {bb.center.y:F3} half {TayaIdentity.BandHalf.x:F3}x{TayaIdentity.BandHalf.y:F3} head reach {reach:F3}");
            Debug.Log("[TayaBand] " + measures);
            System.IO.File.AppendAllText("Logs/taya-identity/measures.txt", measures + System.Environment.NewLine);
            Assert.That(bb.center.y, Is.InRange(neck, top), id + ": band not on the head");
            Vector3 offset = bb.center - head.position; offset.y = 0;
            Assert.Less(offset.magnitude, .2f, id + ": band off the head");
            Assert.That(Mathf.Min(bb.extents.x, bb.extents.z), Is.GreaterThan(.05f), id + ": band too small to be a band");
            // Never wider than the head the model draws: a band, not a hoop round hair or brim.
            Assert.That(Mathf.Max(TayaIdentity.BandHalf.x, TayaIdentity.BandHalf.y), Is.LessThan(reach + .02f), id + ": band a hoop, not a headband");
        }

        /// <summary>
        /// What the rig draws around its head, for fitting the band by data: every renderer (skinned or not, readable or
        /// not) with its extent, and for each readable skin the vertices per dominant bone above the neck, in the body's
        /// frame relative to the head bone (x right, y up, z forward).
        /// </summary>
        private static void DumpHeadRig(string id, CharacterMotor taya, CharacterVisual visual, Transform head)
        {
            var text = new System.Text.StringBuilder();
            Vector3 Local(Vector3 world) => taya.transform.InverseTransformPoint(world) - taya.transform.InverseTransformPoint(head.position);
            text.AppendLine(id + " head bone at " + Local(head.position) + " world y " + head.position.y.ToString("F3"));
            var baked = new Mesh();
            try
            {
                foreach (var renderer in visual.Model.GetComponentsInChildren<Renderer>())
                {
                    var b = renderer.bounds;
                    text.AppendLine(System.FormattableString.Invariant($"renderer {renderer.GetType().Name} '{renderer.name}' parent '{renderer.transform.parent?.name}' enabled {renderer.enabled} min {Local(b.min)} max {Local(b.max)}"));
                    if (!(renderer is SkinnedMeshRenderer skin) || skin.sharedMesh == null) continue;
                    text.AppendLine("  readable " + skin.sharedMesh.isReadable + " vertices " + skin.sharedMesh.vertexCount);
                    if (!skin.sharedMesh.isReadable) continue;
                    skin.BakeMesh(baked, true);
                    var points = baked.vertices; var weights = skin.sharedMesh.boneWeights;
                    if (weights.Length != points.Length) continue;
                    var per = new System.Collections.Generic.Dictionary<int, Bounds>(); var counts = new System.Collections.Generic.Dictionary<int, int>();
                    for (int i = 0; i < points.Length; i++)
                    {
                        Vector3 q = Local(skin.transform.TransformPoint(points[i]));
                        if (q.y < -.08f) continue;
                        int bone = weights[i].boneIndex0;
                        if (per.TryGetValue(bone, out var box)) { box.Encapsulate(q); per[bone] = box; counts[bone]++; }
                        else { per[bone] = new Bounds(q, Vector3.zero); counts[bone] = 1; }
                    }
                    foreach (var pair in per)
                        text.AppendLine(System.FormattableString.Invariant($"  bone '{(pair.Key < skin.bones.Length && skin.bones[pair.Key] != null ? skin.bones[pair.Key].name : "?")}' n {counts[pair.Key]} min {pair.Value.min} max {pair.Value.max}"));
                }
            }
            finally { Object.Destroy(baked); }
            System.IO.Directory.CreateDirectory("Logs/taya-identity");
            System.IO.File.WriteAllText("Logs/taya-identity/" + id + "-rig.txt", text.ToString());
        }
    }
}