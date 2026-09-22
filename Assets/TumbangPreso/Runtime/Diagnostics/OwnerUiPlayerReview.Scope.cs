using System;
using System.Linq;
using TumbangPreso.Core;
using TumbangPreso.UI;

namespace TumbangPreso.Diagnostics
{
    public sealed partial class OwnerUiPlayerReview
    {
        private static string ReviewArgument(string flag)
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,flag);
            if(at<0)return null;
            if(at+1>=args.Length||args[at+1].StartsWith("-",StringComparison.Ordinal))throw new ArgumentException(flag+" requires a value");
            return args[at+1];
        }
        private static string[] ReviewHeroes()
        {
            string selected=ReviewArgument("-tp-review-hero");
            if(selected!=null&&!Roster.HeroPeople.Any(p=>p.Id==selected))throw new ArgumentException("Unknown review hero: "+selected);
            return Roster.HeroPeople.Where(p=>selected==null||p.Id==selected).Select(p=>p.Id).ToArray();
        }
        private static string ReviewMap()
        {
            string map=ReviewArgument("-tp-review-map")??SceneFlow.Eskinita;
            if(!SceneFlow.Maps.Contains(map))throw new ArgumentException("Unknown review map: "+map);
            GameLaunch.SelectedMap=GameLaunch.Maps.First(entry=>entry.Scene==map).Id;
            return map;
        }
    }
}
