using System.Collections.Generic;
using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [TitleGroup("References")]
    public GameObject PauseMenuUI;
    public BananaChannel BananaChannel;
    
    // Store IDs to banana positions.
    [ReadOnly, SerializeField] private readonly List<int> _CollectedBananas = new();
    [ReadOnly, SerializeField] private ScreenArea[] _Screens;
    [ReadOnly, SerializeField] private CollectableBanana[] _Bananas;
    [ReadOnly, SerializeField] private float _TimeScale;
    [ReadOnly, SerializeField] private int _CurrentScreenID;

    private PlayerController _PlayerController;
    private LevelLoader _LevelLoader;
    
    private static bool _isPaused;
    private static int _bananaID;
    
    public static bool IsPaused => _isPaused;
    public static int NewBananaID() { _bananaID++; return _bananaID; }
    public ScreenArea CurrentScreen => _Screens[_CurrentScreenID];
    public Vector3 CurrentSpawnPosition => _Screens[_CurrentScreenID].CurrentSpawnPosition;

    void OnValidate()
    {
        _LevelLoader = FindAnyObjectByType<LevelLoader>();
        
        if (BananaChannel == null)
            Debug.LogWarning("Assign a banana channel for the Scene Manager!", context: this);
        if (PauseMenuUI == null)
            Debug.LogWarning("Assign a pause menu UI for the Scene Manager!", context: this);
    }

    void Start()
    {
        // Load objects
        _Screens = FindObjectsByType<ScreenArea>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        _Bananas = FindObjectsByType<CollectableBanana>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        // Disable all screens before activating the one with the player.
        foreach (var screen in _Screens)
            screen.gameObject.SetActive(false);
        _PlayerController = FindAnyObjectByType<PlayerController>();
        
        // Load and use data
        PlayerData data = SaveSystem.LoadGame();
        if (data != null)
        {
            _CurrentScreenID = data.ScreenID;
            CurrentScreen.SetSpawnPoint(data.SpawnPointID);
            // Stylize bananas collected in another session
            var collectedBananasIDs = data.CollectedBananaIDs;
            foreach (var id in collectedBananasIDs)
            {
                CollectableBanana banana = _Bananas[id];
                SpriteRenderer spriteRenderer = banana.GetComponent<SpriteRenderer>();
                spriteRenderer.color = new Color(0.2f, 0.2f, 1.0f, 0.5f);
            }
        }
        
        // Move player
        _PlayerController.transform.position = CurrentSpawnPosition;
        // Set active the screen one being used
        CurrentScreen.gameObject.SetActive(true);
    }
    
    void OnEnable()
    {
        BananaChannel.OnRaised += HandleBananaCollected;
    }

    void OnDisable()
    {
        BananaChannel.OnRaised -= HandleBananaCollected;
    }
    
    void Update()
    {
        HandlePausing();
    }

    void HandleBananaCollected(CollectableBanana banana)
    {
        _CollectedBananas.Add(banana.ID);
    }

    void HandlePausing()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }
        
        // Info
        _TimeScale = Time.timeScale;
    }

    void Resume()
    {
        PauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        _isPaused = false;
    }

    void Pause()
    {
        PauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        _isPaused = true;
    }

    public void Restart()
    {
        Resume();
        _PlayerController.Die();
    }

    public void Menu()
    {
        PlayerData playerData = new PlayerData(_CurrentScreenID, CurrentScreen.SpawnPointID, _CollectedBananas);
        SaveSystem.SaveGame(playerData);
        Resume();
        _LevelLoader.LoadLevel(0);
    }
}
