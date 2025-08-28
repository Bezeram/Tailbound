using System;
using System.Collections;
using System.Collections.Generic;
using TarodevController;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class LevelLoader : MonoBehaviour
{
    private static readonly int AnimationTriggerStart = Animator.StringToHash("Start");
    private static readonly int AnimationTriggerEnd = Animator.StringToHash("End");
    
    public Animator Transition;
    public PlayerController Player;
    public ScreenManager ScreenManager;

    void Awake()
    {
        Player = FindFirstObjectByType<PlayerController>();
        ScreenManager = FindFirstObjectByType<ScreenManager>();
    }

    public void Respawn()
    {
        StartCoroutine(_Respawn());
    }

    IEnumerator _Respawn()
    {
        Transition.SetTrigger(AnimationTriggerStart);
        
        yield return new WaitForSeconds(1);
        
        _ResetScreenForRespawn();
        
        Transition.SetTrigger(AnimationTriggerEnd);
        
        yield return new WaitForSeconds(1);
        
        // Respawn player after animation is finished.
        // I.e. the player may now move.
        Player.Respawn();
    }

    void _ResetScreenForRespawn()
    {
        // TODO: every entity which must be reset
        //  has its data copied from a clone representing its initial state in the screen.
        //  Also move the player to their current respawn point.
        Player.transform.position = ScreenManager.CurrentSpawnPosition;
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
        Transition.SetTrigger(AnimationTriggerStart);

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
        Transition.SetTrigger(AnimationTriggerStart);

        yield return new WaitForSeconds(1);

        SceneManager.LoadScene(level);
    }
}
