using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class Spring : MonoBehaviour
{
    public enum DirectionSpring
    { 
        Up = 0,
        Left = 1,
        Right = -1,
    }

    [TitleGroup("Input")]
    public DirectionSpring Direction = DirectionSpring.Up;
    [SerializeField] private EntitiesSettings Settings;

    private PlayerController playerController;
    private Animator Animator;
    private AudioSource AudioSource;

    Vector2 GetSpeed()
    {
        return Direction switch
        {
            DirectionSpring.Up => Settings.Spring.SpeedUp,
            DirectionSpring.Left => Vector2.Scale(Settings.Spring.SpeedSideways, new(-1, 1)),// Left x component is negative.
            DirectionSpring.Right => Settings.Spring.SpeedSideways,
            _ => Vector2.zero,
        };
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            playerController = collision.gameObject.GetComponent<PlayerController>();

            // Choose speed depending on spring direction
            Vector2 propelSpeed = GetSpeed();
            playerController.ExecuteSpringJump(propelSpeed);

            // Trigger animation
            Animator.SetTrigger("Extend");
            AudioSource.Play();
        }
    }

    void OnValidate()
    {
        Animator = GetComponent<Animator>();
        AudioSource = transform.GetChild(0).GetComponent<AudioSource>();

        // Rotate depending on the spring direction.
        // Use the enum's values.
        float angle = (int)Direction * 90;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
