using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class Swing : MonoBehaviour
{
    [TitleGroup("References")]
    public LayerMask Attachable;
    public Rigidbody2D RigidBody;
    public LineRenderer LineRenderer;
    public Transform TailOrigin;
    public PlayerController PlayerController;
    public PlayerAbilitiesSettings PlayerAbilitiesSettings;

    [TitleGroup("Info")]
    [ReadOnly, ShowInInspector] public bool IsSwinging;
    [ReadOnly, ShowInInspector] private Vector2 _InputDirection = Vector2.zero;
    [ReadOnly, ShowInInspector] private float _AttachScore = float.MinValue;

    [ReadOnly, ShowInInspector] private Vector2 _JumpDirection;
    [ReadOnly, ShowInInspector] private float _Amplitude;
    [ReadOnly, ShowInInspector] private float _SpeedInherited;
    [ReadOnly, ShowInInspector] private Vector2 _JumpForce;
    [ReadOnly, ShowInInspector] private Vector2 _SwingDirection;

    private ZiplineActivator _ZiplineActivator;
    private SpringJoint2D _TailJoint;
    private Vector2 _TailAttachPoint;
    private GameObject _AttacherObject;

    void Update()
    {
        if (LevelManager.IsPaused)
            return;
        
        GetInputDirection();
        UpdateAttachPoint();

        if (IsSwinging)
            HandleSwinging();

        // Player must be in the air to attach
        bool inAir = !PlayerController.IsGrounded && !PlayerController.IsClimbing;
        if (Input.GetKeyDown(PlayerAbilitiesSettings.AttachKey) && inAir)
            HandleTailUse();

        if (Input.GetKeyUp(PlayerAbilitiesSettings.AttachKey) && IsSwinging)
            HandleTailRelease();
        
        // If the player somehow stopped pressing the attachment key without triggering
        // the release key event (ex: pause menu), automatically disable swinging.
        if (!Input.GetKey(PlayerAbilitiesSettings.AttachKey) && IsSwinging)
            HandleTailRelease();
    }

    void UpdateAttachPoint()
    {
        if (!IsSwinging)
            return;

        _TailAttachPoint = _AttacherObject.transform.position;
        _TailJoint.connectedAnchor = _TailAttachPoint;
    }

    void GetInputDirection()
    {
        _InputDirection = Vector2.zero;
        if (Input.GetKey(PlayerAbilitiesSettings.LeftKey))
            _InputDirection.x = -1;
        if (Input.GetKey(PlayerAbilitiesSettings.RightKey))
            _InputDirection.x = 1;
        if (Input.GetKey(PlayerAbilitiesSettings.DownKey))
            _InputDirection.y = -1;
        if (Input.GetKey(PlayerAbilitiesSettings.UpKey))
            _InputDirection.y = 1;
    }

    void HandleTailUse()
    {
        // Get swing direction
        Vector2 swingDirection = _InputDirection;

        // Cast for objects on attachable layer in the maximum range
        var colliders = Physics2D.OverlapCircleAll
            (TailOrigin.position, PlayerAbilitiesSettings.MaxTailLength, Attachable);
        if (colliders.Length == 0)
            return;

        // Choose the best collider
        // Get the center of the colliders and draw a vector to the
        // center of all colliders detected.
        // Use the CalculateColliderScore() function for the score.
        // Best match has the highest score.
        Vector2 bestColliderPosition = Vector2.zero;
        Collider2D bestCollider = null;
        float bestColliderScore = float.MinValue;
        foreach (Collider2D col in colliders)
        {
            (float score, Vector2 center) = CalculateColliderScore(col);

            if (score > bestColliderScore)
            {
                bestColliderScore = score;
                bestColliderPosition = center;
                bestCollider = col;
            }
        }
        // Info
        _AttachScore = bestColliderScore;

        // Configure the tail joint
        _TailAttachPoint = bestColliderPosition;
        AttachTail(_TailAttachPoint, bestCollider);
        DrawTailLine();
        IsSwinging = true;
        RigidBody.linearDamping = PlayerAbilitiesSettings.LinearDamping;
        return;

        // Local function for calculating the best collider score
        (float, Vector2) CalculateColliderScore(Collider2D collision)
        {
            Vector2 center = collision.bounds.center;
            float score;
            // 2 different scoring mechanisms
            if (swingDirection == Vector2.zero)
            {
                // If no preference direction was chosen, pick the closest attachable.
                // Use the closest point in reference to the player.
                // Return negative to prefer the smallest distance,
                // which with a negative sign becomes the biggest score.
                score = -Vector2.Distance(TailOrigin.position, collision.ClosestPoint(TailOrigin.position));
                return (score, center);
            }

            // Use the dot product to determine "closest arrow" to swing direction.
            Vector2 direction = center - (Vector2)TailOrigin.position;
            score = Vector2.Dot(direction.normalized, swingDirection);
            return (score, center);
        }
    }

    void AttachToZipline(GameObject attachmentObject)
    {
        // Invoke zipline activator script if it's there
        bool isZiplineActivator = attachmentObject.TryGetComponent(out _ZiplineActivator);
        if (isZiplineActivator)
        {
            _ZiplineActivator.SendActivation();
        }
    }

    void AttachTail(Vector2 attachPoint, Collider2D attacherCollider)
    {
        GameObject attachmentObject = attacherCollider.gameObject;

        // Make a new Attacher game object
        _AttacherObject = new GameObject("Attacher");
        _AttacherObject.transform.position = attachPoint;
        // Make it a child of the attachment object
        _AttacherObject.transform.SetParent(attachmentObject.transform, true);

        // Add a spring joint and configure it
        _TailJoint = gameObject.AddComponent<SpringJoint2D>();
        _TailJoint.autoConfigureDistance = false;
        _TailJoint.autoConfigureConnectedAnchor = false;
        _TailJoint.connectedAnchor = attachPoint;

        float distanceFromPoint = Vector2.Distance(TailOrigin.position, attachPoint);
        _TailJoint.distance = distanceFromPoint;
        _TailJoint.enableCollision = true;

        // Adjust spring settings
        _TailJoint.frequency = PlayerAbilitiesSettings.Frequency;
        _TailJoint.dampingRatio = PlayerAbilitiesSettings.DampingRatio;

        AttachToZipline(attachmentObject);
    }


    void HandleSwinging()
    {
        Vector2 forceDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
        // Direction pointing from the attachment point to the player
        Vector2 tailPivot = new(TailOrigin.position.x, TailOrigin.position.y);
        Vector2 swingDirection = (tailPivot - _TailAttachPoint).normalized;
        // Swing force decreases with how high the player is
        // in relation to the attachment point.
        // Directly below the attachment point, the swing force is max.
        float naturalSwingForce = Mathf.Abs(Vector2.Dot(Vector2.down, swingDirection));
        Vector2 force = naturalSwingForce * PlayerAbilitiesSettings.BaseSwingForce * forceDirection;
        RigidBody.AddForce(force);

        _SwingDirection = swingDirection;

        // Gravity
        RigidBody.AddForce(Vector2.down * PlayerAbilitiesSettings.GravityMultiplier);
    }

    void DetachFromZipline()
    {
        if (_ZiplineActivator != null)
        {
            _ZiplineActivator.SendDeactivation();
            // Do NOT use Destroy() because that would destroy
            // the zipline activator component.
            _ZiplineActivator = null;
        }
    }

    void HandleTailRelease()
    {
        Vector2 releaseDirection = _InputDirection;
        IsSwinging = false;
        RigidBody.linearDamping = 0f;

        // Reset line renderer
        ClearTailLine();
        // Reset the tail joint and attacher
        Destroy(_TailJoint);
        Destroy(_AttacherObject);
        // Detach from zipline if it's there
        DetachFromZipline();

        // Jump-boost
        ApplyReleaseJump(releaseDirection);
        // Make sure the normal movement script inherits the velocity left over
        // from this script.
        PlayerController.InheritVelocity(RigidBody.linearVelocity);
    }

    void ApplyReleaseJump(Vector2 releaseDirection)
    {
        // Apply jump boost by scaling the direction the player released the tail.
        Vector2 jumpForce = releaseDirection * PlayerAbilitiesSettings.JumpScalar;
        RigidBody.linearVelocity += jumpForce;

        // Print info
        _JumpForce = jumpForce;
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
        if (IsSwinging)
            DrawTailLine();
    }
}
