using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    public Animator transition;

    public void Reload_level()
    {
        StartCoroutine(ReloadLevel());
    }

    public void Reload_level_no_transition()
    {
        StartCoroutine(ReloadLevelNoTransition());
    }

    public void Finish_Level(int level)
    {
        StartCoroutine(FinishLevel(level));
    }

    public static void Start_Level(int level)
    {
        SceneManager.LoadScene(level);
    }

    IEnumerator ReloadLevel()
    {
        transition.SetTrigger("Restart");

        yield return new WaitForSeconds(1);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator ReloadLevelNoTransition()
    {
        yield return new WaitForSeconds(1);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator FinishLevel(int level)
    {
        transition.SetTrigger("Restart");

        yield return new WaitForSeconds(1);

        SceneManager.LoadScene(level);
    }
}
