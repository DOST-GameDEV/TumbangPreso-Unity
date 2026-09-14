using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    // A portrait-only paper cutout. Other controls retain their own supplied shapes.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class OwnerRosterTile : MaskableGraphic
    {
        private bool _selected;
        public void Select(bool selected){if(_selected==selected)return;_selected=selected;SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();var bounds=GetPixelAdjustedRect();var theme=OwnerUiTheme.Current;
            Shape(helper,bounds,theme.Ink);
            bounds.xMin+=4;bounds.xMax-=4;bounds.yMin+=8;bounds.yMax-=4;
            Shape(helper,bounds,_selected?theme.Lime:theme.Pale);
        }
        private static void Shape(VertexHelper helper,Rect rect,Color ink)
        {
            var points=new[]{new Vector2(.08f,0),new Vector2(.91f,.008f),new Vector2(.978f,.037f),new Vector2(1,.11f),
                new Vector2(.994f,.88f),new Vector2(.962f,.97f),new Vector2(.87f,1),new Vector2(.11f,.992f),
                new Vector2(.028f,.966f),new Vector2(0,.887f),new Vector2(.012f,.116f),new Vector2(.039f,.035f)};
            int start=helper.currentVertCount;helper.AddVert(rect.center,ink,Vector2.zero);
            foreach(var point in points)helper.AddVert(new Vector2(rect.xMin+point.x*rect.width,rect.yMin+point.y*rect.height),ink,Vector2.zero);
            for(int i=0;i<points.Length;i++)helper.AddTriangle(start,start+i+1,start+(i+1)%points.Length+1);
        }
    }
}
