using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    public void NewGame()
    {
        Checkpoint.currentScene = 1;
        Checkpoint.currentCheckpointPosition = new Vector3(0, 0, 0);
        LevelLoader.StartLevel(1);
    }

    public void ContinueGame()
    {
        LevelLoader.StartLevel(Checkpoint.currentScene);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
