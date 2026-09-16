using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GorillaSurvivors.Core;

namespace GorillaSurvivors.UI
{
    // The between-rounds spending screen.
    //
    // Shows ONE branch at a time behind tabs. All three at once fitted while
    // the tree was a grid of columns, but a real tree needs room for its
    // edges — and the edges are the whole point, since which node you took
    // is now what opens the next one.
    //
    // Nodes are placed from their (Row, Column) in half-column units, which
    // is what lets a trunk sit centred between the limbs it feeds.
    public class TechTreePanel : MonoBehaviour
    {
        // Small: it is a fraction of the SCROLLABLE content, which is taller
        // than the window, so a generous-looking fraction turns into a large
        // band of empty space above the trunk.
        const float HeaderFraction = 0.045f;
        const float PadX = 0.006f;
        const float PadY = 0.012f;

        class NodeView
        {
            public TechNode Node;
            public Button Button;
            public Image Image;
            public Text Label;
            public Image Frame;
            public Color Tint;
            public GameObject Root;
        }

        class BranchView
        {
            public TechBranch Branch;
            public GameObject Page;
            public RectTransform Content;
            public int Rows;
            public Button Tab;
            public Image TabImage;
            public Text TabLabel;
            public readonly List<Image> Edges = new List<Image>();
            // Computed placement, by node id: see Layout.
            public Dictionary<string, Slot> Slots;
        }

        // Where one node sits: centre and width as fractions of the page.
        public struct Slot
        {
            public float X;
            public float Width;
            public int Row;
            public int IndexInRow;
        }

        // Positions are COMPUTED, not authored.
        //
        // Hand-written half-column numbers had every row laid out against the
        // same fixed grid, which meant a parent only landed above the middle
        // of its children by luck — mostly it sat off to one side, and the
        // trunk itself wasn't at the centre of the page.
        //
        // So: spread the widest row evenly, then walk outward from it, giving
        // every other node the average position of the nodes it connects to.
        // A parent is then always exactly over the centre of its children,
        // and the trunk lands dead centre because the average of an evenly
        // spread row is its middle.
        static Dictionary<string, Slot> Layout(TechBranch branch, int rows)
        {
            var byRow = new List<TechNode>[rows];
            for (int r = 0; r < rows; r++) byRow[r] = new List<TechNode>();
            foreach (var node in branch.Nodes) byRow[node.Row].Add(node);

            int widest = 0;
            for (int r = 0; r < rows; r++)
            {
                if (byRow[r].Count > byRow[widest].Count) widest = r;
            }

            var slots = new Dictionary<string, Slot>();
            var x = new Dictionary<string, float>();

            var seed = byRow[widest];
            for (int i = 0; i < seed.Count; i++) x[seed[i].Id] = (i + 0.5f) / seed.Count;

            // Above the widest row, a node follows its CHILDREN.
            for (int r = widest - 1; r >= 0; r--)
            {
                foreach (var node in byRow[r]) x[node.Id] = AverageOf(ChildrenOf(branch, node, byRow[r + 1]), x, 0.5f);
            }

            // Below it, a node follows its PARENTS.
            for (int r = widest + 1; r < rows; r++)
            {
                foreach (var node in byRow[r]) x[node.Id] = AverageOf(ParentsIn(branch, node, byRow[r - 1]), x, 0.5f);
            }

            for (int r = 0; r < rows; r++)
            {
                // A row with fewer nodes gets wider boxes, up to a limit —
                // this is what un-squashes the trunk and the capstone, which
                // carry the longest text and previously had to fit the same
                // narrow column as the leaves.
                float width = Mathf.Min(0.94f / Mathf.Max(1, byRow[r].Count), 0.32f);
                for (int i = 0; i < byRow[r].Count; i++)
                {
                    var node = byRow[r][i];
                    slots[node.Id] = new Slot
                    {
                        X = x.TryGetValue(node.Id, out float v) ? v : 0.5f,
                        Width = width, Row = r, IndexInRow = i,
                    };
                }
            }

            return slots;
        }

        static List<TechNode> ChildrenOf(TechBranch branch, TechNode node, List<TechNode> below)
        {
            var found = new List<TechNode>();
            foreach (var candidate in below)
            {
                if (candidate.Parents == null) continue;
                foreach (var id in candidate.Parents)
                {
                    if (id != node.Id) continue;
                    found.Add(candidate);
                    break;
                }
            }
            return found;
        }

