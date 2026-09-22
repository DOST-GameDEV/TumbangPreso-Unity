using System;
using UnityEngine;

namespace TumbangPreso.Visual
{
    [CreateAssetMenu(menuName="Tumbang Preso/World look profile")]
    public sealed class WorldLookProfile : ScriptableObject
    {
        [Serializable] public sealed class MapLook
        {
            public string Map;
            public Color Sky,Equator,Ground,ShadowTint,Chalk,ChalkEdge;
            public float FogStart,FogEnd;
            public int WearKind;
            public MapLook(string map,Color sky,Color equator,Color ground,Color tint,float fogStart,float fogEnd,int wear,bool dark)
            {
                Map=map;Sky=sky;Equator=equator;Ground=ground;ShadowTint=tint;FogStart=fogStart;FogEnd=fogEnd;WearKind=wear;
                Chalk=dark?new Color(.18f,.16f,.14f):new Color(.96f,.92f,.81f);
                ChalkEdge=dark?new Color(.79f,.75f,.65f):new Color(.14f,.11f,.075f);
            }
        }
        [Range(.2f,.6f)] public float ShadowLevel=.44f;
        [Range(.03f,.15f)] public float BandEdge=.065f;
        [Range(0,.25f)] public float UpperRim=.16f;
        [Range(0,.2f)] public float FeetShade=.13f;
        [Range(0,.2f)] public float EnvironmentContact=.12f;
        [Range(0,.3f)] public float MetalHighlight=.22f;
        public MapLook[] Maps={
            new MapLook("BayanPlaza",new Color(.25f,.28f,.32f),new Color(.18f,.17f,.17f),new Color(.10f,.085f,.065f),new Color(.95f,.92f,1.02f),22,100,0,true),
            new MapLook("Eskinita",new Color(.29f,.27f,.24f),new Color(.19f,.16f,.14f),new Color(.10f,.08f,.06f),new Color(1.01f,.95f,.93f),20,90,1,false),
            new MapLook("IlalimNgTulay",new Color(.22f,.28f,.31f),new Color(.15f,.18f,.20f),new Color(.08f,.09f,.085f),new Color(.90f,.94f,1.06f),24,96,2,false),
            new MapLook("SaBubong",new Color(.27f,.25f,.29f),new Color(.18f,.16f,.17f),new Color(.11f,.085f,.065f),new Color(.99f,.91f,1.02f),25,120,3,true),
            new MapLook("Lagoon",new Color(.25f,.31f,.32f),new Color(.16f,.22f,.22f),new Color(.10f,.13f,.12f),new Color(.91f,.99f,1.02f),42,200,4,false)
        };
        public MapLook Find(string map)
        {foreach(var entry in Maps)if(entry.Map==map)return entry;return null;}
        private static WorldLookProfile _current;
        public static WorldLookProfile Current
        {
            get
            {
                if(_current!=null && _current.hideFlags!=HideFlags.HideAndDontSave)return _current;
                var authored=Resources.Load<WorldLookProfile>("WorldLookProfile");
                if(authored!=null)
                {
                    if(_current!=null){if(Application.isPlaying)Destroy(_current);else DestroyImmediate(_current);}
                    return _current=authored;
                }
                if(_current==null){_current=CreateInstance<WorldLookProfile>();_current.hideFlags=HideFlags.HideAndDontSave;}
                return _current;
            }
        }
    }
}
