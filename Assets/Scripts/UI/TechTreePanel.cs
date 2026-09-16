using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.UI
{
    // The between-rounds spending screen.
    //
    // Laid out as one column per ability, nodes stacked in unlock order, so
    // the shape of the choice is visible at a glance: going deep on one
    // ability is visibly a column you are climbing while the others stay
    // shallow. That is the whole reason this replaced three random cards —
    // cards showed you what you could have, not what you were giving up.
    public class TechTreePanel : MonoBehaviour
    {
        // The grid is laid out in ANCHOR fractions of its container rather
        // than in pixels. The canvas scaler matches on width, so the height
        // available in reference units changes with the window's aspect —
        // a pixel layout authored for 1280x720 ran off the bottom of the
        // screen on anything wider, and will again on a phone.
        const float HeaderFraction = 0.075f;
        const float PadX = 0.004f;
        const float PadY = 0.007f;

        class NodeView
        {
            public TechNode Node;
            public Button Button;
            public Image Image;
            public Text Label;
            public Color Tint;
        }

        readonly List<NodeView> _views = new List<NodeView>();
        GameObject _root;
        Text _title;
        Text _doneLabel;
        TechTreeState _state;

        static readonly Color LockedFill = new Color(0.10f, 0.10f, 0.11f, 0.95f);
        static readonly Color MaxedFill = new Color(0.16f, 0.30f, 0.18f, 0.97f);

        public static TechTreePanel Create(Transform parent, TechTreeState state)
        {
            var go = new GameObject("TechTreePanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            go.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.975f);

            var panel = go.AddComponent<TechTreePanel>();
            panel._root = go;
            panel._state = state;
            panel.BuildLayout();
            go.SetActive(false);
            return panel;
        }

        void BuildLayout()
        {
            _title = MakeText(_root.transform, "Title", new Vector2(0f, -30f), new Vector2(1100f, 40f),
                24, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f));

            // Everything below the title and above the Done button.
            var grid = new GameObject("Grid", typeof(RectTransform));
            grid.transform.SetParent(_root.transform, false);
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(16f, 62f);
            gridRect.offsetMax = new Vector2(-16f, -64f);

            // Each branch gets horizontal space in proportion to how many
            // columns of nodes it holds, so a three-column branch isn't
            // squeezed into the same width as a two-column one.
            var branches = TechTree.Branches;
            int totalColumns = 0;
            int rows = 0;
            foreach (var b in branches)
            {
                totalColumns += b.Columns;
                foreach (var n in b.Nodes) rows = Mathf.Max(rows, n.Row + 1);
            }
            float rowFraction = (1f - HeaderFraction) / rows;

            int columnCursor = 0;
            for (int bi = 0; bi < branches.Count; bi++)
            {
                var branch = branches[bi];
                float bx0 = columnCursor / (float)totalColumns;
                float bx1 = (columnCursor + branch.Columns) / (float)totalColumns;
                columnCursor += branch.Columns;

                var header = MakeStretched(gridRect, "Header" + bi,
                    new Vector2(bx0 + PadX, 1f - HeaderFraction + PadY), new Vector2(bx1 - PadX, 1f));
                var headerText = AddText(header, 16, TextAnchor.MiddleCenter);
                headerText.text = $"<b>{branch.Name}</b>\n<size=11>{branch.Blurb}</size>";
                headerText.color = branch.Tint;

                // A faint plate behind each branch so the three read as three
                // trees rather than one wall of boxes.
                var plate = MakeStretched(gridRect, "Plate" + bi,
                    new Vector2(bx0 + PadX * 0.5f, 0f), new Vector2(bx1 - PadX * 0.5f, 1f - HeaderFraction));
                var plateImage = plate.gameObject.AddComponent<Image>();
                plateImage.color = new Color(branch.Tint.r, branch.Tint.g, branch.Tint.b, 0.055f);
                plateImage.raycastTarget = false;

                float columnSpan = (bx1 - bx0) / branch.Columns;
                foreach (var node in branch.Nodes)
                {
                    float x0 = bx0 + node.Column * columnSpan;
                    float x1 = x0 + columnSpan;
                    float yTop = 1f - HeaderFraction - node.Row * rowFraction;

                    _views.Add(MakeNode(node, branch.Tint, gridRect,
                        new Vector2(x0 + PadX, yTop - rowFraction + PadY), new Vector2(x1 - PadX, yTop - PadY)));
                }
            }

            var doneGO = new GameObject("Done", typeof(RectTransform));
            doneGO.transform.SetParent(_root.transform, false);
            var doneRect = doneGO.GetComponent<RectTransform>();
            doneRect.anchorMin = doneRect.anchorMax = new Vector2(0.5f, 0f);
            doneRect.pivot = new Vector2(0.5f, 0f);
            doneRect.anchoredPosition = new Vector2(0f, 12f);
            doneRect.sizeDelta = new Vector2(300f, 40f);
            doneGO.AddComponent<Image>().color = new Color(0.20f, 0.24f, 0.20f, 0.96f);
            doneGO.AddComponent<Button>().onClick.AddListener(Close);

            _doneLabel = MakeText(doneGO.transform, "Label", Vector2.zero, new Vector2(300f, 40f),
                18, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        }

        static RectTransform MakeStretched(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        static Text AddText(RectTransform parent, int fontSize, TextAnchor anchor)
        {
            var text = parent.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        NodeView MakeNode(TechNode node, Color tint, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var rect = MakeStretched(parent, "Node_" + node.Id, anchorMin, anchorMax);
            var go = rect.gameObject;

            var image = go.AddComponent<Image>();
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => Take(node));

            var labelRect = MakeStretched(rect, "Label", Vector2.zero, Vector2.one);
            labelRect.offsetMin = new Vector2(7f, 5f);
            labelRect.offsetMax = new Vector2(-7f, -5f);
            var label = AddText(labelRect, 13, TextAnchor.MiddleCenter);

            return new NodeView { Node = node, Button = button, Image = image, Label = label, Tint = tint };
        }

        static Text MakeText(Transform parent, string name, Vector2 position, Vector2 size,
            int fontSize, TextAnchor anchor, Vector2 pivot)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, pivot.y);
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        public void Show()
        {
            // The HUD's pause button, ability bar and toast are built after
            // this panel, and uGUI draws later siblings on top — without this
            // they punch through the middle of the tech tree.
            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
            Refresh();
        }

        void Take(TechNode node)
        {
            if (!_state.Take(node)) return;

            Sfx.LevelUp();

            // Spending the last point ends the screen on its own, so the
            // common case (one point, one pick) needs no extra click.
            if (_state.AvailablePoints <= 0)
            {
                Close();
                return;
            }
            Refresh();
        }

        void Close()
        {
            _root.SetActive(false);
            GameManager.Instance.ResolveUpgradeChoice();
        }

        void Refresh()
        {
            int points = _state.AvailablePoints;
            _title.text = points == 1
                ? $"Round {GameManager.Instance.CurrentRound} cleared — <color=#ffd863>1 point</color> to spend"
                : $"Round {GameManager.Instance.CurrentRound} cleared — <color=#ffd863>{points} points</color> to spend";

            _doneLabel.text = points > 0 ? $"Save {points} for later" : "Continue";

            foreach (var view in _views)
            {
                int rank = _state.RankOf(view.Node.Id);
                bool maxed = rank >= view.Node.MaxRank;
                bool unlocked = _state.IsUnlocked(view.Node);
                bool affordable = _state.CanTake(view.Node);

                view.Button.interactable = affordable;

                if (maxed)
                {
                    view.Image.color = MaxedFill;
                }
                else if (affordable)
                {
                    // Only nodes you can actually buy right now are lit, so
                    // the screen reads as a short list of real options rather
                    // than thirty boxes of text.
                    view.Image.color = new Color(view.Tint.r * 0.42f, view.Tint.g * 0.42f, view.Tint.b * 0.42f, 0.97f);
                }
                else
                {
                    view.Image.color = LockedFill;
                }

                string rankTag = view.Node.MaxRank > 1 ? $"  <color=#9fd0ff>{rank}/{view.Node.MaxRank}</color>" : "";
                string tick = maxed ? "<color=#8fe08f>✓</color> " : "";

                if (!unlocked)
                {
                    // A locked node still shows what it DOES, not just what
                    // it needs — the whole point of seeing the tree early is
                    // planning a route through it, which you can't do if the
                    // deep nodes are blank until you're already standing on
                    // them. The prerequisite goes on its own dimmer line.
                    string reason = _state.LockReason(view.Node);
                    view.Label.text =
                        $"<color=#6e6e6e><b>{view.Node.Title}</b>{rankTag}\n"
                        + $"<size=11>{view.Node.Description}</size>"
                        + (reason == null ? "" : $"\n<size=10><color=#4f4f4f>{reason}</color></size>")
                        + "</color>";
                }
                else
                {
                    string body = maxed ? "<color=#7f9a7f>" : "<color=#c8c8c8>";
                    view.Label.text = $"{tick}<b>{view.Node.Title}</b>{rankTag}\n{body}<size=11>{view.Node.Description}</size></color>";
                }
            }
        }
    }
}
