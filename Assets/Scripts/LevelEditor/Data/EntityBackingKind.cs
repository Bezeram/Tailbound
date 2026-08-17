/// <summary>
/// What an EntityDefinition resolves to at instantiation time, and where its
/// editable property schema comes from - never authored by hand on the
/// EntityDefinition itself (see EntityDefinition's own comment).
/// </summary>
public enum EntityBackingKind
{
    /// <summary>Wraps a whole hand-built prefab (e.g. Spring, Zipline) as a
    /// single opaque unit. Its editable properties come from whichever
    /// INativePrefabAdapter targets a component already present on Prefab -
    /// see NativePrefabAdapterRegistry - not from anything authored here.</summary>
    NativePrefab,

    /// <summary>A MiniScript-authored behavior. Reserved for a future pass -
    /// MiniScript isn't implemented yet, so this entity type has no
    /// properties to expose or build from until it is. Its exposed variables
    /// will eventually come from the script itself, not this asset.</summary>
    ScriptBehavior,
}
