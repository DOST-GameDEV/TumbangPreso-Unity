using System;
using System.Collections;
using System.Linq;
using TumbangPreso.CameraSystem;
using TumbangPreso.Core;
using TumbangPreso.UI;
using TumbangPreso.Visual;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator IntroductionBodiesOnly()
        {
            bool withScene = Environment.GetCommandLineArgs().Contains("-tp-introduction-scenes");
            Stage("native render-only introduction " + (withScene ? "scene" : "body") + " study, not a live cinematic phase");
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || Find("StartButton") != null, 80);
            if (Find("GuestAccount") != null) yield return Click("GuestAccount");
            else if (Find("ContinueAccount") != null) yield return Click("ContinueAccount");
            Settings.SettingsStore.Current.Fullscreen = false;
            Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));
            SceneFlow.SelectedMap = SceneFlow.Eskinita;
            yield return Click("StartButton"); yield return Click("HeroStrikeButton"); yield return Click("PracticeButton");
            yield return Click("PrimaryButton"); yield return StartReadyRound();
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
                string[] heroes = { "sean", "phaister", "zack", "nemu", "dante", "cheska" };
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
                    if (clip == null || !clip.legacy || clip.length < 2.79f) throw new InvalidOperationException(hero + " native clip was empty or unsupported");
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
                            scene.SetVisibleForCapture(true);
                            var listener = Object.FindObjectsByType<AudioListener>().FirstOrDefault(l => l.enabled && l.gameObject.activeInHierarchy);
                            if (listener == null) throw new InvalidOperationException("No game listener for sound review");
                            audio = listener.gameObject.AddComponent<ReviewAudioCapture>(); audio.Begin();
                            if (!scene.StartSound()) throw new InvalidOperationException(hero + " has no retained theme source");
                        }
                        var movie = StartCoroutine(RecordCatchMotion(motionName, 2.8f));
                        float began = Time.realtimeSinceStartup;
                        while (Time.realtimeSinceStartup - began < 2.8f)
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
            Stage("all six native body curves sampled and captured; shared phase/VFX/held props remain separate work");
        }
    }
}
