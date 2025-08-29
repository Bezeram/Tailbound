using UnityEngine;

[ExecuteAlways]
public class SpawnPoint : MonoBehaviour
{
    public int ID = -1;
    
    void OnValidate()
    {
        if (transform.parent == null)
            return;
        
        if (!transform.parent.TryGetComponent(out ScreenBox screen))
            Debug.LogWarning("SpawnPoint object has no ScreenArea parent!", context: this);
    }
}
