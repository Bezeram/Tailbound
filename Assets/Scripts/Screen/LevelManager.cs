using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using TarodevController;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [TitleGroup("References")]
    public BananaChannel BananaChannel;

    [SerializeField] private ScreenBox _StartScreen;
    [SerializeField] private bool _UseSaveFile = true;

    public void SetStartScreen(ScreenBox screen)
    {
        _StartScreen = screen;
    }

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

    // Called from OnValidate and Start; safe either way since Awake()
    // always runs before Start().
    void CacheReferences()
    {
        _LevelLoader = FindAnyObjectByType<LevelLoader>();
        _CameraFollow  = FindAnyObjectByType<CameraFollow>();
        _MainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        _PlayerController = FindAnyObjectByType<PlayerController>();
    }

    void OnValidate()
    {
        CacheReferences();

        if (BananaChannel == null)
            Debug.LogWarning("Assign a banana channel for the Scene Manager!", context: this);
    }

    void Start()
    {
        CacheReferences();

        _Screens = FindObjectsByType<ScreenBox>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        _Bananas = FindObjectsByType<CollectableBanana>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
        _SpawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

        // Disable all screens before activating the one with the player.
        foreach (var screen in _Screens)
        {
            screen.ID = NewScreenID();
            screen.ToggleScreenContent(false);
        }
        foreach (CollectableBanana banana in _Bananas)
            banana.ID = NewBananaID();
        foreach (SpawnPoint spawnPoint in _SpawnPoints)
            spawnPoint.ID = NewSpawnPointID();

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

        _PlayerController.Died += OnPlayerDeath;

        _PlayerController.transform.position = CurrentSpawnPosition;
        _PlayerController.gameObject.GetComponentInChildren<TrailRenderer>().Clear();
        _CameraFollow.Screen = CurrentScreen;
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
            _CameraFollow.enabled = true;
            // Animation finished - resume game.
            Resume();
        }
    }

    public void RunScreenTransition(int newScreenID)
    {
        PauseNoUI();
        _TransitioningScreens = true;
        _TransitionLastScreenID = _CurrentScreenID;
        _CurrentScreenID = newScreenID;
        // Deactivate old screen collider to prevent colliding during transition.
        TransitionPreviousScreen.IsTransitioning = true;
        CurrentScreen.IsTransitioning = true;
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
        _TransitionLastPlayerPosition = _PlayerController.transform.position;
        _TransitionNextPlayerPosition = _TransitionLastPlayerPosition + (Vector3)(moveDirection * transitionScalar);
        _TransitionLastCameraPosition = _CameraFollow.transform.position;
        Vector2 cameraRestraint = _CameraFollow.GetCameraPosition();
        _TransitionNextCameraPosition = new Vector3(cameraRestraint.x, cameraRestraint.y, _CameraFollow.transform.position.z);

        _CameraFollow.enabled = false;
        // Stop immediately - current speed is preserved in a separate variable, not lost.
        _PlayerController.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        _TransitionTimer = 0;
    }

    public void Resume()
    {
        _isPaused = false;
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
        _LevelLoader.LoadLevel("Start Menu");
    }
}
