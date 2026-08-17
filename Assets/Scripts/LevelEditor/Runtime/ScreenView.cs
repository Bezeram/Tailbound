using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The rectangle representing one ScreenDef on the ScreenCanvasView. Built
/// entirely from code (no hand-authored prefab) since the number of screens
/// is dynamic. Doubles as the interaction surface for both of the canvas's
/// modes: move/resize in Screens mode, paint/erase in Paint mode - the two
/// are mutually exclusive since both happen via click/drag on this same rect.
/// </summary>
public class ScreenView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int ScreenId { get; private set; }

    private ScreenCanvasView _Owner;
    private RectTransform _Rect;
    private Image _Fill;
    private TMP_Text _Label;
    private GameObject _HandleObject;

    private TileCellPool _BackgroundTiles;
    private TileCellPool _ForegroundTiles;

    private Transform _EntitiesRoot;
    private readonly List<EntityMarkerView> _EntityMarkers = new();

    private RectInt _DragStartBounds;
    private RectInt _DragPreviewBounds;
    private bool _DragValid = true;
    private bool _IsDraggingBody;
    private bool _IsDraggingHandle;
    private Vector2 _DragStartLocalPoint;

    private Vector2Int? _LastPaintedCell;

    public static ScreenView Create(Transform parent, ScreenCanvasView owner, int screenId)
    {
        var go = new GameObject($"Screen_{screenId}", typeof(RectTransform), typeof(Image), typeof(ScreenView));
        go.transform.SetParent(parent, false);

        var view = go.GetComponent<ScreenView>();
        view._Owner = owner;
        view.ScreenId = screenId;

        view._Rect = (RectTransform)go.transform;
        view._Rect.anchorMin = Vector2.zero;
        view._Rect.anchorMax = Vector2.zero;
        view._Rect.pivot = Vector2.zero;

        view._Fill = go.GetComponent<Image>();

        // Background tiles, then Foreground tiles, then label/handle - sibling
        // order is draw order, so each of these renders on top of the last.
        // Anchored to the bottom-left corner (a point anchor, like _Rect
        // itself) rather than the RectTransform default of center - a point
        // anchor doesn't move when the parent's sizeDelta changes, so tile
        // positions (which are anchoredPosition relative to these roots)
        // stay put on resize instead of dragging along with it.
        var backgroundRoot = new GameObject("BackgroundTiles", typeof(RectTransform));
        backgroundRoot.transform.SetParent(go.transform, false);
        SetBottomLeftAnchor((RectTransform)backgroundRoot.transform);
        view._BackgroundTiles = new TileCellPool(backgroundRoot.transform);

        var foregroundRoot = new GameObject("ForegroundTiles", typeof(RectTransform));
        foregroundRoot.transform.SetParent(go.transform, false);
        SetBottomLeftAnchor((RectTransform)foregroundRoot.transform);
        view._ForegroundTiles = new TileCellPool(foregroundRoot.transform);

        // Entities render above tiles, same bottom-left anchor so they move
        // with the screen correctly (the same anchor bug fixed for tiles).
        var entitiesRootGO = new GameObject("Entities", typeof(RectTransform));
        entitiesRootGO.transform.SetParent(go.transform, false);
        SetBottomLeftAnchor((RectTransform)entitiesRootGO.transform);
        view._EntitiesRoot = entitiesRootGO.transform;

        var labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        var labelRect = (RectTransform)labelGO.transform;
        labelRect.anchorMin = new Vector2(0, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        labelRect.pivot = new Vector2(0, 1);
        labelRect.anchoredPosition = new Vector2(4, -2);
        labelRect.sizeDelta = new Vector2(-8, 18);

        view._Label = labelGO.GetComponent<TextMeshProUGUI>();
        view._Label.fontSize = 14;
        view._Label.color = Color.white;
        view._Label.raycastTarget = false;

        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image), typeof(ScreenResizeHandle));
        handleGO.transform.SetParent(go.transform, false);
        var handleRect = (RectTransform)handleGO.transform;
        handleRect.anchorMin = Vector2.one;
        handleRect.anchorMax = Vector2.one;
        handleRect.pivot = Vector2.one;
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(ScreenCanvasView.HandleSize, ScreenCanvasView.HandleSize);
        handleGO.GetComponent<Image>().color = Color.white;
        handleGO.GetComponent<ScreenResizeHandle>().Owner = view;
        view._HandleObject = handleGO;

        return view;
    }

    /// <summary>
    /// Snaps a cell-space position to the bottom-left origin of whichever
    /// cell it falls within - the same convention TileCellPool positions
    /// tiles with (integer cell coordinate = that cell's bottom-left
    /// corner), so a snapped entity with a bottom-left-pivoted sprite lands
    /// exactly on the tile beneath it instead of a half-cell off to the
    /// side. Deliberately Floor, not Round: rounding picks whichever grid
    /// line is numerically closest, which lands on a corner shared by 4
    /// cells and flips inconsistently depending on which half of the cell
    /// was clicked. Floor always resolves to the cell actually clicked in.
    /// Shared with EntityMarkerView's drag-move snapping.
    /// </summary>
    public static Vector2 SnapToCellOrigin(Vector2 cellPos)
    {
        return new Vector2(Mathf.Floor(cellPos.x), Mathf.Floor(cellPos.y));
    }

    /// <summary>
    /// Grid snapping is a global editor mode, not a per-entity-type default -
    /// holding Ctrl inverts whatever the toolbar toggle is currently set to,
    /// for both placement and dragging an existing entity.
    /// </summary>
    public static bool IsGridSnapActive(ScreenCanvasView owner)
    {
        bool ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        return owner.SnapToGridEnabled ^ ctrlHeld;
    }

    private static void SetBottomLeftAnchor(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    public void UpdateVisual(ScreenDef screen, bool isSelected, float cellPixelSize)
    {
        RectInt bounds = (_IsDraggingBody || _IsDraggingHandle) ? _DragPreviewBounds : screen.Bounds;

        _Rect.anchoredPosition = (Vector2)bounds.position * cellPixelSize;
        _Rect.sizeDelta = (Vector2)bounds.size * cellPixelSize;

        bool invalid = (_IsDraggingBody || _IsDraggingHandle) && !_DragValid;
        _Fill.color = invalid
            ? new Color(1f, 0.2f, 0.2f, 0.45f)
            : isSelected
                ? new Color(1f, 0.85f, 0.2f, 0.45f)
                : new Color(0.25f, 0.6f, 1f, 0.2f);

        _Label.text = $"Screen {ScreenId} ({bounds.width}x{bounds.height})";

        // Resizing only makes sense in Screens mode - hide the handle in
        // Paint mode so it can't be triggered while trying to paint near a
        // screen's top-right corner.
        _HandleObject.SetActive(_Owner.Mode == ScreenCanvasView.InteractionMode.Screens);
    }

    /// <summary>Cheap per-frame follow-up to UpdateVisual - repositions already-built tile images for the current zoom.</summary>
    public void RepositionTiles(float cellPixelSize)
    {
        _BackgroundTiles.Reposition(cellPixelSize);
        _ForegroundTiles.Reposition(cellPixelSize);
    }

    /// <summary>Structural rebuild of which cells are shown - call after any paint/erase write, and once after creation.</summary>
    public void RefreshTiles()
    {
        var screen = _Owner.Level?.GetScreen(ScreenId);
        if (screen == null)
            return;

        var lookup = TileCatalog.Lookup;
        _Owner.Level.Background.ScreenCells.TryGetValue(ScreenId, out var backgroundCells);
        _Owner.Level.Foreground.ScreenCells.TryGetValue(ScreenId, out var foregroundCells);

        _BackgroundTiles.SetCells(backgroundCells, lookup);
        _ForegroundTiles.SetCells(foregroundCells, lookup);
    }

    /// <summary>Structural rebuild of which entity markers exist - call after any placement/deletion, and once after creation.</summary>
    public void RefreshEntities()
    {
        foreach (var marker in _EntityMarkers)
            if (marker != null)
                Destroy(marker.gameObject);
        _EntityMarkers.Clear();

        if (_Owner.Level == null)
            return;

        foreach (var instance in _Owner.Level.Entities)
        {
            if (instance.ScreenId != ScreenId)
                continue;

            _EntityMarkers.Add(EntityMarkerView.Create(_EntitiesRoot, _Owner, instance));
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _Owner.Select(ScreenId);

        if (_Owner.Mode == ScreenCanvasView.InteractionMode.Paint)
            PaintAtPointer(eventData);
        else if (_Owner.Mode == ScreenCanvasView.InteractionMode.Entities)
            PlaceEntityAtPointer(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_Owner.Mode == ScreenCanvasView.InteractionMode.Paint)
        {
            _LastPaintedCell = null;
            PaintAtPointer(eventData);
            return;
        }

        // Placement is click-only (one click = one entity); the screen body
        // itself has no drag behavior in Entities mode - only individual
        // markers (moving an existing entity) do.
        if (_Owner.Mode == ScreenCanvasView.InteractionMode.Entities)
            return;

        BeginDrag(eventData, isHandle: false);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_Owner.Mode == ScreenCanvasView.InteractionMode.Paint)
        {
            PaintAtPointer(eventData);
            return;
        }

        if (_Owner.Mode == ScreenCanvasView.InteractionMode.Entities)
            return;

        Drag(eventData, isHandle: false);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_Owner.Mode == ScreenCanvasView.InteractionMode.Paint)
        {
            _LastPaintedCell = null;
            return;
        }

        if (_Owner.Mode == ScreenCanvasView.InteractionMode.Entities)
            return;

        EndDrag();
    }

    // Called by this screen's ScreenResizeHandle child (inactive, so
    // unreachable, while in Paint mode - see UpdateVisual).
    public void BeginResize(PointerEventData eventData) => BeginDrag(eventData, isHandle: true);
    public void ResizeDrag(PointerEventData eventData) => Drag(eventData, isHandle: true);
    public void EndResize() => EndDrag();

    private void PaintAtPointer(PointerEventData eventData)
    {
        var screen = _Owner.Level?.GetScreen(ScreenId);
        if (screen == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_Rect, eventData.position, null, out Vector2 local))
            return;

        float cellPixelSize = _Owner.CellPixelSize;
        int cx = Mathf.FloorToInt(local.x / cellPixelSize);
        int cy = Mathf.FloorToInt(local.y / cellPixelSize);
        if (cx < 0 || cy < 0 || cx >= screen.Size.x || cy >= screen.Size.y)
            return;

        var cell = new Vector2Int(cx, cy);
        if (_LastPaintedCell == cell)
            return;
        _LastPaintedCell = cell;

        var layer = _Owner.GetActiveTileLayer();
        var cells = layer.GetOrCreateScreenCells(ScreenId);

        if (string.IsNullOrEmpty(_Owner.ActiveTileId))
            cells.Remove(cell);
        else
            cells[cell] = new TileRef(_Owner.ActiveTileId);

        RefreshTiles();
    }

    private void PlaceEntityAtPointer(PointerEventData eventData)
    {
        var screen = _Owner.Level?.GetScreen(ScreenId);
        if (screen == null)
            return;

        string typeId = _Owner.ActiveEntityTypeId;
        if (string.IsNullOrEmpty(typeId))
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_Rect, eventData.position, null, out Vector2 local))
            return;

        Vector2 localCellPos = local / _Owner.CellPixelSize;

        bool snap = IsGridSnapActive(_Owner);
        if (snap)
        {
            localCellPos = SnapToCellOrigin(localCellPos);

            // Snapping means positions are exact matches, not approximate -
            // don't stack a second entity on a cell that's already occupied.
            var existing = _Owner.Level.Entities.Find(
                e => e.ScreenId == ScreenId && e.LocalPosition == localCellPos);
            if (existing != null)
            {
                _Owner.SelectEntity(existing.Id);
                return;
            }
        }

        var instance = new EntityInstance
        {
            Id = _Owner.Level.NewEntityId(),
            ScreenId = ScreenId,
            TypeId = typeId,
            LocalPosition = localCellPos,
            SnappedToGrid = snap,
        };
        _Owner.Level.Entities.Add(instance);
        _Owner.SelectEntity(instance.Id);

        RefreshEntities();
    }

    private void BeginDrag(PointerEventData eventData, bool isHandle)
    {
        _Owner.Select(ScreenId);

        var screen = _Owner.Level.GetScreen(ScreenId);
        if (screen == null)
            return;

        _DragStartBounds = screen.Bounds;
        _DragPreviewBounds = _DragStartBounds;
        _DragValid = true;
        _IsDraggingBody = !isHandle;
        _IsDraggingHandle = isHandle;

        _Owner.ScreenToContentLocalPoint(eventData, out _DragStartLocalPoint);
    }

    private void Drag(PointerEventData eventData, bool isHandle)
    {
        if (!(_IsDraggingBody || _IsDraggingHandle))
            return;

        if (!_Owner.ScreenToContentLocalPoint(eventData, out Vector2 localPoint))
            return;

        Vector2 deltaPixels = localPoint - _DragStartLocalPoint;
        float cellPixelSize = _Owner.CellPixelSize;

        if (!isHandle)
        {
            int dx = Mathf.RoundToInt(deltaPixels.x / cellPixelSize);
            int dy = Mathf.RoundToInt(deltaPixels.y / cellPixelSize);
            _DragPreviewBounds = new RectInt(
                _DragStartBounds.x + dx, _DragStartBounds.y + dy,
                _DragStartBounds.width, _DragStartBounds.height);
        }
        else
        {
            int dw = Mathf.RoundToInt(deltaPixels.x / cellPixelSize);
            int dh = Mathf.RoundToInt(deltaPixels.y / cellPixelSize);
            int newWidth = Mathf.Max(1, _DragStartBounds.width + dw);
            int newHeight = Mathf.Max(1, _DragStartBounds.height + dh);
            _DragPreviewBounds = new RectInt(_DragStartBounds.x, _DragStartBounds.y, newWidth, newHeight);
        }

        _DragValid = !_Owner.Level.ScreenOverlaps(_DragPreviewBounds, ScreenId);
    }

    private void EndDrag()
    {
        if ((_IsDraggingBody || _IsDraggingHandle) && _DragValid)
        {
            var screen = _Owner.Level.GetScreen(ScreenId);
            if (screen != null)
            {
                screen.Origin = _DragPreviewBounds.position;
                screen.Size = _DragPreviewBounds.size;
            }
        }

        _IsDraggingBody = false;
        _IsDraggingHandle = false;
    }
}
