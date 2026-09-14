using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerTouchDisc : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var rect=GetPixelAdjustedRect();const int count=64;
            helper.AddVert(rect.center,color,Vector2.zero);
            for(int i=0;i<count;i++)
            {
                float angle=i*Mathf.PI*2/count;
                helper.AddVert(rect.center+new Vector2(Mathf.Cos(angle)*rect.width*.49f,Mathf.Sin(angle)*rect.height*.49f),color,Vector2.zero);
            }
            for(int i=0;i<count;i++)helper.AddTriangle(0,i+1,(i+1)%count+1);
        }
    }
}
