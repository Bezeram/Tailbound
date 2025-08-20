using UnityEngine;
using TarodevController;

public class Pause_Menu_Script : MonoBehaviour
{
    public static bool is_paused =  false;
    public GameObject pause_menu_ui;
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
            if (is_paused == true)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }    
    }

    public void Resume()
    {
        pause_menu_ui.SetActive(false);
        Time.timeScale = 1f;
        is_paused = false;
    }

    void Pause()
    {
        pause_menu_ui.SetActive(true);
        Time.timeScale = 0f;
        is_paused = true;
    }

    public void Restart()
    {
        Resume();
        player.GetComponent<PlayerController>().Die();
    }

    public void Menu()
    {
        Debug.Log("LOAD MENU");
    }
}
