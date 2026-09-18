using UnityEngine;
using UnityEngine.U2D.Animation;

namespace Moonlit.UI
{
    public sealed class CompanionActor : MonoBehaviour
    {
        public CompanionRigType rigType;
        public Transform skeletonRoot, saddle;
        public Transform[] bones;
        public SpriteSkin[] skins;
        public Animator animator;
        public float shadowWidth=1;
        public float groundY;
        string mode;
        Transform shadow;
        public void InitializeShadow(Transform parent)
        {
            CombatGroundShadow.Create(parent,transform);
            shadow=parent.GetChild(parent.childCount-1);
            shadow.GetComponent<CombatGroundShadow>().enabled=false;
            if(shadow) shadow.localScale=new Vector3(shadowWidth/2.9f,.65f,1);
        }
        public void SetMotion(string state)
        {
            if(!animator || mode==state)return;
            mode=state;animator.Play(state,0,0);
        }
        public Transform Bone(string boneName)
        {
            if(bones!=null)foreach(var bone in bones)if(bone && bone.name==boneName)return bone;
            return null;
        }
        void LateUpdate() { if(shadow)shadow.position=new Vector3(transform.position.x,groundY+.03f,transform.position.z+1); }
        void OnDisable() { if(shadow)shadow.gameObject.SetActive(false); }
        void OnEnable() { if(shadow)shadow.gameObject.SetActive(true); }
        void OnDestroy() { if(shadow)Destroy(shadow.gameObject); }
    }
}
