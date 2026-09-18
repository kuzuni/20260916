using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Moonlit.UI
{
    // Runs before head HUD updates: rider pose, saddle and companions are settled for the rendered frame.
    [DefaultExecutionOrder(-50)]
    public sealed class CompanionBattleRuntime : MonoBehaviour
    {
        public readonly List<CompanionActor> Pets=new List<CompanionActor>();
        public CompanionActor Mount { get; private set; }
        public Transform RiderHip => riderHip;
        Transform player, motion, rig, riderHip, playerShadow;
        Camera camera;
        CompanionRigCatalog catalog;
        Vector3 originalRigPosition;
        readonly List<Transform> legs=new List<Transform>();
        readonly List<Quaternion> unseated=new List<Quaternion>(), seated=new List<Quaternion>();
        int signature=int.MinValue;
        Vector3 lastPlayerPosition;
        bool wasMounted;
        public void Initialize(Transform actor,Camera renderCamera)
        {
            player=actor;motion=actor.Find("Motion");rig=motion.Find("PlayerRig");camera=renderCamera;
            originalRigPosition=rig.localPosition;
            foreach(var bone in rig.GetComponentsInChildren<Transform>(true))
            {
                if(bone.name=="bone_1")riderHip=bone;
                if(bone.name=="bone_3" || bone.name=="bone_4" || bone.name=="bone_9" || bone.name=="bone_10")
                {legs.Add(bone);unseated.Add(bone.localRotation);seated.Add(bone.localRotation);}
            }
            playerShadow=player.parent.Find(player.name+" ground shadow");
            catalog=CompanionRigCatalog.Load();lastPlayerPosition=player.position;
            RefreshEquipped();
        }
        public void RefreshEquipped()
        {
            if(!catalog || !player)return;
            var pets=CollectionProgression.Equipped(1);var mounts=CollectionProgression.Equipped(2);
            int next=17;
            foreach(var pet in pets)next=next*31+pet.Id+1;
            next=next*31+(mounts.Count>0?mounts[0].Id+1:0);
            if(next==signature)return;signature=next;
            foreach(var pet in Pets)if(pet){pet.gameObject.SetActive(false);Destroy(pet.gameObject);}Pets.Clear();
            RestoreRider();
            if(Mount){Mount.gameObject.SetActive(false);Destroy(Mount.gameObject);}Mount=null;
            foreach(var entry in pets)
            {
                var data=catalog.Find(1,entry.grade,entry.variant);
                if(data?.prefab)Pets.Add(Spawn(data, "Pet "+entry.variant));
            }
            if(mounts.Count>0)
            {
                var data=catalog.Find(2,mounts[0].grade,mounts[0].variant);
                if(data?.prefab)Mount=Spawn(data,"Mount "+mounts[0].variant);
            }
            if(playerShadow)playerShadow.gameObject.SetActive(!Mount);
        }
        CompanionActor Spawn(CompanionRigEntry data,string label)
        {
            var instance=Instantiate(data.prefab,transform,false);
            instance.name=label;
            foreach(var item in instance.GetComponentsInChildren<Transform>(true))item.gameObject.layer=30;
            var actor=instance.GetComponent<CompanionActor>();actor.InitializeShadow(transform);
            var group=instance.GetComponent<SortingGroup>();group.sortingOrder=data.category==2?-10:-20;
            return actor;
        }
        void LateUpdate()
        {
            if(!player || !rig)return;
            RefreshEquipped();
            if(playerShadow)playerShadow.gameObject.SetActive(!Mount);
            bool moving=(player.position-lastPlayerPosition).sqrMagnitude>.0001f;
            lastPlayerPosition=motion.position;
            var state=player.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0);
            bool attacking=state.IsName("Basic") || state.IsName("Weak") || state.IsName("Strong");
            bool dead=state.IsName("Death");
            float left=camera?camera.ViewportToWorldPoint(new Vector3(.035f,0,12)).x:player.position.x-3;
            for(int i=0;i<Pets.Count;i++)
            {
                var pet=Pets[i];
                float behind=pet.rigType==CompanionRigType.Humanoid?1.8f:pet.rigType==CompanionRigType.Bird?1.6f:1.1f;
                float lane=pet.rigType==CompanionRigType.Humanoid?.42f:pet.rigType==CompanionRigType.Bird?1.5f:0;
                float x=Mathf.Max(left+.8f,motion.position.x-behind);
                pet.groundY=player.position.y+(pet.rigType==CompanionRigType.Humanoid?.42f:0);
                pet.transform.position=new Vector3(x,player.position.y+lane,player.position.z+.2f);
                pet.SetMotion(dead?"Death":attacking?"Attack":moving?"Walk":"Idle");
            }
            if(Mount && riderHip && Mount.saddle)
            {
                Mount.groundY=player.position.y;
                Mount.transform.position=new Vector3(motion.position.x,player.position.y,player.position.z+.1f);
                Mount.SetMotion(dead?"Death":moving || attacking?"Walk":"Idle");
                for(int i=0;i<legs.Count;i++)
                {
                    var current=legs[i].localRotation;
                    if(wasMounted && Quaternion.Angle(current,seated[i])<.01f)current=unseated[i];
                    unseated[i]=current;
                    float bend=legs[i].name=="bone_3" || legs[i].name=="bone_4"?62:-48;
                    seated[i]=current*Quaternion.Euler(0,0,bend);legs[i].localRotation=seated[i];
                }
                rig.localPosition=originalRigPosition;
                Vector3 shift=Mount.saddle.position-riderHip.position;shift.z=0;
                rig.position+=shift;wasMounted=true;
            }
        }
        void RestoreRider()
        {
            if(!rig)return;
            rig.localPosition=originalRigPosition;
            if(wasMounted)for(int i=0;i<legs.Count;i++)
                if(legs[i] && Quaternion.Angle(legs[i].localRotation,seated[i])<.01f)legs[i].localRotation=unseated[i];
            wasMounted=false;
            if(playerShadow)playerShadow.gameObject.SetActive(true);
        }
        void OnDisable() { RestoreRider(); }
        void OnDestroy() { RestoreRider(); }
    }
}
