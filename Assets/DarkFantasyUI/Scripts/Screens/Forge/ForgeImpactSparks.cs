using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    // Canvas-native particles remain visible over the anvil without a world camera or extra material.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ForgeImpactSparks : MaskableGraphic
    {
        const int ParticleCount = 18;
        const float Lifetime = .2f;
        float age = Lifetime;
        int strike;
        public int VisibleSparkCount => age >= 0 && age < Lifetime ? ParticleCount : 0;

        public void Sample(int strikeIndex, double secondsSinceImpact)
        {
            strike = strikeIndex;
            age = (float)secondsSinceImpact;
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (VisibleSparkCount == 0) return;
            float t = Mathf.Clamp01(age / Lifetime);
            // Bright central flash, followed by short orange/gold streaks that spread and fall.
            Color flash = new Color(1f, .92f, .56f, (1 - t) * .95f);
            AddStreak(mesh, Vector2.zero, Vector2.up, 30 * (1 - t) + 5, 30 * (1 - t) + 5, flash);
            for (int i = 0; i < ParticleCount; i++)
            {
                float angle = (18 + i * 144 + strike * 19) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Abs(Mathf.Sin(angle)) * .85f + .15f).normalized;
                float distance = (65 + (i % 5) * 17) * t;
                Vector2 center = direction * distance + Vector2.down * (38 * t * t);
                Color tint = Color.Lerp(new Color(1, .94f, .58f), new Color(1, .31f, .025f), t);
                tint.a = 1 - t;
                AddStreak(mesh, center, direction, (14 + i % 4 * 5) * (1 - t) + 4, 3 + i % 3, tint);
            }
        }

        static void AddStreak(VertexHelper mesh, Vector2 center, Vector2 direction, float length, float width, Color tint)
        {
            Vector2 along = direction * (length * .5f);
            Vector2 across = new Vector2(-direction.y, direction.x) * (width * .5f);
            int first = mesh.currentVertCount;
            mesh.AddVert(center - along - across, tint, Vector2.zero);
            mesh.AddVert(center + along - across, tint, Vector2.zero);
            mesh.AddVert(center + along + across, tint, Vector2.zero);
            mesh.AddVert(center - along + across, tint, Vector2.zero);
            mesh.AddTriangle(first, first + 1, first + 2);
            mesh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
