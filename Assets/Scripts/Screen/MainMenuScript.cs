using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MainMenuScript : MonoBehaviour
{
    public void NewGame()
    {
        Checkpoint.currentScene = 1;
        Checkpoint.currentCheckpointPosition = new Vector3(0, 0, 0);
        Checkpoint.bananaPositions = new List<Vector3>();
        Checkpoint.score = 0;
        LevelLoader.StartLevel(1);
    }

    public void ContinueGame()
    {
        PlayerData data = SaveSystem.LoadGame();
        Checkpoint.currentScene = data.currentScene;
        Checkpoint.currentCheckpointPosition = new Vector3(data.position[0], data.position[1], data.position[2]);
        Checkpoint.bananaPositions = new List<Vector3>();
        Checkpoint.score = data.score;
        foreach (float[] elem in data.bananaPositions)
        {
            Checkpoint.bananaPositions.Add(new Vector3(elem[0], elem[1], elem[2]));
        }
        LevelLoader.StartLevel(Checkpoint.currentScene);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
