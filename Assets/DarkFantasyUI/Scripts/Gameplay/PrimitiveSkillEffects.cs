using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Moonlit.UI
{
    // Authored thirty-motion choreography. Combat alone decides actual damage and contact feedback.
    public sealed class PrimitiveSkillEffects : MonoBehaviour
    {
        public const float AttackFlightDuration=SkillChoreography.AttackLead;
        const float VisualScale=2f;
        BattleAssetCatalog catalog;
        readonly List<GameObject> owned=new List<GameObject>(),playbacks=new List<GameObject>();
        int generation;
        public float PlaybackElapsed { get; private set; }
        public bool IsPlaying { get; private set; }
        static readonly Color[] Colors={
            new Color(.75f,.55f,.3f),new Color(1,.78f,.34f),new Color(.65f,1,.25f),
            new Color(.85f,.6f,.25f),new Color(.15f,.95f,1),new Color(.95f,.3f,1),
            new Color(.6f,.4f,1),new Color(.4f,1,.8f),new Color(.5f,1,.3f),new Color(1,.88f,.45f)
        };
        public void Initialize(BattleAssetCatalog assets){catalog=assets;}
        public static string ResourceKey(int tier,int variant)=>SkillCatalog.IconKey(Mathf.Clamp(tier,0,9),Mathf.Clamp(variant,0,2));
        public static Sprite SkillSprite(int tier,int variant)=>Resources.Load<Sprite>(ResourceKey(tier,variant));
        Sprite Art(int tier,int variant)
        {
            var art=SkillSprite(tier,variant);
            if(!art&&tier==0&&catalog)art=variant==0?catalog.buffSprite:variant==1?catalog.weakSprite:catalog.strongSprite;
            return art;
        }
        // Physical objects stay whole in flight; contact particles use real dust/spark textures.
        Sprite ContactArt(int tier,int variant,Sprite projectile)
            => tier<=3 ? Resources.Load<Sprite>(tier>0&&variant==1?BasicSlashKey:HitDustKey) : projectile;
        Sprite AccentArt(int tier,Sprite buff)
            => tier<=3 ? Resources.Load<Sprite>(tier==1?BasicSlashKey:HitDustKey) : buff;
        static void Tint(SpriteRenderer renderer,Color tint)
            => renderer.color=new Color(tint.r,tint.g,tint.b,renderer.color.a);
        void OnDisable()
        {
            generation++;IsPlaying=false;StopAllCoroutines();
            foreach(var item in owned)if(item)Destroy(item);
            owned.Clear();playbacks.Clear();
        }
        public void CancelSkillPlayback()
        {
            generation++;IsPlaying=false;
            foreach(var root in playbacks)if(root)Destroy(root);
            playbacks.Clear();
        }
        public void Play(int variant,Vector3 source,Vector3 target)=>Play(0,variant,source,target);
        public void Play(int tier,int variant,Vector3 source,Vector3 target,Transform sourceAnchor=null,Transform targetAnchor=null,bool burstOnArrival=true)
        {
            if(catalog)StartCoroutine(Animate(Mathf.Clamp(tier,0,9),Mathf.Clamp(variant,0,2),source,target,sourceAnchor,targetAnchor,burstOnArrival));
        }
        static string EffectName(int tier,int variant)=>tier==0?new[]{"Primitive ancestral blessing","Primitive stone crescent","Primitive falling boulder"}[variant]:"Era "+tier+" skill "+variant;
        GameObject NewRoot(string name,bool playback=false)
        {
            var root=new GameObject(name);root.layer=30;root.transform.SetParent(transform,false);
            owned.RemoveAll(item=>!item);owned.Add(root);
            if(playback){playbacks.RemoveAll(item=>!item);playbacks.Add(root);}
            return root;
        }
        SpriteRenderer Render(Transform parent,Sprite art,string name,int order)
        {
            var go=new GameObject(name);go.layer=30;go.transform.SetParent(parent,false);
            var renderer=go.AddComponent<SpriteRenderer>();renderer.sprite=art;
            renderer.sharedMaterial=catalog.effectMaterial;renderer.sortingOrder=order;return renderer;
        }
        void Pose(SpriteRenderer renderer,SkillVisualPose pose,float size)
        {
            float scale=size/Mathf.Max(.01f,Mathf.Max(renderer.sprite.bounds.size.x,renderer.sprite.bounds.size.y));
            renderer.transform.position=pose.position;renderer.transform.rotation=Quaternion.Euler(0,0,pose.rotation);
            renderer.transform.localScale=new Vector3(pose.scale.x*scale,pose.scale.y*scale,scale);
            renderer.color=new Color(1,1,1,pose.alpha);
        }
        public static Vector3 Position(int tier,int variant,Vector3 source,Vector3 target,float t)
            =>SkillChoreography.Sample(tier,variant,source,target,t).position;
        IEnumerator Animate(int tier,int variant,Vector3 source,Vector3 target,Transform sourceAnchor,Transform targetAnchor,bool preview)
        {
            var art=Art(tier,variant);
            if(!art){Debug.LogWarning("[Moonlit] Missing illustrated skill "+ResourceKey(tier,variant));yield break;}
            int version=generation;IsPlaying=true;PlaybackElapsed=0;
            Vector3 sourceOffset=sourceAnchor?source-sourceAnchor.position:Vector3.zero;
            Vector3 targetOffset=targetAnchor?target-targetAnchor.position:Vector3.zero;
            var root=NewRoot(EffectName(tier,variant),true);
            var renderer=Render(root.transform,art,"Choreographed "+SkillCatalog.Name(tier,variant),150);
            var trail=renderer.gameObject.AddComponent<TrailRenderer>();trail.sharedMaterial=catalog.effectMaterial;
            trail.time=tier==6?.55f:.5f;trail.startWidth=(variant==2?.28f:.12f)*VisualScale*2;trail.endWidth=0;
            trail.startColor=Colors[tier];trail.endColor=new Color(Colors[tier].r,Colors[tier].g,Colors[tier].b,0);
            trail.sortingOrder=148;trail.minVertexDistance=.025f;trail.emitting=variant!=0||tier>=4;
            var accents=new List<SpriteRenderer>();
            if(variant==0)for(int i=0;i<4;i++)accents.Add(Render(root.transform,AccentArt(tier,art),"Buff accent "+i,149));
            float[] hits=SkillChoreography.HitTimes(tier,variant);
            float duration=variant==0?SkillChoreography.BuffDuration(tier):AttackFlightDuration+hits[hits.Length-1];
            float time=0;int nextHit=0,previousFlight=-1;
            while(time<=duration&&version==generation&&root)
            {
                if(sourceAnchor)source=sourceAnchor.position+sourceOffset;
                if(targetAnchor)target=targetAnchor.position+targetOffset;
                PlaybackElapsed=time;
                if(variant==0) {
                    float t=time/duration;
                    Pose(renderer,SkillChoreography.Sample(tier,0,source,target,t),2.4f);
                    for(int i=0;i<accents.Count;i++) {
                        Pose(accents[i],SkillChoreography.BuffAccent(tier,i,t,source),2.4f);
                        if(tier<=3)Tint(accents[i],Colors[tier]);
                    }
                } else {
                    while(nextHit<hits.Length&&time>=AttackFlightDuration+hits[nextHit]) {
                        if(preview)PlaySkillHit(tier,variant,nextHit,source,target,true);
                        nextHit++;
                    }
                    int flight=Mathf.Min(nextHit,hits.Length-1);
                    float from=flight==0?0:AttackFlightDuration+hits[flight-1];
                    float to=AttackFlightDuration+hits[flight];
                    float t=Mathf.Clamp01((time-from)/Mathf.Max(.001f,to-from));
                    if(previousFlight!=flight){trail.Clear();previousFlight=flight;}
                    float size=(variant==1?.9f:1.65f)*VisualScale;
                    if(tier==3&&variant==1)size=4.4f;
                    Pose(renderer,SkillChoreography.Sample(tier,variant,source,target,t,flight),size);
                }
                yield return null;time+=Time.deltaTime;
            }
            if(version!=generation||!root)yield break;
            if(preview&&variant>0)while(nextHit<hits.Length){PlaySkillHit(tier,variant,nextHit++,source,target,true);}
            PlaybackElapsed=duration;IsPlaying=false;
            renderer.enabled=false;trail.emitting=false;
            foreach(var accent in accents)if(accent)accent.enabled=false;
            playbacks.Remove(root);Destroy(root,.55f);
        }
        public void PlaySkillHit(int tier,int variant,int hitIndex,Vector3 source,Vector3 target,bool landed=true)
        {
            if(!catalog||!landed)return;
            tier=Mathf.Clamp(tier,0,9);variant=Mathf.Clamp(variant,1,2);
            var art=Art(tier,variant);if(!art)return;
            StartCoroutine(Contact(ContactArt(tier,variant,art),tier,variant,hitIndex,target));
            // Real stones splinter. This remains the only skill using the original stone-fragment burst.
            if(tier==0)Burst(art,tier,variant,target+Vector3.back,Colors[tier]);
            else if(tier==2||tier==3)ContactSmoke(tier,variant,target);
        }
        public void PlayImpact(int tier,int variant,Vector3 point)=>PlaySkillHit(tier,variant,0,point-Vector3.right,point,true);
        IEnumerator Contact(Sprite art,int tier,int variant,int hit,Vector3 point)
        {
            var root=NewRoot("Skill contact "+tier+" "+variant+" hit "+hit);
            int count=variant==1?3:5;
            var pieces=new SpriteRenderer[count];
            for(int i=0;i<count;i++)pieces[i]=Render(root.transform,art,"Impact piece "+i,152+i);
            float duration=variant==1?.38f:.52f;
            for(float elapsed=0;elapsed<duration;elapsed+=Time.deltaTime) {
                if(!root)yield break;
                for(int i=0;i<count;i++) {
                    Pose(pieces[i],SkillChoreography.Impact(tier,variant,i,elapsed/duration,point,hit),2.5f);
                    if(tier<=3)Tint(pieces[i],Colors[tier]);
                }
                yield return null;
            }
            if(root)Destroy(root);
        }
        void ContactSmoke(int tier,int variant,Vector3 point)
        {
            var art=Resources.Load<Sprite>(HitDustKey);if(!art)return;
            var go=NewRoot(tier==2?"Gunpowder smoke recoil":"Rolling armored blast smoke");
            var particles=go.AddComponent<ParticleSystem>();go.transform.position=point+Vector3.back;
            particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=particles.main;main.loop=false;main.duration=.5f;main.startLifetime=tier==2?.3f:.5f;
            main.startSpeed=tier==2?1.8f:.7f;main.startSize=variant==1?.7f:1.6f;
            main.startColor=tier==2?new Color(.8f,.68f,.46f,.75f):new Color(.62f,.65f,.69f,.8f);
            main.gravityModifier=tier==2?.15f:-.3f;main.simulationSpace=ParticleSystemSimulationSpace.World;
            var shape=particles.shape;shape.shapeType=tier==2?ParticleSystemShapeType.Cone:ParticleSystemShapeType.Box;
            shape.radius=.2f;shape.scale=tier==2?Vector3.one:new Vector3(1.2f,.15f,.1f);
            var emission=particles.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,(short)(variant==1?5:9))});
            var sheet=particles.textureSheetAnimation;sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Sprites;sheet.AddSprite(art);
            var size=particles.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,.3f,1,1.5f));
            var color=particles.colorOverLifetime;color.enabled=true;
            var fade=new Gradient();fade.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(.8f,0),new GradientAlphaKey(0,1)});color.color=fade;
            var render=particles.GetComponent<ParticleSystemRenderer>();var material=new Material(catalog.effectMaterial);material.mainTexture=art.texture;render.sharedMaterial=material;render.sortingOrder=151;
            particles.Play();Destroy(material,1);Destroy(go,1);
        }
        public const string HitDustKey = "Moonlit/Combat/BasicHitDust-v1";
        public const string BasicSlashKey = "Moonlit/Combat/BasicSlash-v1";
        public const float BasicSlashDuration = .24f;

        // Shared victim feedback is dust, never a primitive stone skill reused as a basic hit.
        public void PlayHitDust(Vector3 point)
        {
            var art = Resources.Load<Sprite>(HitDustKey);
            if (!catalog || !art) return;
            var puff = new GameObject("Basic hit dust puff").AddComponent<ParticleSystem>();
            owned.RemoveAll(item => !item); owned.Add(puff.gameObject);
            puff.gameObject.layer = 30; puff.transform.SetParent(transform, false);
            puff.transform.position = point + Vector3.back;
            puff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = puff.main; main.loop = false; main.duration = .4f;
            main.startLifetime = .38f; main.startSpeed = 1.15f; main.startSize = 1.65f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            main.startColor = new Color(1, .92f, .78f, .8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 8;
            var emission = puff.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)6) });
            var shape = puff.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .14f;
            var sheet = puff.textureSheetAnimation; sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites; sheet.AddSprite(art);
            var size = puff.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, .45f, 1, 1.2f));
            var color = puff.colorOverLifetime; color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(.6f, .35f), new GradientAlphaKey(0, 1) });
            color.color = fade;
            var renderer = puff.GetComponent<ParticleSystemRenderer>();
            var material = new Material(catalog.effectMaterial); material.mainTexture = art.texture;
            renderer.sharedMaterial = material; renderer.sortingOrder = 153;
            puff.Play(); Destroy(material, .8f); Destroy(puff.gameObject, .8f);
        }

        // Called with the basic swing event even on evasion, including the one permitted double attack.
        public void PlayBasicSlash(Vector3 point, bool rightward)
        {
            var art = Resources.Load<Sprite>(BasicSlashKey);
            if (catalog && art) StartCoroutine(BasicSlash(art, point, rightward));
        }
        IEnumerator BasicSlash(Sprite art, Vector3 point, bool rightward)
        {
            var arc = new GameObject("Basic sword slash");
            owned.RemoveAll(item => !item); owned.Add(arc);
            arc.layer = 30; arc.transform.SetParent(transform, false);
            var renderer = arc.AddComponent<SpriteRenderer>();
            renderer.sprite = art; renderer.sharedMaterial = catalog.effectMaterial; renderer.sortingOrder = 154;
            float direction = rightward ? 1 : -1;
            float scale = 3.2f / Mathf.Max(.01f, art.bounds.size.y);
            float elapsed = 0;
            while (elapsed < BasicSlashDuration)
            {
                float t = elapsed / BasicSlashDuration;
                arc.transform.position = point + new Vector3(direction * Mathf.Lerp(-.35f, .15f, t), 0, -1.1f);
                arc.transform.localScale = new Vector3(direction * scale, scale, scale) * Mathf.Lerp(.9f, 1.15f, t);
                arc.transform.localRotation = Quaternion.Euler(0, 0, direction * Mathf.Lerp(-24, 24, t));
                renderer.color = new Color(1, 1, 1, 1 - t * t);
                yield return null;
                elapsed += Time.deltaTime;
            }
            Destroy(arc);
        }

        void Burst(Sprite art, int tier, int variant, Vector3 point, Color color)
        {
            var dust = new GameObject("Era " + tier + " illustrated impact fragments").AddComponent<ParticleSystem>();
            owned.RemoveAll(item => !item); owned.Add(dust.gameObject);
            dust.gameObject.layer = 30; dust.transform.SetParent(transform,false); dust.transform.position = point;
            dust.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main; main.loop = false; main.duration = .65f; main.startLifetime = .55f;
            main.startSpeed = (variant == 0 ? .6f : variant == 2 ? 2.6f : 1.6f) * VisualScale;
            main.startSize = (variant == 0 ? .14f : tier <= 3 ? .22f : .15f) * VisualScale * 2 * 3;
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI,Mathf.PI);
            main.startColor = Color.white; main.gravityModifier = (variant == 0 ? -.1f : tier <= 3 ? .8f : .05f) * VisualScale;
            main.simulationSpace = ParticleSystemSimulationSpace.World; main.maxParticles = 50;
            var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .1f * VisualScale;
            var emission = dust.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0,(short)(variant==2?22:12)) });
            var sheet = dust.textureSheetAnimation; sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Sprites; sheet.AddSprite(art);
            var size = dust.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,1),new Keyframe(1,0)));
            var renderer = dust.GetComponent<ParticleSystemRenderer>();
            // Explicitly bind the real transparent sprite texture: default white material would make square particles.
            var material = new Material(catalog.effectMaterial); material.mainTexture = art.texture;
            renderer.sharedMaterial = material; renderer.sortingOrder = 151;
            dust.Play(); Destroy(material,1.1f); Destroy(dust.gameObject,1.1f);
        }
    }
}
