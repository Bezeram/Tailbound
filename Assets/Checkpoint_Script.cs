using UnityEngine;

public class Checkpoint_Script : MonoBehaviour
{
    public static Vector3 current_checkpoint_position;

    private void OnTriggerEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            current_checkpoint_position = transform.position;
        }
    }
}
