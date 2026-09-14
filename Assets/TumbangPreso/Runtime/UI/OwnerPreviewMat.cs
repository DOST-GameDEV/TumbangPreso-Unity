using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerPreviewMat : MaskableGraphic
    {
        public bool Shadow;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var rect=GetPixelAdjustedRect();
            Color ink=Shadow?OwnerUiTheme.Current.DeepInk:OwnerUiTheme.Current.Pale;
            if(Shadow)ink.a=.23f;
            helper.AddVert(rect.center,ink,Vector2.zero);
            const int count=64;
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                float radius=Shadow?1:1+.017f*Mathf.Sin(angle*3)+.009f*Mathf.Cos(angle*5);
                helper.AddVert(rect.center+new Vector2(Mathf.Cos(angle)*rect.width*.5f,Mathf.Sin(angle)*rect.height*.5f)*radius,ink,Vector2.zero);
            }
            for(int i=0;i<count;i++)helper.AddTriangle(0,i+1,(i+1)%count+1);
        }
    }
}
