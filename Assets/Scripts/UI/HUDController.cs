using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using GorillaSurvivors.Core;
using GorillaSurvivors.Player;
using GorillaSurvivors.Player.Abilities;

namespace GorillaSurvivors.UI
{
    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }

        Image _hpFill;
        Image _xpFill;
        Text _levelText;
        Text _roundText;
        Text _roundProgressText;
        Text _timerText;
        GameObject _gameOverPanel;
        Text _gameOverText;
        Text _toastText;
        Coroutine _toastRoutine;
        Text _roundBannerText;
        Coroutine _roundBannerRoutine;
        TechTreePanel _techTreePanel;

        PlayerHealth _health;
        PlayerStats _stats;
        PlayerAttack _attack;
        QuickSwipeAttack _swipeAttack;
        PlayerController _controller;
        ChestBeatAbility _chestBeat;
        DungTossAbility _dungToss;

        struct AbilityIcon
        {
            public Image Background;
            public Image CooldownMask;
            public Text Label;
        }
        AbilityIcon _atkIcon, _swipeIcon, _dashIcon, _roarIcon, _chargeIcon;

        static readonly Color LockedColor = new Color(0.15f, 0.15f, 0.15f);
        static readonly Color LockedLabelColor = new Color(1f, 1f, 1f, 0.3f);
        static readonly Color AttackColor = new Color(0.55f, 0.55f, 0.55f);
        static readonly Color SwipeColor = new Color(0.75f, 0.7f, 0.35f);
        static readonly Color DashColor = new Color(0.25f, 0.5f, 0.85f);
        static readonly Color RoarColor = new Color(0.85f, 0.75f, 0.25f);
        static readonly Color ChargeColor = new Color(0.25f, 0.8f, 0.85f);

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
            hud._attack = health.GetComponent<PlayerAttack>();
            hud._swipeAttack = health.GetComponent<QuickSwipeAttack>();
            hud._controller = health.GetComponent<PlayerController>();
            hud._chestBeat = health.GetComponent<ChestBeatAbility>();
            hud._dungToss = health.GetComponent<DungTossAbility>();

            hud._hpFill = CreateBar(canvasGO.transform, "HPBar", "HP", new Vector2(20, -20), new Color(0.85f, 0.2f, 0.2f));
            hud._xpFill = CreateBar(canvasGO.transform, "XPBar", "XP", new Vector2(20, -46), new Color(0.2f, 0.6f, 0.95f));

            hud._levelText = CreateText(canvasGO.transform, "LevelText", new Vector2(20, -72), "Lv.1", 22, TextAnchor.UpperLeft);
            hud._roundText = CreateText(canvasGO.transform, "RoundText", new Vector2(20, -98), "Round 1", 20, TextAnchor.UpperLeft);
            hud._roundProgressText = CreateText(canvasGO.transform, "RoundProgressText", new Vector2(20, -122), "0/100", 18, TextAnchor.UpperLeft);
            hud._timerText = CreateText(canvasGO.transform, "TimerText", new Vector2(-20, -20), "0:00", 26, TextAnchor.UpperRight);

            hud._gameOverPanel = CreateGameOverPanel(canvasGO.transform, out hud._gameOverText);
            hud._gameOverPanel.SetActive(false);

            hud._techTreePanel = TechTreePanel.Create(canvasGO.transform, health.GetComponent<TechTreeState>());

            hud.CreatePauseButton(canvasGO.transform);
            OffscreenEnemyMarkers.Create(canvasGO.transform);
            hud._toastText = CreateToastText(canvasGO.transform);
            hud._roundBannerText = CreateRoundBannerText(canvasGO.transform);
            hud.CreateAbilityBar(canvasGO.transform);

            // Built last so its buttons sit above the rest of the HUD, and
            // only shows itself on a device with a touch screen.
            TouchControls.Create(canvasGO.transform);

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

        void CreateAbilityBar(Transform parent)
        {
            const float iconSize = 56f;
            const float spacing = 14f;
            const int count = 5;
            float totalWidth = count * iconSize + (count - 1) * spacing;
            float startX = -totalWidth / 2f + iconSize / 2f;

            // Every slot uses hand-drawn pixel art (PixelArtIcons); the
            // sprites carry their own palette, so no tint is applied.
            // Order matches the buttons: the quick swipe is the primary
            // (left) attack, the committed slam the secondary (right) one.
            _swipeIcon = CreateAbilityIcon(parent, "AbilitySwipe", "LMB", SwipeColor, startX + 0 * (iconSize + spacing), iconSize, PixelArtIcons.Claw());
            _atkIcon = CreateAbilityIcon(parent, "AbilityAttack", "RMB", AttackColor, startX + 1 * (iconSize + spacing), iconSize, PixelArtIcons.Slam());
            _dashIcon = CreateAbilityIcon(parent, "AbilityDash", "SPC", DashColor, startX + 2 * (iconSize + spacing), iconSize, PixelArtIcons.Dash());
            _roarIcon = CreateAbilityIcon(parent, "AbilityChestBeat", "Q", RoarColor, startX + 3 * (iconSize + spacing), iconSize, PixelArtIcons.GorillaShout());
            _chargeIcon = CreateAbilityIcon(parent, "AbilityDungToss", "E", ChargeColor, startX + 4 * (iconSize + spacing), iconSize, PixelArtIcons.DungToss());
        }

        static AbilityIcon CreateAbilityIcon(Transform parent, string name, string label, Color color, float xOffset, float size, Sprite glyphSprite)
        {
            var root = new GameObject(name, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(xOffset, 16f);
            rect.sizeDelta = new Vector2(size, size);

            var background = root.AddComponent<Image>();
            background.color = color;

            // A simple procedural glyph (see PlaceholderSprites.Icon) drawn
            // over the color-coded tile, so the bar reads as ability icons
            // rather than plain colored squares.
            var iconGO = new GameObject("Glyph", typeof(RectTransform));
            iconGO.transform.SetParent(root.transform, false);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, size * 0.09f);
            iconRect.sizeDelta = new Vector2(size * 0.74f, size * 0.74f);
            var glyph = iconGO.AddComponent<Image>();
            glyph.sprite = glyphSprite;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(root.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.sizeDelta = new Vector2(0f, size * 0.3f);
            labelRect.anchoredPosition = Vector2.zero;
            var labelBg = labelGO.AddComponent<Image>();
            labelBg.color = new Color(0f, 0f, 0f, 0.45f);

            var labelTextGO = new GameObject("Text", typeof(RectTransform));
            labelTextGO.transform.SetParent(labelGO.transform, false);
            var labelTextRect = labelTextGO.GetComponent<RectTransform>();
            labelTextRect.anchorMin = Vector2.zero;
            labelTextRect.anchorMax = Vector2.one;
            labelTextRect.offsetMin = Vector2.zero;
            labelTextRect.offsetMax = Vector2.zero;
            var labelText = labelTextGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 13;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = label;

            // Cooldown mask on top: a Filled image that shrinks from full
            // coverage (just used) down to none (ready). Needs a sprite —
            // Image.Type.Filled silently ignores fillAmount without one.
            var maskGO = new GameObject("CooldownMask", typeof(RectTransform));
            maskGO.transform.SetParent(root.transform, false);
            var maskRect = maskGO.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            var mask = maskGO.AddComponent<Image>();
            mask.sprite = PlaceholderSprites.Square(Color.white, 4);
            mask.color = new Color(0f, 0f, 0f, 0.75f);
            mask.type = Image.Type.Filled;
            mask.fillMethod = Image.FillMethod.Vertical;
            mask.fillOrigin = (int)Image.OriginVertical.Top;
            mask.fillAmount = 0f;

            return new AbilityIcon { Background = background, CooldownMask = mask, Label = labelText };
        }

        void RefreshAbilityIcon(AbilityIcon icon, Color unlockedColor, bool unlocked, float cooldownRemaining01)
        {
            if (!unlocked)
            {
                icon.Background.color = LockedColor;
                icon.Label.color = LockedLabelColor;
                icon.CooldownMask.fillAmount = 1f;
                return;
            }

            icon.Background.color = unlockedColor;
            icon.Label.color = Color.white;
            icon.CooldownMask.fillAmount = cooldownRemaining01;
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
                _roundText.text = $"Round {GameManager.Instance.CurrentRound}";
                _roundProgressText.text = $"{GameManager.Instance.KilledThisRound}/{GameManager.Instance.EnemiesPerRound}";
            }

            RefreshAbilityIcon(_atkIcon, AttackColor, true, _attack != null ? _attack.AttackCooldownRemaining01() : 0f);
            RefreshAbilityIcon(_swipeIcon, SwipeColor, true, _swipeAttack != null ? _swipeAttack.SwipeCooldownRemaining01() : 0f);
            RefreshAbilityIcon(_dashIcon, DashColor, true, _controller != null ? _controller.DashCooldownRemaining01() : 0f);
            RefreshAbilityIcon(_roarIcon, RoarColor, _chestBeat != null && _chestBeat.Unlocked, _chestBeat != null ? _chestBeat.CooldownRemaining01() : 0f);
            RefreshAbilityIcon(_chargeIcon, ChargeColor, _dungToss != null && _dungToss.Unlocked, _dungToss != null ? _dungToss.CooldownRemaining01() : 0f);
        }

        public void ShowRoundBanner(int round)
        {
            if (_roundBannerRoutine != null) StopCoroutine(_roundBannerRoutine);
            _roundBannerRoutine = StartCoroutine(RoundBannerRoutine(round));
        }

        IEnumerator RoundBannerRoutine(int round)
        {
            _roundBannerText.text = $"ROUND {round}";
            var color = _roundBannerText.color;
            color.a = 1f;
            _roundBannerText.color = color;
            _roundBannerText.gameObject.SetActive(true);

            yield return new WaitForSeconds(1.2f);

            float fadeDuration = 0.6f;
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                color.a = Mathf.Lerp(1f, 0f, t / fadeDuration);
                _roundBannerText.color = color;
                yield return null;
            }

            _roundBannerText.gameObject.SetActive(false);
            _roundBannerRoutine = null;
        }

        static Text CreateRoundBannerText(Transform parent)
        {
            var go = new GameObject("RoundBannerText", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 120f);
            rect.sizeDelta = new Vector2(800, 80);

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 44;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = string.Empty;

            go.SetActive(false);
            return text;
        }

        public void ShowUpgradeChoice()
        {
            _techTreePanel.Show();
        }

        // A clickable pause control alongside the Esc/P keys. Browsers
        // reserve Escape for leaving fullscreen, and a web build embedded in
        // a page doesn't always get keyboard focus at all, so the mouse is
        // the one input that is guaranteed to reach a WebGL build.
        void CreatePauseButton(Transform parent)
        {
            var go = new GameObject("PauseButton", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-20f, -56f);
            rect.sizeDelta = new Vector2(46f, 34f);

            var image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.16f, 0.18f, 0.85f);

            var button = go.AddComponent<Button>();
            button.onClick.AddListener(() =>
            {
                var gm = GameManager.Instance;
                if (gm != null) gm.SetManualPause(!gm.IsManuallyPaused);
            });

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 18;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = "II";
        }

        public void ShowPaused(bool paused)
        {
            if (_pausePanel == null)
            {
                _pausePanel = CreateGameOverPanel(transform, out var label);
                _pausePanel.name = "PausePanel";
                label.text = "PAUSED\n\n<size=24>Click, Esc or P to resume</size>";
                label.supportRichText = true;

                // The whole dimmed overlay resumes on click — uGUI ignores
                // timeScale, so this still responds while the game is frozen.
                var resume = _pausePanel.AddComponent<Button>();
                resume.transition = Selectable.Transition.None;
                resume.onClick.AddListener(() => GameManager.Instance?.SetManualPause(false));
            }
            _pausePanel.SetActive(paused);
        }

        GameObject _pausePanel;

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
