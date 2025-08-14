using UnityEngine;

[CreateAssetMenu(fileName = "EntitiesSettings", menuName = "Scriptable Objects/EntitiesSettings")]
public class EntitiesSettings : ScriptableObject
{
    [Header("Zipline")]
    public float AccelerationForward = 20f;
    public float AccelerationBackwards = 0.5f;
    public float MaxSpeedForward = 200f;
    public float MaxSpeedBackwards = 5f;
    public float DelayRetractionSeconds = 1f;
}
