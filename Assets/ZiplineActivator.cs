using UnityEngine;

public class ZiplineActivator : MonoBehaviour
{
    public ZipLine_Script ZiplineReference;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ZiplineReference == null) Debug.LogWarning("Please assign the ZipLine object to ZipLine Reference slot.", this);
    }
#endif

    // Called at the moment of attaching with the tail
    public void ActivateZipline()
    {
        ZiplineReference.Attach();
    }

    // Called at the moment of releasing the tail
    public void DeactivateZipline()
    {
        ZiplineReference.Detach();
    }
}
