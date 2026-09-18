using UnityEngine;
using UnityEngine.UI;
namespace Moonlit.UI
{
    // Native monochrome slot glyphs; separate sprites, never baked into a card or screenshot.
    public static class EquipmentPictograms
    {
        static readonly Sprite[] icons=new Sprite[9];
        static Material frameMaterial;
        static readonly float[] shape0={-.3f,.7f,-.65f,.48f,-.78f,.05f,-.44f,-.06f,-.35f,.18f,-.38f,-.7f,.38f,-.7f,.35f,.18f,.44f,-.06f,.78f,.05f,.65f,.48f,.3f,.7f,.18f,.5f,-.18f,.5f};
        static readonly float[] shape1={-.65f,-.17f,-.5f,-.62f,-.2f,-.65f,-.25f,-.17f};
        static readonly float[] shape2={.65f,-.17f,.5f,-.62f,.2f,-.65f,.25f,-.17f};
        static readonly float[] shape3={0,-.32f,-.2f,-.52f,0,-.78f,.2f,-.52f};
        static readonly float[] shape4={-.28f,.28f,-.38f,.53f,-.2f,.76f,.2f,.76f,.38f,.53f,.28f,.28f};
        static readonly float[] shape5={-.18f,-.31f,-.12f,.54f,0,.86f,.12f,.54f,.18f,-.31f};
        static readonly float[] shape6={-.62f,.58f,0,.78f,.62f,.58f,.53f,-.3f,0,-.79f,-.53f,-.3f};
        static readonly float[] shape7={-.4f,.44f,0,.55f,.4f,.44f,.33f,-.18f,0,-.5f,-.33f,-.18f};
        static readonly float[] shape8={.09f,-.1f,.26f,.45f,.91f,.77f,.82f,.27f,.4f,.14f,.78f,.09f,.59f,-.27f,.32f,-.25f,.52f,-.36f,.31f,-.66f,.1f,-.3f};
        static readonly float[] shape9={-.42f,.16f,-.68f,-.6f,-.2f,-.47f,.01f,-.73f,.2f,-.47f,.66f,-.6f,.42f,.16f};
        public static void TintFrame(Image frame,Color tint)
        {
            if(!frame)return;
            if(!frameMaterial){
                var shader=Resources.Load<Shader>("Moonlit/Forge/NeutralEquipmentFrame");
                if(shader)frameMaterial=new Material(shader){name="Neutral equipment frame",hideFlags=HideFlags.HideAndDontSave};
            }
            if(frameMaterial)frame.material=frameMaterial;
            frame.color=tint;
        }
        public static Sprite Icon(int index)
        {
            index=Mathf.Clamp(index,0,8);if(icons[index])return icons[index];
            const int size=128;var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Empty slot pictogram "+index,filterMode=FilterMode.Bilinear,hideFlags=HideFlags.HideAndDontSave};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                int covered=0;
                for(int sy=0;sy<2;sy++)for(int sx=0;sx<2;sx++)
                    if(Shape(index,(x+(sx+.5f)/2)/size*2-1,(y+(sy+.5f)/2)/size*2-1))covered++;
                pixels[y*size+x]=new Color32(255,255,255,(byte)(covered*255/4));
            }
            tex.SetPixels32(pixels);tex.Apply(false,true);
            return icons[index]=Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);
        }
        static bool Ring(float x,float y,float cx,float cy,float outer,float inner)
        {float d=(x-cx)*(x-cx)+(y-cy)*(y-cy);return d<=outer*outer && d>=inner*inner;}
        static bool Line(float x,float y,float ax,float ay,float bx,float by,float width)
        {float dx=bx-ax,dy=by-ay,t=Mathf.Clamp01(((x-ax)*dx+(y-ay)*dy)/(dx*dx+dy*dy));return (x-ax-t*dx)*(x-ax-t*dx)+(y-ay-t*dy)*(y-ay-t*dy)<width*width;}
        static bool Polygon(float x,float y,params float[] points)
        {
            bool inside=false;
            for(int i=0,j=points.Length-2;i<points.Length;j=i,i+=2)
                if((points[i+1]>y)!=(points[j+1]>y) && x<(points[j]-points[i])*(y-points[i+1])/(points[j+1]-points[i+1])+points[i])inside=!inside;
            return inside;
        }
        static bool Shape(int i,float x,float y)
        {
            switch(i){
                case 0:return Polygon(x,y,shape0);
                case 1:return Ring(x,y,-.4f,-.15f,.27f,.16f)||Ring(x,y,.4f,-.15f,.27f,.16f)||Ring(x,y,-.4f,.39f,.14f,0)||Ring(x,y,.4f,.39f,.14f,0)||Line(x,y,-.4f,.3f,-.4f,.1f,.065f)||Line(x,y,.4f,.3f,.4f,.1f,.065f);
                case 2:return (Ring(x,y,0,-.03f,.65f,0)&&y>-.2f && !(y<.13f&&y>-.02f&&Mathf.Abs(x)<.44f))||Polygon(x,y,shape1)||Polygon(x,y,shape2);
                case 3:return (Ring(x,y,0,.22f,.58f,.47f)&&y>-.22f)||Line(x,y,-.44f,-.15f,0,-.46f,.06f)||Line(x,y,.44f,-.15f,0,-.46f,.06f)||Polygon(x,y,shape3);
                case 4:return Ring(x,y,0,-.16f,.48f,.31f)||Polygon(x,y,shape4);
                case 5:return Polygon(x,y,shape5)||Line(x,y,-.46f,-.32f,.46f,-.32f,.08f)||Line(x,y,0,-.32f,0,-.74f,.1f);
                case 6:return Polygon(x,y,shape6)&&!Polygon(x,y,shape7);
                case 7:return Polygon(Mathf.Abs(x),y,shape8);
                default:return (Ring(x,y,0,.22f,.43f,0)||Polygon(x,y,shape9))&&!Ring(x,y,-.16f,.2f,.085f,0)&&!Ring(x,y,.16f,.2f,.085f,0);
            }
        }
    }
}
