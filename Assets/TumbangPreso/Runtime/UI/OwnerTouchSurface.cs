using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerTouchSurface : MaskableGraphic
    {
        public bool Primary,Selected;
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var theme=OwnerUiTheme.Current;var rect=GetPixelAdjustedRect();
            Face(helper,rect,theme.Ink);
            rect.xMin+=5;rect.xMax-=6;rect.yMin+=8;rect.yMax-=5;
            Face(helper,rect,Selected?theme.Orange:Primary?theme.Lime:theme.Paper);
        }
        private static void Face(VertexHelper helper,Rect rect,Color fill)
        {
            var contour=new[]{new Vector2(.15f,0),new Vector2(.85f,.01f),new Vector2(.96f,.07f),new Vector2(1,.19f),
                new Vector2(.98f,.85f),new Vector2(.92f,.97f),new Vector2(.78f,1),new Vector2(.16f,.98f),
                new Vector2(.035f,.91f),new Vector2(0,.77f),new Vector2(.02f,.15f),new Vector2(.07f,.045f)};
            int start=helper.currentVertCount;helper.AddVert(rect.center,fill,Vector2.zero);
            foreach(var point in contour)helper.AddVert(new Vector2(rect.xMin+point.x*rect.width,rect.yMin+point.y*rect.height),fill,Vector2.zero);
            for(int i=0;i<contour.Length;i++)helper.AddTriangle(start,start+i+1,start+(i+1)%contour.Length+1);
        }
    }
}
