using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using TumbangPreso.Core;
using TumbangPreso.Social;
using TumbangPreso.UI.Hub;
using TumbangPreso.Visual;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator SurfaceAndSocialOnly()
        {
            Stage("controlled actual player mesh contact and social clip availability");
            yield return WaitFor(() => Find("GuestAccount") != null || Find("ContinueAccount") != null || TumpHub.Current != null, 80);
            yield return PerformanceHomeEntry();
            var contact = new List<string> { "map,shoe,min_mesh_floor_gap_m,vertices" };
            var clips = new List<string> { "hero,emote,available" };
            int pairs = 0, rigs = 0, refused = 0;
            foreach (string map in new[] { "IlalimNgTulay", "LagoonCove", "Kanto" })
            {
                HubHome.Choice = 2;
                var rules = CustomGameRules.Defaults(GameMode.HeroStrike);
                rules.Bots = CustomGameRules.MaxBots; rules.ManualReady = false;
                rules.RoundSeconds = CustomGameRules.MaxRoundSeconds;
                SceneFlow.PinSelectedRules(rules); SceneFlow.Networked = false; SceneFlow.SelectedMap = map;
                yield return PerformanceScoredEntry();
                Stage(map + " actual mesh floor contact");
                foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
                foreach (var reader in Object.FindObjectsByType<PlayerInputReader>()) reader.enabled = false;
                foreach (var body in GameServices.Round.Players) { body.Intent.Clear(); body.Intent.Parked = true; }
                var owner = GameServices.Round.Players.First(p => !p.IsDefender && p.GetComponent<Carrier>()?.Held != null);
                var shoe = owner.GetComponent<Carrier>().Held;
                shoe.HostBeginMapRecovery();
                foreach (var renderer in shoe.GetComponentsInChildren<MeshRenderer>(true)) Object.Destroy(renderer.gameObject);
                yield return null;
                foreach (var entry in Roster.Slippers)
                {
                    var art = RosterBook.Load().Slippers.First(a => a.Id == entry.Id);
                    var model = Object.Instantiate(art.Model, shoe.transform); model.name = "Visual";
                    try
                    {
                        shoe.SkinIndex = Roster.IndexIn(Roster.Slippers, entry.Id);
                        ToonSkin.ApplySlipper(model, ToonSkin.PropOutlineWidth);
                        shoe.HostFinishMapRecoveryAt(new Vector3(0, 1, 0)); yield return null;
                        if (shoe.State != SlipperState.Loose) throw new InvalidOperationException(map + "/" + entry.Id + " did not remain loose");
                        float gap = float.PositiveInfinity; int vertices = 0;
                        foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
                        {
                            if (filter.sharedMesh == null || !filter.sharedMesh.isReadable)
                                throw new InvalidOperationException(map + "/" + entry.Id + " mesh vertices cannot be inspected in this player");
                            foreach (var vertex in filter.sharedMesh.vertices)
                            {
                                Vector3 at = filter.transform.TransformPoint(vertex);
                                var hits = Physics.RaycastAll(at + Vector3.up * .25f, Vector3.down, 1, ~0, QueryTriggerInteraction.Ignore)
                                    .Where(h => h.collider.GetComponentInParent<CharacterMotor>() == null
                                        && h.collider.GetComponentInParent<Slipper>() == null && h.collider.GetComponentInParent<Lata>() == null)
                                    .OrderBy(h => h.distance).ToArray();
                                if (hits.Length == 0) throw new InvalidOperationException(map + "/" + entry.Id + " has no measured support");
                                gap = Mathf.Min(gap, at.y - hits[0].point.y); vertices++;
                            }
                        }
                        contact.Add(string.Join(",", map, entry.Id, gap.ToString("F6", CultureInfo.InvariantCulture), vertices));
                        File.WriteAllLines(Path.Combine(_folder, "ground-contact.csv"), contact);
                        if (vertices == 0 || Mathf.Abs(gap) > .003f) throw new InvalidOperationException(map + "/" + entry.Id + " visible sole gap=" + gap);
                        pairs++;
                    }
                    finally { Object.Destroy(model); }
                    yield return null;
                }
                if (map == "Kanto")
                {
                    var actor = PerformanceActor(); actor.IsBot = true; actor.Intent.Parked = true;
                    actor.Teleport(new Vector3(7, Slipper.GroundY(new Vector3(7, 0, -8)), -8));
                    var visual = actor.GetComponent<CharacterVisual>();
                    var brain = actor.GetComponent<AIController>() ?? actor.gameObject.AddComponent<AIController>(); brain.enabled = false;
                    var emotes = actor.GetComponent<EmotePlayer>();
                    const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                    var social = typeof(AIController).GetMethod("StepSocial", hidden);
                    foreach (var hero in Roster.HeroPeople)
                    {
                        var art = RosterBook.Load().FindPersonArt(hero.Id);
                        actor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero.Id);
                        visual.ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                        yield return new WaitForSecondsRealtime(.15f);
                        string unavailable = null;
                        foreach (string id in new[] { "tpose", "sit", "dance", "bow" })
                        {
                            bool ready = emotes.HasEmoteClip(id); clips.Add(hero.Id + "," + id + "," + ready);
                            if (!ready) unavailable = id;
                        }
                        if (unavailable != null)
                        {
                            if (!actor.CanAct() || !emotes.CanEmote() || !(bool)typeof(AIController).GetMethod("SafeToEmote", hidden).Invoke(brain, null))
                                throw new InvalidOperationException(hero.Id + " unavailable-motion consumer preconditions failed");
                            typeof(AIController).GetField("_wantedEmote", hidden).SetValue(brain, unavailable);
                            typeof(AIController).GetField("_wantedFor", hidden).SetValue(brain, 0f);
                            typeof(AIController).GetField("_emoteCooldown", hidden).SetValue(brain, 0f);
                            actor.Intent.Move = Vector2.up; actor.Intent.Set(Verb.Grab, true);
                            if ((bool)social.Invoke(brain, new object[] { actor.Intent, .016f }) || actor.Intent.Move != Vector2.up
                                || !actor.Intent.Pressed(Verb.Grab) || emotes.IsEmoting)
                                throw new InvalidOperationException(hero.Id + " unavailable emote still consumes movement/gameplay keys");
                            actor.Intent.Clear(); actor.Intent.Parked = true; refused++;
                        }
                        rigs++;
                    }
                    File.WriteAllLines(Path.Combine(_folder, "emote-availability.csv"), clips);
                }
                yield return PerformanceReturnHome();
            }
            if (pairs != Roster.Slippers.Count * 3 || rigs != Roster.HeroPeople.Count)
                throw new InvalidOperationException("Actual surface/social coverage incomplete");
            Debug.Log("[SurfaceSocialPlayer] pairs=" + pairs + " rigs=" + rigs + " unavailableConsumers=" + refused);
            Stage("actual mesh contact and unavailable social consumers complete; natural efficacy/performance separate");
        }
    }
}
