using System.Collections.Generic;

public static class NativeComponentBinderRegistry
{
    private static readonly Dictionary<string, INativeComponentBinder> _Binders = new()
    {
        { "SpriteRenderer", new SpriteRendererBinder() },
        { "BoxCollider2D", new BoxCollider2DBinder() },
        { "AudioSource", new AudioSourceBinder() },
    };

    public static bool TryGet(string componentTypeId, out INativeComponentBinder binder)
    {
        return _Binders.TryGetValue(componentTypeId, out binder);
    }

    public static IEnumerable<string> RegisteredTypeIds => _Binders.Keys;
}
