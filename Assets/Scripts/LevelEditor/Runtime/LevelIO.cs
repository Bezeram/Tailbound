using System.Collections.Generic;
using System.IO;
using Sirenix.Serialization;
using UnityEngine;

/// <summary>
/// Runtime save/load for LevelAsset. No AssetDatabase or ScriptableObject
/// asset involved - levels are plain JSON files on disk, written via Odin's
/// runtime-capable serializer (the one serializer already in the project
/// that round-trips the Dictionary-heavy data model without extra work).
/// </summary>
public static class LevelIO
{
    private const string FileExtension = ".level.json";

    private static string LevelsDirectory => Path.Combine(Application.persistentDataPath, "Levels");

    public static void Save(LevelAsset level, string name)
    {
        try
        {
            Directory.CreateDirectory(LevelsDirectory);

            byte[] bytes = SerializationUtility.SerializeValue(level, DataFormat.JSON);
            string path = GetPath(name);
            File.WriteAllBytes(path, bytes);

            Debug.Log($"[LevelIO] Wrote {bytes.Length} bytes to {path}");
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[LevelIO] Failed to save level '{name}': {exception}");
        }
    }

    public static LevelAsset Load(string name)
    {
        string path = GetPath(name);
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[LevelIO] No file at {path}");
            return null;
        }

        try
        {
            byte[] bytes = File.ReadAllBytes(path);
            Debug.Log($"[LevelIO] Read {bytes.Length} bytes from {path}");
            return SerializationUtility.DeserializeValue<LevelAsset>(bytes, DataFormat.JSON);
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"[LevelIO] Failed to load level '{name}': {exception}");
            return null;
        }
    }

    public static List<string> ListLevels()
    {
        var names = new List<string>();

        if (!Directory.Exists(LevelsDirectory))
            return names;

        foreach (string path in Directory.GetFiles(LevelsDirectory, "*" + FileExtension))
        {
            string fileName = Path.GetFileName(path);
            names.Add(fileName.Substring(0, fileName.Length - FileExtension.Length));
        }

        return names;
    }

    private static string GetPath(string name)
    {
        return Path.Combine(LevelsDirectory, name + FileExtension);
    }
}
