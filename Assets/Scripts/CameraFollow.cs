using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform Target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (Target == null)
        {
            Debug.Log("Assign the target to the game object.");
            return;
        }

        Vector2 newPosition = new(Target.position.x, transform.position.y);
        // Keep the same depth value (Z)
        transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
    }
}
