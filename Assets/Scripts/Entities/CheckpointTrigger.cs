using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    private BoxCollider2D _Collider;
    private ScreenArea _ScreenArea;
    
    void OnValidate()
    {
        if (_Collider == null)
            _Collider = GetComponent<BoxCollider2D>();
        if (_ScreenArea == null)
        {
            if (transform.parent == null || !transform.parent.TryGetComponent(out _ScreenArea))
                Debug.LogWarning("Checkpoint trigger requires a ScreenArea parent!");
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (_ScreenArea == null)
        {
            Debug.LogWarning("Entered a Checkpoint-trigger without a ScreenArea parent!");
            return;
        }
        
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            // TODO: Find a respawn object contained in the current screen
            //  and set the current respawn point in the ScreenArea reference.
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider2D>().size);
    }
}
