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

    // Update is called once per frame
    void Update()
    {
        
    }
}
