using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CollectableBanana : MonoBehaviour
{
    public enum BananaState
    {
        Idle = 0,
        PickedUp,
        Collected,
        Destroy,
        Lost
    }
    
    [TitleGroup("References")]
    public LayerMask PlayerLayer;
    public BananaChannel BananaChannel;
    
    [TitleGroup("Input")] 
    [SerializeField] private float _BobOffset = 0.15f;
    [SerializeField] private float _BobTime = 1f;
    [SerializeField] private float _FollowVerticalOffset = 1;
    [SerializeField] private float _DelayFollow = 0.5f;
    [SerializeField] private float _DelayCollect = 0.15f;
    [SerializeField] private float _TimeSquish = 1;
    [SerializeField] private float _TimeLoseAnimation = 3f; 
    [ReadOnly] public int ID = -1;
    [ReadOnly] public BananaState State = BananaState.Idle; 
    
    private float _AnimationTimer;

    private Vector3 _InitialPosition;
    private float _TimerBobbing = 0.5f;
    private int _UpdateCounter;
    private int _BobDirection = 1;
    
    private PlayerController _PlayerController;
    private Light2D _Light;
    private float _InitialOuterLightRadius;
    
    private bool _PickedUp;
    private bool _Collected;
    private Vector3 _PreviousPosition;
    private Vector3 _NextPosition;
    private bool _StoppedMovingNow = true;
    private float _TimerUpdates;
    private float _TimerLoseAnimation;

    void OnValidate()
    {
        _Light = transform.GetComponentInChildren<Light2D>();
        _InitialOuterLightRadius = _Light.pointLightOuterRadius;
    }

    void Start()
    {
        if (BananaChannel == null)
            Debug.LogWarning("BananaChannel has not been set!", context: this);
        
        _InitialPosition = transform.position;
    }

    void HandleBobbing()
    {
        _TimerBobbing += _BobDirection * Time.deltaTime;
        if (_TimerBobbing >= _BobTime || _TimerBobbing < 0)
        {
            _TimerBobbing = Mathf.Clamp(_TimerBobbing, 0, _BobTime);
            _BobDirection *= -1;
        }
        
        // Bob animation
        float top = _BobOffset;
        float bottom = -_BobOffset;
        float t = _TimerBobbing / _BobTime;
        transform.position = _InitialPosition + Vector3.up * Mathf.Lerp(bottom, top, t);
    }

    void FollowPlayer()
    {
        // The banana follows but stops when close enough to the player.
        // Note: A new banana should keep its distance to the last banana collected, otherwise the bananas
        // are going to stack on top of each other and not form a beautiful banana trail.
        // Only update if the player is moving.
        _TimerUpdates += Time.deltaTime;
        while (_TimerUpdates >= _DelayFollow)
        {
            _TimerUpdates -= _DelayFollow;
            _PreviousPosition = _NextPosition;
            
            if (!_PlayerController.IsMoving)
            {
                if (_StoppedMovingNow)
                {
                    // When the player stops, record the current
                    // position as the next position.
                    _StoppedMovingNow = false;
                    _NextPosition = transform.position;
                }
            }
            else
            {
                _StoppedMovingNow = true;
                // Update position                
                _NextPosition = _PlayerController.transform.position + Vector3.up * _FollowVerticalOffset;
            }
        }
        
        float lerpT = _TimerUpdates / _DelayFollow;
        transform.position = Vector3.Lerp(_PreviousPosition, _NextPosition, lerpT);
    }

    void HandleCollection()
    {
        // Notify LevelManager banana has been collected.
        BananaChannel?.Raise(this);
        
        State = BananaState.Collected;        
    }

    void HandleDisappearAnimation()
    {
        _AnimationTimer += Time.deltaTime;
        float lerpT = _AnimationTimer / _TimeSquish;
        lerpT = Utils.EaseInCubic(lerpT);
        
        float lerpScaleY = 1 - lerpT;
        Vector3 lerpScale = new Vector3(transform.localScale.x, lerpScaleY, transform.localScale.z);
        transform.localScale = lerpScale;
        
        // Make outer light radius smaller
        _Light.pointLightOuterRadius = lerpScaleY * _InitialOuterLightRadius;
        
        if (lerpT >= 1)
            State = BananaState.Destroy;
    }

    void Update()
    {
        switch (State)
        {
            case BananaState.Idle:
                HandleBobbing();
                
                break;
            case BananaState.PickedUp:
                FollowPlayer();
                // Player must stand on the ground to register
                // collecting the banana.
                if (_PlayerController.TimeOnGround >= _DelayCollect)
                    HandleCollection();
                
                break;
            case BananaState.Collected:
                HandleDisappearAnimation();
                break;
            case BananaState.Destroy:
                Destroy(gameObject);
                break;
            case BananaState.Lost:
               HandleLoseAnimation();
               break;
        }
    }

    void HandleLoseAnimation()
    {
        _TimerLoseAnimation += Time.deltaTime;
        float lerpT = _TimerLoseAnimation / _TimeLoseAnimation;
        lerpT = Utils.EaseOutCubic(lerpT);
        transform.position = Vector3.Lerp(_PreviousPosition, _NextPosition, lerpT);
        
        if (lerpT >= 1)
            State = BananaState.Idle;
    }

    void OnPlayerDeath()
    {
        if (State is BananaState.Collected or BananaState.Destroy)
            return;
        
        State = BananaState.Lost;
        _NextPosition = _InitialPosition;
        _PreviousPosition = transform.position;
        _TimerLoseAnimation = 0;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (_PickedUp || _Collected)
            return;
            
        if (ID == -1)
            Debug.LogWarning("ID has not been set!", context: this);
        
        if (Utils.IsInMask(collision.gameObject.layer, PlayerLayer))
        {
            State = BananaState.PickedUp;
            
            // Follow player
            _PlayerController = collision.gameObject.GetComponent<PlayerController>();
            _NextPosition = _PlayerController.transform.position;
            _PreviousPosition = transform.position;
            // If the player dies before collecting the banana, it's lost
            // and returns to its original position.
            _PlayerController.Died += OnPlayerDeath;
        }
    }
}
