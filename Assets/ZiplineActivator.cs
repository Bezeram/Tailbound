using UnityEngine;

public class ZiplineActivator : MonoBehaviour
{
    public ZipLine_Script ZiplineReferenceScript;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (ZiplineReferenceScript == null) Debug.LogWarning("Please assign a ScriptableStats asset to the Player Controller's Stats slot", this);
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
