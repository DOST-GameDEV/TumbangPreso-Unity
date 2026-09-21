using System.Collections.Generic;
using TumbangPreso.Abilities;
using TumbangPreso.Net;
using TumbangPreso.Visual;
using UnityEngine;

namespace TumbangPreso.CameraSystem
{
    // These identifiers exist only inside the versioned replay payload. They do
    // not enter WorldEffectSnapshot.Apply or the live hazard replication path.
    public static class RecordedSpecialFields
    {
        public const WorldEffectSnapshot.Kind Coven=(WorldEffectSnapshot.Kind)101;
        public const WorldEffectSnapshot.Kind Kuro=(WorldEffectSnapshot.Kind)102;
        public const WorldEffectSnapshot.Kind Seismic=(WorldEffectSnapshot.Kind)103;
        public const WorldEffectSnapshot.Kind Nova=(WorldEffectSnapshot.Kind)104;
        public const WorldEffectSnapshot.Kind Ward=(WorldEffectSnapshot.Kind)105;
        public static List<WorldEffectSnapshot.Field> Capture()
        {
            var fields=WorldEffectSnapshot.Capture();
            foreach(var coven in Object.FindObjectsByType<HeroHazards.EclipseFall>())
                if(coven.isActiveAndEnabled&&coven.RecordedAge<coven.Duration)
                    fields.Add(new WorldEffectSnapshot.Field{Type=Coven,Source=coven.gameObject,Position=coven.transform.position,Forward=Vector3.forward,
                        Radius=coven.RecordedRadius,Duration=coven.Duration,Remaining=coven.Duration-coven.RecordedAge,FirstScale=coven.GatherSeconds,Owner=-1});
            foreach(var kuro in Object.FindObjectsByType<HeroHazards.SeanceVoidComponent>())
                if(kuro.isActiveAndEnabled&&kuro.Remaining>0&&kuro.GetComponent<KuroIntakePresentation>()!=null)
                {
                    var actor=GameServices.Round?.PlayerAt(kuro.OwnerSlot);
                    var pet=actor!=null?actor.GetComponent<CharacterVisual>()?.Companion:null;
                    if(pet==null)continue;
                    fields.Add(new WorldEffectSnapshot.Field{Type=Kuro,Source=kuro.gameObject,Position=kuro.transform.position,Forward=pet.MouthPosition,
                        Radius=kuro.Radius,Duration=kuro.Duration,Remaining=kuro.Remaining,Owner=kuro.OwnerSlot});
                }
            foreach(var seismic in Object.FindObjectsByType<DanteSeismicVisual>())
                if(seismic.isActiveAndEnabled&&seismic.RecordedAge<seismic.LifeSeconds)
                    fields.Add(new WorldEffectSnapshot.Field{Type=Seismic,Source=seismic.gameObject,Position=seismic.transform.position,Forward=seismic.RecordedForward,
                        Radius=seismic.RecordedRadius,Duration=seismic.LifeSeconds,Remaining=seismic.LifeSeconds-seismic.RecordedAge,FirstScale=seismic.RecordedWarning?1:0,
                        SecondScale=seismic.RecordedTremor?1:0,Split=seismic.RecordedFissure,Owner=-1});
            foreach(var nova in Object.FindObjectsByType<FrostSurfacePresentation>())
                if(nova.isActiveAndEnabled&&nova.RecordedNova&&nova.RecordedAge<nova.Duration)
                    fields.Add(new WorldEffectSnapshot.Field{Type=Nova,Source=nova.gameObject,Position=nova.transform.position,Forward=Vector3.forward,
                        Radius=nova.RecordedRadius,Duration=nova.Duration,Remaining=nova.Duration-nova.RecordedAge,Owner=-1});
            foreach(var ward in Object.FindObjectsByType<DanteCarapaceVisual>())
                if(ward.isActiveAndEnabled&&ward.RecordedVisible&&ward.RecordedOwner!=null&&ward.RecordedAge<ward.LifeSeconds)
                    fields.Add(new WorldEffectSnapshot.Field{Type=Ward,Source=ward.gameObject,Position=ward.RecordedOwner.transform.position,Forward=ward.RecordedOwner.transform.forward,
                        Duration=ward.LifeSeconds,Remaining=ward.LifeSeconds-ward.RecordedAge,Radius=1,FirstScale=ward.RecordedHeavy?1:0,SecondScale=ward.RecordedFront,Owner=ward.RecordedOwner.PlayerSlot});
            return fields;
        }
        public static bool Valid(WorldEffectSnapshot.Field f)
        {
            if(WorldEffectSnapshot.Valid(f))return true;
            if(f.Type!=Coven&&f.Type!=Kuro&&f.Type!=Seismic&&f.Type!=Nova&&f.Type!=Ward)return false;
            bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
            if(!Finite(f.Position.x)||!Finite(f.Position.y)||!Finite(f.Position.z)||f.Position.sqrMagnitude>10000*10000
                ||!Finite(f.Forward.sqrMagnitude)||(f.Type==Kuro?f.Forward.sqrMagnitude>10000*10000:f.Forward.sqrMagnitude<.5f||f.Forward.sqrMagnitude>1.5f)
                ||!Finite(f.Duration)||f.Duration<=0||f.Duration>60||!Finite(f.Remaining)||f.Remaining<0||f.Remaining>f.Duration+.05f
                ||!Finite(f.Radius)||f.Radius<=0||f.Radius>15||!Finite(f.FirstScale)||!Finite(f.SecondScale)||f.Owner< -1||f.Owner>=4)return false;
            if(f.Type==Coven)return f.FirstScale>=0&&f.FirstScale<=3&&f.FirstScale<f.Duration;
            if(f.Type==Kuro)return f.Owner>=0;
            if(f.Type==Ward)return f.Owner>=0&&(f.FirstScale==0||f.FirstScale==1)&&(f.SecondScale==1||f.SecondScale==-1);
            return f.Type!=Seismic||((f.FirstScale==0||f.FirstScale==1)&&(f.SecondScale==0||f.SecondScale==1));
        }
    }
}
