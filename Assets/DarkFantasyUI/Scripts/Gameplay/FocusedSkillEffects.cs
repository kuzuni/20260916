using System.Collections;
using UnityEngine;

namespace Moonlit.UI
{
    public sealed partial class PrimitiveSkillEffects
    {
        public float EarlyPlaybackTimeOverride { get; set; } = -1;
        // BattleRuntime supplies the real profile rectangle through the same UI-to-world projection as its actor clearance.
        public Rect FocusedFoodHeaderBounds { get; set; }
        public const float FocusedFoodBaseSize=1.65f,FocusedFoodPeak=1.28f,FocusedFoodHeadGap=.3f,FocusedFoodHeaderGap=.08f;
        public float FocusedFoodSize(Sprite art,Bounds head)
        {
            if(!art || FocusedFoodHeaderBounds.width<=0 || FocusedFoodHeaderBounds.height<=0)return FocusedFoodBaseSize;
            float dimension=Mathf.Max(.01f,Mathf.Max(art.bounds.size.x,art.bounds.size.y));
            float halfWidth=FocusedFoodBaseSize*FocusedFoodPeak*art.bounds.size.x/dimension/2;
            if(head.center.x+halfWidth<=FocusedFoodHeaderBounds.xMin || head.center.x-halfWidth>=FocusedFoodHeaderBounds.xMax)
                return FocusedFoodBaseSize;
            float availableHeight=Mathf.Max(0,FocusedFoodHeaderBounds.yMin-head.max.y-FocusedFoodHeadGap-FocusedFoodHeaderGap);
            return Mathf.Min(FocusedFoodBaseSize,availableHeight*dimension/Mathf.Max(.01f,art.bounds.size.y*FocusedFoodPeak));
        }
        readonly System.Collections.Generic.List<Object> focusedAssets=new System.Collections.Generic.List<Object>();
        static bool FocusedHeadBounds(Transform motion,out Bounds bounds)
        {
            if(motion)foreach(var renderer in motion.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.enabled&&renderer.sprite&&renderer.sprite.name=="머리"){bounds=renderer.bounds;return true;}
            bounds=default;return false;
        }
        public static Vector3 FocusedTarget(Transform motion,Vector3 bodyPoint)
        {
            if(!FocusedHeadBounds(motion,out var head))return bodyPoint;
            return new Vector3(head.center.x,head.center.y-.5f,bodyPoint.z);
        }
        void ReleaseFocusedAsset(Object asset)
        {
            focusedAssets.Remove(asset);if(asset)Destroy(asset);
        }
        void ClearFocusedAssets()
        {
            foreach(var asset in focusedAssets)if(asset)Destroy(asset);
            focusedAssets.Clear();
        }
        IEnumerator AnimateEarly(int tier,int variant,Vector3 source,Vector3 target,Transform sourceAnchor,Transform targetAnchor,bool preview)
        {
            var art=Art(tier,variant);if(!art)yield break;
            int version=generation;IsPlaying=true;PlaybackElapsed=0;EarlyPlaybackTimeOverride=-1;
            var sourceOffset=sourceAnchor?source-sourceAnchor.position:Vector3.zero;
            var targetOffset=targetAnchor?target-targetAnchor.position:Vector3.zero;
            var root=NewRoot(EffectName(tier,variant),true);
            int count=variant==1?(tier==0?8:5):1;
            var objects=new SpriteRenderer[count];
            for(int i=0;i<count;i++) {
                string name=variant==0?"Overhead food":variant==2?(tier==0?"Single rock":"Single sword"):(tier==0?"Orbit bone ":"Curved arrow ")+i;
                objects[i]=Render(root.transform,art,name,150+i);
            }
            float[] arrivals=SkillChoreography.HitTimes(tier,variant);
            float end=variant==0?SixSkillChoreography.BuffEnd:arrivals[arrivals.Length-1]+.03f;
            int next=0;bool aura=false;
            for(float elapsed=0;;elapsed+=Time.deltaTime) {
                float time=EarlyPlaybackTimeOverride>=0?EarlyPlaybackTimeOverride:elapsed;
                if(EarlyPlaybackTimeOverride<0&&time>end)break;
                if(version!=generation||!root)yield break;
                if(sourceAnchor)source=sourceAnchor.position+sourceOffset;
                if(targetAnchor)target=FocusedTarget(targetAnchor,targetAnchor.position+targetOffset);
                PlaybackElapsed=time;
                if(variant==0) {
                    var pose=SixSkillChoreography.Food(source,time);
                    float foodSize=FocusedFoodBaseSize;
                    if(FocusedHeadBounds(sourceAnchor,out var head)) {
                        foodSize=FocusedFoodSize(art,head);
                        float halfHeight=foodSize*FocusedFoodPeak*art.bounds.size.y/Mathf.Max(art.bounds.size.x,art.bounds.size.y)/2;
                        var overhead=new Vector3(head.center.x,head.max.y+FocusedFoodHeadGap+halfHeight,source.z-1);
                        pose=new SkillVisualPose(overhead,pose.rotation,pose.scale.x,pose.scale.y,pose.alpha);
                    }
                    Pose(objects[0],pose,foodSize);
                    objects[0].enabled=time<SixSkillChoreography.FoodVanishTime;
                    if(!aura&&time>=SixSkillChoreography.FoodVanishTime) {
                        aura=true;StartCoroutine(GreenHealingAura(source,sourceAnchor,sourceOffset));
                    }
                } else {
                    for(int i=0;i<count;i++) {
                        SkillVisualPose pose=variant==1?(tier==0?SixSkillChoreography.Bone(source,target,i,time):SixSkillChoreography.Arrow(source,target,i,time)):
                            tier==0?SixSkillChoreography.Rock(source,target,time):SixSkillChoreography.Sword(source,target,time);
                        Pose(objects[i],pose,variant==1?(tier==0?1.8f:2.0f):3.3f);
                        objects[i].enabled=pose.alpha>0;
                    }
                    while(next<arrivals.Length&&time>=arrivals[next]) {
                        if(preview)PlaySkillHit(tier,variant,next,source,target,true);
                        next++;
                    }
                }
                yield return null;
            }
            if(version!=generation||!root)yield break;
            if(preview&&variant>0)while(next<arrivals.Length)PlaySkillHit(tier,variant,next++,source,target,true);
            PlaybackElapsed=end;IsPlaying=false;playbacks.Remove(root);Destroy(root);
        }
        IEnumerator GreenHealingAura(Vector3 source,Transform anchor,Vector3 offset)
        {
            var art=Resources.Load<Sprite>(HitDustKey);if(!art)yield break;
            var root=NewRoot("Green healing body aura");
            var glow=Render(root.transform,art,"Healing body glow",140);
            for(float t=0;;t+=Time.deltaTime) {
                if(!root)yield break;
                float age=EarlyPlaybackTimeOverride>=0?Mathf.Max(0,EarlyPlaybackTimeOverride-SixSkillChoreography.FoodVanishTime):t;
                if(age>=.65f)break;
                if(anchor)source=anchor.position+offset;
                float p=age/.65f;
                float alpha=Mathf.Sin(p*Mathf.PI)*.75f;
                Pose(glow,new SkillVisualPose(source+new Vector3(0,-.6f,-.8f),0,1.1f,1.7f,alpha),2.4f);
                Tint(glow,new Color(.2f,1,.3f));
                yield return null;
            }
            if(root)Destroy(root);
        }
        IEnumerator FocusedContact(int tier,int variant,int hit,Vector3 target)
        {
            var art=Resources.Load<Sprite>(BasicSlashKey);
            if(!art)yield break;
            var root=NewRoot("Skill contact "+tier+" "+variant+" hit "+hit);
            var slash=Render(root.transform,art,variant==2?"Medieval sword slash":"Head contact spark",156);
            float seconds=variant==2?.3f:.16f;
            for(float t=0;;t+=Time.deltaTime) {
                if(!root)yield break;
                float age=EarlyPlaybackTimeOverride>=0?Mathf.Max(0,EarlyPlaybackTimeOverride-SkillChoreography.HitTimes(tier,variant)[hit]):t;
                if(age>=seconds)break;
                float p=age/seconds;
                Pose(slash,new SkillVisualPose(SixSkillChoreography.Head(target)+Vector3.back*1.2f,
                    variant==2?Mathf.Lerp(65,-55,p):35,1+.25f*p,variant==2?1:.35f,1-p),variant==2?3.5f:1.15f);
                yield return null;
            }
            if(root)Destroy(root);
        }
        static void SplitRockTriangle(Vector2 a,Vector2 b,Vector2 c,Vector2 ua,Vector2 ub,Vector2 uc,int depth,
            System.Collections.Generic.List<Vector2> positions,System.Collections.Generic.List<Vector2> uv)
        {
            if(depth==0){positions.AddRange(new[]{a,b,c});uv.AddRange(new[]{ua,ub,uc});return;}
            var ab=(a+b)/2;var bc=(b+c)/2;var ca=(c+a)/2;
            var uab=(ua+ub)/2;var ubc=(ub+uc)/2;var uca=(uc+ua)/2;
            SplitRockTriangle(a,ab,ca,ua,uab,uca,depth-1,positions,uv);
            SplitRockTriangle(ab,b,bc,uab,ub,ubc,depth-1,positions,uv);
            SplitRockTriangle(ca,bc,c,uca,ubc,uc,depth-1,positions,uv);
            SplitRockTriangle(ab,bc,ca,uab,ubc,uca,depth-1,positions,uv);
        }
        IEnumerator RockExplosion(Sprite art,Vector3 target)
        {
            var root=NewRoot("Skill contact 0 2 hit 0");
            var original=art.vertices;var originalUv=art.uv;var originalTriangles=art.triangles;
            var splitPositions=new System.Collections.Generic.List<Vector2>();
            var splitUv=new System.Collections.Generic.List<Vector2>();
            for(int i=0;i<originalTriangles.Length;i+=3) {
                int a=originalTriangles[i],b=originalTriangles[i+1],c=originalTriangles[i+2];
                SplitRockTriangle(original[a],original[b],original[c],originalUv[a],originalUv[b],originalUv[c],2,splitPositions,splitUv);
            }
            var vertices=splitPositions.ToArray();var uv=splitUv.ToArray();var indices=new int[vertices.Length];
            for(int i=0;i<indices.Length;i++)indices[i]=i;
            const int pieces=6;
            float scale=3.3f/Mathf.Max(art.bounds.size.x,art.bounds.size.y);
            var transforms=new Transform[pieces];var centres=new Vector2[pieces];
            var material=new Material(catalog.effectMaterial);material.mainTexture=art.texture;focusedAssets.Add(material);
            var meshes=new System.Collections.Generic.List<Mesh>();
            for(int piece=0;piece<pieces;piece++) {
                var triangles=new System.Collections.Generic.List<int>();
                Vector2 centre=Vector2.zero;int count=0;
                for(int index=0;index<indices.Length;index+=3) {
                    var midpoint=(vertices[indices[index]]+vertices[indices[index+1]]+vertices[indices[index+2]])/3;
                    int sector=Mathf.Min(pieces-1,(int)((Mathf.Atan2(midpoint.y,midpoint.x)+Mathf.PI)/(2*Mathf.PI)*pieces));
                    if(sector!=piece)continue;
                    for(int n=0;n<3;n++){triangles.Add(indices[index+n]);centre+=vertices[indices[index+n]];count++;}
                }
                if(count==0)continue;
                centre/=count;centres[piece]=centre*scale;
                var go=new GameObject("Rock mesh fragment "+piece,typeof(MeshFilter),typeof(MeshRenderer));
                go.layer=30;go.transform.SetParent(root.transform,false);transforms[piece]=go.transform;
                var mesh=new Mesh();mesh.name="Actual rock texture fragment";
                var positions=new Vector3[vertices.Length];var colors=new Color[vertices.Length];
                for(int i=0;i<positions.Length;i++){positions[i]=(vertices[i]-centre)*scale;colors[i]=Color.white;}
                mesh.vertices=positions;mesh.uv=uv;mesh.colors=colors;mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
                go.GetComponent<MeshFilter>().sharedMesh=mesh;
                var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=155;
                meshes.Add(mesh);focusedAssets.Add(mesh);
            }
            for(float elapsed=0;;elapsed+=Time.deltaTime) {
                if(!root)yield break;
                float time=EarlyPlaybackTimeOverride>=0?Mathf.Max(0,EarlyPlaybackTimeOverride-SkillChoreography.HitTimes(0,2)[0]):elapsed;
                if(time>=.72f)break;
                float p=time/.72f;
                for(int i=0;i<pieces;i++)if(transforms[i]) {
                    var direction=centres[i].sqrMagnitude>.001f?centres[i].normalized:new Vector2(Mathf.Cos(i),Mathf.Sin(i));
                    transforms[i].position=SixSkillChoreography.Head(target)+Vector3.back+
                        (Vector3)(centres[i]+direction*(1.9f*time)+Vector2.up*(1.8f*time-4*time*time));
                    transforms[i].rotation=Quaternion.Euler(0,0,(i%2==0?1:-1)*p*155);
                    transforms[i].localScale=Vector3.one*(1-p*.8f);
                }
                material.color=new Color(1,1,1,1-p);
                yield return null;
            }
            ReleaseFocusedAsset(material);foreach(var mesh in meshes)ReleaseFocusedAsset(mesh);
            if(root)Destroy(root);
        }
    }
}
