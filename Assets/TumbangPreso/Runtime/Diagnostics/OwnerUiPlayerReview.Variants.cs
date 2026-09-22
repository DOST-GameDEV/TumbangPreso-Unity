using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using TumbangPreso.Abilities;
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
        // Accepted input with staged positions/builds. This compares actual choices
        // in both roles; it is neither a freeform balance verdict nor a peer test.
        private IEnumerator ReviewKitVariants()
        {
            bool observer = Environment.GetCommandLineArgs().Contains("-tp-variant-observer");
            var round = GameServices.Round;
            var rig = Camera.main.GetComponent<CameraRig>();
            foreach (var brain in Object.FindObjectsByType<AIController>()) brain.enabled = false;
            foreach (var input in Object.FindObjectsByType<PlayerInputReader>()) input.enabled = false;
            foreach (var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>()) switcher.enabled = false;
            var report = new StringBuilder("hero,slot,variant,role,view,expected,observed,charges_before,charges_after,input_samples,actor_early_travel,actor_travel,target_travel,target_stun_samples,seconds,body_action,fpp_action\n");
            int cases = 0;
            foreach (string hero in ReviewHeroes())
            foreach (int slot in new[] { 1, 2 })
            foreach (var variant in HeroLoadoutRules.VariantsFor(hero, slot))
            foreach (bool defender in new[] { false, true })
            {
                round.EndRound();
                foreach (var player in round.Players)
                {
                    player.AbilitySystem?.ResetKit(); player.ClearStun(); player.ClearTrip();
                    player.IsBot = true; player.Intent.Clear(); player.Intent.Parked = true;
                    player.Teleport(new Vector3(7, .12f, 5 + player.PlayerSlot * 2));
                }
                yield return null;
                round.BeginRound(); round.Lata.HostRestore();
                var actor = round.Players.First(p => p.IsDefender == defender);
                var target = round.Players.First(p => p.IsDefender != defender);
                var witness = round.Players.First(p => p != actor && p != target);
                var centre = round.Lata.transform.position; centre.y = .12f;
                var start = centre + Vector3.back * (defender ? 1.2f : hero == "rafi" ? 8.2f : 3.2f);
                actor.Teleport(start); actor.transform.rotation = Quaternion.identity;
                // Rafi's material/arm review needs an unobstructed owner view;
                // a body at 1.6m filled the frame instead of showing the hands.
                target.Teleport(start + Vector3.forward * (hero == "rafi" ? 7f : 1.6f));
                target.Intent.Parked = false; actor.Intent.Parked = false;
                actor.CharacterIndex = Roster.IndexIn(Roster.HeroPeople, hero);
                var art = RosterBook.Load().FindPersonArt(hero);
                actor.GetComponent<CharacterVisual>().ApplyModel(art.Model, art.Tint, art.Clips, art.Palette, art.PetModel);
                var build = new HeroBuild { HeroId = hero };
                if (slot == 1) build.Slot1VariantId = variant.Id; else build.Slot2VariantId = variant.Id;
                actor.AbilitySystem.BindHero(hero, build);
                var system = actor.AbilitySystem;
                var ability = slot == 1 ? system.Kit.Skill1 : system.Kit.Skill2;
                if (ability.VariantName != variant.Name)
                    throw new InvalidOperationException("Native variant was not equipped: " + variant.Id);
                foreach (var shoe in Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include))
                {
                    shoe.HostDisarm();
                    shoe.transform.position = new Vector3(6 + shoe.SeatOfOrigin, shoe.RestHeight, 5);
                }
                var owned = Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include)
                    .First(s => s.SeatOfOrigin == actor.PlayerSlot);
                owned.gameObject.SetActive(true);
                if (!defender && !(hero == "zack" && slot == 2)) owned.HostForceEquip(actor);
                else owned.transform.position = start + Vector3.forward * 1.1f + Vector3.up * owned.RestHeight;
                if (observer)
                {
                    witness.Teleport(start + new Vector3(-3.6f, 0, -3.6f));
                    witness.transform.rotation = Quaternion.LookRotation(actor.transform.position - witness.transform.position);
                    witness.Intent.FaceAimPoint = true; witness.Intent.AimPoint = actor.transform.position + Vector3.up;
                }
                var followed = observer ? witness : actor;
                rig.Follow(followed, true); rig.SetAimSource(AimSource.Movement);
                Hud.Instance.Bind(followed); Hud.Instance.ShowReadyPrompt(false);
                actor.Intent.AimPoint = target.transform.position; actor.Intent.FaceAimPoint = true;
                yield return new WaitForSecondsRealtime(.2f);
                if(hero=="rafi"&&!observer)
                {
                    var arms=rig.GetComponentInChildren<ViewmodelArms>(true);
                    var meshes=arms!=null?arms.GetComponentsInChildren<MeshFilter>(true):Array.Empty<MeshFilter>();
                    if(arms==null||arms.CurrentHeroId!="rafi"||new[]{"left","right"}.Any(side=>
                        !meshes.Any(m=>m.sharedMesh==Resources.Load<Mesh>("Models/RosterArms/rafi_"+side))))
                        throw new InvalidOperationException("Rafi's actual owner hands are not his source arm meshes.");
                }

                bool roleRefusal = defender && slot == 2 && (hero == "sean" || hero == "zack");
                float seconds = roleRefusal ? 2 : Mathf.Clamp(ability.Duration + ability.Windup + 1.8f, 5, 11);
                string role = defender ? "defender" : "attacker", view = observer ? "observer" : "owner";
                string name = variant.Id + "-" + role + "-" + view;
                Stage(name + ": staged native role and actual equipped choice");
                int before = ability.ChargesRemaining, stunSamples = 0, inputSamples = 0;
                float actorTravel = 0, actorEarlyTravel = 0, targetTravel = 0;
                Vector3 actorStart = actor.transform.position, targetStart = target.transform.position;
                bool accepted = false, refused = false, sawCharge = false, sawRelease = false;
                var verb = slot == 1 ? Verb.Skill1 : Verb.Skill2;
                var answerSlot = slot == 1 ? HeroAbilitySystem.Slot.Skill1 : HeroAbilitySystem.Slot.Skill2;
                var context = new AbilityContext(actor, actor.GetComponent<Carrier>(), actor.GetComponent<CombatVerbs>());
                var listener = Object.FindObjectsByType<AudioListener>().First(l => l.enabled && l.gameObject.activeInHierarchy);
                var audio = listener.gameObject.AddComponent<ReviewAudioCapture>(); audio.Begin(8);
                try
                {
                    float began = Time.realtimeSinceStartup;
                    var movie = StartCoroutine(RecordCatchMotion(name, seconds));
                    while (Time.realtimeSinceStartup - began < seconds)
                    {
                        float age = Time.realtimeSinceStartup - began;
                        if (!system.HasVariant(variant.Id))
                            throw new InvalidOperationException("The equipped choice changed during native capture: " + name);
                        actor.Intent.Set(verb, age >= .25f && age < .7f);
                        if (actor.Intent.Pressed(verb)) inputSamples++;
                        actor.Intent.Move = !roleRefusal && slot == 1 && (hero == "sean" || hero == "zack")
                            && age > .8f && age < 2.5f ? Vector2.up * .65f : Vector2.zero;
                        if (!defender && slot == 2 && (hero == "sean" || hero == "zack"))
                            actor.Intent.Set(Verb.SpecialAbility, age > 1.5f && age < 2.1f);
                        if (hero == "nemu" && slot == 2 && age > .9f && age < 2.5f) actor.Intent.Move = Vector2.up * .5f;
                        // Include real wind-up/release and empty hands in the
                        // source-arm comparison after Rafi's short field ends.
                        if (hero == "rafi" && !defender)
                        {
                            actor.Intent.Set(Verb.SpecialAbility, age > 2.5f && age < 3.1f);
                            sawCharge |= actor.GetComponent<Carrier>().IsCharging;
                            sawRelease |= age > 3.1f && actor.GetComponent<Carrier>().Held == null;
                        }
                        if (age > .25f && system.SecondsSinceAnswer(answerSlot) < 2.5f)
                        {
                            accepted |= system.LastAnswer(answerSlot) == HeroKit.CastOutcome.Cast;
                            refused |= system.LastAnswer(answerSlot) == HeroKit.CastOutcome.CannotAct;
                        }
                        // CannotAct intentionally remains buffered without publishing
                        // LastAnswer. Verify its actual eligibility and resources instead.
                        if (roleRefusal && inputSamples > 0 && actor.CanAct() && ability.IsReady
                            && !ability.IsActive && !ability.CanActivate(context)) refused = true;
                        actorTravel = Mathf.Max(actorTravel, Vector3.Distance(actorStart, actor.transform.position));
                        if (age <= .8f) actorEarlyTravel = actorTravel;
                        targetTravel = Mathf.Max(targetTravel, Vector3.Distance(targetStart, target.transform.position));
                        if (target.IsStunned) stunSamples++;
                        if (observer) witness.Intent.AimPoint = actor.transform.position + Vector3.up;
                        yield return null;
                    }
                    yield return movie; audio.Save(Path.Combine(_folder, name));
                    report.AppendLine(FormattableString.Invariant($"{hero},{slot},{variant.Id},{role},{view},{(roleRefusal ? "refuse" : "cast")},{(accepted ? "cast" : refused ? "refuse" : "none")},{before},{ability.ChargesRemaining},{inputSamples},{actorEarlyTravel:F3},{actorTravel:F3},{targetTravel:F3},{stunSamples},{seconds:F2},{ability.CastAction},{ability.ViewmodelAction}"));
                    File.WriteAllText(Path.Combine(_folder, "skill-variants.csv"), report.ToString());
                    if (inputSamples == 0 || (roleRefusal ? accepted || !refused || ability.IsActive || ability.ChargesRemaining != before : !accepted))
                        throw new InvalidOperationException("Wrong native role outcome: " + name);
                    if(hero=="rafi"&&!defender&&(!sawCharge||!sawRelease))
                        throw new InvalidOperationException("Rafi appearance route did not include a real charge and empty-hand release: "+name);
                    cases++;
                }
                finally { actor.Intent.Clear(); audio.enabled = false; Destroy(audio); system.ResetKit(); }
            }
            int expected=ReviewHeroes().Sum(hero=>HeroLoadoutRules.VariantsFor(hero,1).Count+HeroLoadoutRules.VariantsFor(hero,2).Count)*2;
            if (cases != expected) throw new InvalidOperationException("Expected "+expected+" selected role/choice cases, got " + cases);
            Stage(cases+" selected role/choice cases completed: staged input and capture, not human balance or multiplayer certification");
        }
    }
}
