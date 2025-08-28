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
        public PlayerMovementSettings Stats;
        public PlayerAbilitiesSettings PlayerAbilitiesSettings;
        public Swing SwingScript;
        public GameObject LevelLoader;
        
        [TitleGroup("Info")]
        [ReadOnly, ShowInInspector, SerializeField] private Vector2 _FrameVelocity;
        [ReadOnly, ShowInInspector, SerializeField] private float _Stamina;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsClimbingLeft;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsAdjacentToWallLeft;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsAdjacentToWallRight;
        [ReadOnly, ShowInInspector, SerializeField] private bool _FacingLeft;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsClimbing;
        [ReadOnly, ShowInInspector, SerializeField] private bool _IsDead;
        
        private SpriteRenderer _SpriteRenderer;
        private Rigidbody2D _RigidBody;
        private BoxCollider2D _Collider;
        private BoxCollider2D _ClimbingCollider;
        private FrameInput _FrameInput;
        private bool _CachedQueryStartInColliders;
        
        private float _StaminaHighlightTimer;
        private bool _StaminaFlashingRed;
        
        public bool IsClimbing => _IsClimbing;

        #region Interface

        public Vector2 FrameInput => _FrameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Climbed;
        public event Action Jumped;

        #endregion

        private float _Time;

        void Awake()
        {
            _RigidBody = GetComponent<Rigidbody2D>();
            _Collider = GetComponent<BoxCollider2D>();
            _ClimbingCollider = transform.Find("ClimbHitbox").GetComponent<BoxCollider2D>();
            LevelLoader = GameObject.FindGameObjectWithTag("LevelLoader");
            _IsDead = false;

            _CachedQueryStartInColliders = Physics2D.queriesStartInColliders;
        }

        void Update()
        {
            HighlightStaminaLoss();
            
            if (SwingScript.IsSwinging)
                return;

            if (_IsDead)
                return;

            _Time += Time.deltaTime;
            GatherInput();
        }

		void FixedUpdate()
		{
            if (SwingScript.IsSwinging)
                return;

            if (_IsDead)
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
            _FrameVelocity = velocity;
        }

		void GatherInput()
        {
            _FrameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(PlayerAbilitiesSettings.JumpKey),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(PlayerAbilitiesSettings.JumpKey),
                ClimbHeld = Input.GetKey(PlayerAbilitiesSettings.ClimbKey),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };

            if (Stats.SnapInput)
            {
                _FrameInput.Move.x = Mathf.Abs(_FrameInput.Move.x) < Stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_FrameInput.Move.x);
                _FrameInput.Move.y = Mathf.Abs(_FrameInput.Move.y) < Stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_FrameInput.Move.y);
            }

            if (_FrameInput.JumpDown)
            {
                _JumpToConsume = true;
                _TimeJumpWasPressed = _Time;
            }
        }

        void HighlightStaminaLoss()
        {
            if (!(_Stamina < PlayerAbilitiesSettings.StaminaThreshold)) 
                return;
            
            _StaminaHighlightTimer += Time.deltaTime;

            if (_StaminaHighlightTimer >= PlayerAbilitiesSettings.StaminaFlashPeriod)
            {
                _StaminaFlashingRed = !_StaminaFlashingRed;
                _StaminaHighlightTimer -= PlayerAbilitiesSettings.StaminaFlashPeriod;
                                        
                Color color = _StaminaFlashingRed ? PlayerAbilitiesSettings.StaminaHighlightColor : Color.white;
                _SpriteRenderer.color = color;
            }
        }
        
        // Check if the player can climb.
        bool CanClimb(bool adjacentWallLeft, bool adjacentWallRight)
        {
            bool hasStamina = _Stamina > 0;
            // Prevent climbing while pressing down to avoid funny behaviour.
            bool correctInputs = _FrameInput.ClimbHeld && FrameInput.y >= 0;
            // Cannot climb on a wall if the player is moving upwards too fast.
            bool correctVelocity = _FrameVelocity.y < PlayerAbilitiesSettings.SpeedCapToClimb;
            // Check if player is facing towards wall.
            bool facingWall = (_FacingLeft && adjacentWallLeft) || (!_FacingLeft && adjacentWallRight);
            
            return hasStamina && correctInputs && correctVelocity && facingWall;
        }

        private const float _FractionalDistanceFromWall = 0.456f;
        
        void TriggerClimb()
        {
            // Reset speed
            _FrameVelocity = Vector2.zero;
            // Stick player close to wall
            float x = transform.position.x;
            int direction = _FacingLeft ? 1 : -1;
            float roundedX = _FacingLeft ? Mathf.Floor(x) : Mathf.Ceil(x);
            float newX = roundedX + direction * _FractionalDistanceFromWall;
            transform.position = new(newX, transform.position.y, transform.position.z);

            // Leave the ground
            if (_Grounded)
            {
                // To prevent the player from immediately touching the ground
                // in the climb state, elevate the player a bit.
                transform.position += Stats.GrounderDistance * 1.2f * Vector3.up;
            }
            _Grounded = false;

            _IsClimbingLeft = _FacingLeft;
            _IsClimbing = true;
            Climbed?.Invoke();
        }

        void HandleClimbing()
        {
            if (_IsClimbing && !_FrameInput.ClimbHeld)
                _IsClimbing = false;

            if (!_IsClimbing)
                return;

            // Update stamina
            _Stamina -= PlayerAbilitiesSettings.StaminaConstantCost * Time.fixedDeltaTime;
            // Check for climbing
            if (_FrameInput.Move.y != 0)
            {
                int direction = (_FrameInput.Move.y > 0) ? 1 : -1;
                _FrameVelocity.y = direction * PlayerAbilitiesSettings.ClimbSpeed;
                _Stamina -= PlayerAbilitiesSettings.StaminaClimbingCost * Time.fixedDeltaTime;
            }
            else
            {
                _FrameVelocity.y = 0;
            }

            if (_Stamina <= 0)
                _IsClimbing = false;
        }

        private float _FrameLeftGrounded = float.MinValue;
        [ReadOnly] private bool _Grounded;

        public bool IsGrounded => _Grounded;

        void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            bool groundHit = Physics2D.BoxCast(_Collider.bounds.center, _Collider.size, 0, Vector2.down, Stats.GrounderDistance, Stats.SolidLayer);
            bool ceilingHit = Physics2D.BoxCast(_Collider.bounds.center, _Collider.size, 0, Vector2.up, Stats.GrounderDistance, Stats.SolidLayer);
            // Wall collision
            bool hitWallLeft = Physics2D.BoxCast(_Collider.bounds.center, _Collider.size, 0, Vector2.left, Stats.GrounderDistance, Stats.SolidLayer);
            bool hitWallRight = Physics2D.BoxCast(_Collider.bounds.center, _Collider.size, 0, Vector2.right, Stats.GrounderDistance, Stats.SolidLayer);
            // Check for adjacent wall (climbing)
            bool adjacentWallLeft = IsWallAdjacent(Vector2.left);
            bool adjacentWallRight = IsWallAdjacent(Vector2.right);

            // Info
            _IsAdjacentToWallLeft = adjacentWallLeft;
            _IsAdjacentToWallRight = adjacentWallRight;

            // Landed on the Ground
            if (!_Grounded && groundHit)
            {
                // Climbing
                _IsClimbing = false;
                // Reset stamina
                _Stamina = PlayerAbilitiesSettings.StaminaTotal;
                _SpriteRenderer.color = Color.white;

                _Grounded = true;
                _CoyoteUsable = true;
                _BufferedJumpUsable = true;
                _EndedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_FrameVelocity.y));
            }
            // Left the Ground
            else if (_Grounded && !groundHit)
            {
                _Grounded = false;
                _IsClimbing = false;
                _FrameLeftGrounded = _Time;
                GroundedChanged?.Invoke(false, 0);
            }

            // Check for wall collision
            if (hitWallLeft || hitWallRight)
            {
                // Apply deceleration
                if (!_IsClimbing)
                    _FrameVelocity.x = Mathf.MoveTowards(_FrameVelocity.x, 0, Stats.WallDeceleration * Time.fixedDeltaTime);
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
                    if (FrameInput.y > 0 && _FrameVelocity.y < PlayerAbilitiesSettings.SpeedCapLedgeJump)
                    {
                        Vector2 direction = new(_FacingLeft ? -1 : 1, 1);
                        _FrameVelocity = direction * PlayerAbilitiesSettings.LedgeBoost;
                    }
                }
            }

            Physics2D.queriesStartInColliders = _CachedQueryStartInColliders;
            return;

            // Use the dedicated collider.
            bool IsWallAdjacent(Vector2 direction)
            {
                return Physics2D.BoxCast(_ClimbingCollider.bounds.center, _ClimbingCollider.size, 0,
                    direction, PlayerAbilitiesSettings.AdjacentWallDistance, Stats.SolidLayer);
            }
        }

        private bool _JumpToConsume;
        private bool _BufferedJumpUsable;
        private bool _EndedJumpEarly;
        private bool _CoyoteUsable = true;
        private float _TimeJumpWasPressed = float.MinValue;

        private bool HasBufferedJump => _BufferedJumpUsable && _Time < _TimeJumpWasPressed + Stats.JumpBuffer;
        private bool CanUseCoyote => _CoyoteUsable && !_Grounded && _Time < _FrameLeftGrounded + Stats.CoyoteTime;

        bool IsFacingTowardWall()
        {
            return _FacingLeft == _IsClimbingLeft;
        }

        void HandleJump()
        {
            if (!_EndedJumpEarly && !_Grounded && !_FrameInput.JumpHeld && _RigidBody.linearVelocity.y > 0)
                _EndedJumpEarly = true;

            if (!_JumpToConsume && !HasBufferedJump) 
                return;

            // A normal jump is made on the ground.
            // A climb jump happens only while climbing.
            // A wall jump happens a player is close enough to a wall and:
            // 1. Player is facing away from the wall
            // 2. Player is facing toward wall, but is not climbing
            if (_Grounded || CanUseCoyote) 
                ExecuteJump();
            else if (_IsClimbing && IsFacingTowardWall())
                ExecuteClimbJump();
            else if (!_Grounded && _IsAdjacentToWallLeft || _IsAdjacentToWallRight)
                ExecuteWallJump();

            _JumpToConsume = false;
        }

        void ExecuteJump()
        {
            _EndedJumpEarly = false;
            _TimeJumpWasPressed = 0;
            _BufferedJumpUsable = false;
            _CoyoteUsable = false;

            // After max speed, a jump boost is added proportional to sideways movement.
            _FrameVelocity.y = Stats.JumpPower;
            if (Mathf.Abs(_FrameVelocity.x) > Stats.WalkingSpeedCap)
            {
                // Steal speed from the horizontal component.
                float boost = Stats.JumpInheritanceFactor * _FrameVelocity.x;
                _FrameVelocity.y += boost;
                _FrameVelocity.x -= boost;
            }

            Jumped?.Invoke();
        }

        void ExecuteWallJump()
        {
            _IsClimbing = false;
            _EndedJumpEarly = false;
            _TimeJumpWasPressed = 0;
            _BufferedJumpUsable = false;
            _CoyoteUsable = false;

            Vector2 direction = new(_IsAdjacentToWallLeft ? 1 : -1, 1);
            _FrameVelocity = direction * PlayerAbilitiesSettings.WallJumpPower;

            Jumped?.Invoke();
        }

        void ExecuteClimbJump()
        {
            _IsClimbing = false;
            _EndedJumpEarly = true;
            _TimeJumpWasPressed = 0;
            _BufferedJumpUsable = false;
            _CoyoteUsable = false;

            _Stamina -= PlayerAbilitiesSettings.ClimbJumpStaminaCost;
            _FrameVelocity.y = PlayerAbilitiesSettings.ClimbJumpPower;

            Jumped?.Invoke();
        }

        // Called by Spring.cs
        public void ExecuteSpringJump(Vector2 speed)
        {
            _EndedJumpEarly = true;
            _TimeJumpWasPressed = 0;
            _BufferedJumpUsable = false;
            _CoyoteUsable = false;

            _FrameVelocity = speed;
            ApplyMovement();

            Jumped?.Invoke();
        }
        
        void HandleDirection()
        {
            if (FrameInput.x != 0)
                _FacingLeft = FrameInput.x < 0;

            if (_IsClimbing)
                return;

            // Constant deceleration
            float constantDeceleration = _Grounded ? Stats.GroundDeceleration : Stats.AirDeceleration;
            _FrameVelocity.x = Mathf.MoveTowards(_FrameVelocity.x, 0, constantDeceleration * Time.fixedDeltaTime);

            // Apply Input movement
            if (FrameInput.x == 0)
            {
                // With the stick neutral, apply a deceleration.
                // Deceleration varies if the player is on the ground or in the air.
                float neutralDeceleration = _Grounded ? Stats.NeutralGroundDeceleration : Stats.NeutralAirDeceleration;
                _FrameVelocity.x = Mathf.MoveTowards(_FrameVelocity.x, 0, neutralDeceleration * Time.fixedDeltaTime);
            }
            else
            {
                // Apply acceleration
                _FrameVelocity.x += FrameInput.x * Stats.Acceleration * Time.fixedDeltaTime;
            }

            // Adjustment dependent on speed value
            // The walking speed is capped.
            // To get past it, another means of acceleration must be used.
            // Basically checking if we are within the speed cap + one acceleration update step.
            float errorMargin = Stats.Acceleration * Time.fixedDeltaTime * Stats.MaxSpeedErrorMargin;
            if (Mathf.Abs(_FrameVelocity.x) > Stats.WalkingSpeedCap + errorMargin)
            {
                // High speed regime
                // Apply heavy deceleration
                // Constant deceleration
                float highspeedDeceleration = _Grounded ? Stats.HighspeedGroundDeceleration : Stats.HighspeedAirDeceleration;
                _FrameVelocity.x = Mathf.MoveTowards(_FrameVelocity.x, 0, highspeedDeceleration * Time.fixedDeltaTime);

                // If player hits a wall and holds in opposite direction stop all movement.
                if (_FrameInput.Move.x * _FrameVelocity.x <= 0 && Mathf.Abs(_RigidBody.linearVelocityX) < 0.01)
                {
                    _FrameVelocity.x = 0;
                }
            }
            else
            {
                // Low speed regime
                // Apply walking speed cap.
                // If player hits a wall and holds in opposite direction stop all movement.
                //if (_frameInput.Move.x * _frameVelocity.x < 0)
                //    _frameVelocity.x = 0;

                _FrameVelocity.x = Mathf.Clamp(_FrameVelocity.x, -Stats.WalkingSpeedCap, Stats.WalkingSpeedCap);
            }
        }

        void HandleGravity()
        {
            if (_IsClimbing)
                return;

            if (_Grounded && _FrameVelocity.y <= 0f)
            {
                _FrameVelocity.y = Stats.GroundingForce;
            }
            else
            {
                var inAirGravity = Stats.FallAcceleration;
                if (_EndedJumpEarly && _FrameVelocity.y > 0) 
                    inAirGravity *= Stats.JumpEndEarlyGravityModifier;
                _FrameVelocity.y = Mathf.MoveTowards(_FrameVelocity.y, -Stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        public void Die()
        {
            //play death animation
            _IsDead = true;
            _FrameVelocity.x = 0;
            _FrameVelocity.y = 0;
            ApplyMovement();
            
            LevelLoader.GetComponent<LevelLoader>().Respawn();
        }

        public void Respawn()
        {
            _IsDead = false;
        }

        private void ApplyMovement() => _RigidBody.linearVelocity = _FrameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Stats == null) 
                Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);

            // Find sprite
            _SpriteRenderer = transform.Find("Visual").Find("Sprite").GetComponent<SpriteRenderer>();
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