public static class ScriptPropertyResolver
{
    public const string AdapterId = "Script";

    public static PropertyValue GetEffectiveValue(EntityInstance instance, PropertyDef propDef)
    {
        if (instance.ComponentOverrides.TryGetValue(AdapterId, out var overrides)
            && overrides.TryGetValue(propDef.Key, out var overrideValue))
            return overrideValue;

        return propDef.DefaultValue;
    }
}
