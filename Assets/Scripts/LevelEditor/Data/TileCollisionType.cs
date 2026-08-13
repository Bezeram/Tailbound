/// <summary>
/// Gameplay meaning of a tile when painted on the Foreground layer. Ignored
/// on the Background layer, which is always purely visual. Determines which
/// Unity physics components the runtime instantiator builds for a screen's
/// foreground tiles (solid vs. one-way vs. hazard need different colliders).
/// </summary>
public enum TileCollisionType
{
    None,
    Solid,
    OneWayPlatform,
    Hazard,
    Ladder,
}
