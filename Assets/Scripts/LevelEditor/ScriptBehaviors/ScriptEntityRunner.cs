using System.Collections.Generic;
using System.IO;
using Miniscript;
using UnityEngine;

/// <summary>
/// Runs one ScriptBehavior EntityInstance's MiniScript source as real
/// gameplay - added by LevelInstantiator, never present in the level
/// editor's own uGUI canvas (EntityMarkerView just previews a static icon
/// there, same as any other entity). Owns one Interpreter and supports two
/// execution models, chosen automatically per script:
///
///   - MonoBehaviour-style (recommended): the top-level script just declares
///     properties and defines functions - most notably start() (called once)
///     and update() (called every frame, deltaTime available via the
///     deltaTime intrinsic) - then finishes. Once the top level finishes on
///     its own, this becomes the model: InvokeIfDefined pushes a call to
///     start()/update()/onCollisionEnter()/etc. directly via
///     TAC.Machine.ManuallyPushCall, MiniScript's own documented mechanism
///     for a host to invoke a handler function on demand (see its doc
///     comment in MiniscriptTAC.cs).
///   - Legacy self-driven loop (still supported, e.g. Blinker.ms/.txt): the
///     top-level script itself contains a `while true ... wait ... end
///     while` and never finishes on its own. If the very first run doesn't
///     finish, this entity just keeps resuming that same top-level program
///     every frame via RunUntilDone, exactly as before - start()/update()
///     are never invoked in this case, since the script is already driving
///     itself.
///
/// Both models cooperate with Unity's frame budget the same way: wait()/
/// yield() are stdlib intrinsics that suspend execution, which is what lets
/// RunUntilDone hand control back to Unity instead of running forever in
/// one go.
/// </summary>
public class ScriptEntityRunner : MonoBehaviour, IExposePropertyHost
{
    // Generous per-frame budget - scripts are expected to cooperate via
    // wait()/yield (legacy model) or simply return promptly (update()) well
    // before this, so it's a safety cap, not a target.
    private const double FrameTimeBudget = 0.05;

    private Interpreter _Interpreter;
    private EntityInstance _Instance;
    private TextAsset _Script;
    private string _CompiledSource;
    private bool _Stopped;

    // Set only for a RuntimeEntityIO-created script (see GetSourcePath) -
    // an Editor-imported TextAsset has none of this and instead relies on
    // its own .text updating live, since Unity's asset pipeline watches
    // that file itself; a runtime-constructed TextAsset has no such
    // pipeline behind it, so this polls the file directly instead.
    private string _WatchedFilePath;
    private System.DateTime _WatchedFileLastWriteUtc;

    public void Initialize(TextAsset script, EntityInstance instance)
    {
        _Instance = instance;
        _Script = script;
        _WatchedFilePath = RuntimeEntityIO.GetSourcePath(script);
        if (_WatchedFilePath != null && File.Exists(_WatchedFilePath))
            _WatchedFileLastWriteUtc = File.GetLastWriteTimeUtc(_WatchedFilePath);

        if (script == null)
        {
            Debug.LogWarning($"[ScriptEntityRunner] '{gameObject.name}' has no Script assigned.", this);
            return;
        }

        Compile();
    }

    /// <summary>
    /// (Re)builds the Interpreter from _Script's current text and runs the
    /// top level once - called once from Initialize(), and again from
    /// Update() whenever the text has changed since the last compile (live
    /// reload while editing in Unity or an external editor during Play Mode
    /// / a Play Test). Resets whatever local execution state the old run
    /// had; components it already added (e.g. a SpriteRenderer from
    /// setSprite) are left alone, not removed.
    /// </summary>
    private void Compile()
    {
        TailboundIntrinsics.EnsureRegistered();

        _CompiledSource = _Script.text;
        _Stopped = false;

        string tag = gameObject.name;
        _Interpreter = new Interpreter(_CompiledSource)
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

        // Run the top level once, immediately, rather than waiting for the
        // first Update() - matches a real prefab's Awake() running the
        // instant it's instantiated. Whether this finishes or not decides
        // which of the two execution models (see class comment) applies.
        Tick();

        if (!_Stopped && _Interpreter.done)
            InvokeIfDefined("start");
    }

    private void Update()
    {
        if (HasScriptChanged())
        {
            Debug.Log($"[MiniScript:{gameObject.name}] Script changed, reloading.");
            Compile(); // already ticks (and calls start()) itself this frame
            return;
        }

        if (_Interpreter == null || _Stopped)
            return;

        if (_Interpreter.done)
            InvokeIfDefined("update"); // MonoBehaviour-style model
        else
            Tick(); // legacy self-driven loop, still mid-run
    }

    /// <summary>
    /// Two sources, checked appropriately: an Editor-imported TextAsset
    /// just compares its own (live-updating) .text against what was last
    /// compiled; a runtime-created one (_WatchedFilePath set - see
    /// RuntimeEntityIO) has no Unity import pipeline keeping that current,
    /// so this polls the source file's last-write time on disk instead,
    /// and re-reads + rebuilds _Script from it only when that's actually
    /// changed (cheap file-time check every frame, not a full re-read).
    /// </summary>
    private bool HasScriptChanged()
    {
        if (_WatchedFilePath != null)
        {
            if (!File.Exists(_WatchedFilePath))
                return false;

            var lastWriteUtc = File.GetLastWriteTimeUtc(_WatchedFilePath);
            if (lastWriteUtc == _WatchedFileLastWriteUtc)
                return false;

            _WatchedFileLastWriteUtc = lastWriteUtc;
            _Script = new TextAsset(File.ReadAllText(_WatchedFilePath));
            return _Script.text != _CompiledSource;
        }

        return _Script != null && _Script.text != _CompiledSource;
    }

