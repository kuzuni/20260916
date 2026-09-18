using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    // A genuine world-space canvas follows the moving head, independently of the actor's mirrored scale.
    public sealed class CombatWorldHud : MonoBehaviour
    {
        public Text HealthText { get; private set; }
        public Canvas WorldCanvas { get; private set; }
        public Transform Actor { get; private set; }
        Image fill;
        SpriteRenderer head;
        Font font;
        bool player;
        readonly System.Collections.Generic.List<GameObject> numbers = new System.Collections.Generic.List<GameObject>();
        void OnDisable()
        {
            StopAllCoroutines();
            foreach (var number in numbers) if (number) Destroy(number);
            numbers.Clear();
        }
        public static CombatWorldHud Create(Transform parent, GameObject actor, Camera camera, Font font, bool player)
        {
            var go = new GameObject(player ? "Player world health" : "Enemy world health", typeof(RectTransform));
            go.layer = 30; go.transform.SetParent(parent, false);
            var hud = go.AddComponent<CombatWorldHud>();
            hud.Actor = actor.transform; hud.font = font; hud.player = player;
            foreach (var renderer in actor.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.sprite && renderer.sprite.name == "머리") { hud.head = renderer; break; }
            hud.WorldCanvas = go.AddComponent<Canvas>();
            hud.WorldCanvas.renderMode = RenderMode.WorldSpace; hud.WorldCanvas.worldCamera = camera;
            hud.WorldCanvas.sortingOrder = 220;
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(300, 66);
            rect.localScale = Vector3.one * .007f;
            hud.HealthText = Ui.Text(player ? "Player health" : "Enemy health", rect, 0, 0, 300, 34, "", 24, font);
            hud.HealthText.resizeTextForBestFit = true;
            hud.HealthText.resizeTextMinSize = 18; hud.HealthText.resizeTextMaxSize = hud.HealthText.fontSize;
            hud.HealthText.verticalOverflow = VerticalWrapMode.Truncate;
            Ui.Image("Health track", rect, 5, 40, 290, 12, null, new Color(.02f, .02f, .02f, .9f));
            hud.fill = Ui.Image("Health fill", rect, 5, 40, 290, 12, null, player ? new Color(.24f,.87f,.48f) : new Color(.9f,.28f,.23f));
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
            hud.LateUpdate();
            return hud;
        }
        public void Bind(CombatActorState state)
        {
            HealthText.text = Number(state.Health) + "/" + Number(state.stats.health);
            fill.rectTransform.sizeDelta = new Vector2(290 * (float)(state.Health / state.stats.health), 12);
        }
        void LateUpdate()
        {
            if (!Actor) return;
            Vector3 position = head ? new Vector3(head.bounds.center.x, head.bounds.max.y, Actor.position.z) :
                Actor.position + Vector3.up * 5.1f;
            transform.position = position + new Vector3(0, .5f, -2);
            transform.rotation = Quaternion.identity;
        }
        public void Float(string message, Color color)
        {
            if (isActiveAndEnabled) StartCoroutine(FloatNumber(message, color));
        }
        IEnumerator FloatNumber(string message, Color color)
        {
            // Follow the hit victim initially, then drift upward in world space from the impact position.
            var go = new GameObject(player ? "Player damage number" : "Enemy damage number", typeof(RectTransform));
            numbers.RemoveAll(number => !number); numbers.Add(go);
            go.layer = 30; go.transform.SetParent(transform.parent, false);
            var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = WorldCanvas.worldCamera; canvas.sortingOrder = 240;
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(340, 52);
            rect.localScale = Vector3.one * .008f;
            var label = Ui.Text("Floating combat amount", rect, 0, 0, 340, 52, message, 31, font, color);
            label.gameObject.layer = 30; label.resizeTextForBestFit = true; label.resizeTextMinSize = 19;
            label.resizeTextMaxSize = label.fontSize;
            Vector3 start = transform.position + new Vector3(0, -.5f, -.1f);
            for (float t = 0; t < .85f; t += Time.deltaTime)
            {
                rect.position = start + Vector3.up * t * .3f;
                label.color = new Color(color.r, color.g, color.b, 1 - t / .85f);
                yield return null;
            }
            Destroy(go);
        }
        static string Number(double value) => value >= 1e9 ? value.ToString("0.##E+0") : Math.Ceiling(value).ToString("N0");
    }
}
