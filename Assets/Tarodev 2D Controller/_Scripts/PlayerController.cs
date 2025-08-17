using System;
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
        [Header("References")]
        public ScriptableStats _stats;
        public Swing SwingScript;
        public GameObject LevelLoader;

        [Header("Info")]
        public Vector2 _frameVelocity;
        public float MaxSpeed;

        private Rigidbody2D _RigidBody;
        private BoxCollider2D _col;
        private FrameInput _frameInput;
        private bool _cachedQueryStartInColliders;

        #region Interface

        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;

        #endregion

        private float _time;

        private void Awake()
        {
            MaxSpeed = _stats.MaxSpeed;
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
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
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

        #region Collisions
        
        private float _frameLeftGrounded = float.MinValue;
        public bool _grounded;

        private void CheckCollisions()
        {
            Physics2D.queriesStartInColliders = false;

            // Ground and Ceiling
            bool groundHit = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.down, _stats.GrounderDistance, _stats.SolidLayer);
            bool ceilingHit = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.up, _stats.GrounderDistance, _stats.SolidLayer);
            // Wall collision
            bool wallHitLeft = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.left, _stats.GrounderDistance, _stats.SolidLayer);
            bool wallHitRight = Physics2D.BoxCast(_col.bounds.center, _col.size, 0, Vector2.right, _stats.GrounderDistance, _stats.SolidLayer);

            // Hit a wall
            if (wallHitLeft || wallHitRight)
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, _stats.WallDeceleration * Time.fixedDeltaTime); ;
            }

            // Landed on the Ground
            if (!_grounded && groundHit)
            {
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
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
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

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _RigidBody.linearVelocity.y > 0)
                _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) 
                return;

            if (_grounded || CanUseCoyote) 
                ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
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

        // Called by Spring.cs
        public void SpringJump(Vector2 speed)
        {
            _endedJumpEarly = false;
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
            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.LowSpeedGroundDeceleration : _stats.LowSpeedAirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                if (_RigidBody.linearVelocityX < _stats.MaxSpeed)
                {
                    _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
                }
                else
                {
                    // Apply a weaker deceleration for high speed
                    var deceleration = _grounded ? _stats.HighSpeedGroundDeceleration : _stats.HighSpeedAirDeceleration;
                    _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
                }
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
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
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;

        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}