using UnityEngine;
using UnityEngine.EventSystems;

public class ScreenResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ScreenView Owner;

    public void OnBeginDrag(PointerEventData eventData) => Owner.BeginResize(eventData);
    public void OnDrag(PointerEventData eventData) => Owner.ResizeDrag(eventData);
    public void OnEndDrag(PointerEventData eventData) => Owner.EndResize(eventData);
}
