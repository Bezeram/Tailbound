using UnityEngine;
using TarodevController;

public class PauseMenuScript : MonoBehaviour
{
    public static bool isPaused =  false;
    public GameObject pauseMenuUi;
    public float TimeScale;
    
    private GameObject player;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused == true)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        TimeScale = Time.timeScale;
    }

    public void Resume()
    {
        pauseMenuUi.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }

    void Pause()
    {
        pauseMenuUi.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void Restart()
    {
        Resume();
        player.GetComponent<PlayerController>().Die();
    }

    public void Menu()
    {
        SaveSystem.SaveGame();
        Resume();
        GameObject.Find("LevelLoader").GetComponent<LevelLoader>().LoadLevel(0);
    }
}
