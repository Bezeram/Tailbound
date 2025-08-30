using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class CollectableBanana : MonoBehaviour
{
    [TitleGroup("References")]
    public LayerMask PlayerLayer;
    public BananaChannel BananaChannel;
    
    [TitleGroup("Input")] 
    public float BobOffset = 0.15f;
    public float BobTime = 1f;
    public float FollowVerticalOffset = 1;
    public float DelayFollow = 0.5f;
    
    [ReadOnly] public int ID = -1;

    private Vector3 _InitialPosition;
    private float _TimerBobbing = 0.5f;
    private int _UpdateCounter;
    private int _BobDirection = 1;
    
    private PlayerController _PlayerController;
    private bool _PickedUp;
    private bool _Collected;
    private Vector3 _PreviousPosition;
    private Vector3 _NextPosition;
    private bool _StoppedMovingNow;
    private float _TimerUpdates;

    void Start()
    {
        if (BananaChannel == null)
            Debug.LogWarning("BananaChannel has not been set!", context: this);
        
        _InitialPosition = transform.position;
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
        // Only update if the player is moving.
        if (_PlayerController.IsMoving)
        {
            _StoppedMovingNow = false;
            
            _TimerUpdates += Time.deltaTime;
            while (_TimerUpdates >= DelayFollow)
            {
                _TimerUpdates -= DelayFollow;
                // Update position                
                _PreviousPosition = _NextPosition;
                _NextPosition = _PlayerController.transform.position + Vector3.up * FollowVerticalOffset;
            }
        }
        else
        {
            if (!_StoppedMovingNow)
            {
                _StoppedMovingNow = true;
                // Stop banana immediately.
                // Only run this code once.
                _NextPosition = transform.position;
                _PreviousPosition = _NextPosition;
                _TimerUpdates = DelayFollow;
            }
        }
        
        float lerpT = _TimerUpdates / DelayFollow;
        transform.position = Vector3.Lerp(_PreviousPosition, _NextPosition, lerpT);
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
            _NextPosition = _PlayerController.transform.position;
            _PreviousPosition = transform.position;
        }
    }
}
