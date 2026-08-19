using Miniscript;
using TarodevController;
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
        RegisterCollider();
        RegisterDeltaTime();
        RegisterPlayer();
        RegisterAudio();
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

        // getPosition is local (relative to the entity's screen), same
        // space every other placement/position value in the level editor
        // uses - not directly comparable to getPlayerPosition's world space
        // unless the entity happens to be on a screen at world origin. This
        // is the one to use for e.g. "direction to the player".
        var getWorldPosition = Intrinsic.Create("getWorldPosition");
        getWorldPosition.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is not ScriptEntityRunner runner)
                return Intrinsic.Result.Null;

            Vector3 pos = runner.transform.position;
            var list = new ValList();
            list.values.Add(new ValNumber(pos.x));
            list.values.Add(new ValNumber(pos.y));
            return new Intrinsic.Result(list);
        };
    }

    private static void RegisterSprite()
    {
        var f = Intrinsic.Create("setSprite");
        f.AddParam("path", "");
        f.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.ApplyComponentProperty("SpriteRenderer", "Sprite", PropertyValue.FromString(context.GetLocalString("path")));
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
                runner.ApplyComponentProperty("SpriteRenderer", "Color", PropertyValue.FromColor(color));
            }
            return Intrinsic.Result.Null;
        };
    }

    /// <summary>
    /// Adds (if missing) and configures a BoxCollider2D - needed for
    /// onCollisionEnter/onTriggerEnter to ever fire at all, since a bare
    /// ScriptBehavior entity starts with nothing but a Transform (and
    /// whatever setSprite/setColor added). isTrigger follows MiniScript's
    /// usual "0 is false, anything else is true" convention.
    /// </summary>
    private static void RegisterCollider()
    {
        var f = Intrinsic.Create("setCollider");
        f.AddParam("width", 1);
        f.AddParam("height", 1);
        f.AddParam("isTrigger", 0);
        f.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
            {
                float width = (float)context.GetLocalDouble("width");
                float height = (float)context.GetLocalDouble("height");
                bool isTrigger = context.GetLocalDouble("isTrigger") != 0;

                runner.ApplyComponentProperty("BoxCollider2D", "Size", PropertyValue.FromVector2(new Vector2(width, height)));
                runner.ApplyComponentProperty("BoxCollider2D", "IsTrigger", PropertyValue.FromBool(isTrigger));
            }
            return Intrinsic.Result.Null;
        };
    }

    /// <summary>
    /// Mirrors Unity's AudioSource API: setAudioClip/setAudioVolume/
    /// setAudioPitch/setAudioLoop configure the component (via
    /// AudioSourceBinder, same as setSprite/setColor/setCollider);
    /// playAudio/playAudioOneShot/stopAudio/isAudioPlaying are the actions,
    /// which don't fit the binder's "set these fields" shape so they call
    /// straight through to ScriptEntityRunner instead - same distinction
    /// Unity itself draws between AudioSource's inspector fields and its
    /// Play()/PlayOneShot()/Stop()/isPlaying members.
    /// </summary>
    private static void RegisterAudio()
    {
        var setClip = Intrinsic.Create("setAudioClip");
        setClip.AddParam("path", "");
        setClip.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.ApplyComponentProperty("AudioSource", "Clip", PropertyValue.FromString(context.GetLocalString("path")));
            return Intrinsic.Result.Null;
        };

        var setVolume = Intrinsic.Create("setAudioVolume");
        setVolume.AddParam("volume", 1);
        setVolume.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.ApplyComponentProperty("AudioSource", "Volume", PropertyValue.FromFloat((float)context.GetLocalDouble("volume")));
            return Intrinsic.Result.Null;
        };

        var setPitch = Intrinsic.Create("setAudioPitch");
        setPitch.AddParam("pitch", 1);
        setPitch.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.ApplyComponentProperty("AudioSource", "Pitch", PropertyValue.FromFloat((float)context.GetLocalDouble("pitch")));
            return Intrinsic.Result.Null;
        };

        var setLoop = Intrinsic.Create("setAudioLoop");
        setLoop.AddParam("loop", 0);
        setLoop.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.ApplyComponentProperty("AudioSource", "Loop", PropertyValue.FromBool(context.GetLocalDouble("loop") != 0));
            return Intrinsic.Result.Null;
        };

        var play = Intrinsic.Create("playAudio");
        play.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.PlayAudio();
            return Intrinsic.Result.Null;
        };

        var playOneShot = Intrinsic.Create("playAudioOneShot");
        playOneShot.AddParam("path", "");
        playOneShot.AddParam("volumeScale", 1);
        playOneShot.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.PlayAudioOneShot(context.GetLocalString("path"), (float)context.GetLocalDouble("volumeScale"));
            return Intrinsic.Result.Null;
        };

        var stop = Intrinsic.Create("stopAudio");
        stop.code = (context, partialResult) =>
        {
            if (context.interpreter?.hostData is ScriptEntityRunner runner)
                runner.StopAudio();
            return Intrinsic.Result.Null;
        };

        var isPlaying = Intrinsic.Create("isAudioPlaying");
        isPlaying.code = (context, partialResult) =>
        {
            bool playing = context.interpreter?.hostData is ScriptEntityRunner runner && runner.IsAudioPlaying();
            return new Intrinsic.Result(playing ? 1 : 0);
        };
    }

    /// <summary>Unity's Time.deltaTime, for an update() function to scale
    /// per-frame movement/animation by - the MiniScript-side equivalent of
    /// reading Time.deltaTime directly in a real MonoBehaviour.Update().</summary>
    private static void RegisterDeltaTime()
    {
        var f = Intrinsic.Create("deltaTime");
        f.code = (context, partialResult) => new Intrinsic.Result(Time.deltaTime);
    }

    /// <summary>
    /// Unlike every other intrinsic here, these don't look at hostData at
    /// all - they act on the one global PlayerController, not "this
    /// entity", so they work the same regardless of which ScriptBehavior
    /// entity (if any) called them. getPlayerSpeed/setPlayerSpeed go
    /// through FrameVelocity/InheritVelocity specifically, not the
    /// Rigidbody2D's own velocity - InheritVelocity is PlayerController's
    /// own sanctioned way to push an external velocity onto the player
    /// (Swing.cs uses the same call for the exact same reason), and
    /// FrameVelocity is what the controller itself is about to apply that
    /// frame - reading the Rigidbody2D directly would race against
    /// whichever runs first in the physics step.
    /// </summary>
    private static void RegisterPlayer()
    {
        var getPlayerPosition = Intrinsic.Create("getPlayerPosition");
        getPlayerPosition.code = (context, partialResult) =>
        {
            var player = FindPlayer();
            if (player == null)
                return Intrinsic.Result.Null;

            Vector3 pos = player.transform.position;
            var list = new ValList();
            list.values.Add(new ValNumber(pos.x));
            list.values.Add(new ValNumber(pos.y));
            return new Intrinsic.Result(list);
        };

        var setPlayerPosition = Intrinsic.Create("setPlayerPosition");
        setPlayerPosition.AddParam("x", 0);
        setPlayerPosition.AddParam("y", 0);
        setPlayerPosition.code = (context, partialResult) =>
        {
            var player = FindPlayer();
            if (player != null)
            {
                float x = (float)context.GetLocalDouble("x");
                float y = (float)context.GetLocalDouble("y");
                Transform t = player.transform;
                t.position = new Vector3(x, y, t.position.z);
            }
            return Intrinsic.Result.Null;
        };

        var getPlayerSpeed = Intrinsic.Create("getPlayerSpeed");
        getPlayerSpeed.code = (context, partialResult) =>
        {
            var player = FindPlayer();
            if (player == null)
                return Intrinsic.Result.Null;

            Vector2 vel = player.FrameVelocity;
            var list = new ValList();
            list.values.Add(new ValNumber(vel.x));
            list.values.Add(new ValNumber(vel.y));
            return new Intrinsic.Result(list);
        };

        var setPlayerSpeed = Intrinsic.Create("setPlayerSpeed");
        setPlayerSpeed.AddParam("x", 0);
        setPlayerSpeed.AddParam("y", 0);
        setPlayerSpeed.code = (context, partialResult) =>
        {
            var player = FindPlayer();
            if (player != null)
            {
                float x = (float)context.GetLocalDouble("x");
                float y = (float)context.GetLocalDouble("y");
                player.InheritVelocity(new Vector2(x, y));
            }
            return Intrinsic.Result.Null;
        };

        // instant defaults to 1 (true) - PlayerController.Kill(true) moves
        // the player offscreen immediately; Kill(false) plays out whatever
        // non-instant death handling LevelManager/LevelLoader already do
        // for e.g. DeathBox. Either way this only starts the death - actual
        // respawn is still LevelLoader's job, same as any other death source.
        var killPlayer = Intrinsic.Create("killPlayer");
        killPlayer.AddParam("instant", 1);
        killPlayer.code = (context, partialResult) =>
        {
            var player = FindPlayer();
            player?.Kill(context.GetLocalDouble("instant") != 0);
            return Intrinsic.Result.Null;
        };
    }

    // Cached rather than FindAnyObjectByType'd on every call (these can run
    // every frame) - Unity's == correctly treats a destroyed/unloaded
    // player as null again, so a stale reference from a previous PlayTest
    // session gets re-resolved automatically rather than staying stuck.
    private static PlayerController _CachedPlayer;

    private static PlayerController FindPlayer()
    {
        if (_CachedPlayer == null)
            _CachedPlayer = Object.FindAnyObjectByType<PlayerController>();
        return _CachedPlayer;
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
