using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Checkpoint : MonoBehaviour
{
    public static Vector3 currentCheckpointPosition;
    public static int currentScene;
    public static List<Vector3> bananaPositions;
    public static int score;

    private void Awake()
    {
        if (currentCheckpointPosition == new Vector3(0, 0, 0))
        {
            currentCheckpointPosition = GameObject.Find("Start_Point").transform.position;
        }
        if (bananaPositions == null)
        {
            bananaPositions = new List<Vector3>();
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
