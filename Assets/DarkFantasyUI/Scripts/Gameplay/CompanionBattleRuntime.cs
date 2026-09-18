using System.Collections.Generic;
using UnityEngine;

namespace Moonlit.UI
{
    // Whole PNG companions stay in their calm authored pose: no skinning, Animator, flip or procedural bob.
    [DefaultExecutionOrder(-50)]
    public sealed class CompanionBattleRuntime : MonoBehaviour
    {
        public readonly List<FlatCompanionActor> Pets=new List<FlatCompanionActor>();
        public FlatCompanionActor Mount { get; private set; }
        public Transform RiderHip => riderHip;
        Transform player,motion,rig,riderHip,playerShadow;
        Camera camera;
        Vector3 originalRigPosition;
        int signature=int.MinValue;
        public void Initialize(Transform actor,Camera renderCamera)
        {
            player=actor;motion=actor.Find("Motion");rig=motion.Find("PlayerRig");camera=renderCamera;
            originalRigPosition=rig.localPosition;
            foreach(var bone in rig.GetComponentsInChildren<Transform>(true))
                if(bone.name=="bone_1")riderHip=bone;
            playerShadow=player.parent.Find(player.name+" ground shadow");
            RefreshEquipped();
        }
        public void RefreshEquipped()
        {
            if(!player)return;
            var pets=CollectionProgression.Equipped(1);var mounts=CollectionProgression.Equipped(2);
            int next=17;foreach(var pet in pets)next=next*31+pet.Id+1;
            next=next*31+(mounts.Count>0?mounts[0].Id+1:0);
            if(next==signature)return;signature=next;
            foreach(var pet in Pets)if(pet){pet.gameObject.SetActive(false);Destroy(pet.gameObject);}Pets.Clear();
            RestoreRider();
            if(Mount){Mount.gameObject.SetActive(false);Destroy(Mount.gameObject);}Mount=null;
            foreach(var entry in pets)
                if(entry.grade==0) {
                    var pet=FlatCompanionCatalog.Create(1,entry.variant,transform,"Pet "+entry.variant);
                    if(pet)Pets.Add(pet);
                }
            if(mounts.Count>0 && mounts[0].grade==0)
                Mount=FlatCompanionCatalog.Create(2,mounts[0].variant,transform,"Mount "+mounts[0].variant);
            if(playerShadow)playerShadow.gameObject.SetActive(!Mount);
        }
        void LateUpdate()
        {
            if(!player || !rig)return;
            RefreshEquipped();
            if(playerShadow)playerShadow.gameObject.SetActive(!Mount);
            float left=camera?camera.ViewportToWorldPoint(new Vector3(.03f,0,12)).x:player.position.x-8;
            for(int i=0;i<Pets.Count;i++)
            {
                var pet=Pets[i];
                float x=Mathf.Max(left+pet.VisibleBounds.extents.x+.08f,motion.position.x-1.3f-i*.9f);
                float lane=i*.30f;
                pet.groundY=player.position.y+lane;
                pet.transform.position=new Vector3(x,pet.groundY,player.position.z+.2f);
            }
            if(Mount && riderHip && Mount.saddle)
            {
                Mount.groundY=player.position.y;
                Mount.transform.position=new Vector3(motion.position.x,player.position.y,player.position.z+.1f);
                rig.localPosition=originalRigPosition;
                Vector3 shift=Mount.saddle.position-riderHip.position;shift.z=0;
                rig.position+=shift;
            }
        }
        void RestoreRider()
        {
            if(rig)rig.localPosition=originalRigPosition;
            if(playerShadow)playerShadow.gameObject.SetActive(true);
        }
        void OnDisable(){RestoreRider();}
        void OnDestroy(){RestoreRider();}
    }
}
