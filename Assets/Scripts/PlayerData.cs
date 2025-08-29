using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Serialization;

[System.Serializable]
public class PlayerData
{
    public int ScreenID;
    public int SpawnPointID;
    public List<int> CollectedBananaIDs;

    public PlayerData(int screenID, int spawnPointID, List<int> collectedBananasIDs)
    {
        ScreenID = screenID;
        SpawnPointID = spawnPointID;
        // Serialize Vector3 array in a bidimensional primitive type array.
        CollectedBananaIDs = collectedBananasIDs.ToList();
    }
}
