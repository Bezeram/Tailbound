using UnityEngine;

namespace TarodevController
{
    [CreateAssetMenu]
    public class PlayerMovementSettings : ScriptableObject
    {
        [Header("LAYERS")] [Tooltip("Set this to the solid layer")]
        public LayerMask SolidLayer;

        [Header("INPUT")] 
        [Tooltip("Makes all Input snap to an integer. Prevents gamepads from walking slowly. " +
            "Recommended value is true to ensure gamepad/keyboard parity.")]
        public bool SnapInput = true;

        [Tooltip("Minimum input required before you mount a ladder or climb a ledge. Avoids unwanted climbing using controllers"), Range(0.01f, 0.99f)]
        public float VerticalDeadZoneThreshold = 0.3f;

        [Tooltip("Minimum input required before a left or right is recognized. Avoids drifting with sticky controllers"), Range(0.01f, 0.99f)]
        public float HorizontalDeadZoneThreshold = 0.1f;

        [Header("MOVEMENT")] [Tooltip("The top horizontal movement speed")]
        public float WalkingSpeedCap = 14;

        [Tooltip("The player's capacity to gain horizontal speed")]
        public float Acceleration = 120;

        [Tooltip("Safety margin for not allowing the walking speed cap to be passed by walking.")]
        public float MaxSpeedErrorMargin = 1.5f;

        [Tooltip("Deceleration applied with neutral controls as a dividor of Acceleration.")]
        public float NeutralDecelerationFactor = 1.2f;

        [Tooltip("The pace at which the player comes to a stop at low speed")]
        public float GroundDeceleration = 60;

        [Tooltip("Deceleration in air only after stopping input mid-air")]
        public float AirDeceleration = 80;

        [Tooltip("Only applies when player is above the walking speed cap.")]
        public float HighspeedGroundDeceleration = 60;

        [Tooltip("Only applies when player is above the walking speed cap.")]
        public float HighspeedAirDeceleration = 120;

        [Tooltip("Deceleration upon hitting a wall")]
        public float WallDeceleration = 50;

        [Tooltip("A constant downward force applied while grounded. Helps on slopes"), Range(0f, -10f)]
        public float GroundingForce = -1.5f;

        [Tooltip("The detection distance for grounding and roof detection"), Range(0f, 0.5f)]
        public float GrounderDistance = 0.05f;

        [Header("JUMP")] [Tooltip("The immediate velocity applied when jumping")]
        public float JumpPower = 36;

        [Tooltip("The amount of speed inherited from horizontal movement when jumping.")]
        public float JumpInheritanceFactor = 0.2f;

        [Tooltip("The maximum vertical movement speed")]
        public float MaxFallSpeed = 40;

        [Tooltip("The player's capacity to gain fall speed. a.k.a. In Air Gravity")]
        public float FallAcceleration = 110;

        [Tooltip("The gravity multiplier added when jump is released early")]
        public float JumpEndEarlyGravityModifier = 3;

        [Tooltip("The time before coyote jump becomes unusable. Coyote jump allows jump to execute even after leaving a ledge")]
        public float CoyoteTime = .15f;

        [Tooltip("The amount of time we buffer a jump. This allows jump input before actually hitting the ground")]
        public float JumpBuffer = .2f;
    }
}