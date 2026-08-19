using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adapter for AudioSource's config fields (not playback itself - Play/
/// PlayOneShot/Stop are actions, not properties, so they don't fit
/// INativeComponentBinder.Apply's "set these fields" shape; see
/// ScriptEntityRunner.PlayAudio/PlayAudioOneShot/StopAudio and
/// TailboundIntrinsics' playAudio/playAudioOneShot/stopAudio instead).
/// Clip is a Resources-folder path, same convention as SpriteRendererBinder's
/// Sprite, since there's no object-reference PropertyType yet.
/// </summary>
public class AudioSourceBinder : INativeComponentBinder
{
    public Type UnityType => typeof(AudioSource);

    public List<PropertyDef> Schema { get; } = new()
    {
        new PropertyDef { Key = "Clip", Type = PropertyType.String, Label = "Clip (Resources path)" },
        new PropertyDef { Key = "Volume", Type = PropertyType.Float, DefaultValue = PropertyValue.FromFloat(1f), Label = "Volume", HasRange = true, MinValue = 0f, MaxValue = 1f },
        new PropertyDef { Key = "Pitch", Type = PropertyType.Float, DefaultValue = PropertyValue.FromFloat(1f), Label = "Pitch" },
        new PropertyDef { Key = "Loop", Type = PropertyType.Bool, Label = "Loop" },
    };

    public void Apply(Component target, Dictionary<string, PropertyValue> properties)
    {
        if (target is not AudioSource source)
            return;

        if (properties.TryGetValue("Clip", out var clipProp)
            && clipProp.Type == PropertyType.String
            && !string.IsNullOrEmpty(clipProp.StringValue))
        {
            source.clip = Resources.Load<AudioClip>(clipProp.StringValue);
        }

        if (properties.TryGetValue("Volume", out var volumeProp) && volumeProp.Type == PropertyType.Float)
            source.volume = volumeProp.FloatValue;

        if (properties.TryGetValue("Pitch", out var pitchProp) && pitchProp.Type == PropertyType.Float)
            source.pitch = pitchProp.FloatValue;

        if (properties.TryGetValue("Loop", out var loopProp) && loopProp.Type == PropertyType.Bool)
            source.loop = loopProp.BoolValue;
    }
}
