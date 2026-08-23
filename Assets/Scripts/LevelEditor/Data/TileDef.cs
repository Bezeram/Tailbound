using System;
using Sirenix.OdinInspector;
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

    // EnumToggleButtons, not a dropdown - Odin's dropdown popup crashes on
    // this Unity version (MissingMethodException from a removed UIElements API).
    [EnumToggleButtons]
    public TileCollisionType CollisionType = TileCollisionType.None;
    public string Category;
}
