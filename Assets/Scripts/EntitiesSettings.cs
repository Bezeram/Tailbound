using Sirenix.OdinInspector;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "EntitiesSettings", menuName = "Scriptable Objects/EntitiesSettings")]
public class EntitiesSettings : ScriptableObject
{
    [Serializable]
    public class ZiplineSettings
    {
        [BoxGroup("Common")]
        [Tooltip("Time it takes for Zipline to reset in order to start")]
        public float DelayResetSeconds = 1f;
        [BoxGroup("Common")]
        [Tooltip("Delay before starting retraction")]
        public float DelayRetractionSeconds = 1f;
    
        [BoxGroup("Timed")]
        [Tooltip("Time it takes for ZipLine to reach the end point")]
        public float TimeForwardSeconds = 1f;
        [BoxGroup("Timed")]
        [Tooltip("Time it takes for ZipLine to retract to the start point")]
        public float TimeBackwardSeconds = 5f;

        [BoxGroup("Acceleration")] public float AccelerationForward = 20f;
        [BoxGroup("Acceleration")] public float AccelerationBackwards = 0.5f;
        [BoxGroup("Acceleration")] public float MaxSpeedForward = 200f;
        [BoxGroup("Acceleration")] public float MaxSpeedBackwards = 5f;
    }

    [Serializable]
    public class SpringSettings
    {
        [InfoBox("Note that values are considered absolute. Direction is decided in the respective script.", InfoMessageType = InfoMessageType.Info)]
        public Vector2 SpeedUp = new(0f, 40f);
        public Vector2 SpeedSideways = new(40f, 20f);
    }

    public ZiplineSettings Zipline;
    public SpringSettings Spring;
}
