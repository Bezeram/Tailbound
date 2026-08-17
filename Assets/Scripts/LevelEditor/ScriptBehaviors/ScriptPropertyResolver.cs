/// <summary>
/// The ScriptBehavior counterpart to EntityPropertyResolver (NativePrefab).
/// There's exactly one adapter-equivalent per script entity (the script
/// itself), so unlike NativePrefabAdapterRegistry there's nothing to look
/// up - just the one shared AdapterId every ScriptBehavior entity's
/// overrides are keyed under in EntityInstance.ComponentOverrides.
/// </summary>
public static class ScriptPropertyResolver
{
    public const string AdapterId = "Script";

    /// <summary>Instance override, else the PropertyDef's DefaultValue - the
    /// value the script's own expose(key, defaultValue) call supplied when
    /// ScriptPropertySchemaCollector captured this PropertyDef.</summary>
    public static PropertyValue GetEffectiveValue(EntityInstance instance, PropertyDef propDef)
    {
        if (instance.ComponentOverrides.TryGetValue(AdapterId, out var overrides)
            && overrides.TryGetValue(propDef.Key, out var overrideValue))
            return overrideValue;

        return propDef.DefaultValue;
    }
}
