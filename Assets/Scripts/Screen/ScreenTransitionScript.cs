using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ScreenTransitionScript : MonoBehaviour
{
    public ScreenArea NextScreen; // The screen to switch to when player enters
    public Vector2 ArrowDirection = Vector2.right; // purely visual/editor helper

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>().size);

        // Draw arrow for editor clarity
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)ArrowDirection.normalized;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.05f);
    }
}
