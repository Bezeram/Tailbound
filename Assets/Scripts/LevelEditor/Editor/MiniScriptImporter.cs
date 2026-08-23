#if UNITY_EDITOR
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

[ScriptedImporter(1, "ms")]
public class MiniScriptImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        string source = File.ReadAllText(ctx.assetPath);
        var textAsset = new TextAsset(source);

        ctx.AddObjectToAsset("main", textAsset);
        ctx.SetMainObject(textAsset);
    }
}
#endif
