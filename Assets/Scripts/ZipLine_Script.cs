using UnityEngine;

public class ZipLine_Script : MonoBehaviour
{
    [Header("Input")]
    public Vector2 StartPoint;
    public Vector2 EndPoint;

    [Header("References")]
    public EntitiesSettings Settings;
    public Transform StartPointTransform;
    public Transform EndPointTransform;
    public Transform AttachmentTransform;
    public Transform BeltTransform;

    public enum State
    { 
        Idle = 0,
        Forward,
        IdleEnd,
        Backward,
    }
    [Header("Mode")]
    [SerializeField] private State CurrentState = State.Idle;
    [SerializeField] private bool IsActive;

    private Vector3 _Direction;
    private float _Speed = 0f;
    private float _TimerRetraction = 0f;

    public void Attach()
    {
        IsActive = true;
    }

    public void Detach()
    {
        IsActive = false;
    }

    // Called when moving the start and end points using the
    // inspector variables StartPoint and EndPoint
    void OnValidate()
    {
        ReattachBelt();
    }

    // Called by custom editor scripts
    public void ConnectBelt()
    {
        // Reposition according to the transforms
        StartPoint = new(StartPointTransform.position.x, StartPointTransform.position.y);
        EndPoint = new(EndPointTransform.position.x, EndPointTransform.position.y);

        ReattachBelt();
    }

    void ReattachBelt()
    {
        // Reposition connecting belt after moving the start and end points.
        float delta_x = StartPoint.x - EndPoint.x;
        float delta_y = StartPoint.y - EndPoint.y;

        // Move the start and end objects
        StartPointTransform.position = new(StartPoint.x, StartPoint.y, StartPointTransform.position.z);
        EndPointTransform.position = new(EndPoint.x, EndPoint.y, EndPointTransform.position.z);

        // Move attachment object
        AttachmentTransform.position = StartPointTransform.position;

        // Move, rotate and scale the connecting belt
        BeltTransform.position = (StartPointTransform.position + EndPointTransform.position) / 2.0f;
        BeltTransform.localScale = new Vector3(Vector3.Distance(StartPointTransform.position, EndPointTransform.position) * 0.20f, 1.0f, 1.0f);
        BeltTransform.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f / Mathf.PI * Mathf.Atan2(delta_y, delta_x));
    }

    void Start()
    {
        // Initially off
        IsActive = false;
        CurrentState = State.Idle;
        // Init direction
        AttachmentTransform.position = StartPointTransform.position;
        _Direction = EndPointTransform.position - StartPointTransform.position;
        _Direction.Normalize();
    }

    void Update()
    {
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
                    if (AttachmentReachedAt(StartPointTransform, EndPointTransform))
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

                    AttachmentTransform.position -= _Direction * Time.deltaTime * _Speed;
                    // Check if the ZipLine has returned to the beginning
                    if (AttachmentReachedAt(EndPointTransform, StartPointTransform))
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
        Vector3 positionStart = startPoint.position;
        Vector3 positionAttachment = AttachmentTransform.position;
        Vector3 positionEnd = endPoint.position;

        float t_x = Mathf.InverseLerp(positionStart.x, positionEnd.x, positionAttachment.x);
        float t_y = Mathf.InverseLerp(positionStart.y, positionEnd.y, positionAttachment.y);
        float t_z = Mathf.InverseLerp(positionStart.z, positionEnd.z, positionAttachment.z);

        return t_x >= 1f || t_y >= 1f || t_z >= 1f;
    }
}
