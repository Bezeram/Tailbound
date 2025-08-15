using UnityEngine;

public class ZipLine_Script : MonoBehaviour
{
    [Header("References")]
    public EntitiesSettings Settings;
    public GameObject start_point;
    public GameObject end_point;
    public GameObject attachment_point;
    public GameObject connecting_belt;

    public bool set_position;

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

    void OnValidate()
    {
        // Reposition connecting belt after moving the start and end points.
        float delta_x = start_point.transform.position.x - end_point.transform.position.x;
        float delta_y = start_point.transform.position.y - end_point.transform.position.y;

        connecting_belt.transform.position = (start_point.transform.position + end_point.transform.position) / 2.0f;
        connecting_belt.transform.localScale = new Vector3(Vector3.Distance(start_point.transform.position, end_point.transform.position) * 0.20f, 1.0f, 1.0f);
        connecting_belt.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f / Mathf.PI * Mathf.Atan2(delta_y, delta_x));
    }

    void Start()
    {
        // Initially off
        IsActive = false;
        CurrentState = State.Idle;
        // Init direction
        attachment_point.transform.position = start_point.transform.position;
        _Direction = end_point.transform.position - start_point.transform.position;
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

                    attachment_point.transform.position += _Direction * Time.deltaTime * _Speed;
                    // Check if the end has been reached
                    if (AttachmentReachedAt(start_point, end_point))
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

                    attachment_point.transform.position -= _Direction * Time.deltaTime * _Speed;
                    // Check if the ZipLine has returned to the beginning
                    if (AttachmentReachedAt(end_point, start_point))
                    {
                        CurrentState = State.Idle;
                        _Speed = 0f;
                    }
                    break;
                }
        }
    }

    bool AttachmentReachedAt(GameObject startPoint, GameObject endPoint)
    {
        Vector3 positionStart = startPoint.transform.position;
        Vector3 positionAttachment = attachment_point.transform.position;
        Vector3 positionEnd = endPoint.transform.position;

        float t_x = Mathf.InverseLerp(positionStart.x, positionEnd.x, positionAttachment.x);
        float t_y = Mathf.InverseLerp(positionStart.y, positionEnd.y, positionAttachment.y);
        float t_z = Mathf.InverseLerp(positionStart.z, positionEnd.z, positionAttachment.z);

        return t_x >= 1f || t_y >= 1f || t_z >= 1f;
    }
}
