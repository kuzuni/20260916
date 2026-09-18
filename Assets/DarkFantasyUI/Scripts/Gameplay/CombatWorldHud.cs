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
        Rect[] protectedAreas;
        public void SetProtectedAreas(Rect[] areas)
        {
            protectedAreas = areas;
            // A Safe Area/camera change can happen after the coroutine's Update but before rendering.
            // Recheck numbers already on screen immediately, as well as during their next animation step.
            foreach (var number in numbers)
            {
                if (!number) continue;
                var label = number.GetComponentInChildren<Text>();
                if (!label) continue;
                float halfWidth = Mathf.Min(760, label.preferredWidth + 24) * .008f * 1.2f / 2;
                number.transform.position = KeepOutsideHud(number.transform.position, halfWidth, .8f, 1.7f);
            }
        }
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
            hud.WorldCanvas.enabled = false; // Entrance moves below the fixed reward buttons; reveal only at battle home.
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(300, 64);
            rect.localScale = Vector3.one * .007f;
            Ui.Image("Health track", rect, 5, 8, 290, 48, null, new Color(.02f, .02f, .02f, .95f));
            hud.fill = Ui.Image("Health fill", rect, 5, 8, 290, 48, null, player ? new Color(.24f,.87f,.48f) : new Color(.9f,.28f,.23f));
            hud.HealthText = Ui.Text(player ? "Player health" : "Enemy health", rect, 7, 8, 286, 48, "", 52, font);
            hud.HealthText.fontSize = 52;
            hud.HealthText.resizeTextForBestFit = true;
            hud.HealthText.resizeTextMinSize = 28; hud.HealthText.resizeTextMaxSize = 52;
            hud.HealthText.horizontalOverflow = HorizontalWrapMode.Wrap;
            hud.HealthText.verticalOverflow = VerticalWrapMode.Truncate;
            hud.HealthText.GetComponent<Outline>().effectDistance = new Vector2(3, -3);
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 30;
            hud.LateUpdate();
            return hud;
        }
        public void SetVisible(bool visible) { WorldCanvas.enabled = visible; }
        public void Bind(CombatActorState state)
        {
            HealthText.text = Number(state.Health) + "/" + Number(state.stats.health);
            fill.rectTransform.sizeDelta = new Vector2(290 * (float)(state.Health / state.stats.health), 48);
        }
        void LateUpdate() { RefreshPosition(); }
        public void RefreshPosition()
        {
            if (!Actor) return;
            Vector3 position = head ? new Vector3(head.bounds.center.x, head.bounds.max.y, Actor.position.z) :
                Actor.position + Vector3.up * 5.1f;
            position += new Vector3(player ? -.35f : .35f, .5f, -2);
            transform.position = KeepOutsideHud(position, 1.05f, .224f, .224f);
            transform.rotation = Quaternion.identity;
        }
        Vector3 KeepOutsideHud(Vector3 position, float halfWidth, float below, float above)
        {
            if (protectedAreas == null) return position;
            foreach (var area in protectedAreas)
            {
                if (area.width <= 0 || area.height <= 0 || position.y + above < area.yMin ||
                    position.y - below > area.yMax) continue;
                if (position.x + halfWidth <= area.xMin || position.x - halfWidth >= area.xMax) continue;
                position.x = player ? area.xMin - halfWidth - .06f : area.xMax + halfWidth + .06f;
            }
            return position;
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
            var rect = (RectTransform)go.transform; rect.sizeDelta = new Vector2(760, 160);
            rect.localScale = Vector3.one * .008f;
            var label = Ui.Text("Floating combat amount", rect, 0, 0, 760, 160, message, 117, font, color);
            label.gameObject.layer = 30; label.fontSize = 117; label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 80;
            label.resizeTextMaxSize = 117; label.verticalOverflow = VerticalWrapMode.Truncate;
            var outline = label.GetComponent<Outline>();
            outline.effectDistance = new Vector2(6, -6); outline.effectColor = new Color(.02f, .01f, .03f, 1);
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectDistance = new Vector2(3, -9); shadow.effectColor = new Color(0, 0, 0, .85f);
            Vector3 start = transform.position + new Vector3(0, .65f, -.1f);
            // Reserve the full pop/drift path before the number starts; it never crosses the wave nodes.
            float halfWidth = Mathf.Min(760, label.preferredWidth + 24) * .008f * 1.2f / 2;
            start = KeepOutsideHud(start, halfWidth, .8f, 1.7f);
            for (float t = 0; t < .95f; t += Time.deltaTime)
            {
                // A sharp pop, a short readable hold, then upward drift and fade.
                float pop = t < .10f ? Mathf.Lerp(1, 1.2f, t / .10f) :
                    t < .22f ? Mathf.Lerp(1.2f, 1, (t - .10f) / .12f) : 1;
                rect.localScale = Vector3.one * (.008f * pop);
                start = KeepOutsideHud(start, halfWidth, .8f, 1.7f);
                rect.position = start + Vector3.up * (t * .9f);
                label.color = new Color(color.r, color.g, color.b, t < .35f ? 1 : 1 - (t - .35f) / .60f);
                yield return null;
            }
            Destroy(go);
        }
        static string Number(double value) => MainScreen.Compact(value);
    }
}
