using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace TarodevController
{
    /// <summary>
    /// VERY primitive animator example.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [FormerlySerializedAs("_anim")] [Header("References")] [SerializeField]
        private Animator _Animator;

        [SerializeField] private SpriteRenderer _sprite;

        [Header("Settings")] 
        [SerializeField, Range(1f, 3f)] private float _maxIdleSpeed = 2;
        [SerializeField] private float _TimerIdle = 0;
        [SerializeField] private float _AverageSighTime = 5;
        [SerializeField] private float _RandomOffsetSighTime = 1;

        [SerializeField] private float _maxTilt = 5;
        [SerializeField] private float _tiltSpeed = 20;

        [Header("Particles")] [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _launchParticles;
        [SerializeField] private ParticleSystem _moveParticles;
        [SerializeField] private ParticleSystem _landParticles;

        [Header("Audio Clips")] [SerializeField]
        private AudioClip[] _footsteps;

        private AudioSource _source;
        private PlayerController _PlayerController;
        private Swing _Swing;
        private bool _grounded;
        private ParticleSystem.MinMaxGradient _currentGradient;

        private bool _IsRunning;
        public float _SighTime;

        void OnValidate()
        {
            _source = GetComponent<AudioSource>();
            _PlayerController = GetComponentInParent<PlayerController>();
            _Swing = GetComponentInParent<Swing>();
        }

        void OnEnable()
        {
            _PlayerController.Jumped += OnJumped;
            _PlayerController.GroundedChanged += OnGroundedChanged;
            _PlayerController.Died += OnPlayerDeath;
            _PlayerController.Respawned += OnPlayerRespawn;
            
            _SighTime = _AverageSighTime + Random.Range(-_RandomOffsetSighTime, _RandomOffsetSighTime);

            _moveParticles.Play();
        }

        void OnDisable()
        {
            if (_PlayerController != null)
            {
                _PlayerController.Jumped -= OnJumped;
                _PlayerController.GroundedChanged -= OnGroundedChanged;
                _PlayerController.Died -= OnPlayerDeath;
                _PlayerController.Respawned -= OnPlayerRespawn;
            }

            _moveParticles.Stop();
        }

        void Update()
        {
            if (_PlayerController == null) 
                return;

            HandleSpriteFlip();

            HandleIdleSpeed();
            
            _Animator.SetBool(IsSwingingKey, _Swing.IsSwinging);
            _Animator.SetBool(SwingingLeftKey, _PlayerController.FacingLeft);
            _Animator.SetFloat(VerticalSpeedKey, _PlayerController.FrameVelocity.y);
        }

        void OnPlayerRespawn()
        {
            _Animator.SetTrigger(RespawnedKey);
        }

        void OnPlayerDeath()
        {
            _Animator.SetTrigger(DiedKey);
        }

        private void HandleSpriteFlip()
        {
            if (_PlayerController.FrameInput.x != 0) 
                _sprite.flipX = _PlayerController.FrameInput.x < 0;
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = Mathf.Abs(_PlayerController.FrameInput.x);
            _moveParticles.transform.localScale = Vector3.MoveTowards(_moveParticles.transform.localScale, Vector3.one * inputStrength, 2 * Time.deltaTime);
            
            // Set isRunning based on input
            _IsRunning = inputStrength > 0;
            _Animator.SetBool(IsRunningKey, _IsRunning);
            
            if (_PlayerController.IsGrounded && !_IsRunning)
            {
                // Idle state    
                _TimerIdle += Time.deltaTime;

                if (_TimerIdle > _SighTime)
                {
                    // Trigger sigh animation
                    _Animator.SetTrigger(SighedKey);
                    _SighTime = _AverageSighTime + Random.Range(-_RandomOffsetSighTime, _RandomOffsetSighTime);
                    _TimerIdle = 0;
                }
            }
            else
                _TimerIdle = 0;
        }

        private void OnJumped()
        {
            _Animator.SetTrigger(JumpKey);
            _Animator.ResetTrigger(GroundedKey);

            if (_grounded) // Avoid coyote
            {
                SetColor(_jumpParticles);
                SetColor(_launchParticles);
                _jumpParticles.Play();
            }
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            _grounded = grounded;
            
            if (grounded)
            {
                SetColor(_landParticles);

                _Animator.SetTrigger(GroundedKey);
                _source.PlayOneShot(_footsteps[Random.Range(0, _footsteps.Length)]);
                _moveParticles.Play();

                _landParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, impact);
                _landParticles.Play();
            }
            else
            {
                _moveParticles.Stop();
            }
        }

        private void SetColor(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = _currentGradient;
        }

        private static readonly int IsRunningKey = Animator.StringToHash("IsRunning");
        private static readonly int IsSwingingKey = Animator.StringToHash("IsSwinging");
        private static readonly int SwingingLeftKey = Animator.StringToHash("SwingingLeft");
        private static readonly int JumpKey = Animator.StringToHash("Jump");
        private static readonly int GroundedKey = Animator.StringToHash("Grounded");
        private static readonly int DiedKey = Animator.StringToHash("Died");
        private static readonly int RespawnedKey = Animator.StringToHash("Respawned");
        private static readonly int SighedKey = Animator.StringToHash("Sighed");
        private static readonly int VerticalSpeedKey = Animator.StringToHash("VerticalSpeed");
    }
}