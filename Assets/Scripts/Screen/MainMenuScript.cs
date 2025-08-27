using UnityEngine;

public class MainMenuScript : MonoBehaviour
{
    public void NewGame()
    {
        LevelLoader.StartLevel(1);
    }

    public void ContinueGame()
    {
        LevelLoader.StartLevel(1);
    }

    public void Exit()
    {
        Application.Quit();
    }
}
