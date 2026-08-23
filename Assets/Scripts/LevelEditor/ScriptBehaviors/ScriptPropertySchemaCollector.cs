using System.Collections.Generic;
using Miniscript;
using UnityEngine;

public class ScriptPropertySchemaCollector : IScriptPropertySource, IExposePropertyHost
{
    private const double CollectionTimeBudget = 0.25;

    private List<PropertyDef> _Collected;

    public List<PropertyDef> GetExposedProperties(EntityDefinition definition)
    {
        _Collected = new List<PropertyDef>();

        if (definition == null || definition.Script == null)
            return _Collected;

        TailboundIntrinsics.EnsureRegistered();

        var interpreter = new Interpreter(definition.Script.text)
        {
            hostData = this,
            standardOutput = (string s, bool lineBreak) => { }, // silent during schema collection
            errorOutput = (string message, bool lineBreak) => Debug.LogWarning(
                $"[MiniScript] Error collecting properties from '{definition.TypeId}': {message}"),
        };
        interpreter.RunUntilDone(CollectionTimeBudget, returnEarly: true);

        return _Collected;
    }

    public PropertyValue OnExpose(string key, PropertyValue defaultValue)
    {
        _Collected.Add(new PropertyDef
        {
            Key = key,
            Type = defaultValue.Type,
            DefaultValue = defaultValue,
            Label = key,
        });

        return defaultValue;
    }
}
