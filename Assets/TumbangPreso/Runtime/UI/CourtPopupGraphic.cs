using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    public static class CourtPresentationPalette
    {
        public static readonly Color Ink=new Color32(35,29,33,255);
        public static readonly Color Red=new Color32(204,48,47,255);
        public static readonly Color DeepRed=new Color32(115,24,34,255);
        public static readonly Color Paper=new Color32(255,244,222,255);
        public static readonly Color Gold=new Color32(255,197,76,255);
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CourtPopupGraphic : MaskableGraphic
    {
        public bool Brush;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();var rect=GetPixelAdjustedRect();
            var points=Brush?new[]{new Vector2(.03f,.04f),new Vector2(.01f,.23f),new Vector2(.04f,.37f),
                new Vector2(0,.55f),new Vector2(.04f,.71f),new Vector2(.015f,.9f),new Vector2(.26f,.96f),
                new Vector2(.95f,1),new Vector2(.93f,.86f),new Vector2(1,.76f),new Vector2(.96f,.53f),
                new Vector2(.99f,.33f),new Vector2(.94f,.19f),new Vector2(.97f,.04f),new Vector2(.72f,.07f)}:
                new[]{new Vector2(0,0),new Vector2(.04f,1),new Vector2(1,.97f),new Vector2(.96f,.03f)};
            mesh.AddVert(rect.center,color,Vector2.zero);
            foreach(var p in points)mesh.AddVert(new Vector2(Mathf.Lerp(rect.xMin,rect.xMax,p.x),Mathf.Lerp(rect.yMin,rect.yMax,p.y)),color,Vector2.zero);
            for(int i=0;i<points.Length;i++)mesh.AddTriangle(0,i+1,(i+1)%points.Length+1);
        }
    }
}