    private void OnCollisionEnter2D(Collision2D collision) => InvokeIfDefined("onCollisionEnter", BuildOtherInfo(collision.gameObject));
    private void OnCollisionExit2D(Collision2D collision) => InvokeIfDefined("onCollisionExit", BuildOtherInfo(collision.gameObject));
    private void OnTriggerEnter2D(Collider2D other) => InvokeIfDefined("onTriggerEnter", BuildOtherInfo(other.gameObject));
    private void OnTriggerExit2D(Collider2D other) => InvokeIfDefined("onTriggerExit", BuildOtherInfo(other.gameObject));

    /// <summary>Minimal info about the other GameObject in a collision/
    /// trigger - MiniScript has no notion of a GameObject/Component, so
    /// this is a small map rather than a handle. Extend here if a script
    /// needs more (whatever's added, it's still just data - no way for a
    /// script to reach back into the other object's own components).</summary>
    private static ValMap BuildOtherInfo(GameObject other)
    {
        var info = new ValMap();
        info.map[new ValString("name")] = new ValString(other.name);
        info.map[new ValString("isPlayer")] = new ValNumber(other.layer == LayerMask.NameToLayer("Player") ? 1 : 0);
        info.map[new ValString("x")] = new ValNumber(other.transform.position.x);
        info.map[new ValString("y")] = new ValNumber(other.transform.position.y);
        return info;
    }

    /// <summary>
    /// Looks up a global MiniScript function by name and, if defined, calls
    /// it right now via TAC.Machine.ManuallyPushCall - MiniScript's own
    /// documented mechanism for a host to invoke a handler function it
    /// discovered via a global/intrinsic, rather than only ever resuming
    /// wherever the script itself last paused. Does nothing if the name
    /// isn't bound to a function (e.g. a script that doesn't define
    /// onCollisionEnter just never gets it called - not an error).
    /// </summary>
    private void InvokeIfDefined(string functionName, Value argument = null)
    {
        if (_Interpreter == null || _Stopped)
            return;

        if (_Interpreter.GetGlobalValue(functionName) is not ValFunction function)
            return;

        var arguments = argument != null ? new List<Value> { argument } : null;
        _Interpreter.vm.ManuallyPushCall(function, null, arguments);
        _Interpreter.RunUntilDone(FrameTimeBudget, returnEarly: true);
    }

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

    /// <summary>Adds a component if this entity doesn't have one yet, then
    /// applies one property via the matching registered
    /// INativeComponentBinder - reusing the same curated adapters the
    /// NativePrefab side uses, rather than duplicating "which fields are
    /// safe to touch" logic here. Called by TailboundIntrinsics' setSprite/
    /// setColor/setCollider.</summary>
    public void ApplyComponentProperty(string binderTypeId, string key, PropertyValue value)
    {
        if (!NativeComponentBinderRegistry.TryGet(binderTypeId, out var binder))
            return;

        var component = GetComponent(binder.UnityType);
        if (component == null)
            component = gameObject.AddComponent(binder.UnityType);

        binder.Apply(component, new Dictionary<string, PropertyValue> { [key] = value });
    }

    // ------------------------------------------------------------------
    // Audio - Play/PlayOneShot/Stop/isPlaying are actions/state on the
    // AudioSource itself, not settable fields, so they bypass
    // ApplyComponentProperty/INativeComponentBinder (see AudioSourceBinder's
    // own comment) and talk to the component directly instead. Called by
    // TailboundIntrinsics' playAudio/playAudioOneShot/stopAudio/isAudioPlaying.
    // ------------------------------------------------------------------

    private AudioSource EnsureAudioSource()
    {
        var source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();
        return source;
    }

    /// <summary>Plays whatever clip is currently assigned (setAudioClip) -
    /// same as Unity's AudioSource.Play(). No-op with a warning if no clip
    /// has been set, same as Unity would silently do nothing either way.</summary>
    public void PlayAudio()
    {
        var source = EnsureAudioSource();
        if (source.clip == null)
        {
            Debug.LogWarning($"[ScriptEntityRunner] '{gameObject.name}': playAudio called with no clip set (setAudioClip first).", this);
            return;
        }
        source.Play();
    }

    /// <summary>Loads a clip fresh from a Resources path and plays it once,
    /// without disturbing whatever's already assigned/playing - same as
    /// Unity's AudioSource.PlayOneShot(clip, volumeScale), and the more
    /// common of the two ways to play a one-off sound (e.g. from
    /// onTriggerEnter) without needing setAudioClip first.</summary>
    public void PlayAudioOneShot(string resourcesPath, float volumeScale)
    {
        var clip = Resources.Load<AudioClip>(resourcesPath);
        if (clip == null)
        {
            Debug.LogWarning($"[ScriptEntityRunner] '{gameObject.name}': no AudioClip found at Resources path '{resourcesPath}'.", this);
            return;
        }
        EnsureAudioSource().PlayOneShot(clip, volumeScale);
    }

    public void StopAudio() => EnsureAudioSource().Stop();

    public bool IsAudioPlaying()
    {
        var source = GetComponent<AudioSource>();
        return source != null && source.isPlaying;
    }
}
