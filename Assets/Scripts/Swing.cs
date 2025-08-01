using TarodevController;
using UnityEngine;

public class Swing : MonoBehaviour
{
    [Header("References")]
    public LayerMask Attachable;
    public Rigidbody2D RigidBody;
    public LineRenderer LineRenderer;
    public Transform TailOrigin;
    public PlayerController PlayerControllerScript;
    public ScriptableTail TailSettings;

    [Header("Info")]
    public bool IsSwinging = false;
    public Vector2 SwingDirection = Vector2.zero;

    private SpringJoint2D _TailJoint;
    private Vector2 _TailAttachPoint;
    private readonly KeyCode _SwingKey = KeyCode.X;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (_TailJoint != null)
            HandleSwinging();
        if (Input.GetKeyDown(_SwingKey))
            HandleTailUse();
        if (Input.GetKeyUp(_SwingKey) && _TailJoint != null)
            HandleTailRelease();
    }

    private void OnCollisionEnter2D(Collision2D collision)
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
        // Find swing direction
        SwingDirection = Vector2.zero;
        if (Input.GetKey(KeyCode.LeftArrow))
            SwingDirection.x = -1;
        if (Input.GetKey(KeyCode.RightArrow))
            SwingDirection.x = 1;
        if (Input.GetKey(KeyCode.DownArrow))
            SwingDirection.y = -1;
        if (Input.GetKey(KeyCode.UpArrow))
            SwingDirection.y = 1;

        if (SwingDirection != Vector2.zero)
        {
            RaycastHit2D hit = Physics2D.Raycast(TailOrigin.position, SwingDirection.normalized, TailSettings.MaxTailLength, Attachable);
            if (hit.collider != null)
            {
                float distanceToTarget = Vector2.Distance(TailOrigin.position, hit.point);

                if (distanceToTarget < TailSettings.MinTailLength)
                {
                    return;
                }

                _TailAttachPoint = hit.point;
                AttachWeb(_TailAttachPoint);
                DrawWebLine();
                IsSwinging = true;
                RigidBody.linearDamping = 0.5f;
            }
        }
    }

    void AttachWeb(Vector2 attachPoint)
    {
        _TailJoint = gameObject.AddComponent<SpringJoint2D>();

        // Configure spring joint
        _TailJoint.autoConfigureDistance = false;
        _TailJoint.connectedAnchor = attachPoint;

        float distanceFromPoint = Vector2.Distance(TailOrigin.position, attachPoint);
        _TailJoint.distance = distanceFromPoint;
        _TailJoint.enableCollision = true;

        // Adjust spring settings
        _TailJoint.frequency = TailSettings.frequency;
        _TailJoint.dampingRatio = TailSettings.dampingRatio;
    }

    void HandleSwinging()
    {
        Vector2 forceDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
        RigidBody.AddForce(forceDirection * TailSettings.SwingForce);

        // Gravity
        RigidBody.AddForce(Vector2.down * TailSettings.GravityMultiplier);
    }

    void HandleTailRelease()
    {
        Destroy(_TailJoint);
        ClearTailLine();
        IsSwinging = false;
        RigidBody.linearDamping = 0f;
        ApplyReleaseJump();

        // Make sure the normal movement script inherits the velocity left over
        // from this script.
        PlayerControllerScript.InheritVelocity(RigidBody.linearVelocity);
    }

    void ApplyReleaseJump()
    {
        Vector2 jumpForce = new Vector2(RigidBody.linearVelocityX, TailSettings.NormalJumpForce);
        RigidBody.AddForce(jumpForce, ForceMode2D.Impulse);
    }

    void DrawWebLine()
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
            DrawWebLine();
        }
    }
}
