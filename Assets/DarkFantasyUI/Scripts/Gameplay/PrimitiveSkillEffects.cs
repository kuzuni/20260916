using System.Collections;
using UnityEngine;

namespace Moonlit.UI
{
    // Thirty era-specific sprite, trail and sprite-particle effects. Damage remains exclusively Animator-event driven.
    public sealed class PrimitiveSkillEffects : MonoBehaviour
    {
        public const float AttackFlightDuration = .65f;
        const float VisualScale = 2f;
        BattleAssetCatalog catalog;
        readonly System.Collections.Generic.List<GameObject> owned = new System.Collections.Generic.List<GameObject>();
        void OnDisable()
        {
            StopAllCoroutines();
            foreach (var item in owned) if (item) Destroy(item);
            owned.Clear();
        }
        static readonly Color[] Colors = {
            new Color(.75f,.55f,.3f), new Color(1,.78f,.34f), new Color(.65f,1,.25f),
            new Color(.85f,.6f,.25f), new Color(.15f,.95f,1), new Color(.95f,.3f,1),
            new Color(.6f,.4f,1), new Color(.4f,1,.8f), new Color(.5f,1,.3f), new Color(1,.88f,.45f)
        };
        public void Initialize(BattleAssetCatalog assets) { catalog = assets; }
        public static string ResourceKey(int tier, int variant)
            => "Moonlit/Combat/Skills/Tier" + Mathf.Clamp(tier, 0, 9).ToString("00") + "/" +
                new[] { "Buff", "Weak", "Strong" }[Mathf.Clamp(variant, 0, 2)];
        public static Sprite SkillSprite(int tier, int variant) => Resources.Load<Sprite>(ResourceKey(tier, variant));
        public void Play(int variant, Vector3 source, Vector3 target) => Play(0, variant, source, target);
        public void Play(int tier, int variant, Vector3 source, Vector3 target, Transform sourceAnchor = null, Transform targetAnchor = null, bool burstOnArrival = true)
        {
            if (catalog) StartCoroutine(Animate(Mathf.Clamp(tier, 0, 9), Mathf.Clamp(variant, 0, 2), source, target, sourceAnchor, targetAnchor, burstOnArrival));
        }
        static string EffectName(int tier, int variant)
            => tier == 0 ? new[] { "Primitive ancestral blessing", "Primitive stone crescent", "Primitive falling boulder" }[variant]
                : "Era " + tier + " skill " + variant;
        IEnumerator Animate(int tier, int variant, Vector3 source, Vector3 target, Transform sourceAnchor, Transform targetAnchor, bool burstOnArrival)
        {
            Vector3 sourceOffset = sourceAnchor ? source - sourceAnchor.position : Vector3.zero;
            Vector3 targetOffset = targetAnchor ? target - targetAnchor.position : Vector3.zero;
            Sprite art = SkillSprite(tier, variant);
            // Legacy primitive sprites only support old asset-only fixtures. Production cloud acceptance requires all thirty.
            if (!art && tier == 0) art = variant == 0 ? catalog.buffSprite : variant == 1 ? catalog.weakSprite : catalog.strongSprite;
            if (!art) { Debug.LogWarning("[Moonlit] Missing illustrated skill " + ResourceKey(tier, variant)); yield break; }
            var root = new GameObject(EffectName(tier, variant));
            root.transform.SetParent(transform, false); root.layer = 30;
            owned.RemoveAll(item => !item); owned.Add(root);
            var sprite = root.AddComponent<SpriteRenderer>(); sprite.sprite = art;
            sprite.sharedMaterial = catalog.effectMaterial; sprite.sortingOrder = 150;
            Color color = Colors[tier];
            var trail = root.AddComponent<TrailRenderer>(); trail.sharedMaterial = catalog.effectMaterial;
            trail.time = tier == 6 ? .55f : .50f; trail.startWidth = (variant == 2 ? .28f : .12f) * VisualScale * 2; trail.endWidth = 0;
            trail.startColor = color; trail.endColor = new Color(color.r,color.g,color.b,0);
            trail.sortingOrder = 148; trail.minVertexDistance = .025f;
            trail.emitting = variant != 0 || tier >= 4;
            float size = variant == 0 ? 1.2f : variant == 1 ? .9f : 1.65f;
            if (tier == 3 && variant == 1) size = 2.2f; // tank drives across the ground
            float normalize = size * VisualScale / Mathf.Max(.01f, Mathf.Max(art.bounds.size.x, art.bounds.size.y));
            float duration = AttackFlightDuration;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                // Preview can start during an entrance or a lunge; keep the effect attached to the real moving actors.
                if (sourceAnchor) source = sourceAnchor.position + sourceOffset;
                if (targetAnchor) target = targetAnchor.position + targetOffset;
                float t = Mathf.Clamp01(time / duration);
                root.transform.position = Position(tier, variant, source, target, t);
                float rotation = Rotation(tier, variant, t);
                root.transform.rotation = Quaternion.Euler(0,0,rotation);
                float pulse = variant == 0 ? .8f + .2f * Mathf.Sin(t * Mathf.PI) : 1;
                root.transform.localScale = Vector3.one * normalize * pulse;
                sprite.color = new Color(1,1,1,1);
                yield return null;
            }
            if (sourceAnchor) source = sourceAnchor.position + sourceOffset;
            if (targetAnchor) target = targetAnchor.position + targetOffset;
            root.transform.position = Position(tier, variant, source, target, 1);
            if (burstOnArrival) Burst(art, tier, variant, root.transform.position, color);
            sprite.enabled = false; trail.emitting = false;
            Destroy(root, .5f);
        }
        // Each era combines its own illustrated subject with a movement matching the subject.
        public static Vector3 Position(int tier, int variant, Vector3 source, Vector3 target, float t)
        {
            Vector3 p;
            float direction = target.x >= source.x ? 1 : -1;
            if (variant == 0)
            {
                // Primitive eating/medieval drinking/medical supply stay by the actor; later eras spiral around them.
                if (tier <= 3) p = source + new Vector3(direction * Mathf.Lerp(.8f,.15f,t), .8f + Mathf.Sin(t*Mathf.PI)*.25f,0);
                else p = source + new Vector3(Mathf.Sin(t*Mathf.PI*2)*.45f, Mathf.Cos(t*Mathf.PI*2)*.45f+.4f,0);
            }
            else if (variant == 1)
            {
                p = Vector3.Lerp(source,target,t);
                if (tier == 0) p.y += Mathf.Sin(t*Mathf.PI)*1.1f; // thrown stone
                else if (tier == 3) p.y = Mathf.Lerp(source.y,target.y,t)-1.4f; // charging tank
                else if (tier == 6) p.y += Mathf.Sin(t*Mathf.PI*3)*.35f; // dimensional cut
                else if (tier == 8) p.y += Mathf.Sin(t*Mathf.PI*4)*.2f; // living soul chain
                else if (tier == 5 || tier == 7) p.y += Mathf.Sin(t*Mathf.PI)*.2f;
            }
            else if (tier == 0 || tier == 1 || tier == 2)
                p = Vector3.Lerp(source,target,t) + Vector3.up*(Mathf.Sin(t*Mathf.PI)*(tier==1?1.9f:1.2f));
            else if (tier == 5 || tier == 6 || tier == 7)
                p = target + new Vector3(Mathf.Sin(t*Mathf.PI*4)*(1-t)*.6f,(1-t)*.7f,0);
            else
                p = Vector3.Lerp(target + new Vector3(-direction*.75f,2,0),target,t);
            return p + Vector3.back;
        }
        static float Rotation(int tier, int variant, float t)
        {
            if (variant == 0) return tier >= 4 ? t*90 : Mathf.Sin(t*Mathf.PI)*-15;
            if (tier == 3 || tier == 4 || tier == 8 || tier == 9) return 0;
            return variant == 1 && (tier == 1 || tier == 2 || tier == 5 || tier == 7) ? 0 : t*(tier==6?-270:210);
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

        // Production hits invoke this only from the consumed Animator event; previews may burst on arrival.
        public void PlayImpact(int tier, int variant, Vector3 point)
        {
            tier = Mathf.Clamp(tier, 0, 9); variant = Mathf.Clamp(variant, 0, 2);
            var art = SkillSprite(tier, variant);
            if (!art && tier == 0 && catalog) art = variant == 2 ? catalog.strongSprite : catalog.weakSprite;
            if (catalog && art) Burst(art, tier, variant, point + Vector3.back, Colors[tier]);
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
