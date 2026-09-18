using UnityEngine;

namespace Moonlit.UI
{
    // Project the two rendered feet onto the ground, including rig offsets, mirroring and attack movement.
    public sealed class CombatGroundShadow : MonoBehaviour
    {
        Transform actor, motion;
        SpriteRenderer[] feet;
        Mesh mesh;
        Material material;
        public static void Create(Transform parent, Transform actor)
        {
            var go = new GameObject(actor.name + " ground shadow");
            go.layer = 30; go.transform.SetParent(parent, false);
            var shadow = go.AddComponent<CombatGroundShadow>();
            shadow.actor = actor; shadow.motion = actor.Find("Motion");
            var footParts = new System.Collections.Generic.List<SpriteRenderer>();
            foreach (var sprite in actor.GetComponentsInChildren<SpriteRenderer>(true))
                if (sprite.sprite && (sprite.sprite.name == "다리1" || sprite.sprite.name == "다리2")) footParts.Add(sprite);
            shadow.feet = footParts.ToArray();
            const int segments = 48;
            var vertices = new Vector3[segments + 1];
            var colors = new Color[segments + 1];
            var triangles = new int[segments * 3];
            colors[0] = new Color(0, 0, 0, .48f);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * 1.45f, Mathf.Sin(angle) * .25f, 0);
                colors[i + 1] = Color.clear;
                triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = (i + 1) % segments + 1;
            }
            shadow.mesh = new Mesh { name = "Soft ellipse ground shadow" };
            shadow.mesh.vertices = vertices; shadow.mesh.colors = colors; shadow.mesh.triangles = triangles;
            shadow.mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = shadow.mesh;
            shadow.material = new Material(Shader.Find("Sprites/Default"));
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = shadow.material;
            renderer.sortingOrder = -100;
            shadow.LateUpdate();
        }
        void LateUpdate()
        {
            if (!actor) return;
            float x = motion ? motion.position.x : actor.position.x;
            float ground = actor.position.y;
            int count = 0; float feetX = 0, feetBottom = float.PositiveInfinity;
            foreach (var foot in feet)
            {
                if (!foot || !foot.sprite) continue;
                feetX += foot.bounds.center.x;
                feetBottom = Mathf.Min(feetBottom, foot.bounds.min.y);
                count++;
            }
            if (count > 0)
            {
                x = feetX / count;
                // Remove only the authored vertical jump; retain the real rig/foot offset and pose.
                ground = feetBottom - (motion ? motion.position.y - actor.position.y : 0);
            }
            transform.position = new Vector3(x, ground + .03f, actor.position.z + 1);
        }
        void OnDestroy()
        {
            if (mesh) Destroy(mesh);
            if (material) Destroy(material);
        }
    }
}
