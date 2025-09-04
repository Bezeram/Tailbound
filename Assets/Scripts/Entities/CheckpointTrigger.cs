using System.Linq;
using Sirenix.OdinInspector;
using TarodevController;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class CheckpointTrigger : MonoBehaviour
{
    [FormerlySerializedAs("ScreenArea")] [TitleGroup("Input"), ReadOnly] public ScreenBox ScreenBox;
    
    private BoxCollider2D _Collider;
    private float _UpdateTimer;
    
    void UpdateScreenParent()
    {
        // Do not run while in prefab editing mode.
        if (_Collider == null)
            _Collider = GetComponent<BoxCollider2D>();
        
        // Find the closest screen to attach to.
        var screens = FindObjectsByType<ScreenBox>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (screens.Length == 0)
            return;
        
        // Compute screens bounds to determine its closest point and thus the shortest distance.  
        int minIndex = 0;
        var minBounds = new Bounds(screens[0].Center, screens[0].Size);
        float minDistance = Vector3.Distance(minBounds.ClosestPoint(transform.position), transform.position);
        for (int i = 1; i < screens.Length; i++)
        {
            var bounds = new Bounds(screens[i].Center, screens[i].Size);
            float distance = Vector3.Distance(bounds.ClosestPoint(transform.position), transform.position);
            
            if (distance < minDistance)
            {
                minIndex = i;
                minDistance = distance;
            }
        }
        
        ScreenBox = screens[minIndex];
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (ScreenBox == null)
        {
            Debug.LogWarning("Entered a Checkpoint trigger without a ScreenArea parent!", context: this);
            return;
        }
        if (collision.gameObject.layer != LayerMask.NameToLayer("Player")) 
            return;
        
        // Locate the closest RespawnPoint
        var spawnPoints = transform.parent.GetComponentsInChildren<SpawnPoint>();
        if (spawnPoints.Length == 0)
        {
            // ScreenArea already throws error
            return;
        }
            
        // Get the closest respawn point and set the spawn point.
        // Compute screens bounds to determine its closest point and thus the shortest distance.
        Vector3 playerPosition = collision.transform.position;
        int minIndex = 0;
        float minDistance = Vector3.Distance(playerPosition, spawnPoints[0].transform.position);
        for (int i = 1; i < spawnPoints.Length; i++)
        {
            float distance = Vector3.Distance(playerPosition, spawnPoints[i].transform.position);
            
            if (distance < minDistance)
            {
                minIndex = i;
                minDistance = distance;
            }
        }
        
        ScreenBox.CurrentSpawnPoint = spawnPoints[minIndex];
    }

    void Update()
    {
        // Do not run while in prefab editing mode.
        if (EditorUtility.IsPersistent(this)) 
            return;
        if (PrefabUtility.IsPartOfPrefabAsset(this))
            return;
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            return;
        if (Application.isPlaying)
            return;
        
        _UpdateTimer += Time.deltaTime;
        if (_UpdateTimer >= 0.5f)
        {
            // Set parent to the screen area.
            if (ScreenBox != null)
            {
                Transform screenContent = ScreenBox.transform.Find("Content");
                transform.SetParent(screenContent);
            }
            
            UpdateScreenParent();
            _UpdateTimer -= 0.5f;
        }
    }
    
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Vector3 center = transform.position + new Vector3(col.offset.x, col.offset.y, 0);
        Gizmos.DrawWireCube(center, col.size);
    }
}
