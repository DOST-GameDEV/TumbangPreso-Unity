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
using Object=UnityEngine.Object;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private IEnumerator OrdinarySkillsOnly()
        {
            yield return WaitFor(()=>Find("GuestAccount")!=null||Find("ContinueAccount")!=null||Find("StartButton")!=null,80);
            if(Find("GuestAccount")!=null)yield return Click("GuestAccount");
            else if(Find("ContinueAccount")!=null)yield return Click("ContinueAccount");
            Settings.SettingsStore.Current.Fullscreen=false;Screen.SetResolution(1280,720,FullScreenMode.Windowed);
            SceneFlow.SetSelectedRules(CustomGameRules.Defaults(GameMode.HeroStrike));SceneFlow.SelectedMap=SceneFlow.Eskinita;
            yield return Click("StartButton");yield return Click("HeroStrikeButton");yield return Click("PracticeButton");
            if(GameLaunch.Spectator)yield return Click("SpectateButton");
            yield return Click("PrimaryButton");yield return StartReadyRound();
            var rig=Camera.main.GetComponent<CameraRig>();int index=0;
            var report=new StringBuilder("hero,slot,body,fpp,seconds,accepted,contact_events\n");
            foreach(string hero in new[]{"sean","phaister","zack","nemu","dante","cheska"})
            {
                if(index++>0){GameServices.Round.EndRound();GameServices.Match.AdvanceRound();}
                foreach(var brain in Object.FindObjectsByType<AIController>())brain.enabled=false;
                foreach(var reader in Object.FindObjectsByType<PlayerInputReader>())reader.enabled=false;
                foreach(var switcher in Object.FindObjectsByType<DebugPlayerSwitcher>())switcher.enabled=false;
                var round=GameServices.Round;var actor=round.Players.First(p=>!p.IsDefender);
                foreach(var player in round.Players)
                {
                    player.IsBot=true;player.Intent.Clear();player.Intent.Parked=player!=actor;
                    player.ClearStun();player.ClearTrip();
                    if(player!=actor)player.Teleport(new Vector3(5,player.transform.position.y,-5+player.PlayerSlot*2));
                }
                actor.CharacterIndex=Roster.IndexIn(Roster.HeroPeople,hero);
                var art=RosterBook.Load().FindPersonArt(hero);
                actor.GetComponent<CharacterVisual>().ApplyModel(art.Model,art.Tint,art.Clips,art.Palette,art.PetModel);
                actor.AbilitySystem.BindHero(hero);rig.Follow(actor,true);rig.SetAimSource(AimSource.Movement);Hud.Instance.Bind(actor);Hud.Instance.ShowReadyPrompt(false);
                for(int slot=0;slot<2;slot++)
                {
                    Stage(hero+" ordinary skill "+(slot+1)+", native owner and actual game audio");
                    var abilities=actor.AbilitySystem;abilities.ResetKit();actor.ClearStun();actor.ClearTrip();actor.Intent.Clear();actor.Intent.Parked=false;
                    actor.Teleport(new Vector3(0,actor.transform.position.y,-7));actor.transform.rotation=Quaternion.identity;
                    var owned=Object.FindObjectsByType<Slipper>(FindObjectsInactive.Include).First(s=>s.SeatOfOrigin==actor.PlayerSlot);
                    owned.gameObject.SetActive(true);owned.HostForceEquip(actor);
                    if(hero=="zack"&&slot==1){owned.HostDisarm();owned.transform.position=new Vector3(1,owned.RestHeight,-3);}
                    var ability=slot==0?abilities.Kit.Skill1:abilities.Kit.Skill2;
                    var verb=slot==0?Verb.Skill1:Verb.Skill2;var answer=(HeroAbilitySystem.Slot)slot;
                    actor.Intent.AimPoint=new Vector3(0,.1f,-2);actor.Intent.FaceAimPoint=true;
                    yield return new WaitForSecondsRealtime(.2f);
                    float seconds=Mathf.Clamp(ability.Duration+ability.Windup+2,5,10);
                    string name=hero+"-ordinary-"+(slot+1);
                    var listener=Object.FindObjectsByType<AudioListener>().First(l=>l.enabled&&l.gameObject.activeInHierarchy);
                    var sound=listener.gameObject.AddComponent<ReviewAudioCapture>();sound.Begin(8);
                    int contacts=0;bool accepted=false;float began=Time.realtimeSinceStartup;
                    void Event(MatchFlair.Kind kind,int source,int subject,Vector3 at,float strength)
                    {if(source==actor.PlayerSlot&&kind!=MatchFlair.Kind.Throw)contacts++;}
                    MatchFlair.Presented+=Event;
                    try
                    {
                        var movie=StartCoroutine(RecordCatchMotion(name,seconds));
                        while(Time.realtimeSinceStartup-began<seconds)
                        {
                            float age=Time.realtimeSinceStartup-began;
                            actor.Intent.Set(verb,age>=.25f&&age<.7f);
                            actor.Intent.Move=(hero=="sean"||hero=="zack")&&slot==0&&age>.8f&&age<2.6f?Vector2.up*.65f:Vector2.zero;
                            // Empowerment must reach its actual thrown-shoe aftermath.
                            if(slot==1&&(hero=="sean"||hero=="zack"))actor.Intent.Set(Verb.SpecialAbility,age>1.45f&&age<2.05f);
                            if(age>.25f&&abilities.LastAnswer(answer)==HeroKit.CastOutcome.Cast&&abilities.SecondsSinceAnswer(answer)<2.8f)accepted=true;
                            yield return null;
                        }
                        yield return movie;sound.Save(Path.Combine(_folder,name));
                        report.AppendLine(FormattableString.Invariant($"{hero},{slot+1},{ability.CastAction},{ability.ViewmodelAction},{seconds:F2},{accepted},{contacts}"));
                        File.WriteAllText(Path.Combine(_folder,"ordinary-skills.csv"),report.ToString());
                        if(!accepted)throw new InvalidOperationException(hero+" ordinary "+(slot+1)+" did not accept its real press/release");
                    }
                    finally{MatchFlair.Presented-=Event;actor.Intent.Clear();sound.enabled=false;Object.Destroy(sound);}
                }
            }
            Stage("All twelve ordinary skills captured through native input and real audio; staged profiles and targets, not human freeform or guaranteed-hit evidence");
        }
    }
}
