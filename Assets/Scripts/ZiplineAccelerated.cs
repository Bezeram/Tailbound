using Sirenix.OdinInspector;
using UnityEngine;

[ExecuteAlways]
public class ZiplineAccelerated : ActivatableEntity
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
    private float _Speed = 0f;
    private Vector3 _Direction = Vector2.zero;
    private float _TimerRetraction = 0f;

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
        AttachmentTransform.transform.position = StartTransform.transform.position;
        _Direction = EndTransform.transform.position - StartTransform.transform.position;
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
                    }
                    break;
                }
            case State.Forward:
                {
                    // Accelerate
                    _Speed += Time.deltaTime * Settings.AccelerationForward;
                    // Cap max speed
                    _Speed = Mathf.Clamp(_Speed, 0f, Settings.MaxSpeedForward);

                    AttachmentTransform.position += _Direction * Time.deltaTime * _Speed;
                    // Check if the end has been reached
                    if (AttachmentReachedAt(StartTransform, EndTransform))
                    {
                        CurrentState = State.IdleEnd;
                        _Speed = 0f;
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
                    }
                }
                break;
            case State.Backward:
                {
                    // Accelerate
                    _Speed += Time.deltaTime * Settings.AccelerationBackwards;
                    // Cap max speed
                    _Speed = Mathf.Clamp(_Speed, 0f, Settings.MaxSpeedBackwards);

                    AttachmentTransform.transform.position -= _Direction * Time.deltaTime * _Speed;
                    // Check if the ZipLine has returned to the beginning
                    if (AttachmentReachedAt(EndTransform, StartTransform))
                    {
                        CurrentState = State.Idle;
                        _Speed = 0f;
                    }
                    break;
                }
        }
    }

    bool AttachmentReachedAt(Transform startPoint, Transform endPoint)
    {
        Vector3 positionStart = startPoint.transform.position;
        Vector3 positionAttachment = AttachmentTransform.transform.position;
        Vector3 positionEnd = endPoint.transform.position;

        float t_x = Mathf.InverseLerp(positionStart.x, positionEnd.x, positionAttachment.x);
        float t_y = Mathf.InverseLerp(positionStart.y, positionEnd.y, positionAttachment.y);
        float t_z = Mathf.InverseLerp(positionStart.z, positionEnd.z, positionAttachment.z);

        return t_x >= 1f || t_y >= 1f || t_z >= 1f;
    }

}
