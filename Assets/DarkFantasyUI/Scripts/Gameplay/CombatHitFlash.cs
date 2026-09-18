using System.Collections;
using UnityEngine;

namespace Moonlit.UI
{
    // Preserve the sprite alpha and shape while replacing its visible texels with white.
    public sealed class CombatHitFlash : MonoBehaviour
    {
        public const float Duration = .12f;
        SpriteRenderer[] sprites;
        Material[] originals;
        Material white;
        Coroutine routine;
        public bool IsFlashing { get; private set; }

        public void Play()
        {
            if (!isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            Restore();
            var shader = Resources.Load<Shader>("Moonlit/Combat/HitWhite");
            if (!shader) { Debug.LogWarning("[Moonlit] Missing hit flash shader."); return; }
            if (!white) white = new Material(shader) { name = "Combat white hit flash" };
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            originals = new Material[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                originals[i] = sprites[i].sharedMaterial;
                sprites[i].sharedMaterial = white;
            }
            IsFlashing = true;
            routine = StartCoroutine(Finish());
        }
        IEnumerator Finish()
        {
            // At least one rendered frame, even when the cloud renderer or device is slow.
            yield return null;
            yield return new WaitForSeconds(Duration);
            Restore(); routine = null;
        }
        void Restore()
        {
            if (sprites != null && originals != null)
                for (int i = 0; i < sprites.Length; i++)
                    if (sprites[i]) sprites[i].sharedMaterial = originals[i];
            sprites = null; originals = null; IsFlashing = false;
        }
        void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null; Restore();
        }
        void OnDestroy() { Restore(); if (white) Destroy(white); }
    }
}
