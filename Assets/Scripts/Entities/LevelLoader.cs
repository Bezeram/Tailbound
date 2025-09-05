using System.Collections;
using TarodevController;
using UnityEngine;
using UnityEngine.Rendering;

public class LevelLoader : MonoBehaviour
{
    private static readonly int AnimationTriggerStart = Animator.StringToHash("Start");
    private static readonly int AnimationTriggerEnd = Animator.StringToHash("End");
    
    public Animator Transition;
    public AudioClip RespawnAudioClip;
    [SerializeField] private float _SoundVolume = 0.3f;

    private PlayerController _Player;
    private LevelManager _LevelManager;
    private AudioSource _AudioSource;

    void Start()
    {
        _Player = FindAnyObjectByType<PlayerController>();
        _LevelManager = FindAnyObjectByType<LevelManager>();
        _AudioSource = _Player.GetComponentInChildren<AudioSource>();
    }

    public void RespawnPlayer(bool instantDeath)
    {
        _Player.Kill(instantDeath);
        StartCoroutine(RespawnCoroutine(instantDeath));
    }

    IEnumerator RespawnCoroutine(bool instantDeath)
    {
        Transition.SetTrigger(AnimationTriggerStart);
        
        yield return new WaitForSeconds(1);
        
        _ResetScreenForRespawn(instantDeath);
        
        yield return new WaitForSeconds(0.1f);
        
        _AudioSource.PlayOneShot(RespawnAudioClip, _SoundVolume);
        Transition.SetTrigger(AnimationTriggerEnd);
        
        yield return new WaitForSeconds(0.1f);
        
        _Player.Respawn(_LevelManager.CurrentSpawnPosition);
    }

    void _ResetScreenForRespawn(bool instantDeath)
    {
        
    }

    public void LoadLevel(string level)
    {
        StartCoroutine(LoadLevelCoroutine(level));
    }

    public void ReloadLevelNoTransition()
    {
        StartCoroutine(Reload_Level_No_Transition());
    }

    public void FinishLevel(string level)
    {
        StartCoroutine(Finish_Level(level));
    }

    public static void StartLevel(string level)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(level);
    }

    IEnumerator LoadLevelCoroutine(string level)
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

    IEnumerator Finish_Level(string level)
    {
        Transition.SetTrigger(AnimationTriggerStart);

        yield return new WaitForSeconds(1);

        UnityEngine.SceneManagement.SceneManager.LoadScene(level);
    }
}
