using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GorillaSurvivors.Core;
using GorillaSurvivors.Enemies;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.UI
{
    // Red arrows pinned to the screen edge pointing at enemies that are off
    // camera. Only shown when hardly anything is left on screen, which is
    // exactly the moment they matter: the tail of a round where two medics
    // are loitering somewhere off in the dark and the player is left
    // wandering to find them.
    public class OffscreenEnemyMarkers : MonoBehaviour
    {
        // While a fight is on screen the arrows would be pure noise, so they
        // only appear once the visible enemy count drops to about this.
        public int ShowWhenVisibleAtMost = 2;
        public int MaxArrows = 6;
        public float EdgePadding = 46f;
        public float RefreshInterval = 0.1f;

        readonly List<RectTransform> _arrows = new List<RectTransform>();
        RectTransform _canvasRect;
        float _nextRefresh;

        public static OffscreenEnemyMarkers Create(Transform canvas)
        {
            var go = new GameObject("OffscreenEnemyMarkers", typeof(RectTransform));
            go.transform.SetParent(canvas, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var markers = go.AddComponent<OffscreenEnemyMarkers>();
            markers._canvasRect = canvas as RectTransform;
            return markers;
        }

        void Update()
        {
            if (Time.time < _nextRefresh) return;
            _nextRefresh = Time.time + RefreshInterval;

            var cam = Camera.main;
            if (cam == null || _canvasRect == null || PlayerController.Instance == null)
            {
                HideFrom(0);
                return;
            }

            var enemies = Object.FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            var offscreen = new List<Vector3>();
            int visible = 0;

            foreach (var enemy in enemies)
            {
                Vector3 viewport = cam.WorldToViewportPoint(enemy.transform.position);
                bool onScreen = viewport.z > 0f
                    && viewport.x > 0.02f && viewport.x < 0.98f
                    && viewport.y > 0.02f && viewport.y < 0.98f;

                if (onScreen) visible++;
                else offscreen.Add(enemy.transform.position);
            }

            if (visible > ShowWhenVisibleAtMost || offscreen.Count == 0)
            {
                HideFrom(0);
                return;
            }

            // Nearest first, so the handful of arrows shown point at the ones
            // worth walking toward.
            Vector3 playerPos = PlayerController.Instance.transform.position;
            offscreen.Sort((a, b) => (a - playerPos).sqrMagnitude.CompareTo((b - playerPos).sqrMagnitude));

            int shown = Mathf.Min(offscreen.Count, MaxArrows);
            for (int i = 0; i < shown; i++)
            {
                PositionArrow(GetArrow(i), offscreen[i], playerPos);
            }
            HideFrom(shown);
        }

        void PositionArrow(RectTransform arrow, Vector3 target, Vector3 playerPos)
        {
            // Direction is taken on the ground plane from the player rather
            // than from projected screen points: a target behind the camera
            // projects to a mirrored position, which would point arrows the
            // wrong way exactly when they're needed.
            Vector3 toTarget = target - playerPos;
            Vector2 dir = new Vector2(toTarget.x, toTarget.z).normalized;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;

            // The camera looks down a fixed pitch with no yaw, so world X maps
            // to screen X and world Z maps to screen Y directly.
            //
            // The arrows ride the edge of the VISIBLE picture, not the edge
            // of the canvas: on a touch device the bottom of the screen is a
            // control strip the camera doesn't render into, and arrows placed
            // against the canvas edge would sit behind it.
            Vector2 size = _canvasRect.rect.size;
            float band = TouchControls.Active ? size.y * TouchControls.CurrentBandFraction : 0f;
            Vector2 half = new Vector2(size.x, size.y - band) * 0.5f - Vector2.one * EdgePadding;
            float centreY = band * 0.5f;

            // Push the direction out to whichever edge it hits first.
            float scale = Mathf.Min(
                half.x / Mathf.Max(0.0001f, Mathf.Abs(dir.x)),
                half.y / Mathf.Max(0.0001f, Mathf.Abs(dir.y)));

            arrow.anchoredPosition = dir * scale + Vector2.up * centreY;
            // The chevron glyph points along +X, so aiming it is just the
            // heading angle with no extra offset.
            arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            arrow.gameObject.SetActive(true);
        }

        RectTransform GetArrow(int index)
        {
            while (_arrows.Count <= index) _arrows.Add(CreateArrow());
            return _arrows[index];
        }

        RectTransform CreateArrow()
        {
            var go = new GameObject("Arrow", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            // Half again the size, fully opaque, and sitting on a dark disc.
            // These only appear when the arena is nearly empty and you are
            // hunting the last two men — at which point the whole job of the
            // marker is to be seen from the corner of your eye, and a small
            // translucent chevron against sand was not managing it.
            rect.sizeDelta = new Vector2(52f, 52f);

            var backing = new GameObject("Backing", typeof(RectTransform));
            backing.transform.SetParent(rect, false);
            var backRect = backing.GetComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = new Vector2(-5f, -5f);
            backRect.offsetMax = new Vector2(5f, 5f);
            var backImage = backing.AddComponent<Image>();
            backImage.sprite = PlaceholderSprites.Circle(Color.white, 48);
            backImage.color = new Color(0.10f, 0.05f, 0.05f, 0.75f);
            backImage.raycastTarget = false;

            var glyph = new GameObject("Glyph", typeof(RectTransform));
            glyph.transform.SetParent(rect, false);
            var glyphRect = glyph.GetComponent<RectTransform>();
            glyphRect.anchorMin = Vector2.zero;
            glyphRect.anchorMax = Vector2.one;
            glyphRect.offsetMin = Vector2.zero;
            glyphRect.offsetMax = Vector2.zero;
            var image = glyph.AddComponent<Image>();
            image.sprite = PlaceholderSprites.Icon(PlaceholderSprites.IconShape.Chevron, Color.white, 48);
            image.color = new Color(1f, 0.30f, 0.22f, 1f);
            image.raycastTarget = false;

            rect.gameObject.AddComponent<PulsingMarker>();
            return rect;
        }

        void HideFrom(int index)
        {
            for (int i = index; i < _arrows.Count; i++)
            {
                if (_arrows[i] != null) _arrows[i].gameObject.SetActive(false);
            }
        }
    }

    // A slow throb. Motion is what the eye catches at the edge of vision,
    // which is exactly where these sit and exactly who they are for.
    public class PulsingMarker : MonoBehaviour
    {
        RectTransform _rect;

        void Awake() => _rect = GetComponent<RectTransform>();

        void Update()
        {
            if (_rect == null) return;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);
            _rect.localScale = new Vector3(pulse, pulse, 1f);
        }
    }
}
