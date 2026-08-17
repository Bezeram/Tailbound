#if UNITY_EDITOR
using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

/// <summary>
/// Makes .ms files import as a TextAsset, the same as .txt/.json/etc. -
/// without this, Unity treats .ms as an unrecognized extension and imports
/// it as a generic DefaultAsset, which a TextAsset-typed field (like
/// EntityDefinition.Script) silently refuses to accept - exactly what broke
/// dragging Blinker.ms into an EntityDefinition before it got renamed to
/// Blinker.txt as a workaround.
///
/// Lives under Editor/ (Unity's magic folder name auto-excludes it from
/// player builds) and is additionally #if-guarded as a belt-and-suspenders
/// measure, since UnityEditor.AssetImporters wouldn't compile outside the
/// Editor anyway. ScriptedImporter only ever runs at import time in the
/// Editor regardless - it has no runtime/build footprint either way, so
/// this can't affect a shipped game.
/// </summary>
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
