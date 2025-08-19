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

}
