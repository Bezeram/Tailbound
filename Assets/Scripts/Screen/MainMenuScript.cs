using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MainMenuScript : MonoBehaviour
{
    public void NewGame()
    {
        // TODO: Reset player data
        
        StartGame();
    }

    public void StartGame()
    {
        LevelLoader.StartLevel(1);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
