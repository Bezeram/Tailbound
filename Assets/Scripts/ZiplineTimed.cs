using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class ZiplineTimed : ActivatableEntity
{
    [TitleGroup("References")]
    [Required] public EntitiesSettings Settings;
    [ShowInInspector] private bool AttachmentAtStart = true;

    // Transforms
    [Required] private Transform _StartTransform;
    [Required] private Transform _EndTransform;
    [Required] private Transform _AttachmentTransform;
    [Required] private Transform _BeltTransform;

    // Audio
    private AudioSource _AudioSource;
    public AudioClip _ForwardAudioClip;
    public AudioClip _ImpactAudioClip;
    public AudioClip _RetractionAudioClip;
    public AudioClip _ResetAudioClip;

    public enum State
    { 
        Idle = 0,
        Forward,
        IdleEnd,
        Backward,
    }

    [TitleGroup("Info")]
    [ReadOnly, ShowInInspector] private State CurrentState = State.Idle;
    [ReadOnly, ShowInInspector] private bool IsActive;

    void ReattachBelt()
    {
        // Reposition connecting belt after moving the start and end points.
        float delta_x = _StartTransform.position.x - _EndTransform.position.x;
        float delta_y = _StartTransform.position.y - _EndTransform.position.y;

        // Reposition attachment
        if (AttachmentAtStart)
        {
            _AttachmentTransform.position = _StartTransform.position;
        }

        // Retransform belt
        _BeltTransform.position = (_StartTransform.position + _EndTransform.position) / 2.0f;
        _BeltTransform.localScale = new Vector3(Vector3.Distance(_StartTransform.position, _EndTransform.position) * 0.20f, 1.0f, 1.0f);
        _BeltTransform.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f / Mathf.PI * Mathf.Atan2(delta_y, delta_x));
    }

    // Where the ZipLine is in between the StartPoint and EndPoint
    private float _TimerForward = 0f;
    private float _TimerBeforeRetraction = 0f;
    private float _TimerBackward = 0f;
    private float _TimerReset = 0f;

    void Awake()
    {
        // Init referenced transforms
        if (_StartTransform == null) 
            _StartTransform = transform.Find("StartPoint");
        if (_EndTransform == null) 
            _EndTransform = transform.Find("EndPoint");
        if (_AttachmentTransform == null)
            _AttachmentTransform = transform.Find("Attachment");
        if (_BeltTransform == null)
            _BeltTransform = transform.Find("Belt");
        if (_AudioSource == null)
        {
            _AudioSource = GetComponent<AudioSource>();
            _AudioSource.volume = 0.8f;
        }

        // By default, the Zipline is ready to start the moment it is instantiated.
        _TimerReset = 2 * Settings.Zipline.DelayResetSeconds;
    }

    void OnValidate()
    {
        Awake();
    }

    public override void ReceiveActivation()
    {
        IsActive = true;
    }

    public override void ReceiveDeactivation()
    {
        IsActive = false;
    }

    void Start()
    {
        // Initially off
        IsActive = false;
        CurrentState = State.Idle;
        // Init direction
        _AttachmentTransform.position = _StartTransform.position;
    }

    void Update()
    {
        // Only run in Editor Mode
        if (!Application.isPlaying)
        {
            // Reattach belt
            ReattachBelt();
            return;
        }

        switch (CurrentState)
        {
            case State.Idle:
                {
                    _TimerReset += Time.deltaTime;

                    // Start moving if active
                    if (IsActive && _TimerReset > Settings.Zipline.DelayResetSeconds)
                    {
                        CurrentState = State.Forward;
                        _TimerForward = 0f;
                        // Play sound
                        _AudioSource.clip = _ForwardAudioClip;
                        _AudioSource.loop = false;
                        _AudioSource.Play();
                    }
                    break;
                }
            case State.Forward:
                {
                    _TimerForward += Time.deltaTime;
                        
                    // Get progress as value between 0 and 1.
                    float progress = _TimerForward / Settings.Zipline.TimeForwardSeconds;
                    // Use easing function for dramatic flow.
                    progress = Utils.EaseInCubic(progress);
                    // Lerp in-between the start and end point.
                    Vector2 position = Vector2.Lerp(_StartTransform.position, _EndTransform.position, progress);
                    _AttachmentTransform.position = new(position.x, position.y, _AttachmentTransform.position.z);

                    // Check if the end has been reached
                    if (progress >= 1)
                    {
                        CurrentState = State.IdleEnd;
                        _TimerBeforeRetraction = 0f;
                        // Play sound
                        _AudioSource.clip = _ImpactAudioClip;
                        _AudioSource.loop = false;
                        _AudioSource.Play();
                    }
                }
                break;
            case State.IdleEnd:
                {
                    _TimerBeforeRetraction += Time.deltaTime;

                    // Wait a bit before starting to retract
                    if (_TimerBeforeRetraction > Settings.Zipline.DelayRetractionSeconds)
                    {
                        CurrentState = State.Backward;
                        _TimerBackward = 0f;
                        // Play sound
                        _AudioSource.clip = _RetractionAudioClip;
                        _AudioSource.loop = true;
                        _AudioSource.Play();
                    }

                }
                break;
            case State.Backward:
                {
                    _TimerBackward += Time.deltaTime;

                    // Backwards movement remains linear.
                    float progress = _TimerBackward / Settings.Zipline.TimeBackwardSeconds;
                    // Use easing function for better flow.
                    progress = Utils.EaseInQuad(progress);
                    // Lerp in-between the start and end point.
                    Vector2 position = Vector2.Lerp(_EndTransform.position, _StartTransform.position, progress);
                    _AttachmentTransform.position = new(position.x, position.y, _AttachmentTransform.position.z);

                    // Check if the ZipLine has returned to the beginning
                    if (progress >= 1)
                    {
                        CurrentState = State.Idle;
                        _TimerReset = 0f;
                        // Play sound
                        _AudioSource.clip = _ResetAudioClip;
                        _AudioSource.loop = false;
                        _AudioSource.Play();
                    }
                    break;
                }
        }
    }

}
