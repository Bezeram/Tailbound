using Sirenix.OdinInspector;
using UnityEngine;

public class ScreenBox : MonoBehaviour
{
    [TitleGroup("Input")] public Vector2 Size;
    [TitleGroup("Input")] public SpawnPoint FirstSpawnPoint;
    [TitleGroup("Input")] public LayerMask PlayerLayer;
    [TitleGroup("Info"), ReadOnly] public SpawnPoint CurrentSpawnPoint;
    [TitleGroup("Info"), ReadOnly] public bool IsTransitioning;
    [TitleGroup("Info"), ReadOnly, SerializeField] private int _CurrentSpawnPointID;
    [TitleGroup("Info"), ReadOnly, SerializeField] private SpawnPoint[] _SpawnPoints;

    public int ID = -1;
    
    public Vector3 Center => transform.position + new Vector3(Size.x * 0.5f, Size.y * 0.5f, 0f);
    public Vector3 CurrentSpawnPosition => CurrentSpawnPoint.transform.position;
    public Vector3 BottomLeft => transform.position;

    private BoxCollider2D _TransitionCollider;
    private LevelManager _LevelManager;
    
    public void ToggleScreenContent(bool active)
    {
        transform.Find("Content").gameObject.SetActive(active);
        transform.Find("DeathBox").gameObject.SetActive(active);
    }

    void OnEnable()
    {
        CurrentSpawnPoint = FirstSpawnPoint;
    }

    /// <summary>
    /// Caches _LevelManager, sizes the transition collider from Size, and
    /// picks a FirstSpawnPoint if none is set - previously only done in
    /// OnValidate, which never runs in a build. Called from Awake() (so
    /// hand-placed screens work in a real build too) and from LevelInstantiator
    /// once a runtime-built screen's Size/content are finalized.
    /// </summary>
    public void RuntimeInit()
    {
        _LevelManager = FindAnyObjectByType<LevelManager>();

        _TransitionCollider = GetComponent<BoxCollider2D>();
        _TransitionCollider.offset = Size / 2;
        _TransitionCollider.size = Size;

        _SpawnPoints = GetComponentsInChildren<SpawnPoint>();
        if (_SpawnPoints.Length != 0)
            FirstSpawnPoint = _SpawnPoints[0];
    }

    void Awake()
    {
        RuntimeInit();
    }

    void OnValidate()
    {
        RuntimeInit();
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
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(Center, Size);
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
