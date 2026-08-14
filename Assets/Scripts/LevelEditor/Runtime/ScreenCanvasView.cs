using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The pan/zoom canvas area: renders the grid and one ScreenView per
/// ScreenDef, and owns pan/zoom state. Screen views are positioned in
/// "content-local" pixel space (cell * CellPixelSize, no pan baked in) by
/// being parented under a Content rect whose own anchoredPosition carries
/// the pan offset - so panning is just moving one transform, not
/// recomputing every screen's position.
/// </summary>
public class ScreenCanvasView : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public enum InteractionMode { Screens, Paint, Entities }
    public enum TileLayerKind { Background, Foreground }

    public const float HandleSize = 14f;
    private const float BaseCellPixelSize = 24f;

    public LevelAsset Level { get; private set; }
    public float CellPixelSize => BaseCellPixelSize * _Zoom;

    public InteractionMode Mode { get; private set; } = InteractionMode.Screens;
    public TileLayerKind ActiveLayer { get; private set; } = TileLayerKind.Foreground;
    // Empty string means the eraser is selected.
    public string ActiveTileId { get; private set; } = "";
    public string ActiveEntityTypeId { get; private set; } = "";
    public int SelectedEntityId { get; private set; } = -1;
    public bool SnapToGridEnabled { get; private set; } = true;

    private RectTransform _Rect;
    private RectTransform _Content;
    private readonly Dictionary<int, ScreenView> _ScreenViews = new();
    private readonly List<RectTransform> _VLines = new();
    private readonly List<RectTransform> _HLines = new();

    private Vector2 _Pan = Vector2.zero;
    private float _Zoom = 1f;
    private int _SelectedScreenId = -1;
    private bool _IsPanning;
    private Vector2 _PanLastLocalPoint;

    public static ScreenCanvasView Create(Transform parent)
    {
        var go = new GameObject("ScreenCanvas", typeof(RectTransform), typeof(Image), typeof(ScreenCanvasView));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        go.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f);

        var view = go.GetComponent<ScreenCanvasView>();
        view._Rect = rect;

        var contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(go.transform, false);
        view._Content = (RectTransform)contentGO.transform;
        view._Content.anchorMin = Vector2.zero;
        view._Content.anchorMax = Vector2.zero;
        view._Content.pivot = Vector2.zero;
        view._Content.anchoredPosition = Vector2.zero;
        view._Content.sizeDelta = Vector2.zero;

        return view;
    }

    public void SetLevel(LevelAsset level)
    {
        Level = level;
        _SelectedScreenId = -1;
        SelectedEntityId = -1;
        _Pan = Vector2.zero;
        _Zoom = 1f;
        RebuildScreenViews();
    }

    public void AddScreen()
    {
        if (Level == null)
            return;

        var size = new Vector2Int(16, 9);
        int x = 0;
        foreach (var existing in Level.Screens)
            x = Mathf.Max(x, existing.Origin.x + existing.Size.x);

        var screen = new ScreenDef { Id = Level.NewScreenId(), Origin = new Vector2Int(x, 0), Size = size };
        Level.Screens.Add(screen);
        _SelectedScreenId = screen.Id;
        RebuildScreenViews();
    }

    public void DeleteSelectedScreen()
    {
        if (Level == null || _SelectedScreenId < 0)
            return;

        Level.RemoveScreen(_SelectedScreenId);
        _SelectedScreenId = -1;
        RebuildScreenViews();
    }

    public void Select(int screenId)
    {
        _SelectedScreenId = screenId;
    }

    public void SetMode(InteractionMode mode)
    {
        Mode = mode;
    }

    public void SetActiveLayer(TileLayerKind layer)
    {
        ActiveLayer = layer;
    }

    public void SetActiveTile(string tileId)
    {
        ActiveTileId = tileId ?? "";
    }

    public TileLayer GetActiveTileLayer()
    {
        return ActiveLayer == TileLayerKind.Background ? Level.Background : Level.Foreground;
    }

    public void SetActiveEntityType(string typeId)
    {
        ActiveEntityTypeId = typeId ?? "";
    }

    public void SetSnapToGridEnabled(bool enabled)
    {
        SnapToGridEnabled = enabled;
    }

    public void SelectEntity(int entityId)
    {
        SelectedEntityId = entityId;
    }

    public void DeleteSelectedEntity()
    {
        if (Level == null || SelectedEntityId < 0)
            return;

        Level.Entities.RemoveAll(e => e.Id == SelectedEntityId);
        SelectedEntityId = -1;
        RebuildScreenViews();
    }

    /// <summary>
    /// Converts a pointer position into Content-local pixel space. Content
    /// doesn't move during a screen drag (only panning moves it, and that's
    /// a separate operation), so this is stable to measure drag deltas against.
    /// </summary>
    public bool ScreenToContentLocalPoint(PointerEventData eventData, out Vector2 localPoint)
    {
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(_Content, eventData.position, null, out localPoint);
    }

    private void RebuildScreenViews()
    {
        foreach (var view in _ScreenViews.Values)
            if (view != null)
                Destroy(view.gameObject);
        _ScreenViews.Clear();

        if (Level == null)
            return;

        foreach (var screen in Level.Screens)
        {
            var view = ScreenView.Create(_Content, this, screen.Id);
            view.RefreshTiles();
            view.RefreshEntities();
            _ScreenViews[screen.Id] = view;
        }
    }

    private void Update()
    {
        _Content.anchoredPosition = _Pan;

        if (Level != null)
        {
            foreach (var screen in Level.Screens)
            {
                if (_ScreenViews.TryGetValue(screen.Id, out var view))
                {
                    view.UpdateVisual(screen, screen.Id == _SelectedScreenId, CellPixelSize);
                    view.RepositionTiles(CellPixelSize);
                }
            }
        }

        UpdateGrid();
    }

    public void OnScroll(PointerEventData eventData)
    {
        _Zoom = Mathf.Clamp(_Zoom + eventData.scrollDelta.y * 0.05f, 0.25f, 3f);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        _IsPanning = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_Rect, eventData.position, null, out _PanLastLocalPoint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_IsPanning)
            return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_Rect, eventData.position, null, out Vector2 localPoint))
        {
            _Pan += localPoint - _PanLastLocalPoint;
            _PanLastLocalPoint = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _IsPanning = false;
    }

    // ------------------------------------------------------------------
    // Grid (thin pooled Image bars, parented directly under this rect -
    // not Content - since pan is applied to their position manually).
    // ------------------------------------------------------------------

    private void UpdateGrid()
    {
        float step = CellPixelSize;
        if (step < 4f)
        {
            SetActiveCount(_VLines, 0);
            SetActiveCount(_HLines, 0);
            return;
        }

        Rect area = _Rect.rect;
        int vCount = Mathf.CeilToInt(area.width / step) + 2;
        int hCount = Mathf.CeilToInt(area.height / step) + 2;

        EnsureLinePool(_VLines, vCount, vertical: true);
        EnsureLinePool(_HLines, hCount, vertical: false);

        float startX = Mod(_Pan.x, step) - step;
        for (int i = 0; i < _VLines.Count; i++)
        {
            bool active = i < vCount;
            _VLines[i].gameObject.SetActive(active);
            if (active)
                _VLines[i].anchoredPosition = new Vector2(startX + i * step, 0);
        }

        float startY = Mod(_Pan.y, step) - step;
        for (int i = 0; i < _HLines.Count; i++)
        {
            bool active = i < hCount;
            _HLines[i].gameObject.SetActive(active);
            if (active)
                _HLines[i].anchoredPosition = new Vector2(0, startY + i * step);
        }
    }

    private void EnsureLinePool(List<RectTransform> pool, int count, bool vertical)
    {
        while (pool.Count < count)
        {
            var go = new GameObject(vertical ? "VLine" : "HLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = vertical ? new Vector2(0, 1) : new Vector2(1, 0);
            rect.pivot = Vector2.zero;
            rect.sizeDelta = vertical ? new Vector2(1, 0) : new Vector2(0, 1);

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.06f);
            image.raycastTarget = false;

            pool.Add(rect);
        }
    }

    private static void SetActiveCount(List<RectTransform> pool, int activeCount)
    {
        for (int i = 0; i < pool.Count; i++)
            pool[i].gameObject.SetActive(i < activeCount);
    }

    private static float Mod(float value, float modulus)
    {
        float result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
