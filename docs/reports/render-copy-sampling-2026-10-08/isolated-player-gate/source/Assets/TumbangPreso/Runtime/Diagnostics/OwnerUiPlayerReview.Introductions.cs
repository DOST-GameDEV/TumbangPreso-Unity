using System;
using System.Collections;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.UI.Hub;
using TumbangPreso.Visual;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator RootedCopySamplesOnly()
        {
            Stage("actual player copied rooted clips; authored introduction study is a separate gate");
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || TumpHub.Current != null, 80);
            yield return PerformanceHomeEntry();
            HubHome.Choice = 2;
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Bots = CustomGameRules.MaxBots; rules.ManualReady = false;
            SceneFlow.PinSelectedRules(rules); SceneFlow.Networked = false; SceneFlow.SelectedMap = ReviewMap();
            yield return PerformanceScoredEntry();
            var actor = PerformanceActor(); actor.Intent.Parked = true;
            var visual = actor.GetComponent<CharacterVisual>(); int sampled = 0;
            foreach (string hero in ReviewHeroes())
            {
                var art = RosterBook.Load().People.First(p => p.Id == hero);
                actor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
                visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                yield return new WaitForSecondsRealtime(.15f);
                var track = new MatchPoseHistory.Track(actor, visual.Model); track.Record(Time.time); track.Record(Time.time + .05f);
                var stage = new GameObject("RootedCopyPlayerFixture"); stage.SetActive(false);
                try
                {
                    var copy = track.Clone(stage.transform); if (copy == null) throw new InvalidOperationException(hero + " copy missing");
                    track.Apply(copy, track.Newest); stage.SetActive(true);
                    var originalAnimator = visual.Model.GetComponentInChildren<Animator>();
                    var root = originalAnimator != null ? track.CopiedBone(copy, originalAnimator.transform) : null;
                    var sampler = root != null ? root.GetComponent<Animator>() : null;
                    if (sampler == null || sampler.enabled || sampler.runtimeAnimatorController != null || sampler.applyRootMotion)
                        throw new InvalidOperationException(hero + " sampler is missing or automatic");
                    var set = GeneratedMotionAssets.For(RootedMotion.Folder, DanceClip.ResourceName(originalAnimator.transform));
                    if (set?.Clips == null || set.Clips.Length != 4) throw new InvalidOperationException(hero + " rooted set incomplete");
                    foreach (var clip in set.Clips)
                    {
                        track.Apply(copy, track.Newest);
                        var beforeRotations = copy.Bones.Select(b => b.localRotation).ToArray();
                        var beforePositions = copy.Bones.Select(b => b.localPosition).ToArray();
                        clip.SampleAnimation(root.gameObject, clip.length * .37f);
                        int moved = Enumerable.Range(0, copy.Bones.Length).Count(n => Quaternion.Angle(beforeRotations[n], copy.Bones[n].localRotation) > .1f
                            || (beforePositions[n] - copy.Bones[n].localPosition).sqrMagnitude > 1e-7f);
                        if (moved == 0) throw new InvalidOperationException(hero + " / " + clip.name + " copied bones did not move in player");
                        var heldRotations = copy.Bones.Select(b => b.localRotation).ToArray();
                        var heldPositions = copy.Bones.Select(b => b.localPosition).ToArray();
                        yield return null; yield return null;
                        for (int n = 0; n < copy.Bones.Length; n++)
                            if (Quaternion.Angle(heldRotations[n], copy.Bones[n].localRotation) > .01f
                                || Vector3.Distance(heldPositions[n], copy.Bones[n].localPosition) > .0001f)
                                throw new InvalidOperationException(hero + " copied pose updated automatically");
                        Debug.Log($"[CopyRootedPlayer] hero={hero} clip={clip.name} movedBones={moved} disabledSampler=true controller=false"); sampled++;
                    }
                    if (stage.GetComponentsInChildren<MonoBehaviour>(true).Length != 0) throw new InvalidOperationException(hero + " copy has gameplay scripts");
                }
                finally { Object.Destroy(stage); }
            }
            if (sampled != ReviewHeroes().Length * 4) throw new InvalidOperationException("Missing copied clip samples");
            Stage("actual player copied rooted samples complete: " + sampled);
        }

        private IEnumerator IntroductionBodiesOnly()
        {
            bool withScene = Environment.GetCommandLineArgs().Contains("-tp-introduction-scenes");
            Stage("native render-only introduction " + (withScene ? "scene" : "body") + " study, not a live cinematic phase");
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || TumpHub.Current != null, 80);
            yield return PerformanceHomeEntry();
            Settings.SettingsStore.Current.Fullscreen = false;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            HubHome.Choice = 2;
            var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
            rules.Bots = CustomGameRules.MaxBots;
            rules.ManualReady = false;
            SceneFlow.PinSelectedRules(rules);
            SceneFlow.Networked = false;
            SceneFlow.SelectedMap = ReviewMap();
            yield return PerformanceScoredEntry();
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
            var actor = Object.FindAnyObjectByType<PauseWatcher>().Local;
            actor.IsBot = true;
            foreach (var other in GameServices.Round.Players)
            {
                other.Intent.Clear(); other.Intent.Parked = true;
                if (other != actor) other.Teleport(new Vector3(-9, other.transform.position.y, 8 + other.PlayerSlot * 3));
            }
            var visual = actor.GetComponent<CharacterVisual>();
            var rig = Camera.main.GetComponent<CameraRig>();
            var camera = new GameObject("NativeIntroductionArtCamera").AddComponent<Camera>();
            camera.CopyFrom(Camera.main); camera.tag = "Untagged"; camera.fieldOfView = 45;
            camera.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            rig.SetActive(false); Hud.Instance.gameObject.SetActive(false);
            try
            {
                string[] heroes = ReviewHeroes();
                for (int i = 0; i < heroes.Length; i++)
                {
                    string hero = heroes[i]; Stage(hero + " native introduction body");
                    actor.CharacterIndex = Roster.IndexIn(Roster.GetPeople(GameMode.HeroStrike), hero);
                    var art = RosterBook.Load().People.First(p => p.Id == hero);
                    visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                    actor.Teleport(new Vector3(0, actor.transform.position.y, -4)); actor.transform.rotation = Quaternion.identity;
                    yield return new WaitForSeconds(.15f);
                    var track = new MatchPoseHistory.Track(actor, visual.Model);
                    track.Record(Time.time); track.Record(Time.time + .05f);
                    var stage = new GameObject("NativeIntroductionRenderCopy"); stage.SetActive(false);
                    var copy = track.Clone(stage.transform); track.Apply(copy, track.Newest);
                    var clip = HeroAbilityClips.BuildUltimateIntroduction(copy.Root.transform, hero, actor.GetComponent<Carrier>().Held != null);
                    float seconds = UltimatePerformance.For(hero, actor.GetComponent<Carrier>().Held != null)?.Seconds ?? 2.8f;
                    if (clip == null || !clip.legacy || clip.length < seconds - .01f) throw new InvalidOperationException(hero + " native clip was empty or unsupported");
                    var arm = copy.Bones.First(b => b.name == "arm-right");
                    clip.SampleAnimation(copy.Root, 0); var rest = arm.localRotation;
                    clip.SampleAnimation(copy.Root, 1.2f);
                    if (Quaternion.Angle(rest, arm.localRotation) < 5) throw new InvalidOperationException(hero + " native curve did not animate its arm");
                    var held = actor.GetComponent<Carrier>().Held;
                    bool heldActive = held != null && held.gameObject.activeSelf;
                    if (held != null) held.gameObject.SetActive(false);
                    if (visual.Companion != null) visual.Companion.gameObject.SetActive(false);
                    visual.Model.SetActive(false); stage.SetActive(true); copy.ShowOnlyForCapture(true);
                    // The study has only this camera. Give the isolated copy a
                    // real contact shadow and place its neutral feet on this street.
                    clip.SampleAnimation(copy.Root, 0);
                    var surfaces = copy.Root.GetComponentsInChildren<Renderer>();
                    float low = surfaces.Min(r => r.bounds.min.y);
                    stage.transform.position += Vector3.up * (Slipper.GroundY(actor.transform.position) - low);
                    foreach (var surface in surfaces) surface.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    camera.transform.position = actor.transform.position + new Vector3(i == 2 || i == 4 ? -2.4f : 2, 1.2f, 3.8f);
                    camera.transform.LookAt(actor.transform.position + Vector3.up * .9f);
                    HeroIntroductionScene scene = null;
                    ReviewAudioCapture audio = null;
                    string motionName = hero + (withScene ? "-introduction-scene-motion" : "-introduction-motion");
                    try
                    {
                        if (withScene)
                        {
                            scene = new HeroIntroductionScene(stage.transform, hero, actor, copy);
                            if (held != null)
                            {
                                var prop = copy.Root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "IntroductionHeldSlipper");
                                if (prop == null || prop.GetComponentsInChildren<MeshFilter>(true).Length == 0
                                    || prop.GetComponentsInChildren<Slipper>(true).Length != 0
                                    || prop.GetComponentsInChildren<Collider>(true).Length != 0)
                                    throw new InvalidOperationException(hero + " native held shoe violated render-only grip contract");
                            }
                            scene.SetVisibleForCapture(true);
                            var listener = Object.FindObjectsByType<AudioListener>().FirstOrDefault(l => l.enabled && l.gameObject.activeInHierarchy);
                            if (listener == null) throw new InvalidOperationException("No game listener for sound review");
                            audio = listener.gameObject.AddComponent<ReviewAudioCapture>(); audio.Begin();
                            if (!scene.StartSound() && Audio.AudioCues.SkillSfxOn) throw new InvalidOperationException(hero + " has no retained theme source");
                        }
                        var movie = StartCoroutine(RecordCatchMotion(motionName, seconds));
                        float began = Time.realtimeSinceStartup;
                        while (Time.realtimeSinceStartup - began < seconds)
                        {
                            float age = Time.realtimeSinceStartup - began;
                            clip.SampleAnimation(copy.Root, age);
                            if (scene != null)
                            {
                                scene.Sample(age); scene.Shot(age, out var eye, out var target, out var lens, camera.aspect);
                                camera.transform.position = eye; camera.transform.LookAt(target); camera.fieldOfView = lens;
                            }
                            yield return null;
                        }
                        yield return movie;
                        if (audio != null) audio.Save(System.IO.Path.Combine(_folder, motionName));
                    }
                    finally
                    {
                        if (audio != null) { audio.enabled = false; Object.Destroy(audio); }
                        scene?.Dispose();
                        visual.Model.SetActive(true);
                        if (held != null) held.gameObject.SetActive(heldActive);
                        if (visual.Companion != null) visual.Companion.gameObject.SetActive(true);
                        stage.SetActive(false); Object.Destroy(stage); Object.Destroy(clip);
                    }
                }
            }
            finally { rig.SetActive(true); Hud.Instance.gameObject.SetActive(true); Object.Destroy(camera.gameObject); }
            Stage(withScene ? "all six native scenes and held shoe copies captured; live shared phase remains separate work"
                : "all six native body curves sampled and captured; live shared phase remains separate work");
        }
    }
}
