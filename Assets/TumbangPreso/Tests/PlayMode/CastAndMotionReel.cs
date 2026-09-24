using System;
using System.Collections;
using System.IO;
using System.Linq;
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
    /// Films every body in the roster moving, every base verb, and every hero's three casts, in the body and
    /// in first person, so each can be judged and refined on its own pictures.
    ///
    /// 🧑 2026-09-24, after the throw and run pass: *"can u finish all other characters and all otehr
    /// animations"*, *"make tagging better too"*, *"and the animation of all skill casting"*. REFINE-2.9c
    /// requires every body to be inspected INDIVIDUALLY rather than assumed from Sean's. Everything plays
    /// through the real `CharacterAnimator.PlayAction` bridge (body and viewmodel together), render-only:
    /// no ability fires, nothing is scored. `$TUMP_REEL` picks the part: `move`, `verbs` or `casts`
    /// (default all). Frames land in `$TUMP_EVIDENCE/reel-<body>-<action>/`.
    /// </summary>
    public sealed class CastAndMotionReel
    {
        private int _seat; private bool _spectator, _bots;
        [UnitySetUp] public IEnumerator Before() { _seat = GameLaunch.SoloSeat; _spectator = GameLaunch.Spectator; _bots = GameLaunch.AllBots; yield return PlayModeWorld.Reset(); }
        [UnityTearDown] public IEnumerator After() { yield return PlayModeWorld.Reset(); GameLaunch.SoloSeat = _seat; GameLaunch.Spectator = _spectator; GameLaunch.AllBots = _bots; }

        private static string Part => Environment.GetEnvironmentVariable("TUMP_REEL") ?? "all";

        [UnityTest]
        public IEnumerator EveryBodyEveryVerbEveryCast()
        {
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
            var rig = Camera.main.GetComponent<CameraRig>(); rig.Follow(who, true); rig.SetAimSource(AimSource.Movement);
            var witness = new GameObject("Reel witness").AddComponent<Camera>();
            witness.CopyFrom(Camera.main); witness.enabled = false; witness.tag = "Untagged";
            witness.cullingMask &= ~(1 << 5); witness.fieldOfView = 42;
            witness.gameObject.AddComponent<ColourGrade>().AdoptFromScene();
            var animator = who.GetComponent<CharacterAnimator>();
            var book = RosterBook.Load();
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
                        foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.SetCharacter(id);
                        yield return null; yield return null;

                        if (Part == "all" || Part == "move")
                        {
                            // Side-on, running then walking, so each body's own proportions are seen moving.
                            who.Teleport(new Vector3(-6, .2f, -6)); who.transform.rotation = Quaternion.identity;
                            yield return ImprovementEvidenceProbe.Record(witness, $"reel-{id}-move", 2.2f, who, t =>
                            {
                                who.Intent.Parked = false; who.Stamina.RefillAndClearFatigue();
                                who.Intent.Move = Vector2.up; who.Intent.Set(Verb.Sprint, t < 1.1f);
                                who.Intent.FaceAimPoint = true; who.Intent.AimPoint = who.transform.position + Vector3.forward * 20;
                            }, new Vector3(3.2f, 1.3f, .6f));
                            who.Intent.Move = Vector2.zero; who.Intent.Set(Verb.Sprint, false);
                            // And from the front, running at the lens: the view that shows whether the arms
                            // clear a wide or costumed body (Dante's, Cheska's pack, Nemu's sleeves).
                            who.Teleport(new Vector3(-6, .2f, -9)); who.transform.rotation = Quaternion.identity;
                            yield return ImprovementEvidenceProbe.Record(witness, $"reel-{id}-move-front", 1.2f, who, t =>
                            {
                                who.Intent.Parked = false; who.Stamina.RefillAndClearFatigue();
                                who.Intent.Move = Vector2.up; who.Intent.Set(Verb.Sprint, true);
                                who.Intent.FaceAimPoint = true; who.Intent.AimPoint = who.transform.position + Vector3.forward * 20;
                            }, new Vector3(.3f, 1.2f, 3.6f));
                            who.Intent.Move = Vector2.zero; who.Intent.Set(Verb.Sprint, false);
                            filmed++;
                        }

                        // The base verbs on the first hero only: every body shares the verb clips. The tags are
                        // filmed on an EMPTY-HANDED body from the side, as the taya is in a match (a carried
                        // slipper keeps first person in its carry pose and hides the tag's hand).
                        if ((Part == "all" || Part == "verbs") && mode == GameMode.HeroStrike && index == 0)
                        {
                            var carrier = who.GetComponent<Carrier>();
                            var shoe = carrier.Held;
                            if (shoe != null) foreach (var r in shoe.GetComponentsInChildren<Renderer>()) r.enabled = false;
                            foreach (var arms in Object.FindObjectsByType<ViewmodelArms>(FindObjectsSortMode.None)) arms.SetHolding(false);
                            foreach (var verb in new[] { "punch", "lunge", "shove", "grab" })
                            { yield return Act(verb, new Vector3(4.4f, 1.3f, .8f)); filmed++; }
                        }

                        if ((Part == "all" || Part == "casts") && mode == GameMode.HeroStrike)
                            foreach (var cast in CastsFor(id))
                            { yield return Act(cast, new Vector3(1.9f, 1.3f, 2.9f)); filmed++; }

                        IEnumerator Act(string action, Vector3 view)
                        {
                            who.Teleport(new Vector3(0, .2f, -6)); who.transform.rotation = Quaternion.identity;
                            who.Intent.Move = Vector2.zero;
                            yield return new WaitForSeconds(.2f);
                            bool played = false;
                            // Three-quarter front: what an opponent sees.
                            yield return ImprovementEvidenceProbe.Record(witness, $"reel-{id}-{action}", 1.8f, who, t =>
                            {
                                if (!played && t >= .25f) { played = true; animator.PlayAction(action, ViewmodelFor(action)); }
                            }, view);
                        }
                    }
                }
            }
            finally { Object.Destroy(witness.gameObject); }
            Assert.Greater(filmed, 0, "Nothing was filmed.");
        }

        private static readonly System.Collections.Generic.Dictionary<string, string> Viewmodel = new System.Collections.Generic.Dictionary<string, string>
        {
            { "hero-sean-dash", "thrust-fire" },
            { "hero-sean-ignite", "ignite" },
            { "hero-sean-supernova", "supernova-slam" },
            { "hero-zack-sprint", "sprint-electric" },
            { "hero-zack-charge", "overcharge" },
            { "hero-zack-summon", "summon-lightning" },
            { "hero-dante-stomp", "stomp-heavy" },
            { "hero-dante-roar", "carapace-guard" },
            { "hero-dante-fissure", "fissure-slam" },
            { "hero-cheska-frostwave", "frost-sweep" },
            { "hero-cheska-raise", "raise-barricade" },
            { "hero-cheska-nova", "nova-burst" },
            { "hero-nemu-ghoststep", "ghost-step" },
            { "hero-nemu-project", "project-spirit" },
            { "hero-nemu-seance", "seance-channel" },
            { "hero-phaister-hex", "cast-hex" },
            { "hero-phaister-blink", "blink" },
            { "hero-phaister-eclipse", "coven-eclipse" },
            { "hero-rafi-cut", "current-cut" },
            { "hero-rafi-feint", "mirror-feint" },
            { "hero-rafi-breakwater", "breakwater-release" },
        };
        /// <summary>The first-person action a real cast sends with its body clip (`HeroAbility.ViewmodelAction`).</summary>
        private static string ViewmodelFor(string action) => Viewmodel.TryGetValue(action, out var v) ? v : action;

        /// <summary>A hero's three cast actions, by the body-clip names `CharacterAnimator.ActionClips` maps.</summary>
        private static string[] CastsFor(string id)
        {
            switch (id)
            {
                case "sean": return new[] { "hero-sean-dash", "hero-sean-ignite", "hero-sean-supernova" };
                case "zack": return new[] { "hero-zack-sprint", "hero-zack-charge", "hero-zack-summon" };
                case "dante": return new[] { "hero-dante-stomp", "hero-dante-roar", "hero-dante-fissure" };
                case "cheska": return new[] { "hero-cheska-frostwave", "hero-cheska-raise", "hero-cheska-nova" };
                case "nemu": return new[] { "hero-nemu-ghoststep", "hero-nemu-project", "hero-nemu-seance" };
                case "phaister": return new[] { "hero-phaister-hex", "hero-phaister-blink", "hero-phaister-eclipse" };
                case "rafi": return new[] { "hero-rafi-cut", "hero-rafi-feint", "hero-rafi-breakwater" };
                default: return Array.Empty<string>();
            }
        }
    }
}
