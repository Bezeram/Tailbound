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
    public ScriptablePlayer PlayerSettings;

    [Header("Info")]
    public bool IsSwinging = false;
    public Vector2 InputDirection = Vector2.zero;
    public float AttachScore = float.MinValue;
    public GameObject AttacherObject = null;

    public Vector2 _JumpDirection;
    public float _Amplitude;
    public float _SpeedInherited;
    public Vector2 _JumpForce;

    private ZiplineActivator _ZiplineActivator;
    private SpringJoint2D _TailJoint;
    private Vector2 _TailAttachPoint;

    // Update is called once per frame
    void Update()
    {
        GetInputDirection();
        UpdateAttachPoint();

        if (AttacherObject != null)
            HandleSwinging();

        // Player must be in the air to attach
        if (Input.GetKeyDown(PlayerSettings.AttachKey) && !PlayerControllerScript._grounded)
            HandleTailUse();

        if (Input.GetKeyUp(PlayerSettings.AttachKey) && AttacherObject != null)
            HandleTailRelease();
    }

    void UpdateAttachPoint()
    {
        if (AttacherObject == null)
            return;

        Vector3 attacherPosition = AttacherObject.transform.position;
        _TailJoint.connectedAnchor = new(attacherPosition.x, attacherPosition.y);
        _TailAttachPoint = attacherPosition;
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

    void HandleTailUse()
    {
        // Get swing direction
        Vector2 swingDirection = InputDirection;

        // Local function for calculating the best collider score
        (float, Vector2) CalculateColliderScore(Collider2D collider)
        {
            Vector2 center = (Vector2)collider.bounds.center;
            float score = 0;
            // 2 different scoring mechanisms
            if (swingDirection == Vector2.zero)
            {
                // If no preference direction was chosen, pick the closest attachable.
                // Use the closest point in reference to the player.
                // Return negative to prefer the smallest distance,
                // which with a negative sign becomes the biggest score.
                score = -Vector2.Distance(TailOrigin.position, collider.ClosestPoint(TailOrigin.position));
                return (score, center);
            }

            // Use the dot product to determine "closest arrow" to swing direction.
            Vector2 direction = center - (Vector2)TailOrigin.position;
            score = Vector2.Dot(direction.normalized, swingDirection);
            return (score, center);
        }

        // Cast for objects on attachable layer in the maximum range
        Collider2D[] colliders = Physics2D.OverlapCircleAll
            (TailOrigin.position, PlayerSettings.MaxTailLength, Attachable);
        if (colliders.Length == 0)
            return;

        /// Choose the best collider
        // Get the center of the colliders and draw a vector to the
        // center of all colliders detected.
        // Use the CalculateColliderScore() function for the score.
        // Best match has highest score.
        Vector2 bestColliderPosition = Vector2.zero;
        Collider2D bestCollider = null;
        float bestColliderScore = float.MinValue;
        foreach (Collider2D collider in colliders)
        {
            (float score, Vector2 center) = CalculateColliderScore(collider);

            if (score > bestColliderScore)
            {
                bestColliderScore = score;
                bestColliderPosition = center;
                bestCollider = collider;
            }
        }
        // Info
        AttachScore = bestColliderScore;

        // Configure the tail joint
        _TailAttachPoint = bestColliderPosition;
        AttachTail(_TailAttachPoint, bestCollider);
        DrawTailLine();
        IsSwinging = true;
        RigidBody.linearDamping = PlayerSettings.LinearDamping;
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
        AttacherObject = new GameObject("Attacher");
        AttacherObject.transform.position = attachPoint;
        // Make it a child of the attachment object
        AttacherObject.transform.SetParent(attachmentObject.transform, true);

        // Add a spring joint and configure it
        _TailJoint = gameObject.AddComponent<SpringJoint2D>();
        _TailJoint.autoConfigureDistance = false;
        _TailJoint.autoConfigureConnectedAnchor = false;
        _TailJoint.connectedAnchor = attachPoint;

        float distanceFromPoint = Vector2.Distance(TailOrigin.position, attachPoint);
        _TailJoint.distance = distanceFromPoint;
        _TailJoint.enableCollision = true;

        // Adjust spring settings
        _TailJoint.frequency = PlayerSettings.frequency;
        _TailJoint.dampingRatio = PlayerSettings.dampingRatio;

        AttachToZipline(attachmentObject);
    }

    public Vector2 SwingDirection;

    void HandleSwinging()
    {
        Vector2 forceDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
        // Direction pointing from the attach point to the player
        Vector2 tailPivot = new(TailOrigin.position.x, TailOrigin.position.y);
        Vector2 swingDirection = (tailPivot - _TailAttachPoint).normalized;
        // Swing force decreases with how high the player is
        // in relation to the attachment point.
        // Directly below the attach point, the swing force is max.
        float naturalSwingForce = Mathf.Abs(Vector2.Dot(Vector2.down, swingDirection));
        Vector2 force = naturalSwingForce * PlayerSettings.BaseSwingForce * forceDirection;
        RigidBody.AddForce(force);

        SwingDirection = swingDirection;

        // Gravity
        RigidBody.AddForce(Vector2.down * PlayerSettings.GravityMultiplier);
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
        Vector2 releaseDirection = InputDirection;
        IsSwinging = false;
        RigidBody.linearDamping = 0f;

        // Reset line renderer
        ClearTailLine();
        // Reset the tail joint and attacher
        Destroy(_TailJoint);
        Destroy(AttacherObject);
        // Detach from zipline if it's there
        DetachFromZipline();

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
        // Add an upwards force inherited from speed.
        // Inherited speed is disabled if played did not press upwards.
        float speedInherited = Mathf.Abs(RigidBody.linearVelocityX) * PlayerSettings.JumpInheritanceFactor;
        if (InputDirection.y != 1)
            speedInherited = 0;

        Vector2 jumpForce = jumpDirection * amplitude + new Vector2(0f, speedInherited);
        RigidBody.linearVelocity += jumpForce;

        // Print info
        _JumpDirection = jumpDirection;
        _Amplitude = amplitude;
        _SpeedInherited = speedInherited;
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
        if (AttacherObject != null)
        {
            DrawTailLine();
        }
    }
}
