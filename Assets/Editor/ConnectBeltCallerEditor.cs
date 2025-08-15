using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ConnectBeltCaller))]
public class ConnectBeltCallerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var caller = (ConnectBeltCaller)target;

        if (GUILayout.Button("Reattach Belt", GUILayout.Height(25)))
        {
            caller.CallConnectBelt();
        }
    }
}
