using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "Tileset", menuName = "Level Editor/Tileset")]
public class Tileset : SerializedScriptableObject
{
    public List<TileDef> Tiles = new();

    public TileDef FindTile(string id)
    {
        return Tiles.Find(t => t.Id == id);
    }
}
