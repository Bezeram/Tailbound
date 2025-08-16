using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class ZiplineAccelerated : ActivatableEntity
{
    [TitleGroup("References")]
    [Required] public EntitiesSettings Settings;
    [ShowInInspector] private bool AttachmentAtStart = true;

    private Transform _StartTransform;
    private Transform _EndTransform;
    private Transform _AttachmentTransform;
    private Transform _BeltTransform;

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
    private float _Speed = 0f;
    private Vector3 _Direction = Vector2.zero;
    private float _TimerRetraction = 0f;

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
        _AttachmentTransform.transform.position = _StartTransform.transform.position;
        _Direction = _EndTransform.transform.position - _StartTransform.transform.position;
        _Direction.Normalize();
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
                    // Start moving if active
                    if (IsActive)
                    {
                        CurrentState = State.Forward;
                        // Play sound
                        _AudioSource.clip = _ForwardAudioClip;
                        _AudioSource.loop = false;
                        _AudioSource.Play();
                    }
                    break;
                }
            case State.Forward:
                {
                    // Accelerate
                    _Speed += Time.deltaTime * Settings.AccelerationForward;
                    // Cap max speed
                    _Speed = Mathf.Clamp(_Speed, 0f, Settings.MaxSpeedForward);

                    _AttachmentTransform.position += _Direction * Time.deltaTime * _Speed;
                    // Check if the end has been reached
                    if (AttachmentReachedAt(_StartTransform, _EndTransform))
                    {
                        CurrentState = State.IdleEnd;
                        _Speed = 0f;
                        // Play sound
                        _AudioSource.clip = _ImpactAudioClip;
                        _AudioSource.loop = false;
                        _AudioSource.Play();
                    }
                }
                break;
            case State.IdleEnd:
                {
                    _TimerRetraction += Time.deltaTime;
                    // Wait a bit before starting to retract
                    if (_TimerRetraction > Settings.DelayRetractionSeconds)
                    {
                        CurrentState = State.Backward;
                        _TimerRetraction = 0f;
                        // Play sound
                        _AudioSource.clip = _RetractionAudioClip;
                        _AudioSource.loop = true;
                        _AudioSource.Play();
                    }
                }
                break;
            case State.Backward:
                {
                    // Accelerate
                    _Speed += Time.deltaTime * Settings.AccelerationBackwards;
                    // Cap max speed
                    _Speed = Mathf.Clamp(_Speed, 0f, Settings.MaxSpeedBackwards);

                    _AttachmentTransform.transform.position -= _Direction * Time.deltaTime * _Speed;
                    // Check if the ZipLine has returned to the beginning
                    if (AttachmentReachedAt(_EndTransform, _StartTransform))
                    {
                        CurrentState = State.Idle;
                        _Speed = 0f;
                        // Play sound
                        _AudioSource.clip = _ResetAudioClip;
                        _AudioSource.loop = false;
                        _AudioSource.Play();
                    }
                    break;
                }
        }
    }

    bool AttachmentReachedAt(Transform startPoint, Transform endPoint)
    {
        Vector3 positionStart = startPoint.transform.position;
        Vector3 positionAttachment = _AttachmentTransform.transform.position;
        Vector3 positionEnd = endPoint.transform.position;

        float t_x = Mathf.InverseLerp(positionStart.x, positionEnd.x, positionAttachment.x);
        float t_y = Mathf.InverseLerp(positionStart.y, positionEnd.y, positionAttachment.y);
        float t_z = Mathf.InverseLerp(positionStart.z, positionEnd.z, positionAttachment.z);

        return t_x >= 1f || t_y >= 1f || t_z >= 1f;
    }

}
