using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.UI
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        Image _hpFill;
        Image _xpFill;
        Text _levelText;
        Text _timerText;
        GameObject _gameOverPanel;
        Text _gameOverText;
        Text _toastText;
        Coroutine _toastRoutine;

        PlayerHealth _health;
        PlayerStats _stats;

        public static HUDController Build(PlayerHealth health, PlayerStats stats)
        {
            EnsureEventSystem();

            var canvasGO = new GameObject("HUDCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            canvasGO.AddComponent<GraphicRaycaster>();

            var hud = canvasGO.AddComponent<HUDController>();
            Instance = hud;
            hud._health = health;
            hud._stats = stats;

            hud._hpFill = CreateBar(canvasGO.transform, "HPBar", "HP", new Vector2(20, -20), new Color(0.85f, 0.2f, 0.2f));
            hud._xpFill = CreateBar(canvasGO.transform, "XPBar", "XP", new Vector2(20, -46), new Color(0.2f, 0.6f, 0.95f));

            hud._levelText = CreateText(canvasGO.transform, "LevelText", new Vector2(20, -72), "Lv.1", 22, TextAnchor.UpperLeft);
            hud._timerText = CreateText(canvasGO.transform, "TimerText", new Vector2(-20, -20), "0:00", 26, TextAnchor.UpperRight);

            hud._gameOverPanel = CreateGameOverPanel(canvasGO.transform, out hud._gameOverText);
            hud._gameOverPanel.SetActive(false);

            hud._toastText = CreateToastText(canvasGO.transform);
            CreateHintText(canvasGO.transform);

            health.OnHealthChanged += hud.HandleHealthChanged;
            stats.OnXPChanged += hud.HandleXPChanged;
            stats.OnLevelUp += hud.HandleLevelUp;

            hud.HandleHealthChanged(health.CurrentHP, health.MaxHP);
            hud.HandleXPChanged(stats.CurrentXP, stats.XPToNextLevel);
            hud.HandleLevelUp(stats.Level);

            return hud;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowToast(string message)
        {
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine(message));
        }

        IEnumerator ToastRoutine(string message)
        {
            _toastText.text = message;
            var color = _toastText.color;
            color.a = 1f;
            _toastText.color = color;
            _toastText.gameObject.SetActive(true);

            yield return new WaitForSeconds(1.6f);

            float fadeDuration = 0.5f;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                color.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                _toastText.color = color;
                yield return null;
            }

            _toastText.gameObject.SetActive(false);
            _toastRoutine = null;
        }

        static Text CreateToastText(Transform parent)
        {
            var go = new GameObject("ToastText", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -110f);
            rect.sizeDelta = new Vector2(700, 50);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 0.92f, 0.55f);
            text.fontStyle = FontStyle.Bold;
            text.text = string.Empty;

            go.SetActive(false);
            return text;
        }

        static void CreateHintText(Transform parent)
        {
            var go = new GameObject("HintText", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 14f);
            rect.sizeDelta = new Vector2(700, 30);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(1f, 1f, 1f, 0.7f);
            text.text = "WASD move  |  J / Click to attack  |  Space to dash";
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        void Update()
        {
            if (GameManager.Instance == null) return;

            if (!GameManager.Instance.IsGameOver)
            {
                float t = GameManager.Instance.SurvivalTime;
                int minutes = Mathf.FloorToInt(t / 60f);
                int seconds = Mathf.FloorToInt(t % 60f);
                _timerText.text = $"{minutes}:{seconds:00}";
            }
        }

        public void ShowGameOver()
        {
            _gameOverPanel.SetActive(true);
            int minutes = Mathf.FloorToInt(GameManager.Instance.SurvivalTime / 60f);
            int seconds = Mathf.FloorToInt(GameManager.Instance.SurvivalTime % 60f);
            _gameOverText.text = $"THE HORDE WINS\nSurvived {minutes}:{seconds:00} — Lv.{_stats.Level}\n\nPress R to restart";
        }

        void HandleHealthChanged(float current, float max)
        {
            _hpFill.fillAmount = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }

        void HandleXPChanged(float current, float needed)
        {
            _xpFill.fillAmount = needed <= 0f ? 0f : Mathf.Clamp01(current / needed);
        }

        void HandleLevelUp(int level)
        {
            _levelText.text = $"Lv.{level}";
        }

        static Image CreateBar(Transform parent, string name, string label, Vector2 anchoredPos, Color color)
        {
            var bg = new GameObject(name + "_BG", typeof(RectTransform));
            bg.transform.SetParent(parent, false);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 1);
            bgRect.anchorMax = new Vector2(0, 1);
            bgRect.pivot = new Vector2(0, 1);
            bgRect.anchoredPosition = anchoredPos;
            bgRect.sizeDelta = new Vector2(220, 18);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, 0.5f);

            var fillGO = new GameObject(name + "_Fill", typeof(RectTransform));
            fillGO.transform.SetParent(bg.transform, false);
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            var fillImage = fillGO.AddComponent<Image>();
            // Image.Type.Filled only actually clips geometry when a sprite is
            // assigned — with sprite == null it silently falls back to
            // rendering a plain full quad regardless of fillAmount.
            fillImage.sprite = PlaceholderSprites.Square(Color.white, 4);
            fillImage.color = color;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 1f;

            var labelGO = new GameObject(name + "_Label", typeof(RectTransform));
            labelGO.transform.SetParent(bg.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(4, 0);
            labelRect.offsetMax = new Vector2(-4, 0);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 13;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = new Color(1f, 1f, 1f, 0.85f);
            labelText.text = label;

            return fillImage;
        }

        static Text CreateText(Transform parent, string name, Vector2 anchoredPos, string initial, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();

            bool right = anchor == TextAnchor.UpperRight;
            rect.anchorMin = new Vector2(right ? 1 : 0, 1);
            rect.anchorMax = new Vector2(right ? 1 : 0, 1);
            rect.pivot = new Vector2(right ? 1 : 0, 1);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(220, 32);

            var text = go.AddComponent<Text>();
            text.text = initial;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor == TextAnchor.UpperRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
            text.color = Color.white;

            return text;
        }

        static GameObject CreateGameOverPanel(Transform parent, out Text label)
        {
            var panel = new GameObject("GameOverPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.75f);

            var textGO = new GameObject("GameOverText", typeof(RectTransform));
            textGO.transform.SetParent(panel.transform, false);
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(800, 300);
            textRect.anchoredPosition = Vector2.zero;

            label = textGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 36;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "GAME OVER";

            return panel;
        }
    }
}
