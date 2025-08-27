using UnityEngine;

public class ScreenArea : MonoBehaviour
{
    public Vector2 size;

    public Vector3 CurrentCheckpoint;

    void OnValidate()
    {
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>().size);
    }
}
