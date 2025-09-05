using UnityEngine;
using System.IO;

public class MainMenuScript : MonoBehaviour
{
    public void NewGame()
    {
        // Delete save file
        File.Delete(SaveSystem.SaveFilePath); 
        StartGame("Level_1");
    }

    public void Continue()
    {
        StartGame("Level_1");
    }

    public void Exit()
    {
        Application.Quit();
    }
    
    void StartGame(string level)
    {
        LevelLoader.StartLevel(level);
    }
}
