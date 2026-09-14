using System;
using TumbangPreso.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public sealed partial class PlayerHub
    {
        private void BuildCareerTab()
        {
            var profile=GameServices.Career?.Profile;
            HubChoice("CareerMode","Mode",new[]{"CLASSIC","HERO STRIKE"},_mode==GameMode.HeroStrike?1:0,
                value=>{_mode=value==1?GameMode.HeroStrike:GameMode.Classic;Show(Tab.Career);});
            if(profile==null){EmptyCareer();return;}
            var totals=ProfileRules.ModeFor(profile,_mode.ToString()).Totals;
            if(totals.Matches==0){EmptyCareer(profile);return;}
            var rank=profile.Rank;
            if(rank!=null && rank.MatchesThisSeason>0 && Group("Competitive rank","Your seasonal ranked standing."))
            {
                bool placing=rank.Deviation>RatingRules.SettledDeviation;
                var rankRow=HubValue("Current tier",RatingRules.TierName(RatingRules.TierFor(rank.Rating))+(placing?" · PLACING":""),
                    rank.MatchesThisSeason+" matches this season.",OwnerUiTheme.Current.Green);
                var badge=OwnerUiLayout.Rect(rankRow,"CareerRankEmblem").gameObject.AddComponent<TumpRankBadge>();
                badge.Tier=(int)RatingRules.TierFor(rank.Rating);badge.raycastTarget=false;OwnerUiLayout.Place(badge.rectTransform,0,2,88,92);
                OwnerUiLayout.Place((RectTransform)rankRow.Find("ValueName"),108,0,799,64);
                OwnerUiLayout.Place((RectTransform)rankRow.Find("ValueDetail"),108,65,1432,43);
            }
            if(Group("Overview","Classic and Hero Strike statistics stay separate."))
            {
                var summary=OwnerUiLayout.Rect(_list,"CareerOverview");summary.gameObject.AddComponent<LayoutElement>().preferredHeight=137;
                string[] names={"MATCHES","WINS","HOURS","WIN RATE"};
                string[] values={totals.Matches.ToString(),totals.Wins.ToString(),ProfileRules.HoursPlayed(totals).ToString("0.0"),
                    MatchRecordRules.IsReportable(totals.Matches)?(ProfileRules.WinRate(totals)*100).ToString("0")+"%":"--"};
                for(int i=0;i<4;i++)
                {
                    var value=OwnerUiLayout.Text(summary,"MetricValue"+i,values[i],48,OwnerUiLayout.TypeRole.Display);
                    OwnerUiLayout.Place(value.rectTransform,i*395,0,376,77);value.alignment=TextAnchor.MiddleCenter;
                    var name=OwnerUiLayout.Text(summary,"MetricName"+i,names[i],25,OwnerUiLayout.TypeRole.Accent);
                    OwnerUiLayout.Place(name.rectTransform,i*395,82,376,45);name.alignment=TextAnchor.MiddleCenter;
                }
                HubValue("Finishes",$"1st {totals.Placements[0]} · 2nd {totals.Placements[1]} · 3rd {totals.Placements[2]} · 4th {totals.Placements[3]}");
            }
            if(Group("Attack","What you did with the slipper in your hand.",false))
            {
                HubValue("Throws",totals.Throws.ToString());HubValue("Knockdowns",totals.Knockdowns.ToString());
                OwnerRate("Knockdowns per throw",ProfileRules.KnockdownsPerThrow(totals),totals.Throws);
            }
            if(Group("Retrieval","Getting your slipper back under pressure.",false))
            {
                HubValue("Retrievals",totals.Retrievals.ToString());
                HubValue("Under pressure",totals.RetrievalsUnderPressure.ToString(),$"Within {MatchRecordRules.PressureRadius:0.0} m of the defender.");
                if(totals.MatchesWithAThrow>0)HubValue("Average first throw",$"{ProfileRules.AverageTimeToFirstThrow(totals):0.0} s");
            }
            if(Group("Defence","The rounds you spent as defender.",false))
            {
                HubValue("Rounds defended",totals.RoundsDefended.ToString());HubValue("Tags",totals.Tags.ToString());
                OwnerRate("Tags per round defended",ProfileRules.TagsPerRoundDefended(totals),totals.RoundsDefended,false);
                HubValue("Passive defence",$"{ProfileRules.PassiveDefenceSeconds(totals):0} s");HubValue("Sabotages",totals.Sabotages.ToString());
            }
            if(Group("Melee","Your close-range shove and lunge.",false))
            {
                OwnerRate("Shove hit rate",ProfileRules.ShoveHitRate(totals),totals.ShoveAttempts);
                OwnerRate("Lunge hit rate",ProfileRules.LungeHitRate(totals),totals.LungeAttempts);
            }
            if(Group("Standout","Your personal bests.",false))
            {
                HubValue("Longest last stand",$"{totals.LongestLastAttacker:0.0} s");HubValue("Longest win streak",totals.LongestWinStreak.ToString());
                OwnerRate("Clutch rate",ProfileRules.ClutchRate(totals),totals.ComebackChances);
            }
            BuildMasteryRows(profile);BuildAchievementsRows(profile);SetFooter("","Rates appear once there are enough attempts to be useful.");
        }
        private void OwnerRate(string label,float value,float sample,bool percent=true)
        {
            if(MatchRecordRules.IsReportable(sample))HubValue(label,percent?$"{value*100:0}%":$"{value:0.00}");
        }
        private void BuildMasteryRows(PlayerProfile profile)
        {
            if(!Group("Hero mastery","Each hero has their own progression.",false))return;
            foreach(var hero in Roster.HeroPeople)
            {
                int xp=0,games=0;
                if(profile.Mastery!=null)foreach(var item in profile.Mastery)if(item!=null && item.Id==hero.Id)xp=item.Xp;
                if(profile.Characters!=null)foreach(var item in profile.Characters)if(item!=null && item.Id==hero.Id)games=item.Games;
                int level=ProgressionRules.MasteryLevelForXp(xp),remaining=ProgressionRules.MasteryXpPerLevel-(xp%ProgressionRules.MasteryXpPerLevel);
                HubValue(hero.Name,xp>0?"MASTERY "+level:"NOT PLAYED",xp>0?$"{games} games · {remaining} XP to mastery {level+1}":"",
                    xp>0?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.ActionInk,"UI/portraits/"+hero.Id);
            }
        }
        private void BuildAchievementsRows(PlayerProfile profile)
        {
            int earned=0;foreach(var entry in AchievementRules.Catalog)if(AchievementRules.IsUnlocked(entry,profile))earned++;
            if(!Group("Achievements",earned+" of "+AchievementRules.Catalog.Count+" earned. Cosmetic rewards only.",false))return;
            foreach(var tier in new[]{AchievementTier.Bronze,AchievementTier.Silver,AchievementTier.Gold})
                foreach(var entry in AchievementRules.Tier(tier))
                {
                    bool unlocked=AchievementRules.IsUnlocked(entry,profile);int progress=AchievementRules.ProgressFor(entry,profile);
                    HubValue(entry.Title,unlocked?"EARNED":progress+" / "+entry.TargetCount,entry.Description,
                        unlocked?OwnerUiTheme.Current.Green:OwnerUiTheme.Current.ActionInk);
                }
        }
        private void EmptyCareer(PlayerProfile profile=null)
        {
            HubNote("Your first completed match starts your career here.");
            HubAction("PlayFromCareer","Ready when you are","PLAY",()=>{Close();SceneFlow.Go(SceneFlow.ModeSelect);});
            if(profile!=null){BuildMasteryRows(profile);BuildAchievementsRows(profile);}
            SetFooter("","");
        }
    }
}
