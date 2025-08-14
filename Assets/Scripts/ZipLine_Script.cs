using UnityEngine;

public class ZipLine_Script : MonoBehaviour
{
    [Header("References")]
    public GameObject start_point;
    public GameObject end_point;
    public GameObject atachment_point;
    public GameObject connecting_belt;
    public float speed;
    public bool set_position;

    [Header("Info")]
    public bool _is_attached;
    public bool _is_active;
    private Vector3 direction;

    private bool _forward;

    public void Attach()
    {
        _is_attached = true;
        _is_active = true;
    }

    public void Detach()
    {
        _is_attached = false;
    }

    private void OnValidate()
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
        _forward = true;
        atachment_point.transform.position = start_point.transform.position;
        direction = end_point.transform.position - start_point.transform.position;
        direction.Normalize();
    }

    // Update is called once per frame
    void Update()
    {
        if (_is_active)
        {
            if (_forward)
            {
                atachment_point.transform.position += direction * Time.deltaTime * speed;
                if (Utilities_Script.Compare_close_vectors(atachment_point.transform.position, end_point.transform.position))
                    _forward = false;
            } 
            else
            {
                atachment_point.transform.position -= direction * Time.deltaTime * speed * 0.3f;
                if (Utilities_Script.Compare_close_vectors(atachment_point.transform.position, start_point.transform.position))
                {
                    _forward = true;
                    if (!_is_attached)
                        _is_active = false;
                }
            }
        }
    }
}
