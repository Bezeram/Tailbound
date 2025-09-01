using TarodevController;
using UnityEngine;

[ExecuteAlways]
public class DeathBox : MonoBehaviour
{
    private ScreenBox _ParentScreen;
    private LevelLoader _LevelLoader;
    private BoxCollider2D _DeathCollider;
    private PlayerController _PlayerController;

    void OnValidate()
    {
        _ParentScreen = transform.parent.GetComponent<ScreenBox>();
        _DeathCollider = GetComponent<BoxCollider2D>();
        _PlayerController = FindAnyObjectByType<PlayerController>();

        if (_ParentScreen == null)
            Debug.LogError("DeathBox is not a direct child of a ScreenBox!", context: this);
    }
    
    void UpdateDeathCollider()
    {
        _DeathCollider.offset = _ParentScreen.Size / 2;
        _DeathCollider.size = _ParentScreen.Size;
    }

    void Start()
    {
        _LevelLoader = GameObject.Find("LevelLoader").GetComponent<LevelLoader>();
    }

    private float _TimerUpdate;
    
    void Update()
    {
        if (Application.isPlaying)
            return;
        
        // Update collider every once in a while
        _TimerUpdate += Time.deltaTime;
        while (_TimerUpdate >= 0.1)
        {
            _TimerUpdate -= 0.1f;
            UpdateDeathCollider();
        }
    }
    
    void OnTriggerExit2D(Collider2D collision)
    {
        if (_ParentScreen.IsTransitioning)
            return;
        
        if (Utils.IsInMask(collision.gameObject.layer, _ParentScreen.PlayerLayer))
        {
            // Left below the screen
            if (_PlayerController.transform.position.y < transform.position.y)
            {
                // Death
                _LevelLoader.RespawnPlayerInstant();
            }
        }
    }
}
