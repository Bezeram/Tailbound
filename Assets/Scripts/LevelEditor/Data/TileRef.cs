using System;

/// <summary>
/// A painted cell's reference into a <see cref="Tileset"/>'s <see cref="TileDef"/>
/// by stable string id, resolved by scanning the level's UsedTilesets.
/// </summary>
[Serializable]
public struct TileRef
{
    public string TileId;

    public TileRef(string tileId)
    {
        TileId = tileId;
    }

    public bool IsEmpty => string.IsNullOrEmpty(TileId);
}
