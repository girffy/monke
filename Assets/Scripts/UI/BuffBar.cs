using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GorillaSurvivors.Player;

namespace GorillaSurvivors.UI
{
    // Shows what is temporarily running on the player: powerup buffs and the
    // Silverback frenzy stacks.
    //
    // Before this, a powerup was a toast that appeared for two seconds and
    // then nothing — so for the following eight you had no way of knowing
    // whether you still had it, which is exactly the window in which it
    // matters. Each chip drains left to right as its timer runs out.
    public class BuffBar : MonoBehaviour
    {
        // Sized for the canvas's 1280x720 reference resolution.
        const float ChipWidth = 176f;
        const float ChipHeight = 26f;
        const float ChipGap = 4f;
        // Clear of the HP bar, XP bar, level, round and round-progress text.
        const float TopOffset = -150f;

        class Chip
        {
            public GameObject Root;
            public Image Fill;
            public Image Stripe;
            public Text Label;
        }

        readonly List<Chip> _chips = new List<Chip>();
        readonly List<PlayerStats.ActiveBuff> _active = new List<PlayerStats.ActiveBuff>();

        PlayerStats _stats;
        PlayerPerks _perks;
        RectTransform _root;

        public static BuffBar Create(Transform parent, PlayerStats stats)
        {
            var go = new GameObject("BuffBar", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, TopOffset);
            rect.sizeDelta = new Vector2(ChipWidth, 200f);

            var bar = go.AddComponent<BuffBar>();
            bar._root = rect;
            bar._stats = stats;
            bar._perks = stats != null ? stats.GetComponent<PlayerPerks>() : null;
            return bar;
        }

        // Colours match the powerup each buff comes from, so the chip and the
        // thing you picked up read as the same effect.
        static Color ColorFor(PlayerStats.BuffKind kind)
        {
            switch (kind)
            {
                case PlayerStats.BuffKind.Damage: return new Color(0.88f, 0.28f, 0.24f);
                case PlayerStats.BuffKind.AttackSpeed: return new Color(0.94f, 0.62f, 0.20f);
                case PlayerStats.BuffKind.MoveSpeed: return new Color(0.32f, 0.78f, 0.46f);
                case PlayerStats.BuffKind.Cooldown: return new Color(0.34f, 0.66f, 0.92f);
                default: return new Color(0.66f, 0.46f, 0.88f);
            }
        }

        static string NameFor(PlayerStats.BuffKind kind)
        {
            switch (kind)
            {
                case PlayerStats.BuffKind.Damage: return "Damage";
                case PlayerStats.BuffKind.AttackSpeed: return "Atk Speed";
                case PlayerStats.BuffKind.MoveSpeed: return "Move Speed";
                case PlayerStats.BuffKind.Cooldown: return "Cooldowns";
                default: return "Area";
            }
        }

        void Update()
        {
            if (_stats == null) return;

            _stats.GetActiveBuffs(_active);

            int used = 0;
            foreach (var buff in _active)
            {
                // Cooldown reads as a reduction, so its number is negative
                // where the others are positive.
                string magnitude = buff.Kind == PlayerStats.BuffKind.Cooldown
                    ? $"-{Mathf.RoundToInt(buff.Amount * 100f)}%"
                    : $"+{Mathf.RoundToInt(buff.Amount * 100f)}%";

                Show(used++, ColorFor(buff.Kind),
                    $"{NameFor(buff.Kind)}  <color=#ffffffaa>{magnitude}</color>",
                    buff.Remaining, buff.Fraction01);
            }

            // Frenzy isn't a powerup, but it is temporary and the player has
            // no other way to see how many stacks are up or how long is left.
            if (_perks != null && _perks.FrenzyStacks > 0)
            {
                float remaining = _perks.FrenzyRemaining;
                Show(used++, new Color(0.95f, 0.80f, 0.30f),
                    $"Frenzy x{_perks.FrenzyStacks}  <color=#ffffffaa>+{Mathf.RoundToInt(_perks.FrenzyStacks * PlayerPerks.FrenzyPerStack * 100f)}%</color>",
                    remaining, Mathf.Clamp01(remaining / PlayerPerks.FrenzyDuration));
            }

            for (int i = used; i < _chips.Count; i++)
            {
                if (_chips[i].Root.activeSelf) _chips[i].Root.SetActive(false);
            }
        }

        void Show(int index, Color color, string label, float remaining, float fraction)
        {
            while (_chips.Count <= index) _chips.Add(BuildChip(_chips.Count));

            var chip = _chips[index];
            if (!chip.Root.activeSelf) chip.Root.SetActive(true);

            chip.Stripe.color = color;
            chip.Fill.color = new Color(color.r, color.g, color.b, 0.30f);
            chip.Fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            chip.Label.text = $"{label}  <color=#ffffff88>{remaining:0.0}s</color>";
        }

        Chip BuildChip(int index)
        {
            var go = new GameObject("Chip" + index, typeof(RectTransform));
            go.transform.SetParent(_root, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -index * (ChipHeight + ChipGap));
            rect.sizeDelta = new Vector2(ChipWidth, ChipHeight);

            var background = go.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.55f);
            background.raycastTarget = false;

            // The draining fill sits behind the text and shrinks from the
            // right, so the chip empties as the buff runs out.
            var fillGO = new GameObject("Fill", typeof(RectTransform));
            fillGO.transform.SetParent(go.transform, false);
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fill = fillGO.AddComponent<Image>();
            fill.raycastTarget = false;

            // A solid bar of the buff's colour down the left edge, so the
            // chips are distinguishable at a glance without reading them.
            var stripeGO = new GameObject("Stripe", typeof(RectTransform));
            stripeGO.transform.SetParent(go.transform, false);
            var stripeRect = stripeGO.GetComponent<RectTransform>();
            stripeRect.anchorMin = Vector2.zero;
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.sizeDelta = new Vector2(4f, 0f);
            var stripe = stripeGO.AddComponent<Image>();
            stripe.raycastTarget = false;

            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);
            var labelRect = labelGO.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 0f);
            labelRect.offsetMax = new Vector2(-6f, 0f);

            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.supportRichText = true;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;

            go.SetActive(false);
            return new Chip { Root = go, Fill = fill, Stripe = stripe, Label = label };
        }
    }
}
