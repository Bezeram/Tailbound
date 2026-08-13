using System.Collections.Generic;

/// <summary>
/// Lookup from ComponentTypeId (e.g. "SpriteRenderer") to its binder.
/// Manually registered rather than reflection-scanned, on purpose - the set
/// of exposed native components should be a deliberate choice, not
/// whatever happens to implement the interface.
/// </summary>
public static class NativeComponentBinderRegistry
{
    private static readonly Dictionary<string, INativeComponentBinder> _Binders = new()
    {
        { "SpriteRenderer", new SpriteRendererBinder() },
        { "BoxCollider2D", new BoxCollider2DBinder() },
    };

    public static bool TryGet(string componentTypeId, out INativeComponentBinder binder)
    {
        return _Binders.TryGetValue(componentTypeId, out binder);
    }

    public static IEnumerable<string> RegisteredTypeIds => _Binders.Keys;
}
