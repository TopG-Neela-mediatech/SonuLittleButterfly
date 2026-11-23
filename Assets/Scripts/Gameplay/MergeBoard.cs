using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace tmkoc.butterfly
{
    /// <summary>
    /// The merge grid. Rules mirror the HTML prototype:
    /// - tap an item to select it, tap a matching neighbour to merge, or drag it onto a neighbour;
    /// - an item can move to ANY empty cell (tap item then tap empty cell, or drag);
    /// - two equal tiers merge only when the cells are orthogonally adjacent; tier 4 never merges further.
    /// The board never spawns extra items: exactly enough tier-1 items for the level goals are placed.
    /// </summary>
    public enum MergeRule { Anywhere, OrthogonalNeighbours, NeighboursIncludingDiagonal }

    public class MergeBoard : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform boardRect;
        [SerializeField] private GridLayoutGroup grid;
        [SerializeField] private MergeCell cellTemplate;
        [SerializeField] private MergeItemView itemTemplate;
        [SerializeField] private RectTransform dragLayer;
        [SerializeField] private Sprite fallbackItemSprite;

        [Header("Layout")]
        [SerializeField] private float spacing = 12f;
        [SerializeField] private float padding = 18f;
        [SerializeField, Range(0.6f, 1f)] private float itemFill = 0.86f;
        [Tooltip("Anywhere: any two matching items merge regardless of position. Otherwise they must be neighbours.")]
        [SerializeField] private MergeRule mergeRule = MergeRule.Anywhere;

        [Header("Feedback")]
        [SerializeField] private float moveDuration = 0.18f;
        [SerializeField] private float mergeFlyDuration = 0.14f;
        [SerializeField] private Color mergeRingColor = new Color(1f, 0.92f, 0.55f, 0.9f);
        [SerializeField] private Color wrongFlashColor = new Color(1f, 0.62f, 0.58f, 1f);

        [Header("Tiles")]
        [Tooltip("Wooden tiles in a soft checker pattern.")]
        [SerializeField] private Color cellColorA = new Color(0.89f, 0.76f, 0.6f, 1f);
        [SerializeField] private Color cellColorB = new Color(0.84f, 0.7f, 0.53f, 1f);

        public event Action<int, int> OnMerged;          // (newTier, cellIndex)
        public event Action OnInvalidMove;
        public event Action OnMoved;
        public event Action OnPlayerInteracted;

        public bool Locked { get; set; }
        public int Columns => cols;
        public int Rows => rows;
        public int SelectedIndex => selected;

        private int cols, rows;
        public const int MaxSide = 4;
        private int[] tiles = new int[0];
        private readonly List<MergeCell> cells = new List<MergeCell>();
        private readonly Dictionary<int, MergeItemView> items = new Dictionary<int, MergeItemView>();
        private readonly Stack<MergeItemView> itemPool = new Stack<MergeItemView>(); // views are reused, never destroyed per move
        private readonly List<int> releaseBuffer = new List<int>();
        private ThemeData theme;
        private int selected = -1;
        private Canvas canvas;

        // drag state
        private MergeItemView dragItem;
        private Transform dragOriginalParent;
        private int dragOriginalSibling;
        private Vector2 pressScreenPos;

        private void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
            if (cellTemplate != null) cellTemplate.gameObject.SetActive(false);
            if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
        }

        public void Setup(LevelDefinition def, ThemeData themeData)
        {
            theme = themeData;
            cols = Mathf.Clamp(def.columns, 2, MaxSide); // never bigger than 4x4
            rows = Mathf.Clamp(def.rows, 2, MaxSide);
            selected = -1;
            Locked = false;
            ClearBoard();
            tiles = new int[cols * rows];
            BuildCells();
            Spawn(def.TotalSpawnCount);
            Layout();
            RenderAll();
        }

        public int GetTier(int index) => index >= 0 && index < tiles.Length ? tiles[index] : 0;
        public RectTransform GetCellRect(int index) => index >= 0 && index < cells.Count ? cells[index].Rect : null;
        public MergeItemView GetItem(int index) => items.TryGetValue(index, out var v) ? v : null;
        public Canvas Canvas => canvas;
        public Sprite FallbackSprite => fallbackItemSprite;
        public MergeItemView ItemTemplate => itemTemplate;

        /// <summary>Removes an item (used when a goal item flies to the garden).</summary>
        public void RemoveItem(int index)
        {
            if (index < 0 || index >= tiles.Length) return;
            tiles[index] = 0;
            if (items.TryGetValue(index, out var v)) { ReleaseView(v); items.Remove(index); }
        }

        /// <summary>First pair of adjacent equal-tier cells, for the tutorial hand. Returns false if none.</summary>
        public bool TryFindMergeablePair(out int a, out int b)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == 0 || tiles[i] >= 4) continue;
                for (int j = i + 1; j < tiles.Length; j++)
                    if (tiles[j] == tiles[i] && IsAdjacent(i, j)) { a = i; b = j; return true; }
            }
            // No adjacent pair: suggest moving an item next to its match.
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == 0) continue;
                for (int j = i + 1; j < tiles.Length; j++)
                {
                    if (tiles[j] != tiles[i]) continue;
                    int empty = FindEmptyNeighbour(j);
                    if (empty >= 0) { a = i; b = empty; return true; }
                }
            }
            a = b = -1;
            return false;
        }

        private int FindEmptyNeighbour(int index)
        {
            // orthogonal neighbours first, then diagonal ones when allowed
            int r = index / cols, c = index % cols;
            int[] dr = { 0, 0, 1, -1, 1, 1, -1, -1 }, dc = { 1, -1, 0, 0, 1, -1, 1, -1 };
            int count = mergeRule == MergeRule.OrthogonalNeighbours ? 4 : 8;
            for (int k = 0; k < count; k++)
            {
                int nr = r + dr[k], nc = c + dc[k];
                if (nr < 0 || nc < 0 || nr >= rows || nc >= cols) continue;
                int n = nr * cols + nc;
                if (tiles[n] == 0) return n;
            }
            return -1;
        }

        // ------------------------------------------------------------------ setup helpers
        private void ClearBoard()
        {
            foreach (var kv in items) if (kv.Value != null) ReleaseView(kv.Value);
            items.Clear();
            if (dragItem != null) { ReleaseView(dragItem); dragItem = null; }
            foreach (var c in cells) if (c != null) { c.transform.DOKill(); Destroy(c.gameObject); }
            cells.Clear();
        }

        // ------------------------------------------------------------------ item view pool
        private MergeItemView GetView(Transform parent)
        {
            MergeItemView view = null;
            while (itemPool.Count > 0 && view == null) view = itemPool.Pop();
            if (view == null) view = Instantiate(itemTemplate, parent);
            else view.transform.SetParent(parent, false);
            view.gameObject.SetActive(true);
            return view;
        }

        private void ReleaseView(MergeItemView view)
        {
            if (view == null) return;
            view.transform.DOKill();
            view.SetSelected(false);
            view.SetRaycast(true);
            view.gameObject.SetActive(false);
            view.transform.SetParent(dragLayer != null ? dragLayer : boardRect, false);
            itemPool.Push(view);
        }

        private void BuildCells()
        {
            for (int i = 0; i < cols * rows; i++)
            {
                var cell = Instantiate(cellTemplate, boardRect);
                cell.gameObject.SetActive(true);
                cell.name = "Cell_" + i;
                cell.Bind(this, i);
                var bg = cell.GetComponent<Image>();
                if (bg != null) bg.color = ((i / cols) + (i % cols)) % 2 == 0 ? cellColorA : cellColorB;
                cells.Add(cell);
            }
        }

        private void Spawn(int count)
        {
            var empty = new List<int>();
            for (int i = 0; i < tiles.Length; i++) empty.Add(i);
            for (int n = 0; n < count && empty.Count > 0; n++)
            {
                int pick = UnityEngine.Random.Range(0, empty.Count);
                tiles[empty[pick]] = 1;
                empty.RemoveAt(pick);
            }
        }

        private Vector2 lastLayoutArea;

        /// <summary>The CanvasScaler can settle after Setup (first frame, rotation, resize): re-fit when the area changes.</summary>
        private void LateUpdate()
        {
            if (boardRect == null || grid == null || cols <= 0) return;
            var parent = boardRect.parent as RectTransform;
            if (parent == null || (parent.rect.size - lastLayoutArea).sqrMagnitude < 1f) return;
            Layout();
            RefitItems();
        }

        /// <summary>Fits the grid into the parent rect, keeping square cells.</summary>
        public void Layout()
        {
            if (boardRect == null || grid == null) return;
            var parent = boardRect.parent as RectTransform;
            Vector2 avail = parent != null ? parent.rect.size : boardRect.rect.size;
            lastLayoutArea = avail;
            float cellW = (avail.x - 2 * padding - (cols - 1) * spacing) / cols;
            float cellH = (avail.y - 2 * padding - (rows - 1) * spacing) / rows;
            float cell = Mathf.Floor(Mathf.Max(40f, Mathf.Min(cellW, cellH)));
            grid.cellSize = new Vector2(cell, cell);
            grid.spacing = new Vector2(spacing, spacing);
            grid.padding = new RectOffset((int)padding, (int)padding, (int)padding, (int)padding);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            grid.childAlignment = TextAnchor.MiddleCenter;
            boardRect.sizeDelta = new Vector2(cols * cell + (cols - 1) * spacing + 2 * padding, rows * cell + (rows - 1) * spacing + 2 * padding);
            LayoutRebuilder.ForceRebuildLayoutImmediate(boardRect);
        }

        private void RenderAll()
        {
            if (cells.Count != tiles.Length) RecoverCells();
            releaseBuffer.Clear();
            foreach (var kv in items)
                if (kv.Value == null || tiles[kv.Key] == 0 || kv.Value.Tier != tiles[kv.Key]) releaseBuffer.Add(kv.Key);
            foreach (int i in releaseBuffer) { ReleaseView(items[i]); items.Remove(i); }
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i] == 0) continue;
                if (items.TryGetValue(i, out var v)) ResetViewInCell(v, i);
                else CreateItemView(i, tiles[i]);
            }
            RefreshSelection();
        }

        private void ResetViewInCell(MergeItemView view, int index)
        {
            var rt = view.Rect;
            if (rt.parent != cells[index].Rect) rt.SetParent(cells[index].Rect, false);
            rt.anchoredPosition = Vector2.zero;
            float size = grid.cellSize.x * itemFill;
            rt.sizeDelta = new Vector2(size, size);
        }

        /// <summary>Rebuilds the cell list from the hierarchy (editor hot-reload resets non-serialized lists).</summary>
        private void RecoverCells()
        {
            cells.Clear();
            foreach (var c in boardRect.GetComponentsInChildren<MergeCell>(true))
            {
                if (c == cellTemplate || !c.gameObject.activeSelf) continue;
                cells.Add(c);
            }
            cells.Sort((x, y) => x.transform.GetSiblingIndex().CompareTo(y.transform.GetSiblingIndex()));
            for (int i = 0; i < cells.Count; i++) cells[i].Bind(this, i);
        }

        private void RefitItems()
        {
            float size = grid.cellSize.x * itemFill;
            foreach (var kv in items)
                if (kv.Value != null && kv.Value.Rect.parent == cells[kv.Key].Rect) kv.Value.Rect.sizeDelta = new Vector2(size, size);
        }

        private MergeItemView CreateItemView(int index, int tier)
        {
            var cell = cells[index];
            var view = GetView(cell.Rect);
            view.name = "Item_" + index;
            var rt = view.Rect;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            float size = grid.cellSize.x * itemFill;
            rt.sizeDelta = new Vector2(size, size);
            rt.localScale = Vector3.one;
            view.Bind(this, index, tier, theme, fallbackItemSprite);
            items[index] = view;
            return view;
        }

        private void RefreshSelection()
        {
            for (int i = 0; i < cells.Count; i++) cells[i].SetHighlight(i == selected);
            foreach (var kv in items) kv.Value.SetSelected(kv.Key == selected);
        }

        // ------------------------------------------------------------------ input
        public void OnItemPointerDown(MergeItemView item, PointerEventData e)
        {
            if (Locked) return;
            pressScreenPos = e.position;
            OnPlayerInteracted?.Invoke();
        }

        public void OnItemBeginDrag(MergeItemView item, PointerEventData e)
        {
            if (Locked || dragLayer == null) return;
            dragItem = item;
            dragOriginalParent = item.transform.parent;
            dragOriginalSibling = item.transform.GetSiblingIndex();
            item.transform.SetParent(dragLayer, true);
            item.SetRaycast(false);
            item.transform.DOKill();
            item.transform.localScale = Vector3.one * 1.1f;
            MoveDragItem(e);
        }

        public void OnItemDrag(MergeItemView item, PointerEventData e)
        {
            if (dragItem != item) return;
            MoveDragItem(e);
        }

        private void MoveDragItem(PointerEventData e)
        {
            if (dragItem == null) return;
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragLayer, e.position, cam, out var local))
                dragItem.Rect.anchoredPosition = local;
        }

        public void OnItemRelease(MergeItemView item, PointerEventData e, bool wasDrag)
        {
            if (Locked && dragItem == null) return;
            int from = item.CellIndex;
            bool moved = wasDrag && Vector2.Distance(e.position, pressScreenPos) > 12f;
            Vector3? dropPos = null;

            if (dragItem == item)
            {
                dropPos = item.transform.position;
                // put the view back in its cell before resolving so RenderAll can clean it up
                item.transform.SetParent(dragOriginalParent, false);
                item.transform.SetSiblingIndex(dragOriginalSibling);
                item.Rect.anchoredPosition = Vector2.zero;
                item.transform.localScale = Vector3.one;
                item.SetRaycast(true);
                dragItem = null;
            }
            if (Locked) { RenderAll(); return; }

            int to = CellUnderPointer(e.position);
            if (moved && to >= 0 && to != from)
            {
                TryAction(from, to, dropPos);
                return;
            }
            if (moved)
            {
                RenderAll();
                var back = GetItem(from);
                if (back != null && dropPos.HasValue) SlideIn(back, dropPos.Value);
                return;
            }

            // tap
            if (selected == -1) { selected = from; RefreshSelection(); }
            else if (selected == from) { selected = -1; RefreshSelection(); }
            else TryAction(selected, from);
        }

        public void OnCellTapped(MergeCell cell)
        {
            if (Locked) return;
            if (tiles[cell.Index] != 0) return;      // item cells handle their own taps
            if (selected == -1) return;
            OnPlayerInteracted?.Invoke();
            TryAction(selected, cell.Index);
        }

        private int CellUnderPointer(Vector2 screenPos)
        {
            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            for (int i = 0; i < cells.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(cells[i].Rect, screenPos, cam)) return i;
            return -1;
        }

        /// <summary>Whether a and b may merge by position: always (Anywhere), or only as neighbours.</summary>
        private bool IsAdjacent(int a, int b)
        {
            if (a == b) return false;
            if (mergeRule == MergeRule.Anywhere) return true;
            int ra = a / cols, ca = a % cols, rb = b / cols, cb = b % cols;
            int dr = Mathf.Abs(ra - rb), dc = Mathf.Abs(ca - cb);
            if (mergeRule == MergeRule.NeighboursIncludingDiagonal) return dr <= 1 && dc <= 1;
            return dr + dc == 1;
        }

        /// <summary>Mirrors merge(a,b) from the prototype. fromPos: where the moving item is drawn (drag drop point).</summary>
        private void TryAction(int a, int b, Vector3? fromPos = null)
        {
            if (a < 0 || b < 0 || a == b || tiles[a] == 0) { selected = -1; RenderAll(); return; }
            Vector3 start = fromPos ?? cells[a].Rect.position;

            if (tiles[b] == 0)
            {
                tiles[b] = tiles[a];
                tiles[a] = 0;
                selected = -1;
                // keep the same view: it slides to its new cell
                if (items.TryGetValue(a, out var moving))
                {
                    items.Remove(a);
                    items[b] = moving;
                    moving.Bind(this, b, tiles[b], theme, fallbackItemSprite);
                    moving.name = "Item_" + b;
                }
                RenderAll();
                var view = GetItem(b);
                if (view != null) SlideIn(view, start);
                OnMoved?.Invoke();
                return;
            }
            if (!IsAdjacent(a, b) || tiles[a] != tiles[b] || tiles[a] >= 4)
            {
                selected = -1;
                RenderAll();
                PlayWrong(a, b);
                OnInvalidMove?.Invoke();
                return;
            }

            int t = tiles[a] + 1;
            tiles[a] = 0;
            tiles[b] = t;
            selected = -1;
            // the source view flies into the target, then the merged item pops
            if (items.TryGetValue(a, out var flyer)) items.Remove(a);
            RenderAll();
            var merged = GetItem(b);
            if (flyer != null && dragLayer != null)
            {
                flyer.transform.DOKill();
                flyer.SetSelected(false);
                flyer.SetRaycast(false);
                flyer.transform.SetParent(dragLayer, true);
                flyer.transform.position = start;
                flyer.transform.localScale = Vector3.one;
                if (merged != null) merged.transform.localScale = Vector3.zero;
                flyer.transform.DOMove(cells[b].Rect.position, mergeFlyDuration).SetEase(Ease.InQuad).OnComplete(() =>
                {
                    ReleaseView(flyer);
                    PopMerged(GetItem(b), b);
                });
            }
            else
            {
                if (flyer != null) ReleaseView(flyer);
                PopMerged(merged, b);
            }
            OnMerged?.Invoke(t, b);
        }

        private void SlideIn(MergeItemView view, Vector3 fromWorld)
        {
            var tr = view.transform;
            tr.DOKill();
            tr.localScale = Vector3.one;
            tr.position = fromWorld;
            tr.DOLocalMove(Vector3.zero, moveDuration).SetEase(Ease.OutQuad)
              .OnComplete(() => tr.DOPunchScale(new Vector3(0.12f, -0.08f, 0f), 0.22f, 5, 0.5f));
        }

        private void PopMerged(MergeItemView view, int cell)
        {
            if (cell < 0 || cell >= cells.Count) return;
            if (view != null)
            {
                view.transform.DOKill();
                view.transform.localScale = Vector3.one * 0.4f;
                view.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack, 2.2f);
            }
            var fx = UiFx.Instance;
            if (fx == null) return;
            var pos = cells[cell].Rect.position;
            fx.Ring(pos, grid.cellSize.x * 1.5f, mergeRingColor);
            fx.Play(FxType.Sparkle, pos);
        }

        private void PlayWrong(int a, int b)
        {
            Shake(a); Shake(b);
            if (UiFx.Instance != null && b >= 0 && b < cells.Count) UiFx.Instance.Play(FxType.Puff, cells[b].Rect.position);
        }

        private void Shake(int i)
        {
            var v = GetItem(i);
            if (v != null)
            {
                v.transform.DOKill();
                v.transform.localScale = Vector3.one;
                v.Rect.anchoredPosition = Vector2.zero;
                v.Rect.DOShakeAnchorPos(0.35f, new Vector2(16f, 0f), 18, 0f, false, true);
            }
            if (i >= 0 && i < cells.Count) cells[i].Flash(wrongFlashColor);
        }

        /// <summary>Level intro: items pop in one after another.</summary>
        public void PlayItemsIntro(float delay)
        {
            int n = 0;
            for (int i = 0; i < tiles.Length; i++)
            {
                if (!items.TryGetValue(i, out var v) || v == null) continue;
                var tr = v.transform;
                tr.DOKill();
                tr.localScale = Vector3.zero;
                tr.DOScale(1f, 0.32f).SetDelay(delay + n * 0.04f).SetEase(Ease.OutBack);
                n++;
            }
        }
    }
}
