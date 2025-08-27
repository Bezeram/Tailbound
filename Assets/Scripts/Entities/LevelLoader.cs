using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;

    public void ReloadLevel()
    {
        StartCoroutine(Load_Level(SceneManager.GetActiveScene().buildIndex));
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
