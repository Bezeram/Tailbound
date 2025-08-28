using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteInEditMode]
public class ScreenArea : MonoBehaviour
{
    [TitleGroup("Input")]
    public Vector2 Size;
    public SpawnPoint CurrentSpawnPoint;
        
    [TitleGroup("Input"), ReadOnly] 
    public Vector3 Center => transform.position + new Vector3(Size.x * 0.5f, Size.y * 0.5f, 0f);

    private float _UpdateTimer;

    void AutomaticallySetSpawnPoint()
    {
        // Exit if the spawn point has been set explicitly or with
        // the first spawn point added to the screen.
        if (CurrentSpawnPoint != null) 
            return;
        
        // Spawn point has not been explicitly set, 
        var spawnPoint = transform.GetComponentsInChildren<SpawnPoint>();
        if (spawnPoint.Length == 1)
        {
            // Found the first spawn point, automatically set it
            CurrentSpawnPoint = spawnPoint[0];
            return;
        }

        if (spawnPoint.Length > 1)
        {
            // Ambiguous, cannot pick favourite out of multiple spawn points.
            Debug.LogError("Screen has more than one spawn point", context: this);
        }
    }

    void Start()
    {
        if (Application.isPlaying)
        {
            if (CurrentSpawnPoint == null)
                Debug.LogError("Screen has no spawn point!", context: this);
        }
    }

    void Update()
    {
        if (Application.isPlaying)
            return;

        _UpdateTimer += Time.deltaTime;
        if (_UpdateTimer >= 0.5f)
        {
            AutomaticallySetSpawnPoint();

            _UpdateTimer -= 0.5f;
        }
    }
    
    void OnDrawGizmos()
    {
        Vector3 offset = new Vector3(1, 1, 0) * Size / 2;
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position + offset, Size);
    }
}
