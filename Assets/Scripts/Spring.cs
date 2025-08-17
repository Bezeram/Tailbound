using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class Spring : MonoBehaviour
{
    public enum DirectionSpring
    { 
        Upwards,
        Left,
        Right,
    }

    [TitleGroup("Input")]
    public Vector2 Speed = new(50f, 30f);
    public DirectionSpring Direction = DirectionSpring.Upwards;

    private PlayerController playerController;
    private Animator Animator;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerController = collision.gameObject.GetComponent<PlayerController>();
            playerController.SpringJump(Speed);
            // Trigger animation
            Animator.SetTrigger("Extend");
        }
    }

    void OnValidate()
    {
        Animator = GetComponent<Animator>();
    }
}
