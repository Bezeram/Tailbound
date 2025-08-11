using UnityEngine;

[CreateAssetMenu(fileName = "ScriptableTailSpring", menuName = "Scriptable Objects/ScriptableTailSpring")]
public class ScriptableTail : ScriptableObject
{
    [Header("Spring Joint")]
    public float frequency = 1.0f;

    [Range(0.0f, 1.0f)]
    public float dampingRatio = 0.5f;

    [Header("Swinging")]
    public float SwingForce = 1f;
    public float JumpScalar = 7f;
    public float GravityMultiplier = 1;

    [Header("Attaching")]
    public float MaxTailLength = 15f;
    public float MinTailLength = 2f;
}
