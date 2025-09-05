using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [TitleGroup("References")]
    public GameObject PauseMenuUI;
    public BananaChannel BananaChannel;

    [SerializeField] private ScreenBox _StartScreen;
    [SerializeField] private bool _UseSaveFile = true;
    
    // Store IDs to banana positions.
    private readonly List<int> _CollectedBananas = new();
    [ReadOnly, SerializeField] private ScreenBox[] _Screens;
    [ReadOnly, SerializeField] private CollectableBanana[] _Bananas;
    [ReadOnly, SerializeField] private SpawnPoint[] _SpawnPoints;
    [ReadOnly, SerializeField] private int _CurrentScreenID;

    private PlayerController _PlayerController;
    private LevelLoader _LevelLoader;
    private CameraFollow _CameraFollow;
    private GameObject _MainCamera;
    
    private static bool _isPaused;
    public static bool IsPaused => _isPaused;
    
    private int _LastScreenID = -1;
    public int NewScreenID() { _LastScreenID++; return _LastScreenID; }
    private int _LastBananaID = -1;
    public int NewBananaID() { _LastBananaID++; return _LastBananaID; }
    private int _LastSpawnPointID = -1;
    public int NewSpawnPointID() { _LastSpawnPointID++; return _LastSpawnPointID; }
    
    public ScreenBox CurrentScreen => _Screens[_CurrentScreenID];
    public ScreenBox TransitionPreviousScreen => _Screens[_TransitionLastScreenID];
    public Vector3 CurrentSpawnPosition => _Screens[_CurrentScreenID].CurrentSpawnPosition;

    [ReadOnly, SerializeField] private bool _TransitioningScreens;
    [SerializeField] private float _TransitionTime;
    [SerializeField] private float _TransitionMoveScalarHorizontal = 1;
    [SerializeField] private float _TransitionMoveScalarDownwards = 1.3f;
    [SerializeField] private float _TransitionMoveScalarUpwards = 2;
    private float _TransitionTimer;
    private Vector3 _TransitionLastPlayerPosition;
    private Vector3 _TransitionNextPlayerPosition;
    private Vector3 _TransitionLastCameraPosition;
    private Vector3 _TransitionNextCameraPosition;
    private int _TransitionLastScreenID;

    void OnValidate()
    {
        _LevelLoader = FindAnyObjectByType<LevelLoader>();
        _CameraFollow  = FindAnyObjectByType<CameraFollow>();
        _MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        _PlayerController = FindAnyObjectByType<PlayerController>();

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
        
        // Assign ID to each screen.
        // Disable all screens before activating the one with the player.
        foreach (var screen in _Screens)
        {
            screen.ID = NewScreenID();            
            screen.ToggleScreenContent(false);
        }
        // Assign ID to each banana
        foreach (CollectableBanana banana in _Bananas)
            banana.ID = NewBananaID();
        // Assign ID to each Spawn-Point
        foreach (SpawnPoint spawnPoint in _SpawnPoints)
            spawnPoint.ID = NewSpawnPointID();
        
        // Load and use file data
        PlayerData data = SaveSystem.LoadGame();
        if (data != null && _UseSaveFile)
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
            _CurrentScreenID = _StartScreen.ID;
        }

        // Subscribe to player death
        _PlayerController.Died += OnPlayerDeath;
        
        // Move player and clear trail renderer.
        _PlayerController.transform.position = CurrentSpawnPosition;
        _PlayerController.gameObject.GetComponentInChildren<TrailRenderer>().Clear();
        // Move camera
        _CameraFollow.Screen = CurrentScreen;
        // Set active the content of the screen used.
        CurrentScreen.ToggleScreenContent(true);
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
        HandleScreenTransition();
    }

    void HandleBananaCollected(CollectableBanana banana)
    {
        _CollectedBananas.Add(banana.ID);
    }

    void HandleScreenTransition()
    {
        if (!_TransitioningScreens)
            return;
        
        // Animate screen
        _TransitionTimer += Time.deltaTime;
        float t = _TransitionTimer / _TransitionTime;
        t = Utils.EaseOutCubic(t);
        
        // Subtly move player towards final position
        _MainCamera.transform.position = Vector3.Lerp(_TransitionLastCameraPosition, _TransitionNextCameraPosition, t);
        _PlayerController.transform.position = Vector3.Lerp(_TransitionLastPlayerPosition, _TransitionNextPlayerPosition, t);

        if (t >= 1)
        {
            _TransitioningScreens = false;
            // Disable old screen content and re-enable collider.
            TransitionPreviousScreen.ToggleScreenContent(false);
            TransitionPreviousScreen.IsTransitioning = false;
            CurrentScreen.IsTransitioning = false;
            // Set new screen.
            _CameraFollow.enabled = true;
            // Animation finished, resume game.
            Resume();
        }
    }
    
    public void RunScreenTransition(int newScreenID)
    {
        // Pause game momentarily
        PauseNoUI();
        _TransitioningScreens = true;
        // Setup old and new screen.
        _TransitionLastScreenID = _CurrentScreenID;
        _CurrentScreenID = newScreenID;
        // Deactivate old screen collider to prevent colliding during transition.
        TransitionPreviousScreen.IsTransitioning = true;
        CurrentScreen.IsTransitioning = true;
        // Activate the new screen.
        CurrentScreen.ToggleScreenContent(true);
        _CameraFollow.Screen = CurrentScreen;

        // Calculate closest wall to player
        float bottomWall = TransitionPreviousScreen.BottomLeft.y - _PlayerController.transform.position.y;
        float upWall = (TransitionPreviousScreen.BottomLeft.y + TransitionPreviousScreen.Size.y) - _PlayerController.transform.position.y;
        float leftWall = TransitionPreviousScreen.BottomLeft.x - _PlayerController.transform.position.x;
        float rightWall = (TransitionPreviousScreen.BottomLeft.x + TransitionPreviousScreen.Size.x) - _PlayerController.transform.position.x;
        bottomWall = Mathf.Abs(bottomWall);
        upWall = Mathf.Abs(upWall);
        leftWall = Mathf.Abs(leftWall);
        rightWall = Mathf.Abs(rightWall);
        float[] walls = { (bottomWall), (upWall), (leftWall), (rightWall) };
        float minDistance = walls.Min();

        Vector2 moveDirection = Vector2.one;
        if (minDistance == leftWall || minDistance == rightWall)
        {
            moveDirection.y = 0;
            moveDirection.x = minDistance == leftWall ? -1 : 1;
        }
        else
        {
            moveDirection.x = 0;
            moveDirection.y = minDistance == bottomWall ? -1 : 1;
        }
        float transitionScalar = (moveDirection.x != 0) ? _TransitionMoveScalarHorizontal :
            (moveDirection.y > 0) ? _TransitionMoveScalarUpwards : _TransitionMoveScalarDownwards;

        // Setup lerp points
        // Player
        _TransitionLastPlayerPosition = _PlayerController.transform.position;
        _TransitionNextPlayerPosition = _TransitionLastPlayerPosition + (Vector3)(moveDirection * transitionScalar);
        // Camera
        _TransitionLastCameraPosition = _CameraFollow.transform.position;
        // Get the camera position in the new screen.
        Vector2 cameraRestraint = _CameraFollow.GetCameraPosition();
        _TransitionNextCameraPosition = new Vector3(cameraRestraint.x, cameraRestraint.y, _CameraFollow.transform.position.z);
        
        // Disable camera follow script during transition
        _CameraFollow.enabled = false;
        // Stop player immediately.
        // Current speed is stored internally in another variable, so the speed is not lost.
        _PlayerController.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        
        _TransitionTimer = 0;
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
    }

    public void Resume()
    {
        PauseMenuUI.SetActive(false);
        _isPaused = false;
    }

    public void Pause()
    {
        PauseMenuUI.SetActive(true);
        _isPaused = true;
    }

    public void PauseNoUI()
    {
        _isPaused = true;
    }

    public void Restart()
    {
        Resume();
        _LevelLoader.RespawnPlayer(true);
    }

    void OnPlayerDeath(bool instantly)
    {
        _LevelLoader.RespawnPlayer(instantly);
    }

    public void Menu()
    {
        PlayerData playerData = new PlayerData(_CurrentScreenID, CurrentScreen.CurrentSpawnPoint.ID, _CollectedBananas);
        SaveSystem.SaveGame(playerData);
        Resume();
        _LevelLoader.LoadLevel(0);
    }
}
