using System.Collections;
using UnityEngine;

namespace Moonlit.UI
{
    // Three code-authored primitive skills: ancestral blessing, stone crescent, falling boulder.
    // All use a distinct 2D sprite, a ParticleSystem, and a moving TrailRenderer.
    public sealed class PrimitiveSkillEffects : MonoBehaviour
    {
        BattleAssetCatalog catalog;
        public void Initialize(BattleAssetCatalog assets) { catalog = assets; }
        public void Play(int variant, Vector3 source, Vector3 target)
        {
            if (catalog) StartCoroutine(Animate(Mathf.Clamp(variant, 0, 2), source, target));
        }
        IEnumerator Animate(int variant, Vector3 source, Vector3 target)
        {
            var root = new GameObject(new[] { "Primitive ancestral blessing", "Primitive stone crescent", "Primitive falling boulder" }[variant]);
            root.transform.SetParent(transform, false); root.layer = 30;
            Color color = variant == 0 ? new Color(.35f, 1, .65f) : variant == 1 ? new Color(1, .83f, .37f) : new Color(1, .35f, .13f);
            var sprite = root.AddComponent<SpriteRenderer>();
            sprite.sprite = variant == 0 ? catalog.buffSprite : variant == 1 ? catalog.weakSprite : catalog.strongSprite;
            sprite.sharedMaterial = catalog.effectMaterial;
            sprite.sortingOrder = 150;
            var trail = root.AddComponent<TrailRenderer>();
            trail.sharedMaterial = catalog.effectMaterial; trail.time = .28f;
            trail.startWidth = variant == 2 ? .35f : .16f; trail.endWidth = 0;
            trail.startColor = color; trail.endColor = new Color(color.r, color.g, color.b, 0);
            trail.sortingOrder = 148; trail.minVertexDistance = .025f;
            var dust = new GameObject("Independent burst particles").AddComponent<ParticleSystem>();
            dust.transform.SetParent(transform, false); dust.gameObject.layer = 30;
            dust.transform.position = variant == 0 ? source : target;
            dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dust.main; main.loop = false; main.duration = .65f; main.startLifetime = .6f;
            main.startSpeed = variant == 0 ? .65f : 1.8f; main.startSize = .1f;
            main.startColor = color; main.gravityModifier = variant == 0 ? -.1f : .3f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;
            var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .2f;
            var emission = dust.emission; emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0, (short)(variant == 2 ? 36 : 22)) });
            var render = dust.GetComponent<ParticleSystemRenderer>(); render.sharedMaterial = catalog.effectMaterial; render.sortingOrder = 149;
            float duration = .65f;
            for (float time = 0; time < duration; time += Time.deltaTime)
            {
                float t = time / duration;
                Vector3 position;
                if (variant == 0)
                    position = source + new Vector3(Mathf.Sin(t * Mathf.PI * 4) * .5f, t * 1.4f - .5f, -1);
                else if (variant == 1)
                    position = Vector3.Lerp(source, target, t) + new Vector3(0, Mathf.Sin(t * Mathf.PI) * .55f, -1);
                else
                    position = Vector3.Lerp(target + new Vector3(-1.3f, 2.8f, -1), target + Vector3.back, t);
                root.transform.position = position;
                root.transform.localScale = Vector3.one * (variant == 2 ? .8f : .65f);
                root.transform.localRotation = Quaternion.Euler(0, 0, variant == 2 ? t * -160 : t * 180);
                sprite.color = new Color(1, 1, 1, 1 - Mathf.Max(0, t - .8f) * 5);
                yield return null;
            }
            dust.Play();
            Destroy(root, .35f); Destroy(dust.gameObject, 1);
        }
    }
}
