using UnityEngine;
using UnityEngine.Serialization;

public class ScreenArea : MonoBehaviour
{
    public Vector2 _Size;
    public Vector3 CurrentCheckpoint;

    void OnValidate()
    {
    }
    
    void OnDrawGizmos()
    {
        Vector3 offset = new Vector3(1, 1, 0) * _Size / 2;
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position + offset, _Size);
    }
}