        static List<TechNode> ParentsIn(TechBranch branch, TechNode node, List<TechNode> above)
        {
            var found = new List<TechNode>();
            if (node.Parents == null) return found;
            foreach (var id in node.Parents)
            {
                foreach (var candidate in above)
                {
                    if (candidate.Id == id) found.Add(candidate);
                }
            }
            return found;
        }

        static float AverageOf(List<TechNode> nodes, Dictionary<string, float> x, float fallback)
        {
            float sum = 0f;
            int count = 0;
            foreach (var node in nodes)
            {
                if (!x.TryGetValue(node.Id, out float v)) continue;
                sum += v;
                count++;
            }
            return count == 0 ? fallback : sum / count;
        }

        // Smallest a tree row may be, in canvas units. Three lines of node
        // text plus padding; below this the descriptions get clipped.
        const float MinRowHeight = 92f;

        readonly List<NodeView> _views = new List<NodeView>();
        readonly List<BranchView> _pages = new List<BranchView>();

        GameObject _root;
        Text _title;
        Text _doneLabel;
        TechTreeState _state;
        int _active;

        static readonly Color LockedFill = new Color(0.135f, 0.135f, 0.15f, 1f);
        static readonly Color LockedFrame = new Color(0.27f, 0.27f, 0.30f, 1f);
        static readonly Color MaxedFill = new Color(0.16f, 0.30f, 0.18f, 0.97f);
        static readonly Color EdgeIdle = new Color(0.32f, 0.32f, 0.34f, 0.85f);

        public static TechTreePanel Create(Transform parent, TechTreeState state)
        {
            var go = new GameObject("TechTreePanel", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Fully opaque: the tree is dense enough without the arena
            // showing through the gaps between nodes.
            go.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.06f, 1f);

            var panel = go.AddComponent<TechTreePanel>();
            panel._root = go;
            panel._state = state;
            panel.BuildLayout();
            // Anything that changes the tree from outside the panel — the
            // debug grant key, a future reward — has to redraw it, or the
            // screen shows points and ranks that are already out of date.
            state.OnChanged += panel.RefreshIfOpen;
            go.SetActive(false);
            return panel;
        }

        void BuildLayout()
        {
            _title = MakeText(_root.transform, "Title", new Vector2(0f, -22f), new Vector2(1100f, 34f),
                23, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f));

            BuildTabs();

            var branches = TechTree.Branches;
            for (int i = 0; i < branches.Count; i++)
            {
                BuildPage(branches[i], _pages[i]);
            }

            var doneGO = new GameObject("Done", typeof(RectTransform));
            doneGO.transform.SetParent(_root.transform, false);
            var doneRect = doneGO.GetComponent<RectTransform>();
            doneRect.anchorMin = doneRect.anchorMax = new Vector2(0.5f, 0f);
            doneRect.pivot = new Vector2(0.5f, 0f);
            doneRect.anchoredPosition = new Vector2(0f, 10f);
            doneRect.sizeDelta = new Vector2(300f, 38f);
            doneGO.AddComponent<Image>().color = new Color(0.20f, 0.24f, 0.20f, 0.96f);
            doneGO.AddComponent<Button>().onClick.AddListener(Close);

