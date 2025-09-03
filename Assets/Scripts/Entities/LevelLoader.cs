using System.Collections;
using TarodevController;
using UnityEngine;
using UnityEngine.Rendering;

public class LevelLoader : MonoBehaviour
{
    private static readonly int AnimationTriggerStart = Animator.StringToHash("Start");
    private static readonly int AnimationTriggerEnd = Animator.StringToHash("End");
    
    public Animator Transition;
    public PlayerController Player;
    public LevelManager LevelManager;
    public AudioClip RespawnAudioClip;
    
    private AudioSource _AudioSource;
    [SerializeField] private float _SoundVolume = 0.3f;

    void OnValidate()
    {
        Player = FindAnyObjectByType<PlayerController>();
        LevelManager = FindAnyObjectByType<LevelManager>();
        _AudioSource = Player.GetComponentInChildren<AudioSource>();
    }

    public void RespawnPlayer(bool instantDeath)
    {
        Player.Kill(instantDeath);
        StartCoroutine(_Respawn(instantDeath));
    }

    IEnumerator _Respawn(bool instantDeath)
    {
        Transition.SetTrigger(AnimationTriggerStart);
        
        yield return new WaitForSeconds(1);
        
        _ResetScreenForRespawn(instantDeath);
        
        yield return new WaitForSeconds(0.1f);
        
        _AudioSource.PlayOneShot(RespawnAudioClip, _SoundVolume);
        Transition.SetTrigger(AnimationTriggerEnd);
        
        yield return new WaitForSeconds(0.1f);
        
        Player.Respawn(LevelManager.CurrentSpawnPosition);
    }

    void _ResetScreenForRespawn(bool instantDeath)
    {
        
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
        UnityEngine.SceneManagement.SceneManager.LoadScene(level);
    }

    IEnumerator Load_Level(int level)
    {
        Transition.SetTrigger(AnimationTriggerStart);

        yield return new WaitForSeconds(1);

        UnityEngine.SceneManagement.SceneManager.LoadScene(level);
    }

    IEnumerator Reload_Level_No_Transition()
    {
        yield return new WaitForSeconds(1);
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    IEnumerator Finish_Level(int level)
    {
        Transition.SetTrigger(AnimationTriggerStart);

        yield return new WaitForSeconds(1);

        UnityEngine.SceneManagement.SceneManager.LoadScene(level);
    }
}
