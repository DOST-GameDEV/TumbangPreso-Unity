using UnityEngine;

namespace TumbangPreso.UI
{
    // A compact hanging status ticket, distinct from full reading sheets/actions.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerQueueTicket : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper h)
        {
            h.Clear();var r=GetPixelAdjustedRect();
            var points=new[]{new Vector2(r.xMin+14,r.yMax),new Vector2(r.xMax-31,r.yMax-3),
                new Vector2(r.xMax,r.yMax-28),new Vector2(r.xMax-3,r.yMin+22),
                new Vector2(r.xMax-20,r.yMin+6),new Vector2(r.xMin+32,r.yMin),
                new Vector2(r.xMin,r.yMin+19),new Vector2(r.xMin+2,r.yMax-18)};
            h.AddVert(r.center,OwnerUiTheme.Current.DeepInk,Vector2.zero);
            foreach(var p in points)h.AddVert(p,OwnerUiTheme.Current.DeepInk,Vector2.zero);
            for(int i=0;i<points.Length;i++)h.AddTriangle(0,i+1,(i+1)%points.Length+1);
        }
    }
}
