using UnityEngine;

public class ZiplineActivator : MonoBehaviour, IEntityActivator
{
    [SerializeField] private ActivatableEntity Zipline;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Zipline == null) 
            Debug.LogWarning("Please assign a Zipline reference to the Zipline Activator script.", this);
    }
#endif

    // Called at the moment of attaching with the tail
    public void SendActivation()
    {
        Zipline.ReceiveActivation();
    }

    // Called at the moment of releasing the tail
    public void SendDeactivation()
    {
        Zipline.ReceiveDeactivation();
    }
}
