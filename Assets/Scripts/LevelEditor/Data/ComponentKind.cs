/// <summary>
/// What a <see cref="ComponentSpec"/> resolves to at instantiation time.
/// </summary>
public enum ComponentKind
{
    /// <summary>Wraps a whole hand-built prefab (e.g. Zipline, Spring) as a single opaque unit.</summary>
    NativePrefab,

    /// <summary>A curated 1:1 adapter over a real Unity component type (see INativeComponentBinder).</summary>
    NativeUnityComponent,

    /// <summary>A MiniScript-authored behavior. Reserved for a future pass.</summary>
    ScriptBehavior,
}
