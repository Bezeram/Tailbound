using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "ScriptablePlayer", menuName = "Scriptable Objects/ScriptablePlayer")]
public class ScriptablePlayer : ScriptableObject
{
    [TitleGroup("CONTROLS")]
    public KeyCode LeftKey = KeyCode.LeftArrow;
    public KeyCode RightKey = KeyCode.RightArrow;
    public KeyCode UpKey = KeyCode.UpArrow;
    public KeyCode DownKey = KeyCode.DownArrow;
    public KeyCode JumpKey = KeyCode.C;
    public KeyCode AttachKey = KeyCode.C;
    public KeyCode ClimbKey = KeyCode.X;

    [TitleGroup("SPRING JOINT")]
    public float Frequency = 1.0f;
    public float MaxTailLength = 15f;

    [Range(0.0f, 1.0f)]
    public float DampingRatio = 0.5f;

    [TitleGroup("SWINGING")]
    public float BaseSwingForce = 1f;
    public float JumpScalar = 7f;
    public float GravityMultiplier = 1;
    public float LinearDamping = 1f;

    [TitleGroup("CLIMBING")]
    public float StaminaTotal = 35f;
    public float StaminaIdleCost = 4f;
    public float StaminaClimbingCost = 2f;
    public float StaminaJumpCost = 6f;
    public float ClimbSpeed = 2f;
    public Vector2 WallJumpPower = new(4f, 18f);
    public float SpeedCapToClimb = 10f;
    public float AdjacentWallDistance = 0.1f;
}
