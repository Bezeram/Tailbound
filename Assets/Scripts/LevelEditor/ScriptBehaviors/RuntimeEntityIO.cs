using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class RuntimeEntityIO
{
    [Serializable]
    private class Record
    {
        public string TypeId;
        public string DisplayName;
        public bool IsSpawnPoint;
        public string ScriptFileName;
        // Resources-folder path, no extension; empty/null means no icon.
        public string IconPath;
    }

    public static string EntitiesDirectory => Path.Combine(Application.persistentDataPath, "Entities");

    // On-disk source file for a runtime-built TextAsset; ScriptEntityRunner
    // polls it for live-reload.
    private static readonly Dictionary<TextAsset, string> _SourcePaths = new();
    private static readonly Dictionary<TextAsset, DateTime> _SourceLastWriteUtc = new();

    public static string GetSourcePath(TextAsset script)
    {
        return script != null && _SourcePaths.TryGetValue(script, out var path) ? path : null;
    }

    // Reloads `script` from disk if its source file changed since the last
    // check - used by the level editor's inspector to pick up expose()'d
    // properties live, instead of needing the whole app restarted. Returns
    // the same instance (no-op) for a TextAsset with no tracked source path
    // (e.g. an Editor-imported one, which Unity's own import pipeline
    // already keeps current), if the file's gone, or if nothing changed.
    public static TextAsset ReloadIfChanged(TextAsset script)
    {
        if (script == null)
            return null;

        string path = GetSourcePath(script);
        if (path == null || !File.Exists(path))
            return script;

        var writeUtc = File.GetLastWriteTimeUtc(path);
        if (_SourceLastWriteUtc.TryGetValue(script, out var known) && writeUtc == known)
            return script;

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (IOException)
        {
            // Locked by whatever's editing it (e.g. a save in progress) -
            // try again next call rather than recording writeUtc as seen.
            return script;
        }

        if (text == script.text)
        {
            // Touched but content-identical - just record it, no need to
            // replace the TextAsset (its .text is already correct).
            _SourceLastWriteUtc[script] = writeUtc;
            return script;
        }

        var reloaded = new TextAsset(text) { name = script.name };
        _SourcePaths.Remove(script);
        _SourcePaths[reloaded] = path;
        _SourceLastWriteUtc.Remove(script);
        _SourceLastWriteUtc[reloaded] = writeUtc;
        return reloaded;
    }

    public static EntityDefinition Create(string typeId, string displayName, bool isSpawnPoint, string iconPath, out string scriptPath)
    {
        Directory.CreateDirectory(EntitiesDirectory);

        string scriptFileName = typeId + ".ms";
        scriptPath = Path.Combine(EntitiesDirectory, scriptFileName);
        File.WriteAllText(scriptPath, StarterScript());

        var record = new Record
        {
            TypeId = typeId,
            DisplayName = displayName,
            IsSpawnPoint = isSpawnPoint,
            ScriptFileName = scriptFileName,
            IconPath = iconPath,
        };
        File.WriteAllText(Path.Combine(EntitiesDirectory, typeId + ".json"), JsonUtility.ToJson(record, prettyPrint: true));

        Debug.Log($"[RuntimeEntityIO] Created '{typeId}' at {scriptPath}");
        return ToEntityDefinition(record);
    }

    public static void LoadAll(Action<EntityDefinition> onLoaded)
    {
        if (!Directory.Exists(EntitiesDirectory))
            return;

        foreach (string jsonPath in Directory.GetFiles(EntitiesDirectory, "*.json"))
        {
            try
            {
                var record = JsonUtility.FromJson<Record>(File.ReadAllText(jsonPath));
                if (record == null || string.IsNullOrEmpty(record.TypeId))
                    continue;

                onLoaded(ToEntityDefinition(record));
            }
            catch (Exception exception)
            {
                Debug.LogError($"[RuntimeEntityIO] Failed to load '{jsonPath}': {exception}");
            }
        }
    }

    private static EntityDefinition ToEntityDefinition(Record record)
    {
        string scriptPath = Path.Combine(EntitiesDirectory, record.ScriptFileName);
        string scriptText = File.Exists(scriptPath) ? File.ReadAllText(scriptPath) : "";

        var scriptAsset = new TextAsset(scriptText) { name = record.TypeId };
        _SourcePaths[scriptAsset] = scriptPath;

        var def = ScriptableObject.CreateInstance<EntityDefinition>();
        def.TypeId = record.TypeId;
        def.DisplayName = record.DisplayName;
        def.IsSpawnPoint = record.IsSpawnPoint;
        def.Backing = EntityBackingKind.ScriptBehavior;
        def.Script = scriptAsset;

        if (!string.IsNullOrEmpty(record.IconPath))
        {
            def.Icon = RuntimeResourceLoader.LoadSprite(record.IconPath);
            if (def.Icon == null)
                Debug.LogWarning($"[RuntimeEntityIO] '{record.TypeId}': no Sprite found at Resources path '{record.IconPath}'.");
        }

        return def;
    }

    // One literal block so it reads as a single cheatsheet; keep in sync
    // with TailboundIntrinsics.cs by hand.
    private const string Cheatsheet =
@"// Cheatsheet
//
// expose(key, defaultValue) calls anywhere below become editable
// properties for this entity type in the level editor's inspector.
//
// start() runs once; update() runs every frame
// deltaTime - keyword, gives the game's deltaTime
//
// Position (this entity):
// getPosition - [x, y], relative to this entity's own screen
// setPosition(x, y) - moves this entity, same space as getPosition
// getWorldPosition - [x, y], absolute position (use this one for e.g.
//     ""direction to the player"" - getPosition isn't comparable to
//     getPlayerPosition, only to other local-space values)
//
// Sprite:
// setSprite(path) - Resources-folder path to a Sprite, no extension
// setColor(r, g, b, a) - tints the sprite
//
// Collider (needed for onCollisionEnter/onTriggerEnter below to ever fire -
// a bare entity starts with no collider at all):
// setCollider(width, height, isTrigger) - isTrigger 0 = solid (collision
//     events), 1 = pass-through (trigger events)
//
// Audio:
// setAudioClip(path) - Resources-folder path to an AudioClip
// setAudioVolume(volume) / setAudioPitch(pitch) / setAudioLoop(loop)
// playAudio - plays whatever clip setAudioClip assigned
// playAudioOneShot(path, volumeScale) - plays a clip once, no setAudioClip needed
// stopAudio
// isAudioPlaying - 0 or 1
//
// Player:
// getPlayerPosition - [x, y], absolute position
// setPlayerPosition(x, y) - teleports the player
// getPlayerSpeed - [x, y], the player's current velocity
// setPlayerSpeed(x, y) - overrides the player's velocity
// killPlayer(instant) - instant defaults to 1 (true)
//
// Collision/trigger - define any of these as a function to react to
// contact; other is a map: {""name"", ""isPlayer"", ""x"", ""y""}:
// onCollisionEnter(other) / onCollisionExit(other)
// onTriggerEnter(other) / onTriggerExit(other)
";

    private static string StarterScript()
    {
        return Cheatsheet +
            "\n" +
            "start = function()\n" +
            "end function\n" +
            "\n" +
            "update = function()\n" +
            "end function\n";
    }
}
