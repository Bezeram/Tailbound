using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class ZiplineTimed : ActivatableEntity
{
    [TitleGroup("References")]
    [Required] public EntitiesSettings Settings;
    [ShowInInspector] private bool AttachmentAtStart = true;

    private Transform StartTransform;
    private Transform EndTransform;
    private Transform AttachmentTransform;
    private Transform BeltTransform;

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
        float delta_x = StartTransform.position.x - EndTransform.position.x;
        float delta_y = StartTransform.position.y - EndTransform.position.y;

        // Reposition attachment
        if (AttachmentAtStart)
        {
            AttachmentTransform.position = StartTransform.position;
        }

        // Retransform belt
        BeltTransform.position = (StartTransform.position + EndTransform.position) / 2.0f;
        BeltTransform.localScale = new Vector3(Vector3.Distance(StartTransform.position, EndTransform.position) * 0.20f, 1.0f, 1.0f);
        BeltTransform.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f / Mathf.PI * Mathf.Atan2(delta_y, delta_x));
    }

    // Where the ZipLine is in between the StartPoint and EndPoint
    private float _TimerForward = 0f;
    private float _TimerBeforeRetraction = 0f;
    private float _TimerBackward = 0f;

    void Awake()
    {
        // Init referenced transforms
        StartTransform = transform.Find("StartPoint");
        EndTransform = transform.Find("EndPoint");
        AttachmentTransform = transform.Find("Attachment");
        BeltTransform = transform.Find("Belt");
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
        AttachmentTransform.position = StartTransform.position;
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
                        _TimerForward = 0f;
                    }
                    break;
                }
            case State.Forward:
                {
                    _TimerForward += Time.deltaTime;
                        
                    // Get progress as value between 0 and 1.
                    float progress = _TimerForward / Settings.TimeForwardSeconds;
                    // Use easing function for dramatic flow.
                    progress = Utils.EaseInCubic(progress);
                    // Lerp in-between the start and end point.
                    Vector2 position = Vector2.Lerp(StartTransform.position, EndTransform.position, progress);
                    AttachmentTransform.position = new(position.x, position.y, AttachmentTransform.position.z);

                    // Check if the end has been reached
                    if (progress >= 1)
                    {
                        CurrentState = State.IdleEnd;
                        _TimerBeforeRetraction = 0f;
                    }
                }
                break;
            case State.IdleEnd:
                {
                    _TimerBeforeRetraction += Time.deltaTime;

                    // Wait a bit before starting to retract
                    if (_TimerBeforeRetraction > Settings.DelayRetractionSeconds)
                    {
                        CurrentState = State.Backward;
                        _TimerBackward = 0f;
                    }
                }
                break;
            case State.Backward:
                {
                    _TimerBackward += Time.deltaTime;

                    // Backwards movement remains linear.
                    float progress = _TimerBackward / Settings.TimeBackwardSeconds;
                    // Use easing function for better flow.
                    progress = Utils.EaseInQuad(progress);
                    // Lerp in-between the start and end point.
                    Vector2 position = Vector2.Lerp(EndTransform.position, StartTransform.position, progress);
                    AttachmentTransform.position = new(position.x, position.y, AttachmentTransform.position.z);

                    // Check if the ZipLine has returned to the beginning
                    if (progress >= 1)
                    {
                        CurrentState = State.Idle;
                    }
                    break;
                }
        }
    }

}
