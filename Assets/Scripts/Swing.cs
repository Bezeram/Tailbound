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
    public Vector2 SwingDirection = Vector2.zero;
    public Vector2 ReleaseDirection = Vector2.zero;

    private SpringJoint2D _TailJoint;
    private Vector2 _TailAttachPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (_TailJoint != null)
            HandleSwinging();
        if (Input.GetKeyDown(PlayerSettings.SwingKey))
            HandleTailUse();
        if (Input.GetKeyUp(PlayerSettings.SwingKey) && _TailJoint != null)
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
            RaycastHit2D hit = Physics2D.Raycast(TailOrigin.position, SwingDirection.normalized, 
                PlayerSettings.MaxTailLength, Attachable);
            if (hit.collider != null)
            {
                float distanceToTarget = Vector2.Distance(TailOrigin.position, hit.point);

                if (distanceToTarget < PlayerSettings.MinTailLength)
                    return;

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
        // Find swing direction
        ReleaseDirection = Vector2.zero;
        if (Input.GetKey(KeyCode.LeftArrow))
            ReleaseDirection.x = -1;
        if (Input.GetKey(KeyCode.RightArrow))
            ReleaseDirection.x = 1;
        if (Input.GetKey(KeyCode.DownArrow))
            ReleaseDirection.y = -1;
        if (Input.GetKey(KeyCode.UpArrow))
            ReleaseDirection.y = 1;

        Destroy(_TailJoint);
        ClearTailLine();
        IsSwinging = false;
        RigidBody.linearDamping = 0f;
        ApplyReleaseJump(ReleaseDirection);

        // Make sure the normal movement script inherits the velocity left over
        // from this script.
        PlayerControllerScript.InheritVelocity(RigidBody.linearVelocity);
    }

    void ApplyReleaseJump(Vector2 releaseDirection)
    {
        Vector2 jumpDirection = RigidBody.linearVelocity.normalized;
        float amplitude = Vector2.Dot(jumpDirection, releaseDirection) * PlayerSettings.JumpScalar;
        Vector2 jumpForce = jumpDirection * amplitude;
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
