using UnityEngine;

[ExecuteAlways]
public class SpawnPoint : MonoBehaviour
{
    void OnValidate()
    {
        if (transform.parent == null)
            return;
        
        if (!transform.parent.TryGetComponent(out ScreenArea screen))
            Debug.LogWarning("SpawnPoint object has no ScreenArea parent!", context: this);
    }
}
