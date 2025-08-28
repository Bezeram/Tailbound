using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;
    public PlayerController Player;
    public ScreenManager ScreenManager;

    void Awake()
    {
        Player = FindFirstObjectByType<PlayerController>();
        ScreenManager = FindFirstObjectByType<ScreenManager>();
    }

    public IEnumerator Respawn()
    {
        transition.SetTrigger("Restart");
        
        yield return new WaitForSeconds(1);
        
        StartCoroutine(_ResetScreenForRespawn());
    }

    IEnumerator _ResetScreenForRespawn()
    {
        // TODO: every entity which must be reset
        //  has its data copied from a clone representing its initial state in the screen.
        //  Also move the player to their current respawn point.
        Player.transform.position = ScreenManager.CurrentSpawnPosition;

        yield return null;
    }

    public void LoadLevel(int level)
    {
        StartCoroutine(Load_Level(level));
    }

    public void ReloadLevelNoTransition()
    {
        StartCoroutine(Reload_Level_No_Transition());
    }

    public void FinishLevel(int level)
    {
        StartCoroutine(Finish_Level(level));
    }

    public static void StartLevel(int level)
    {
        SceneManager.LoadScene(level);
    }

    IEnumerator Load_Level(int level)
    {
        transition.SetTrigger("Restart");

        yield return new WaitForSeconds(1);

        SceneManager.LoadScene(level);
    }

    IEnumerator Reload_Level_No_Transition()
    {
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator Finish_Level(int level)
    {
        transition.SetTrigger("Restart");

        yield return new WaitForSeconds(1);

        SceneManager.LoadScene(level);
    }
}
