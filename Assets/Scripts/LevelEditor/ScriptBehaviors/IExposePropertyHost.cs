/// <summary>
/// Whoever's running a ScriptBehavior's Interpreter (see TailboundIntrinsics'
/// "expose" intrinsic) sets itself as that Interpreter.hostData and
/// implements this - two implementors, both under ScriptBehaviors/:
///   - ScriptPropertySchemaCollector: an editor-time dry run that records
///     each expose() call into a PropertyDef list (IScriptPropertySource).
///   - ScriptEntityRunner: real gameplay, resolves the instance's effective
///     value (override, else whatever the script itself passed as default).
/// Same script source works unmodified in both cases - expose() always
/// returns a usable value either way, it just means something different
/// depending on who's asking.
/// </summary>
public interface IExposePropertyHost
{
    PropertyValue OnExpose(string key, PropertyValue defaultValue);
}
