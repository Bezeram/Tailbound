using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The draggable/resizable rectangle representing one ScreenDef on the
/// ScreenCanvasView. Built entirely from code (no hand-authored prefab)
/// since the number of screens is dynamic.
/// </summary>
public class ScreenView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int ScreenId { get; private set; }

    private ScreenCanvasView _Owner;
    private RectTransform _Rect;
    private Image _Fill;
    private TMP_Text _Label;

    private RectInt _DragStartBounds;
    private RectInt _DragPreviewBounds;
    private bool _DragValid = true;
    private bool _IsDraggingBody;
    private bool _IsDraggingHandle;
    private Vector2 _DragStartLocalPoint;

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

        return view;
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
                : new Color(0.25f, 0.6f, 1f, 0.35f);

        _Label.text = $"Screen {ScreenId} ({bounds.width}x{bounds.height})";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _Owner.Select(ScreenId);
    }

    public void OnBeginDrag(PointerEventData eventData) => BeginDrag(eventData, isHandle: false);
    public void OnDrag(PointerEventData eventData) => Drag(eventData, isHandle: false);
    public void OnEndDrag(PointerEventData eventData) => EndDrag();

    // Called by this screen's ScreenResizeHandle child.
    public void BeginResize(PointerEventData eventData) => BeginDrag(eventData, isHandle: true);
    public void ResizeDrag(PointerEventData eventData) => Drag(eventData, isHandle: true);
    public void EndResize() => EndDrag();

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
