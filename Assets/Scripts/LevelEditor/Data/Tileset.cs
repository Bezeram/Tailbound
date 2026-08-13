using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// A pack of paintable tiles, usually one asset per art source (e.g. one
/// tileset image or Rule Tile family). A <see cref="LevelAsset"/> references
/// the Tilesets it paints from via its UsedTilesets list.
/// </summary>
[CreateAssetMenu(fileName = "Tileset", menuName = "Level Editor/Tileset")]
public class Tileset : SerializedScriptableObject
{
    public List<TileDef> Tiles = new();

    public TileDef FindTile(string id)
    {
        return Tiles.Find(t => t.Id == id);
    }
}
