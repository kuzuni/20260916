using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Small bronze line ornament; remains crisp and never stretches with its panel.</summary>
    public sealed class PanelOrnament : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var center=rectTransform.rect.center;
            float scale=Mathf.Min(rectTransform.rect.width/64f,rectTransform.rect.height/40f);
            Vector2[] shape={new Vector2(-30,0),new Vector2(-11,5),new Vector2(0,18),new Vector2(11,5),new Vector2(30,0),new Vector2(11,-5),new Vector2(0,-18),new Vector2(-11,-5)};
            for(int i=0;i<shape.Length;i++) Segment(vh,center+shape[i]*scale,center+shape[(i+1)%shape.Length]*scale,1.6f*scale,color);
            for(int i=0;i<4;i++) Segment(vh,center,center+shape[i*2]*scale,1.3f*scale,new Color(.38f,.27f,.14f,1));
            Vector2[] inner={new Vector2(0,11),new Vector2(7,0),new Vector2(0,-11),new Vector2(-7,0)};
            for(int i=0;i<4;i++) Segment(vh,center+inner[i]*scale,center+inner[(i+1)%4]*scale,1.1f*scale,new Color(.72f,.55f,.31f,1));
        }
        static void Segment(VertexHelper vh,Vector2 a,Vector2 b,float width,Color tint)
        {
            var normal=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f; int i=vh.currentVertCount;
            vh.AddVert(a-normal,tint,Vector2.zero); vh.AddVert(a+normal,tint,Vector2.zero); vh.AddVert(b+normal,tint,Vector2.zero); vh.AddVert(b-normal,tint,Vector2.zero);
            vh.AddTriangle(i,i+1,i+2); vh.AddTriangle(i,i+2,i+3);
        }
    }
}
