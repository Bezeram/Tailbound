using UnityEngine;
using UnityEngine.Tilemaps;

public class create_grid : MonoBehaviour
{
    [SerializeField] private GameObject _TilesPrefab;
    [SerializeField] private Tile _Tile;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var gridGO = Instantiate(_TilesPrefab);
        gridGO.name = "Tiles";
        
        var backgroundTilemap = gridGO.transform.Find("Foreground").GetComponent<Tilemap>();
        
        for (int i = 0; i < 10; i++)
            backgroundTilemap?.SetTile(new Vector3Int(i, 0, 0), _Tile);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
