using UnityEngine;

public abstract class ActivatableEntity : MonoBehaviour
{
    public abstract void ReceiveActivation();
    public abstract void ReceiveDeactivation();
}
