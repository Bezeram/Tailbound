using NUnit.Framework.Constraints;
using TarodevController;
using UnityEngine;

public class Swing : MonoBehaviour
{
    [Header("References")]
    public LayerMask Attachable;
    public Rigidbody2D RigidBody;
    public LineRenderer LineRenderer;
    public Transform TailOrigin;
    public float GravityMultipler = 1;

    [Header("Input")]
    public float MaxTailLength = 15f;
    public float MinTailLength = 2f;
    public float SwingForce = 1f;
    public float NormalJumpForce = 7f;

    [Header("Info")]
    public bool IsSwinging = false;
    public Vector2 SwingDirection = Vector2.zero;

    private DistanceJoint2D _TailJoint;
    private Vector2 _TailAttachPoint;
    private KeyCode _SwingKey = KeyCode.X;

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
            HandleWebShooting();
        if (Input.GetKeyUp(_SwingKey) && _TailJoint != null)
            HandleWebRelease();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (IsSwinging)
        {
            Destroy(_TailJoint);
            ClearWebLine();
            IsSwinging = false;
            RigidBody.linearDamping = 0f;
        }
    }

    void HandleWebShooting()
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
            RaycastHit2D hit = Physics2D.Raycast(TailOrigin.position, SwingDirection.normalized, MaxTailLength, Attachable);
            if (hit.collider != null)
            {
                float distanceToTarget = Vector2.Distance(TailOrigin.position, hit.point);

                if (distanceToTarget < MinTailLength)
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
        _TailJoint = gameObject.AddComponent<DistanceJoint2D>();
        _TailJoint.connectedAnchor = attachPoint;
        _TailJoint.autoConfigureDistance = false;
        _TailJoint.distance = Vector2.Distance(TailOrigin.position, attachPoint);
        _TailJoint.enableCollision = true;
    }

    void HandleSwinging()
    {
        Vector2 forceDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
        RigidBody.AddForce(forceDirection * SwingForce);

        // Gravity
        RigidBody.AddForce(Vector2.down * GravityMultipler);
    }

    void HandleWebRelease()
    {
        Destroy(_TailJoint);
        ClearWebLine();
        IsSwinging = false;
        RigidBody.linearDamping = 0f;
        ApplyReleaseJump();
    }

    void ApplyReleaseJump()
    {
        Vector2 jumpForce = new Vector2(RigidBody.linearVelocityX, NormalJumpForce);
        RigidBody.AddForce(jumpForce, ForceMode2D.Impulse);
    }

    void DrawWebLine()
    {
        LineRenderer.positionCount = 2;
        LineRenderer.SetPosition(0, TailOrigin.position);
        LineRenderer.SetPosition(1, _TailAttachPoint);
    }

    void ClearWebLine()
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
