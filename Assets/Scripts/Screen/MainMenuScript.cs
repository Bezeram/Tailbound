using UnityEngine;
using System.IO;

public class MainMenuScript : MonoBehaviour
{
    public void NewGame()
    {
        // Delete save file
        File.Delete(SaveSystem.SaveFilePath); 
        StartGame(1);
    }

    public void Continue()
    {
        StartGame(1);
    }

    public void Exit()
    {
        Application.Quit();
    }
    
    void StartGame(int level)
    {
        LevelLoader.StartLevel(level);
    }
}
