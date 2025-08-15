using System.Reflection;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ZipLine_Script))]
public class ZipLineEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Create and draw custom button
        HandleReattachButton();

        // Draw the default inspector UI
        DrawDefaultInspector();
    }

    void HandleReattachButton()
    {
        var zip = (ZipLine_Script)target;

        EditorGUILayout.Space(6);

        // Big inline button
        if (GUILayout.Button("Reattach Belt", GUILayout.Height(20)))
        {
            // Make changes undoable
            Undo.RecordObject(zip, "Reattach Belt");
            if (zip.StartPointTransform) Undo.RecordObject(zip.StartPointTransform, "Reattach Belt");
            if (zip.EndPointTransform) Undo.RecordObject(zip.EndPointTransform, "Reattach Belt");
            if (zip.AttachmentTransform) Undo.RecordObject(zip.AttachmentTransform, "Reattach Belt");
            if (zip.BeltTransform) Undo.RecordObject(zip.BeltTransform, "Reattach Belt");

            // Call ReattachBelt() even if it's private
            MethodInfo method = typeof(ZipLine_Script).GetMethod(
                "ConnectBelt",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );

            if (method != null)
            {
                method.Invoke(zip, null);

                // Mark objects dirty so changes persist in the editor
                EditorUtility.SetDirty(zip);
                if (zip.StartPointTransform) EditorUtility.SetDirty(zip.StartPointTransform);
                if (zip.EndPointTransform) EditorUtility.SetDirty(zip.EndPointTransform);
                if (zip.AttachmentTransform) EditorUtility.SetDirty(zip.AttachmentTransform);
                if (zip.BeltTransform) EditorUtility.SetDirty(zip.BeltTransform);
            }
            else
            {
                Debug.LogError("ReattachBelt() not found on ZipLine_Script.");
            }
        }
    }
}
