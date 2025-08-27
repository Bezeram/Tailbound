using UnityEngine;

[ExecuteAlways]
public class SpawnPoint : MonoBehaviour
{
    private SpriteRenderer _SpriteRenderer;
    
    void OnValidate()
    {
        if (transform.parent == null || !transform.parent.TryGetComponent(out _SpriteRenderer))
            Debug.LogWarning("SpawnPoint object has no ScreenArea parent!", context: this);
    }

    void Update()
    {
        if (_SpriteRenderer != null)
            _SpriteRenderer.enabled = !Application.isPlaying;
    }
}
