using System.Collections.Generic;
using System.IO;
using Miniscript;
using UnityEngine;

public class ScriptEntityRunner : MonoBehaviour, IExposePropertyHost
{
    // Generous per-frame budget - a safety cap, not a target.
    private const double FrameTimeBudget = 0.05;

    private Interpreter _Interpreter;
    private EntityInstance _Instance;
    private TextAsset _Script;
    private string _CompiledSource;
    private bool _Stopped;

    // Set only for a RuntimeEntityIO-created script; polls the file
    // directly for live-reload (no Unity import pipeline behind it).
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
                // Stop ticking - otherwise a broken compile retries and re-errors every frame.
                _Stopped = true;
            },
        };

        // Runs immediately, like a prefab's Awake().
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

    private static ValMap BuildOtherInfo(GameObject other)
    {
        var info = new ValMap();
        info.map[new ValString("name")] = new ValString(other.name);
        info.map[new ValString("isPlayer")] = new ValNumber(other.layer == LayerMask.NameToLayer("Player") ? 1 : 0);
        info.map[new ValString("x")] = new ValNumber(other.transform.position.x);
        info.map[new ValString("y")] = new ValNumber(other.transform.position.y);
        return info;
    }

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
    // Audio - actions/state on AudioSource itself, so these talk to the
    // component directly instead of going through ApplyComponentProperty.
    // ------------------------------------------------------------------

    private AudioSource EnsureAudioSource()
    {
        var source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();
        return source;
    }

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

    public void PlayAudioOneShot(string resourcesPath, float volumeScale)
    {
        var clip = RuntimeResourceLoader.LoadAudioClip(resourcesPath);
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
