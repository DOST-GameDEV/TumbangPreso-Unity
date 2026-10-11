using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TumbangPreso.Core;
using TumbangPreso.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TumbangPreso.PlayTests
{
    /// <summary>
    /// REVIEW, not a gate: photographs POSSESSED (Nemu's PRUSISYON) on a real map so the pictures can be looked at. Writes
    /// `Logs/nemu-possession-review/`: the rider from beside the victim in each of its moments (drift, tell, surge, a
    /// worn-down grip, leaving), and the possessed player's own screen (hands, ink, grey).
    /// </summary>
    [Category("Review")]
    public sealed class NemuPossessionReviewProbe
    {
        private const string Out = "Logs/nemu-possession-review";
        private bool _allBots, _spectator, _pinned;
        private int _soloSeat;
        private CustomRules _rules;

        [UnitySetUp] public IEnumerator Before()
        {
            _allBots = GameLaunch.AllBots; _spectator = GameLaunch.Spectator; _soloSeat = GameLaunch.SoloSeat;
            _rules = SceneFlow.SelectedRules.Clone(); _pinned = SceneFlow.RulesPinned;
            yield return PlayModeWorld.Reset();
            SceneFlow.PinSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            GameLaunch.AllBots = false; GameLaunch.Spectator = false; GameLaunch.SoloSeat = 1;
            yield return SceneManager.LoadSceneAsync(SceneFlow.Eskinita);
            yield return new WaitForSeconds(.4f);
            Object.FindFirstObjectByType<SliceRunner>().Begin();
            foreach (var brain in Object.FindObjectsByType<AIController>(FindObjectsSortMode.None)) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) reader.enabled = false;
            Directory.CreateDirectory(Out);
            yield return new WaitForSeconds(.3f);
        }

        [UnityTearDown] public IEnumerator After()
        {
            yield return PlayModeWorld.Reset();
            GameLaunch.AllBots = _allBots; GameLaunch.Spectator = _spectator; GameLaunch.SoloSeat = _soloSeat;
            SceneFlow.AdoptRemoteRules(_rules);
            if (_pinned) SceneFlow.PinSelectedRules(_rules); else SceneFlow.UnpinSelectedRules();
        }

        private static void Shoot(Vector3 eye, Vector3 look, string name, float fov = 40f)
        {
            var go = new GameObject("ReviewLens");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = fov; cam.nearClipPlane = .05f;
            go.transform.position = eye; go.transform.LookAt(look);
            var rt = new RenderTexture(960, 540, 24);
            cam.targetTexture = rt; cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(960, 540, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null; cam.targetTexture = null;
            Object.Destroy(rt); Object.Destroy(tex); Object.Destroy(go);
        }

        private static void Dump(CharacterMotor victim, string name)
        {
            var rider = victim.GetComponentInChildren<TumbangPreso.Visual.PossessedPresence>(true);
            var lines = new System.Collections.Generic.List<string> { $"possessed={victim.IsPossessed} left={victim.PossessedLeft:F2} rider={(rider != null)} feet={victim.transform.position}" };
            if (rider != null)
                foreach (var r in rider.GetComponentsInChildren<Renderer>(true))
                    lines.Add($"{r.name} enabled={r.enabled} active={r.gameObject.activeInHierarchy} at={r.bounds.center} size={r.bounds.size} mat={(r.sharedMaterial != null ? r.sharedMaterial.shader.name : "none")}");
            File.WriteAllLines(Path.Combine(Out, name + "-dump.txt"), lines);
        }

        /// <summary>The live camera with the HUD canvas drawn through it (batchmode has no end-of-frame screen capture).</summary>
        private static void ShootHud(string name)
        {
            var cam = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
            var rt = new RenderTexture(1280, 720, 24);
            foreach (var c in canvases) { c.renderMode = RenderMode.ScreenSpaceCamera; c.worldCamera = cam; c.planeDistance = .5f; }
            Canvas.ForceUpdateCanvases();
            var old = cam.targetTexture; cam.targetTexture = rt; cam.Render(); cam.targetTexture = old;
            foreach (var c in canvases) c.renderMode = RenderMode.ScreenSpaceOverlay;
            RenderTexture.active = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
            RenderTexture.active = null; Object.Destroy(rt); Object.Destroy(tex);
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator PhotographTheRiderAndTheVictimsScreen()
        {
            var round = GameServices.Round;
            var taya = round.Players.First(p => p.IsDefender);
            var victim = round.Players.First(p => !p.IsDefender && p.PlayerSlot != 1);
            foreach (var p in round.Players) { p.Intent.Clear(); p.Intent.Parked = false; }
            Vector3 can = round.Lata.transform.position;
            taya.Teleport(can + new Vector3(0f, .12f, 1.2f));
            victim.Teleport(can + new Vector3(2.5f, .12f, 6.0f));
            yield return new WaitForSeconds(.3f);
            victim.ApplyPossessed();
            int seat = victim.PlayerSlot;
            // Each moment by setting the replicated clock, then letting a few frames pose it.
            var moments = new (string Name, float Elapsed)[]
            {
                ("1-settling", .3f), ("2-drift", NemuRules.SurgeStart(seat, 1) - 1.2f),
                ("3-tell", NemuRules.SurgeStart(seat, 1) - .12f), ("4-surge", NemuRules.SurgeStart(seat, 1) + .3f),
                ("4b-sucked-back", NemuRules.SurgeStart(seat, 1) + NemuRules.SurgeSeconds + .03f),
                ("4c-escape", 3.15f + .37f * seat + .45f),
                ("6-leaving", 14.3f),
            };
            foreach (var m in moments)
            {
                // A teleport is a fresh start for every status (`BeginSpawnSettle`), so place first, then set the clock.
                victim.Teleport(can + new Vector3(2.5f, .12f, 6.0f));
                yield return null;
                if (!victim.IsPossessed) victim.ApplyPossessed();
                victim.ApplyNetworkPossessed(NemuRules.PossessedSeconds - m.Elapsed);
                yield return null; yield return null; yield return null;
                Dump(victim, m.Name);
                var head = victim.transform.position + Vector3.up * 1.4f;
                var side = Quaternion.Euler(0f, 35f, 0f) * victim.transform.forward;
                Shoot(head + side * 2.3f + Vector3.up * .35f, head, m.Name);
                Shoot(head - victim.transform.forward * 2.6f + victim.transform.right * 1.2f + Vector3.up * .9f, head - Vector3.up * .6f, m.Name + "-behind", 46f);
            }
            // A worn-down grip: fight it for a while (the simulating peer keeps grip).
            victim.ApplyNetworkPossessed(NemuRules.PossessedSeconds - (NemuRules.SurgeStart(seat, 2) - 1.4f));
            Vector3 away = victim.transform.position - taya.transform.position; away.y = 0; away.Normalize();
            victim.Intent.Move = new Vector2(away.x, away.z);
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            victim.Intent.Move = Vector2.zero;
            yield return null;
            {
                var head = victim.transform.position + Vector3.up * 1.4f;
                Shoot(head + Quaternion.Euler(0f, 35f, 0f) * victim.transform.forward * 2.3f + Vector3.up * .35f, head, "5-worn-grip");
            }

            // The possessed player's own screen: the local seat.
            var me = round.PlayerAt(1);
            me.Teleport(can + new Vector3(-2.5f, .12f, 6.5f));
            yield return new WaitForSeconds(.2f);
            me.ApplyPossessed();
            var shots = new (string Name, float Elapsed)[]
            {
                ("own-drift", NemuRules.SurgeStart(1, 1) - 1.2f), ("own-tell", NemuRules.SurgeStart(1, 1) - .1f),
                ("own-surge", NemuRules.SurgeStart(1, 1) + .3f),
            };
            foreach (var s in shots)
            {
                me.ApplyNetworkPossessed(NemuRules.PossessedSeconds - s.Elapsed);
                yield return new WaitForSeconds(.7f);
                me.ApplyNetworkPossessed(NemuRules.PossessedSeconds - s.Elapsed);
                yield return null;
                var hud = Object.FindFirstObjectByType<TumpHudEffects>();
                var rig = Camera.main.GetComponent<CameraSystem.CameraRig>();
                File.WriteAllLines(Path.Combine(Out, s.Name + "-hud.txt"), new[] {
                    $"local={NetAuthority.LocalSlot} me={me.PlayerSlot} possessed={me.IsPossessed} following={(rig != null && rig.Following != null ? rig.Following.PlayerSlot : -1)}",
                    $"hud={(hud != null)} screen={(hud != null && hud.PossessedScreenVisible)} weight={TumbangPreso.Visual.PossessedPerception.Weight(Camera.main):F2}",
                    $"grade={(Camera.main.GetComponent<TumbangPreso.Visual.ColourGrade>() != null)} shader={(Shader.Find("TumbangPreso/PossessedVignette") != null)}" });
                ShootHud(s.Name);
                // The screen layer's own material on its own, to tell "the shader draws nothing" from "the canvas hides it".
                var layer = Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None).FirstOrDefault(i => i.name == "PossessedEdges");
                if (layer != null && layer.material != null)
                {
                    var rt = new RenderTexture(1280, 720, 0);
                    RenderTexture.active = rt; GL.Clear(true, true, Color.gray);
                    Graphics.Blit(null, rt, layer.material);
                    var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
                    File.WriteAllBytes(Path.Combine(Out, s.Name + "-layer.png"), tex.EncodeToPNG());
                    RenderTexture.active = null; Object.Destroy(rt); Object.Destroy(tex);
                }
            }
            Assert.IsTrue(File.Exists(Path.Combine(Out, "4-surge.png")));
        }
    }
}