            _doneLabel = MakeText(doneGO.transform, "Label", Vector2.zero, new Vector2(300f, 38f),
                17, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));
        }

        void BuildTabs()
        {
            var branches = TechTree.Branches;
            const float tabWidth = 190f;
            const float gap = 8f;
            float total = branches.Count * tabWidth + (branches.Count - 1) * gap;
            float startX = -total * 0.5f + tabWidth * 0.5f;

            for (int i = 0; i < branches.Count; i++)
            {
                var branch = branches[i];
                int index = i;

                var tabGO = new GameObject("Tab_" + branch.Name, typeof(RectTransform));
                tabGO.transform.SetParent(_root.transform, false);
                var rect = tabGO.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(startX + i * (tabWidth + gap), -58f);
                rect.sizeDelta = new Vector2(tabWidth, 34f);

                var image = tabGO.AddComponent<Image>();
                var button = tabGO.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() => ShowBranch(index));

                var label = MakeText(tabGO.transform, "Label", Vector2.zero, new Vector2(tabWidth, 34f),
                    15, TextAnchor.MiddleCenter, new Vector2(0.5f, 0.5f));

                // Each page is a scroll view. The tree is six rows deep and
                // every node carries three lines of text; at the larger UI
                // scale a phone uses, those lines no longer fit a row sized
                // to a fraction of the screen and the descriptions were
                // silently truncated to nothing. Rows get a real minimum
                // height now and the page scrolls if that overflows.
                var page = new GameObject("Page_" + branch.Name, typeof(RectTransform));
                page.transform.SetParent(_root.transform, false);
                var pageRect = page.GetComponent<RectTransform>();
                pageRect.anchorMin = Vector2.zero;
                pageRect.anchorMax = Vector2.one;
                pageRect.offsetMin = new Vector2(16f, 56f);
                pageRect.offsetMax = new Vector2(-16f, -98f);

                // RectMask2D rather than Mask: no stencil buffer, no extra
                // draw call, and it is all a rectangular clip needs.
                page.AddComponent<RectMask2D>();

                var content = new GameObject("Content", typeof(RectTransform));
                content.transform.SetParent(page.transform, false);
                var contentRect = content.GetComponent<RectTransform>();
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.offsetMin = new Vector2(0f, contentRect.offsetMin.y);
                contentRect.offsetMax = new Vector2(0f, contentRect.offsetMax.y);

                var scroll = page.AddComponent<ScrollRect>();
                scroll.content = contentRect;
                scroll.viewport = pageRect;
                scroll.horizontal = false;
                scroll.vertical = true;
                scroll.movementType = ScrollRect.MovementType.Clamped;
                scroll.scrollSensitivity = 28f;
                scroll.inertia = true;
                scroll.decelerationRate = 0.12f;

                _pages.Add(new BranchView
                {
                    Branch = branch, Page = page, Tab = button, TabImage = image, TabLabel = label,
                    Content = contentRect,
                });
            }
        }

        void BuildPage(TechBranch branch, BranchView view)
        {
            // Everything lives in the scrollable content, not the page: the
            // page is only the window onto it.
            var pageRect = view.Content;

            int rows = 0;
            foreach (var n in branch.Nodes) rows = Mathf.Max(rows, n.Row + 1);
            // One extra row for the grand capstone, which is shown at the
            // foot of every branch because it is the one node all three of
            // them lead to — hiding it on two pages out of three would make
            // the convergence invisible.
            view.Slots = Layout(branch, rows);

            int grandRow = rows;
            rows += 1;
            view.Rows = rows;
            float rowFraction = (1f - HeaderFraction) / rows;

            var header = MakeStretched(pageRect, "Header",
                new Vector2(0f, 1f - HeaderFraction), new Vector2(1f, 1f));
            var headerText = AddText(header, 13, TextAnchor.MiddleCenter);
            headerText.text = branch.Blurb;
            headerText.color = new Color(branch.Tint.r, branch.Tint.g, branch.Tint.b, 0.85f);

            // Edges are drawn FIRST so nodes sit on top of them. Each is an
            // L of axis-aligned bars rather than a diagonal: a rotated line
            // can't be anchored in normalised space, and the right-angle
            // routing reads more like a tree diagram anyway.
            foreach (var node in branch.Nodes)
            {
                if (node.Parents == null) continue;
                foreach (var parentId in node.Parents)
                {
                    var parent = TechTree.Find(parentId);
                    if (parent == null || parent.Branch != branch) continue;
                    BuildEdge(pageRect, view, parent, node, rowFraction);
                }
            }

            // The branch capstone feeds the grand capstone; draw that link so
            // the page shows where the branch is going.
            var capstone = BranchCapstone(branch);
            var grand = TechTree.GrandCapstone;
            if (capstone != null)
            {
                var a = NodeBounds(view, capstone, rowFraction);
                var b = GrandBounds(branch, grandRow, rowFraction);
                float ax = (a.min.x + a.max.x) * 0.5f;
                float bx = (b.min.x + b.max.x) * 0.5f;
                float mid = (a.min.y + b.max.y) * 0.5f;
                const float t = 0.0022f;
                AddBar(pageRect, view, new Vector2(ax - t, mid), new Vector2(ax + t, a.min.y));
                AddBar(pageRect, view, new Vector2(Mathf.Min(ax, bx), mid - t * 1.6f), new Vector2(Mathf.Max(ax, bx), mid + t * 1.6f));
                AddBar(pageRect, view, new Vector2(bx - t, b.max.y), new Vector2(bx + t, mid));
            }

            foreach (var node in branch.Nodes)
            {
                var bounds = NodeBounds(view, node, rowFraction);
                _views.Add(MakeNode(node, branch.Tint, pageRect, bounds.min, bounds.max));
            }

            var grandBounds = GrandBounds(branch, grandRow, rowFraction);
            _views.Add(MakeNode(grand, new Color(0.85f, 0.62f, 0.32f), pageRect, grandBounds.min, grandBounds.max));
        }

        static TechNode BranchCapstone(TechBranch branch)
        {
            TechNode best = null;
            foreach (var node in branch.Nodes)
            {
                if (best == null || node.Row > best.Row) best = node;
            }
            return best;
        }

        // Centred, and wider than a normal node: it is not part of any one
        // branch's layout.
        static (Vector2 min, Vector2 max) GrandBounds(TechBranch branch, int row, float rowFraction)
        {
            float yTop = 1f - HeaderFraction - row * rowFraction;
            return (new Vector2(0.32f, yTop - rowFraction + PadY),
                    new Vector2(0.68f, yTop - PadY));
        }

        static (Vector2 min, Vector2 max) NodeBounds(BranchView view, TechNode node, float rowFraction)
        {
            var slot = view.Slots[node.Id];
            float x0 = slot.X - slot.Width * 0.5f;
            float x1 = slot.X + slot.Width * 0.5f;
            float yTop = 1f - HeaderFraction - slot.Row * rowFraction;

            return (new Vector2(x0 + PadX, yTop - rowFraction + PadY),
                    new Vector2(x1 - PadX, yTop - PadY));
        }

        void BuildEdge(RectTransform parentRect, BranchView view,
            TechNode from, TechNode to, float rowFraction)
        {
            var a = NodeBounds(view, from, rowFraction);
            var b = NodeBounds(view, to, rowFraction);

            float ax = (a.min.x + a.max.x) * 0.5f;
            float bx = (b.min.x + b.max.x) * 0.5f;
            float ayBottom = a.min.y;
            float byTop = b.max.y;

            // Each parent routes its horizontal run in its own lane. With
            // every edge crossing at the same height the long runs — the
            // Dung Toss limb reaching its children on the far right — merged
            // with the short ones into a single bar spanning the page, which
            // read as everything connecting to everything.
            float lane = 0.34f + 0.16f * (view.Slots[from.Id].IndexInRow % 3);
            float mid = Mathf.Lerp(byTop, ayBottom, lane);

            const float thickness = 0.0022f;

            // Down out of the parent, across, and down into the child.
            AddBar(parentRect, view, new Vector2(ax - thickness, mid), new Vector2(ax + thickness, ayBottom));
            AddBar(parentRect, view, new Vector2(Mathf.Min(ax, bx), mid - thickness * 1.6f), new Vector2(Mathf.Max(ax, bx), mid + thickness * 1.6f));
            AddBar(parentRect, view, new Vector2(bx - thickness, byTop), new Vector2(bx + thickness, mid));
        }

        void AddBar(RectTransform parentRect, BranchView view, Vector2 min, Vector2 max)
        {
            var rect = MakeStretched(parentRect, "Edge",
                new Vector2(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y)),
                new Vector2(Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y)));
            var image = rect.gameObject.AddComponent<Image>();
            image.color = EdgeIdle;
            image.raycastTarget = false;
            view.Edges.Add(image);
        }

        NodeView MakeNode(TechNode node, Color tint, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            // Every option is a BOX, whatever its state. The old locked fill
            // was within a hair of the panel's background, so a node you
            // couldn't take yet read as a hole in the tree rather than as
            // something waiting for you — and the tree looked half-empty on
            // the screen where you are choosing what to fill it with.
            var rect = MakeStretched(parent, "Node_" + node.Id, anchorMin, anchorMax);
            var go = rect.gameObject;
            var frame = go.AddComponent<Image>();

            var fillRect = MakeStretched(rect, "Fill", Vector2.zero, Vector2.one);
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);
            var image = fillRect.gameObject.AddComponent<Image>();

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => Take(node));

            var labelRect = MakeStretched(fillRect, "Label", Vector2.zero, Vector2.one);
            labelRect.offsetMin = new Vector2(7f, 5f);
            labelRect.offsetMax = new Vector2(-7f, -5f);
            var label = AddText(labelRect, 12, TextAnchor.MiddleCenter);

            return new NodeView
            {
                Node = node, Button = button, Image = image, Label = label,
                Frame = frame, Tint = tint, Root = go,
            };
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
            text.raycastTarget = false;
            return text;
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
            return AddText(rect, fontSize, anchor);
        }

        public void Show()
        {
            // The HUD's pause button, ability bar and toast are built after
            // this panel, and uGUI draws later siblings on top.
            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
            SizeContent();

            // Open on a branch the player can actually spend in, so the
            // screen doesn't land on a page where everything is locked.
            for (int i = 0; i < _pages.Count; i++)
            {
                if (!BranchHasAffordableNode(_pages[i].Branch)) continue;
                _active = i;
                break;
            }

            ShowBranch(_active);
        }

        // The content is as tall as the window, or tall enough to give every
        // row its minimum height — whichever is more. Done on open rather
        // than at build time because the viewport has no real size until the
        // panel is actually shown.
        void SizeContent()
        {
            foreach (var page in _pages)
            {
                if (page.Content == null) continue;

                var viewport = page.Page.GetComponent<RectTransform>();
                float viewportHeight = viewport.rect.height;
                float needed = page.Rows * MinRowHeight / (1f - HeaderFraction);
                float height = Mathf.Max(viewportHeight, needed);

                page.Content.sizeDelta = new Vector2(0f, height);
                page.Content.anchoredPosition = new Vector2(0f, 0f);
            }
        }

        bool BranchHasAffordableNode(TechBranch branch)
        {
            foreach (var node in branch.Nodes)
            {
                if (_state.CanTake(node)) return true;
            }
            return false;
        }

        void ShowBranch(int index)
        {
            _active = Mathf.Clamp(index, 0, _pages.Count - 1);
            for (int i = 0; i < _pages.Count; i++)
            {
                _pages[i].Page.SetActive(i == _active);
            }
            Refresh();
        }

        void Take(TechNode node)
        {
            if (!_state.Take(node)) return;

            Sfx.LevelUp();

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

        void RefreshIfOpen()
        {
            if (_root != null && _root.activeSelf) Refresh();
        }

        void OnDestroy()
        {
            if (_state != null) _state.OnChanged -= RefreshIfOpen;
        }

        void Refresh()
        {
            int points = _state.AvailablePoints;
            _title.text = points == 1
                ? $"Round {GameManager.Instance.CurrentRound} cleared — <color=#ffd863>1 point</color> to spend"
                : $"Round {GameManager.Instance.CurrentRound} cleared — <color=#ffd863>{points} points</color> to spend";

            _doneLabel.text = points > 0 ? $"Save {points} for later" : "Continue";

            for (int i = 0; i < _pages.Count; i++)
            {
                var page = _pages[i];
                var tint = page.Branch.Tint;
                bool active = i == _active;
                bool spendable = BranchHasAffordableNode(page.Branch);

                page.TabImage.color = active
                    ? new Color(tint.r * 0.45f, tint.g * 0.45f, tint.b * 0.45f, 0.98f)
                    : new Color(0.12f, 0.12f, 0.13f, 0.95f);

                int spent = _state.PointsIn(page.Branch);
                string dot = spendable && !active ? " <color=#ffd863>•</color>" : "";
                page.TabLabel.text = $"<b>{page.Branch.Name}</b> <size=11>{spent}</size>{dot}";
                page.TabLabel.color = active ? Color.white : new Color(0.72f, 0.72f, 0.72f);
            }

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
                    view.Frame.color = new Color(0.34f, 0.56f, 0.36f, 1f);
                }
                else if (affordable)
                {
                    view.Image.color = new Color(view.Tint.r * 0.42f, view.Tint.g * 0.42f, view.Tint.b * 0.42f, 0.97f);
                    view.Frame.color = new Color(view.Tint.r, view.Tint.g, view.Tint.b, 1f);
                }
                else if (unlocked)
                {
                    // Open, but there are no points left to spend on it.
                    view.Image.color = new Color(0.19f, 0.19f, 0.21f, 1f);
                    view.Frame.color = new Color(0.40f, 0.40f, 0.43f, 1f);
                }
                else
                {
                    view.Image.color = LockedFill;
                    view.Frame.color = LockedFrame;
                }

                string rankTag = view.Node.MaxRank > 1 ? $"  <color=#9fd0ff>{rank}/{view.Node.MaxRank}</color>" : "";
                string tick = maxed ? "<color=#8fe08f>✓</color> " : "";

                // No skill tag and no "needs X" line. Both were saying what
                // the diagram already says — which limb you are on, and what
                // sits above this node — and between them they took two of
                // the three lines a box has room for.
                if (!unlocked)
                {
                    view.Label.text =
                        $"<color=#7b7b7b><b>{view.Node.Title}</b>{rankTag}\n"
                        + $"<size=10>{view.Node.Description}</size></color>";
                }
                else
                {
                    string body = maxed ? "<color=#7f9a7f>" : "<color=#c8c8c8>";
                    view.Label.text = $"{tick}<b>{view.Node.Title}</b>{rankTag}\n{body}<size=10>{view.Node.Description}</size></color>";
                }
            }
        }
    }
}
