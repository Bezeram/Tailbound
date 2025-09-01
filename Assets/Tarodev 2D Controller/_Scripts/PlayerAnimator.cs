using Sirenix.OdinInspector;
using UnityEngine;

namespace TarodevController
{
    /// <summary>
    /// VERY primitive animator example.
    /// </summary>
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private Animator _Animator;

        [SerializeField] private SpriteRenderer _SpriteRenderer;

        [Header("Settings")] 
        [SerializeField] private float _AverageSighTime = 5;
        [SerializeField] private float _RandomOffsetSighTime = 1;
        [SerializeField] private float _WalkSoundsInterval = 0.3f;
        [SerializeField] private float _SoundsVolume = 0.2f;
        [ReadOnly, SerializeField] private float _SighTime;

        [Header("Particles")] [SerializeField] private ParticleSystem _JumpParticles;
        [SerializeField] private ParticleSystem _LaunchParticles;
        [SerializeField] private ParticleSystem _MoveParticles;
        [SerializeField] private ParticleSystem _LandParticles;

        [Header("Audio Clips")] [SerializeField]
        private AudioClip[] _FootstepAudioClips;

        private float _TimerIdle;
        private float _TimerWalkingSounds;
        private AudioSource _AudioSource;
        private PlayerController _PlayerController;
        private Swing _Swing;
        private bool _Grounded;
        private ParticleSystem.MinMaxGradient _CurrentColorGradient;

        private bool _IsRunning;

        void OnValidate()
        {
            _AudioSource = GetComponent<AudioSource>();
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

            _MoveParticles.Play();
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

            _MoveParticles.Stop();
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

        void OnPlayerDeath(bool instantly)
        {
            _Animator.SetTrigger(DiedKey);
        }

        private void HandleSpriteFlip()
        {
            if (_PlayerController.FrameInput.x != 0) 
                _SpriteRenderer.flipX = _PlayerController.FrameInput.x < 0;
        }

        private void HandleIdleSpeed()
        {
            var inputStrength = Mathf.Abs(_PlayerController.FrameInput.x);
            _MoveParticles.transform.localScale = Vector3.MoveTowards(_MoveParticles.transform.localScale, 
                Vector3.one * inputStrength, 2 * Time.deltaTime);
            
            // Set isRunning based on input
            _IsRunning = inputStrength > 0 && _PlayerController.IsGrounded;
            _Animator.SetBool(IsRunningKey, _IsRunning);

            if (_IsRunning)
            {
                _TimerWalkingSounds += Time.deltaTime;
                if (_TimerWalkingSounds >= _WalkSoundsInterval)
                {
                    _AudioSource.PlayOneShot(_FootstepAudioClips[Random.Range(0, _FootstepAudioClips.Length)], _SoundsVolume);
                    _TimerWalkingSounds = 0 ;
                }
            }
            
            if (_PlayerController.IsGrounded && inputStrength == 0)
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

            if (_Grounded) // Avoid coyote
            {
                SetColor(_JumpParticles);
                SetColor(_LaunchParticles);
                _JumpParticles.Play();
            }
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            _Grounded = grounded;
            
            if (grounded)
            {
                SetColor(_LandParticles);

                _Animator.SetTrigger(GroundedKey);
                _AudioSource.PlayOneShot(_FootstepAudioClips[Random.Range(0, _FootstepAudioClips.Length)], _SoundsVolume);
                _MoveParticles.Play();

                _LandParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, impact);
                _LandParticles.Play();
            }
            else
            {
                _MoveParticles.Stop();
            }
        }

        private void SetColor(ParticleSystem ps)
        {
            var main = ps.main;
            main.startColor = _CurrentColorGradient;
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