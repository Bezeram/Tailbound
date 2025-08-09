using UnityEngine;
using TarodevController;

public class Spike_Script : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.name == "Player")
        {
            collision.gameObject.GetComponent<PlayerController>().Die();
        }
    }
}
