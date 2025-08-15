using UnityEngine;
using System.Reflection;

public class ConnectBeltCaller : MonoBehaviour
{
    [Tooltip("Reference to the ZipLine_Script component you want to control.")]
    public ZipLine_Script targetZipline;

    // Calls ConnectBelt() on the target zipline
    public void CallConnectBelt()
    {
        if (targetZipline == null)
        {
            Debug.LogWarning("No ZipLine_Script assigned to ConnectBeltCaller on " + gameObject.name);
            return;
        }

        // Use reflection so ConnectBelt() can stay private
        MethodInfo method = typeof(ZipLine_Script).GetMethod(
            "ConnectBelt",
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );

        if (method != null)
        {
            method.Invoke(targetZipline, null);
            Debug.Log("ConnectBelt() called on " + targetZipline.name);
        }
        else
        {
            Debug.LogError("ConnectBelt() not found on " + targetZipline.name);
        }
    }
}