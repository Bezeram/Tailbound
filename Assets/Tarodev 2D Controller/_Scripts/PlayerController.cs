using Sirenix.OdinInspector;
using System;
using Unity.VisualScripting;
using UnityEngine;

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
        public ScriptableStats _stats;
        public ScriptablePlayer PlayerSettings;
        public Swing SwingScript;
        public GameObject LevelLoader;

        [TitleGroup("Info")]
        [ReadOnly, ShowInInspector, SerializeField] private Vector2 _frameVelocity;
        [ReadOnly, ShowInInspector, SerializeField] private float _Stamina;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsClimbing = false;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsClimbingLeft = false;
        [ReadOnly, ShowInInspector, SerializeField] private bool AdjacentWallLeft = false;
        [ReadOnly, ShowInInspector, SerializeField] private bool AdjacentWallRight = false;
        [ReadOnly, ShowInInspector, SerializeField] private bool _FacingLeft = false;

        private Rigidbody2D _RigidBody;
        private BoxCollider2D _col;
        private FrameInput _frameInput;
        private bool _cachedQueryStartInColliders;

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
            LevelLoader = GameObject.FindGameObjectWithTag("LevelLoader");

            _cachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        void Update()
        {
            if (SwingScript.IsSwinging)
                return;

            _time += Time.deltaTime;
            GatherInput();
        }

		void FixedUpdate()
		{
            if (SwingScript.IsSwinging)
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
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(PlayerSettings.JumpKey),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(PlayerSettings.JumpKey),
                ClimbHeld = Input.GetKey(PlayerSettings.ClimbKey),
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
            // Prevent climbing while pressing down to avoid funny behaviour.
            if (!_frameInput.ClimbHeld || FrameInput.y < 0)
                return false;
            // Cannot climb on a wall if the player is moving upwards too fast.
            if (_frameVelocity.y > PlayerSettings.SpeedCapToClimb)
                return false;

            // Check if player is facing towards wall.
            return (_FacingLeft && adjacentWallLeft) || (!_FacingLeft && adjacentWallRight);
        }

        void TriggerClimb()
        {
            // Reset speed
            _frameVelocity = Vector2.zero;
            // Position player adjacent to wall
            float x = transform.position.x;
            float newX = Mathf.Floor(x) - 0.5f * Mathf.Sign(x);
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
            // If the player stops holding climb
            // after initiating it stop.
            if (_IsClimbing && !_frameInput.ClimbHeld)
                _IsClimbing = false;
            if (!_IsClimbing)
                return;

            _Stamina -= PlayerSettings.StaminaIdleCost * Time.fixedDeltaTime;
            // Check for climbing
            if (_frameInput.Move.y != 0)
            {
                int direction = (_frameInput.Move.y > 0) ? 1 : -1;
                _frameVelocity.y = direction * PlayerSettings.ClimbSpeed;
                _Stamina -= PlayerSettings.StaminaClimbingCost * Time.fixedDeltaTime;
            }
            else
                _frameVelocity.y = 0;
        }

        #endregion

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        [ReadOnly] public bool _grounded;

        void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            bool groundHit = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.down, _stats.GrounderDistance, _stats.SolidLayer);
            bool ceilingHit = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.up, _stats.GrounderDistance, _stats.SolidLayer);
            // Wall collision
            bool hitWallLeft = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.left, _stats.GrounderDistance, _stats.SolidLayer);
            bool hitWallRight = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.right, _stats.GrounderDistance, _stats.SolidLayer);
            // Check for adjacent wall (climbing)
            bool adjacentWallLeft = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.left, PlayerSettings.AdjacentWallDistance, _stats.SolidLayer);
            bool adjacentWallRight = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.right, PlayerSettings.AdjacentWallDistance, _stats.SolidLayer);

            // Info
            AdjacentWallLeft = adjacentWallLeft;
            AdjacentWallRight = adjacentWallRight;

            // Landed on the Ground
            if (!_grounded && groundHit)
            {
                // Climbing
                _IsClimbing = false;
                _Stamina = PlayerSettings.StaminaTotal;

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

            if (hitWallLeft || hitWallRight)
            {
                // Apply deceleration
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
                }
            }

            Physics2D.queriesStartInColliders = _cachedQueryStartInColliders;
        }

        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        bool IsFacingAwayFromWall()
        {
            int inputDirection;
            if (_frameInput.Move.x == 0)
                inputDirection = 0;
            else
                inputDirection = (int)Mathf.Sign(_frameInput.Move.x);

            return inputDirection != 0 && 
                (inputDirection == -1 && !_IsClimbingLeft) ||
                (inputDirection == 1 && _IsClimbingLeft);
        }

        void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _RigidBody.linearVelocity.y > 0)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) 
                return;

            if (_grounded || CanUseCoyote) 
                ExecuteJump();

            if (_IsClimbing && IsFacingAwayFromWall())
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
            if (Mathf.Abs(_frameVelocity.x) > _stats.MaxSpeed)
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

            _frameVelocity = PlayerSettings.WallJumpPower;

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

        private void HandleDirection()
        {
            if (FrameInput.x != 0)
                _FacingLeft = FrameInput.x < 0;

            if (_IsClimbing)
                return;

            // Constant deceleration
            var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);

            if (Mathf.Abs(_frameVelocity.x) > _stats.MaxSpeed)
            {
                // High speed regime
                // If player hits a wall and holds in opposite direction stop all movement.
                if (_frameInput.Move.x * _frameVelocity.x <= 0 && Mathf.Abs(_RigidBody.linearVelocityX) < 0.01)
                {
                    _frameVelocity.x = 0;
                }
            }

            _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
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
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        public void Die()
        {
            //play death animation
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