using UnityEngine;

namespace Moonlit.UI
{
    public sealed class FlatCompanionActor : MonoBehaviour
    {
        public int Category { get; private set; }
        public int Variant { get; private set; }
        public SpriteRenderer Illustration { get; private set; }
        public Transform saddle { get; private set; }
        public float groundY;
        Rect visibleLocal;
        Transform shadow;
        public Bounds VisibleBounds
        {
            get {
                var min=Illustration.transform.TransformPoint(visibleLocal.min);
                var max=Illustration.transform.TransformPoint(visibleLocal.max);
                return new Bounds((min+max)*.5f,new Vector3(Mathf.Abs(max.x-min.x),Mathf.Abs(max.y-min.y),.01f));
            }
        }
        public void Initialize(int category,int variant,Sprite sprite,FlatCompanionCatalog.ArtBounds alpha)
        {
            Category=category;Variant=variant;
            var art=new GameObject("Whole companion illustration");art.transform.SetParent(transform,false);art.layer=30;
            Illustration=art.AddComponent<SpriteRenderer>();Illustration.sprite=sprite;
            Illustration.sortingOrder=category==2?-10:-20-variant;
            Illustration.flipX=false;Illustration.flipY=false;
            Rect pixels=alpha!=null && alpha.width>0 && alpha.height>0?
                new Rect(alpha.x,alpha.y,alpha.width,alpha.height):sprite.rect;
            visibleLocal=new Rect((pixels.x-sprite.rect.x-sprite.pivot.x)/sprite.pixelsPerUnit,
                (pixels.y-sprite.rect.y-sprite.pivot.y)/sprite.pixelsPerUnit,
                pixels.width/sprite.pixelsPerUnit,pixels.height/sprite.pixelsPerUnit);
            float height=category==2?2.45f:1.2f;
            float scale=height/Mathf.Max(.01f,visibleLocal.height);
            art.transform.localScale=Vector3.one*scale;
            art.transform.localPosition=new Vector3(-visibleLocal.center.x*scale,-visibleLocal.yMin*scale,0);
            saddle=new GameObject("Simple back anchor").transform;saddle.SetParent(transform,false);
            saddle.localPosition=new Vector3((Mathf.Clamp01(alpha?.backX??.5f)-.5f)*visibleLocal.width*scale,
                Mathf.Clamp01(alpha?.backY??.72f)*height,0);
            CombatGroundShadow.Create(transform.parent,transform);
            shadow=transform.parent.Find(name+" ground shadow");
            if(shadow) {
                shadow.GetComponent<CombatGroundShadow>().enabled=false;
                shadow.localScale=new Vector3(VisibleBounds.size.x/2.9f,.62f,1);
            }
            groundY=transform.position.y;RefreshShadow();
        }
        public void RefreshShadow()
        {
            if(shadow)shadow.position=new Vector3(transform.position.x,groundY+.03f,transform.position.z+1);
        }
        void LateUpdate(){RefreshShadow();}
        void OnDisable(){if(shadow)shadow.gameObject.SetActive(false);}
        void OnEnable(){if(shadow)shadow.gameObject.SetActive(true);}
        void OnDestroy(){if(shadow)Destroy(shadow.gameObject);}
    }
}
