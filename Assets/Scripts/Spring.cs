using TarodevController;
using UnityEngine;

public class Spring : MonoBehaviour
{
    [Header("Input")]
    public float Speed = 50f;

    private PlayerController playerController;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerController = collision.gameObject.GetComponent<PlayerController>();
            playerController.Propel(new(0f, Speed));
        }
    }
}
