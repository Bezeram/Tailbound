using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class ScreenBox : MonoBehaviour
{
    [TitleGroup("Input")] public Vector2 Size;
    [TitleGroup("Input")] public SpawnPoint FirstSpawnPoint;
    [TitleGroup("Info"), ReadOnly] public SpawnPoint CurrentSpawnPoint;
    [TitleGroup("Info"), ReadOnly, SerializeField] private int _CurrentSpawnPointID;
    [TitleGroup("Info"), ReadOnly, SerializeField] private SpawnPoint[] _SpawnPoints;

    private float _UpdateTimer;
    public int ID = -1;
    
    public Vector3 Center => transform.position + new Vector3(Size.x * 0.5f, Size.y * 0.5f, 0f);
    public Vector3 CurrentSpawnPosition => CurrentSpawnPoint.transform.position;
    
    private BoxCollider2D _TransitionCollider;

    void OnValidate()
    {
        // Setup collider
        _TransitionCollider = GetComponent<BoxCollider2D>();
        _TransitionCollider.offset = Size / 2;
        _TransitionCollider.size = Size;
        
        if (FirstSpawnPoint != null)
        {
            // Check if the spawn point selected is a child of the screen.
            if (FirstSpawnPoint.transform.parent != transform)
                Debug.LogError("First spawn point selected is not a child of the screen!", context: this);
            return;
        }
        
        // If no spawn point has been set, automatically choose one.
        _SpawnPoints = GetComponentsInChildren<SpawnPoint>();
        if (_SpawnPoints.Length != 0)
            FirstSpawnPoint = _SpawnPoints[0];
    }

    void OnTransformChildrenChanged()
    {
        // If no spawn point has been set, automatically choose one.
        _SpawnPoints = GetComponentsInChildren<SpawnPoint>();
        if (_SpawnPoints.Length != 0)
            FirstSpawnPoint = _SpawnPoints[0];
    }
    
    void OnDrawGizmos()
    {
        Vector3 offset = new Vector3(1, 1, 0) * Size / 2;
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position + offset, Size);
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
}
