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

        // Height of the reserved control strip. The game is rendered only
        // ABOVE it — the camera's viewport is shrunk to match — so a thumb
        // on a button is never on top of the fight.
        //
        // Capped against screen WIDTH, not just height. A thumb is a fixed
        // physical size, so the strip only ever needs to be about as tall as
        // a hand is wide; taking a flat third of a tall portrait phone hands
        // over far more of the screen than the controls can use, and leaves
        // the buttons swimming in empty space.
        public static float CurrentBandFraction { get; private set; } = 0.3f;

        static float ComputeBandFraction()
        {
            float w = Screen.width;
            float h = Screen.height;
            if (h <= 1f || w <= 1f) return 0.3f;

            float height = Mathf.Min(h * 0.34f, w * 0.46f);
            return Mathf.Clamp(height / h, 0.14f, 0.38f);
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

            bool touch = HasTouchScreen();
            controls._touch = touch;
            controls.BuildLayout(rect);
            go.SetActive(touch);
            controls.ApplyBandSize();

            return controls;
        }

        // Public so the HUD can size its canvas for a phone before any of
        // this exists.
        public static bool HasTouchScreen()
        {
            return Touchscreen.current != null || Application.isMobilePlatform;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        Vector2 _lastBandSize;

        void Update()
        {
            // Rotating a phone changes both how tall the strip should be and
            // how the buttons fit in it.
            if (_lastScreen.x != Screen.width || _lastScreen.y != Screen.height) ApplyBandSize();

            if (_bandRect != null && _bandRect.rect.size != _lastBandSize)
            {
                _lastBandSize = _bandRect.rect.size;
                LayoutButtons();
            }

            var player = Player.PlayerController.Instance;
            if (player == null) return;

            var beat = player.GetComponent<Player.Abilities.ChestBeatAbility>();
            var dung = player.GetComponent<Player.Abilities.DungTossAbility>();
            var slam = player.GetComponent<Player.PlayerAttack>();
            var swipe = player.GetComponent<Player.QuickSwipeAttack>();

            bool beatReady = beat != null && beat.Unlocked;
            bool dungReady = dung != null && dung.Unlocked;
            _buttons[(int)TouchButton.ChestBeat].SetAvailable(beatReady);
            _buttons[(int)TouchButton.DungToss].SetAvailable(dungReady);

            _buttons[(int)TouchButton.Slam].SetCooldown(slam != null ? slam.AttackCooldownRemaining01() : 0f);
            _buttons[(int)TouchButton.Swipe].SetCooldown(swipe != null ? swipe.SwipeCooldownRemaining01() : 0f);
            _buttons[(int)TouchButton.Dash].SetCooldown(player.DashCooldownRemaining01());
            _buttons[(int)TouchButton.ChestBeat].SetCooldown(beatReady ? beat.CooldownRemaining01() : 0f);
            _buttons[(int)TouchButton.DungToss].SetCooldown(dungReady ? dung.CooldownRemaining01() : 0f);
        }

        void BuildLayout(RectTransform root)
        {
            // The strip itself. Opaque, because the camera does not clear
            // outside its own viewport — without something solid here the
            // bottom of the screen shows whatever was in the buffer last.
            var band = new GameObject("ControlBand", typeof(RectTransform));
            band.transform.SetParent(root, false);
            var bandRect = band.GetComponent<RectTransform>();
            bandRect.anchorMin = Vector2.zero;
            // Overwritten immediately by ApplyBandSize, which derives the
            // real height from the screen.
            bandRect.anchorMax = new Vector2(1f, CurrentBandFraction);
            bandRect.offsetMin = Vector2.zero;
            bandRect.offsetMax = Vector2.zero;
            var bandImage = band.AddComponent<Image>();
            bandImage.color = new Color(0.09f, 0.09f, 0.11f, 1f);
            // Swallows any tap that misses a control, so a stray thumb can't
            // fall through to the world behind.
            bandImage.raycastTarget = true;

            // A lip along the top edge, so the strip reads as a bezel rather
            // than the picture having been cropped.
            var lip = new GameObject("BandLip", typeof(RectTransform));
            lip.transform.SetParent(band.transform, false);
            var lipRect = lip.GetComponent<RectTransform>();
            lipRect.anchorMin = new Vector2(0f, 1f);
            lipRect.anchorMax = new Vector2(1f, 1f);
            lipRect.pivot = new Vector2(0.5f, 1f);
            lipRect.sizeDelta = new Vector2(0f, 3f);
            var lipImage = lip.AddComponent<Image>();
            lipImage.color = new Color(0.32f, 0.30f, 0.26f, 1f);
            lipImage.raycastTarget = false;

            _joystick = TouchJoystick.Create(bandRect);
            _bandRect = bandRect;

            // Abilities in ONE row along the left of the strip, ordered as
            // they are on a keyboard: the two attacks, then dash, then the
            // two unlockables. Positions and sizes are computed from the
            // strip's real height in LayoutButtons — a phone in landscape is
            // much wider than 16:9, which makes the canvas SHORTER in
            // reference units, and a two-row layout with fixed sizes stopped
            // fitting the moment the aspect changed.
            _buttons[(int)TouchButton.Swipe] = TouchButtonWidget.Create(bandRect, "LMB", PixelArtIcons.Claw(),
                new Color(0.75f, 0.70f, 0.35f));
            _buttons[(int)TouchButton.Slam] = TouchButtonWidget.Create(bandRect, "RMB", PixelArtIcons.Slam(),
                new Color(0.55f, 0.55f, 0.55f));
            _buttons[(int)TouchButton.Dash] = TouchButtonWidget.Create(bandRect, "Dash", PixelArtIcons.Dash(),
                new Color(0.25f, 0.50f, 0.85f));
            _buttons[(int)TouchButton.ChestBeat] = TouchButtonWidget.Create(bandRect, "Q", PixelArtIcons.GorillaShout(),
                new Color(0.62f, 0.45f, 0.72f));
            _buttons[(int)TouchButton.DungToss] = TouchButtonWidget.Create(bandRect, "E", PixelArtIcons.DungToss(),
                new Color(0.52f, 0.44f, 0.30f));

            LayoutButtons();
        }

        RectTransform _bandRect;
        bool _touch;
        Vector2 _lastScreen;

        // Recomputes the strip's height and hands the rest of the screen to
        // the camera. Called on build and whenever the screen changes, which
        // on a phone includes rotating it.
        void ApplyBandSize()
        {
            CurrentBandFraction = ComputeBandFraction();

            if (_bandRect != null)
            {
                _bandRect.anchorMax = new Vector2(1f, CurrentBandFraction);
            }

            // Set explicitly either way: the camera object survives a scene
            // reload, so a rect left over from a previous session would
            // otherwise persist into a desktop run.
            var cam = Camera.main;
            if (cam != null)
            {
                cam.rect = _touch
                    ? new Rect(0f, CurrentBandFraction, 1f, 1f - CurrentBandFraction)
                    : new Rect(0f, 0f, 1f, 1f);
            }

            _lastScreen = new Vector2(Screen.width, Screen.height);
        }

        // Sizes the row to whatever the strip actually is. Buttons take most
        // of the strip's height and are spread across its left 56%, leaving
        // the right for the stick.
        void LayoutButtons()
        {
            if (_bandRect == null) return;

            float bandHeight = _bandRect.rect.height;
            float bandWidth = _bandRect.rect.width;
            if (bandHeight <= 1f || bandWidth <= 1f) return;

            // A margin at the screen edge, or the first button is half off it.
            float pad = bandWidth * 0.022f;
            // Most of the strip's width: the stick needs far less room than
            // five buttons do, and the buttons were sitting a long way from
            // it with dead space between.
            float usable = bandWidth * 0.70f - pad * 2f;

            // Two candidate arrangements, and whichever gives BIGGER buttons
            // wins. On a wide landscape screen the strip is short and one row
            // is best; on a tall portrait one it is deep enough that two rows
            // roughly doubles how big each button can be, which is the whole
            // difference between comfortable and fiddly.
            float oneRow = Mathf.Min(bandHeight * 0.80f, usable / 5.4f);
            float twoRow = Mathf.Min(bandHeight * 0.46f, usable / 3.4f);

            if (twoRow > oneRow) LayoutTwoRows(twoRow, pad, usable);
            else LayoutOneRow(oneRow, pad, usable);

            _joystick?.Resize(Mathf.Min(bandHeight * 0.42f, bandWidth * 0.12f));
        }

        void LayoutOneRow(float size, float pad, float usable)
        {
            float gap = size * 0.14f;
            float total = _buttons.Length * size + (_buttons.Length - 1) * gap;
            float startX = pad + (usable - total) * 0.5f + size * 0.5f;

            for (int i = 0; i < _buttons.Length; i++)
            {
                Place(_buttons[i], size, new Vector2(startX + i * (size + gap), 0f));
            }
        }

        // Three on the bottom — the attacks and the dash, the ones reached
        // for constantly — with the two unlockables above them.
        void LayoutTwoRows(float size, float pad, float usable)
        {
            float gap = size * 0.16f;
            float rowOffset = (size + gap) * 0.5f;

            var bottom = new[] { TouchButton.Swipe, TouchButton.Slam, TouchButton.Dash };
            float bottomTotal = bottom.Length * size + (bottom.Length - 1) * gap;
            float bottomStart = pad + (usable - bottomTotal) * 0.5f + size * 0.5f;
            for (int i = 0; i < bottom.Length; i++)
            {
                Place(_buttons[(int)bottom[i]], size, new Vector2(bottomStart + i * (size + gap), -rowOffset));
            }

            var top = new[] { TouchButton.ChestBeat, TouchButton.DungToss };
            float topTotal = top.Length * size + (top.Length - 1) * gap;
            float topStart = pad + (usable - topTotal) * 0.5f + size * 0.5f;
            for (int i = 0; i < top.Length; i++)
            {
                Place(_buttons[(int)top[i]], size, new Vector2(topStart + i * (size + gap), rowOffset));
            }
        }

        static void Place(TouchButtonWidget button, float size, Vector2 position)
        {
            var rect = button.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = position;
        }
    }

    // A FIXED stick sitting on the right of the control strip. It used to
    // float — appearing wherever the thumb landed anywhere in the right half
    // of the screen — which put it on top of the fight and gave it no
    // resting place to find without looking.
    public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        // Set by Resize from the strip's real size; this is only the value
        // used before the first layout pass runs.
        float _radius = 92f;

        RectTransform _self;
        RectTransform _ring;
        RectTransform _knob;
        int _pointerId = -99;

        public Vector3 Value { get; private set; }

        public static TouchJoystick Create(RectTransform parent)
        {
            // The whole right half of the strip is draggable, so the thumb
            // doesn't have to land exactly on the stick to steer — but the
            // stick itself stays put.
            var zone = new GameObject("JoystickZone", typeof(RectTransform));
            zone.transform.SetParent(parent, false);
            var rect = zone.GetComponent<RectTransform>();
            // The buttons take the left 70%, so the stick owns what's left.
            rect.anchorMin = new Vector2(0.70f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Transparent, but it still has to be a Graphic to receive the
            // raycast — a fully invisible Image is not raycast at all, hence
            // the sliver of alpha rather than zero.
            var catcher = zone.AddComponent<Image>();
            catcher.color = new Color(1f, 1f, 1f, 0.004f);

            var stick = zone.AddComponent<TouchJoystick>();
            stick._self = rect;

            stick._ring = MakeCircle(rect, "Ring", stick._radius * 2f, new Color(1f, 1f, 1f, 0.15f));
            // Centred in its zone at a fixed spot, always visible so there is
            // something to aim a thumb at without looking.
            stick._ring.anchorMin = stick._ring.anchorMax = new Vector2(0.5f, 0.5f);
            stick._ring.anchoredPosition = Vector2.zero;

            stick._knob = MakeCircle(stick._ring, "Knob", stick._radius * 0.92f, new Color(1f, 1f, 1f, 0.34f));
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

        // Scaled with the strip, like the buttons are.
        public void Resize(float radius)
        {
            if (radius <= 1f || Mathf.Approximately(radius, _radius)) return;

            _radius = radius;
            _ring.sizeDelta = new Vector2(radius * 2f, radius * 2f);
            _knob.sizeDelta = new Vector2(radius * 0.92f, radius * 0.92f);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerId = eventData.pointerId;
            Steer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;
            Steer(eventData);
        }

        // Direction is measured from the stick's FIXED centre, so pressing
        // anywhere in the zone steers immediately rather than needing a drag
        // to build up an offset.
        void Steer(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _self, eventData.position, eventData.pressEventCamera, out Vector2 local);

            // The ring is right-anchored, so its centre in the zone's own
            // coordinates has to be read from the transform rather than
            // assumed to be its anchoredPosition.
            Vector2 centre = _self.InverseTransformPoint(_ring.position);

            Vector2 clamped = Vector2.ClampMagnitude(local - centre, _radius);
            _knob.anchoredPosition = clamped;

            // A small dead zone so resting a thumb doesn't drift the gorilla.
            Vector2 normalized = clamped / _radius;
            Value = normalized.magnitude < 0.16f
                ? Vector3.zero
                : new Vector3(normalized.x, 0f, normalized.y);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId) return;

            _pointerId = -99;
            Value = Vector3.zero;
            _knob.anchoredPosition = Vector2.zero;
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

        Image _cooldown;

        // 1 = just used, 0 = ready.
        public void SetCooldown(float remaining01)
        {
            if (_cooldown != null) _cooldown.fillAmount = Mathf.Clamp01(remaining01);
        }

        // Position and size are set by TouchControls.LayoutButtons, which
        // derives them from the control strip's real dimensions.
        public static TouchButtonWidget Create(RectTransform parent, string label, Sprite icon, Color tint)
        {
            var go = new GameObject("TouchButton_" + label, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            // Anchored to the strip's left edge, centred vertically in it,
            // with a CENTRE pivot so the layout code can think in centres.
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var background = go.AddComponent<Image>();
            background.sprite = CircleSprite.Get();
            background.color = new Color(tint.r, tint.g, tint.b, 0.45f);

            var iconGO = new GameObject("Icon", typeof(RectTransform));
            iconGO.transform.SetParent(go.transform, false);
            var iconRect = iconGO.GetComponent<RectTransform>();
            // Inset by anchor fraction rather than pixels, so the icon keeps
            // its proportion whatever size the layout gives the button.
            iconRect.anchorMin = new Vector2(0.19f, 0.19f);
            iconRect.anchorMax = new Vector2(0.81f, 0.81f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.sprite = icon;
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;

            // Cooldown sweep over the whole button. The desktop ability bar
            // is hidden on touch devices (it duplicates these and eats the
            // bottom of a phone screen), and it was the only cooldown
            // readout — so the buttons have to carry it themselves.
            var cooldownGO = new GameObject("Cooldown", typeof(RectTransform));
            cooldownGO.transform.SetParent(go.transform, false);
            var cdRect = cooldownGO.GetComponent<RectTransform>();
            cdRect.anchorMin = Vector2.zero;
            cdRect.anchorMax = Vector2.one;
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;
            var cooldown = cooldownGO.AddComponent<Image>();
            cooldown.sprite = CircleSprite.Get();
            cooldown.color = new Color(0f, 0f, 0f, 0.62f);
            cooldown.raycastTarget = false;
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = (int)Image.Origin360.Top;
            cooldown.fillClockwise = false;
            cooldown.fillAmount = 0f;

            var widget = go.AddComponent<TouchButtonWidget>();
            widget._background = background;
            widget._idleColor = background.color;
            widget._cooldown = cooldown;
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
