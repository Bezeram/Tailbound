using Sirenix.OdinInspector;
using TarodevController;
using Unity.Mathematics.Geometry;
using UnityEngine;
using UnityEngine.Serialization;

public class CollectableBanana : MonoBehaviour
{
    [TitleGroup("References")]
    public LayerMask PlayerLayer;
    public BananaChannel BananaChannel;
    
    [TitleGroup("Input")] 
    public float BobOffset = 0.15f;
    public float BobTime = 1f;
    [Tooltip("Delay before the banana follows the player and before the player stops following.")] 
    public int FollowUpdatesPerSecond = 20;
    public float FollowVerticalOffset = 1;
    public float MaxPlayerDistance = 2;
    
    [ReadOnly] public int ID = -1;

    private Vector3 _InitialPosition;
    private float _TimerBobbing = 0.5f;
    private int _UpdateCounter;
    private int _BobDirection = 1;
    
    private PlayerController _PlayerController;
    private bool _PickedUp;
    private bool _Collected;
    [FormerlySerializedAs("_PlayerPositions")] public Vector3 _PlayerPosition;
    private Vector3 _LastPlayerPosition;
    private int _PositionsIndex;
    public float _TimerUpdates;
    private float _DeltaTimeFollowUpdates;

    void Start()
    {
        if (BananaChannel == null)
            Debug.LogWarning("BananaChannel has not been set!", context: this);
        
        _InitialPosition = transform.position;
        _DeltaTimeFollowUpdates = 1f / FollowUpdatesPerSecond;
        
        int updatesDuringFollowDelay = Mathf.CeilToInt(FollowUpdatesPerSecond);
    }

    void HandleBobbing()
    {
        _TimerBobbing += _BobDirection * Time.deltaTime;
        if (_TimerBobbing >= BobTime || _TimerBobbing < 0)
        {
            _TimerBobbing = Mathf.Clamp(_TimerBobbing, 0, BobTime);
            _BobDirection *= -1;
        }
        
        // Bob animation
        float top = BobOffset;
        float bottom = -BobOffset;
        float t = _TimerBobbing / BobTime;
        transform.position = _InitialPosition + Vector3.up * Mathf.Lerp(bottom, top, t);
    }

    void FollowPlayer()
    {
        // The banana follows but stops when close enough to the player.
        // Note: A new banana should keep its distance to the last banana collected, otherwise the bananas
        // are going to stack on top of each other and not form a beautiful banana trail.
        _TimerUpdates += Time.deltaTime;
        while (_TimerUpdates >= _DeltaTimeFollowUpdates)
        {
            _TimerUpdates -= _DeltaTimeFollowUpdates;
            
            bool closeToPlayer = Vector3.Distance(transform.position, _PlayerController.transform.position) < MaxPlayerDistance;
            bool playerNotMoving = !_PlayerController.IsMoving;
            if (closeToPlayer || playerNotMoving)
                break;
            
            // Add player positions continuously.
            // Positions are overwritten at 1 second intervals.
            _LastPlayerPosition = _PlayerPosition;
            _PlayerPosition = _PlayerController.transform.position;
        }
        
        // Only once enough time has passed will the banana actually be moved.
        // In the meantime, a lot of updates have been steadily going.
        float lerpT = _TimerUpdates / _DeltaTimeFollowUpdates;
        Vector3 lerpPos = Vector3.Lerp(_LastPlayerPosition, _PlayerPosition, lerpT);
        transform.position = lerpPos + Vector3.up * FollowVerticalOffset;
    }

    void Update()
    {
        if (!_PickedUp)
        {
            HandleBobbing();
            return;
        }

        if (!_Collected)
            FollowPlayer();
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (_PickedUp || _Collected)
            return;
            
        if (ID == -1)
            Debug.LogWarning("ID has not been set!", context: this);
        
        if (Utils.IsInMask(collision.gameObject.layer, PlayerLayer))
        {
            BananaChannel?.Raise(this);
            // Follow player
            _PickedUp = true;
            _PlayerController = collision.gameObject.GetComponent<PlayerController>();
            _LastPlayerPosition = _PlayerController.transform.position;
            _PlayerPosition = _PlayerController.transform.position;
        }
    }
}
