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
        Disable,
        Lost
    }
    
    [TitleGroup("References")]
    public LayerMask PlayerLayer;
    public BananaChannel BananaChannel;
    public AudioClip BananaPulse;
    public AudioClip BananaTouch;
    public AudioClip BananaCollect;
    
    [TitleGroup("Input")] 
    [SerializeField] private float _BobOffset = 0.15f;
    [SerializeField] private float _BobTime = 1f;
    [SerializeField] private float _FollowVerticalOffset = 1;
    [SerializeField] private float _DelayFollow = 0.5f;
    [SerializeField] private float _DelayCollect = 0.15f;
    [SerializeField] private float _TimeSquish = 1;
    [SerializeField] private float _TimeLoseAnimation = 3f;
    [SerializeField] private float _TimePulse = 5f;
    [SerializeField] private float _AudioVolume = 0.3f;
    [SerializeField] private float _PulseRotationOffset = Mathf.PI / 6;
    [SerializeField] private float _PulseAnimationTime = 0.2f;
    [ReadOnly] public int ID = -1;
    [ReadOnly] public BananaState State = BananaState.Idle;
    
    private float _DisappearAnimationTimer;

    private Vector3 _InitialPosition;
    private float _TimerBobbing = 0.5f;
    private int _UpdateCounter;
    private int _BobDirection = 1;
    
    private PlayerController _PlayerController;
    private AudioSource _AudioSource;
    private SpriteRenderer _SpriteRenderer;
    private Animator _Animator;
    private Light2D _Light;
    private float _InitialOuterLightRadius;
    
    private bool _PickedUp;
    private bool _Collected;
    private Vector3 _PreviousPosition;
    private Vector3 _NextPosition;
    private bool _StoppedMovingNow = true;
    private float _TimerUpdates;
    private float _TimerLoseAnimation;
    private float _TimerPulseTrigger;
    [SerializeField] private float _TimerPulseAnimation;

    void OnValidate()
    {
        _Light = transform.GetComponentInChildren<Light2D>();
        _InitialOuterLightRadius = _Light.pointLightOuterRadius;
        _AudioSource = transform.GetComponentInChildren<AudioSource>();
        _SpriteRenderer = GetComponent<SpriteRenderer>();
        _Animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (BananaChannel == null)
            Debug.LogWarning("BananaChannel has not been set!", context: this);
        
        _InitialPosition = transform.position;
        _AudioSource.clip = BananaPulse;
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
        State = BananaState.Collected;
        // Notify LevelManager banana has been collected.
        BananaChannel?.Raise(this);
        
        _AudioSource.PlayOneShot(BananaCollect, _AudioVolume);
    }

    void HandleDisappearAnimation()
    {
        _DisappearAnimationTimer += Time.deltaTime;
        float lerpT = _DisappearAnimationTimer / _TimeSquish;
        lerpT = Utils.EaseInCubic(lerpT);
        
        float lerpScaleY = 1 - lerpT;
        Vector3 lerpScale = new Vector3(transform.localScale.x, lerpScaleY, transform.localScale.z);
        transform.localScale = lerpScale;
        
        // Make outer light radius smaller
        _Light.pointLightOuterRadius = lerpScaleY * _InitialOuterLightRadius;
        
        if (lerpT >= 1)
            State = BananaState.Disable;
    }

    enum PulseAnimationState
    {
        TurnLeft = 0,
        SwingRight,
        SwingLeft,
        TurnRight,
        Finish
    }

    [SerializeField] private PulseAnimationState _PulseAnimationState = PulseAnimationState.Finish;
    
    // TODO:
    void HandlePulseAnimation()
    {
        if (_PulseAnimationState is PulseAnimationState.Finish)
            return;
        
        _TimerPulseAnimation += Time.deltaTime;
        float t = _TimerPulseAnimation / _PulseAnimationTime;
        
        switch (_PulseAnimationState)
        {
            case PulseAnimationState.TurnLeft:
            {
                Quaternion leftEdge = Quaternion.Euler(0, 0, 0);
                Quaternion rightEdge = Quaternion.Euler(0, 0, 2 * _PulseRotationOffset);
                transform.rotation = Quaternion.Lerp(leftEdge, rightEdge, t);
                // Interrupt half-way. "Distances" between points lerped must be the same to maintain the same speed.
                if (t >= 0.5)
                {
                    _PulseAnimationState = PulseAnimationState.SwingRight;
                    _TimerPulseAnimation -= _PulseAnimationTime * 0.5f;
                }
                break;
            }
            case PulseAnimationState.SwingRight:
            {
                Quaternion leftEdge = Quaternion.Euler(0, 0, _PulseRotationOffset);
                Quaternion rightEdge = Quaternion.Euler(0, 0, -_PulseRotationOffset);
                transform.rotation = Quaternion.Lerp(leftEdge, rightEdge, t);
                if (t >= 1)
                {
                    _PulseAnimationState = PulseAnimationState.SwingLeft;
                    _TimerPulseAnimation -= _PulseAnimationTime;
                }
                break;
            }
            case PulseAnimationState.SwingLeft:
            {
                Quaternion leftEdge = Quaternion.Euler(0, 0, -_PulseRotationOffset);
                Quaternion rightEdge = Quaternion.Euler(0, 0, _PulseRotationOffset);
                transform.rotation = Quaternion.Lerp(leftEdge, rightEdge, t);
                if (t >= 1)
                {
                    _PulseAnimationState = PulseAnimationState.TurnRight;
                    _TimerPulseAnimation -= _PulseAnimationTime;
                }
                break;
            }
            case PulseAnimationState.TurnRight:
            {
                Quaternion leftEdge = Quaternion.Euler(0, 0, _PulseRotationOffset);
                Quaternion rightEdge = Quaternion.Euler(0, 0, -_PulseRotationOffset);
                transform.rotation = Quaternion.Lerp(leftEdge, rightEdge, t);
                // Interrupt half-way. "Distances" between points lerped must be the same to maintain the same speed.
                if (t >= 0.5)
                {
                    _PulseAnimationState = PulseAnimationState.Finish;
                    _TimerPulseAnimation -= _PulseAnimationTime * 0.5f;
                    transform.rotation = Quaternion.Euler(0, 0, 0);
                }
                break;
            }
        }
    }

    void Update()
    {
        // Pulse sound
        if (State is BananaState.Idle or BananaState.PickedUp)
        {
            _TimerPulseTrigger += Time.deltaTime;
            if (_TimerPulseTrigger >= _TimePulse)
            {
                _PulseAnimationState = PulseAnimationState.TurnLeft;
                _TimerPulseAnimation = 0;
                _AudioSource.PlayOneShot(BananaPulse, _AudioVolume);
                _TimerPulseTrigger -= _TimePulse;
            }
            
            HandlePulseAnimation();
        }
        
        // Main update
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
            case BananaState.Disable:
                _SpriteRenderer.enabled = false;
                _Light.enabled = false;
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
        if (State is BananaState.Collected or BananaState.Disable)
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
        
        if (Utils.IsInMask(collision.gameObject.layer, PlayerLayer) && State == BananaState.Idle)
        {
            State = BananaState.PickedUp;
            
            // Follow player
            _PlayerController = collision.gameObject.GetComponent<PlayerController>();
            _NextPosition = _PlayerController.transform.position;
            _PreviousPosition = transform.position;
            // If the player dies before collecting the banana, it's lost
            // and returns to its original position.
            _PlayerController.Died += OnPlayerDeath;
            
            _AudioSource.PlayOneShot(BananaTouch, _AudioVolume);
        }
    }
}
