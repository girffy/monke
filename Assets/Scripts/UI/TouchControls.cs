using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.UI
{
    public enum TouchButton { Swipe, Slam, Dash, ChestBeat, DungToss }

    // On-screen controls for phones and tablets.
    //
    // The desktop game is twin-stick: move with WASD, aim independently with
    // the mouse. That second stick has no equivalent on a touch screen worth
    // giving up half the display for, so on touch the gorilla AIMS ITSELF at
    // the nearest enemy (see PlayerController.GetAimDirection) and the player
    // only has to drive movement and pick abilities. That is what the genre
    // does on mobile, and it is the difference between playable and not.
    //
    // The left half of the screen is a floating stick — press anywhere and
    // the stick appears under your thumb rather than at a fixed spot you
    // have to find without looking. Ability buttons sit bottom-right under
    // the other thumb.
    public class TouchControls : MonoBehaviour
    {
        public static TouchControls Instance { get; private set; }

        // Touch controls appear only on a device that actually has a touch
        // screen. Checked once at build time rather than per frame, and
        // deliberately NOT "is this a small window" — a narrow desktop
        // window is still played with a mouse.
        public static bool Active => Instance != null && Instance.gameObject.activeSelf;

        TouchJoystick _joystick;
        readonly TouchButtonWidget[] _buttons = new TouchButtonWidget[5];

        public static Vector3 MoveInput => Instance != null ? Instance._joystick.Value : Vector3.zero;

        public static bool Held(TouchButton button)
        {
            return Instance != null && Instance._buttons[(int)button].IsHeld;
        }

        // Consume-on-read: an ability that reads its own button clears the
        // press. The EventSystem's pointer callbacks don't have a guaranteed
        // order relative to the abilities' Update, so a frame-number test
        // would either miss presses or fire them twice.
        public static bool ConsumePress(TouchButton button)
        {
            return Instance != null && Instance._buttons[(int)button].ConsumePress();
        }

        public static TouchControls Create(Transform parent)
        {
            var go = new GameObject("TouchControls", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var controls = go.AddComponent<TouchControls>();
            Instance = controls;
            controls.BuildLayout(rect);

            go.SetActive(HasTouchScreen());
            return controls;
        }

        static bool HasTouchScreen()
        {
            return Touchscreen.current != null || Application.isMobilePlatform;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var player = Player.PlayerController.Instance;
            if (player == null) return;

            var beat = player.GetComponent<Player.Abilities.ChestBeatAbility>();
            var dung = player.GetComponent<Player.Abilities.DungTossAbility>();
            _buttons[(int)TouchButton.ChestBeat].SetAvailable(beat != null && beat.Unlocked);
            _buttons[(int)TouchButton.DungToss].SetAvailable(dung != null && dung.Unlocked);
        }

        void BuildLayout(RectTransform root)
        {
            _joystick = TouchJoystick.Create(root);

            // Bottom-right cluster, arranged so the primary attack sits
            // where the thumb rests and the rest fan up and left.
            _buttons[(int)TouchButton.Swipe] = TouchButtonWidget.Create(root, "LMB", PixelArtIcons.Claw(),
                new Vector2(-230f, 86f), 96f, new Color(0.75f, 0.70f, 0.35f));
            _buttons[(int)TouchButton.Slam] = TouchButtonWidget.Create(root, "RMB", PixelArtIcons.Slam(),
                new Vector2(-104f, 104f), 116f, new Color(0.55f, 0.55f, 0.55f));
            _buttons[(int)TouchButton.Dash] = TouchButtonWidget.Create(root, "Dash", PixelArtIcons.Dash(),
                new Vector2(-96f, 232f), 88f, new Color(0.25f, 0.50f, 0.85f));
            _buttons[(int)TouchButton.ChestBeat] = TouchButtonWidget.Create(root, "Q", PixelArtIcons.GorillaShout(),
                new Vector2(-218f, 212f), 80f, new Color(0.62f, 0.45f, 0.72f));
            _buttons[(int)TouchButton.DungToss] = TouchButtonWidget.Create(root, "E", PixelArtIcons.DungToss(),
                new Vector2(-318f, 132f), 80f, new Color(0.52f, 0.44f, 0.30f));
        }
    }

    // A floating stick: invisible over the left half of the screen until
    // touched, then drawn where the thumb landed.
    public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        const float Radius = 78f;

        RectTransform _self;
        RectTransform _ring;
        RectTransform _knob;
        int _pointerId = -99;

        public Vector3 Value { get; private set; }

        public static TouchJoystick Create(RectTransform parent)
        {
            var zone = new GameObject("JoystickZone", typeof(RectTransform));
            zone.transform.SetParent(parent, false);
            var rect = zone.GetComponent<RectTransform>();
            // Left 46%, stopping short of the very top so it can't swallow
            // taps meant for the pause button or the HUD.
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0.46f, 0.88f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Transparent, but it still has to be a Graphic to receive the
            // raycast — a fully invisible Image is not raycast at all, hence
            // the sliver of alpha rather than zero.
            var catcher = zone.AddComponent<Image>();
            catcher.color = new Color(1f, 1f, 1f, 0.004f);

            var stick = zone.AddComponent<TouchJoystick>();
            stick._self = rect;
            stick._ring = MakeCircle(rect, "Ring", Radius * 2f, new Color(1f, 1f, 1f, 0.18f));
            stick._knob = MakeCircle(stick._ring, "Knob", Radius * 0.95f, new Color(1f, 1f, 1f, 0.38f));
            stick._ring.gameObject.SetActive(false);
            return stick;
        }

        static RectTransform MakeCircle(Transform parent, string name, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);

            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            image.sprite = CircleSprite.Get();
            return rect;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerId = eventData.pointerId;
            _ring.gameObject.SetActive(true);
            PlaceRing(eventData);
            _knob.anchoredPosition = Vector2.zero;
            Value = Vector3.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _self, eventData.position, eventData.pressEventCamera, out Vector2 local);

            Vector2 offset = local - _ring.anchoredPosition;
            Vector2 clamped = Vector2.ClampMagnitude(offset, Radius);
            _knob.anchoredPosition = clamped;

            // A small dead zone so resting a thumb doesn't drift the gorilla.
            Vector2 normalized = clamped / Radius;
            Value = normalized.magnitude < 0.16f
                ? Vector3.zero
                : new Vector3(normalized.x, 0f, normalized.y);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            _pointerId = -99;
            Value = Vector3.zero;
            _ring.gameObject.SetActive(false);
        }

        void PlaceRing(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _self, eventData.position, eventData.pressEventCamera, out Vector2 local);
            _ring.anchoredPosition = local;
        }
    }

    // One ability button. Implements the pointer handlers directly rather
    // than using Button, because an ability needs both "was tapped" and
    // "is being held" (the slam's charge), and Button only reports clicks.
    public class TouchButtonWidget : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public bool IsHeld { get; private set; }
        float _pressQueuedAt = float.NegativeInfinity;
        Image _background;
        Color _idleColor;

        // A queued press is good for a short window and no longer. It is
        // deliberately more than one frame — that's input buffering, so a
        // tap during an animation still lands — but bounded, because an
        // ability that never reads its button (a locked one, whose Update
        // returns early) would otherwise bank the tap forever and fire it
        // the instant the tech tree unlocks it.
        const float PressLifetime = 0.25f;

        public bool ConsumePress()
        {
            if (Time.unscaledTime - _pressQueuedAt > PressLifetime) return false;
            _pressQueuedAt = float.NegativeInfinity;
            return true;
        }

        // Dimmed when the ability behind it isn't available yet, so the
        // buttons read the same way the ability bar does.
        public void SetAvailable(bool available)
        {
            _background.color = available ? _idleColor : new Color(0.3f, 0.3f, 0.3f, 0.25f);
            _background.raycastTarget = available;
        }

        public static TouchButtonWidget Create(RectTransform parent, string label, Sprite icon,
            Vector2 anchoredPosition, float size, Color tint)
        {
            var go = new GameObject("TouchButton_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(size, size);

            var background = go.AddComponent<Image>();
            background.sprite = CircleSprite.Get();
            background.color = new Color(tint.r, tint.g, tint.b, 0.45f);

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(go.transform, false);
            var iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(size * 0.2f, size * 0.2f);
            iconRect.offsetMax = new Vector2(-size * 0.2f, -size * 0.2f);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            var widget = go.AddComponent<TouchButtonWidget>();
            widget._background = background;
            widget._idleColor = background.color;
            return widget;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsHeld = true;
            _pressQueuedAt = Time.unscaledTime;
            _background.color = new Color(_idleColor.r, _idleColor.g, _idleColor.b, 0.85f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsHeld = false;
            _background.color = _idleColor;
        }
    }

    // A filled circle, generated once. uGUI ships no built-in round sprite
    // and the whole project is procedural anyway.
    public static class CircleSprite
    {
        static Sprite _sprite;

        public static Sprite Get()
        {
            if (_sprite != null) return _sprite;

            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color32[size * size];
            float centre = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Sqrt((x - centre) * (x - centre) + (y - centre) * (y - centre));
                    // One pixel of falloff at the rim so the edge isn't a
                    // staircase at button size.
                    float alpha = Mathf.Clamp01(centre - 1f - distance);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            _sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
            return _sprite;
        }
    }
}
