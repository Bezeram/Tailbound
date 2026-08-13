using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Sits on a ScreenView's corner-handle child GameObject and forwards drag
/// events to the owning ScreenView as a resize rather than a move. A
/// separate component (rather than handling both on ScreenView itself) so
/// uGUI's own raycasting - which hits the frontmost graphic under the
/// pointer - naturally gives the handle priority over the screen body
/// without any manual hit-order logic.
/// </summary>
public class ScreenResizeHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ScreenView Owner;

    public void OnBeginDrag(PointerEventData eventData) => Owner.BeginResize(eventData);
    public void OnDrag(PointerEventData eventData) => Owner.ResizeDrag(eventData);
    public void OnEndDrag(PointerEventData eventData) => Owner.EndResize();
}
