using UnityEngine;

[CreateAssetMenu(fileName = "ScriptablePlayer", menuName = "Scriptable Objects/ScriptablePlayer")]
public class ScriptablePlayer : ScriptableObject
{
    [Header("Default Controls")]
    public KeyCode LeftKey = KeyCode.LeftArrow;
    public KeyCode RightKey = KeyCode.RightArrow;
    public KeyCode UpKey = KeyCode.UpArrow;
    public KeyCode DownKey = KeyCode.DownArrow;
    public KeyCode JumpKey = KeyCode.C;
    public KeyCode AttachKey = KeyCode.C;

    [Header("Spring Joint")]
    public float frequency = 1.0f;

    [Range(0.0f, 1.0f)]
    public float dampingRatio = 0.5f;

    [Header("Swinging")]
    public float BaseSwingForce = 1f;
    public float JumpScalar = 7f;
    public float GravityMultiplier = 1;
    [Range(0.0f, 2.0f)]
    public float JumpInheritanceFactor = 0.2f;
    public float LinearDamping = 1f;

    [Header("Attaching")]
    public float MaxTailLength = 15f;
}
