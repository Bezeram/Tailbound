using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "ZiplineSettings", menuName = "Scriptable Objects/ZiplineSettings")]
public class ZiplineSettings : ScriptableObject
{
    [TitleGroup("Common")]
    [Tooltip("Time it takes for Zipline to reset in order to start")]
    public float DelayResetSeconds = 1f;

    [TitleGroup("Timed")]
    [Tooltip("Delay before starting retraction")]
    public float DelayRetractionSeconds = 1f;
    [Tooltip("Time it takes for ZipLine to reach the end point")]
    public float TimeForwardSeconds = 1f;
    [Tooltip("Time it takes for ZipLine to retract to the start point")]
    public float TimeBackwardSeconds = 5f;

    [TitleGroup("Acceleration")]
    public float AccelerationForward = 20f;
    public float AccelerationBackwards = 0.5f;
    public float MaxSpeedForward = 200f;
    public float MaxSpeedBackwards = 5f;
}
