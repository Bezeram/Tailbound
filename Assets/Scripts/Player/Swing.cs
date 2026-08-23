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

        // Safety net if the release key event was somehow missed.
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
        Vector2 swingDirection = _InputDirection;

        // Cast for attachable objects in range.
        var colliders = Physics2D.OverlapCircleAll
            (TailOrigin.position, PlayerAbilitiesSettings.MaxTailLength, Attachable);
        if (colliders.Length == 0)
            return;

        // Pick the collider with the highest CalculateColliderScore.
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
        _AttachScore = bestColliderScore;

        _TailAttachPoint = bestColliderPosition;
        AttachTail(_TailAttachPoint, bestCollider);
        DrawTailLine();
        IsSwinging = true;
        RigidBody.linearDamping = PlayerAbilitiesSettings.LinearDamping;
        return;

        (float, Vector2) CalculateColliderScore(Collider2D collision)
        {
            Vector2 center = collision.bounds.center;
            float score;
            if (swingDirection == Vector2.zero)
            {
                // No preference direction - pick the closest attachable
                // (negated distance = higher score).
                score = -Vector2.Distance(TailOrigin.position, collision.ClosestPoint(TailOrigin.position));
                return (score, center);
            }

            // Dot product picks the "closest arrow" to swing direction.
            Vector2 direction = center - (Vector2)TailOrigin.position;
            score = Vector2.Dot(direction.normalized, swingDirection);
            return (score, center);
        }
    }

    void AttachToZipline(GameObject attachmentObject)
    {
        // Notify the zipline activator, if any.
        bool isZiplineActivator = attachmentObject.TryGetComponent(out _ZiplineActivator);
        if (isZiplineActivator)
        {
            _ZiplineActivator.SendActivation();
        }
    }

    void AttachTail(Vector2 attachPoint, Collider2D attacherCollider)
    {
        GameObject attachmentObject = attacherCollider.gameObject;

        _AttacherObject = new GameObject("Attacher");
        _AttacherObject.transform.position = attachPoint;
        _AttacherObject.transform.SetParent(attachmentObject.transform, true);

        _TailJoint = gameObject.AddComponent<SpringJoint2D>();
        _TailJoint.autoConfigureDistance = false;
        _TailJoint.autoConfigureConnectedAnchor = false;
        _TailJoint.connectedAnchor = attachPoint;

        float distanceFromPoint = Vector2.Distance(TailOrigin.position, attachPoint);
        _TailJoint.distance = distanceFromPoint;
        _TailJoint.enableCollision = true;

        _TailJoint.frequency = PlayerAbilitiesSettings.Frequency;
        _TailJoint.dampingRatio = PlayerAbilitiesSettings.DampingRatio;

        AttachToZipline(attachmentObject);
    }


    void HandleSwinging()
    {
        Vector2 forceDirection = new Vector2(Input.GetAxis("Horizontal"), 0);
        // Attachment point -> player.
        Vector2 tailPivot = new(TailOrigin.position.x, TailOrigin.position.y);
        Vector2 swingDirection = (tailPivot - _TailAttachPoint).normalized;
        // Swing force peaks directly below the attachment point, decreasing with height.
        float naturalSwingForce = Mathf.Abs(Vector2.Dot(Vector2.down, swingDirection));
        Vector2 force = naturalSwingForce * PlayerAbilitiesSettings.BaseSwingForce * forceDirection;
        RigidBody.AddForce(force);

        _SwingDirection = swingDirection;

        RigidBody.AddForce(Vector2.down * PlayerAbilitiesSettings.GravityMultiplier);
    }

    void DetachFromZipline()
    {
        if (_ZiplineActivator != null)
        {
            _ZiplineActivator.SendDeactivation();
            // Don't Destroy() this - it'd destroy the zipline activator component too.
            _ZiplineActivator = null;
        }
    }

    void HandleTailRelease()
    {
        Vector2 releaseDirection = _InputDirection;
        IsSwinging = false;
        RigidBody.linearDamping = 0f;

        ClearTailLine();
        Destroy(_TailJoint);
        Destroy(_AttacherObject);
        DetachFromZipline();

        ApplyReleaseJump(releaseDirection);
        // Hand off leftover velocity to the normal movement script.
        PlayerController.InheritVelocity(RigidBody.linearVelocity);
    }

    void ApplyReleaseJump(Vector2 releaseDirection)
    {
        // Scale release direction into a jump boost.
        Vector2 jumpForce = releaseDirection * PlayerAbilitiesSettings.JumpScalar;
        RigidBody.linearVelocity += jumpForce;

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
