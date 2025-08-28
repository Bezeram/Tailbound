using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public int currentScene;
    public float[] position = new float[4];
    public List<float[]> bananaPositions;

    public PlayerData()
    {
        currentScene = Checkpoint.currentScene;
        position[0] = Checkpoint.currentCheckpointPosition.x;
        position[1] = Checkpoint.currentCheckpointPosition.y;
        position[2] = Checkpoint.currentCheckpointPosition.z;
        bananaPositions = new List<float[]>();
        foreach (Vector3 elem in Checkpoint.bananaPositions)
        {
            float[] tempArray = new float[4];
            tempArray[0] = elem.x;
            tempArray[1] = elem.y;
            tempArray[2] = elem.z;
            bananaPositions.Add(tempArray);
        }
    }
}
