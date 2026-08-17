using Miniscript;
using UnityEngine;

/// <summary>
/// Custom MiniScript intrinsics bridging ScriptBehavior entities to the
/// Tailbound property system and a minimal set of Unity operations. Kept in
/// its own file rather than touching the vendored MiniScript source, per
/// MiniscriptIntrinsics.cs's own header comment. Intrinsic.Create registers
/// into MiniScript's process-wide static registry (same one the stdlib
/// intrinsics use), so EnsureRegistered() only needs to run once - safe to
/// call it every time an Interpreter is about to compile a script.
///
/// Each intrinsic finds out what it's running for via
/// Interpreter.hostData: expose() wants an IExposePropertyHost (either
/// ScriptPropertySchemaCollector, during the editor's dry-run schema
/// collection, or ScriptEntityRunner, during real gameplay); everything
/// else wants a ScriptEntityRunner specifically, and just does nothing if
/// hostData isn't one (e.g. called during schema collection, where there's
/// no real GameObject to act on yet).
/// </summary>
public static class TailboundIntrinsics
{
    private static bool _Registered;

    public static void EnsureRegistered()
    {
        if (_Registered)
            return;
        _Registered = true;

        RegisterExpose();
        RegisterPosition();
        RegisterSprite();
        RegisterColor();
    }

    private static void RegisterExpose()
    {
        var f = Intrinsic.Create("expose");
        f.AddParam("key", "");
        f.AddParam("defaultValue");
        f.code = (context, partialResult) =>
        {
            string key = context.GetLocalString("key");
            Value defaultRaw = context.GetLocal("defaultValue");

            if (context.interpreter?.hostData is IExposePropertyHost host)
            {
                PropertyValue effective = host.OnExpose(key, ToPropertyValue(defaultRaw));
                return new Intrinsic.Result(FromPropertyValue(effective));
            }

            // No host wired up (e.g. a standalone test script) - behave as
            // a harmless identity function.
            return new Intrinsic.Result(defaultRaw);
        };
    }

    private static void RegisterPosition()
    {
        var getPosition = Intrinsic.Create("getPosition");
        getPosition.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is not ScriptEntityRunner runner)
                return Intrinsic.Result.Null;

            Vector3 pos = runner.transform.localPosition;
            var list = new ValList();
            list.values.Add(new ValNumber(pos.x));
            list.values.Add(new ValNumber(pos.y));
            return new Intrinsic.Result(list);
        };

        var setPosition = Intrinsic.Create("setPosition");
        setPosition.AddParam("x", 0);
        setPosition.AddParam("y", 0);
        setPosition.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
            {
                float x = (float)context.GetLocalDouble("x");
                float y = (float)context.GetLocalDouble("y");
                Transform t = runner.transform;
                t.localPosition = new Vector3(x, y, t.localPosition.z);
            }
            return Intrinsic.Result.Null;
        };
    }

    private static void RegisterSprite()
    {
        var f = Intrinsic.Create("setSprite");
        f.AddParam("path", "");
        f.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.ApplySpriteRendererProperty("Sprite", PropertyValue.FromString(context.GetLocalString("path")));
            return Intrinsic.Result.Null;
        };
    }

    private static void RegisterColor()
    {
        var f = Intrinsic.Create("setColor");
        f.AddParam("r", 1);
        f.AddParam("g", 1);
        f.AddParam("b", 1);
        f.AddParam("a", 1);
        f.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
            {
                var color = new Color(
                    (float)context.GetLocalDouble("r"), (float)context.GetLocalDouble("g"),
                    (float)context.GetLocalDouble("b"), (float)context.GetLocalDouble("a"));
                runner.ApplySpriteRendererProperty("Color", PropertyValue.FromColor(color));
            }
            return Intrinsic.Result.Null;
        };
    }

    /// <summary>Number/string only for v1 - matches PropertyType's coverage
    /// of what expose() can infer from a bare MiniScript value. Bool/Int/
    /// Vector2/Color aren't distinguishable from a plain number/string
    /// without an explicit type hint, which expose() doesn't take yet.</summary>
    private static PropertyValue ToPropertyValue(Value raw)
    {
        return raw switch
        {
            ValNumber num => PropertyValue.FromFloat((float)num.value),
            ValString str => PropertyValue.FromString(str.value),
            _ => PropertyValue.FromString(raw != null ? raw.ToString() : ""),
        };
    }

    private static Value FromPropertyValue(PropertyValue value)
    {
        return value.Type switch
        {
            PropertyType.Float => new ValNumber(value.FloatValue),
            PropertyType.Int => new ValNumber(value.IntValue),
            PropertyType.Bool => new ValNumber(value.BoolValue ? 1 : 0),
            PropertyType.String => new ValString(value.StringValue ?? ""),
            _ => ValString.empty,
        };
    }
}
