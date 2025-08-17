using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class Spring : MonoBehaviour
{
    public enum DirectionSpring
    { 
        Up,
        Left,
        Right,
    }

    [TitleGroup("Input")]
    public Vector2 SpeedUpwards = new(0f, 40f);
    public Vector2 SpeedSideways = new(40f, 20f);
    public Vector2 Speed;
    public DirectionSpring Direction = DirectionSpring.Up;

    private PlayerController playerController;
    private Animator Animator;
    private AudioSource AudioSource;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerController = collision.gameObject.GetComponent<PlayerController>();
            playerController.SpringJump(Speed);
            // Trigger animation
            Animator.SetTrigger("Extend");
            AudioSource.Play();
        }
    }

    void OnValidate()
    {
        Animator = GetComponent<Animator>();
        AudioSource = GetComponent<AudioSource>();

        // Choose a speed value depending on the spring direction
        switch (Direction)
        {
        case DirectionSpring.Left:
                break;
        case DirectionSpring.Up:
                break;
        case DirectionSpring.Right:
                break;
        }
    }
}
