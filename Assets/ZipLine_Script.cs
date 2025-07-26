using UnityEngine;

public class ZipLine_Script : MonoBehaviour
{
    public GameObject start_point;
    public GameObject end_point;
    public GameObject atachment_point;
    public float speed;
    private bool _is_attached;
    private bool _is_active;
    private bool _forward;
    private Vector3 direction;

    public void Attach()
    {
        _is_attached = true;
        _is_active = true;
    }

    public void Detach()
    {
        _is_attached = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _is_active = true;
        _is_attached = false;
        _forward = true;
        atachment_point.transform.position = start_point.transform.position;
        direction = end_point.transform.position - start_point.transform.position;
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
