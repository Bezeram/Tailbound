using UnityEngine;

[CreateAssetMenu(fileName = "EntitiesSettings", menuName = "Scriptable Objects/EntitiesSettings")]
public class EntitiesSettings : ScriptableObject
{
    [Header("Zipline - Timed")]
    [Tooltip("Delay before starting retraction")]
    public float DelayRetractionSeconds = 1f;
    [Tooltip("Time it takes for ZipLine to reach the end point")]
    public float TimeForwardSeconds = 1f;
    [Tooltip("Time it takes for ZipLine to retract to the start point")]
    public float TimeBackwardSeconds = 5f;

    [Header("Zipline - Acceleration")]
    public float AccelerationForward = 20f;
    public float AccelerationBackwards = 0.5f;
    public float MaxSpeedForward = 200f;
    public float MaxSpeedBackwards = 5f;
}
