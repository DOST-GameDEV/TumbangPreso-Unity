using UnityEngine;
using UnityEngine.UI;

namespace TumbangPreso.UI
{
    /// <summary>New editable pictograms. AbilityGlyph remains the semantic contract with gameplay.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TumpAbilitySymbol : MaskableGraphic
    {
        public AbilityGlyph Glyph;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            switch (Glyph)
            {
                case AbilityGlyph.DanteStomp: case AbilityGlyph.Slam:
                    P(vh,-.18f,.38f,-.18f,-.02f,.25f,-.02f,.32f,-.16f,-.30f,-.16f);
                    P(vh,-.38f,-.34f,-.12f,-.27f,.05f,-.39f,.36f,-.25f); break;
                case AbilityGlyph.DanteShield: case AbilityGlyph.Shield:
                    P(vh,0,.40f,.31f,.26f,.25f,-.13f,0,-.40f,-.25f,-.13f,-.31f,.26f,0,.40f);
                    P(vh,0,.25f,0,-.23f); break;
                case AbilityGlyph.DanteFissure:
                    P(vh,-.40f,-.25f,-.19f,.29f,.29f,.23f,.40f,-.31f,-.40f,-.25f);
                    P(vh,.04f,.28f,-.09f,.11f,.08f,.04f,-.07f,-.11f,.10f,-.29f);
                    P(vh,-.10f,.11f,-.30f,.04f);P(vh,.07f,-.01f,.33f,-.07f);break;
                case AbilityGlyph.SeanRush: case AbilityGlyph.Dash:
                    P(vh,-.37f,.19f,.05f,.19f,.01f,.38f,.38f,0,.01f,-.33f,.05f,-.13f,-.37f,-.13f);
                    P(vh,-.40f,.36f,-.18f,.36f); break;
                case AbilityGlyph.SeanIgnite: case AbilityGlyph.Burst: case AbilityGlyph.PhaisterWitchfire:
                    P(vh,0,.42f,.10f,.19f,.25f,.28f,.33f,-.08f,.15f,-.36f,-.14f,-.36f,-.32f,-.11f,-.19f,.17f,-.13f,-.03f,0,.42f); break;
                case AbilityGlyph.SeanSupernova:
                    Ring(vh,0,0,.23f);
                    Rays(vh,8,.30f,.44f); break;
                case AbilityGlyph.CheskaFrostSheet: case AbilityGlyph.Zone:
                    P(vh,-.39f,-.04f,0,-.31f,.39f,-.04f,0,.17f,-.39f,-.04f);
                    P(vh,0,.40f,0,-.08f);P(vh,-.17f,.29f,.17f,.07f);P(vh,.17f,.29f,-.17f,.07f); break;
                case AbilityGlyph.CheskaBarricade: case AbilityGlyph.Wall:
                    P(vh,-.37f,-.33f,-.37f,.15f,-.17f,.31f,-.05f,.09f,.11f,.40f,.35f,.18f,.35f,-.33f,-.37f,-.33f);
                    P(vh,-.05f,.09f,-.05f,-.33f);P(vh,.15f,.14f,.15f,-.33f); break;
                case AbilityGlyph.CheskaNova:
                    for(int i=0;i<3;i++){float a=i*Mathf.PI/3;V(vh,Dir(a)*.40f,Dir(a+Mathf.PI)*.40f);}
                    Ring(vh,0,0,.17f); break;
                case AbilityGlyph.ZackSprint:
                    P(vh,-.04f,.33f,-.10f,.02f,.18f,-.07f,.34f,-.26f,-.20f,-.26f,-.25f,-.14f,-.10f,.02f);
                    P(vh,-.39f,.17f,-.20f,.17f);P(vh,-.42f,.02f,-.24f,.02f);P(vh,.08f,.35f,.01f,.18f,.17f,.18f,.08f,.03f);break;
                case AbilityGlyph.ZackOvercharge: case AbilityGlyph.Empower:
                    P(vh,-.31f,.32f,-.31f,-.10f,-.19f,-.32f,.18f,-.32f,.31f,-.10f,.31f,.32f,.14f,.32f,.14f,-.06f,.07f,-.14f,-.07f,-.14f,-.14f,-.06f,-.14f,.32f,-.31f,.32f);
                    P(vh,-.31f,.17f,-.14f,.17f);P(vh,.14f,.17f,.31f,.17f);break;
                case AbilityGlyph.ZackThunderstrike:
                    P(vh,-.35f,.12f,-.37f,.24f,-.23f,.33f,-.08f,.29f,.02f,.41f,.21f,.36f,.30f,.24f,.38f,.21f,.36f,.11f,-.35f,.12f);
                    P(vh,.03f,.10f,-.14f,-.13f,.03f,-.12f,-.04f,-.37f,.24f,-.03f,.08f,-.04f);
                    P(vh,-.36f,-.37f,-.18f,-.31f);P(vh,.17f,-.31f,.36f,-.37f);break;
                case AbilityGlyph.NemuPhase: case AbilityGlyph.Phase:
                    P(vh,-.30f,-.33f,-.30f,.16f,-.16f,.35f,.14f,.35f,.30f,.16f,.30f,-.33f,.14f,-.23f,0,-.36f,-.14f,-.23f,-.30f,-.33f);
                    Ring(vh,-.10f,.12f,.04f);Ring(vh,.10f,.12f,.04f); break;
                case AbilityGlyph.NemuAstralPet:
                    P(vh,-.33f,-.10f,-.28f,.32f,-.07f,.18f,.14f,.19f,.32f,.34f,.33f,-.11f,0,-.34f,-.33f,-.10f);
                    P(vh,-.12f,.02f,-.08f,-.03f);P(vh,.10f,.02f,.14f,-.03f); break;
                case AbilityGlyph.NemuSeanceVoid:
                    Ring(vh,0,0,.37f);P(vh,-.29f,0,-.10f,.17f,.13f,.17f,.30f,0,.11f,-.17f,-.11f,-.17f,-.29f,0);
                    Ring(vh,0,0,.08f); break;
                case AbilityGlyph.PhaisterHexSigil:
                    Ring(vh,0,0,.38f);P(vh,0,.29f,.26f,-.16f,-.27f,.04f,.27f,.05f,-.22f,-.19f,0,.29f); break;
                case AbilityGlyph.PhaisterShadowBlink:
                    P(vh,-.35f,.34f,-.13f,.08f,-.25f,-.21f,.04f,-.38f);
                    P(vh,-.09f,.34f,.18f,.34f,.31f,.10f,.21f,-.20f); break;
                case AbilityGlyph.PhaisterEclipse:
                    Ring(vh,0,0,.28f);P(vh,.06f,.28f,-.09f,.10f,-.05f,-.12f,.11f,-.26f);
                    Rays(vh,6,.35f,.44f); break;
                case AbilityGlyph.RafiCrosscurrent:
                    P(vh,-.38f,-.25f,-.12f,-.25f,.10f,-.13f,.18f,.08f,.12f,.27f,-.08f,.34f);
                    P(vh,-.08f,.34f,.08f,.42f,-.05f,.21f);break;
                case AbilityGlyph.RafiMirrorwake:
                    Ring(vh,-.20f,.24f,.10f);P(vh,-.32f,.06f,-.34f,-.28f,-.07f,-.28f,-.08f,.06f,-.32f,.06f);
                    Ring(vh,.18f,.20f,.09f);P(vh,.09f,.03f,.10f,-.22f,.32f,-.27f);break;
                case AbilityGlyph.RafiBreakwater:
                    P(vh,-.42f,-.27f,.40f,-.27f,.35f,-.08f,.18f,.02f,.06f,.23f,.17f,.34f,.32f,.28f,.25f,.11f);
                    P(vh,-.42f,-.27f,-.34f,-.02f,-.15f,.19f,.06f,.23f);break;
                default:
                    P(vh,-.37f,-.21f,.06f,.24f,.34f,.30f,.27f,.01f,-.20f,-.37f,-.37f,-.21f); break;
            }
        }
        private void Bolt(VertexHelper vh) => P(vh,.12f,.42f,-.24f,-.03f,-.03f,-.03f,-.12f,-.42f,.29f,.09f,.04f,.09f,.12f,.42f);
        private static Vector2 Dir(float a) => new Vector2(Mathf.Cos(a),Mathf.Sin(a));
        private void Rays(VertexHelper vh,int count,float near,float far)
        {for(int i=0;i<count;i++){var d=Dir(i*Mathf.PI*2/count);V(vh,d*near,d*far);}}
        private void Ring(VertexHelper vh,float x,float y,float radius)
        {var c=new Vector2(x,y);for(int i=0;i<24;i++)V(vh,c+Dir(i*Mathf.PI/12)*radius,c+Dir((i+1)*Mathf.PI/12)*radius);}
        private void P(VertexHelper vh,params float[] points)
        {for(int i=2;i<points.Length;i+=2)V(vh,new Vector2(points[i-2],points[i-1]),new Vector2(points[i],points[i+1]));}
        private void V(VertexHelper vh,Vector2 a,Vector2 b)
        {
            var rect=rectTransform.rect;float size=Mathf.Min(rect.width,rect.height);
            a=rect.center+a*size;b=rect.center+b*size;
            var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*Mathf.Max(1.5f,size*.036f);
            int i=vh.currentVertCount;
            vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
            vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
        }
    }
}
