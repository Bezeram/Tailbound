using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class ScreenArea : MonoBehaviour
{
    [TitleGroup("Input")] public Vector2 Size;
    [TitleGroup("Input"), SerializeField] private SpawnPoint _CurrentSpawnPoint;
    [TitleGroup("Info"), ReadOnly, SerializeField] private int _CurrentSpawnPointID;
    [TitleGroup("Info"), ReadOnly, SerializeField] private SpawnPoint[] _SpawnPoints;

    private float _UpdateTimer;
    
    public Vector3 Center => transform.position + new Vector3(Size.x * 0.5f, Size.y * 0.5f, 0f);
    public Vector3 CurrentSpawnPosition => _CurrentSpawnPoint.transform.position;
    public int SpawnPointID => _CurrentSpawnPointID;
    
    public void SetSpawnPoint(int id)
    {
        var spawnPoints = GetComponentsInChildren<SpawnPoint>();
        _CurrentSpawnPoint = spawnPoints[id];
    }
    
    // Sets the Screen's spawn point ID based on the given SpawnPoint's position.
    public void SetSpawnPoint(SpawnPoint spawnPoint)
    {
        // Find where spawnPoint is in the list based on its position.
        _CurrentSpawnPoint = spawnPoint;

        for (int i = 0; i < _SpawnPoints.Length; i++)
        {
            var childSpawn = _SpawnPoints[i];
            if (childSpawn.transform.position == spawnPoint.transform.position)
            {
                _CurrentSpawnPointID = i;
                return;
            }
        }
        
        Debug.LogWarning("SetSpawnPoint was called with an invalid spawn point!");
    }
        
    public Vector3[] GetBananas()
    {
        // All bananas in a screen must be children to a screen.
        var bananas = GetComponentsInChildren<CollectableBanana>();
        var positions = new Vector3[bananas.Length];
        
        for (int i = 0; i < positions.Length; i++)
            positions[i] = bananas[i].transform.position;

        return positions;
    }

    void OnValidate()
    {
        _SpawnPoints = GetComponentsInChildren<SpawnPoint>();

        if (_SpawnPoints.Length != 0)
        {
            // In the editor it is preferred to set the spawn point object directly.
            SetSpawnPoint(_CurrentSpawnPoint);
        }
    }

    void AutomaticallySetSpawnPoint()
    {
        // Exit if the spawn point has been set explicitly or with
        // the first spawn point added to the screen.
        if (_CurrentSpawnPoint != null) 
            return;
        
        // Spawn point has not been explicitly set, 
        var spawnPoint = transform.GetComponentsInChildren<SpawnPoint>();
        if (spawnPoint.Length == 1)
        {
            // Found the first spawn point, automatically set it
            _CurrentSpawnPoint = spawnPoint[0];
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
            _SpawnPoints = GetComponentsInChildren<SpawnPoint>();
            if (_CurrentSpawnPoint == null)
                Debug.LogError("Screen has no spawn point!", context: this);
        }
    }

    void UpdateInEditMode()
    {
        _UpdateTimer += Time.deltaTime;
        if (_UpdateTimer >= 0.5f)
        {
            AutomaticallySetSpawnPoint();
            _UpdateTimer -= 0.5f;
        }
    }

    void Update()
    {
        if (Application.isPlaying) 
            return;
        
        UpdateInEditMode();
    }
    
    void OnDrawGizmos()
    {
        Vector3 offset = new Vector3(1, 1, 0) * Size / 2;
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position + offset, Size);
    }
}
