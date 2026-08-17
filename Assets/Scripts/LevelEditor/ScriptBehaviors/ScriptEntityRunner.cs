using System.Collections.Generic;
using Miniscript;
using UnityEngine;

/// <summary>
/// Runs one ScriptBehavior EntityInstance's MiniScript source as real
/// gameplay - added by LevelInstantiator, never present in the level
/// editor's own uGUI canvas (EntityMarkerView just previews a static icon
/// there, same as any other entity). Owns one Interpreter, ticked
/// incrementally: a script is expected to declare its exposed properties
/// up front, then (optionally) loop forever cooperating via wait()/yield -
/// both of those are stdlib intrinsics that suspend execution, which is
/// what lets RunUntilDone hand control back to Unity every frame instead of
/// running the whole loop in one go.
/// </summary>
public class ScriptEntityRunner : MonoBehaviour, IExposePropertyHost
{
    // Generous per-frame budget - scripts are expected to cooperate via
    // wait()/yield well before this, so it's a safety cap, not a target.
    private const double FrameTimeBudget = 0.05;

    private Interpreter _Interpreter;
    private EntityInstance _Instance;
    private bool _Stopped;

    public void Initialize(TextAsset script, EntityInstance instance)
    {
        _Instance = instance;

        if (script == null)
        {
            Debug.LogWarning($"[ScriptEntityRunner] '{gameObject.name}' has no Script assigned.", this);
            return;
        }

        TailboundIntrinsics.EnsureRegistered();

        string tag = gameObject.name;
        _Interpreter = new Interpreter(script.text)
        {
            hostData = this,
            standardOutput = (string s, bool lineBreak) => Debug.Log($"[MiniScript:{tag}] {s}"),
            errorOutput = (string s, bool lineBreak) =>
            {
                Debug.LogError($"[MiniScript:{tag}] {s}", this);
                // Stop ticking a broken script - Interpreter.Compile() would
                // otherwise just retry (and re-report the same error) every
                // single frame forever, since a failed compile leaves its vm
                // null rather than "done".
                _Stopped = true;
            },
        };

        // Run whatever we can immediately (e.g. one-shot setup code with no
        // loop at all) rather than waiting for the first Update() - matches
        // a real prefab's Awake() running the instant it's instantiated.
        Tick();
    }

    private void Update() => Tick();

    /// <summary>
    /// Deliberately does NOT early-out on _Interpreter.done - that's true
    /// both when the script has finished AND before it's ever compiled at
    /// all (Interpreter.done is "vm == null || vm.done", and vm stays null
    /// until RunUntilDone's first call compiles it), so checking it here
    /// used to mean the script would never run a single line. RunUntilDone
    /// itself already no-ops cheaply once actually done, so there's nothing
    /// to gain by duplicating that check.
    /// </summary>
    private void Tick()
    {
        if (_Interpreter == null || _Stopped)
            return;

        _Interpreter.RunUntilDone(FrameTimeBudget, returnEarly: true);
    }

    public PropertyValue OnExpose(string key, PropertyValue defaultValue)
    {
        if (_Instance != null
            && _Instance.ComponentOverrides.TryGetValue(ScriptPropertyResolver.AdapterId, out var overrides)
            && overrides.TryGetValue(key, out var overrideValue))
            return overrideValue;

        return defaultValue;
    }

    /// <summary>Adds a SpriteRenderer if this entity doesn't have one yet,
    /// then applies one property via the existing SpriteRendererBinder -
    /// reusing the same curated adapter the NativePrefab side uses, rather
    /// than duplicating "which SpriteRenderer fields are safe to touch"
    /// logic here. Called by TailboundIntrinsics' setSprite/setColor.</summary>
    public void ApplySpriteRendererProperty(string key, PropertyValue value)
    {
        if (!NativeComponentBinderRegistry.TryGet("SpriteRenderer", out var binder))
            return;

        var renderer = GetComponent<SpriteRenderer>();
        if (renderer == null)
            renderer = gameObject.AddComponent<SpriteRenderer>();

        binder.Apply(renderer, new Dictionary<string, PropertyValue> { [key] = value });
    }
}
