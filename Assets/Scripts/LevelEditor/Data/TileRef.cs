using System;

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
