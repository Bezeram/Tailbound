using UnityEngine;

public class ZipLine_Script : MonoBehaviour
{
    [Header("References")]
    public EntitiesSettings settings;
    public GameObject start_point;
    public GameObject end_point;
    public GameObject attachment_point;
    public GameObject connecting_belt;

    public bool set_position;

    [Header("Info")]
    public bool _is_attached;
    public bool _is_active;

    private Vector3 _Direction;
    private float _Speed = 0f;
    private bool _Forward;
    private float _TimerRetraction = 0f;

    public void Attach()
    {
        _is_attached = true;
        _is_active = true;
    }

    public void Detach()
    {
        _is_attached = false;
    }

    void OnValidate()
    {
        float delta_x = start_point.transform.position.x - end_point.transform.position.x;
        float delta_y = start_point.transform.position.y - end_point.transform.position.y;

        connecting_belt.transform.position = (start_point.transform.position + end_point.transform.position) / 2.0f;
        connecting_belt.transform.localScale = new Vector3(Vector3.Distance(start_point.transform.position, end_point.transform.position) * 0.20f, 1.0f, 1.0f);
        connecting_belt.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 180.0f / Mathf.PI * Mathf.Atan2(delta_y, delta_x));
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _is_active = false;
        _is_attached = false;
        _Forward = true;
        attachment_point.transform.position = start_point.transform.position;
        _Direction = end_point.transform.position - start_point.transform.position;
        _Direction.Normalize();
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

    // Update is called once per frame
    void Update()
    {
        if (!_is_active)
            return;

        if (_Forward)
        {
            // Accelerate
            _Speed += Time.deltaTime * settings.AccelerationForward;
            // Cap max speed
            _Speed = Mathf.Clamp(_Speed, 0f, settings.MaxSpeedForward);

            attachment_point.transform.position += _Direction * Time.deltaTime * _Speed;
            if (AttachmentReachedAt(start_point, end_point))
            {
                _Forward = false;
                _Speed = 0f;
            }
        } 
        else
        {
            _TimerRetraction += Time.deltaTime;
            if (_TimerRetraction > settings.DelayRetractionSeconds)
            {
                // Accelerate
                _Speed += Time.deltaTime * settings.AccelerationBackwards;
                // Cap max speed
                _Speed = Mathf.Clamp(_Speed, 0f, settings.MaxSpeedBackwards);

                attachment_point.transform.position -= _Direction * Time.deltaTime * _Speed;
                if (AttachmentReachedAt(end_point, start_point))
                {
                    _Forward = true;
                    _TimerRetraction = 0f;
                    _Speed = 0f;
                    if (!_is_attached)
                        _is_active = false;
                }
            }
        }
    }
}
