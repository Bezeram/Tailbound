using System.Collections.Generic;
using Miniscript;
using UnityEngine;

/// <summary>
/// Implements IScriptPropertySource by actually running the script - once,
/// in a throwaway Interpreter, purely to collect its expose(key, default)
/// calls into a PropertyDef list. No separate declaration syntax to parse
/// or keep in sync with what the script actually calls; the real MiniScript
/// interpreter (already vendored) reads its own source.
///
/// Scripts are expected to call expose() for everything they want editable
/// before entering any long-running loop - CollectionTimeBudget bounds how
/// long a dry run can take if a script loops before ever hitting a wait()/
/// yield (both are intrinsics that suspend execution, which is what lets
/// RunUntilDone return promptly for a well-behaved script).
/// </summary>
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
