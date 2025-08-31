using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class ScreenBox : MonoBehaviour
{
    [TitleGroup("Input")] public Vector2 Size;
    [TitleGroup("Input")] public SpawnPoint FirstSpawnPoint;
    [TitleGroup("Input")] public LayerMask PlayerLayer;
    [TitleGroup("Input")] public float ColliderMargin = 0.2f;
    [TitleGroup("Info"), ReadOnly] public SpawnPoint CurrentSpawnPoint;
    [TitleGroup("Info"), ReadOnly] public bool IsTransitioning;
    [TitleGroup("Info"), ReadOnly, SerializeField] private int _CurrentSpawnPointID;
    [TitleGroup("Info"), ReadOnly, SerializeField] private SpawnPoint[] _SpawnPoints;

    private float _UpdateTimer;
    public int ID = -1;
    
    public Vector3 Center => transform.position + new Vector3(Size.x * 0.5f, Size.y * 0.5f, 0f);
    public Vector3 CurrentSpawnPosition => CurrentSpawnPoint.transform.position;
    
    public BoxCollider2D _TransitionCollider;
    private LevelManager _LevelManager;
    
    public void ToggleScreenContent(bool active)
    {
        transform.GetChild(0).gameObject.SetActive(active);
    }

    void Start()
    {
        CurrentSpawnPoint = FirstSpawnPoint;
    }

    void OnValidate()
    {
        _LevelManager = FindAnyObjectByType<LevelManager>();
        
        // Setup collider
        _TransitionCollider = GetComponent<BoxCollider2D>();
        _TransitionCollider.offset = Size / 2;
        _TransitionCollider.size = Size - Vector2.one * ColliderMargin * 2;
        
        // If no spawn point has been set, automatically choose one.
        _SpawnPoints = GetComponentsInChildren<SpawnPoint>();
        if (_SpawnPoints.Length != 0)
            FirstSpawnPoint = _SpawnPoints[0];
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (IsTransitioning)
            return;
        
        if (Utils.IsInMask(collision.gameObject.layer, PlayerLayer))
        {
            // No transitions on the same screen.
            if (_LevelManager.CurrentScreen.ID == ID)
                return;
            
            _LevelManager.RunScreenTransition(ID);
        }
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
