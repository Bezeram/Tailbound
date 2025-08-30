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
    private readonly List<int> _CollectedBananas = new();
    [ReadOnly, SerializeField] private ScreenBox[] _Screens;
    [ReadOnly, SerializeField] private CollectableBanana[] _Bananas;
    [ReadOnly, SerializeField] private SpawnPoint[] _SpawnPoints;
    [ReadOnly, SerializeField] private float _TimeScale;
    [ReadOnly, SerializeField] private int _CurrentScreenID;

    private PlayerController _PlayerController;
    private LevelLoader _LevelLoader;
    
    private static bool _isPaused;
    public static bool IsPaused => _isPaused;
    
    private int _LastScreenID = -1;
    public int NewScreenID() { _LastScreenID++; return _LastScreenID; }
    private int _LastBananaID = -1;
    public int NewBananaID() { _LastBananaID++; return _LastBananaID; }
    private int _LastSpawnPointID = -1;
    public int NewSpawnPointID() { _LastSpawnPointID++; return _LastSpawnPointID; }
    
    public ScreenBox CurrentScreen => _Screens[_CurrentScreenID];
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
        _Screens = FindObjectsByType<ScreenBox>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        _Bananas = FindObjectsByType<CollectableBanana>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        _SpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        _PlayerController = FindAnyObjectByType<PlayerController>();
        // Assign ID to each screen.
        // Disable all screens before activating the one with the player.
        foreach (var screen in _Screens)
        {
            screen.ID = NewScreenID();            
            screen.gameObject.SetActive(false);
        }
        // Assign ID to each banana
        foreach (CollectableBanana banana in _Bananas)
            banana.ID = NewBananaID();
        // Assign ID to each Spawn-Point
        foreach (SpawnPoint spawnPoint in _SpawnPoints)
            spawnPoint.ID = NewSpawnPointID();
        
        // Load and use file data
        PlayerData data = SaveSystem.LoadGame();
        if (data != null)
        {
            _CurrentScreenID = data.ScreenID;
            CurrentScreen.CurrentSpawnPoint = _SpawnPoints[data.SpawnPointID];
            // Stylize bananas collected in another session
            var collectedBananasIDs = data.CollectedBananaIDs;
            foreach (var id in collectedBananasIDs)
            {
                CollectableBanana banana = _Bananas[id];
                SpriteRenderer spriteRenderer = banana.GetComponent<SpriteRenderer>();
                spriteRenderer.color = new Color(0.2f, 0.2f, 1.0f, 0.9f);
            }
        }
        else
        {
            // Assume new game
            _CurrentScreenID = 0;
            CurrentScreen.CurrentSpawnPoint = CurrentScreen.FirstSpawnPoint;
        }
        
        // Move player and clear trail renderer.
        _PlayerController.transform.position = CurrentSpawnPosition;
        _PlayerController.gameObject.GetComponentInChildren<TrailRenderer>().Clear();
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

    public void Resume()
    {
        PauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        _isPaused = false;
    }

    public void Pause()
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
        PlayerData playerData = new PlayerData(_CurrentScreenID, CurrentScreen.CurrentSpawnPoint.ID, _CollectedBananas);
        SaveSystem.SaveGame(playerData);
        Resume();
        _LevelLoader.LoadLevel(0);
    }
}
