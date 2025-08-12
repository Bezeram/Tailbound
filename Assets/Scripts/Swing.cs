using TarodevController;
using UnityEditor;
using UnityEngine;

public class Swing : MonoBehaviour
{
    [Header("References")]
    public LayerMask Attachable;
    public Rigidbody2D RigidBody;
    public LineRenderer LineRenderer;
    public Transform TailOrigin;
    public PlayerController PlayerControllerScript;
    public ScriptablePlayer PlayerSettings;

    [Header("Info")]
    public bool IsSwinging = false;
    public Vector2 InputDirection = Vector2.zero;
    public float AttachScore = float.MinValue;

    private SpringJoint2D _TailJoint;
    private Vector2 _TailAttachPoint;

    // Update is called once per frame
    void Update()
    {
        GetInputDirection();

        if (_TailJoint != null)
            HandleSwinging();

        if (Input.GetKeyDown(PlayerSettings.SwingKey))
            HandleTailUse();

        if (Input.GetKeyUp(PlayerSettings.SwingKey) && _TailJoint != null)
            HandleTailRelease();
    }

    Vector2 GetInputDirection()
    {
        InputDirection = Vector2.zero;
        if (Input.GetKey(PlayerSettings.LeftKey))
            InputDirection.x = -1;
        if (Input.GetKey(PlayerSettings.RightKey))
            InputDirection.x = 1;
        if (Input.GetKey(PlayerSettings.DownKey))
            InputDirection.y = -1;
        if (Input.GetKey(PlayerSettings.UpKey))
            InputDirection.y = 1;
        return InputDirection.normalized;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsSwinging)
        {
            Destroy(_TailJoint);
            ClearTailLine();
            IsSwinging = false;
            RigidBody.linearDamping = 0f;
        }
    }

    void HandleTailUse()
    {
        // Get swing direction
        Vector2 swingDirection = InputDirection;

        // Cast for objects on attachable layer in the maximum range
        Collider2D[] colliders = Physics2D.OverlapCircleAll
            (TailOrigin.position, PlayerSettings.MaxTailLength, Attachable);

        if (colliders.Length == 0)
            return;

        // Get the center of the colliders and
        // draw a vector to the center of all colliders detected.
        // Use the dot product to find the best match collider with the direction.
        // Best match has the highest dot product value.
        Vector2 bestColliderPosition = Vector2.zero;
        float bestColliderScore = -2;
        foreach (Collider2D collider in colliders)
        {
            Vector2 center = (Vector2)collider.bounds.center;
            Vector2 direction = center - (Vector2)TailOrigin.position;
            float score = Vector2.Dot(direction.normalized, swingDirection);

            if (score > bestColliderScore)
            {
                bestColliderScore = score;
                bestColliderPosition = center;
            }
        }

        AttachScore = bestColliderScore;

        // Configure the tail joint
        _TailAttachPoint = bestColliderPosition;
        AttachWeb(_TailAttachPoint);
        DrawTailLine();
        IsSwinging = true;
        RigidBody.linearDamping = 0.5f;
    }

    void AttachWeb(Vector2 attachPoint)
    {
        _TailJoint = gameObject.AddComponent<SpringJoint2D>();

        // Configure spring joint
        _TailJoint.autoConfigureDistance = false;
        _TailJoint.autoConfigureConnectedAnchor = false;
        _TailJoint.connectedAnchor = attachPoint;

        float distanceFromPoint = Vector2.Distance(TailOrigin.position, attachPoint);
        _TailJoint.distance = distanceFromPoint;
        _TailJoint.enableCollision = true;

        // Adjust spring settings
        _TailJoint.frequency = PlayerSettings.frequency;
        _TailJoint.dampingRatio = PlayerSettings.dampingRatio;
    }

    void HandleSwinging()
    {
        Vector2 forceDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
        RigidBody.AddForce(forceDirection * PlayerSettings.SwingForce);

        // Gravity
        RigidBody.AddForce(Vector2.down * PlayerSettings.GravityMultiplier);
    }

    void HandleTailRelease()
    {
        Vector2 releaseDirection = InputDirection;

        // Reset the tail joint
        Destroy(_TailJoint);
        ClearTailLine();
        IsSwinging = false;
        RigidBody.linearDamping = 0f;
        // Jump boost
        ApplyReleaseJump(releaseDirection);

        // Make sure the normal movement script inherits the velocity left over
        // from this script.
        PlayerControllerScript.InheritVelocity(RigidBody.linearVelocity);
    }

    void ApplyReleaseJump(Vector2 releaseDirection)
    {
        // Apply jump boost by scaling the direction the player released the tail.
        Vector2 jumpDirection = RigidBody.linearVelocity.normalized;
        float amplitude = Vector2.Dot(jumpDirection, releaseDirection) * PlayerSettings.JumpScalar;

        Vector2 jumpForce = jumpDirection * amplitude;
        RigidBody.AddForce(jumpForce, ForceMode2D.Impulse);
    }

    void DrawTailLine()
    {
        LineRenderer.positionCount = 2;
        LineRenderer.SetPosition(0, TailOrigin.position);
        LineRenderer.SetPosition(1, _TailAttachPoint);
    }

    void ClearTailLine()
    {
        LineRenderer.positionCount = 0;
    }

    void LateUpdate()
    {
        if (_TailJoint != null)
        {
            DrawTailLine();
        }
    }
}
