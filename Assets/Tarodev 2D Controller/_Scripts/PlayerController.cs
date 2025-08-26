using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace TarodevController
{
    /// <summary>
    /// Hey!
    /// Tarodev here. I built this controller as there was a severe lack of quality & free 2D controllers out there.
    /// I have a premium version on Patreon, which has every feature you'd expect from a polished controller. Link: https://www.patreon.com/tarodev
    /// You can play and compete for best times here: https://tarodev.itch.io/extended-ultimate-2d-controller
    /// If you hve any questions or would like to brag about your score, come to discord: https://discord.gg/tarodev
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [TitleGroup("References")]
        public PlayerMovementSettings _stats;
        [FormerlySerializedAs("PlayerSettings")] public PlayerAbilitiesSettings playerAbilitiesSettingsSettings;
        public Swing SwingScript;
        public GameObject LevelLoader;

        [TitleGroup("Info")]
        [ReadOnly, ShowInInspector, SerializeField] private Vector2 _frameVelocity;
        [ReadOnly, ShowInInspector, SerializeField] private float _Stamina;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsClimbingLeft = false;
        [ReadOnly, ShowInInspector, SerializeField] private bool IsAdjacentToWallLeft = false;
        [ReadOnly, ShowInInspector, SerializeField] private bool IsAdjacentToWallRight = false;
        [ReadOnly, ShowInInspector, SerializeField] private bool _FacingLeft = false;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsClimbing = false;

        public bool IsClimbing => _IsClimbing;

        private Rigidbody2D _RigidBody;
        private BoxCollider2D _col;
        private BoxCollider2D _ClimbingCollider;
        private FrameInput _frameInput;
        private bool _cachedQueryStartInColliders;
        private bool is_dead;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Climbed;
        public event Action Jumped;

        #endregion

        private float _time;

        void Awake()
        {
            _RigidBody = GetComponent<Rigidbody2D>();
            _col = GetComponent<BoxCollider2D>();
            _RigidBody.transform.position = Checkpoint.current_checkpoint_position; 
            _ClimbingCollider = transform.Find("ClimbHitbox").GetComponent<BoxCollider2D>();
            LevelLoader = GameObject.FindGameObjectWithTag("LevelLoader");
            is_dead = false;

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        void Update()
        {
            if (SwingScript.IsSwinging)
                return;

            if (is_dead)
                return;

            _time += Time.deltaTime;
            GatherInput();
        }

		void FixedUpdate()
		{
            if (SwingScript.IsSwinging)
                return;

            if (is_dead)
                return;

            CheckCollisions();

            HandleClimbing();
			HandleJump();
			HandleDirection();
			HandleGravity();

			ApplyMovement();
		}

        public void InheritVelocity(Vector2 velocity)
        {
            _frameVelocity = velocity;
        }

		void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(playerAbilitiesSettingsSettings.JumpKey),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(playerAbilitiesSettingsSettings.JumpKey),
                ClimbHeld = Input.GetKey(playerAbilitiesSettingsSettings.ClimbKey),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        #region Climbing
        
        // Check if the player can climb.
        bool CanClimb(bool adjacentWallLeft, bool adjacentWallRight)
        {
            bool hasStamina = _Stamina > 0;
            // Prevent climbing while pressing down to avoid funny behaviour.
            bool correctInputs = _frameInput.ClimbHeld && FrameInput.y >= 0;
            // Cannot climb on a wall if the player is moving upwards too fast.
            bool correctVelocity = _frameVelocity.y < playerAbilitiesSettingsSettings.SpeedCapToClimb;
            // Check if player is facing towards wall.
            bool facingWall = (_FacingLeft && adjacentWallLeft) || (!_FacingLeft && adjacentWallRight);
            
            return hasStamina && correctInputs && correctVelocity && facingWall;
        }

        private const float _FractionalDistanceFromWall = 0.456f;
        
        void TriggerClimb()
        {
            // Reset speed
            _frameVelocity = Vector2.zero;
            // Stick player close to wall
            float x = transform.position.x;
            int direction = _FacingLeft ? 1 : -1;
            float roundedX = _FacingLeft ? Mathf.Floor(x) : Mathf.Ceil(x);
            float newX = roundedX + direction * _FractionalDistanceFromWall;
            transform.position = new(newX, transform.position.y, transform.position.z);

            // Leave the ground
            if (_grounded)
            {
                // To prevent the player from immediately touching the ground
                // in the climb state, elevate the player a bit.
                transform.position += _stats.GrounderDistance * 1.2f * Vector3.up;
            }
            _grounded = false;

            _IsClimbingLeft = _FacingLeft;
            _IsClimbing = true;
            Climbed?.Invoke();
        }

        void HandleClimbing()
        {
            if (_IsClimbing && !_frameInput.ClimbHeld)
                _IsClimbing = false;

            if (!_IsClimbing)
                return;

            // Update stamina
            _Stamina -= playerAbilitiesSettingsSettings.StaminaConstantCost * Time.fixedDeltaTime;
            // Check for climbing
            if (_frameInput.Move.y != 0)
            {
                int direction = (_frameInput.Move.y > 0) ? 1 : -1;
                _frameVelocity.y = direction * playerAbilitiesSettingsSettings.ClimbSpeed;
                _Stamina -= playerAbilitiesSettingsSettings.StaminaClimbingCost * Time.fixedDeltaTime;
            }
            else
            {
                _frameVelocity.y = 0;
            }

            if (_Stamina <= 0)
                _IsClimbing = false;
        }

        #endregion

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        [ReadOnly] private bool _grounded;

        public bool IsGrounded => _grounded;

        void CheckCollisions()
        {
            // Use the dedicated collider.
            bool IsWallAdjacent(Vector2 direction)
            {
                return Physics2D.BoxCast(_ClimbingCollider.bounds.center, _ClimbingCollider.size, 0,
                    direction, playerAbilitiesSettingsSettings.AdjacentWallDistance, _stats.SolidLayer);
            }

            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            bool groundHit = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.down, _stats.GrounderDistance, _stats.SolidLayer);
            bool ceilingHit = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.up, _stats.GrounderDistance, _stats.SolidLayer);
            // Wall collision
            bool hitWallLeft = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.left, _stats.GrounderDistance, _stats.SolidLayer);
            bool hitWallRight = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.right, _stats.GrounderDistance, _stats.SolidLayer);
            // Check for adjacent wall (climbing)
            bool adjacentWallLeft = IsWallAdjacent(Vector2.left);
            bool adjacentWallRight = IsWallAdjacent(Vector2.right);

            // Info
            IsAdjacentToWallLeft = adjacentWallLeft;
            IsAdjacentToWallRight = adjacentWallRight;

            // Landed on the Ground
            if (!_grounded && groundHit)
            {
                // Climbing
                _IsClimbing = false;
                _Stamina = playerAbilitiesSettingsSettings.StaminaTotal;

                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            // Left the Ground
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _IsClimbing = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            // Check for wall collision
            if (hitWallLeft || hitWallRight)
            {
                // Apply deceleration
                if (!_IsClimbing)
                    _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, _stats.WallDeceleration * Time.fixedDeltaTime);
            }

            // Check for climbing
            if (adjacentWallLeft || adjacentWallRight)
            {
                if (!_IsClimbing)
                {
                    // Check if the player can climb.
                    // Must be holding towards the wall.
                    // Cannot climb on a wall if the player is moving upwards too fast.
                    if (CanClimb(adjacentWallLeft, adjacentWallRight))
                        TriggerClimb();
                }
            }
            else
            {
                if (_IsClimbing)
                {
                    // No walls adjacent => stop climbing
                    _IsClimbing = false;

                    // Apply small boost when reaching the top of a ledge.
                    if (FrameInput.y > 0 && _frameVelocity.y < playerAbilitiesSettingsSettings.SpeedCapLedgeJump)
                    {
                        Debug.Log("Applied ledge jump");
                        Vector2 direction = new(_FacingLeft ? -1 : 1, 1);
                        _frameVelocity = direction * playerAbilitiesSettingsSettings.LedgeBoost;
                    }
                }
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume = false;
        private bool _bufferedJumpUsable = false;
        private bool _endedJumpEarly = false;
        private bool _coyoteUsable = true;
        private float _timeJumpWasPressed = float.MinValue;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        bool IsFacingTowardWall()
        {
            return _FacingLeft == _IsClimbingLeft;
        }

        void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _RigidBody.linearVelocity.y > 0)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) 
                return;

            // A normal jump is made on the ground.
            // A climb jump happens only while climbing.
            // A wall jump happens a player is close enough to a wall and:
            // 1. Player is facing away from the wall
            // 2. Player is facing toward wall, but is not climbing
            if (_grounded || CanUseCoyote) 
                ExecuteJump();
            else if (_IsClimbing && IsFacingTowardWall())
                ExecuteClimbJump();
            else if (!_grounded && IsAdjacentToWallLeft || IsAdjacentToWallRight)
                ExecuteWallJump();

            _jumpToConsume = false;
        }

        void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            // After max speed, a jump boost is added proportional to sideways movement.
            _frameVelocity.y = _stats.JumpPower;
            if (Mathf.Abs(_frameVelocity.x) > _stats.WalkingSpeedCap)
            {
                // Steal speed from the horizontal component.
                float boost = _stats.JumpInheritanceFactor * _frameVelocity.x;
                _frameVelocity.y += boost;
                _frameVelocity.x -= boost;
            }

            Jumped?.Invoke();
        }

        void ExecuteWallJump()
        {
            _IsClimbing = false;
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            Vector2 direction = new(IsAdjacentToWallLeft ? 1 : -1, 1);
            _frameVelocity = direction * playerAbilitiesSettingsSettings.WallJumpPower;

            Jumped?.Invoke();
        }

        void ExecuteClimbJump()
        {
            _IsClimbing = false;
            _endedJumpEarly = true;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _Stamina -= playerAbilitiesSettingsSettings.ClimbJumpStaminaCost;
            _frameVelocity.y = playerAbilitiesSettingsSettings.ClimbJumpPower;

            Jumped?.Invoke();
        }

        // Called by Spring.cs
        public void ExecuteSpringJump(Vector2 speed)
        {
            _endedJumpEarly = true;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;

            _frameVelocity = speed;

            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal
        
        void HandleDirection()
        {
            if (FrameInput.x != 0)
                _FacingLeft = FrameInput.x < 0;

            if (_IsClimbing)
                return;

            // Constant deceleration
            float constantDeceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, constantDeceleration * Time.fixedDeltaTime);

            /// Apply Input movement
            if (FrameInput.x == 0)
            {
                // With the stick neutral, apply a smaller deceleration
                // than if you
                float neutralDeceleration = _stats.Acceleration / _stats.NeutralDecelerationFactor;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, neutralDeceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Apply acceleration
                _frameVelocity.x += FrameInput.x * _stats.Acceleration * Time.fixedDeltaTime;
            }

            /// Adjustment dependent on speed value
            // The walking speed is capped.
            // To get past it, another means of acceleration must be used.
            // Basically checking if we are within the speed cap + one acceleration update step.
            float errorMargin = _stats.Acceleration * Time.fixedDeltaTime * _stats.MaxSpeedErrorMargin;
            if (Mathf.Abs(_frameVelocity.x) > _stats.WalkingSpeedCap + errorMargin)
            {
                // High speed regime
                // Apply heavy deceleration
                // Constant deceleration
                float highspeedDeceleration = _grounded ? _stats.HighspeedGroundDeceleration : _stats.HighspeedAirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, highspeedDeceleration * Time.fixedDeltaTime);

                // If player hits a wall and holds in opposite direction stop all movement.
                if (_frameInput.Move.x * _frameVelocity.x <= 0 && Mathf.Abs(_RigidBody.linearVelocityX) < 0.01)
                {
                    _frameVelocity.x = 0;
                }
            }
            else
            {
                // Low speed regime
                // Apply walking speed cap.
                // If player hits a wall and holds in opposite direction stop all movement.
                //if (_frameInput.Move.x * _frameVelocity.x < 0)
                //    _frameVelocity.x = 0;

                _frameVelocity.x = Mathf.Clamp(_frameVelocity.x, -_stats.WalkingSpeedCap, _stats.WalkingSpeedCap);
            }
        }

        #endregion

        #region Gravity

        void HandleGravity()
        {
            if (_IsClimbing)
                return;

            if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0) 
                    inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        public void Die()
        {
            //play death animation
            is_dead = true;
            _frameVelocity.x = 0;
            _frameVelocity.y = 0;
            _RigidBody.linearVelocity = _frameVelocity;
            LevelLoader.GetComponent<LevelLoader>().Reload_level();
        }

        private void ApplyMovement() => _RigidBody.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public bool ClimbHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;

        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}