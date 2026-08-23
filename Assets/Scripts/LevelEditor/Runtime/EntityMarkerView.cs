using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EntityMarkerView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ScreenCanvasView _Owner;
    private EntityInstance _Instance;
    private RectTransform _Rect;
    private Image _Icon;
    private Color _BaseColor;

    private bool _IsDragging;
    private Vector2 _DragStartLocalPoint;
    private Vector2 _DragStartPosition;

    public static EntityMarkerView Create(Transform parent, ScreenCanvasView owner, EntityInstance instance)
    {
        var go = new GameObject($"Entity_{instance.Id}", typeof(RectTransform), typeof(Image), typeof(EntityMarkerView));
        go.transform.SetParent(parent, false);

        var view = go.GetComponent<EntityMarkerView>();
        view._Owner = owner;
        view._Instance = instance;

        view._Rect = (RectTransform)go.transform;
        view._Rect.anchorMin = Vector2.zero;
        view._Rect.anchorMax = Vector2.zero;

        view._Icon = go.GetComponent<Image>();
        view._Icon.preserveAspect = true;

        EntityCatalog.Lookup.TryGetValue(instance.TypeId, out var def);
        Sprite sprite = def != null ? def.Icon : null;
        view._Icon.sprite = sprite;
        view._BaseColor = sprite != null ? Color.white : new Color(1f, 0.4f, 0.9f, 0.9f);

        // UI Image ignores Sprite.pivot - map it onto the RectTransform's
        // pivot so anchoredPosition lines up with the art's authored anchor.
        view._Rect.pivot = sprite != null
            ? new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height)
            : new Vector2(0.5f, 0.5f);
        view._Icon.color = view._BaseColor;

        return view;
    }

    private void Update()
    {
        float cellPixelSize = _Owner.CellPixelSize;
        _Rect.anchoredPosition = _Instance.LocalPosition * cellPixelSize;
        // Same size as a tile - renders 1:1 with the grid.
        _Rect.sizeDelta = new Vector2(cellPixelSize, cellPixelSize);

        ApplyAdapterVisualTransform();

        // Only raycastable in Entities mode, so it doesn't intercept clicks in the other modes.
        bool interactive = _Owner.Mode == ScreenCanvasView.InteractionMode.Entities;
        _Icon.raycastTarget = interactive;

        _Icon.color = _Owner.SelectedEntityId == _Instance.Id ? Color.yellow : _BaseColor;
    }

    private void ApplyAdapterVisualTransform()
    {
        if (!EntityPropertyResolver.TryResolveAdapter(_Instance, out var def, out var adapter))
        {
            _Rect.localRotation = Quaternion.identity;
            _Rect.localScale = Vector3.one;
            return;
        }

        var properties = EntityPropertyResolver.GetEffectiveProperties(_Instance, def.Prefab, adapter);
        _Rect.localRotation = Quaternion.Euler(0f, 0f, adapter.GetEditorRotationDegrees(properties));

        Vector2 scale = adapter.GetEditorScale(properties);
        _Rect.localScale = new Vector3(scale.x, scale.y, 1f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Right click does nothing here - see ScreenCanvasView.BeginPan.
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        _Owner.SelectEntity(_Instance.Id);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Right click always pans - forward to the canvas (uGUI binds drag to us on OnBeginDrag).
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _Owner.BeginPan(eventData);
            return;
        }

        _Owner.SelectEntity(_Instance.Id);
        _IsDragging = true;
        _Owner.IsDraggingEntity = true;
        _DragStartPosition = _Instance.LocalPosition;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (RectTransform)transform.parent, eventData.position, null, out _DragStartLocalPoint);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _Owner.ContinuePan(eventData);
            return;
        }

        if (!_IsDragging)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform.parent, eventData.position, null, out Vector2 localPoint))
            return;

        Vector2 deltaCells = (localPoint - _DragStartLocalPoint) / _Owner.CellPixelSize;
        Vector2 newPosition = _DragStartPosition + deltaCells;

        bool snap = ScreenView.IsGridSnapActive(_Owner);
        if (snap)
            newPosition = ScreenView.SnapToCellOrigin(newPosition);

        _Instance.LocalPosition = newPosition;
        _Instance.SnappedToGrid = snap;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _Owner.EndPan();
            return;
        }

        _IsDragging = false;
        _Owner.IsDraggingEntity = false;
    }
}
