using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public static Vector3 currentCheckpointPosition;
    public static int currentScene;

    private void Awake()
    {
        if (currentCheckpointPosition == new Vector3(0, 0, 0))
        {
            currentCheckpointPosition = GameObject.Find("Start_Point").transform.position;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            currentCheckpointPosition = transform.position;
        }
    }
}
