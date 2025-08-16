using UnityEngine;

public class ZiplineActivator : MonoBehaviour
{
    public Zipline ZiplineReferenceScript;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ZiplineReferenceScript == null) 
            Debug.LogWarning("Please assign a Zipline reference to the Zipline Activator script.", this);
    }
#endif

    // Called at the moment of attaching with the tail
    public void ActivateZipline()
    {
        ZiplineReferenceScript.Attach();
    }

    // Called at the moment of releasing the tail
    public void DeactivateZipline()
    {
        ZiplineReferenceScript.Detach();
    }
}
