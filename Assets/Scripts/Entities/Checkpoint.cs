using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public static Vector3 current_checkpoint_position;

    private void Awake()
    {
        if (current_checkpoint_position == new Vector3(0, 0, 0))
        {
            current_checkpoint_position = GameObject.Find("Start_Point").transform.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            current_checkpoint_position = transform.position;
        }
    }
}
