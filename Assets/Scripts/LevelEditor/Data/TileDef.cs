using System;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class TileDef
{
    [Tooltip("Stable key referenced by TileRef. Keep unchanged once tiles have been painted with it.")]
    public string Id;
    public string DisplayName;

    [Tooltip("Use either a plain Sprite for simple/decorative tiles, or a RuleTile for autotiled terrain.")]
    public Sprite Sprite;
    public RuleTile RuleTile;

    public string Category;
}
