using UnityEngine;

public class Spring_Script : MonoBehaviour
{
    public bool is_active;
    public float force = 10000;
    private Rigidbody2D colided_body;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        is_active = false;    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
       if (collision.gameObject.name == "Player")
       {
            is_active = true;
            colided_body = collision.gameObject.GetComponent<Rigidbody2D>();
            colided_body.AddForceY(force);
            is_active = false;
       }
    }
}
