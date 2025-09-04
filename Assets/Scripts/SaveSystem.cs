using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class SaveSystem
{
    public static readonly string SaveFilePath = Application.persistentDataPath + "/gameSave.monk";
    
    public static void SaveGame(PlayerData playerData)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream = new FileStream(SaveFilePath, FileMode.Create);

        formatter.Serialize(stream, playerData);
        stream.Close();
        
        // Debug
        if (File.Exists(SaveFilePath))
            Debug.Log("Save file has been updated successfully.");
        else
            Debug.LogError("Could not create save file.");
    }

    public static PlayerData LoadGame()
    {
        if (File.Exists(SaveFilePath))
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(SaveFilePath, FileMode.Open);

            PlayerData data = formatter.Deserialize(stream) as PlayerData;
            stream.Close();
            return data;
        }
        
        Debug.Log("Save file not found");
        return null;
    }
}
