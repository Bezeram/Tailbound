using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Runtime-created ScriptBehavior entity types - has to work in a
/// standalone build (no Unity Editor, no AssetDatabase), so this is a
/// starter .ms text file plus a small JSON record, both plain files under
/// Application.persistentDataPath, the same "no Unity asset involved"
/// approach LevelIO already uses for levels themselves. Each is wrapped
/// into a real in-memory EntityDefinition via ScriptableObject.CreateInstance
/// and `new TextAsset(text)` - both plain runtime APIs, not Editor-only ones
/// (unlike AssetDatabase.CreateAsset, which the level editor's New Script
/// Entity dialog used before this).
/// </summary>
public static class RuntimeEntityIO
{
    [Serializable]
    private class Record
    {
        public string TypeId;
        public string DisplayName;
        public string Category;
        public bool IsSpawnPoint;
        public string ScriptFileName;
        // Resources-folder path (no extension), same convention as
        // setSprite/setAudioClip - empty/null means no icon, same as
        // leaving EntityDefinition.Icon unassigned by hand.
        public string IconPath;
    }

    private static string EntitiesDirectory => Path.Combine(Application.persistentDataPath, "Entities");

    // Which on-disk file (if any) a given in-memory TextAsset was built
    // from - ScriptEntityRunner uses this to poll the file directly for
    // live-reload, since a runtime-constructed TextAsset has no Unity
    // import pipeline behind it to keep its .text current on its own the
    // way an Editor-imported one does.
    private static readonly Dictionary<TextAsset, string> _SourcePaths = new();

    public static string GetSourcePath(TextAsset script)
    {
        return script != null && _SourcePaths.TryGetValue(script, out var path) ? path : null;
    }

    /// <summary>
    /// Creates a new runtime script entity: a starter .ms file and its
    /// metadata record, both under EntitiesDirectory. Returns the resulting
    /// EntityDefinition (already usable) and the script's full path (so the
    /// caller can hand it to e.g. Process.Start to open it for editing).
    /// </summary>
    public static EntityDefinition Create(string typeId, string displayName, string category, bool isSpawnPoint, string iconPath, out string scriptPath)
    {
        Directory.CreateDirectory(EntitiesDirectory);

        string scriptFileName = typeId + ".ms";
        scriptPath = Path.Combine(EntitiesDirectory, scriptFileName);
        File.WriteAllText(scriptPath, StarterScript());

        var record = new Record
        {
            TypeId = typeId,
            DisplayName = displayName,
            Category = category,
            IsSpawnPoint = isSpawnPoint,
            ScriptFileName = scriptFileName,
            IconPath = iconPath,
        };
        File.WriteAllText(Path.Combine(EntitiesDirectory, typeId + ".json"), JsonUtility.ToJson(record, prettyPrint: true));

        Debug.Log($"[RuntimeEntityIO] Created '{typeId}' at {scriptPath}");
        return ToEntityDefinition(record);
    }

    /// <summary>Loads every runtime entity definition already saved on disk
    /// from a previous session, calling onLoaded once per one - used by
    /// EntityCatalog at startup, alongside its Resources.LoadAll scan.</summary>
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
        def.Category = record.Category;
        def.IsSpawnPoint = record.IsSpawnPoint;
        def.Backing = EntityBackingKind.ScriptBehavior;
        def.Script = scriptAsset;

        if (!string.IsNullOrEmpty(record.IconPath))
        {
            def.Icon = Resources.Load<Sprite>(record.IconPath);
            if (def.Icon == null)
                Debug.LogWarning($"[RuntimeEntityIO] '{record.TypeId}': no Sprite found at Resources path '{record.IconPath}'.");
        }

        return def;
    }

    // Kept as one literal block (not composed from per-intrinsic doc
    // comments in TailboundIntrinsics.cs) so it reads as a single cheatsheet
    // in the order a script author actually needs it - keep this in sync by
    // hand whenever an intrinsic there is added, renamed, or removed.
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
