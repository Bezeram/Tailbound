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

    /// <summary>A MiniScript-authored behavior - Script's own expose(key,
    /// defaultValue) calls declare its editable properties (see
    /// ScriptPropertySchemaCollector, TailboundIntrinsics), run at runtime by
    /// ScriptEntityRunner. Not from anything authored on this asset itself.</summary>
    ScriptBehavior,
}
